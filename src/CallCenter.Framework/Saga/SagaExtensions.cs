using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.Saga;

public static class SagaExtensions
{
    /// <summary>
    /// Execute an action with Saga retry + compensation configured via fluent builder.
    /// Usage: (() => ExecuteRefund()).ExecuteWithSaga(saga => saga
    ///     .OnFailure("ExecuteRefund", async ct => await restoreCoupon.Execute(ct))
    ///     .WithRetry(3, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)));
    /// </summary>
    public static async Task ExecuteWithSaga(
        this Func<CancellationToken, Task> action,
        Action<SagaBuilder> configure,
        CancellationToken ct = default)
    {
        var builder = new SagaBuilder();
        configure(builder);
        await builder.ExecuteAsync(action, ct);
    }
}
