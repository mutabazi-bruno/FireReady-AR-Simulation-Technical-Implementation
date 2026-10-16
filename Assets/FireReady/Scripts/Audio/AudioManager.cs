using System.Collections.Generic;
using FireReady.Config;
using FireReady.Core;
using FireReady.Data;
using FireReady.Effects;
using FireReady.Simulation;
using UnityEngine;

namespace FireReady.Audio
{
    /// <summary>
    /// Plays looping and one-shot sounds in response to events. Nothing in the simulation calls
    /// this class directly. Also triggers vibration for critical mistakes when it is turned on.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] PoolManager pools;
        [SerializeField] DataManager data;

        [Header("Category volumes")]
        [SerializeField, Range(0f, 1f)] float interfaceVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] float effectsVolume = 1f;
        [SerializeField, Range(0f, 1f)] float ambientVolume = 0.6f;

        [Header("Loops")]
        [SerializeField] AudioSource sprayLoop;
        [SerializeField] AudioSource alarmLoop;
        [SerializeField] float hitSoundInterval = 0.5f;

        readonly Dictionary<AudioCueId, AudioCue> cues = new Dictionary<AudioCueId, AudioCue>();
        readonly Dictionary<FireSource, AudioSource> fireLoops = new Dictionary<FireSource, AudioSource>();
        readonly List<AudioCueTable> tables = new List<AudioCueTable>();
        float lastHitSound;

        void OnEnable()
        {
            EventBus.Subscribe<UiClicked>(OnUiClicked);
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<SessionStarted>(OnSessionStarted);
            EventBus.Subscribe<SessionEnded>(OnSessionEnded);
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Subscribe<ExtinguisherSelected>(OnExtinguisherSelected);
            EventBus.Subscribe<PinPulled>(OnPinPulled);
            EventBus.Subscribe<DischargeBlocked>(OnDischargeBlocked);
            EventBus.Subscribe<SprayStarted>(OnSprayStarted);
            EventBus.Subscribe<SprayStopped>(OnSprayStopped);
            EventBus.Subscribe<ExtinguisherEmpty>(OnExtinguisherEmpty);
            EventBus.Subscribe<AgentApplied>(OnAgentApplied);
            EventBus.Subscribe<DistanceWarning>(OnDistanceWarning);
            EventBus.Subscribe<FireIgnited>(OnFireIgnited);
            EventBus.Subscribe<FireStateChanged>(OnFireStateChanged);
            EventBus.Subscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Subscribe<FireReignited>(OnFireReignited);
            EventBus.Subscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Subscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Subscribe<TaskCompleted>(OnTaskCompleted);
            EventBus.Subscribe<MistakeRecorded>(OnMistakeRecorded);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<UiClicked>(OnUiClicked);
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<SessionStarted>(OnSessionStarted);
            EventBus.Unsubscribe<SessionEnded>(OnSessionEnded);
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Unsubscribe<ExtinguisherSelected>(OnExtinguisherSelected);
            EventBus.Unsubscribe<PinPulled>(OnPinPulled);
            EventBus.Unsubscribe<DischargeBlocked>(OnDischargeBlocked);
            EventBus.Unsubscribe<SprayStarted>(OnSprayStarted);
            EventBus.Unsubscribe<SprayStopped>(OnSprayStopped);
            EventBus.Unsubscribe<ExtinguisherEmpty>(OnExtinguisherEmpty);
            EventBus.Unsubscribe<AgentApplied>(OnAgentApplied);
            EventBus.Unsubscribe<DistanceWarning>(OnDistanceWarning);
            EventBus.Unsubscribe<FireIgnited>(OnFireIgnited);
            EventBus.Unsubscribe<FireStateChanged>(OnFireStateChanged);
            EventBus.Unsubscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Unsubscribe<FireReignited>(OnFireReignited);
            EventBus.Unsubscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Unsubscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Unsubscribe<TaskCompleted>(OnTaskCompleted);
            EventBus.Unsubscribe<MistakeRecorded>(OnMistakeRecorded);
        }

        void Start()
        {
            ApplySettings();
        }

        // Cue tables come from Addressables: the core table at start, the scenario table after placement.
        public void RegisterCues(AudioCueTable table)
        {
            if (table == null || tables.Contains(table)) return;
            tables.Add(table);
            RebuildLookup();
        }

        public void UnregisterCues(AudioCueTable table)
        {
            if (table == null || !tables.Remove(table)) return;
            StopAllLoops();
            RebuildLookup();
        }

        void RebuildLookup()
        {
            cues.Clear();
            foreach (var table in tables)
                foreach (var cue in table.cues)
                    if (cue.clip != null) cues[cue.id] = cue;
        }

        public void ApplySettings()
        {
            if (data != null && data.Settings != null) AudioListener.volume = data.Settings.MasterVolume;
        }

        public void Play(AudioCueId id, Vector3? position = null, float volumeScale = 1f, float pitch = 1f)
        {
            if (!cues.TryGetValue(id, out var cue) || pools == null) return;

            var source = pools.GetAudioSource(position ?? Vector3.zero);
            float spatial = position.HasValue ? cue.spatialBlend : 0f;
            bool isInterface = cue.category == AudioCategory.Interface;
            source.Play(cue.clip, cue.volume * CategoryVolume(cue.category) * volumeScale, pitch, spatial, isInterface);
        }

        float CategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Interface: return interfaceVolume;
                case AudioCategory.Ambient: return ambientVolume;
                default: return effectsVolume;
            }
        }

        void StartLoop(AudioSource source, AudioCueId id, float volumeScale = 1f)
        {
            if (source == null || !cues.TryGetValue(id, out var cue)) return;
            source.clip = cue.clip;
            source.loop = true;
            source.volume = cue.volume * CategoryVolume(cue.category) * volumeScale;
            if (!source.isPlaying) source.Play();
        }

        void StopAllLoops()
        {
            if (sprayLoop != null) sprayLoop.Stop();
            if (alarmLoop != null) alarmLoop.Stop();
            foreach (var loop in fireLoops.Values)
                if (loop != null) loop.Stop();
            fireLoops.Clear();
        }

        void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (data != null && data.Settings != null && data.Settings.VibrationEnabled) Handheld.Vibrate();
