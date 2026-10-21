using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.Simulation
{
    /// <summary>
    /// Small world-space bar above a fire showing its intensity and state in text and colour.
    /// </summary>
    public class FireStatusView : MonoBehaviour
    {
        [SerializeField] FireSource fire;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image fill;
        [SerializeField] TMP_Text label;
        [SerializeField] Color smallColor = new Color(1f, 0.78f, 0.2f);
        [SerializeField] Color growingColor = new Color(1f, 0.5f, 0.1f);
        [SerializeField] Color outOfControlColor = new Color(0.9f, 0.15f, 0.12f);
        [SerializeField] Color smoulderColor = new Color(0.6f, 0.6f, 0.6f);
        [Tooltip("The label fades out when the camera is closer than this, so it never covers the view.")]
        [SerializeField] float fadeStart = 1.3f;
        [SerializeField] float fadeEnd = 0.8f;

        Camera viewer;
        FireState shownState = (FireState)(-1);
        bool shownSmoulder;

        void Awake()
        {
            if (fire == null) fire = GetComponentInParent<FireSource>();
        }

        void LateUpdate()
        {
            if (fire == null) return;

            bool smouldering = fire.State == FireState.Extinguished && fire.HasPendingReignite;
            bool visible = fire.IsBurning || smouldering;

            if (viewer == null) viewer = Camera.main;
            float nearFade = 1f;
            if (viewer != null)
            {
                float distance = Vector3.Distance(viewer.transform.position, transform.position);
                nearFade = Mathf.InverseLerp(fadeEnd, fadeStart, distance);
            }
            if (group != null) group.alpha = visible ? nearFade : 0f;
            if (!visible || nearFade <= 0f) return;

            if (fill != null) fill.fillAmount = smouldering ? 0.05f : fire.Intensity / 100f;

            if (fire.State != shownState || smouldering != shownSmoulder)
            {
                shownState = fire.State;
                shownSmoulder = smouldering;
                RefreshLabel(smouldering);
            }

            if (viewer != null)
            {
                var toViewer = transform.position - viewer.transform.position;
                toViewer.y = 0f;
                if (toViewer.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(toViewer);
            }
        }

        void RefreshLabel(bool smouldering)
        {
            string text;
            Color color;
            if (smouldering)
            {
                text = "Smouldering";
                color = smoulderColor;
            }
            else
            {
                text = $"{EnumText.FireClassName(fire.FireClass)}  ·  {EnumText.FireStateName(fire.State)}";
                color = fire.State == FireState.OutOfControl ? outOfControlColor
                    : fire.State == FireState.Growing ? growingColor
                    : smallColor;
            }

            if (label != null) label.text = text;
            if (fill != null) fill.color = color;
        }
    }
}
