using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 查询订单快照的共享业务动作。
/// </summary>
public sealed class QueryOrderBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "QueryOrder";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        OrderSnapshot order = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, OrderSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Order", "GetOrder", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["orderId"] = order.OrderId;
        data["amount"] = order.Amount.ToString("0.##");
        data["orderStatus"] = order.Status;
        data["couponUsed"] = order.CouponUsed.ToString();

        return Success(context, "订单已查询。", data);
    }
}

/// <summary>
/// 等待用户确认的共享业务动作。
/// </summary>
public sealed class WaitUserConfirmBusinessAction : IBusinessAction
{
    public string Name => "WaitUserConfirm";

    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        bool confirmed = context.Message.Contains("确认", StringComparison.OrdinalIgnoreCase) ||
                         context.Message.Contains("yes", StringComparison.OrdinalIgnoreCase) ||
                         context.Message.Contains("同意", StringComparison.OrdinalIgnoreCase);

        if (!confirmed)
        {
            data["awaiting"] = "user-confirmation";
            return Task.FromResult(new BusinessActionResult(
                context.StepName,
                BusinessActionExecutionStatus.AwaitingHumanInput,
                "请确认是否继续退款。",
                data,
                context.Session,
                context.WorkflowName,
                RequiresHumanInput: true));
        }

        data["confirmed"] = "true";
        return Task.FromResult(Success(context, "用户已确认。", data));
    }
}
