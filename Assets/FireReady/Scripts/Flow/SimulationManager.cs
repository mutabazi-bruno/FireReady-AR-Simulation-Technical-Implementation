using System;
using FireReady.AR;
using FireReady.Audio;
using FireReady.Config;
using FireReady.Content;
using FireReady.Core;
using FireReady.Data;
using FireReady.Effects;
using FireReady.Simulation;
using UnityEngine;

namespace FireReady.Flow
{
    /// <summary>
    /// Owns the session state machine and decides when to move between Start, Setup, Simulation
    /// and Completion. The only singleton in the app: the UI reaches it from several screens.
    /// Other systems get their references through the inspector.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] ARManager arManager;
        [SerializeField] AssetManager assetManager;
        [SerializeField] EntityManager entityManager;
        [SerializeField] InteractionManager interactionManager;
        [SerializeField] TaskManager taskManager;
        [SerializeField] PerformanceTracker performanceTracker;
        [SerializeField] DataManager dataManager;
        [SerializeField] PoolManager poolManager;
        [SerializeField] AudioManager audioManager;

        [Header("Scenario")]
        [SerializeField] string scenarioKey = "Scenario_WorkshopOffice/Config";
        [SerializeField] int targetFrameRate = 60;

        SimulationStateMachine stateMachine;
        MainMenuState mainMenuState;
        BriefingState briefingState;
        ARSetupState arSetupState;
        RunningState runningState;
        PausedState pausedState;
        SummaryState summaryState;

        bool hasPendingEnd;
        SessionOutcome pendingOutcome;
        string pendingReason;
        bool recordNeedsSaving;

        public ARManager AR => arManager;
        public AssetManager Assets => assetManager;
        public EntityManager Entities => entityManager;
        public InteractionManager Interaction => interactionManager;
        public TaskManager Tasks => taskManager;
        public PerformanceTracker Performance => performanceTracker;
        public DataManager Data => dataManager;
        public PoolManager Pools => poolManager;
        public AudioManager Audio => audioManager;

