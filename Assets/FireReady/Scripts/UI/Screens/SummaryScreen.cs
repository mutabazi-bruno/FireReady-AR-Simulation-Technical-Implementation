using System.Collections;
using FireReady.Data;
using FireReady.Simulation;
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

        [Header("Outcome stamp")]
        [SerializeField] RectTransform stamp;
        [SerializeField] Image stampFrame;
        [SerializeField] TMP_Text stampText;
        [SerializeField] float stampAngle = -8f;

        Coroutine reveal;

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
            if (shownRecord != null)
            {
                if (reveal != null) StopCoroutine(reveal);
                reveal = StartCoroutine(Reveal(shownRecord));
            }
        }

        // The score ring fills while the number counts up, then the outcome stamp slams down.
        IEnumerator Reveal(SessionRecord record)
        {
            float target = record.score / 100f;
            if (stamp != null) stamp.gameObject.SetActive(false);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.9f)
            {
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (scoreRing != null) scoreRing.fillAmount = target * eased;
                if (scoreText != null) scoreText.text = Mathf.RoundToInt(record.score * eased).ToString();
                yield return null;
            }
            if (scoreRing != null) scoreRing.fillAmount = target;
            if (scoreText != null) scoreText.text = record.score.ToString();

            if (stamp == null) yield break;
            stamp.gameObject.SetActive(true);
            var group = stamp.GetComponent<CanvasGroup>();
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
            {
                stamp.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, t * t);
                if (group != null) group.alpha = Mathf.Clamp01(t * 1.5f);
                yield return null;
            }
            stamp.localScale = Vector3.one;
            if (group != null) group.alpha = 1f;
            reveal = null;
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

            if (stampText != null) stampText.text = UITheme.OutcomeTitle(outcome);
            if (stampText != null) stampText.color = color;
            if (stampFrame != null) stampFrame.color = color;
            if (stamp != null) stamp.localEulerAngles = new Vector3(0, 0, stampAngle);

            mistakes.Clear();
            foreach (var mistake in record.mistakes)
            {
                MistakeStyle(mistake.code, out var icon, out var iconColor);
                mistakes.Add().Set(icon, iconColor, mistake.detail, null, FormatTime(mistake.atSeconds));
            }
            SetActive(noMistakesText, record.mistakes.Count == 0);
        }

        // Each kind of mistake gets its own pictogram and sign shape so they can be told apart at a glance.
        void MistakeStyle(string code, out Sprite icon, out Color color)
        {
            switch (code)
            {
                case PerformanceTracker.Codes.FatalAgent:
                case PerformanceTracker.Codes.DangerousAgent:
                    icon = theme.fire; color = theme.danger; break;
                case PerformanceTracker.Codes.WeakAgent:
                case PerformanceTracker.Codes.EmptyWithFire:
                    icon = theme.extinguisher; color = theme.warning; break;
                case PerformanceTracker.Codes.NoAlarm:
                    icon = theme.alarm; color = theme.danger; break;
                case PerformanceTracker.Codes.LiveElectrical:
                    icon = theme.power; color = theme.warning; break;
                case PerformanceTracker.Codes.LowAim:
                    icon = theme.target; color = theme.info; break;
                case PerformanceTracker.Codes.OutOfControl:
                    icon = theme.exit; color = theme.danger; break;
                default:
                    icon = theme.warningIcon; color = theme.warning; break;
            }
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
