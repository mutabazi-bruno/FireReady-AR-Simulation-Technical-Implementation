using UnityEngine;

namespace FireReady.Simulation
{
    public readonly struct InteractionContext
    {
        public readonly InteractionManager Interaction;
        public readonly Vector3 HitPoint;
        public readonly float Distance;

        public InteractionContext(InteractionManager interaction, Vector3 hitPoint, float distance)
        {
            Interaction = interaction;
            HitPoint = hitPoint;
            Distance = distance;
        }
    }
}
