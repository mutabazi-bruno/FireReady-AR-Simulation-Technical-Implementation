using FireReady.Simulation;
using UnityEngine;

namespace FireReady.Config
{
    [CreateAssetMenu(fileName = "ExtinguisherConfig", menuName = "FireReady/Extinguisher Config")]
    public class ExtinguisherConfig : ScriptableObject
    {
        public string displayName = "Water";

        [Tooltip("Strategy that decides how this agent reacts to each fire class.")]
        public ExtinguishingAgent agent;

        [Header("Discharge (FR-13)")]
        public float capacitySeconds = 12f;
        [Tooltip("Intensity removed per second when the spray hits the base of a fire.")]
        public float suppressionRate = 11f;

        [Header("Look")]
        public Color bandColor = Color.red;
        public Color sprayColor = Color.white;
        [Tooltip("Spray cone angle in degrees. CO2 and powder spread wider than water.")]
        public float sprayAngle = 8f;
        public Sprite icon;

        [Header("Briefing")]
        [TextArea] public string useFor = "";
        [TextArea] public string neverUseOn = "";
    }
}
