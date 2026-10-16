using TMPro;
using UnityEngine;

namespace FireReady.UI
{
    public class LoadingOverlay : UIScreen
    {
        [SerializeField] RectTransform spinner;
        [SerializeField] TMP_Text label;

        public void ShowMessage(string text)
        {
            if (label != null) label.text = text;
            Show();
        }

        void Update()
        {
            if (spinner != null) spinner.Rotate(0f, 0f, -240f * Time.unscaledDeltaTime);
        }
    }
}
