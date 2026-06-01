using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Refund.Executors;

internal sealed class RestoreCouponExecutor : Executor<RefundExecuted, CouponRestored>
{
    private readonly IMemberMcpClient _memberService;

    public RestoreCouponExecutor(IMemberMcpClient memberService) : base("RestoreCoupon")
    {
        _memberService = memberService;
    }

    public RestoreCouponExecutor() : this(null!) { }

    public override async ValueTask<CouponRestored> HandleAsync(RefundExecuted message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (message.Result == null)
        {
            // User cancelled refund, no coupon to restore
            return new CouponRestored(null);
        }

        await _memberService.RestoreCouponAsync("U100", "CPN-2024", cancellationToken);
        return new CouponRestored("CPN-2024");
    }
}
