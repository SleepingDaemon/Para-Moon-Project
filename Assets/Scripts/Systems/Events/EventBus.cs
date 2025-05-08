using System;
using System.Collections.Generic;

namespace ParaMoon
{
    [Injectable]
    public class EventBus : ServiceBehaviour<EventBus>
    {
        private Dictionary<Type, List<Delegate>> _eventHandlers = new();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_eventHandlers.TryGetValue(type, out var handlers))
            {
                handlers = new List<Delegate>();
                _eventHandlers[type] = handlers;
            }
            handlers.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_eventHandlers.TryGetValue(type, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            if (_eventHandlers.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers.ToArray())
                {
                    ((Action<T>)handler)(eventData);
                }
            }
        }
    }
}