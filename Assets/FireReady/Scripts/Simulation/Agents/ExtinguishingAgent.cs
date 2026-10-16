using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Agents are assets so an ExtinguisherConfig can swap its strategy in the inspector.
    /// Adding a new agent means a new subclass and a new asset, nothing else changes.
    /// </summary>
    public abstract class ExtinguishingAgent : ScriptableObject, IExtinguishingAgent
    {
        public abstract AgentType Type { get; }
        public abstract AgentEffect Evaluate(FireClass fireClass, bool powerLive);
    }
}
