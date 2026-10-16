using UnityEngine;

namespace FireReady.Config
{
    // Score deductions from Table 2.2 of the SRS. Every session starts at 100.
    [CreateAssetMenu(fileName = "ScoringRules", menuName = "FireReady/Scoring Rules")]
    public class ScoringRules : ScriptableObject
    {
        public int startScore = 100;
        public int weakAgentPerFire = 5;
        public int dangerousAgentPerUse = 20;
        public int alarmNotRaised = 10;
        public int electricalBeforePowerIsolated = 10;
        public int lowAimAccuracy = 10;
        [Range(0f, 1f)] public float aimAccuracyThreshold = 0.6f;
        public int tooClosePerEvent = 5;
        public int emptiedWithFireBurning = 5;
        public int failedScoreCap = 40;
    }
}
