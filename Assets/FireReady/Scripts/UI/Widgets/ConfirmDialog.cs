using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    public class ConfirmDialog : UIScreen
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text message;
        [SerializeField] Button confirmButton;
        [SerializeField] TMP_Text confirmLabel;
        [SerializeField] Button cancelButton;

        Action onConfirm;

        protected override void BindButtons()
        {
            Bind(confirmButton, () =>
            {
                var action = onConfirm;
                Hide();
                action?.Invoke();
            });
            Bind(cancelButton, Hide);
        }

        public void Ask(string titleText, string messageText, string confirmText, Action confirmed)
        {
            if (title != null) title.text = titleText;
            if (message != null) message.text = messageText;
            if (confirmLabel != null) confirmLabel.text = confirmText;
            onConfirm = confirmed;
            Show();
            transform.SetAsLastSibling();
        }

        protected override void OnHide()
        {
            onConfirm = null;
        }
    }
}
