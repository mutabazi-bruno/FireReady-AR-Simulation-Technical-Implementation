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

        BriefingContent content;
        int page;

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

            foreach (var item in current.items)
                items.Add().Set(item.icon, item.color, item.heading, item.text);

            pageCounter.text = $"{page + 1} / {PageCount}";
            nextLabel.text = page >= PageCount - 1 ? "Set up AR" : "Next";
            SetActive(skipButton, page < PageCount - 1);
        }
    }
}
