using FireReady.Config;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Base for every object in the scenario. Holds the shared id, the reset step used on restart
    /// and the highlight shown when the aim reticle is on the object.
    /// </summary>
    public abstract class SimulationEntity : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string entityId;
        [SerializeField] protected string displayName;
        [SerializeField] GameObject highlight;

        protected bool isActive = true;
        bool highlighted;

        public string EntityId => entityId;
        public string DisplayName => displayName;
        public virtual bool CanInteract => isActive;
        public virtual string ActionHint => "Tap to use";

        public virtual void Initialise(ScenarioConfig config)
        {
            isActive = true;
            ResetEntity();
        }

        // Puts the entity back to how it was at the start of a session.
        public virtual void ResetEntity()
        {
            SetHighlighted(false);
        }

        public virtual void SetHighlighted(bool on)
        {
            highlighted = on;
            if (highlight != null) highlight.SetActive(on && CanInteract);
        }

        protected void RefreshHighlight()
        {
            SetHighlighted(highlighted);
        }

        public abstract void OnTap(InteractionContext context);
    }
}
