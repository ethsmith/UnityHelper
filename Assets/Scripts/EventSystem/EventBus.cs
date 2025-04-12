using System;
using System.Collections.Generic;
using System.Linq;

namespace EventSystem
{
    public static class EventBus
    {
        private class ListenerEntry
        {
            public Delegate Callback;
            public int Priority;
            public object Owner;

            public ListenerEntry(Delegate callback, int priority, object owner)
            {
                Callback = callback;
                Priority = priority;
                Owner = owner;
            }
        }

        private static readonly Dictionary<Type, List<ListenerEntry>> _listeners = new();

        public static void ListenTo<T>(object owner, Action<T> callback, int priority = 0) where T : Event
        {
            var type = typeof(T);
            if (!_listeners.ContainsKey(type))
                _listeners[type] = new List<ListenerEntry>();

            _listeners[type].Add(new ListenerEntry(callback, priority, owner));
            _listeners[type] = _listeners[type].OrderByDescending(e => e.Priority).ToList();
        }

        public static void StopListening<T>(object owner, Action<T> callback) where T : Event
        {
            var type = typeof(T);
            if (_listeners.TryGetValue(type, out var list))
            {
                list.RemoveAll(e => e.Callback.Equals(callback) && e.Owner == owner);
            }
        }

        public static void StopListeningToAll(object owner)
        {
            foreach (var list in _listeners.Values)
            {
                list.RemoveAll(e => e.Owner == owner);
            }
        }

        public static void Fire<T>(T evt) where T : Event
        {
            var type = typeof(T);
            if (_listeners.TryGetValue(type, out var list))
            {
                foreach (var entry in list)
                {
                    (entry.Callback as Action<T>)?.Invoke(evt);
                }
            }
        }
    }
}