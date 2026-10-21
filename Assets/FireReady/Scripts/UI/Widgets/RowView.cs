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
        [Tooltip("Optional block behind the icon. When set, the colour goes on the block and the icon turns dark, like a safety sign.")]
        [SerializeField] Image iconBlock;
        [SerializeField] Color iconOnBlock = new Color(0.06f, 0.06f, 0.07f);
        [SerializeField] UITheme theme;
        [SerializeField] CanvasGroup group;

        public Button Button => button;

        public void Set(Sprite iconSprite, Color iconColor, string titleText, string detailText = null, string valueText = null)
        {
            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.color = iconBlock != null ? iconOnBlock : iconColor;
                icon.enabled = iconSprite != null;
            }
            if (iconBlock != null)
            {
                iconBlock.color = iconColor;
                if (theme != null && icon != null)
                {
                    iconBlock.sprite = theme.SignShape(iconColor, out bool triangle);
                    // The pictogram sits lower and smaller inside a warning triangle.
                    var rect = icon.rectTransform;
                    rect.anchorMin = triangle ? new Vector2(0.3f, 0.17f) : new Vector2(0.22f, 0.22f);
                    rect.anchorMax = triangle ? new Vector2(0.7f, 0.6f) : new Vector2(0.78f, 0.78f);
                }
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

        // Shows a real full-colour safety sign (ISO 7010) in the icon slot.
        public void SetSign(Sprite sign, string titleText, string detailText = null)
        {
            Set(null, Color.white, titleText, detailText);
            if (iconBlock != null)
            {
                iconBlock.sprite = sign;
                iconBlock.color = Color.white;
                iconBlock.preserveAspect = true;
            }
            else if (icon != null)
            {
                icon.enabled = true;
                icon.sprite = sign;
                icon.color = Color.white;
            }
        }

        public void SetEmphasis(bool on)
        {
            if (group != null) group.alpha = on ? 1f : 0.35f;
            transform.localScale = Vector3.one * (on ? 1.03f : 1f);
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
