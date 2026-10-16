using System.Collections.Generic;
using FireReady.Config;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Content
{
    // Plain data handed from the AssetManager to the systems that build the scene.
    public class LoadedScenario
    {
        public ScenarioConfig Config;
        public GameObject LayoutPrefab;
        public GameObject ExtinguisherPrefab;
        public readonly Dictionary<FireClass, GameObject> FirePrefabs = new Dictionary<FireClass, GameObject>();
        public readonly List<GameObject> EffectPrefabs = new List<GameObject>();
    }
}
