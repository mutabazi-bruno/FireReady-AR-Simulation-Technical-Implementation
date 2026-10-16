using System;
using FireReady.Core;

namespace FireReady.Config
{
    // One entry in the scenario's ignition schedule.
    [Serializable]
    public class FireSpawnData
    {
        public string id = "fire";
        public string displayName = "fire";
        public FireClass fireClass = FireClass.ClassA;
        public string spawnPointId = "";
        public float ignitionTime;
    }
}
