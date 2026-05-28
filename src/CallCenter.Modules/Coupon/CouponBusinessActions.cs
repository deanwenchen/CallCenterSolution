using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Coupon;

/// <summary>
/// 发放优惠券。
/// </summary>
public sealed class IssueCouponBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "IssueCoupon";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase)
        {
            ["userId"] = context.Session.UserId
        };

        CouponReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, CouponReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Coupon", "Issue", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["couponId"] = receipt.CouponId;
        data["couponStatus"] = receipt.Status;
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, "Coupon has been issued.", data, context.Session, context.WorkflowName);
    }
}
