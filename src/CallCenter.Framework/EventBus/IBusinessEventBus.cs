namespace CallCenter.Framework.EventBus;

/// <summary>
/// 业务事件总线接口。用于解耦跨切面关注点。
/// 工作流通过 PublishAsync 发布领域事件（如 RefundCompletedEvent），
/// 外部系统（通知服务、分析系统）可订阅这些事件而不必紧耦合。
/// </summary>
public interface IBusinessEventBus
{
    /// <summary>Publishes an event to all registered handlers.</summary>
    Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class;

    /// <summary>Registers a handler for the given event type. Returns a disposable subscription.</summary>
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : class;
}
