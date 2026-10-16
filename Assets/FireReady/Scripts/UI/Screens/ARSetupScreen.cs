using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // FR-03 to FR-07: scanning guidance, placement instructions and the confirm step.
    public class ARSetupScreen : UIScreen
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text instructions;
        [SerializeField] Image stepIcon;
        [SerializeField] GameObject progressGroup;
        [SerializeField] Image progressFill;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button confirmButton;
        [SerializeField] Button backButton;
        [SerializeField] RectTransform scanHint;

        PlacementStage stage;
        float progress;
        float width;
        float length;

        protected override void BindButtons()
        {
            Bind(confirmButton, () => Sim.ConfirmPlacement());
            Bind(backButton, () => Sim.ReturnToMenu());
        }

        void OnEnable()
        {
            EventBus.Subscribe<PlacementStageChanged>(OnStageChanged);
            EventBus.Subscribe<PlaneScanUpdated>(OnScanUpdated);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlacementStageChanged>(OnStageChanged);
            EventBus.Unsubscribe<PlaneScanUpdated>(OnScanUpdated);
        }

        protected override void OnShow()
        {
            stage = Sim != null ? Sim.AR.Stage : PlacementStage.Scanning;
            progress = 0f;
            SetActive(backButton, Sim == null || !Sim.IsReposition);
            Refresh();
        }

        void OnStageChanged(PlacementStageChanged evt)
        {
            stage = evt.Stage;
            Refresh();
        }

        void OnScanUpdated(PlaneScanUpdated evt)
        {
            progress = evt.Progress;
            width = evt.LargestWidth;
            length = evt.LargestLength;
            Refresh();
        }

        void Refresh()
        {
            bool reposition = Sim != null && Sim.IsReposition;
            bool scanning = stage == PlacementStage.Scanning || stage == PlacementStage.Idle;

            SetActive(progressGroup, scanning);
            SetActive(scanHint, scanning);
            SetActive(confirmButton, stage == PlacementStage.Previewing);

            if (progressFill != null) progressFill.fillAmount = progress;
            if (progressText != null)
                progressText.text = width > 0f ? $"Floor found: {width:0.0} m x {length:0.0} m (need 1.5 m x 1.5 m)" : "Looking for the floor...";

            switch (stage)
            {
                case PlacementStage.ReadyToPlace:
                    title.text = reposition ? "Move the scene" : "Floor found";
                    instructions.text = "Tap the floor where you want the training scene. Leave about 3 m x 3 m of clear space.";
                    SetIcon(theme != null ? theme.check : null, theme != null ? theme.success : Color.white);
                    break;
                case PlacementStage.Previewing:
                    title.text = "Check the spot";
                    instructions.text = "This outline shows where the scene will be. Tap somewhere else to move it, or press Confirm.";
                    SetIcon(theme != null ? theme.target : null, theme != null ? theme.info : Color.white);
                    break;
                default:
                    title.text = "Scan the floor";
                    instructions.text = "Point the camera at the floor and move the phone slowly from side to side until the floor is covered in dots.";
                    SetIcon(theme != null ? theme.infoIcon : null, theme != null ? theme.warning : Color.white);
                    break;
            }
        }

        void SetIcon(Sprite sprite, Color color)
        {
            if (stepIcon == null) return;
            stepIcon.sprite = sprite;
            stepIcon.color = color;
        }

        void Update()
        {
            // Gentle side-to-side sway on the phone icon while scanning.
            if (scanHint != null && scanHint.gameObject.activeSelf)
                scanHint.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 2.2f) * 60f, scanHint.anchoredPosition.y);
        }
    }
}
