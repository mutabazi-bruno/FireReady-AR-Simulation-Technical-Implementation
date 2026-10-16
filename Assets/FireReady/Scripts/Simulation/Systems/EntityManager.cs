using System.Collections.Generic;
using FireReady.Config;
using FireReady.Content;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Builds the scene from the scenario config, runs the ignition schedule and ticks the fires
    /// while the simulation is running.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        readonly List<FireSource> fires = new List<FireSource>();
        readonly List<SimulationEntity> entities = new List<SimulationEntity>();
        readonly List<FireSpawnData> pendingIgnitions = new List<FireSpawnData>();
        readonly Dictionary<string, SpawnPoint> spawnPoints = new Dictionary<string, SpawnPoint>();

        ScenarioConfig config;
        FireFactory factory;
        GameObject layout;

        public IReadOnlyList<FireSource> Fires => fires;
        public IReadOnlyList<SimulationEntity> Entities => entities;
        public bool HasScene => layout != null;
        public bool AllScheduledIgnited => pendingIgnitions.Count == 0;
        public int TotalFires => fires.Count;

        public int ActiveFireCount
        {
            get
            {
                int count = 0;
                foreach (var fire in fires) if (fire.IsBurning) count++;
                return count;
            }
        }

        // Fires still to deal with: burning, smouldering or not yet ignited.
        public int FiresRemaining
        {
            get
            {
                int count = pendingIgnitions.Count;
                foreach (var fire in fires)
                    if (fire.IsBurning || fire.HasPendingReignite) count++;
                return count;
            }
        }

        public int FiresExtinguished
        {
            get
            {
                int count = 0;
                foreach (var fire in fires)
                    if (fire.State == FireState.Extinguished && !fire.HasPendingReignite) count++;
                return count;
            }
        }

        // FR-25: every fire is out and none is waiting to reignite.
        public bool AllFiresSafelyOut
        {
            get
            {
                if (pendingIgnitions.Count > 0 || fires.Count == 0) return false;
                foreach (var fire in fires)
                    if (fire.State != FireState.Extinguished || fire.HasPendingReignite) return false;
                return true;
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<PowerIsolated>(OnPowerIsolated);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PowerIsolated>(OnPowerIsolated);
        }

        public void BuildScene(Transform root, LoadedScenario scenario)
        {
            ClearScene();
            config = scenario.Config;

            layout = Instantiate(scenario.LayoutPrefab, root, false);
            layout.name = "ScenarioLayout";

            spawnPoints.Clear();
            foreach (var point in layout.GetComponentsInChildren<SpawnPoint>(true))
                spawnPoints[point.Id] = point;

            SpawnExtinguishers(scenario);

            factory = new FireFactory(config.rules, layout.transform);
            factory.Created += RegisterFire;
            foreach (var entry in config.firePrefabs)
            {
                if (scenario.FirePrefabs.TryGetValue(entry.fireClass, out var prefab))
                    factory.Register(entry.fireClass, prefab, entry.profile);
            }

            foreach (var spawn in config.fires)
            {
                if (!spawnPoints.TryGetValue(spawn.spawnPointId, out var point))
                {
                    Debug.LogError($"Spawn point '{spawn.spawnPointId}' for fire '{spawn.id}' is missing in the layout.");
                    continue;
                }
                factory.Create(spawn, point.transform.position, point.transform.rotation);
            }

            entities.Clear();
            layout.GetComponentsInChildren(true, entities);
            foreach (var entity in entities) entity.Initialise(config);
        }

        void SpawnExtinguishers(LoadedScenario scenario)
        {
            var stand = layout.GetComponentInChildren<ExtinguisherStand>(true);
            if (stand == null || scenario.ExtinguisherPrefab == null)
            {
                Debug.LogError("Layout needs an ExtinguisherStand and the scenario needs an extinguisher prefab.");
                return;
            }

            for (int i = 0; i < config.extinguishers.Count; i++)
            {
                var slot = stand.GetSlot(i);
                var instance = Instantiate(scenario.ExtinguisherPrefab, slot, false);
                var extinguisher = instance.GetComponent<Extinguisher>();
                extinguisher.Setup(config.extinguishers[i], slot);
            }
        }

        void RegisterFire(FireSource fire)
        {
            fires.Add(fire);
            if (!entities.Contains(fire)) entities.Add(fire);
        }

        public void BeginSchedule()
        {
            pendingIgnitions.Clear();
            pendingIgnitions.AddRange(config.fires);
            pendingIgnitions.Sort((a, b) => a.ignitionTime.CompareTo(b.ignitionTime));
        }

        public void Tick(float dt, float sessionTime)
        {
            while (pendingIgnitions.Count > 0 && pendingIgnitions[0].ignitionTime <= sessionTime)
            {
                var spawn = pendingIgnitions[0];
                pendingIgnitions.RemoveAt(0);
                var fire = fires.Find(f => f.Spawn == spawn);
                if (fire != null) fire.Ignite();
            }

            // Index loop: a fuel fire can spread and add to the list during a tick.
            for (int i = 0; i < fires.Count; i++) fires[i].Tick(dt);
        }

        // Restart in the same placed scene: remove spread fires and reset everything else.
        public void ResetScenario()
        {
            for (int i = fires.Count - 1; i >= 0; i--)
            {
                if (!fires[i].IsSecondary) continue;
                entities.Remove(fires[i]);
                Destroy(fires[i].gameObject);
                fires.RemoveAt(i);
            }

            foreach (var entity in entities) entity.ResetEntity();
            factory?.ResetSpreadCount();
            pendingIgnitions.Clear();
        }

        public void ClearScene()
        {
            if (factory != null) factory.Created -= RegisterFire;
            factory = null;

            // A held extinguisher lives under the camera, so it would not go with the layout.
            foreach (var entity in entities)
            {
                if (entity != null && layout != null && !entity.transform.IsChildOf(layout.transform))
                    Destroy(entity.gameObject);
            }

            fires.Clear();
            entities.Clear();
            pendingIgnitions.Clear();
            spawnPoints.Clear();
            if (layout != null) Destroy(layout);
            layout = null;
        }

        public FireSource NearestBurningFire(Vector3 position, out float horizontalDistance)
        {
            FireSource nearest = null;
            horizontalDistance = float.MaxValue;
            foreach (var fire in fires)
            {
                if (!fire.IsBurning) continue;
                float distance = HorizontalDistance(position, fire.transform.position);
                if (distance < horizontalDistance)
                {
                    horizontalDistance = distance;
                    nearest = fire;
                }
            }
            return nearest;
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void OnPowerIsolated(PowerIsolated evt)
        {
            foreach (var fire in fires)
                if (fire is ElectricalFire electrical) electrical.OnPowerIsolated();
        }
    }
}
