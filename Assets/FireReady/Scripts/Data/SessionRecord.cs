using System;
using System.Collections.Generic;
using FireReady.Core;

namespace FireReady.Data
{
    [Serializable]
    public class MistakeEntry
    {
        public float atSeconds;
        public string code;
        public string detail;

        public MistakeEntry() { }

        public MistakeEntry(float atSeconds, string code, string detail)
        {
            this.atSeconds = atSeconds;
            this.code = code;
            this.detail = detail;
        }
    }

    [Serializable]
    public class AgentUsage
    {
        public string agent;
        public float seconds;
    }

    /// <summary>
    /// The result of one session, as stored in sessions.json (DMP section 6.2).
    /// Outcome is kept as a string because JsonUtility writes enums as numbers.
    /// </summary>
    [Serializable]
    public class SessionRecord
    {
        public string sessionId;
        public string scenarioId;
        public string startedAtUtc;
        public float durationSeconds;
        public string outcome;
        public string outcomeReason;
        public int score;
        public int firesTotal;
        public int firesExtinguished;
        public int weakAgentUses;
        public int dangerousAgentUses;
        public float aimAccuracy;
        public bool alarmRaised;
        public float alarmRaisedAtSeconds = -1f;
        public bool powerIsolated;
        public int tooCloseEvents;
        public List<AgentUsage> agentUsage = new List<AgentUsage>();
        public List<MistakeEntry> mistakes = new List<MistakeEntry>();
        public string appVersion;

        public SessionOutcome Outcome
        {
            get => Enum.TryParse(outcome, out SessionOutcome parsed) ? parsed : SessionOutcome.Abandoned;
            set => outcome = value.ToString();
        }

        public int WrongAgentUses => weakAgentUses + dangerousAgentUses;

        public DateTime StartedLocal
        {
            get
            {
                if (DateTime.TryParse(startedAtUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var utc))
                    return utc.ToLocalTime();
                return DateTime.MinValue;
            }
        }
    }

    [Serializable]
    public class SessionHistory
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<SessionRecord> sessions = new List<SessionRecord>();
    }
}
