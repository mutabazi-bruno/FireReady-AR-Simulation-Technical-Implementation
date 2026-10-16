using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // FR-30 and FR-32: the most recent sessions, newest first, and clearing the history.
    public class HistoryScreen : UIScreen
    {
        [SerializeField] RowList rows;
        [SerializeField] TMP_Text emptyText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Button backButton;
        [SerializeField] Button clearButton;
        [SerializeField] int sessionsShown = 10;

        protected override void BindButtons()
        {
            Bind(backButton, () => UI.ShowMainMenu());
            Bind(clearButton, () => UI.Confirm.Ask(
                "Clear session history?",
                "Every saved session on this phone will be deleted. This cannot be undone.",
                "Clear",
                () =>
                {
                    Sim.Data.Clear();
                    Refresh();
                }));
        }

        protected override void OnShow()
        {
            Refresh();
        }

        void Refresh()
        {
            var sessions = Sim.Data.LoadRecent(sessionsShown);
            rows.Clear();

            foreach (var record in sessions)
            {
                var outcome = record.Outcome;
                var started = record.StartedLocal;
                var row = rows.Add();
                row.Set(theme.OutcomeIcon(outcome), theme.OutcomeColor(outcome),
                    $"{UITheme.OutcomeTitle(outcome)}  -  score {record.score}",
                    $"{(started.Year > 2000 ? started.ToString("d MMM, HH:mm") : "-")}   {FormatTime(record.durationSeconds)}   {record.mistakes.Count} {(record.mistakes.Count == 1 ? "mistake" : "mistakes")}",
                    ">");

                var selected = record;
                if (row.Button != null) row.Button.onClick.AddListener(() => UI.ShowSessionDetail(selected));
            }

            SetActive(emptyText, sessions.Count == 0);
            if (clearButton != null) clearButton.interactable = sessions.Count > 0;
            if (countText != null) countText.text = sessions.Count > 0 ? $"Newest first. {Sim.Data.SessionCount} saved on this phone." : "";
        }
    }
}
