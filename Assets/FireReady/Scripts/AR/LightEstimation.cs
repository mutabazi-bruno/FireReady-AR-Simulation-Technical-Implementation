using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FireReady.AR
{
    /// <summary>
    /// Matches the scene light to the real room using ARCore light estimation, so the
    /// virtual workshop is not brighter or a different colour than the room around it.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightEstimation : MonoBehaviour
    {
        [SerializeField] ARCameraManager cameraManager;
        [SerializeField] float minIntensityScale = 0.45f;
        [SerializeField] float maxIntensityScale = 1.3f;

        Light sceneLight;
        float baseIntensity;
        Color baseAmbient;

        void Awake()
        {
            sceneLight = GetComponent<Light>();
            baseIntensity = sceneLight.intensity;
            baseAmbient = RenderSettings.ambientLight;
        }

        void OnEnable()
        {
            if (cameraManager != null) cameraManager.frameReceived += OnFrameReceived;
        }

        void OnDisable()
        {
            if (cameraManager != null) cameraManager.frameReceived -= OnFrameReceived;
        }

        void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            var estimate = args.lightEstimation;

            if (estimate.averageBrightness.HasValue)
            {
                float scale = Mathf.Lerp(minIntensityScale, maxIntensityScale, estimate.averageBrightness.Value);
                sceneLight.intensity = baseIntensity * scale;
                RenderSettings.ambientLight = baseAmbient * scale;
            }

            if (estimate.colorCorrection.HasValue)
            {
                var correction = estimate.colorCorrection.Value;
                sceneLight.color = new Color(correction.r, correction.g, correction.b);
            }
        }
    }
}
