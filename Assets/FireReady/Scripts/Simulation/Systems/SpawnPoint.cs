using UnityEngine;

namespace FireReady.Simulation
{
    // Marks where a fire from the scenario schedule appears inside the layout prefab.
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] string id;

        public string Id => id;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.08f);
        }
    }
}
