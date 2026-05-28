using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Refund;

/// <summary>
/// 校验退款资格。
/// </summary>
public sealed class CheckRefundRuleBusinessAction : IBusinessAction
{
    public string Name => "CheckRefundRule";

    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        decimal amount = decimal.TryParse(data.GetValueOrDefault("amount"), out decimal parsed) ? parsed : 0;
        bool approved = data.GetValueOrDefault("orderStatus") == "Paid" && amount <= 1000;
        data["refundApproved"] = approved.ToString();
        data["riskLevel"] = amount > 1000 ? RiskLevel.High.ToString() : RiskLevel.Low.ToString();

        if (!approved)
        {
            return Task.FromResult(new BusinessActionResult(
                context.StepName,
                BusinessActionExecutionStatus.AwaitingHumanInput,
                "该退款需要人工审核。",
                data,
                context.Session,
                context.WorkflowName,
                RequiresHumanInput: true));
        }

        return Task.FromResult(Success(context, "退款规则校验通过。", data));
    }

    private static BusinessActionResult Success(BusinessActionContext context, string message, Dictionary<string, string> data)
    {
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, message, data, context.Session, context.WorkflowName);
    }

    private static Dictionary<string, string> Merge(Dictionary<string, string> data)
    {
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 发起退款。
/// </summary>
public sealed class ExecuteRefundBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "ExecuteRefund";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        RefundReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, RefundReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Finance", "Refund", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["refundId"] = receipt.RefundId;
        data["refundStatus"] = receipt.Status;

        return Success(context, "退款已发起。", data);
    }

    private static BusinessActionResult Success(BusinessActionContext context, string message, Dictionary<string, string> data)
    {
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, message, data, context.Session, context.WorkflowName);
    }

    private static Dictionary<string, string> Merge(Dictionary<string, string> data)
    {
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 恢复订单优惠券。
/// </summary>
public sealed class RestoreCouponBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "RestoreCoupon";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(context.Data.GetValueOrDefault("couponUsed"), out bool couponUsed) || !couponUsed)
        {
            return Success(context, "无需恢复优惠券。", Merge(context.Data));
        }

        await externalSystemGateway.InvokeAsync<Dictionary<string, string>, NotificationReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Member", "RestoreCoupon", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["couponRestored"] = "true";

        return Success(context, "优惠券已恢复。", data);
    }

    private static BusinessActionResult Success(BusinessActionContext context, string message, Dictionary<string, string> data)
    {
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, message, data, context.Session, context.WorkflowName);
    }

    private static Dictionary<string, string> Merge(Dictionary<string, string> data)
    {
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    }
}
