using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 查询物流快照的业务动作。
/// </summary>
public sealed class QueryLogisticsBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "QueryLogistics";

    /// <summary>
    /// 通过物流外部系统查询承运商、物流单号和物流状态。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        LogisticsSnapshot logistics = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, LogisticsSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Logistics", "Query", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["carrier"] = logistics.Carrier;
        data["trackingNo"] = logistics.TrackingNo;
        data["logisticsStatus"] = logistics.Status;
        return Success(context, $"Logistics status: {logistics.Status}, tracking number: {logistics.TrackingNo}.", data);
    }
}
