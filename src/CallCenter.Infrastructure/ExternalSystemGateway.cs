using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

// 本地骨架验证用的外部系统网关，只模拟订单、财务、会员、物流等系统响应。
// 生产环境应替换为基于 MAF MCP 访问方式实现的适配器，避免把本地模拟实现误认为 MCP。
public sealed class InMemoryExternalSystemGateway : IExternalSystemGateway
{
    public Task<TResponse> InvokeAsync<TRequest, TResponse>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken = default)
    {
        object response = (call.SystemName, call.OperationName) switch
        {
            ("Order", "GetOrder") => CreateOrder(call.Request),
            ("Finance", "Refund") => CreateRefund(call.Request),
            ("Member", "RestoreCoupon") => new NotificationReceipt(Guid.NewGuid().ToString("N"), "member", "coupon-restored"),
            ("WeCom", "SendMessage") => new NotificationReceipt(Guid.NewGuid().ToString("N"), "wecom", "sent"),
            ("Logistics", "Query") => CreateLogistics(call.Request),
            ("CRM", "AddTag") => new CrmTagReceipt("unknown", "ai-service", "tagged"),
            _ => throw new InvalidOperationException($"External system operation '{call.SystemName}.{call.OperationName}' is not registered.")
        };

        return Task.FromResult((TResponse)response);
    }

    private static OrderSnapshot CreateOrder<TRequest>(TRequest request)
    {
        string orderId = Extract(request, "orderId") ?? "ORD-10001";
        decimal amount = decimal.TryParse(Extract(request, "amount"), out decimal parsed) ? parsed : 99;
        return new OrderSnapshot(orderId, amount, "Paid", Paid: true, CouponUsed: true);
    }

    private static RefundReceipt CreateRefund<TRequest>(TRequest request)
    {
        string orderId = Extract(request, "orderId") ?? "ORD-10001";
        decimal amount = decimal.TryParse(Extract(request, "amount"), out decimal parsed) ? parsed : 99;
        return new RefundReceipt(Guid.NewGuid().ToString("N"), orderId, amount, "accepted");
    }

    private static LogisticsSnapshot CreateLogistics<TRequest>(TRequest request)
    {
        string orderId = Extract(request, "orderId") ?? "ORD-10001";
        return new LogisticsSnapshot(orderId, "SF Express", $"SF{Random.Shared.Next(100000, 999999)}", "in-transit");
    }

    private static string? Extract<TRequest>(TRequest request, string key)
    {
        return request is IReadOnlyDictionary<string, string> values && values.TryGetValue(key, out string? value)
            ? value
            : null;
    }
}

// 预留生产适配器名称：这里的 MCP 明确指 MAF 的 MCP 访问方式。
public sealed class MafMcpExternalSystemGateway : IExternalSystemGateway
{
    public Task<TResponse> InvokeAsync<TRequest, TResponse>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Production implementation must invoke external systems through MAF MCP.");
    }
}
