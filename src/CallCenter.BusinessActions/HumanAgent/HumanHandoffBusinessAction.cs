using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 创建人工客服工单并暂停 Workflow 的业务动作。
/// </summary>
public sealed class HumanHandoffBusinessAction(IHumanAgentService humanAgentService) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "HumanHandoff";

    /// <summary>
    /// 创建人工客服工单，并返回需要人工输入的结果。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        HumanAgentTicket ticket = await humanAgentService.CreateTicketAsync(
                context.Session,
                data.GetValueOrDefault("failureReason") ?? "Manual handling required.",
                data,
                cancellationToken)
            .ConfigureAwait(false);

        data["handoff"] = "required";
        data["humanTicketId"] = ticket.TicketId;
        data["humanTicketStatus"] = ticket.Status;

        return new BusinessActionResult(
            context.StepName,
            BusinessActionExecutionStatus.AwaitingHumanInput,
            $"Transferred to a human agent. Ticket: {ticket.TicketId}.",
            data,
            context.Session,
            context.WorkflowName,
            RequiresHumanInput: true);
    }
}
