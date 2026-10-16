using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Class B fire (fuel). Water hitting burning fuel splashes it and starts a second fire.
    /// </summary>
    public class FlammableLiquidFire : FireSource
    {
        [SerializeField] float spreadRadius = 0.7f;

        FireFactory factory;
        bool hasSpread;

        public override FireClass FireClass => FireClass.ClassB;

        public void SetFactory(FireFactory fireFactory)
        {
            factory = fireFactory;
        }

        public override void ResetEntity()
        {
            hasSpread = false;
            base.ResetEntity();
        }

        protected override void OnDangerousHit(AgentType agent)
        {
            if (hasSpread || IsSecondary || factory == null || agent != AgentType.Water) return;
            hasSpread = true;

            // Push the spill away from the user so the new fire is visible.
            var viewer = Camera.main;
            var away = viewer != null ? transform.position - viewer.transform.position : transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = transform.forward;
            var sideways = Vector3.Cross(Vector3.up, away.normalized) * Random.Range(-0.5f, 0.5f);
            var direction = (away.normalized + sideways).normalized;

            factory.CreateSpread(this, transform.position + direction * spreadRadius);
        }
    }
}
