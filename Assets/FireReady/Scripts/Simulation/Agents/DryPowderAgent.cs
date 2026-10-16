using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    [CreateAssetMenu(fileName = "DryPowderAgent", menuName = "FireReady/Agents/Dry Powder")]
    public class DryPowderAgent : ExtinguishingAgent
    {
        public override AgentType Type => AgentType.DryPowder;

        // ABC powder works on all three classes in this scenario.
        public override AgentEffect Evaluate(FireClass fireClass, bool powerLive)
        {
            return AgentEffect.Effective;
        }
    }
}
