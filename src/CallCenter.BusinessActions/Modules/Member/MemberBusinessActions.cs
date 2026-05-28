using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Member;

/// <summary>
/// 查询会员信息。
/// </summary>
public sealed class QueryMemberBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "QueryMember";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase)
        {
            ["userId"] = context.Session.UserId
        };

        MemberSnapshot member = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, MemberSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Member", "GetMember", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["memberLevel"] = member.Level;
        data["memberPoints"] = member.Points.ToString();
        data["memberStatus"] = member.Status;
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, $"Member level: {member.Level}, points: {member.Points}.", data, context.Session, context.WorkflowName);
    }
}
