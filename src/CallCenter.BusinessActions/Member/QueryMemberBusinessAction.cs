using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 查询会员信息的业务动作。
/// </summary>
public sealed class QueryMemberBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "QueryMember";

    /// <summary>
    /// 通过会员外部系统查询等级、积分和状态。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        data["userId"] = context.Session.UserId;

        MemberSnapshot member = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, MemberSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Member", "GetMember", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["memberLevel"] = member.Level;
        data["memberPoints"] = member.Points.ToString();
        data["memberStatus"] = member.Status;
        return Success(context, $"Member level: {member.Level}, points: {member.Points}.", data);
    }
}
