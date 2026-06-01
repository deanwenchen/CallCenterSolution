using System.Collections.Concurrent;

namespace CallCenter.Framework.EventBus;

public class InMemoryBusinessEventBus : IBusinessEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var handlerList))
        {
            foreach (var handler in handlerList.ToList())
            {
                if (handler is Func<T, Task> func)
                {
                    _ = func.Invoke(evt);
                }
            }
        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var handlers = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        handlers.Add(handler);
        return new Subscription(this, typeof(T), handler);
    }

    private class Subscription : IDisposable
    {
        private readonly InMemoryBusinessEventBus _bus;
        private readonly Type _eventType;
        private readonly Delegate _handler;

        public Subscription(InMemoryBusinessEventBus bus, Type eventType, Delegate handler)
        {
            _bus = bus;
            _eventType = eventType;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_bus._handlers.TryGetValue(_eventType, out var handlers))
            {
                handlers.Remove(_handler);
            }
        }
    }
}
