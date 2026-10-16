using UnityEngine;

namespace FireReady.Simulation
{
    // Collider on a fire. The base zone gets the full suppression rate, the flame zone a reduced one (FR-15).
    [RequireComponent(typeof(Collider))]
    public class FireZone : MonoBehaviour
    {
        [SerializeField] bool isBase;

        FireSource owner;

        public bool IsBase => isBase;
        public FireSource Owner => owner != null ? owner : owner = GetComponentInParent<FireSource>();
    }
}
