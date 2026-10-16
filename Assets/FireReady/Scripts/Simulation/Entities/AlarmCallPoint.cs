using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    public class AlarmCallPoint : SimulationEntity
    {
        [SerializeField] GameObject intactGlass;
        [SerializeField] GameObject pressedGlass;
        [SerializeField] Renderer beacon;
        [SerializeField] float flashRate = 2.5f;

        bool raised;
        Material beaconMaterial;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public bool Raised => raised;
        public override bool CanInteract => base.CanInteract && !raised;
        public override string ActionHint => raised ? "Alarm already raised" : "Tap to raise the alarm";

        void Awake()
        {
            if (beacon != null) beaconMaterial = beacon.material;
        }

        public override void ResetEntity()
        {
            raised = false;
            UpdateVisuals();
            base.ResetEntity();
        }

        public override void OnTap(InteractionContext context)
        {
            if (raised) return;
            raised = true;
            UpdateVisuals();
            RefreshHighlight();
            EventBus.Publish(new AlarmRaised());
        }

        void UpdateVisuals()
        {
            if (intactGlass != null) intactGlass.SetActive(!raised);
            if (pressedGlass != null) pressedGlass.SetActive(raised);
            SetBeacon(0f);
        }

        void Update()
        {
            if (!raised || beaconMaterial == null) return;
            SetBeacon(Mathf.PingPong(Time.time * flashRate, 1f));
        }

        void SetBeacon(float level)
        {
            if (beaconMaterial == null) return;
            beaconMaterial.SetColor(EmissionColor, Color.red * (level * 4f));
        }
    }
}
