using System;
using System.Collections.Generic;
using FireReady.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FireReady.Config
{
    [Serializable]
    public class FirePrefabEntry
    {
        public FireClass fireClass;
        public AssetReferenceGameObject prefab;
        public FireProfile profile;
    }

    /// <summary>
    /// Everything that defines one training scenario. Loaded through Addressables after placement,
    /// read-only at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioConfig", menuName = "FireReady/Scenario Config")]
    public class ScenarioConfig : ScriptableObject
    {
        public string scenarioId = "workshop_office";
        public string displayName = "Workshop office fire";

        [Header("Content")]
        public AssetReferenceGameObject layoutPrefab;
        public AssetReferenceGameObject extinguisherPrefab;
        public List<FirePrefabEntry> firePrefabs = new List<FirePrefabEntry>();
        [Tooltip("Addressables label of the pooled effect prefabs.")]
        public string effectsLabel = "effects";

        [Header("Schedule (FR-08)")]
        public List<FireSpawnData> fires = new List<FireSpawnData>();

        [Header("Equipment")]
        public List<ExtinguisherConfig> extinguishers = new List<ExtinguisherConfig>();

        [Header("Rules")]
        public SimulationRules rules = new SimulationRules();
        public ScoringRules scoring;
        public AudioCueTable audioCues;

        public FirePrefabEntry GetFirePrefab(FireClass fireClass)
        {
            return firePrefabs.Find(entry => entry.fireClass == fireClass);
        }
    }
}
