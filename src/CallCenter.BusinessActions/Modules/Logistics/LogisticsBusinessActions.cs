using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.BusinessActions.Modules.Logistics;

/// <summary>
/// 查询物流快照。
/// </summary>
public sealed class QueryLogisticsBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "QueryLogistics";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        LogisticsSnapshot logistics = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, LogisticsSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Logistics", "Query", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase);
        data["carrier"] = logistics.Carrier;
        data["trackingNo"] = logistics.TrackingNo;
        data["logisticsStatus"] = logistics.Status;

        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, $"Logistics status: {logistics.Status}, tracking number: {logistics.TrackingNo}.", data, context.Session, context.WorkflowName);
    }
}
