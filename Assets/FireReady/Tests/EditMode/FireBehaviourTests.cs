using System.Collections.Generic;
using FireReady.Config;
using FireReady.Core;
using FireReady.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace FireReady.Tests
{
    public class FireBehaviourTests
    {
        readonly List<Object> created = new List<Object>();
        SimulationRules rules;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            rules = new SimulationRules { offBaseFactor = 0.25f, weakFactor = 0.5f, outOfControlFailSeconds = 10f };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
            EventBus.Clear();
        }

        T CreateFire<T>(FireClass fireClass, float growth = 1f) where T : FireSource
        {
            var profile = ScriptableObject.CreateInstance<FireProfile>();
            profile.fireClass = fireClass;
            profile.growthRate = growth;
            profile.startIntensity = 20f;
            profile.growingThreshold = 35f;
            profile.outOfControlThreshold = 75f;
            profile.reigniteDelay = 5f;
            profile.reigniteIntensity = 25f;
            profile.dangerousRate = 10f;
            created.Add(profile);

            var go = new GameObject("TestFire");
            created.Add(go);
            var fire = go.AddComponent<T>();
            fire.Configure(profile, rules, new FireSpawnData { id = "test", displayName = "test", fireClass = fireClass }, false);
            fire.ResetEntity();
            return fire;
        }

        [Test]
        public void Ignited_fire_grows_when_left_alone()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA, growth: 2f);
            fire.Ignite();
            Assert.AreEqual(FireState.Small, fire.State);

            fire.Tick(5f);
            Assert.AreEqual(30f, fire.Intensity, 0.001f);
        }

        [Test]
        public void Fire_moves_through_states_by_intensity()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA, growth: 10f);
            fire.Ignite();
            fire.Tick(2f);
            Assert.AreEqual(FireState.Growing, fire.State);
            fire.Tick(4f);
            Assert.AreEqual(FireState.OutOfControl, fire.State);
        }

        [Test]
        public void Suppressed_fire_does_not_grow_that_frame()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA, growth: 5f);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Effective, AgentType.Water, 10f, true, 2f, 1f, 1);
            fire.Tick(1f);
            Assert.AreEqual(10f, fire.Intensity, 0.001f);
        }

        [Test]
        public void Spray_on_the_flames_only_applies_the_reduced_rate()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Effective, AgentType.Water, 10f, false, 2f, 1f, 1);
            Assert.AreEqual(17.5f, fire.Intensity, 0.001f);
        }

        [Test]
        public void Dangerous_agent_makes_the_fire_bigger()
        {
            var fire = CreateFire<FlammableLiquidFire>(FireClass.ClassB);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Dangerous, AgentType.Foam, 10f, true, 2f, 1f, 1);
            Assert.Greater(fire.Intensity, 20f);
        }

        [Test]
        public void Class_A_knocked_down_by_CO2_reignites()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Weak, AgentType.CO2, 50f, true, 2f, 1f, 1);

            Assert.AreEqual(FireState.Extinguished, fire.State);
            Assert.IsTrue(fire.HasPendingReignite);

            fire.Tick(5.1f);
            Assert.AreEqual(FireState.Small, fire.State);
            Assert.AreEqual(25f, fire.Intensity, 0.001f);
        }

        [Test]
        public void Class_A_put_out_properly_stays_out()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Effective, AgentType.Water, 50f, true, 2f, 1f, 1);
            fire.Tick(30f);
            Assert.AreEqual(FireState.Extinguished, fire.State);
            Assert.IsFalse(fire.HasPendingReignite);
        }

        [Test]
        public void Cooling_smouldering_embers_stops_the_reignite()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Weak, AgentType.CO2, 50f, true, 2f, 1f, 1);
            fire.ApplyAgent(AgentEffect.Effective, AgentType.Water, 10f, true, 2f, 0.1f, 2);

            Assert.IsFalse(fire.HasPendingReignite);
            fire.Tick(10f);
            Assert.AreEqual(FireState.Extinguished, fire.State);
        }

        [Test]
        public void Electrical_fire_reignites_while_power_is_live()
        {
            var fire = CreateFire<ElectricalFire>(FireClass.Electrical);
            fire.Ignite();
            fire.ApplyAgent(AgentEffect.Effective, AgentType.CO2, 50f, true, 2f, 1f, 1);
            Assert.IsTrue(fire.HasPendingReignite);

            fire.Tick(5.1f);
            Assert.AreEqual(FireState.Small, fire.State);
        }

        [Test]
        public void Electrical_fire_stays_out_after_power_is_isolated()
        {
            var fire = CreateFire<ElectricalFire>(FireClass.Electrical);
            fire.Ignite();
            fire.OnPowerIsolated();
            fire.ApplyAgent(AgentEffect.Effective, AgentType.CO2, 50f, true, 2f, 1f, 1);

            Assert.IsFalse(fire.IsPowerLive);
            fire.Tick(20f);
            Assert.AreEqual(FireState.Extinguished, fire.State);
        }

        [Test]
        public void Fire_out_of_control_too_long_raises_beyond_control()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA, growth: 100f);
            int raised = 0;
            System.Action<FireBeyondControl> handler = e => raised++;
            EventBus.Subscribe(handler);

            fire.Ignite();
            fire.Tick(1f);
            Assert.AreEqual(FireState.OutOfControl, fire.State);

            for (int i = 0; i < 11; i++) fire.Tick(1f);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Bringing_a_fire_back_under_control_resets_the_timer()
        {
            var fire = CreateFire<OrdinaryFire>(FireClass.ClassA, growth: 100f);
            fire.Ignite();
            fire.Tick(1f);
            fire.Tick(5f);
            Assert.Greater(fire.OutOfControlTime, 0f);

            fire.ApplyAgent(AgentEffect.Effective, AgentType.Water, 40f, true, 2f, 1f, 1);
            Assert.AreEqual(FireState.Growing, fire.State);
            Assert.AreEqual(0f, fire.OutOfControlTime);
        }
    }
}
