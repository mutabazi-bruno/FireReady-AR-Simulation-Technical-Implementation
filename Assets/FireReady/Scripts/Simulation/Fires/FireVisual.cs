using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Drives flame size, smoke and glow from the fire's intensity. Visual only, it never changes the fire.
    /// </summary>
    public class FireVisual : MonoBehaviour
    {
        [SerializeField] FireSource fire;
        [SerializeField] Transform scaleRoot;
        [SerializeField] ParticleSystem flames;
        [SerializeField] ParticleSystem smoke;
        [SerializeField] Light glow;
        [SerializeField] float minScale = 0.35f;
        [SerializeField] float maxScale = 1.5f;
        [SerializeField] float maxGlow = 3f;

        ParticleSystem[] allSystems;
        float flameRate;
        float smokeRate;
        float currentScale;
        bool paused;

        void Awake()
        {
            if (fire == null) fire = GetComponentInParent<FireSource>();
            allSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (flames != null) flameRate = flames.emission.rateOverTimeMultiplier;
            if (smoke != null) smokeRate = smoke.emission.rateOverTimeMultiplier;
            ApplyScale(0f);
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnSimulationStateChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnSimulationStateChanged);
        }

        void OnSimulationStateChanged(SimulationStateChanged evt)
        {
            // Freeze the effects while paused so the scene looks frozen too (FR-23).
            bool shouldPause = evt.Current == SimulationStateId.Paused || evt.Current == SimulationStateId.ARSetup;
            if (shouldPause == paused) return;
            paused = shouldPause;
            foreach (var system in allSystems)
            {
                if (system == null) continue;
                if (paused) system.Pause(true);
                else if (system.isPaused) system.Play(true);
            }
        }

        void Update()
        {
            if (fire == null || paused) return;

            float t = fire.Intensity / 100f;
            bool burning = fire.IsBurning;

            float targetScale = burning ? Mathf.Lerp(minScale, maxScale, t) : 0f;
            currentScale = Mathf.MoveTowards(currentScale, targetScale, Time.deltaTime * 2f);
            ApplyScale(currentScale);

            if (flames != null)
            {
                var emission = flames.emission;
                emission.rateOverTimeMultiplier = burning ? flameRate * (0.4f + t) : 0f;
            }

            if (smoke != null)
            {
                var emission = smoke.emission;
                if (burning) emission.rateOverTimeMultiplier = smokeRate * (0.3f + 1.5f * t);
                else emission.rateOverTimeMultiplier = fire.HasPendingReignite ? smokeRate * 0.4f : 0f;
            }

            if (glow != null)
            {
                float flicker = 0.85f + Mathf.PerlinNoise(Time.time * 6f, transform.position.x) * 0.3f;
                glow.intensity = burning ? (0.4f + maxGlow * t) * flicker : 0f;
            }
        }

        void ApplyScale(float scale)
        {
            if (scaleRoot == null) return;
            scaleRoot.localScale = Vector3.one * Mathf.Max(scale, 0.0001f);
            scaleRoot.gameObject.SetActive(scale > 0.001f);
        }
    }
}
