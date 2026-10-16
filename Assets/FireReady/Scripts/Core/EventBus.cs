using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Core
{
    /// <summary>
    /// Typed publish/subscribe hub. A publisher never knows who is listening,
    /// so fires and extinguishers stay free of UI and audio code.
    /// Events are structs so publishing every frame does not allocate.
    /// </summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var type = typeof(T);
            handlers.TryGetValue(type, out var existing);
            handlers[type] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!handlers.TryGetValue(type, out var existing)) return;

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) handlers.Remove(type);
            else handlers[type] = remaining;
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler).Invoke(evt);
        }

        public static int SubscriberCount<T>() where T : struct
        {
            return handlers.TryGetValue(typeof(T), out var handler) ? handler.GetInvocationList().Length : 0;
        }

        public static void Clear()
        {
            handlers.Clear();
        }

        // Static state survives play mode when domain reload is off, so start clean each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            handlers.Clear();
        }
    }
}
