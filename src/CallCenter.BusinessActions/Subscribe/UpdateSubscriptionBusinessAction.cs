using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 更新用户订阅状态的业务动作。
/// </summary>
public sealed class UpdateSubscriptionBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "UpdateSubscription";

    /// <summary>
    /// 通过订阅外部系统更新订阅状态。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        data["userId"] = context.Session.UserId;
        data.TryAdd("subscriptionAction", "updated");

        SubscriptionReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, SubscriptionReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Subscribe", "Update", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["subscriptionId"] = receipt.SubscriptionId;
        data["subscriptionStatus"] = receipt.Status;
        return Success(context, $"Subscription has been processed: {receipt.Status}.", data);
    }
}
