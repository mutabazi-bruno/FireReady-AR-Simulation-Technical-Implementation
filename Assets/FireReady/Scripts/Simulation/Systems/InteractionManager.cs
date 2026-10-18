using System;
using FireReady.Config;
using FireReady.Core;
using FireReady.Effects;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Turns input into actions: taps on entities, holding the spray, the aim ray from the centre
    /// of the camera and the distance to the target fire. Ticked by the running state only,
    /// so nothing here runs while paused.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] Camera arCamera;
        [SerializeField] Transform handAnchor;
        [SerializeField] Transform nozzleAnchor;
        [SerializeField] EntityManager entityManager;
        [SerializeField] PoolManager pools;
        [SerializeField] LayerMask raycastMask = ~0;
        [SerializeField] float maxRayDistance = 12f;
        [SerializeField] float idleAimDistance = 1.8f;
        [SerializeField] float puffInterval = 0.07f;

        readonly RaycastHit[] hits = new RaycastHit[24];

        SimulationRules rules = new SimulationRules();
        Extinguisher held;
        SimulationEntity highlighted;
        FireSource targetFire;
        bool targetOnBase;
        float targetDistance;
        Vector3 aimPoint;
        DistanceBand band = DistanceBand.NoTarget;
        bool inputEnabled;
        float tooCloseTimer;
        bool tooCloseReported;
        float puffTimer;

        public Extinguisher Held => held;
        public FireSource TargetFire => targetFire;
        public bool InputEnabled => inputEnabled;
        public Camera ARCamera => arCamera;

        public void Configure(SimulationRules simulationRules)
        {
            rules = simulationRules ?? new SimulationRules();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (enabled) return;

            if (held != null) held.StopDischarge();
            SetHighlighted(null);
        }

        public void Tick(float dt)
        {
            if (!inputEnabled || arCamera == null) return;

            if (TouchInput.TryGetWorldTap(out var tapPosition)) HandleTap(tapPosition);

            UpdateAim();
            UpdateSpray(dt);
            UpdateSafety(dt);
        }

        // Selection

        public void Select(Extinguisher extinguisher)
        {
            if (extinguisher == null || held == extinguisher) return;

            if (held != null) DropHeld();

            held = extinguisher;
            held.AttachTo(handAnchor, nozzleAnchor);
            EventBus.Publish(new ExtinguisherSelected { Extinguisher = held });
        }

        public void DropHeld()
        {
            if (held == null) return;
            var dropped = held;
            held = null;
            dropped.ReturnToStand();
            EventBus.Publish(new ExtinguisherDropped { Extinguisher = dropped });
        }

        public void RequestPullPin()
        {
            if (!inputEnabled) return;
            if (held == null)
            {
                EventBus.Publish(new HintRequested { Message = "Pick up an extinguisher first" });
                return;
            }
            held.PullPin();
        }

        // Called by the hold-to-spray button on the HUD.
        public void SetSprayHeld(bool pressed)
        {
            if (!inputEnabled) return;

            if (!pressed)
            {
                if (held != null) held.StopDischarge();
                return;
            }

            if (held == null)
            {
                EventBus.Publish(new HintRequested { Message = "Pick up an extinguisher first" });
                return;
            }

            held.StartDischarge();
        }

        public void ResetInteraction()
        {
            DropHeld();
            SetHighlighted(null);
            targetFire = null;
            band = DistanceBand.NoTarget;
            tooCloseTimer = 0f;
            tooCloseReported = false;
        }

        // Taps

        void HandleTap(Vector2 screenPosition)
        {
            var ray = arCamera.ScreenPointToRay(screenPosition);
            int count = Physics.RaycastNonAlloc(ray, hits, maxRayDistance, raycastMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, 0, count, Comparer.Instance);

            for (int i = 0; i < count; i++)
            {
                var interactable = hits[i].collider.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract) continue;

                float distance = EntityManager.HorizontalDistance(arCamera.transform.position, hits[i].point);
                bool partOfHeldItem = interactable is SafetyPin;

                if (!partOfHeldItem && distance > rules.interactionReach)
                {
                    var entityName = interactable is SimulationEntity entity ? entity.DisplayName : "it";
                    EventBus.Publish(new HintRequested { Message = $"Walk closer to use the {entityName}" });
                    return;
                }

                interactable.OnTap(new InteractionContext(this, hits[i].point, distance));
                return;
            }
        }

        // Aim

        void UpdateAim()
        {
            var cameraTransform = arCamera.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            int count = Physics.RaycastNonAlloc(ray, hits, maxRayDistance, raycastMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, 0, count, Comparer.Instance);

            FireSource fire = null;
            bool onBase = false;
            SimulationEntity entity = null;
            bool hasPoint = false;
            aimPoint = ray.GetPoint(idleAimDistance);

            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var zone = hit.collider.GetComponent<FireZone>();
                if (zone != null)
                {
                    var owner = zone.Owner;
                    if (owner == null || !(owner.IsBurning || owner.HasPendingReignite)) continue;

                    if (fire == null)
                    {
                        fire = owner;
                        if (!hasPoint) { aimPoint = hit.point; hasPoint = true; }
                    }

                    // The ray often enters the flames before it reaches the base, so any base hit counts.
                    if (owner == fire && zone.IsBase) onBase = true;
                    continue;
                }

                if (fire == null && entity == null)
                {
                    var candidate = hit.collider.GetComponentInParent<SimulationEntity>();
                    if (candidate != null && candidate.CanInteract && !(candidate is FireSource)) entity = candidate;
                }

                if (!hasPoint) { aimPoint = hit.point; hasPoint = true; }
            }

            targetFire = fire;
            targetOnBase = onBase;
            targetDistance = fire != null ? EntityManager.HorizontalDistance(cameraTransform.position, fire.transform.position) : 0f;

            SetHighlighted(entity);
            if (held != null) held.AimNozzleAt(aimPoint);
        }

        void SetHighlighted(SimulationEntity entity)
        {
            if (entity == highlighted) return;
            if (highlighted != null) highlighted.SetHighlighted(false);
            highlighted = entity;
            if (highlighted != null) highlighted.SetHighlighted(true);

            EventBus.Publish(new AimTargetChanged
            {
                Entity = highlighted,
                Label = highlighted != null ? highlighted.DisplayName : null,
                Hint = highlighted != null ? highlighted.ActionHint : null
            });
        }

        // Spray

        void UpdateSpray(float dt)
        {
            if (held == null || !held.IsDischarging) return;
            if (!held.UpdateDischarge(dt)) return;

            bool inReach = targetFire != null && targetDistance <= rules.safeDistance.y;
            if (inReach)
            {
                held.Discharge(targetFire, targetOnBase, targetDistance, dt);
            }
            else if (entityManager != null && entityManager.ActiveFireCount > 0)
            {
                EventBus.Publish(new SprayMissed { DeltaTime = dt });
            }

            puffTimer -= dt;
            if (puffTimer <= 0f && pools != null && targetFire != null)
            {
                puffTimer = puffInterval;
                var puff = pools.Spawn(EffectId.SprayPuff, aimPoint, Quaternion.LookRotation(-arCamera.transform.forward));
                if (puff != null && held.Config != null) puff.SetColor(held.Config.sprayColor);
            }
        }

        // Distance rules (FR-16)

        void UpdateSafety(float dt)
        {
            var cameraPosition = arCamera.transform.position;
            float nearestDistance = float.MaxValue;
            if (entityManager != null) entityManager.NearestBurningFire(cameraPosition, out nearestDistance);

            // Standing too close to any burning fire is unsafe, whether or not it is the one being aimed at.
            if (nearestDistance < rules.safeDistance.x)
            {
                tooCloseTimer += dt;
                if (!tooCloseReported && tooCloseTimer >= rules.tooCloseSeconds)
                {
                    tooCloseReported = true;
                    EventBus.Publish(new TooCloseRecorded { Distance = nearestDistance });
                }
            }
            else
            {
                tooCloseTimer = 0f;
                tooCloseReported = false;
            }

            DistanceBand next;
            float shownDistance;
            if (nearestDistance < rules.safeDistance.x)
            {
                next = DistanceBand.TooClose;
                shownDistance = nearestDistance;
            }
            else if (targetFire != null)
            {
                next = targetDistance > rules.safeDistance.y ? DistanceBand.TooFar : DistanceBand.InRange;
                shownDistance = targetDistance;
            }
            else
            {
                next = DistanceBand.NoTarget;
                shownDistance = 0f;
            }

            if (next != band)
            {
                band = next;
                bool warn = (band == DistanceBand.TooClose) || (band == DistanceBand.TooFar && held != null);
                if (warn) EventBus.Publish(new DistanceWarning { Band = band, Distance = shownDistance });
            }

            EventBus.Publish(new AimUpdated
            {
                HasFire = targetFire != null,
                FireName = targetFire != null ? targetFire.DisplayName : null,
                Distance = shownDistance,
                Band = band,
                OnBase = targetOnBase
            });
        }

        sealed class Comparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
