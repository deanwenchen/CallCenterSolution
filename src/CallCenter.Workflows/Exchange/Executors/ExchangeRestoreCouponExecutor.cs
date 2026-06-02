using System;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

/// <summary>
/// 换货优惠券恢复执行器（骨架）。
/// 主要作用：在换货成功后处理优惠券补偿逻辑，保持和退款流程一致的补偿结构。
/// 当前未实现，仅保留扩展点。
/// </summary>
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
