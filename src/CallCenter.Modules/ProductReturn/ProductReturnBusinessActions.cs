using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.ProductReturn;

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

    private static BusinessActionResult Success(BusinessActionContext context, string message, Dictionary<string, string> data)
    {
        return new BusinessActionResult(
            context.StepName,
            BusinessActionExecutionStatus.Succeeded,
            message,
            data,
            context.Session,
            context.WorkflowName);
    }

    private static Dictionary<string, string> Merge(Dictionary<string, string> data)
    {
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
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
        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase)
        {
            ["returnOrderId"] = $"RET-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            ["returnOrderStatus"] = "created"
        };

        return Task.FromResult(new BusinessActionResult(
            context.StepName,
            BusinessActionExecutionStatus.Succeeded,
            "退货单已创建。",
            data,
            context.Session,
            context.WorkflowName));
    }
}
