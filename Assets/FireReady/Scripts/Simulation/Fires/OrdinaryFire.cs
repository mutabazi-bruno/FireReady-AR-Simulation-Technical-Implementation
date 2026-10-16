using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Class A fire (paper, wood). If it is knocked down by a weak agent the embers keep
    /// smouldering and the fire comes back after the reignite delay.
    /// </summary>
    public class OrdinaryFire : FireSource
    {
        [SerializeField] ParticleSystem smoulderSmoke;

        bool knockedDownByWeakAgent;
        float smoulderTimer = -1f;

        public override FireClass FireClass => FireClass.ClassA;
        public override bool HasPendingReignite => smoulderTimer > 0f;
        public float SmoulderTimeLeft => Mathf.Max(0f, smoulderTimer);

        public override void ResetEntity()
        {
            knockedDownByWeakAgent = false;
            SetSmoulder(-1f);
            base.ResetEntity();
        }

        protected override void OnWeakHit()
        {
            knockedDownByWeakAgent = true;
        }

        protected override void OnEffectiveHit()
        {
            knockedDownByWeakAgent = false;
        }

        protected override void OnExtinguished()
        {
            if (knockedDownByWeakAgent) SetSmoulder(profile.reigniteDelay);
        }

        protected override void CoolEmbers()
        {
            knockedDownByWeakAgent = false;
            SetSmoulder(-1f);
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (State != FireState.Extinguished || smoulderTimer <= 0f) return;

            smoulderTimer -= dt;
            if (smoulderTimer <= 0f)
            {
                knockedDownByWeakAgent = false;
                SetSmoulder(-1f);
                Reignite("The embers were still hot");
            }
        }

        void SetSmoulder(float seconds)
        {
            smoulderTimer = seconds;
            if (smoulderSmoke == null) return;
            if (seconds > 0f) smoulderSmoke.Play();
            else smoulderSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
