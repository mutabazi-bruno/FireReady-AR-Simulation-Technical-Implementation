using System;
using System.Collections.Generic;
using FireReady.Config;
using FireReady.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FireReady.Simulation
{
    /// <summary>
    /// Creates the right FireSource subclass for a spawn entry. The EntityManager never touches
    /// prefabs or Addressables keys, and the fuel fire uses the same factory when it spreads.
    /// </summary>
    public class FireFactory
    {
        readonly struct FireTemplate
        {
            public readonly FireSource Prefab;
            public readonly FireProfile Profile;

            public FireTemplate(FireSource prefab, FireProfile profile)
            {
                Prefab = prefab;
                Profile = profile;
            }
        }

        readonly Dictionary<FireClass, FireTemplate> templates = new Dictionary<FireClass, FireTemplate>();
        readonly SimulationRules rules;
        readonly Transform parent;
        int spreadCount;

        public event Action<FireSource> Created;

        public FireFactory(SimulationRules rules, Transform parent)
        {
            this.rules = rules;
            this.parent = parent;
        }

        public void Register(FireClass fireClass, GameObject prefab, FireProfile profile)
        {
            var fire = prefab != null ? prefab.GetComponent<FireSource>() : null;
            if (fire == null)
            {
                Debug.LogError($"Fire prefab for {fireClass} has no FireSource component.");
                return;
            }
            if (fire.FireClass != fireClass)
                Debug.LogWarning($"Fire prefab {prefab.name} is {fire.FireClass} but registered as {fireClass}.");

            templates[fireClass] = new FireTemplate(fire, profile);
        }

        public bool CanCreate(FireClass fireClass) => templates.ContainsKey(fireClass);

        public FireSource Create(FireSpawnData spawn, Vector3 position, Quaternion rotation, bool secondary = false)
        {
            if (!templates.TryGetValue(spawn.fireClass, out var template))
            {
                Debug.LogError($"No fire prefab registered for {spawn.fireClass}.");
                return null;
            }

            var fire = Object.Instantiate(template.Prefab, position, rotation, parent);
            fire.name = $"Fire_{spawn.id}";
            fire.Configure(template.Profile, rules, spawn, secondary);
            fire.ResetEntity();

            if (fire is FlammableLiquidFire liquid) liquid.SetFactory(this);

            Created?.Invoke(fire);
            return fire;
        }

        public FireSource CreateSpread(FireSource origin, Vector3 position)
        {
            spreadCount++;
            var spawn = new FireSpawnData
            {
                id = $"{origin.Spawn.id}_spread{spreadCount}",
                displayName = "spreading fuel",
                fireClass = origin.FireClass,
                spawnPointId = origin.Spawn.spawnPointId,
                ignitionTime = 0f
            };

            position.y = origin.transform.position.y;
            var fire = Create(spawn, position, origin.transform.rotation, secondary: true);
            if (fire != null) fire.Ignite();
            return fire;
        }

        public void ResetSpreadCount()
        {
            spreadCount = 0;
        }
    }
}
