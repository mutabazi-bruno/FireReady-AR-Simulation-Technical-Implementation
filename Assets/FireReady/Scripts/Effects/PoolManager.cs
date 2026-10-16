using System;
using System.Collections.Generic;
using System.Text;
using FireReady.Audio;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Effects
{
    /// <summary>
    /// Owns the pools for short-lived effects and one-shot audio sources. Spraying makes several
    /// puffs a second, so creating and destroying them would cause GC spikes on a phone.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [SerializeField] int effectPrewarm = 8;
        [SerializeField] int audioPrewarm = 8;

        readonly Dictionary<EffectId, ObjectPool<PooledEffect>> effectPools = new Dictionary<EffectId, ObjectPool<PooledEffect>>();
        readonly Dictionary<EffectId, Action<PooledEffect>> effectReleases = new Dictionary<EffectId, Action<PooledEffect>>();
        ObjectPool<PooledAudioSource> audioPool;
        Action<PooledAudioSource> audioRelease;
        Transform effectsParent;
        Transform audioParent;

        public bool HasEffects => effectPools.Count > 0;

        void Awake()
        {
            effectsParent = new GameObject("EffectPool").transform;
            effectsParent.SetParent(transform, false);
            audioParent = new GameObject("AudioPool").transform;
            audioParent.SetParent(transform, false);
            CreateAudioPool();
        }

        void CreateAudioPool()
        {
            // The audio template is built in code, it needs nothing but an AudioSource.
            var template = new GameObject("OneShotAudio");
            template.SetActive(false);
            template.transform.SetParent(audioParent, false);
            template.AddComponent<AudioSource>();
            var pooled = template.AddComponent<PooledAudioSource>();

            audioPool = new ObjectPool<PooledAudioSource>(pooled, audioParent, audioPrewarm, 32);
            audioPool.Prewarm(audioPrewarm);
            audioRelease = audioPool.Release;
        }

        public void CreateEffectPools(IEnumerable<GameObject> prefabs)
        {
            ClearEffects();
            foreach (var prefab in prefabs)
            {
                var effect = prefab != null ? prefab.GetComponent<PooledEffect>() : null;
                if (effect == null || effectPools.ContainsKey(effect.Id)) continue;

                var pool = new ObjectPool<PooledEffect>(effect, effectsParent, effectPrewarm, 48);
                pool.Prewarm(effectPrewarm);
                effectPools[effect.Id] = pool;
                effectReleases[effect.Id] = pool.Release;
            }
        }

        public PooledEffect Spawn(EffectId id, Vector3 position, Quaternion rotation)
        {
            if (!effectPools.TryGetValue(id, out var pool)) return null;
            var effect = pool.Get(position, rotation);
            effect.Bind(effectReleases[id]);
            return effect;
        }

        public PooledAudioSource GetAudioSource(Vector3 position)
        {
            var source = audioPool.Get(position, Quaternion.identity);
            source.Bind(audioRelease);
            return source;
        }

        public void ReleaseAllEffects()
        {
            foreach (var pool in effectPools.Values) pool.ReleaseAll();
        }

        // Called before the Effects Addressables group is released, so no instance outlives its prefab.
        public void ClearEffects()
        {
            foreach (var pool in effectPools.Values) pool.Clear();
            effectPools.Clear();
            effectReleases.Clear();
        }

        public string DescribeStats()
        {
            var text = new StringBuilder();
            foreach (var pair in effectPools)
                text.Append(pair.Key).Append(' ').Append(pair.Value.CountActive).Append('/').Append(pair.Value.CountCreated).Append("  ");
            text.Append("Audio ").Append(audioPool.CountActive).Append('/').Append(audioPool.CountCreated);
            return text.ToString();
        }
    }
}
