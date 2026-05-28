using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Invoice;

/// <summary>
/// 创建发票。
/// </summary>
public sealed class CreateInvoiceBusinessAction(IExternalSystemGateway externalSystemGateway) : IBusinessAction
{
    public string Name => "CreateInvoice";

    public async Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = new(context.Data, StringComparer.OrdinalIgnoreCase)
        {
            ["userId"] = context.Session.UserId
        };

        InvoiceReceipt receipt = await externalSystemGateway.InvokeAsync<Dictionary<string, string>, InvoiceReceipt>(
                new ExternalSystemCall<Dictionary<string, string>>("Invoice", "Create", data, context.Session.CorrelationId, context.WorkflowName),
                cancellationToken)
            .ConfigureAwait(false);

        data["invoiceId"] = receipt.InvoiceId;
        data["invoiceStatus"] = receipt.Status;
        data["invoiceTitle"] = receipt.Title;

        return new BusinessActionResult(context.StepName, BusinessActionExecutionStatus.Succeeded, $"Invoice has been created. Status: {receipt.Status}.", data, context.Session, context.WorkflowName);
    }
}
