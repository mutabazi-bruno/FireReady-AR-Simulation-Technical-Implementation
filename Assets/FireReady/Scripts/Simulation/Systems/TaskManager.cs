using System;
using System.Collections.Generic;
using FireReady.Config;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Tracks the procedure tasks (raise the alarm, isolate the power, put out each fire)
    /// and works out what the trainee should do next. Driven entirely by events.
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public const string AlarmTaskId = "alarm";
        public const string PowerTaskId = "power";

        readonly List<TrainingTask> tasks = new List<TrainingTask>();
        Func<float> clock = () => 0f;
        bool active;

        public IReadOnlyList<TrainingTask> Tasks => tasks;
        public TrainingTask CurrentTask { get; private set; }

        public int CompletedCount
        {
            get
            {
                int count = 0;
                foreach (var task in tasks) if (task.IsComplete) count++;
                return count;
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Subscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Subscribe<FireIgnited>(OnFireIgnited);
            EventBus.Subscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Subscribe<FireReignited>(OnFireReignited);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<AlarmRaised>(OnAlarmRaised);
            EventBus.Unsubscribe<PowerIsolated>(OnPowerIsolated);
            EventBus.Unsubscribe<FireIgnited>(OnFireIgnited);
            EventBus.Unsubscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Unsubscribe<FireReignited>(OnFireReignited);
        }

        public void BeginSession(ScenarioConfig config, Func<float> sessionClock)
        {
            clock = sessionClock ?? (() => 0f);
            tasks.Clear();

            tasks.Add(new TrainingTask(AlarmTaskId, "Raise the fire alarm", true, true));

            var schedule = new List<FireSpawnData>(config.fires);
            schedule.Sort((a, b) => a.ignitionTime.CompareTo(b.ignitionTime));

            bool powerTaskAdded = false;
            foreach (var spawn in schedule)
            {
                if (spawn.fireClass == FireClass.Electrical && !powerTaskAdded)
                {
                    tasks.Add(new TrainingTask(PowerTaskId, "Switch off the power at the breaker", true, false));
                    powerTaskAdded = true;
                }
                tasks.Add(new TrainingTask(FireTaskId(spawn.id), FireTaskText(spawn.displayName, spawn.fireClass), true, false, spawn.id));
            }

            active = true;
            Refresh();
        }

        public void EndSession()
        {
            active = false;
        }

        static string FireTaskId(string fireId) => $"fire_{fireId}";

        static string FireTaskText(string displayName, FireClass fireClass)
        {
            return $"Put out the {displayName} fire ({EnumText.FireClassName(fireClass)})";
        }

        TrainingTask Find(string id) => tasks.Find(task => task.Id == id);

        void Complete(string id)
        {
            var task = Find(id);
            if (task == null || task.IsComplete) return;
            task.MarkComplete(clock());
            EventBus.Publish(new TaskCompleted { Task = task });
            Refresh();
        }

        void Refresh()
        {
            CurrentTask = null;
            foreach (var task in tasks)
            {
                if (task.IsAvailable && !task.IsComplete)
                {
                    CurrentTask = task;
                    break;
                }
            }
            EventBus.Publish(new TasksChanged());
        }

        void OnAlarmRaised(AlarmRaised evt)
        {
            if (active) Complete(AlarmTaskId);
        }

        void OnPowerIsolated(PowerIsolated evt)
        {
            if (active) Complete(PowerTaskId);
        }

        void OnFireIgnited(FireIgnited evt)
        {
            if (!active) return;

            var fire = evt.Fire;
            var task = Find(FireTaskId(fire.EntityId));
            if (task == null)
            {
                // A spread fire was not in the schedule, so it gets its own task.
                task = new TrainingTask(FireTaskId(fire.EntityId), FireTaskText(fire.DisplayName, fire.FireClass), true, true, fire.EntityId);
                tasks.Add(task);
            }
            task.Unlock();

            if (fire.FireClass == FireClass.Electrical) Find(PowerTaskId)?.Unlock();
            Refresh();
        }

        void OnFireExtinguished(FireExtinguished evt)
        {
            if (active) Complete(FireTaskId(evt.Fire.EntityId));
        }

        void OnFireReignited(FireReignited evt)
        {
            if (!active) return;
            var task = Find(FireTaskId(evt.Fire.EntityId));
            if (task == null) return;
            task.Reopen();
            Refresh();
        }
    }
}
