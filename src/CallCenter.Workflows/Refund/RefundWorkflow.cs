using CallCenter.Framework.EventBus;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Refund;
using CallCenter.Workflows.Refund.Executors;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund;

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
