namespace CallCenter.Framework.EventBus;

/// <summary>
/// Pub/sub event bus for decoupled cross-cutting concerns.
/// Used to emit domain events (e.g., RefundCompletedEvent) that external
/// systems can subscribe to without being tightly coupled to workflows.
/// </summary>
public interface IBusinessEventBus
{
    /// <summary>Publishes an event to all registered handlers.</summary>
    Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class;

    /// <summary>Registers a handler for the given event type. Returns a disposable subscription.</summary>
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : class;
}
