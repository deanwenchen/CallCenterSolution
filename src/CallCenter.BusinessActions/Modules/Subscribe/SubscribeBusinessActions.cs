using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Subscribe;

/// <summary>
/// 更新用户订阅状态。
/// </summary>
public sealed class UpdateSubscriptionBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "UpdateSubscription";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase)
        {
            ["userId"] = context.Session.UserId
        };
        data.TryAdd("subscriptionAction", "updated");

        SubscriptionReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, SubscriptionReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Subscribe", "Update", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["subscriptionId"] = receipt.SubscriptionId;
        data["subscriptionStatus"] = receipt.Status;
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, $"Subscription has been processed: {receipt.Status}.", data, context.Session, context.WorkflowName);
    }
}
