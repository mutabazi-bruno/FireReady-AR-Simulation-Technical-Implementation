using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    public class PowerBreaker : SimulationEntity
    {
        [SerializeField] Transform lever;
        [SerializeField] Vector3 leverOnEuler = new Vector3(-35f, 0f, 0f);
        [SerializeField] Vector3 leverOffEuler = new Vector3(35f, 0f, 0f);
        [SerializeField] GameObject powerOnLight;
        [SerializeField] GameObject powerOffLight;

        bool powerOn = true;

        public bool PowerOn => powerOn;
        public override bool CanInteract => base.CanInteract && powerOn;
        public override string ActionHint => powerOn ? "Tap to switch off the power" : "Power is off";

        public override void ResetEntity()
        {
            powerOn = true;
            UpdateVisuals();
            base.ResetEntity();
        }

        public override void OnTap(InteractionContext context)
        {
            if (!powerOn) return;
            powerOn = false;
            UpdateVisuals();
            RefreshHighlight();
            EventBus.Publish(new PowerIsolated());
        }

        void UpdateVisuals()
        {
            if (lever != null) lever.localRotation = Quaternion.Euler(powerOn ? leverOnEuler : leverOffEuler);
            if (powerOnLight != null) powerOnLight.SetActive(powerOn);
            if (powerOffLight != null) powerOffLight.SetActive(!powerOn);
        }
    }
}
