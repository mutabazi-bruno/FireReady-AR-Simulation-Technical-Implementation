using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // Shown when AR tracking is lost during a session (FR-24). The session resumes by itself.
    public class TrackingOverlay : UIScreen
    {
        [SerializeField] TMP_Text message;
        [SerializeField] Button menuButton;

        protected override void BindButtons()
        {
            Bind(menuButton, () => UI.ShowPauseMenu());
        }

        public void SetMessage(string text)
        {
            if (message != null) message.text = text;
        }
    }
}
