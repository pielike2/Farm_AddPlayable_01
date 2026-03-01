using System;
using System.Collections.Generic;

namespace Base.SignalSystem
{
    public class SignalBus
    {
        public static readonly SignalBus Default = new SignalBus();

        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[typeof(T)] = handlers;
            }
            handlers.Add(handler);

            return new Subscription(() =>
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                    _subscribers.Remove(typeof(T));
            });
        }

        public void Publish<T>(T signal)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var handlers)) 
                return;
            
            for (int i = 0; i < handlers.Count;)
            {
                var handler = (Action<T>)handlers[i];
                var countBefore = handlers.Count;
                handler?.Invoke(signal);
                var countAfter = handlers.Count;
                if (countAfter < countBefore)
                    continue;
                i++;
            }
        }
    }
}