using FireReady.Core;
using UnityEngine;

namespace FireReady.UI
{
    /// <summary>
    /// The slowly turning extinguisher with a small fire behind it on the main menu.
    /// It hangs in front of the camera and is only shown while the menu is open.
    /// </summary>
    public class MenuShowcase : MonoBehaviour
    {
        [SerializeField] GameObject content;
        [SerializeField] Transform spinner;
        [SerializeField] float degreesPerSecond = 22f;
        [SerializeField] float bobHeight = 0.01f;
        [SerializeField] SimulationStateId visibleIn = SimulationStateId.MainMenu;
        [Tooltip("Optional camera that renders this showcase into a texture. Only runs while visible.")]
        [SerializeField] Camera renderCamera;

        Vector3 spinnerHome;

        void Awake()
        {
            if (spinner != null) spinnerHome = spinner.localPosition;
            if (content != null) content.SetActive(false);
            if (renderCamera != null) renderCamera.enabled = false;
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
        }

        void OnStateChanged(SimulationStateChanged evt)
        {
            bool visible = evt.Current == visibleIn;
            if (content != null) content.SetActive(visible);
            if (renderCamera != null) renderCamera.enabled = visible;
        }

        void Update()
        {
            if (spinner == null || content == null || !content.activeSelf) return;
            spinner.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
            spinner.localPosition = spinnerHome + Vector3.up * (Mathf.Sin(Time.time * 1.3f) * bobHeight);
        }
    }
}
