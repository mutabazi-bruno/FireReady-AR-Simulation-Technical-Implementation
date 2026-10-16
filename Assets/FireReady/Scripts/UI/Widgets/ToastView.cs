using System.Collections.Generic;
using FireReady.Core;
using FireReady.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    public enum ToastKind
    {
        Info,
        Success,
        Warning,
        Danger
    }

    /// <summary>
    /// Short captions for what just happened. Every sound has a matching caption here, so the app
    /// can be followed with the sound off (NFR-06).
    /// </summary>
    public class ToastView : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] RowView template;
        [SerializeField] int maxVisible = 3;
        [SerializeField] float duration = 3f;

        class Toast
        {
            public RowView Row;
            public Image Background;
            public float TimeLeft;
            public string Message;
        }

        readonly List<Toast> active = new List<Toast>();
        readonly Stack<Toast> spare = new Stack<Toast>();
        string lastMessage;
        float lastMessageTime;
        int lastHitBurst = -1;

        void Awake()
        {
            if (template != null) template.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            EventBus.Subscribe<HintRequested>(OnHint);
            EventBus.Subscribe<ExtinguisherSelected>(OnSelected);
            EventBus.Subscribe<PinPulled>(OnPinPulled);
            EventBus.Subscribe<DischargeBlocked>(OnDischargeBlocked);
            EventBus.Subscribe<ExtinguisherEmpty>(OnEmpty);
            EventBus.Subscribe<AgentApplied>(OnAgentApplied);
            EventBus.Subscribe<DistanceWarning>(OnDistanceWarning);
            EventBus.Subscribe<FireIgnited>(OnFireIgnited);
            EventBus.Subscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Subscribe<FireReignited>(OnFireReignited);
            EventBus.Subscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Subscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Subscribe<TaskCompleted>(OnTaskCompleted);
            EventBus.Subscribe<MistakeRecorded>(OnMistake);
            EventBus.Subscribe<SessionStarted>(OnSessionStarted);
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<HintRequested>(OnHint);
            EventBus.Unsubscribe<ExtinguisherSelected>(OnSelected);
            EventBus.Unsubscribe<PinPulled>(OnPinPulled);
            EventBus.Unsubscribe<DischargeBlocked>(OnDischargeBlocked);
            EventBus.Unsubscribe<ExtinguisherEmpty>(OnEmpty);
            EventBus.Unsubscribe<AgentApplied>(OnAgentApplied);
            EventBus.Unsubscribe<DistanceWarning>(OnDistanceWarning);
            EventBus.Unsubscribe<FireIgnited>(OnFireIgnited);
            EventBus.Unsubscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Unsubscribe<FireReignited>(OnFireReignited);
            EventBus.Unsubscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Unsubscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Unsubscribe<TaskCompleted>(OnTaskCompleted);
            EventBus.Unsubscribe<MistakeRecorded>(OnMistake);
            EventBus.Unsubscribe<SessionStarted>(OnSessionStarted);
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
        }

        public void Show(string message, ToastKind kind)
        {
            if (string.IsNullOrEmpty(message) || template == null) return;
            if (message == lastMessage && Time.unscaledTime - lastMessageTime < 1.5f) return;
            lastMessage = message;
            lastMessageTime = Time.unscaledTime;

            if (active.Count >= maxVisible) Recycle(active[0]);

            var toast = spare.Count > 0 ? spare.Pop() : CreateToast();
            toast.Message = message;
            toast.TimeLeft = duration;
            toast.Row.gameObject.SetActive(true);
            toast.Row.transform.SetAsLastSibling();

            var color = KindColor(kind);
            toast.Row.Set(KindIcon(kind), color, message);
            if (toast.Background != null) toast.Background.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.92f);
            active.Add(toast);
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) Recycle(active[i]);
        }

        Toast CreateToast()
        {
            var row = Instantiate(template, template.transform.parent, false);
            return new Toast { Row = row, Background = row.GetComponent<Image>() };
        }

        void Recycle(Toast toast)
        {
            active.Remove(toast);
            toast.Row.gameObject.SetActive(false);
            spare.Push(toast);
        }

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].TimeLeft -= Time.unscaledDeltaTime;
                if (active[i].TimeLeft <= 0f) Recycle(active[i]);
            }
        }

        Color KindColor(ToastKind kind)
        {
            if (theme == null) return Color.white;
            switch (kind)
            {
                case ToastKind.Success: return theme.success;
                case ToastKind.Warning: return theme.warning;
                case ToastKind.Danger: return theme.danger;
                default: return theme.info;
            }
        }

        Sprite KindIcon(ToastKind kind)
        {
            if (theme == null) return null;
            switch (kind)
            {
                case ToastKind.Success: return theme.check;
                case ToastKind.Warning: return theme.warningIcon;
                case ToastKind.Danger: return theme.cross;
                default: return theme.infoIcon;
            }
        }

        // Event captions

        void OnHint(HintRequested evt) => Show(evt.Message, ToastKind.Info);

        void OnSelected(ExtinguisherSelected evt)
        {
            var extinguisher = evt.Extinguisher;
            Show(extinguisher.PinPulled
                ? $"Holding the {extinguisher.DisplayName}"
                : $"Picked up the {extinguisher.DisplayName}. Pull the pin.", ToastKind.Info);
        }

        void OnPinPulled(PinPulled evt) => Show("Pin pulled. Aim at the base of the fire and hold SQUEEZE.", ToastKind.Success);

        void OnDischargeBlocked(DischargeBlocked evt)
        {
            if (evt.Reason == DischargeBlockReason.PinIn) Show("Nothing happens: pull the safety pin first", ToastKind.Warning);
            else Show("This extinguisher is empty. Pick up another one.", ToastKind.Warning);
        }

        void OnEmpty(ExtinguisherEmpty evt) => Show($"The {evt.Extinguisher.DisplayName} is empty", ToastKind.Warning);

        void OnAgentApplied(AgentApplied evt)
        {
            if (evt.Effect != AgentEffect.Effective || evt.BurstId == lastHitBurst) return;
            lastHitBurst = evt.BurstId;
            Show(evt.OnBase ? "Good: spraying the base, the fire is shrinking" : "Hitting the flames. Aim lower, at the base.",
                evt.OnBase ? ToastKind.Success : ToastKind.Warning);
        }

        void OnDistanceWarning(DistanceWarning evt)
        {
            if (evt.Band == DistanceBand.TooClose) Show($"Too close ({evt.Distance:0.0} m). Step back to 1 to 2.5 m.", ToastKind.Danger);
            else if (evt.Band == DistanceBand.TooFar) Show($"Too far ({evt.Distance:0.0} m). The spray cannot reach, move closer.", ToastKind.Warning);
        }

        void OnFireIgnited(FireIgnited evt) =>
            Show($"Fire! {Capitalise(evt.Fire.DisplayName)} ({EnumText.FireClassName(evt.Fire.FireClass)})", ToastKind.Danger);

        void OnFireExtinguished(FireExtinguished evt) => Show($"The {evt.Fire.DisplayName} fire is out", ToastKind.Success);

        void OnFireReignited(FireReignited evt) => Show($"The {evt.Fire.DisplayName} fire reignited. {evt.Cause}.", ToastKind.Danger);

        void OnAlarmRaised(AlarmRaised evt) => Show("Alarm raised. Everyone has been warned.", ToastKind.Success);

        void OnPowerIsolated(PowerIsolated evt) => Show("Power switched off at the breaker", ToastKind.Success);

        void OnTaskCompleted(TaskCompleted evt) => Show($"Done: {evt.Task.Description}", ToastKind.Success);

        void OnMistake(MistakeRecorded evt) => Show(evt.Mistake.detail, evt.Critical ? ToastKind.Danger : ToastKind.Warning);

        void OnSessionStarted(SessionStarted evt)
        {
            lastHitBurst = -1;
            Show("Training started. Raise the alarm first.", ToastKind.Info);
        }

        void OnStateChanged(SimulationStateChanged evt)
        {
            if (evt.Current == SimulationStateId.MainMenu || evt.Current == SimulationStateId.Summary) ClearAll();
        }

        static string Capitalise(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
