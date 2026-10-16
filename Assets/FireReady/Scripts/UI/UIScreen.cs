using System;
using System.Collections;
using FireReady.Core;
using FireReady.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>
    /// Base for every screen and overlay: show/hide with a short fade, and button binding that
    /// also plays the click sound through the event bus.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] protected UITheme theme;
        [SerializeField] float fadeDuration = 0.15f;

        CanvasGroup group;
        bool bound;
        Coroutine fade;

        protected UIManager UI { get; private set; }
        protected static SimulationManager Sim => SimulationManager.Instance;
        public bool IsVisible { get; private set; }

        public void Init(UIManager manager)
        {
            UI = manager;
            group = GetComponent<CanvasGroup>();
            if (!bound)
            {
                bound = true;
                BindButtons();
            }
        }

        public void Show()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            IsVisible = true;
            gameObject.SetActive(true);
            group.interactable = true;
            group.blocksRaycasts = true;

            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeIn());
            OnShow();
        }

        public void Hide()
        {
            if (!IsVisible && !gameObject.activeSelf) return;
            IsVisible = false;
            OnHide();
            gameObject.SetActive(false);
        }

        IEnumerator FadeIn()
        {
            float t = 0f;
            group.alpha = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            group.alpha = 1f;
            fade = null;
        }

        protected virtual void BindButtons() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected static void Bind(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                EventBus.Publish(new UiClicked());
                action();
            });
        }

        protected static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        protected static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }

        protected static string FormatTime(float seconds)
        {
            if (seconds < 0f) return "-";
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
