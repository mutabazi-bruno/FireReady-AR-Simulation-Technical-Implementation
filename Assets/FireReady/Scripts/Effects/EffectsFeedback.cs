using FireReady.Core;
using UnityEngine;

namespace FireReady.Effects
{
    /// <summary>
    /// Spawns pooled one-off effects in response to simulation events: steam when a fire goes out,
    /// smoke when one reignites and a fuel splash when the wrong agent hits burning fuel.
    /// </summary>
    public class EffectsFeedback : MonoBehaviour
    {
        [SerializeField] PoolManager pools;
        [SerializeField] float splashInterval = 0.25f;

        float lastSplash;

        void OnEnable()
        {
            EventBus.Subscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Subscribe<FireReignited>(OnFireReignited);
            EventBus.Subscribe<AgentApplied>(OnAgentApplied);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<FireExtinguished>(OnFireExtinguished);
            EventBus.Unsubscribe<FireReignited>(OnFireReignited);
            EventBus.Unsubscribe<AgentApplied>(OnAgentApplied);
        }

        void OnFireExtinguished(FireExtinguished evt)
        {
            pools.Spawn(EffectId.SteamBurst, evt.Fire.transform.position, Quaternion.identity);
        }

        void OnFireReignited(FireReignited evt)
        {
            pools.Spawn(EffectId.SmokePuff, evt.Fire.transform.position, Quaternion.identity);
        }

        void OnAgentApplied(AgentApplied evt)
        {
            if (evt.Effect != AgentEffect.Dangerous && evt.Effect != AgentEffect.Fatal) return;
            if (Time.time - lastSplash < splashInterval) return;
            lastSplash = Time.time;
            pools.Spawn(EffectId.Splash, evt.Fire.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        }
    }
}
