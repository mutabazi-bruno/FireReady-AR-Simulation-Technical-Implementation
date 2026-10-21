using FireReady.Core;
using UnityEngine;

namespace FireReady.UI
{
    // Colours and icons shared by every screen. Warnings always use icon + text + colour (NFR-06).
    [CreateAssetMenu(fileName = "UITheme", menuName = "FireReady/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Colours")]
        public Color accent = new Color(0.93f, 0.33f, 0.18f);
        public Color success = new Color(0.22f, 0.74f, 0.42f);
        public Color warning = new Color(1f, 0.71f, 0.16f);
        public Color danger = new Color(0.9f, 0.22f, 0.2f);
        public Color info = new Color(0.33f, 0.62f, 0.95f);
        public Color textPrimary = Color.white;
        public Color textMuted = new Color(1f, 1f, 1f, 0.65f);

        [Header("Icons")]
        public Sprite check;
        public Sprite circle;
        public Sprite locked;
        public Sprite warningIcon;
        public Sprite cross;
        public Sprite fire;
        public Sprite alarm;
        public Sprite power;
        public Sprite exit;
        public Sprite timer;
        public Sprite target;
        public Sprite infoIcon;
        public Sprite extinguisher;

        [Header("Safety sign shapes (ISO 7010)")]
        public Sprite signSquare;
        public Sprite signCircle;
        public Sprite signTriangle;

        // Warning colours sit on a triangle, instructions on a circle, everything else on a square.
        public Sprite SignShape(Color color, out bool triangle)
        {
            triangle = Close(color, warning) || Close(color, accent);
            if (triangle && signTriangle != null) return signTriangle;
            if (Close(color, info) && signCircle != null) return signCircle;
            return signSquare;
        }

        static bool Close(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.12f;
        }

        public Color OutcomeColor(SessionOutcome outcome)
        {
            switch (outcome)
            {
                case SessionOutcome.Completed: return success;
                case SessionOutcome.Evacuated: return info;
                case SessionOutcome.Failed: return danger;
                default: return warning;
            }
        }

        public Sprite OutcomeIcon(SessionOutcome outcome)
        {
            switch (outcome)
            {
                case SessionOutcome.Completed: return check;
                case SessionOutcome.Evacuated: return exit;
                case SessionOutcome.Failed: return cross;
                default: return warningIcon;
            }
        }

        public static string OutcomeTitle(SessionOutcome outcome)
        {
            switch (outcome)
            {
                case SessionOutcome.Completed: return "All fires out";
                case SessionOutcome.Evacuated: return "Evacuated";
                case SessionOutcome.Failed: return "Session failed";
                default: return "Abandoned";
            }
        }
    }
}
