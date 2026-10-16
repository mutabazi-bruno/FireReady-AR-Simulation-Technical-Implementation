using System;
using UnityEngine;

namespace FireReady.Config
{
    // Scenario-wide rule values. Defaults match the SRS.
    [Serializable]
    public class SimulationRules
    {
        [Tooltip("Safe working distance band in metres (FR-16).")]
        public Vector2 safeDistance = new Vector2(1.0f, 2.5f);

        [Tooltip("Seconds closer than the near limit before an error is recorded (FR-16).")]
        public float tooCloseSeconds = 3f;

        [Tooltip("Share of the suppression rate applied when spray hits the upper flames (FR-15).")]
        [Range(0f, 1f)] public float offBaseFactor = 0.25f;

        [Tooltip("Share of the suppression rate applied by a weak agent (FR-14).")]
        [Range(0f, 1f)] public float weakFactor = 0.6f;

        [Tooltip("Seconds a fire can stay out of control before the session fails (FR-19).")]
        public float outOfControlFailSeconds = 10f;

        [Tooltip("How far away the trainee can tap an object to use it, in metres.")]
        public float interactionReach = 2.2f;
    }
}
