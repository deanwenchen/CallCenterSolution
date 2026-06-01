using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(CouponRestored))]
internal sealed class ExchangeRestoreCouponExecutor : Executor<ExchangeExecuted, CouponRestored>
{
    private readonly IMemberMcpClient _memberService;

    public ExchangeRestoreCouponExecutor(IMemberMcpClient memberService) : base("ExchangeRestoreCoupon")
    {
        _memberService = memberService;
    }

    public ExchangeRestoreCouponExecutor() : this(null!) { }

    public override ValueTask<CouponRestored> HandleAsync(ExchangeExecuted message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
