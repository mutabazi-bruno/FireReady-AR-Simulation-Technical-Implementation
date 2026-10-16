using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Electrical fire on an overloaded extension board. While the power is live it reignites
    /// after being put out, and water on it is fatal.
    /// </summary>
    public class ElectricalFire : FireSource
    {
        [SerializeField] ParticleSystem sparks;

        bool powerLive = true;
        float reigniteTimer = -1f;

        public override FireClass FireClass => FireClass.Electrical;
        public override bool IsPowerLive => powerLive;
        public override bool HasPendingReignite => reigniteTimer > 0f;

        public override void ResetEntity()
        {
            powerLive = true;
            reigniteTimer = -1f;
            base.ResetEntity();
            UpdateSparks();
        }

        public void OnPowerIsolated()
        {
            powerLive = false;
            reigniteTimer = -1f;
            UpdateSparks();
        }

        protected override void OnExtinguished()
        {
            if (powerLive) reigniteTimer = profile.reigniteDelay;
            UpdateSparks();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            UpdateSparks();

            if (State != FireState.Extinguished || reigniteTimer <= 0f) return;

            reigniteTimer -= dt;
            if (reigniteTimer <= 0f)
            {
                reigniteTimer = -1f;
                Reignite("The power was still on");
            }
        }

        void UpdateSparks()
        {
            if (sparks == null) return;
            bool show = powerLive && State != FireState.Dormant;
            if (show && !sparks.isPlaying) sparks.Play();
            else if (!show && sparks.isPlaying) sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
