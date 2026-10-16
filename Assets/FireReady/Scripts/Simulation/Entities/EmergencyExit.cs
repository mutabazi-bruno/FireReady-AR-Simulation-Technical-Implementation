using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    public class EmergencyExit : SimulationEntity
    {
        [SerializeField] Renderer sign;
        [SerializeField] Color signColor = new Color(0.1f, 0.85f, 0.35f);

        Material signMaterial;
        int firesOutOfControl;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public override string ActionHint => "Tap to evacuate (ends the session)";

        void Awake()
        {
            if (sign != null) signMaterial = sign.material;
        }

        void OnEnable()
        {
            EventBus.Subscribe<FireStateChanged>(OnFireStateChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<FireStateChanged>(OnFireStateChanged);
        }

        public override void ResetEntity()
        {
            firesOutOfControl = 0;
            base.ResetEntity();
        }

        public override void OnTap(InteractionContext context)
        {
            EventBus.Publish(new EvacuationChosen());
        }

        void OnFireStateChanged(FireStateChanged evt)
        {
            if (evt.Current == FireState.OutOfControl) firesOutOfControl++;
            else if (evt.Previous == FireState.OutOfControl) firesOutOfControl = Mathf.Max(0, firesOutOfControl - 1);
        }

        void Update()
        {
            if (signMaterial == null) return;
            // The sign pulses while a fire is out of control to draw the eye to the way out.
            float level = firesOutOfControl > 0 ? 1.5f + Mathf.PingPong(Time.time * 3f, 2.5f) : 1.2f;
            signMaterial.SetColor(EmissionColor, signColor * level);
        }
    }
}
