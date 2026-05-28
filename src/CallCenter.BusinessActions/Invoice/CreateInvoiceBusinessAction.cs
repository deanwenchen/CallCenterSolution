using CallCenter.Application;
using CallCenter.Domain;
using static CallCenter.BusinessActions.BusinessActionResults;

namespace CallCenter.BusinessActions;

/// <summary>
/// 创建发票的业务动作。
/// </summary>
public sealed class CreateInvoiceBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    /// <summary>
    /// 业务动作名称。
    /// </summary>
    public string Name => "CreateInvoice";

    /// <summary>
    /// 通过发票外部系统创建发票，并写入发票处理结果。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = Merge(context.Data);
        data["userId"] = context.Session.UserId;

        InvoiceReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, InvoiceReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Invoice", "Create", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["invoiceId"] = receipt.InvoiceId;
        data["invoiceStatus"] = receipt.Status;
        data["invoiceTitle"] = receipt.Title;
        return Success(context, $"Invoice has been created. Status: {receipt.Status}.", data);
    }
}
