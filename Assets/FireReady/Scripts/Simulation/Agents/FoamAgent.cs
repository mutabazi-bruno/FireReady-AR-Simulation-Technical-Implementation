using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    [CreateAssetMenu(fileName = "FoamAgent", menuName = "FireReady/Agents/Foam")]
    public class FoamAgent : ExtinguishingAgent
    {
        public override AgentType Type => AgentType.Foam;

        public override AgentEffect Evaluate(FireClass fireClass, bool powerLive)
        {
            switch (fireClass)
            {
                case FireClass.ClassA:
                case FireClass.ClassB:
                    return AgentEffect.Effective;
                case FireClass.Electrical:
                    // Foam is water based, so it conducts on a live circuit.
                    return powerLive ? AgentEffect.Dangerous : AgentEffect.Effective;
                default:
                    return AgentEffect.Weak;
            }
        }
    }
}
