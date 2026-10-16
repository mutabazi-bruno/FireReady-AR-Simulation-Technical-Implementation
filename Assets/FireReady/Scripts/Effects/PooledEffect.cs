using System;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Effects
{
    public enum EffectId
    {
        SprayPuff,
        SteamBurst,
        SmokePuff,
        Splash
    }

    /// <summary>
    /// A short particle effect that returns itself to its pool when it has finished playing.
    /// </summary>
    public class PooledEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] EffectId id;
        [SerializeField] float lifetime = 1.2f;

        ParticleSystem[] systems;
        Action<PooledEffect> release;
        float timer;

        public EffectId Id => id;

        void Awake()
        {
            systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public void Bind(Action<PooledEffect> releaseAction)
        {
            release = releaseAction;
        }

        public void SetColor(Color color)
        {
            foreach (var system in systems)
            {
                var main = system.main;
                main.startColor = color;
            }
        }

        public void OnSpawn()
        {
            timer = lifetime;
            foreach (var system in systems)
            {
                system.Clear(true);
                system.Play(true);
            }
        }

        public void OnDespawn()
        {
            foreach (var system in systems)
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localScale = Vector3.one;
            release = null;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) release?.Invoke(this);
        }
    }
}
