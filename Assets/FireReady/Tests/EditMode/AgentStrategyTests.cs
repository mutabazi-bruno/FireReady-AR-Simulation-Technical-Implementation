using FireReady.Core;
using FireReady.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace FireReady.Tests
{
    // Checks every pair in the extinguisher and fire class table (SRS 1.5, FR-14).
    public class AgentStrategyTests
    {
        static ExtinguishingAgent Create(AgentType type)
        {
            switch (type)
            {
                case AgentType.Water: return ScriptableObject.CreateInstance<WaterAgent>();
                case AgentType.Foam: return ScriptableObject.CreateInstance<FoamAgent>();
                case AgentType.CO2: return ScriptableObject.CreateInstance<CO2Agent>();
                default: return ScriptableObject.CreateInstance<DryPowderAgent>();
            }
        }

        [TestCase(AgentType.Water, FireClass.ClassA, AgentEffect.Effective)]
        [TestCase(AgentType.Water, FireClass.ClassB, AgentEffect.Dangerous)]
        [TestCase(AgentType.Water, FireClass.Electrical, AgentEffect.Fatal)]
        [TestCase(AgentType.Foam, FireClass.ClassA, AgentEffect.Effective)]
        [TestCase(AgentType.Foam, FireClass.ClassB, AgentEffect.Effective)]
        [TestCase(AgentType.Foam, FireClass.Electrical, AgentEffect.Dangerous)]
        [TestCase(AgentType.CO2, FireClass.ClassA, AgentEffect.Weak)]
        [TestCase(AgentType.CO2, FireClass.ClassB, AgentEffect.Effective)]
        [TestCase(AgentType.CO2, FireClass.Electrical, AgentEffect.Effective)]
        [TestCase(AgentType.DryPowder, FireClass.ClassA, AgentEffect.Effective)]
        [TestCase(AgentType.DryPowder, FireClass.ClassB, AgentEffect.Effective)]
        [TestCase(AgentType.DryPowder, FireClass.Electrical, AgentEffect.Effective)]
        public void Agent_on_live_fire_matches_rule_table(AgentType agent, FireClass fireClass, AgentEffect expected)
        {
            var strategy = Create(agent);
            Assert.AreEqual(expected, strategy.Evaluate(fireClass, powerLive: true));
            Assert.AreEqual(agent, strategy.Type);
            Object.DestroyImmediate(strategy);
        }

        [TestCase(AgentType.Water)]
        [TestCase(AgentType.Foam)]
        public void Water_based_agents_are_safe_once_power_is_isolated(AgentType agent)
        {
            var strategy = Create(agent);
            Assert.AreEqual(AgentEffect.Effective, strategy.Evaluate(FireClass.Electrical, powerLive: false));
            Object.DestroyImmediate(strategy);
        }
    }
}
