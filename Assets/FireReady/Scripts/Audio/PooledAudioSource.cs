using System;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Audio
{
    // One-shot sound that goes back to the pool when the clip has finished.
    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioSource : MonoBehaviour, IPoolable
    {
        AudioSource source;
        Action<PooledAudioSource> release;
        float timer;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Bind(Action<PooledAudioSource> releaseAction)
        {
            release = releaseAction;
        }

        public void Play(AudioClip clip, float volume, float pitch, float spatialBlend, bool ignorePause)
        {
            if (clip == null)
            {
                release?.Invoke(this);
                return;
            }

            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend;
            source.ignoreListenerPause = ignorePause;
            source.minDistance = 0.5f;
            source.maxDistance = 12f;
            source.Play();
            timer = clip.length / Mathf.Max(0.1f, pitch) + 0.1f;
        }

        public void OnSpawn() { }

        public void OnDespawn()
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            release = null;
        }

        void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f) release?.Invoke(this);
        }
    }
}
