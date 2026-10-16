using System;
using System.Collections.Generic;
using FireReady.Config;
using FireReady.Core;
using FireReady.Data;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Records mistakes, aim accuracy, agent use and timings during a session, works out the
    /// score from the ScoringRules (SRS table 2.2) and builds the session record at the end.
    /// </summary>
    public class PerformanceTracker : MonoBehaviour
    {
        public static class Codes
        {
            public const string WeakAgent = "WEAK_AGENT";
            public const string DangerousAgent = "DANGEROUS_AGENT";
            public const string FatalAgent = "FATAL_AGENT";
            public const string NoAlarm = "NO_ALARM";
            public const string LiveElectrical = "LIVE_ELECTRICAL";
            public const string LowAim = "LOW_AIM";
            public const string TooClose = "TOO_CLOSE";
            public const string EmptyWithFire = "EMPTY_WITH_FIRE";
            public const string OutOfControl = "STAYED_WITH_FIRE";
        }

        ScoringRules scoring;
        Func<float> clock = () => 0f;
        string scenarioId;
        DateTime startedUtc;
        bool active;

        readonly List<MistakeEntry> mistakes = new List<MistakeEntry>();
        readonly Dictionary<AgentType, float> agentSeconds = new Dictionary<AgentType, float>();
        readonly HashSet<FireSource> weakAgentFires = new HashSet<FireSource>();
        readonly Dictionary<FireSource, int> lastDangerousBurst = new Dictionary<FireSource, int>();
        readonly HashSet<Extinguisher> emptiedWithFire = new HashSet<Extinguisher>();
        readonly HashSet<FireSource> burningFires = new HashSet<FireSource>();

        float sprayOnFireTime;
        float sprayOnBaseTime;
        float sprayMissedTime;
        int dangerousUses;
        int tooCloseEvents;
        bool alarmRaised;
        float alarmRaisedAt = -1f;
        bool powerIsolated;
        bool alarmMistake;
        bool liveElectricalMistake;

        public IReadOnlyList<MistakeEntry> Mistakes => mistakes;
        public bool AlarmRaisedFlag => alarmRaised;
        public bool PowerIsolatedFlag => powerIsolated;
        public int DangerousUses => dangerousUses;
        public int WeakUses => weakAgentFires.Count;

        public float AimAccuracy
        {
            get
            {
                float total = sprayOnFireTime + sprayMissedTime;
                return total > 0f ? sprayOnBaseTime / total : 0f;
            }
        }

        bool HasSprayed => sprayOnFireTime + sprayMissedTime > 0.25f;

        void OnEnable()
        {
            EventBus.Subscribe<AgentApplied>(OnAgentApplied);
            EventBus.Subscribe<SprayMissed>(OnSprayMissed);
            EventBus.Subscribe<SprayStopped>(OnSprayStopped);
            EventBus.Subscribe<FireIgnited>(OnFireIgnited);
            EventBus.Subscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Subscribe<FireReignited>(OnFireReignited);
            EventBus.Subscribe<FireBeyondControl>(OnFireBeyondControl);
            EventBus.Subscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Subscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Subscribe<TooCloseRecorded>(OnTooClose);
            EventBus.Subscribe<ExtinguisherEmpty>(OnExtinguisherEmpty);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<AgentApplied>(OnAgentApplied);
            EventBus.Unsubscribe<SprayMissed>(OnSprayMissed);
            EventBus.Unsubscribe<SprayStopped>(OnSprayStopped);
            EventBus.Unsubscribe<FireIgnited>(OnFireIgnited);
            EventBus.Unsubscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Unsubscribe<FireReignited>(OnFireReignited);
            EventBus.Unsubscribe<FireBeyondControl>(OnFireBeyondControl);
            EventBus.Unsubscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Unsubscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Unsubscribe<TooCloseRecorded>(OnTooClose);
            EventBus.Unsubscribe<ExtinguisherEmpty>(OnExtinguisherEmpty);
        }

        public void BeginSession(ScenarioConfig config, Func<float> sessionClock)
        {
            scoring = config.scoring != null ? config.scoring : ScriptableObject.CreateInstance<ScoringRules>();
            scenarioId = config.scenarioId;
            clock = sessionClock ?? (() => 0f);
            startedUtc = DateTime.UtcNow;

            mistakes.Clear();
            agentSeconds.Clear();
            weakAgentFires.Clear();
            lastDangerousBurst.Clear();
            emptiedWithFire.Clear();
            burningFires.Clear();
            sprayOnFireTime = 0f;
            sprayOnBaseTime = 0f;
            sprayMissedTime = 0f;
            dangerousUses = 0;
            tooCloseEvents = 0;
            alarmRaised = false;
            alarmRaisedAt = -1f;
            powerIsolated = false;
            alarmMistake = false;
            liveElectricalMistake = false;

            active = true;
            PublishPerformance();
        }

        public void EndSession()
        {
            active = false;
        }

        void AddMistake(string code, string detail, bool critical = false)
        {
            var entry = new MistakeEntry(clock(), code, detail);
            mistakes.Add(entry);
            EventBus.Publish(new MistakeRecorded { Mistake = entry, Critical = critical });
            PublishPerformance();
        }

        void PublishPerformance()
        {
            EventBus.Publish(new PerformanceUpdated
            {
                Score = CalculateScore(null, includeEndOfSession: false),
                AimAccuracy = AimAccuracy,
                MistakeCount = mistakes.Count
            });
        }

        public int CalculateScore(SessionOutcome? outcome, bool includeEndOfSession = true)
        {
            return ScoreCalculator.Calculate(scoring, CurrentInputs(), outcome, includeEndOfSession);
        }

        ScoreInputs CurrentInputs()
        {
            return new ScoreInputs
            {
                WeakAgentFires = weakAgentFires.Count,
                DangerousUses = dangerousUses,
                TooCloseEvents = tooCloseEvents,
                EmptiedWithFire = emptiedWithFire.Count,
                LiveElectricalMistake = liveElectricalMistake,
                AlarmMistake = alarmMistake,
                AlarmRaised = alarmRaised,
                HasSprayed = HasSprayed,
                AimAccuracy = AimAccuracy
            };
        }

        public SessionRecord BuildRecord(SessionOutcome outcome, string reason, float duration, int firesTotal, int firesExtinguished)
        {
            // Mistakes that can only be judged once the session is over.
            if (!alarmRaised && !alarmMistake)
            {
                alarmMistake = true;
                mistakes.Add(new MistakeEntry(duration, Codes.NoAlarm, "The fire alarm was never raised"));
            }
            if (HasSprayed && AimAccuracy < scoring.aimAccuracyThreshold)
            {
                mistakes.Add(new MistakeEntry(duration, Codes.LowAim,
                    $"Aim accuracy {AimAccuracy:P0} is below {scoring.aimAccuracyThreshold:P0}. Aim at the base of the fire"));
            }

            var record = new SessionRecord
            {
                sessionId = Guid.NewGuid().ToString(),
                scenarioId = scenarioId,
                startedAtUtc = startedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                durationSeconds = Mathf.Round(duration * 10f) / 10f,
                Outcome = outcome,
                outcomeReason = reason,
                score = CalculateScore(outcome),
                firesTotal = firesTotal,
                firesExtinguished = firesExtinguished,
                weakAgentUses = weakAgentFires.Count,
                dangerousAgentUses = dangerousUses,
                aimAccuracy = Mathf.Round(AimAccuracy * 100f) / 100f,
                alarmRaised = alarmRaised,
                alarmRaisedAtSeconds = alarmRaisedAt,
                powerIsolated = powerIsolated,
                tooCloseEvents = tooCloseEvents,
                appVersion = Application.version
            };

            foreach (var pair in agentSeconds)
                record.agentUsage.Add(new AgentUsage { agent = pair.Key.ToString(), seconds = Mathf.Round(pair.Value * 10f) / 10f });

            mistakes.Sort((a, b) => a.atSeconds.CompareTo(b.atSeconds));
            record.mistakes.AddRange(mistakes);

            active = false;
            return record;
        }

        // Event handlers

        void OnAgentApplied(AgentApplied evt)
        {
            if (!active) return;

            sprayOnFireTime += evt.DeltaTime;
            if (evt.OnBase) sprayOnBaseTime += evt.DeltaTime;

            var fire = evt.Fire;
            string agentName = EnumText.AgentName(evt.Agent);
            string fireName = EnumText.FireClassName(fire.FireClass);

            if (fire.FireClass == FireClass.Electrical && fire.IsPowerLive && !liveElectricalMistake)
            {
                liveElectricalMistake = true;
                AddMistake(Codes.LiveElectrical, "Tackled the electrical fire before switching off the power");
            }

            switch (evt.Effect)
            {
                case AgentEffect.Weak:
                    if (weakAgentFires.Add(fire))
                        AddMistake(Codes.WeakAgent, $"{agentName} on {fireName} fire: knocks the flames down but it can reignite");
                    break;

                case AgentEffect.Dangerous:
                case AgentEffect.Fatal:
                    // One use is one squeeze of the handle, not one frame.
                    if (lastDangerousBurst.TryGetValue(fire, out int burst) && burst == evt.BurstId) break;
                    lastDangerousBurst[fire] = evt.BurstId;
                    dangerousUses++;

                    if (evt.Effect == AgentEffect.Fatal)
                        AddMistake(Codes.FatalAgent, $"{agentName} on a live electrical fire: electric shock", true);
                    else
                        AddMistake(Codes.DangerousAgent, DangerousDetail(evt.Agent, fire), true);
                    break;
            }
        }

        static string DangerousDetail(AgentType agent, FireSource fire)
        {
            if (fire.FireClass == FireClass.ClassB && agent == AgentType.Water)
                return "Water on burning fuel: it spread the fire";
            if (fire.FireClass == FireClass.Electrical)
                return $"{EnumText.AgentName(agent)} on a live electrical fire: conducts electricity";
            return $"{EnumText.AgentName(agent)} made the {EnumText.FireClassName(fire.FireClass)} fire worse";
        }

        void OnSprayMissed(SprayMissed evt)
        {
            if (active) sprayMissedTime += evt.DeltaTime;
        }

        void OnSprayStopped(SprayStopped evt)
        {
            if (!active || evt.Extinguisher == null) return;
            var type = evt.Extinguisher.Type;
            agentSeconds.TryGetValue(type, out float seconds);
            agentSeconds[type] = seconds + evt.Duration;
        }

        void OnFireIgnited(FireIgnited evt)
        {
            if (active) burningFires.Add(evt.Fire);
        }

        void OnFireReignited(FireReignited evt)
        {
            if (active) burningFires.Add(evt.Fire);
        }

        void OnFireExtinguished(FireExtinguished evt)
        {
            if (!active) return;
            burningFires.Remove(evt.Fire);

            if (!alarmRaised && !alarmMistake)
            {
                alarmMistake = true;
                AddMistake(Codes.NoAlarm, "Put out a fire before raising the alarm");
            }
        }

        void OnFireBeyondControl(FireBeyondControl evt)
        {
            if (active) AddMistake(Codes.OutOfControl, $"Stayed with the out-of-control {evt.Fire.DisplayName} fire instead of evacuating", true);
        }

        void OnAlarmRaised(AlarmRaised evt)
        {
            if (!active || alarmRaised) return;
            alarmRaised = true;
            alarmRaisedAt = Mathf.Round(clock() * 10f) / 10f;
        }

        void OnPowerIsolated(PowerIsolated evt)
        {
            if (active) powerIsolated = true;
        }

        void OnTooClose(TooCloseRecorded evt)
        {
            if (!active) return;
            tooCloseEvents++;
            AddMistake(Codes.TooClose, $"Stood {evt.Distance:0.0} m from a burning fire for too long");
        }

        void OnExtinguisherEmpty(ExtinguisherEmpty evt)
        {
            if (!active || burningFires.Count == 0) return;
            if (emptiedWithFire.Add(evt.Extinguisher))
                AddMistake(Codes.EmptyWithFire, $"{evt.Extinguisher.DisplayName} ran empty with a fire still burning");
        }
    }
}
