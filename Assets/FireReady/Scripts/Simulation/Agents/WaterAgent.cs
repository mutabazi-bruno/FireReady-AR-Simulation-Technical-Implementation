using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    [CreateAssetMenu(fileName = "WaterAgent", menuName = "FireReady/Agents/Water")]
    public class WaterAgent : ExtinguishingAgent
    {
        public override AgentType Type => AgentType.Water;

        public override AgentEffect Evaluate(FireClass fireClass, bool powerLive)
        {
            switch (fireClass)
            {
                case FireClass.ClassA:
                    return AgentEffect.Effective;
                case FireClass.ClassB:
                    // Water sinks under burning fuel, boils and throws it out.
                    return AgentEffect.Dangerous;
                case FireClass.Electrical:
                    // Conducts electricity back to the user. Once isolated it is an ordinary fire.
                    return powerLive ? AgentEffect.Fatal : AgentEffect.Effective;
                default:
                    return AgentEffect.Weak;
            }
        }
    }
}
