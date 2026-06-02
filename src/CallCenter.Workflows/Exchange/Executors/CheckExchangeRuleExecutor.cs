using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 换货规则校验执行器（骨架）。
/// 主要作用：校验订单是否满足换货条件，并决定进入确认路径还是拒绝路径。
/// 当前未实现，仅保留扩展点。
/// </summary>
[SendsMessage(typeof(ExchangeRuleResult))]
internal sealed class CheckExchangeRuleExecutor : Executor<ExchangeOrderFound, ExchangeRuleResult>
{
    public CheckExchangeRuleExecutor() : base("CheckExchangeRule") { }

    public override ValueTask<ExchangeRuleResult> HandleAsync(ExchangeOrderFound message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
