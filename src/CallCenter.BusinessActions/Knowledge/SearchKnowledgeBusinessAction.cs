using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 检索知识库的业务动作。
/// </summary>
public sealed class SearchKnowledgeBusinessAction(IKnowledgeService knowledgeService) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "SearchKnowledge";

    /// <summary>
    /// 根据当前消息和业务数据查询知识库，并把摘要写入 Workflow Data。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        KnowledgeSearchResult result = await knowledgeService.SearchAsync(
                context.Session,
                context.Message,
                context.Data,
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["knowledgeSummary"] = result.Summary;
        data["knowledgeEntryIds"] = string.Join(",", result.Entries.Select(entry => entry.Id));
        return Success(context, result.Summary, data);
    }
}
