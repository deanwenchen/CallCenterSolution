using System.Collections.Concurrent;

namespace CallCenter.Framework.EventBus;

/// <summary>
/// In-memory event bus implementation using ConcurrentDictionary for thread-safe handler storage.
/// Fire-and-forget dispatch — handlers are invoked without awaiting, so slow handlers won't block publishers.
/// Suitable for demo/dev; production should use a durable message broker.
/// </summary>
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
                    // Fire-and-forget: handlers run without blocking the publisher.
                    // In production, consider awaiting with timeout or using a queue.
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

    /// <summary>Represents an active subscription. Disposing removes the handler.</summary>
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
