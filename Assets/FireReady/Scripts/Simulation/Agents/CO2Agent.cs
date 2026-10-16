using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    [CreateAssetMenu(fileName = "CO2Agent", menuName = "FireReady/Agents/CO2")]
    public class CO2Agent : ExtinguishingAgent
    {
        public override AgentType Type => AgentType.CO2;

        public override AgentEffect Evaluate(FireClass fireClass, bool powerLive)
        {
            switch (fireClass)
            {
                case FireClass.ClassA:
                    // Knocks the flames down but does not cool the embers, so paper can reignite.
                    return AgentEffect.Weak;
                case FireClass.ClassB:
                case FireClass.Electrical:
                    return AgentEffect.Effective;
                default:
                    return AgentEffect.Weak;
            }
        }
    }
}
