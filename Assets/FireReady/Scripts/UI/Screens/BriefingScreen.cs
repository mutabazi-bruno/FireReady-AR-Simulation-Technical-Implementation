using FireReady.Config;
using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // FR-02: goal, the four extinguishers and PASS, with Skip.
    public class BriefingScreen : UIScreen
    {
        [SerializeField] TMP_Text pageTitle;
        [SerializeField] TMP_Text pageBody;
        [SerializeField] RowList items;
        [SerializeField] TMP_Text pageCounter;
        [SerializeField] Button backButton;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text nextLabel;
        [SerializeField] Button skipButton;
        [SerializeField] GameObject tipCard;
        [SerializeField] TMP_Text tipText;
        [SerializeField] TMP_Text watermark;

        BriefingContent content;
        int page;
        readonly System.Collections.Generic.List<RowView> shownRows = new System.Collections.Generic.List<RowView>();
        bool stepThrough;
        float stepTimer;
        int activeStep;

        int PageCount => content != null ? content.pages.Count : 0;

        protected override void BindButtons()
        {
            Bind(backButton, Back);
            Bind(nextButton, Next);
            Bind(skipButton, () => Sim.ContinueFromBriefing());
        }

        void OnEnable()
        {
            EventBus.Subscribe<CoreContentLoaded>(OnCoreLoaded);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<CoreContentLoaded>(OnCoreLoaded);
        }

        protected override void OnShow()
        {
            content = Sim != null ? Sim.Assets.Briefing : null;
            page = 0;
            ShowPage();
        }

        void OnCoreLoaded(CoreContentLoaded evt)
        {
            content = evt.Briefing;
            ShowPage();
        }

        void Update()
        {
            if (!stepThrough) return;
            stepTimer -= Time.unscaledDeltaTime;
            if (stepTimer > 0f) return;
            stepTimer = 1.3f;
            activeStep = (activeStep + 1) % shownRows.Count;
            for (int i = 0; i < shownRows.Count; i++) shownRows[i].SetEmphasis(i == activeStep);
        }

        void Back()
        {
            if (page == 0)
            {
                Sim.ReturnToMenu();
                return;
            }
            page--;
            ShowPage();
        }

        void Next()
        {
            if (page >= PageCount - 1)
            {
                Sim.ContinueFromBriefing();
                return;
            }
            page++;
            ShowPage();
        }

        void ShowPage()
        {
            items.Clear();

            if (PageCount == 0)
            {
                pageTitle.text = "Briefing";
                pageBody.text = "Loading...";
                pageCounter.text = "";
                return;
            }

            var current = content.pages[page];
            pageTitle.text = current.title;
            pageBody.text = current.body;
            pageBody.gameObject.SetActive(!string.IsNullOrEmpty(current.body));

            shownRows.Clear();
            foreach (var item in current.items)
            {
                var row = items.Add();
                if (item.sign != null) row.SetSign(item.sign, item.heading, item.text);
                else row.Set(item.icon, item.color, item.heading, item.text);
                row.SetEmphasis(true);
                shownRows.Add(row);
            }
            stepThrough = current.stepThrough && shownRows.Count > 1;
            activeStep = -1;
            stepTimer = 0f;

            pageCounter.text = $"BRIEFING {page + 1:00} / {PageCount:00}";
            if (watermark != null) watermark.text = $"{page + 1:00}";
            SetActive(tipCard, !string.IsNullOrEmpty(current.tip));
            if (tipText != null) tipText.text = current.tip;
            nextLabel.text = page >= PageCount - 1 ? "Set up AR" : "Next";
            SetActive(skipButton, page < PageCount - 1);
        }
    }
}
