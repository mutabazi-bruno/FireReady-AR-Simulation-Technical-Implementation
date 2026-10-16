using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Data
{
    /// <summary>
    /// The only class that reads and writes session history. The file is read once at startup,
    /// so the history screen never touches the disk.
    /// </summary>
    public class SessionRepository
    {
        readonly IDataStorage storage;
        readonly int maxSessions;
        SessionHistory history = new SessionHistory();

        public int Count => history.sessions.Count;
        public bool RecoveredFromCorruptFile { get; private set; }

        public SessionRepository(IDataStorage storage, int maxSessions = 50)
        {
            this.storage = storage;
            this.maxSessions = Mathf.Max(1, maxSessions);
        }

        public void Load()
        {
            history = new SessionHistory();
            RecoveredFromCorruptFile = false;
            if (!storage.Exists()) return;

            try
            {
                var json = storage.Read();
                var loaded = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SessionHistory>(json);
                if (loaded == null || loaded.sessions == null) throw new FormatException("History file is empty or invalid.");

                history = Upgrade(loaded);
            }
            catch (Exception e)
            {
                // Keep the broken file for inspection and carry on with an empty history.
                Debug.LogWarning($"Session history could not be read, starting empty. {e.Message}");
                storage.Backup();
                history = new SessionHistory();
                RecoveredFromCorruptFile = true;
            }
        }

        // Later versions of the record format are upgraded here using schemaVersion.
        static SessionHistory Upgrade(SessionHistory loaded)
        {
            if (loaded.schemaVersion < SessionHistory.CurrentSchemaVersion)
                loaded.schemaVersion = SessionHistory.CurrentSchemaVersion;

            loaded.sessions.RemoveAll(record => record == null);
            return loaded;
        }

        public void Save(SessionRecord record)
        {
            if (record == null) return;

            history.sessions.Insert(0, record);
            if (history.sessions.Count > maxSessions)
                history.sessions.RemoveRange(maxSessions, history.sessions.Count - maxSessions);

            storage.Write(JsonUtility.ToJson(history, true));
        }

        public List<SessionRecord> LoadRecent(int count)
        {
            int take = Mathf.Clamp(count, 0, history.sessions.Count);
            return history.sessions.GetRange(0, take);
        }

        public void Clear()
        {
            history = new SessionHistory();
            storage.Write(JsonUtility.ToJson(history, true));
        }
    }
}
