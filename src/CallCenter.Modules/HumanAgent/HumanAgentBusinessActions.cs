using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.HumanAgent;

/// <summary>
/// 检索知识库。
/// </summary>
public sealed class SearchKnowledgeBusinessAction(IKnowledgeService knowledgeService) : IBusinessAction
{
    public string Name => "SearchKnowledge";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        KnowledgeSearchResult result = await knowledgeService.SearchAsync(
                context.Session,
                context.Message,
                context.Data,
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase);
        data["knowledgeSummary"] = result.Summary;
        data["knowledgeEntryIds"] = string.Join(",", result.Entries.Select(entry => entry.Id));
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, result.Summary, data, context.Session, context.WorkflowName);
    }
}

/// <summary>
/// 创建人工客服工单并暂停流程。
/// </summary>
public sealed class HumanHandoffBusinessAction(IHumanAgentService humanAgentService) : IBusinessAction
{
    public string Name => "HumanHandoff";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase);
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
