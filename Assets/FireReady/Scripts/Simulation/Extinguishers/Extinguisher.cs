using FireReady.Config;
using FireReady.Core;
using TMPro;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// One extinguisher. Every type shares the same pin, capacity and discharge behaviour; only the
    /// agent strategy differs, so there is one class holding an IExtinguishingAgent instead of
    /// four near-identical subclasses.
    /// </summary>
    public class Extinguisher : SimulationEntity
    {
        [SerializeField] ExtinguisherConfig config;

        [Header("Parts")]
        [SerializeField] SafetyPin pin;
        [SerializeField] Transform nozzle;
        [SerializeField] ParticleSystem spray;
        [SerializeField] Renderer[] bandRenderers;
        [SerializeField] TMP_Text typeLabel;
        [SerializeField] Collider[] bodyColliders;

        [Header("Hose (shown while held)")]
        [SerializeField] Transform hoseStart;
        [SerializeField] LineRenderer hose;
        [SerializeField] GameObject heldNozzleVisual;
        [SerializeField] float hoseSag = 0.12f;

        [Header("Held pose (child of the camera)")]
        [SerializeField] Vector3 heldPosition = new Vector3(0.13f, -0.24f, 0.42f);
        [SerializeField] Vector3 heldEuler = new Vector3(0f, -12f, 0f);
        [SerializeField] float heldScale = 0.45f;

        IExtinguishingAgent agent;
        Transform standSlot;
        float agentRemaining;
        bool pinPulled;
        bool discharging;
        bool held;
        int burstId;
        float burstDuration;
        Transform nozzleHome;
        Vector3 nozzleHomePosition;
        Quaternion nozzleHomeRotation;
        readonly Vector3[] hosePoints = new Vector3[14];

        public ExtinguisherConfig Config => config;
        public AgentType Type => agent != null ? agent.Type : AgentType.Water;
        public float AgentRemaining => agentRemaining;
        public float AgentFraction => config != null && config.capacitySeconds > 0f ? agentRemaining / config.capacitySeconds : 0f;
        public bool IsHeld => held;
        public bool PinPulled => pinPulled;
        public bool IsDischarging => discharging;
        public bool IsEmpty => agentRemaining <= 0f;
        public int BurstId => burstId;

        public override bool CanInteract => base.CanInteract && !held;
        public override string ActionHint => config != null ? $"Tap to pick up. {config.useFor}" : "Tap to pick up";

        public void Setup(ExtinguisherConfig extinguisherConfig, Transform slot)
        {
            config = extinguisherConfig;
            agent = config.agent;
            standSlot = slot;
            entityId = $"extinguisher_{config.name}";
            displayName = $"{config.displayName} extinguisher";
            name = $"Extinguisher_{config.displayName}";
            ApplyLook();
        }

        public override void Initialise(ScenarioConfig scenario)
        {
            if (agent == null && config != null) agent = config.agent;
            base.Initialise(scenario);
        }

        public override void ResetEntity()
        {
            StopDischarge();
            agentRemaining = config != null ? config.capacitySeconds : 0f;
            pinPulled = false;
            burstId = 0;
            if (pin != null) pin.ResetPin();
            ReturnToStand();
            base.ResetEntity();
        }

        public override void OnTap(InteractionContext context)
        {
            if (!held) context.Interaction.Select(this);
        }

        void Awake()
        {
            if (nozzle != null)
            {
                nozzleHome = nozzle.parent;
                nozzleHomePosition = nozzle.localPosition;
                nozzleHomeRotation = nozzle.localRotation;
            }
            SetHoseVisible(false);
        }

        // The cylinder sits at the bottom of the view and the nozzle is held in front, like a real hose.
        public void AttachTo(Transform hand, Transform nozzleAnchor = null)
        {
            held = true;
            transform.SetParent(hand, false);
            transform.localPosition = heldPosition;
            transform.localRotation = Quaternion.Euler(heldEuler);
            transform.localScale = Vector3.one * heldScale;

            if (nozzle != null && nozzleAnchor != null)
            {
                nozzle.SetParent(nozzleAnchor, false);
                nozzle.localPosition = Vector3.zero;
                nozzle.localRotation = Quaternion.identity;
                nozzle.localScale = Vector3.one;
                SetHoseVisible(true);
            }

            SetBodyCollidersEnabled(false);
            SetHighlighted(false);
        }

        public void ReturnToStand()
        {
            StopDischarge();
            held = false;
            if (nozzle != null && nozzleHome != null)
            {
                nozzle.SetParent(nozzleHome, false);
                nozzle.localPosition = nozzleHomePosition;
                nozzle.localRotation = nozzleHomeRotation;
                nozzle.localScale = Vector3.one;
            }
            SetHoseVisible(false);
            if (standSlot != null)
            {
                transform.SetParent(standSlot, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            transform.localScale = Vector3.one;
            SetBodyCollidersEnabled(true);
        }

        public void PullPin()
        {
            if (pinPulled || !held) return;
            pinPulled = true;
            if (pin != null) pin.PlayPulled();
            EventBus.Publish(new PinPulled { Extinguisher = this });
        }

        public bool StartDischarge()
        {
            if (discharging) return true;

            if (!pinPulled)
            {
                EventBus.Publish(new DischargeBlocked { Extinguisher = this, Reason = DischargeBlockReason.PinIn });
                return false;
            }

            if (IsEmpty)
            {
                EventBus.Publish(new DischargeBlocked { Extinguisher = this, Reason = DischargeBlockReason.Empty });
                return false;
            }

            discharging = true;
            burstId++;
            burstDuration = 0f;
            if (spray != null) spray.Play();
            EventBus.Publish(new SprayStarted { Extinguisher = this });
            return true;
        }

        public void StopDischarge()
        {
            if (!discharging) return;
            discharging = false;
            if (spray != null) spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            EventBus.Publish(new SprayStopped { Extinguisher = this, Duration = burstDuration });
        }

        // Uses up agent while the spray is held. Returns false once the extinguisher runs dry.
        public bool UpdateDischarge(float dt)
        {
            if (!discharging) return false;

            agentRemaining = Mathf.Max(0f, agentRemaining - dt);
            burstDuration += dt;

            if (agentRemaining > 0f) return true;

            StopDischarge();
            EventBus.Publish(new ExtinguisherEmpty { Extinguisher = this });
            return false;
        }

        // Asks the strategy what this agent does to the fire, then lets the fire apply it.
        public void Discharge(FireSource target, bool onBase, float distance, float dt)
        {
            if (!discharging || target == null || agent == null) return;
            var effect = agent.Evaluate(target.FireClass, target.IsPowerLive);
            target.ApplyAgent(effect, agent.Type, config.suppressionRate, onBase, distance, dt, burstId);
        }

        public void AimNozzleAt(Vector3 worldPoint)
        {
            if (nozzle == null) return;
            var direction = worldPoint - nozzle.position;
            if (direction.sqrMagnitude > 0.0001f) nozzle.rotation = Quaternion.LookRotation(direction);
        }

        void ApplyLook()
        {
            if (config == null) return;

            if (bandRenderers != null)
            {
                foreach (var band in bandRenderers)
                    if (band != null) band.material.color = config.bandColor;
            }

            if (typeLabel != null)
            {
                typeLabel.text = config.displayName.ToUpperInvariant();
                // Dark text on light bands, white text on dark bands.
                typeLabel.color = config.bandColor.grayscale > 0.5f ? new Color(0.08f, 0.08f, 0.08f) : Color.white;
            }

            if (spray != null)
            {
                var main = spray.main;
                main.startColor = config.sprayColor;
                var shape = spray.shape;
                shape.angle = config.sprayAngle;
            }
        }

        void SetHoseVisible(bool visible)
        {
            if (hose != null) hose.enabled = visible;
            if (heldNozzleVisual != null) heldNozzleVisual.SetActive(visible);
        }

        void LateUpdate()
        {
            if (!held || hose == null || hoseStart == null || nozzle == null) return;

            // Curve the hose from the valve down and up to the nozzle in the hand.
            var start = hoseStart.position;
            var end = nozzle.position;
            var control = (start + end) * 0.5f + Vector3.down * hoseSag;
            int count = hosePoints.Length;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                hosePoints[i] = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * end;
            }
            hose.positionCount = count;
            hose.SetPositions(hosePoints);
        }

        void OnDestroy()
        {
            // While held the nozzle is parked under the camera, so take it with us.
            if (held && nozzle != null) Destroy(nozzle.gameObject);
        }

        void SetBodyCollidersEnabled(bool enabled)
        {
            if (bodyColliders == null) return;
            foreach (var body in bodyColliders)
                if (body != null) body.enabled = enabled;
        }
    }
}
