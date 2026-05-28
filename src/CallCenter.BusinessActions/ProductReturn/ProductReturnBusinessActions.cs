using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 校验商品退货条件。
/// </summary>
public sealed class CheckProductReturnRuleBusinessAction : IBusinessAction
{
    public string Name => "CheckProductReturnRule";

    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        bool paid = string.Equals(data.GetValueOrDefault("orderStatus"), "Paid", StringComparison.OrdinalIgnoreCase);

        data["productReturnApproved"] = paid.ToString();
        data["returnReason"] = data.GetValueOrDefault("returnReason") ?? "customer-request";

        if (!paid)
        {
            return Task.FromResult(new BusinessActionResult(
                context.StepName,
                BusinessActionExecutionStatus.AwaitingHumanInput,
                "订单状态不满足自动退货条件，已转人工确认。",
                data,
                context.Session,
                context.WorkflowName,
                RequiresHumanInput: true));
        }

        return Task.FromResult(Success(context, "商品退货条件已通过。", data));
    }
}

/// <summary>
/// 创建商品退货单。
/// </summary>
public sealed class CreateProductReturnOrderBusinessAction : IBusinessAction
{
    public string Name => "CreateProductReturnOrder";

    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        data["returnOrderId"] = $"RET-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        data["returnOrderStatus"] = "created";

        return Task.FromResult(Success(context, "退货单已创建。", data));
    }
}
