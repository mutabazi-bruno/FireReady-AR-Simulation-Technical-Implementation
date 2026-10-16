using UnityEngine;

namespace FireReady.Simulation
{
    // The pin on the held extinguisher. Tapping it is the "Pull" step of PASS (FR-12).
    public class SafetyPin : MonoBehaviour, IInteractable
    {
        [SerializeField] Extinguisher extinguisher;
        [SerializeField] GameObject pinVisual;
        [SerializeField] Vector3 pullOffset = new Vector3(0.12f, 0f, 0f);
        [SerializeField] float pullDuration = 0.25f;

        Vector3 restPosition;
        float pullTimer = -1f;

        public bool CanInteract => extinguisher != null && extinguisher.IsHeld && !extinguisher.PinPulled;

        void Awake()
        {
            if (extinguisher == null) extinguisher = GetComponentInParent<Extinguisher>();
            if (pinVisual == null) pinVisual = gameObject;
            restPosition = pinVisual.transform.localPosition;
        }

        public void OnTap(InteractionContext context)
        {
            if (CanInteract) extinguisher.PullPin();
        }

        public void PlayPulled()
        {
            pullTimer = 0f;
        }

        public void ResetPin()
        {
            pullTimer = -1f;
            if (pinVisual == null) return;
            pinVisual.transform.localPosition = restPosition;
            pinVisual.SetActive(true);
        }

        void Update()
        {
            if (pullTimer < 0f || pinVisual == null) return;

            pullTimer += Time.deltaTime;
            float t = Mathf.Clamp01(pullTimer / pullDuration);
            pinVisual.transform.localPosition = restPosition + pullOffset * t;

            if (t >= 1f)
            {
                pinVisual.SetActive(false);
                pullTimer = -1f;
            }
        }
    }
}
