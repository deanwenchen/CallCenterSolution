using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Crm;

/// <summary>
/// 给用户添加 CRM 标签。
/// </summary>
public sealed class AddCrmTagBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "AddCrmTag";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        CrmTagReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, CrmTagReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("CRM", "AddTag", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase);
        data["crmTagStatus"] = receipt.Status;
        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, "CRM tag has been updated.", data, context.Session, context.WorkflowName);
    }
}
