using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 发放优惠券的业务动作。
/// </summary>
public sealed class IssueCouponBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "IssueCoupon";

    /// <summary>
    /// 通过优惠券外部系统发券，并写入发券结果。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        data["userId"] = context.Session.UserId;

        CouponReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, CouponReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Coupon", "Issue", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["couponId"] = receipt.CouponId;
        data["couponStatus"] = receipt.Status;
        return Success(context, "Coupon has been issued.", data);
    }
}
