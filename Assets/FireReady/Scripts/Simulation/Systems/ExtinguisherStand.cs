using UnityEngine;

namespace FireReady.Simulation
{
    // Holds the slots extinguishers return to when they are put down.
    public class ExtinguisherStand : MonoBehaviour
    {
        [SerializeField] Transform[] slots;

        public int SlotCount => slots != null ? slots.Length : 0;

        public Transform GetSlot(int index)
        {
            if (slots == null || slots.Length == 0) return transform;
            return slots[Mathf.Clamp(index, 0, slots.Length - 1)];
        }
    }
}
