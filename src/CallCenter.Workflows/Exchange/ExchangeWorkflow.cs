using CallCenter.Framework.EventBus;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using CallCenter.Workflows.Exchange.Executors;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange;

/// <summary>
/// 换货工作流定义（骨架）。
/// 主要作用：为后续换货业务提供与退款工作流一致的可扩展流程骨架，方便按同样模式接入新业务。
/// 当前图结构已搭好，但具体业务逻辑尚未实现。
/// 预期流程：查订单 → 校验换货规则 → 用户确认 → 执行换货 → 恢复优惠券 → 发送通知。
/// </summary>
public static class ExchangeWorkflow
{
    public static Workflow Build(IOrderMcpClient orderService, IFinanceMcpClient financeService, IMemberMcpClient memberService, IBusinessEventBus eventBus)
    {
        var infoPort = RequestPort.Create<ExchangeSignal, ExchangeIntent>("ExchangeInfoPort");
        var confirmPort = RequestPort.Create<ConfirmExchangeRequest, UserConfirmation>("ExchangeConfirmPort");

        var getOrder = new GetExchangeOrderExecutor(orderService);
        var checkRule = new CheckExchangeRuleExecutor();
        var waitConfirm = new WaitExchangeConfirmExecutor();
        var doExchange = new ExecuteExchangeExecutor(financeService);
        var restoreCoupon = new ExchangeRestoreCouponExecutor(memberService);
        var notify = new ExchangeSendNotificationExecutor(eventBus);
        var denied = new ExchangeDeniedExecutor();

        var builder = new WorkflowBuilder(getOrder);

        builder.ForwardMessage<ExchangeOrderFound>(getOrder, checkRule);
        builder.ForwardMessage<ExchangeSignal>(getOrder, infoPort);
        builder.AddEdge<ExchangeRuleResult>(checkRule, waitConfirm, r => r?.IsEligible == true);
        builder.AddEdge<ExchangeRuleResult>(checkRule, denied, r => r?.IsEligible == false);
        builder.ForwardMessage<ConfirmExchangeRequest>(waitConfirm, confirmPort);
        builder.AddEdge(confirmPort, doExchange);
        builder.ForwardMessage<ExchangeExecuted>(doExchange, restoreCoupon);
        builder.ForwardMessage<CouponRestored>(restoreCoupon, notify);

        return builder
            .WithOutputFrom(notify, denied)
            .Build();
    }
}
