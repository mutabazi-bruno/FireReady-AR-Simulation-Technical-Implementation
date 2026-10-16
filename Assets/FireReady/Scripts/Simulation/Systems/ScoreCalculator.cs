using FireReady.Config;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    // What the score depends on. Filled in by the PerformanceTracker.
    public struct ScoreInputs
    {
        public int WeakAgentFires;
        public int DangerousUses;
        public int TooCloseEvents;
        public int EmptiedWithFire;
        public bool LiveElectricalMistake;
        public bool AlarmMistake;
        public bool AlarmRaised;
        public bool HasSprayed;
        public float AimAccuracy;
    }

    /// <summary>
    /// Applies the deductions from SRS table 2.2. Kept separate from the tracker so the rules
    /// can be tested without a scene.
    /// </summary>
    public static class ScoreCalculator
    {
        public static int Calculate(ScoringRules rules, ScoreInputs inputs, SessionOutcome? outcome, bool sessionOver)
        {
            if (rules == null) return 100;

            int score = rules.startScore;
            score -= inputs.WeakAgentFires * rules.weakAgentPerFire;
            score -= inputs.DangerousUses * rules.dangerousAgentPerUse;
            score -= inputs.TooCloseEvents * rules.tooClosePerEvent;
            score -= inputs.EmptiedWithFire * rules.emptiedWithFireBurning;
            if (inputs.LiveElectricalMistake) score -= rules.electricalBeforePowerIsolated;

            // Some mistakes can only be judged once the session is over.
            bool alarmPenalty = inputs.AlarmMistake || (sessionOver && !inputs.AlarmRaised);
            if (alarmPenalty) score -= rules.alarmNotRaised;

            if (sessionOver && inputs.HasSprayed && inputs.AimAccuracy < rules.aimAccuracyThreshold)
                score -= rules.lowAimAccuracy;

            score = Mathf.Max(0, score);
            if (outcome == SessionOutcome.Failed) score = Mathf.Min(score, rules.failedScoreCap);
            return score;
        }
    }
}
