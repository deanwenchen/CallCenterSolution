using CallCenter.Framework.EventBus;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using CallCenter.Workflows.Exchange.Executors;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange;

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
