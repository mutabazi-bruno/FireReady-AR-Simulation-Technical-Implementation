using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FireReady.UI
{
    // Button that reports press and release, used for the hold-to-spray control (FR-13).
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Graphic target;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color pressedColor = new Color(0.85f, 0.85f, 0.85f);
        [SerializeField] float pressedScale = 0.92f;

        bool pressed;

        public event Action Pressed;
        public event Action Released;
        public bool IsPressed => pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pressed) return;
            pressed = true;
            Apply();
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ForceRelease();
        }

        public void ForceRelease()
        {
            if (!pressed) return;
            pressed = false;
            Apply();
            Released?.Invoke();
        }

        public void SetColors(Color normal, Color down)
        {
            normalColor = normal;
            pressedColor = down;
            Apply();
        }

        void Apply()
        {
            if (target != null) target.color = pressed ? pressedColor : normalColor;
            transform.localScale = Vector3.one * (pressed ? pressedScale : 1f);
        }

        void OnDisable()
        {
            ForceRelease();
        }
    }
}
