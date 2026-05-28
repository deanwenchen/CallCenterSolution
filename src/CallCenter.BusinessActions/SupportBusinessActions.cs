using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

public sealed class SendNotificationBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "SendNotification";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        NotificationReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, NotificationReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("WeCom", "SendMessage", context.Data, context.Session.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["notificationId"] = receipt.MessageId;
        return Success(context, "处理完成，已通知用户。", data);
    }
}

public sealed class QueryLogisticsBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "QueryLogistics";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        LogisticsSnapshot logistics = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, LogisticsSnapshot>(
                new ExternalSystemCall<Dictionary<string, string>>("Logistics", "Query", context.Data, context.Session.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["carrier"] = logistics.Carrier;
        data["trackingNo"] = logistics.TrackingNo;
        data["logisticsStatus"] = logistics.Status;
        return Success(context, $"物流状态：{logistics.Status}，单号：{logistics.TrackingNo}。", data);
    }
}

public sealed class AddCrmTagBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "AddCrmTag";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        CrmTagReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, CrmTagReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("CRM", "AddTag", context.Data, context.Session.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["crmTagStatus"] = receipt.Status;
        return Success(context, "CRM 标签已更新。", data);
    }
}

public sealed class HumanHandoffBusinessAction : IBusinessAction
{
    public string Name => "HumanHandoff";

    public Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        data["handoff"] = "required";
        return Task.FromResult(new BusinessActionResult(
            context.StepName,
            BusinessActionExecutionStatus.AwaitingHumanInput,
            "已转人工处理。",
            data,
            context.Session,
            context.WorkflowName,
            RequiresHumanInput: true));
    }
}
