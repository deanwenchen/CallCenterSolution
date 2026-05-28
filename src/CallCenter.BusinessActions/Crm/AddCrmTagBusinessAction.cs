using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 给用户添加 CRM 标签的业务动作。
/// </summary>
public sealed class AddCrmTagBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "AddCrmTag";

    /// <summary>
    /// 通过 CRM 外部系统写入标签处理结果。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        CrmTagReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, CrmTagReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("CRM", "AddTag", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["crmTagStatus"] = receipt.Status;
        return Success(context, "CRM tag has been updated.", data);
    }
}
