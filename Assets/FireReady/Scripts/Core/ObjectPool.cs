using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Core
{
    /// <summary>
    /// Pool for short-lived components such as spray puffs and one-shot audio.
    /// Wraps UnityEngine.Pool.ObjectPool and adds the spawn/despawn hooks from IPoolable.
    /// </summary>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        readonly UnityEngine.Pool.ObjectPool<T> pool;
        readonly T prefab;
        readonly Transform parent;
        readonly HashSet<T> active = new HashSet<T>();

        public T Prefab => prefab;
        public int CountActive => active.Count;
        public int CountInactive => pool.CountInactive;
        public int CountCreated { get; private set; }

        public ObjectPool(T prefab, Transform parent, int defaultCapacity = 8, int maxSize = 64)
        {
            this.prefab = prefab;
            this.parent = parent;
            pool = new UnityEngine.Pool.ObjectPool<T>(
                Create,
                OnTake,
                OnReturn,
                item => { if (item != null) Object.Destroy(item.gameObject); },
                collectionCheck: false,
                defaultCapacity,
                maxSize);
        }

        T Create()
        {
            var item = Object.Instantiate(prefab, parent);
            item.gameObject.SetActive(false);
            CountCreated++;
            return item;
        }

        void OnTake(T item)
        {
            item.gameObject.SetActive(true);
            active.Add(item);
        }

        void OnReturn(T item)
        {
            item.OnDespawn();
            item.gameObject.SetActive(false);
            item.transform.SetParent(parent, false);
            active.Remove(item);
        }

        // Creates objects up front so the first spray does not instantiate anything.
        public void Prewarm(int count)
        {
            var temp = new List<T>(count);
            for (int i = 0; i < count; i++) temp.Add(pool.Get());
            foreach (var item in temp) pool.Release(item);
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var item = pool.Get();
            item.transform.SetPositionAndRotation(position, rotation);
            item.OnSpawn();
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !active.Contains(item)) return;
            pool.Release(item);
        }

        public void ReleaseAll()
        {
            var items = new List<T>(active);
            foreach (var item in items) pool.Release(item);
        }

        public void Clear()
        {
            ReleaseAll();
            pool.Clear();
            CountCreated = 0;
        }
    }
}
