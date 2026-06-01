namespace CallCenter.Framework.EventBus;

public interface IBusinessEventBus
{
    Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class;
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : class;
}
