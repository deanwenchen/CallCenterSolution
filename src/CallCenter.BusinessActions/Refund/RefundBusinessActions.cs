using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 查询订单快照的退款流程业务动作。
/// </summary>
public sealed class QueryOrderBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "QueryOrder";

    /// <summary>
    /// 通过外部系统网关查询订单，并把订单数据写入 Workflow Data。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        // 查订单只通过外部系统网关访问订单系统，BusinessAction 不直接访问数据库或 HTTP API。
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
/// 校验退款资格的业务动作。
/// </summary>
public sealed class CheckRefundRuleBusinessAction : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "CheckRefundRule";

    /// <summary>
    /// 根据已查询到的订单快照判断是否允许自动退款。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        // 退款规则校验只基于上游 Step 已经放入 Data 的订单快照。
        // 这里不再查询外部系统，避免同一个 Step 混入多个职责。
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

/// <summary>
/// 等待用户确认退款的跨轮对话业务动作。
/// </summary>
public sealed class WaitUserConfirmBusinessAction : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "WaitUserConfirm";

    /// <summary>
    /// 判断用户是否已确认继续退款；未确认时暂停 Workflow。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        // 人工确认 Step 是跨轮对话的中断点。
        // 未确认时返回 AwaitingHumanInput，ConversationGateway 会保存 WorkflowState。
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

/// <summary>
/// 发起退款的业务动作。
/// </summary>
public sealed class ExecuteRefundBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "ExecuteRefund";

    /// <summary>
    /// 通过外部系统网关向财务系统发起退款。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        // 执行退款是有副作用动作，只允许在 Workflow Step 中触发。
        // CorrelationId 会透传给外部系统，便于审计和链路追踪。
        RefundReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, RefundReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Finance", "Refund", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["refundId"] = receipt.RefundId;
        data["refundStatus"] = receipt.Status;

        return Success(context, "退款已发起。", data);
    }
}

/// <summary>
/// 恢复订单优惠券的业务动作，也可作为退款失败补偿动作。
/// </summary>
public sealed class RestoreCouponBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "RestoreCoupon";

    /// <summary>
    /// 在订单使用过优惠券时调用会员系统恢复优惠券。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        // 只有订单确实使用过优惠券时才调用会员系统恢复优惠券。
        // 该动作既可以作为正常 Step，也可以作为退款失败时的补偿动作复用。
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
}
