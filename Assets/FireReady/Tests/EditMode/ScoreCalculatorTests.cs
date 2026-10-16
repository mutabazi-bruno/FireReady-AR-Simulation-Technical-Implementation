using FireReady.Config;
using FireReady.Core;
using FireReady.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace FireReady.Tests
{
    // SRS table 2.2.
    public class ScoreCalculatorTests
    {
        ScoringRules rules;

        [SetUp]
        public void SetUp()
        {
            rules = ScriptableObject.CreateInstance<ScoringRules>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rules);
        }

        static ScoreInputs Clean() => new ScoreInputs { AlarmRaised = true, HasSprayed = true, AimAccuracy = 0.9f };

        [Test]
        public void Perfect_session_scores_100()
        {
            Assert.AreEqual(100, ScoreCalculator.Calculate(rules, Clean(), SessionOutcome.Completed, true));
        }

        [Test]
        public void Each_mistake_applies_its_deduction()
        {
            var inputs = Clean();
            inputs.WeakAgentFires = 1;
            inputs.DangerousUses = 1;
            inputs.TooCloseEvents = 2;
            inputs.EmptiedWithFire = 1;
            inputs.LiveElectricalMistake = true;

            // 100 - 5 - 20 - 10 - 5 - 10
            Assert.AreEqual(50, ScoreCalculator.Calculate(rules, inputs, SessionOutcome.Completed, true));
        }

        [Test]
        public void Alarm_never_raised_is_only_judged_at_the_end()
        {
            var inputs = Clean();
            inputs.AlarmRaised = false;
            Assert.AreEqual(100, ScoreCalculator.Calculate(rules, inputs, null, false));
            Assert.AreEqual(90, ScoreCalculator.Calculate(rules, inputs, SessionOutcome.Completed, true));
        }

        [Test]
        public void Low_aim_accuracy_costs_10_but_only_if_the_user_sprayed()
        {
            var inputs = Clean();
            inputs.AimAccuracy = 0.4f;
            Assert.AreEqual(90, ScoreCalculator.Calculate(rules, inputs, SessionOutcome.Completed, true));

            inputs.HasSprayed = false;
            Assert.AreEqual(100, ScoreCalculator.Calculate(rules, inputs, SessionOutcome.Evacuated, true));
        }

        [Test]
        public void Failed_session_is_capped_at_40()
        {
            Assert.AreEqual(40, ScoreCalculator.Calculate(rules, Clean(), SessionOutcome.Failed, true));
        }

        [Test]
        public void Score_never_goes_below_zero()
        {
            var inputs = Clean();
            inputs.DangerousUses = 10;
            Assert.AreEqual(0, ScoreCalculator.Calculate(rules, inputs, SessionOutcome.Completed, true));
        }
    }
}
