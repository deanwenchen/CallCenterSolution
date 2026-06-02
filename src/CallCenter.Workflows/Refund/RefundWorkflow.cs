using CallCenter.Framework.EventBus;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Refund;
using CallCenter.Workflows.Refund.Executors;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund;

/// <summary>
/// 退款工作流定义。
/// 主要作用：把退款业务拆成一组可恢复、可分支、可审计的工作流步骤，并串成完整执行链路。
///
/// 流程步骤：
/// 1. GetOrder         → 根据订单号查询订单信息，若无订单号则通过 InfoPort 追问用户
/// 2. CheckRefundRule  → 校验退款规则（7天退货期、已签收、非定制商品）
/// 3. WaitUserConfirm  → 若校验通过，通过 ConfirmPort 向用户展示订单详情并确认退款
///    3a. RefundDenied → 若校验不通过，直接输出拒绝信息，流程结束
/// 4. ExecuteRefund    → 用户确认后调用财务服务执行退款
/// 5. RestoreCoupon    → 恢复用户使用的优惠券（若退款被取消则跳过）
/// 6. SendNotification → 发布 RefundCompletedEvent 事件并输出退款完成通知
///
/// 图结构：
///   GetOrder → CheckRefundRule → WaitUserConfirm → ConfirmPort → ExecuteRefund → RestoreCoupon → SendNotification
///                        ↘ RefundDenied
///   GetOrder → RefundInfoPort (追问订单号)
/// </summary>
public static class RefundWorkflow
{
    public static Workflow Build(IOrderMcpClient orderService, IFinanceMcpClient financeService, IMemberMcpClient memberService, IBusinessEventBus eventBus)
    {
        var infoPort = RequestPort.Create<RefundSignal, RefundIntent>("RefundInfoPort");
        var confirmPort = RequestPort.Create<ConfirmRefundRequest, UserConfirmation>("RefundConfirmPort");

        var getOrder = new GetOrderExecutor(orderService);
        var checkRule = new CheckRefundRuleExecutor();
        var waitConfirm = new WaitUserConfirmExecutor();
        var doRefund = new ExecuteRefundExecutor(financeService);
        var restoreCoupon = new RestoreCouponExecutor(memberService);
        var notify = new SendNotificationExecutor(eventBus);
        var denied = new RefundDeniedExecutor();

        var builder = new WorkflowBuilder(getOrder);

        builder.ForwardMessage<OrderFound>(getOrder, checkRule);
        builder.ForwardMessage<RefundSignal>(getOrder, infoPort);
        builder.AddEdge<RefundRuleResult>(checkRule, waitConfirm, r => r?.IsEligible == true);
        builder.AddEdge<RefundRuleResult>(checkRule, denied, r => r?.IsEligible == false);
        builder.ForwardMessage<ConfirmRefundRequest>(waitConfirm, confirmPort);
        builder.AddEdge(confirmPort, doRefund);
        builder.ForwardMessage<RefundExecuted>(doRefund, restoreCoupon);
        builder.ForwardMessage<CouponRestored>(restoreCoupon, notify);

        return builder
            .WithOutputFrom(notify, denied)
            .Build();
    }
}
