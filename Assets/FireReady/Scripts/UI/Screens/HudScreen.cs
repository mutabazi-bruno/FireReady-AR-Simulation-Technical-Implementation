using FireReady.Core;
using FireReady.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>
    /// The simulation interface (FR-21): current task, task list, fires remaining, the held
    /// extinguisher and its agent, the aim reticle, distance feedback and the spray controls.
    /// </summary>
    public class HudScreen : UIScreen
    {
        [Header("Top bar")]
        [SerializeField] TMP_Text currentTaskText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text firesText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] Button pauseButton;

        [Header("Task list")]
        [SerializeField] Button taskToggle;
        [SerializeField] GameObject taskPanel;
        [SerializeField] RowList taskRows;
        [SerializeField] TMP_Text taskProgressText;

        [Header("Held extinguisher")]
        [SerializeField] GameObject heldPanel;
        [SerializeField] TMP_Text heldName;
        [SerializeField] Image heldBand;
        [SerializeField] Image agentFill;
        [SerializeField] TMP_Text agentText;
        [SerializeField] TMP_Text pinStateText;
        [SerializeField] GameObject noExtinguisherHint;

        [Header("Controls")]
        [SerializeField] Button pullPinButton;
        [SerializeField] HoldButton sprayButton;
        [SerializeField] TMP_Text sprayLabel;
        [SerializeField] Image sprayRing;

        [Header("Aim")]
        [SerializeField] Image reticle;
        [SerializeField] TMP_Text reticleLabel;
        [SerializeField] TMP_Text reticleHint;
        [SerializeField] GameObject distancePanel;
        [SerializeField] Image distanceIcon;
        [SerializeField] TMP_Text distanceText;

        [Header("Evacuation (FR-19)")]
        [SerializeField] GameObject evacuateBanner;
        [SerializeField] TMP_Text evacuateText;

        int outOfControlCount;
        Extinguisher held;

        protected override void BindButtons()
        {
            Bind(pauseButton, () => Sim.PauseSession());
            Bind(pullPinButton, () => Sim.Interaction.RequestPullPin());
            Bind(taskToggle, () => SetActive(taskPanel, !taskPanel.activeSelf));

            if (sprayButton != null)
            {
                sprayButton.Pressed += () => Sim.Interaction.SetSprayHeld(true);
                sprayButton.Released += () => Sim.Interaction.SetSprayHeld(false);
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<TasksChanged>(OnTasksChanged);
            EventBus.Subscribe<SessionStarted>(OnSessionStarted);
            EventBus.Subscribe<ExtinguisherSelected>(OnSelected);
            EventBus.Subscribe<ExtinguisherDropped>(OnDropped);
            EventBus.Subscribe<PinPulled>(OnPinPulled);
            EventBus.Subscribe<SprayStarted>(OnSprayChanged);
            EventBus.Subscribe<SprayStopped>(OnSprayStopped);
            EventBus.Subscribe<AimTargetChanged>(OnAimTargetChanged);
            EventBus.Subscribe<AimUpdated>(OnAimUpdated);
            EventBus.Subscribe<FireStateChanged>(OnFireStateChanged);
            EventBus.Subscribe<PerformanceUpdated>(OnPerformanceUpdated);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<TasksChanged>(OnTasksChanged);
            EventBus.Unsubscribe<SessionStarted>(OnSessionStarted);
            EventBus.Unsubscribe<ExtinguisherSelected>(OnSelected);
            EventBus.Unsubscribe<ExtinguisherDropped>(OnDropped);
            EventBus.Unsubscribe<PinPulled>(OnPinPulled);
            EventBus.Unsubscribe<SprayStarted>(OnSprayChanged);
            EventBus.Unsubscribe<SprayStopped>(OnSprayStopped);
            EventBus.Unsubscribe<AimTargetChanged>(OnAimTargetChanged);
            EventBus.Unsubscribe<AimUpdated>(OnAimUpdated);
            EventBus.Unsubscribe<FireStateChanged>(OnFireStateChanged);
            EventBus.Unsubscribe<PerformanceUpdated>(OnPerformanceUpdated);
        }

        protected override void OnShow()
        {
            held = Sim.Interaction.Held;
            RecountOutOfControl();
            RefreshHeld();
            RefreshTasks();
            OnAimTargetChanged(default);
        }

        void Update()
        {
            if (Sim == null) return;

            if (timerText != null) timerText.text = FormatTime(Sim.SessionTime);
            if (firesText != null) firesText.text = Sim.Entities.FiresRemaining.ToString();

            if (held != null)
            {
                float fraction = held.AgentFraction;
                if (agentFill != null)
                {
                    agentFill.fillAmount = fraction;
                    agentFill.color = fraction < 0.25f ? theme.danger : fraction < 0.5f ? theme.warning : theme.success;
                }
                if (agentText != null) agentText.text = held.IsEmpty ? "Empty" : $"{held.AgentRemaining:0.0} s left";
                if (sprayRing != null)
                {
                    sprayRing.fillAmount = fraction;
                    sprayRing.color = agentFill != null ? agentFill.color : theme.accent;
                }
            }
        }

        // Tasks

        void OnSessionStarted(SessionStarted evt)
        {
            outOfControlCount = 0;
            SetActive(evacuateBanner, false);
            if (scoreText != null) scoreText.text = "Score 100";
        }

        void OnTasksChanged(TasksChanged evt) => RefreshTasks();

        void RefreshTasks()
        {
            var tasks = Sim.Tasks;
            var current = tasks.CurrentTask;
            if (currentTaskText != null)
                currentTaskText.text = current != null ? current.Description : Sim.Entities.FiresRemaining > 0 ? "Watch for new fires" : "All tasks done";

            if (taskRows == null) return;
            taskRows.Clear();
            foreach (var task in tasks.Tasks)
            {
                var row = taskRows.Add();
                if (task.IsComplete)
                {
                    row.Set(theme.check, theme.success, task.Description, null, FormatTime(task.CompletedAt));
                }
                else if (!task.IsAvailable)
                {
                    row.Set(theme.locked, theme.textMuted, task.Description, task.LinkedFireId != null ? "Not burning yet" : "Needed later");
                    row.SetTitleColor(theme.textMuted);
                }
                else
                {
                    bool isCurrent = task == current;
                    row.Set(isCurrent ? theme.target : theme.circle, isCurrent ? theme.accent : theme.textPrimary, task.Description, isCurrent ? "Do this now" : null);
                }
            }

            if (taskProgressText != null) taskProgressText.text = $"{tasks.CompletedCount}/{tasks.Tasks.Count}";
        }

        // Held extinguisher

        void OnSelected(ExtinguisherSelected evt)
        {
            held = evt.Extinguisher;
            RefreshHeld();
        }

        void OnDropped(ExtinguisherDropped evt)
        {
            if (held == evt.Extinguisher) held = null;
            RefreshHeld();
        }

        void OnPinPulled(PinPulled evt) => RefreshHeld();

        void OnSprayChanged(SprayStarted evt) => RefreshSprayLabel(true);

        void OnSprayStopped(SprayStopped evt) => RefreshSprayLabel(false);

        void RefreshHeld()
        {
            bool hasHeld = held != null;
            SetActive(heldPanel, hasHeld);
            SetActive(noExtinguisherHint, !hasHeld);
            SetActive(pullPinButton, hasHeld && !held.PinPulled);
            SetActive(sprayButton, hasHeld);

            if (!hasHeld)
            {
                if (sprayButton != null) sprayButton.ForceRelease();
                return;
            }

            if (heldName != null) heldName.text = held.DisplayName;
            if (heldBand != null && held.Config != null) heldBand.color = held.Config.bandColor;
            if (pinStateText != null)
            {
                pinStateText.text = held.PinPulled ? "Pin out: ready" : "Pin in: pull it first";
                pinStateText.color = held.PinPulled ? theme.success : theme.warning;
            }
            RefreshSprayLabel(held.IsDischarging);
        }

        void RefreshSprayLabel(bool spraying)
        {
            if (sprayLabel != null) sprayLabel.text = spraying ? "SPRAYING\nsweep side to side" : "HOLD TO\nSQUEEZE";
        }

        // Aim

        void OnAimTargetChanged(AimTargetChanged evt)
        {
            bool hasTarget = evt.Entity != null;
            if (reticleLabel != null)
            {
                reticleLabel.text = hasTarget ? evt.Label : "";
                reticleLabel.gameObject.SetActive(hasTarget);
            }
            if (reticleHint != null)
            {
                reticleHint.text = hasTarget ? evt.Hint : "";
                reticleHint.gameObject.SetActive(hasTarget);
            }
            if (reticle != null && theme != null) reticle.color = hasTarget ? theme.info : theme.textPrimary;
        }

        void OnAimUpdated(AimUpdated evt)
        {
            SetActive(distancePanel, evt.Band != DistanceBand.NoTarget);
            if (evt.Band == DistanceBand.NoTarget) return;

            string text;
            Color color;
            Sprite icon;
            switch (evt.Band)
            {
                case DistanceBand.TooClose:
                    text = $"{evt.Distance:0.0} m  Too close, step back";
                    color = theme.danger;
                    icon = theme.warningIcon;
                    break;
                case DistanceBand.TooFar:
                    text = $"{evt.Distance:0.0} m  Too far, move closer";
                    color = theme.warning;
                    icon = theme.warningIcon;
                    break;
                default:
                    text = evt.OnBase ? $"{evt.Distance:0.0} m  Safe distance, aiming at the base" : $"{evt.Distance:0.0} m  Safe distance, aim lower";
                    color = evt.OnBase ? theme.success : theme.warning;
                    icon = evt.OnBase ? theme.check : theme.target;
                    break;
            }

            if (distanceText != null)
            {
                distanceText.text = text;
                distanceText.color = color;
            }
            if (distanceIcon != null)
            {
                distanceIcon.sprite = icon;
                distanceIcon.color = color;
            }
            if (reticle != null && evt.HasFire) reticle.color = color;
        }

        // Evacuation prompt

        void OnFireStateChanged(FireStateChanged evt)
        {
            if (evt.Current == FireState.OutOfControl || evt.Previous == FireState.OutOfControl) RecountOutOfControl();
        }

        void RecountOutOfControl()
        {
            outOfControlCount = 0;
            foreach (var fire in Sim.Entities.Fires)
                if (fire.State == FireState.OutOfControl) outOfControlCount++;

            SetActive(evacuateBanner, outOfControlCount > 0);
            if (evacuateText != null && outOfControlCount > 0)
                evacuateText.text = "A fire is out of control. Evacuate: walk to the green EXIT and tap it.";
        }

        void OnPerformanceUpdated(PerformanceUpdated evt)
        {
            if (scoreText != null)
                scoreText.text = evt.MistakeCount > 0 ? $"Score {evt.Score}  ({evt.MistakeCount} {(evt.MistakeCount == 1 ? "mistake" : "mistakes")})" : $"Score {evt.Score}";
        }
    }
}