#endif
        }

        // Event handlers

        void OnUiClicked(UiClicked evt) => Play(AudioCueId.UiClick);

        void OnStateChanged(SimulationStateChanged evt)
        {
            // Pausing the listener freezes every world sound; interface sounds ignore the pause.
            AudioListener.pause = evt.Current == SimulationStateId.Paused || evt.Current == SimulationStateId.ARSetup;
            if (evt.Current == SimulationStateId.MainMenu) StopAllLoops();
        }

        void OnSessionStarted(SessionStarted evt) => Play(AudioCueId.SessionStart);

        void OnSessionEnded(SessionEnded evt)
        {
            StopAllLoops();
            Play(evt.Outcome == SessionOutcome.Completed || evt.Outcome == SessionOutcome.Evacuated
                ? AudioCueId.SessionSuccess
                : AudioCueId.SessionFail);
        }

        void OnScenePlaced(ScenePlaced evt) => Play(AudioCueId.Placement);

        void OnExtinguisherSelected(ExtinguisherSelected evt) => Play(AudioCueId.Select);

        void OnPinPulled(PinPulled evt) => Play(AudioCueId.PinPulled);

        void OnDischargeBlocked(DischargeBlocked evt) => Play(AudioCueId.EmptyClick);

        void OnSprayStarted(SprayStarted evt)
        {
            StartLoop(sprayLoop, AudioCueId.SprayLoop);
            if (sprayLoop != null)
                sprayLoop.pitch = evt.Extinguisher.Type == AgentType.CO2 ? 1.25f
                    : evt.Extinguisher.Type == AgentType.Water ? 0.85f
                    : 1f;
        }

        void OnSprayStopped(SprayStopped evt)
        {
            if (sprayLoop != null) sprayLoop.Stop();
        }

        void OnExtinguisherEmpty(ExtinguisherEmpty evt) => Play(AudioCueId.EmptyClick);

        void OnAgentApplied(AgentApplied evt)
        {
            if (evt.Effect != AgentEffect.Effective && evt.Effect != AgentEffect.Weak) return;
            if (Time.time - lastHitSound < hitSoundInterval) return;
            lastHitSound = Time.time;
            Play(AudioCueId.EffectiveHit, evt.Fire.transform.position, evt.Effect == AgentEffect.Weak ? 0.5f : 1f);
        }

        void OnDistanceWarning(DistanceWarning evt) => Play(AudioCueId.DistanceWarning);

        void OnFireIgnited(FireIgnited evt)
        {
            if (!cues.ContainsKey(AudioCueId.FireCrackleLoop)) return;

            var loop = evt.Fire.gameObject.AddComponent<AudioSource>();
            loop.spatialBlend = 1f;
            loop.minDistance = 0.6f;
            loop.maxDistance = 10f;
            loop.rolloffMode = AudioRolloffMode.Linear;
            loop.playOnAwake = false;
            fireLoops[evt.Fire] = loop;
            UpdateFireLoop(evt.Fire, FireState.Small);
        }

        void OnFireStateChanged(FireStateChanged evt) => UpdateFireLoop(evt.Fire, evt.Current);

        // FR-10: each state has its own sound. Small and Growing crackle at different levels, out of control roars.
        void UpdateFireLoop(FireSource fire, FireState state)
        {
            if (!fireLoops.TryGetValue(fire, out var loop) || loop == null) return;

            switch (state)
            {
                case FireState.Small:
                    StartLoop(loop, AudioCueId.FireCrackleLoop, 0.45f);
                    loop.pitch = 1f;
                    break;
                case FireState.Growing:
                    StartLoop(loop, AudioCueId.FireCrackleLoop, 0.85f);
                    loop.pitch = 0.9f;
                    break;
                case FireState.OutOfControl:
                    if (cues.ContainsKey(AudioCueId.FireRoarLoop))
                    {
                        loop.Stop();
                        StartLoop(loop, AudioCueId.FireRoarLoop, 1f);
                    }
                    break;
                default:
                    loop.Stop();
                    break;
            }

            // Swap back from the roar when the fire comes down again.
            if (state != FireState.OutOfControl && cues.TryGetValue(AudioCueId.FireCrackleLoop, out var crackle)
                && loop.clip != crackle.clip && loop.isPlaying)
            {
                loop.Stop();
                StartLoop(loop, AudioCueId.FireCrackleLoop, state == FireState.Growing ? 0.85f : 0.45f);
            }
        }

        void OnFireExtinguished(FireExtinguished evt)
        {
            if (fireLoops.TryGetValue(evt.Fire, out var loop) && loop != null) loop.Stop();
            Play(AudioCueId.Steam, evt.Fire.transform.position);
            Play(AudioCueId.FireExtinguished);
        }

        void OnFireReignited(FireReignited evt) => Play(AudioCueId.Reignite, evt.Fire.transform.position);

        void OnAlarmRaised(AlarmRaised evt) => StartLoop(alarmLoop, AudioCueId.AlarmBell, 0.35f);

        void OnPowerIsolated(PowerIsolated evt) => Play(AudioCueId.BreakerSwitch);

        void OnTaskCompleted(TaskCompleted evt) => Play(AudioCueId.TaskCompleted);

        void OnMistakeRecorded(MistakeRecorded evt)
        {
            Play(evt.Critical ? AudioCueId.WrongAgent : AudioCueId.Error);
            if (evt.Critical) Vibrate();
        }
    }
}
