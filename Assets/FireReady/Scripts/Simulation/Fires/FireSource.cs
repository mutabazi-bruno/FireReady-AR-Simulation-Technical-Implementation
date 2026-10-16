using FireReady.Config;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// One fire in the scenario. Intensity is private and only changes through Tick (growth)
    /// and ApplyAgent (suppression), so nothing can skip the rules.
    /// Subclasses override only the behaviour that differs per fire class.
    /// </summary>
    public abstract class FireSource : SimulationEntity
    {
        protected FireProfile profile;
        protected SimulationRules rules;

        float intensity;
        FireState state = FireState.Dormant;
        bool suppressedThisFrame;
        float outOfControlTime;
        bool beyondControlRaised;

        public abstract FireClass FireClass { get; }
        public float Intensity => intensity;
        public FireState State => state;
        public FireProfile Profile => profile;
        public FireSpawnData Spawn { get; private set; }
        public bool IsSecondary { get; private set; }
        public float OutOfControlTime => outOfControlTime;

        public bool IsBurning => state == FireState.Small || state == FireState.Growing || state == FireState.OutOfControl;
        public virtual bool IsPowerLive => false;
        public virtual bool HasPendingReignite => false;

        public override bool CanInteract => false;
        public override string ActionHint => "Aim at the base and spray";

        public void Configure(FireProfile fireProfile, SimulationRules simulationRules, FireSpawnData spawn, bool secondary)
        {
            profile = fireProfile;
            rules = simulationRules;
            Spawn = spawn;
            IsSecondary = secondary;
            entityId = spawn.id;
            displayName = spawn.displayName;
        }

        public override void ResetEntity()
        {
            intensity = 0f;
            state = FireState.Dormant;
            suppressedThisFrame = false;
            outOfControlTime = 0f;
            beyondControlRaised = false;
            base.ResetEntity();
        }

        public void Ignite()
        {
            if (state != FireState.Dormant) return;
            intensity = profile.startIntensity;
            SetState(FireState.Small);
            EventBus.Publish(new FireIgnited { Fire = this });
        }

        // Called once per simulation frame by the EntityManager, after spray has been applied.
        public virtual void Tick(float dt)
        {
            if (IsBurning)
            {
                if (!suppressedThisFrame)
                    intensity = Mathf.Min(100f, intensity + profile.growthRate * dt);

                UpdateStateFromIntensity();

                if (state == FireState.OutOfControl)
                {
                    outOfControlTime += dt;
                    if (!beyondControlRaised && outOfControlTime >= rules.outOfControlFailSeconds)
                    {
                        beyondControlRaised = true;
                        EventBus.Publish(new FireBeyondControl { Fire = this });
                    }
                }
            }

            suppressedThisFrame = false;
        }

        public void ApplyAgent(AgentEffect effect, AgentType agent, float rate, bool onBase, float distance, float dt, int burstId)
        {
            if (state == FireState.Dormant) return;

            if (state == FireState.Extinguished)
            {
                // Spraying smouldering embers with a proper agent cools them for good.
                if (effect == AgentEffect.Effective && HasPendingReignite) CoolEmbers();
                return;
            }

            float aimFactor = onBase ? 1f : rules.offBaseFactor;
            switch (effect)
            {
                case AgentEffect.Effective:
                    intensity -= rate * aimFactor * dt;
                    suppressedThisFrame = true;
                    OnEffectiveHit();
                    break;
                case AgentEffect.Weak:
                    intensity -= rate * rules.weakFactor * aimFactor * dt;
                    suppressedThisFrame = true;
                    OnWeakHit();
                    break;
                case AgentEffect.Dangerous:
                case AgentEffect.Fatal:
                    intensity += profile.dangerousRate * dt;
                    OnDangerousHit(agent);
                    break;
            }

            intensity = Mathf.Clamp(intensity, 0f, 100f);

            EventBus.Publish(new AgentApplied
            {
                Fire = this,
                Agent = agent,
                Effect = effect,
                OnBase = onBase,
                Distance = distance,
                DeltaTime = dt,
                BurstId = burstId
            });

            if (intensity <= 0f) Extinguish();
            else UpdateStateFromIntensity();
        }

        protected void Extinguish()
        {
            intensity = 0f;
            SetState(FireState.Extinguished);
            EventBus.Publish(new FireExtinguished { Fire = this });
            OnExtinguished();
        }

        protected void Reignite(string cause)
        {
            if (state != FireState.Extinguished) return;
            intensity = profile.reigniteIntensity;
            SetState(FireState.Small);
            EventBus.Publish(new FireReignited { Fire = this, Cause = cause });
        }

        void UpdateStateFromIntensity()
        {
            var next = profile.StateFor(intensity);
            if (next != state) SetState(next);
        }

        void SetState(FireState next)
        {
            var previous = state;
            state = next;

            if (previous == FireState.OutOfControl && next != FireState.OutOfControl)
            {
                // Brought back under the threshold, so the fail timer is cancelled.
                outOfControlTime = 0f;
                beyondControlRaised = false;
            }

            EventBus.Publish(new FireStateChanged { Fire = this, Previous = previous, Current = next });
        }

        public override void OnTap(InteractionContext context) { }

        protected virtual void OnEffectiveHit() { }
        protected virtual void OnWeakHit() { }
        protected virtual void OnDangerousHit(AgentType agent) { }
        protected virtual void OnExtinguished() { }
        protected virtual void CoolEmbers() { }
    }
}
