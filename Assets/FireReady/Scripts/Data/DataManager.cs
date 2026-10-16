using System.Collections.Generic;
using System.IO;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Data
{
    public class DataManager : MonoBehaviour
    {
        [SerializeField] string fileName = "sessions.json";
        [SerializeField] int maxSessions = 50;

        SessionRepository repository;

        public SettingsStore Settings { get; private set; }
        public string StoragePath { get; private set; }
        public int SessionCount => repository?.Count ?? 0;
        public bool RecoveredFromCorruptFile => repository != null && repository.RecoveredFromCorruptFile;

        void Awake()
        {
            Settings = new SettingsStore();
            StoragePath = Path.Combine(Application.persistentDataPath, fileName);
            repository = new SessionRepository(new JsonFileStorage(StoragePath), maxSessions);
            repository.Load();
        }

        public void Save(SessionRecord record)
        {
            try
            {
                repository.Save(record);
                EventBus.Publish(new SessionSaved { Record = record });
            }
            catch (IOException e)
            {
                Debug.LogError($"Could not save the session: {e.Message}");
            }
        }

        public List<SessionRecord> LoadRecent(int count) => repository.LoadRecent(count);

        public void Clear()
        {
            repository.Clear();
            EventBus.Publish(new HistoryCleared());
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Settings?.Save();
        }

        void OnApplicationQuit()
        {
            Settings?.Save();
        }
    }
}
