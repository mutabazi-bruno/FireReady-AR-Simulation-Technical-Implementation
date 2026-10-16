using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FireReady.Core
{
    /// <summary>
    /// Reads taps on the world (not on UI). Works with touch on device and the mouse in the editor.
    /// </summary>
    public static class TouchInput
    {
        static readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        static PointerEventData pointerData;
        static EventSystem pointerDataOwner;

        public static bool TryGetWorldTap(out Vector2 screenPosition)
        {
            screenPosition = default;
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return false;

            screenPosition = pointer.position.ReadValue();
            return !IsOverUI(screenPosition);
        }

        public static bool IsOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (pointerData == null || pointerDataOwner != eventSystem)
            {
                pointerData = new PointerEventData(eventSystem);
                pointerDataOwner = eventSystem;
            }

            pointerData.position = screenPosition;
            uiHits.Clear();
            eventSystem.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }
    }
}
