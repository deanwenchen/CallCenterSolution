using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 执行换货执行器（骨架）。
/// 主要作用：在用户确认后调用外部服务执行换货，并产出换货结果。
/// 当前未实现，仅保留扩展点。
/// </summary>
[SendsMessage(typeof(ExchangeExecuted))]
internal sealed class ExecuteExchangeExecutor : Executor<UserConfirmation, ExchangeExecuted>
{
    private readonly IFinanceMcpClient _financeService;

    public ExecuteExchangeExecutor(IFinanceMcpClient financeService) : base("ExecuteExchange")
    {
        _financeService = financeService;
    }

    public ExecuteExchangeExecutor() : this(null!) { }

    public override ValueTask<ExchangeExecuted> HandleAsync(UserConfirmation message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
