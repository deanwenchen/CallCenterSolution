using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 换货拒绝执行器（骨架）。
/// 主要作用：当换货规则校验失败时，输出拒绝信息并终止流程。
/// 当前未实现，仅保留扩展点。
/// </summary>
[SendsMessage(typeof(ExchangeNotification))]
internal sealed class ExchangeDeniedExecutor : Executor<ExchangeRuleResult, ExchangeNotification>
{
    public ExchangeDeniedExecutor() : base("ExchangeDenied") { }

    public override ValueTask<ExchangeNotification> HandleAsync(ExchangeRuleResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
