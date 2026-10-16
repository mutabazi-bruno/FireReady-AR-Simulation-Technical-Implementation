using FireReady.Core;

namespace FireReady.Simulation
{
    // Strategy: each agent decides for itself how it reacts to a class of fire.
    public interface IExtinguishingAgent
    {
        AgentType Type { get; }
        AgentEffect Evaluate(FireClass fireClass, bool powerLive);
    }
}
