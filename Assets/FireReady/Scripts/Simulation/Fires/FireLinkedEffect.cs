using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Base for visuals that show the damage one fire has done, such as scorch marks and charred props.
    /// It finds its fire by id inside the scenario layout and reads how much it has burned.
    /// </summary>
    public abstract class FireLinkedEffect : MonoBehaviour
    {
        [SerializeField] string fireId;
        [Tooltip("Burn time at which the effect reaches full strength.")]
        [SerializeField] float fullBurnTime = 30f;

        FireSource fire;
        float searchTimer;
        float shown = -1f;

        protected FireSource Fire => fire;

        void Update()
        {
            if (fire == null)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer > 0f) return;
                searchTimer = 0.5f;
                fire = FindFire();
                if (fire == null) return;
            }

            float amount = Mathf.Clamp01(fire.BurnTime / fullBurnTime);
            if (Mathf.Abs(amount - shown) < 0.005f) return;
            shown = amount;
            Apply(amount);
        }

        FireSource FindFire()
        {
            var layout = transform.root;
            foreach (var candidate in layout.GetComponentsInChildren<FireSource>())
                if (candidate.EntityId == fireId) return candidate;
            return null;
        }

        // amount is 0 before the fire has burned and 1 once it has burned for fullBurnTime.
        protected abstract void Apply(float amount);
    }
}
