using FireReady.Core;
using UnityEngine;

namespace FireReady.Config
{
    // Tuning values for one class of fire. Intensity runs from 0 to 100.
    [CreateAssetMenu(fileName = "FireProfile", menuName = "FireReady/Fire Profile")]
    public class FireProfile : ScriptableObject
    {
        public FireClass fireClass;

        [Header("Growth")]
        [Tooltip("Intensity gained per second while not being suppressed (FR-09).")]
        public float growthRate = 1.2f;
        public float startIntensity = 15f;

        [Header("State thresholds (FR-10)")]
        public float growingThreshold = 35f;
        public float outOfControlThreshold = 75f;

        [Header("Reignition")]
        public float reigniteDelay = 8f;
        public float reigniteIntensity = 25f;

        [Header("Wrong agent")]
        [Tooltip("Intensity gained per second while a dangerous agent hits the fire.")]
        public float dangerousRate = 14f;

        public FireState StateFor(float intensity)
        {
            if (intensity <= 0f) return FireState.Extinguished;
            if (intensity >= outOfControlThreshold) return FireState.OutOfControl;
            if (intensity >= growingThreshold) return FireState.Growing;
            return FireState.Small;
        }
    }
}
