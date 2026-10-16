using FireReady.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>
    /// FR-26 to FR-28: outcome, score, the metrics and every mistake with its time.
    /// Also opens saved sessions from the history screen in read-only mode.
    /// </summary>
    public class SummaryScreen : UIScreen
    {
        [SerializeField] Image outcomeIcon;
        [SerializeField] TMP_Text outcomeTitle;
        [SerializeField] TMP_Text reasonText;
        [SerializeField] Image scoreRing;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text dateText;
        [SerializeField] RowList metrics;
        [SerializeField] RowList mistakes;
        [SerializeField] TMP_Text noMistakesText;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;
        [SerializeField] Button backButton;
        [SerializeField] ScrollRect scroll;

        SessionRecord shownRecord;
        bool fromHistory;

        protected override void BindButtons()
        {
            Bind(restartButton, () => Sim.RestartSession());
            Bind(menuButton, () => Sim.ReturnToMenu());
            Bind(backButton, () => UI.ShowHistory());
        }

        public void ShowLatest()
        {
            fromHistory = false;
            shownRecord = Sim.LastRecord;
            Show();
        }

        public void ShowSaved(SessionRecord record)
        {
            fromHistory = true;
            shownRecord = record;
            Show();
        }

        protected override void OnShow()
        {
            SetActive(restartButton, !fromHistory);
            SetActive(menuButton, !fromHistory);
            SetActive(backButton, fromHistory);
            if (shownRecord != null) Populate(shownRecord);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        void Populate(SessionRecord record)
        {
            var outcome = record.Outcome;
            var color = theme.OutcomeColor(outcome);

            outcomeIcon.sprite = theme.OutcomeIcon(outcome);
            outcomeIcon.color = color;
            outcomeTitle.text = UITheme.OutcomeTitle(outcome);
            outcomeTitle.color = color;
            reasonText.text = record.outcomeReason;

            scoreText.text = record.score.ToString();
            if (scoreRing != null)
            {
                scoreRing.fillAmount = record.score / 100f;
                scoreRing.color = record.score >= 80 ? theme.success : record.score >= 50 ? theme.warning : theme.danger;
            }

            var started = record.StartedLocal;
            if (dateText != null) dateText.text = started.Year > 2000 ? started.ToString("ddd d MMM yyyy, HH:mm") : "";

            metrics.Clear();
            AddMetric(theme.timer, true, "Duration", FormatTime(record.durationSeconds));
            AddMetric(theme.fire, record.firesExtinguished == record.firesTotal, "Fires put out", $"{record.firesExtinguished} of {record.firesTotal}");
            AddMetric(theme.extinguisher, record.WrongAgentUses == 0, "Wrong extinguisher uses",
                record.WrongAgentUses == 0 ? "None" : $"{record.WrongAgentUses} ({record.dangerousAgentUses} dangerous)");
            AddMetric(theme.target, record.aimAccuracy >= 0.6f, "Aim accuracy (spray on the base)", $"{record.aimAccuracy:P0}");
            AddMetric(theme.alarm, record.alarmRaised, "Alarm raised",
                record.alarmRaised ? $"Yes, at {FormatTime(record.alarmRaisedAtSeconds)}" : "No");
            AddMetric(theme.power, record.powerIsolated, "Power isolated", record.powerIsolated ? "Yes" : "No");
            AddMetric(theme.warningIcon, record.tooCloseEvents == 0, "Stood too close", record.tooCloseEvents == 0 ? "Never" : $"{record.tooCloseEvents} times");

            mistakes.Clear();
            foreach (var mistake in record.mistakes)
                mistakes.Add().Set(theme.warningIcon, theme.warning, mistake.detail, null, FormatTime(mistake.atSeconds));
            SetActive(noMistakesText, record.mistakes.Count == 0);
        }

        // Each metric shows an icon, text and a good/bad colour, never colour alone.
        void AddMetric(Sprite icon, bool good, string label, string value)
        {
            var row = metrics.Add();
            row.Set(icon, good ? theme.success : theme.warning, label, null, value);
            row.SetValueColor(good ? theme.textPrimary : theme.warning);
        }
    }
}