        public SimulationStateId CurrentState => stateMachine?.CurrentId ?? SimulationStateId.None;
        public bool IsReposition => arSetupState != null && arSetupState.IsReposition;
        public bool PausedByTracking => pausedState != null && pausedState.PausedByTracking;
        public float SessionTime { get; private set; }
        public bool SessionActive { get; private set; }
        public bool ARSupported { get; private set; } = true;
        public SessionRecord LastRecord { get; private set; }
        public ScenarioConfig ScenarioConfig => assetManager != null && assetManager.Scenario != null ? assetManager.Scenario.Config : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            stateMachine = new SimulationStateMachine();
            mainMenuState = new MainMenuState(this);
            briefingState = new BriefingState(this);
            arSetupState = new ARSetupState(this);
            runningState = new RunningState(this);
            pausedState = new PausedState(this);
            summaryState = new SummaryState(this);
        }

        void OnEnable()
        {
            EventBus.Subscribe<ARAvailabilityChecked>(OnARAvailabilityChecked);
            EventBus.Subscribe<TrackingLost>(OnTrackingLost);
            EventBus.Subscribe<TrackingRestored>(OnTrackingRestored);
            EventBus.Subscribe<EvacuationChosen>(OnEvacuationChosen);
            EventBus.Subscribe<FireBeyondControl>(OnFireBeyondControl);
            EventBus.Subscribe<AgentApplied>(OnAgentApplied);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ARAvailabilityChecked>(OnARAvailabilityChecked);
            EventBus.Unsubscribe<TrackingLost>(OnTrackingLost);
            EventBus.Unsubscribe<TrackingRestored>(OnTrackingRestored);
            EventBus.Unsubscribe<EvacuationChosen>(OnEvacuationChosen);
            EventBus.Unsubscribe<FireBeyondControl>(OnFireBeyondControl);
            EventBus.Unsubscribe<AgentApplied>(OnAgentApplied);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        async void Start()
        {
            stateMachine.ChangeState(mainMenuState);

            try
            {
                var coreCues = await assetManager.LoadCoreAsync();
                if (audioManager != null) audioManager.RegisterCues(coreCues);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void Update()
        {
            // Session ends requested by events are applied here, after every listener has seen the event.
            if (hasPendingEnd)
            {
                hasPendingEnd = false;
                EndSession(pendingOutcome, pendingReason);
            }

            stateMachine.Tick(Time.deltaTime);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && CurrentState == SimulationStateId.Running) PauseSession();
        }

        // Commands from the UI

        public void StartTraining()
        {
            if (!ARSupported) return;
            stateMachine.ChangeState(briefingState);
        }

        public void ContinueFromBriefing()
        {
            arSetupState.Prepare(reposition: false);
            stateMachine.ChangeState(arSetupState);
        }

        public void ConfirmPlacement()
        {
            if (CurrentState == SimulationStateId.ARSetup) arManager.ConfirmPlacement();
        }

        public void PauseSession(bool byTracking = false)
        {
            if (CurrentState != SimulationStateId.Running) return;
            pausedState.Prepare(byTracking);
            stateMachine.ChangeState(pausedState);
        }

        public void ResumeSession()
        {
            if (CurrentState == SimulationStateId.Paused) stateMachine.ChangeState(runningState);
        }

        public void Reposition()
        {
            if (CurrentState != SimulationStateId.Paused) return;
            arSetupState.Prepare(reposition: true);
            stateMachine.ChangeState(arSetupState);
        }

        // FR-28: a new session in the same placed scene.
        public void RestartSession()
        {
            if (SessionActive)
            {
                FinishSession(SessionOutcome.Abandoned, "Restarted from the pause menu");
                SaveLastSession();
            }

            interactionManager.ResetInteraction();
            entityManager.ResetScenario();
            SessionActive = false;
            stateMachine.ChangeState(runningState);
        }

        public void QuitToMenu()
        {
            if (SessionActive)
            {
                FinishSession(SessionOutcome.Abandoned, "Quit from the pause menu");
                SaveLastSession();
            }
            stateMachine.ChangeState(mainMenuState);
        }

        public void ReturnToMenu()
        {
            if (SessionActive) QuitToMenu();
            else stateMachine.ChangeState(mainMenuState);
        }

        // Session lifecycle

        internal async void OnScenePlaced(bool reposition)
        {
            if (reposition && entityManager.HasScene)
            {
                // Same session carries on from where it was paused.
                stateMachine.ChangeState(runningState);
                return;
            }

            try
            {
                var scenario = await assetManager.LoadScenarioAsync(scenarioKey);
                if (CurrentState != SimulationStateId.ARSetup) return;

                if (audioManager != null) audioManager.RegisterCues(scenario.Config.audioCues);
                poolManager.CreateEffectPools(scenario.EffectPrefabs);
                interactionManager.Configure(scenario.Config.rules);
                entityManager.BuildScene(arManager.ScenarioRoot, scenario);
                stateMachine.ChangeState(runningState);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EventBus.Publish(new HintRequested { Message = "The training scene could not be loaded. Please try again." });
                stateMachine.ChangeState(mainMenuState);
            }
        }

        public void BeginSession()
        {
            var config = ScenarioConfig;
            SessionTime = 0f;
            SessionActive = true;
            LastRecord = null;
            recordNeedsSaving = false;
            hasPendingEnd = false;

            entityManager.BeginSchedule();
            taskManager.BeginSession(config, () => SessionTime);
            performanceTracker.BeginSession(config, () => SessionTime);
            EventBus.Publish(new SessionStarted { ScenarioId = config.scenarioId });
        }

        public void AdvanceClock(float dt)
        {
            SessionTime += dt;
        }

        public void EndSession(SessionOutcome outcome, string reason)
        {
            if (!SessionActive) return;
            FinishSession(outcome, reason);
            stateMachine.ChangeState(summaryState);
        }

        void RequestEnd(SessionOutcome outcome, string reason)
        {
            if (!SessionActive || hasPendingEnd) return;
            hasPendingEnd = true;
            pendingOutcome = outcome;
            pendingReason = reason;
        }

        void FinishSession(SessionOutcome outcome, string reason)
        {
            SessionActive = false;
            hasPendingEnd = false;
            interactionManager.SetInputEnabled(false);
            interactionManager.DropHeld();
            taskManager.EndSession();

            LastRecord = performanceTracker.BuildRecord(outcome, reason, SessionTime,
                entityManager.TotalFires, entityManager.FiresExtinguished);
            recordNeedsSaving = true;

            EventBus.Publish(new SessionEnded { Outcome = outcome, Reason = reason });
        }

        public void SaveLastSession()
        {
            if (!recordNeedsSaving || LastRecord == null) return;
            recordNeedsSaving = false;
            dataManager.Save(LastRecord);
        }

        // Order matters: instances first, then pools, then the Addressables handles.
        public void TeardownScenario()
        {
            SessionActive = false;
            hasPendingEnd = false;
            interactionManager.ResetInteraction();
            entityManager.ClearScene();
            poolManager.ClearEffects();
            if (audioManager != null && ScenarioConfig != null) audioManager.UnregisterCues(ScenarioConfig.audioCues);
            assetManager.ReleaseScenario();
            arManager.ResetPlacement();
        }

        // Event handlers

        void OnARAvailabilityChecked(ARAvailabilityChecked evt)
        {
            ARSupported = evt.Supported;
        }

        void OnTrackingLost(TrackingLost evt)
        {
            if (CurrentState == SimulationStateId.Running) PauseSession(byTracking: true);
        }

        void OnTrackingRestored(TrackingRestored evt)
        {
            if (CurrentState == SimulationStateId.Paused && pausedState.PausedByTracking) ResumeSession();
        }

        void OnEvacuationChosen(EvacuationChosen evt)
        {
            bool anyOutOfControl = false;
            foreach (var fire in entityManager.Fires)
                if (fire.State == FireState.OutOfControl) anyOutOfControl = true;

            RequestEnd(SessionOutcome.Evacuated, anyOutOfControl
                ? "Evacuated from a fire that was out of control. The right call."
                : "Left the building while the fires could still be fought");
        }

        void OnFireBeyondControl(FireBeyondControl evt)
        {
            RequestEnd(SessionOutcome.Failed, $"Stayed with the out-of-control {evt.Fire.DisplayName} fire instead of evacuating");
        }

        // FR-20: water on a live electrical fire ends the session at once.
        void OnAgentApplied(AgentApplied evt)
        {
            if (evt.Effect == AgentEffect.Fatal)
                RequestEnd(SessionOutcome.Failed, $"{EnumText.AgentName(evt.Agent)} on a live electrical fire. In real life this is an electric shock.");
        }
    }
}
