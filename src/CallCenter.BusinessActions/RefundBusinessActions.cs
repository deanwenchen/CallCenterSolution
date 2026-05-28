using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

public sealed class QueryOrderBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "QueryOrder";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        OrderSnapshot order = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, OrderSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Order", "GetOrder", context.Data, context.Session.CorrelationId),
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
}

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

public sealed class ExecuteRefundBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "ExecuteRefund";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        RefundReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, RefundReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Finance", "Refund", context.Data, context.Session.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["refundId"] = receipt.RefundId;
        data["refundStatus"] = receipt.Status;

        return Success(context, "退款已发起。", data);
    }
}

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
                new ExternalSystemCall<Dictionary<string, string>>("Member", "RestoreCoupon", context.Data, context.Session.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["couponRestored"] = "true";

        return Success(context, "优惠券已恢复。", data);
    }
}

internal static class BusinessActionResults
{
    public static BusinessActionResult Success(BusinessActionContext context, string message, Dictionary<string, string> data)
    {
        return new BusinessActionResult(
            context.StepName,
            BusinessActionExecutionStatus.Succeeded,
            message,
            data,
            context.Session,
            context.WorkflowName);
    }

    public static Dictionary<string, string> Merge(Dictionary<string, string> data)
    {
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    }
}
