using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // FR-01: title, one-line description, Start and History.
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] Button startButton;
        [SerializeField] Button historyButton;
        [SerializeField] Button settingsButton;
        [SerializeField] TMP_Text lastSessionText;
        [SerializeField] GameObject unsupportedPanel;
        [SerializeField] TMP_Text unsupportedText;

        protected override void BindButtons()
        {
            Bind(startButton, () => Sim.StartTraining());
            Bind(historyButton, () => UI.ShowHistory());
            Bind(settingsButton, () => UI.ShowSettings());
        }

        void OnEnable()
        {
            EventBus.Subscribe<ARAvailabilityChecked>(OnAvailability);
            EventBus.Subscribe<HistoryCleared>(OnHistoryCleared);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ARAvailabilityChecked>(OnAvailability);
            EventBus.Unsubscribe<HistoryCleared>(OnHistoryCleared);
        }

        protected override void OnShow()
        {
            RefreshSupport(Sim == null || Sim.ARSupported, null);
            RefreshLastSession();
        }

        void OnAvailability(ARAvailabilityChecked evt) => RefreshSupport(evt.Supported, evt.Message);

        void OnHistoryCleared(HistoryCleared evt) => RefreshLastSession();

        // NFR-09: a clear message instead of a crash on phones without ARCore.
        void RefreshSupport(bool supported, string message)
        {
            if (startButton != null) startButton.interactable = supported;
            SetActive(unsupportedPanel, !supported);
            if (!supported && unsupportedText != null && !string.IsNullOrEmpty(message)) unsupportedText.text = message;
        }

        void RefreshLastSession()
        {
            if (lastSessionText == null || Sim == null || Sim.Data == null) return;

            var recent = Sim.Data.LoadRecent(1);
            if (recent.Count == 0)
            {
                lastSessionText.text = "No sessions yet. Your results are saved on this phone.";
                return;
            }

            var last = recent[0];
            lastSessionText.text = $"Last session: {UITheme.OutcomeTitle(last.Outcome)}, score {last.score}  ({Sim.Data.SessionCount} saved)";
        }
    }
}
