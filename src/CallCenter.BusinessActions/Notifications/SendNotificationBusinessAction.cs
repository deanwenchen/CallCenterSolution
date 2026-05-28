using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 发送用户通知的业务动作。
/// </summary>
public sealed class SendNotificationBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "SendNotification";

    /// <summary>
    /// 通过通知外部系统发送消息，并记录通知标识。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        NotificationReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, NotificationReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("WeCom", "SendMessage", context.Data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, string> data = Merge(context.Data);
        data["notificationId"] = receipt.MessageId;
        return Success(context, "Done. The user has been notified.", data);
    }
}
