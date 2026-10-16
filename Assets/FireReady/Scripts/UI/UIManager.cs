using FireReady.Core;
using FireReady.Data;
using FireReady.Flow;
using UnityEngine;

namespace FireReady.UI
{
    /// <summary>
    /// Shows the right screen for each simulation state and handles navigation between the
    /// menu screens. Sends user choices to the SimulationManager through the screens.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] MainMenuScreen mainMenu;
        [SerializeField] BriefingScreen briefing;
        [SerializeField] ARSetupScreen arSetup;
        [SerializeField] HudScreen hud;
        [SerializeField] PauseScreen pause;
        [SerializeField] SummaryScreen summary;
        [SerializeField] HistoryScreen history;
        [SerializeField] SettingsScreen settings;

        [Header("Overlays")]
        [SerializeField] ConfirmDialog confirm;
        [SerializeField] LoadingOverlay loading;
        [SerializeField] TrackingOverlay tracking;
        [SerializeField] DiagnosticsOverlay diagnostics;

        UIScreen[] screens;
        string trackingMessage = "Tracking lost. Point the camera at the floor where the scene was placed.";

        public ConfirmDialog Confirm => confirm;

        void Awake()
        {
            screens = new UIScreen[] { mainMenu, briefing, arSetup, hud, pause, summary, history, settings };
            foreach (var screen in screens)
            {
                if (screen == null) continue;
                screen.Init(this);
                screen.Hide();
            }

            foreach (var overlay in new UIScreen[] { confirm, loading, tracking })
            {
                if (overlay == null) continue;
                overlay.Init(this);
                overlay.Hide();
            }
        }

        void Start()
        {
            var sim = SimulationManager.Instance;
            SetDiagnosticsVisible(sim != null && sim.Data != null && sim.Data.Settings.ShowDiagnostics);
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<ScenarioLoading>(OnScenarioLoading);
            EventBus.Subscribe<TrackingLost>(OnTrackingLost);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<ScenarioLoading>(OnScenarioLoading);
            EventBus.Unsubscribe<TrackingLost>(OnTrackingLost);
        }

        void OnStateChanged(SimulationStateChanged evt)
        {
            if (confirm != null) confirm.Hide();
            if (tracking != null) tracking.Hide();

            switch (evt.Current)
            {
                case SimulationStateId.MainMenu:
                    ShowOnly(mainMenu);
                    break;
                case SimulationStateId.Briefing:
                    ShowOnly(briefing);
                    break;
                case SimulationStateId.ARSetup:
                    ShowOnly(arSetup);
                    break;
                case SimulationStateId.Running:
                    ShowOnly(hud);
                    break;
                case SimulationStateId.Paused:
                    // The HUD stays underneath so the trainee can see where they left off.
                    var sim = SimulationManager.Instance;
                    if (sim != null && sim.PausedByTracking && tracking != null)
                    {
                        tracking.SetMessage(trackingMessage);
                        tracking.Show();
                    }
                    else if (pause != null)
                    {
                        pause.Show();
                    }
                    break;
                case SimulationStateId.Summary:
                    ShowOnly(null);
                    summary.ShowLatest();
                    break;
            }
        }

        void ShowOnly(UIScreen target)
        {
            foreach (var screen in screens)
                if (screen != null && screen != target) screen.Hide();
            if (target != null) target.Show();
        }

        // Navigation inside the main menu state

        public void ShowMainMenu() => ShowOnly(mainMenu);

        public void ShowHistory() => ShowOnly(history);

        public void ShowSettings() => ShowOnly(settings);

        public void ShowSessionDetail(SessionRecord record)
        {
            ShowOnly(null);
            summary.ShowSaved(record);
        }

        public void ShowPauseMenu()
        {
            if (tracking != null) tracking.Hide();
            if (pause != null) pause.Show();
        }

        public void SetDiagnosticsVisible(bool visible)
        {
            if (diagnostics != null) diagnostics.gameObject.SetActive(visible);
        }

        void OnScenarioLoading(ScenarioLoading evt)
        {
            if (loading == null) return;
            if (evt.IsLoading) loading.ShowMessage("Loading the training scene...");
            else loading.Hide();
        }

        void OnTrackingLost(TrackingLost evt)
        {
            if (!string.IsNullOrEmpty(evt.Reason)) trackingMessage = evt.Reason;
            if (tracking != null && tracking.IsVisible) tracking.SetMessage(trackingMessage);
        }
    }
}
