using System.Collections;
using System.Collections.Generic;
using FireReady.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FireReady.AR
{
    /// <summary>
    /// Wraps AR Foundation: support check, plane detection, raycasting, the placement preview,
    /// anchoring and tracking state. Nothing else in the app talks to AR Foundation.
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        [Header("AR Foundation")]
        [SerializeField] ARSession session;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARAnchorManager anchorManager;
        [SerializeField] Camera arCamera;

        [Header("Placement")]
        [SerializeField] Transform placementPreview;
        [SerializeField] Transform scenarioRoot;
        [Tooltip("Smallest floor plane, in metres per side, before placement is allowed (FR-04).")]
        [SerializeField] float minPlaneSize = 1.5f;

        [Header("Editor testing without a device")]
        [SerializeField] bool editorFallback = true;
        [SerializeField] GameObject editorFloor;

        static readonly List<ARRaycastHit> rayHits = new List<ARRaycastHit>();

        PlacementStage stage = PlacementStage.Idle;
        Pose previewPose;
        bool hasPreview;
        Transform anchorTransform;
        bool usingFallback;
        bool tracking = true;
        bool placing;
        float lastProgress = -1f;
        bool lastReady;

        public PlacementStage Stage => stage;
        public Transform ScenarioRoot => scenarioRoot;
        public bool UsingFallback => usingFallback;
        public bool IsTracking => tracking;
        public bool HasPreview => hasPreview;

        void Awake()
        {
            if (scenarioRoot == null) scenarioRoot = new GameObject("ScenarioRoot").transform;
            if (placementPreview != null) placementPreview.gameObject.SetActive(false);
            if (editorFloor != null) editorFloor.SetActive(false);
            if (planeManager != null) planeManager.enabled = false;
        }

        IEnumerator Start()
        {
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();

            bool supported = ARSession.state != ARSessionState.Unsupported;

            if (!supported && Application.isEditor && editorFallback)
            {
                // No AR provider in the editor: use a virtual floor at y = 0 so the flow can still be tested.
                usingFallback = true;
                supported = true;
                if (session != null) session.enabled = false;
            }

            EventBus.Publish(new ARAvailabilityChecked
            {
                Supported = supported,
                Message = supported
                    ? null
                    : "This phone does not support Google Play Services for AR (ARCore). FireReady AR needs an ARCore supported Android device."
            });
        }

        // Setup

        public void BeginScanning()
        {
            hasPreview = false;
            lastProgress = -1f;
            if (placementPreview != null) placementPreview.gameObject.SetActive(false);

            if (usingFallback)
            {
                if (editorFloor != null) editorFloor.SetActive(true);
            }
            else if (planeManager != null)
            {
                planeManager.enabled = true;
                SetPlanesVisible(true);
            }

            SetStage(PlacementStage.Scanning);
        }

        public void StopScanning()
        {
            if (planeManager != null)
            {
                planeManager.enabled = false;
                SetPlanesVisible(false);
            }
            if (editorFloor != null) editorFloor.SetActive(false);
            if (placementPreview != null) placementPreview.gameObject.SetActive(false);
        }

        // Starts placement again for an already built scene. The scene is hidden until the new spot is confirmed.
        public void BeginReposition()
        {
            scenarioRoot.gameObject.SetActive(false);
            BeginScanning();
        }

        void Update()
        {
            switch (stage)
            {
                case PlacementStage.Scanning:
                case PlacementStage.ReadyToPlace:
                case PlacementStage.Previewing:
                    UpdateScan();
                    if (stage != PlacementStage.Scanning && TouchInput.TryGetWorldTap(out var tap)) TryMovePreview(tap);
                    break;
                case PlacementStage.Placed:
                    UpdateTracking();
                    break;
            }
        }

        void UpdateScan()
        {
            float width = 0f;
            float length = 0f;

            if (usingFallback)
            {
                width = length = minPlaneSize * 2f;
            }
            else if (planeManager != null)
            {
                foreach (var plane in planeManager.trackables)
                {
                    if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                    var size = plane.size;
                    if (Mathf.Min(size.x, size.y) > Mathf.Min(width, length))
                    {
                        width = size.x;
                        length = size.y;
                    }
                }
            }

            float progress = Mathf.Clamp01(Mathf.Min(width, length) / minPlaneSize);
            bool ready = progress >= 1f;

            if (Mathf.Abs(progress - lastProgress) > 0.02f || ready != lastReady)
            {
                lastProgress = progress;
                lastReady = ready;
                EventBus.Publish(new PlaneScanUpdated { LargestWidth = width, LargestLength = length, Progress = progress, Ready = ready });
            }

            if (ready && stage == PlacementStage.Scanning) SetStage(PlacementStage.ReadyToPlace);
        }

        void TryMovePreview(Vector2 screenPosition)
        {
            if (!TryGetPlacementPose(screenPosition, out var pose)) return;

            previewPose = pose;
            hasPreview = true;
            if (placementPreview != null)
            {
                placementPreview.SetPositionAndRotation(pose.position, pose.rotation);
                placementPreview.gameObject.SetActive(true);
            }
            SetStage(PlacementStage.Previewing);
        }

        public bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose)
        {
            pose = default;
            Vector3 point;

            if (usingFallback)
            {
                var ray = arCamera.ScreenPointToRay(screenPosition);
                var floor = new Plane(Vector3.up, Vector3.zero);
                if (!floor.Raycast(ray, out float enter)) return false;
                point = ray.GetPoint(enter);
            }
            else
            {
                if (raycastManager == null || !raycastManager.Raycast(screenPosition, rayHits, TrackableType.PlaneWithinPolygon))
                    return false;

                bool found = false;
                point = default;
                foreach (var hit in rayHits)
                {
                    if (hit.trackable is ARPlane plane && plane.alignment == PlaneAlignment.HorizontalUp)
                    {
                        point = hit.pose.position;
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            // FR-05: the scene faces the user, so its front edge is the side they are standing on.
            var toCamera = arCamera.transform.position - point;
            toCamera.y = 0f;
            var rotation = toCamera.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toCamera) : Quaternion.identity;
            pose = new Pose(point, rotation);
            return true;
        }

        public async void ConfirmPlacement()
        {
            if (!hasPreview || placing) return;
            placing = true;

            var anchor = await CreateAnchorAsync(previewPose);
            if (anchorTransform != null && anchorTransform != anchor) Destroy(anchorTransform.gameObject, 0.1f);
            anchorTransform = anchor;

            scenarioRoot.SetParent(anchorTransform, false);
            scenarioRoot.localPosition = Vector3.zero;
            scenarioRoot.localRotation = Quaternion.identity;
            scenarioRoot.gameObject.SetActive(true);

            StopScanning();
            placing = false;
            tracking = true;
            SetStage(PlacementStage.Placed);
            EventBus.Publish(new ScenePlaced { Root = scenarioRoot });
        }

        async Awaitable<Transform> CreateAnchorAsync(Pose pose)
        {
            if (!usingFallback && anchorManager != null && anchorManager.enabled && anchorManager.subsystem != null)
            {
                var result = await anchorManager.TryAddAnchorAsync(pose);
                if (result.status.IsSuccess() && result.value != null)
                {
                    result.value.name = "SceneAnchor";
                    return result.value.transform;
                }
                Debug.LogWarning("Could not create an AR anchor, the scene will use a plain transform.");
            }

            var fallback = new GameObject("SceneAnchor").transform;
            fallback.SetPositionAndRotation(pose.position, pose.rotation);
            return fallback;
        }

        public void ResetPlacement()
        {
            scenarioRoot.SetParent(null, false);
            scenarioRoot.gameObject.SetActive(true);
            if (anchorTransform != null) Destroy(anchorTransform.gameObject);
            anchorTransform = null;
            hasPreview = false;
            StopScanning();
            SetStage(PlacementStage.Idle);
        }

        void UpdateTracking()
        {
            if (usingFallback) return;

            bool nowTracking = ARSession.state == ARSessionState.SessionTracking
                && ARSession.notTrackingReason == NotTrackingReason.None;
            if (nowTracking == tracking) return;

            tracking = nowTracking;
            if (tracking) EventBus.Publish(new TrackingRestored());
            else EventBus.Publish(new TrackingLost { Reason = DescribeTrackingProblem(ARSession.notTrackingReason) });
        }

        static string DescribeTrackingProblem(NotTrackingReason reason)
        {
            switch (reason)
            {
                case NotTrackingReason.InsufficientLight: return "It is too dark. Turn on a light or move somewhere brighter.";
                case NotTrackingReason.InsufficientFeatures: return "The camera cannot see enough detail. Point it at the floor and the scene.";
                case NotTrackingReason.ExcessiveMotion: return "The phone is moving too fast. Hold it steady.";
                case NotTrackingReason.Relocalizing: return "Finding the scene again. Point the camera where it was placed.";
                default: return "Tracking lost. Point the camera at the floor where the scene was placed.";
            }
        }

        void SetPlanesVisible(bool visible)
        {
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(visible);
        }

        void SetStage(PlacementStage next)
        {
            if (stage == next) return;
            stage = next;
            EventBus.Publish(new PlacementStageChanged { Stage = stage });
        }
    }
}
