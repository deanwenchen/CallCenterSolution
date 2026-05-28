using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 本地内存版外部系统网关，用于模拟订单、财务、会员、物流、CRM 等系统响应。
/// </summary>
public sealed class InMemoryExternalSystemGateway(IEnumerable<IWorkflowPermissionProvider> permissionProviders) : IExternalSystemGateway
{
    /// <summary>
    /// 根据 SystemName 和 OperationName 返回模拟外部系统响应。
    /// </summary>
    /// <typeparam name="TRequest">请求体类型。</typeparam>
    /// <typeparam name="TResponse">响应体类型。</typeparam>
    /// <param name="call">外部系统调用描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模拟外部系统响应。</returns>
    public Task<TResponse> InvokeAsync<TRequest, TResponse>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken = default)
    {
        EnsureToolAllowedAsync(call, cancellationToken).GetAwaiter().GetResult();

        object response = (call.SystemName, call.OperationName) switch
        {
            ("Order", "GetOrder") => CreateOrder(call.Request),
            ("Finance", "Refund") => CreateRefund(call.Request),
            ("Member", "RestoreCoupon") => new NotificationReceipt(Guid.NewGuid().ToString("N"), "member", "coupon-restored"),
            ("WeCom", "SendMessage") => new NotificationReceipt(Guid.NewGuid().ToString("N"), "wecom", "sent"),
            ("Logistics", "Query") => CreateLogistics(call.Request),
            ("CRM", "AddTag") => new CrmTagReceipt("unknown", "ai-service", "tagged"),
            ("Invoice", "Create") => CreateInvoice(call.Request),
            ("Member", "GetMember") => CreateMember(call.Request),
            ("Coupon", "Issue") => CreateCoupon(call.Request),
            ("Subscribe", "Update") => CreateSubscription(call.Request),
            _ => throw new InvalidOperationException($"External system operation '{call.SystemName}.{call.OperationName}' is not registered.")
        };

        return Task.FromResult((TResponse)response);
    }

    private async Task EnsureToolAllowedAsync<TRequest>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(call.WorkflowName))
        {
            return;
        }

        WorkflowPermissionDefinition? permission = null;
        foreach (IWorkflowPermissionProvider permissionProvider in permissionProviders)
        {
            permission = await permissionProvider.GetPermissionAsync(call.WorkflowName, cancellationToken)
                .ConfigureAwait(false) ?? permission;
        }

        if (permission is null)
        {
            throw new InvalidOperationException($"Workflow '{call.WorkflowName}' does not have permission configuration.");
        }

        string toolName = $"{call.SystemName}.{call.OperationName}";
        if (!permission.Tools.Contains(toolName, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Tool '{toolName}' is not allowed in workflow '{call.WorkflowName}'.");
        }
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

    private static InvoiceReceipt CreateInvoice<TRequest>(TRequest request)
    {
        string orderId = Extract(request, "orderId") ?? "ORD-10001";
        string title = Extract(request, "invoiceTitle") ?? "Personal";
        return new InvoiceReceipt(Guid.NewGuid().ToString("N"), orderId, title, "created");
    }

    private static MemberSnapshot CreateMember<TRequest>(TRequest request)
    {
        string userId = Extract(request, "userId") ?? "unknown";
        return new MemberSnapshot(userId, "gold", 1280, "active");
    }

    private static CouponReceipt CreateCoupon<TRequest>(TRequest request)
    {
        string userId = Extract(request, "userId") ?? "unknown";
        return new CouponReceipt(Guid.NewGuid().ToString("N"), userId, "issued");
    }

    private static SubscriptionReceipt CreateSubscription<TRequest>(TRequest request)
    {
        string userId = Extract(request, "userId") ?? "unknown";
        string action = Extract(request, "subscriptionAction") ?? "updated";
        return new SubscriptionReceipt(Guid.NewGuid().ToString("N"), userId, action);
    }

    private static string? Extract<TRequest>(TRequest request, string key)
    {
        return request is IReadOnlyDictionary<string, string> values && values.TryGetValue(key, out string? value)
            ? value
            : null;
    }
}

/// <summary>
/// 预留的生产 MCP 外部系统网关，用于将 ExternalSystemCall 映射到 MAF MCP tool 调用。
/// </summary>
public sealed class MafMcpExternalSystemGateway(IEnumerable<IWorkflowPermissionProvider> permissionProviders) : IExternalSystemGateway
{
    /// <summary>
    /// 调用真实 MAF MCP 工具。当前为生产接入预留，尚未实现。
    /// </summary>
    /// <typeparam name="TRequest">请求体类型。</typeparam>
    /// <typeparam name="TResponse">响应体类型。</typeparam>
    /// <param name="call">外部系统调用描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>外部系统响应。</returns>
    public Task<TResponse> InvokeAsync<TRequest, TResponse>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(call.WorkflowName))
        {
            WorkflowPermissionDefinition? permission = null;
            foreach (IWorkflowPermissionProvider permissionProvider in permissionProviders)
            {
                permission = permissionProvider.GetPermissionAsync(call.WorkflowName, cancellationToken)
                    .GetAwaiter()
                    .GetResult() ?? permission;
            }

            string toolName = $"{call.SystemName}.{call.OperationName}";
            if (permission is null)
            {
                throw new InvalidOperationException($"Workflow '{call.WorkflowName}' does not have permission configuration.");
            }

            if (!permission.Tools.Contains(toolName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Tool '{toolName}' is not allowed in workflow '{call.WorkflowName}'.");
            }
        }

        throw new NotImplementedException("Production implementation must invoke external systems through MAF MCP.");
    }
}
