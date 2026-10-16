using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // One line in a list: icon, title, optional detail and value. Used for tasks, metrics, mistakes and history.
    public class RowView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text detail;
        [SerializeField] TMP_Text value;
        [SerializeField] Button button;

        public Button Button => button;

        public void Set(Sprite iconSprite, Color iconColor, string titleText, string detailText = null, string valueText = null)
        {
            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.color = iconColor;
                icon.enabled = iconSprite != null;
            }

            if (title != null) title.text = titleText;

            if (detail != null)
            {
                detail.text = detailText;
                detail.gameObject.SetActive(!string.IsNullOrEmpty(detailText));
            }

            if (value != null)
            {
                value.text = valueText;
                value.gameObject.SetActive(!string.IsNullOrEmpty(valueText));
            }
        }

        public void SetTitleColor(Color color)
        {
            if (title != null) title.color = color;
        }

        public void SetValueColor(Color color)
        {
            if (value != null) value.color = color;
        }
    }
}
