using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.Refund;

/// <summary>
/// 退款能力，根据金额风险选择自动退款或人工退款流程。
/// </summary>
public sealed class RefundCapability : ICapability
{
    public string Key => "Refund";

    public CapabilityType Type => CapabilityType.Refund;

    public Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        RiskLevel risk = intent.Entities.TryGetValue("amount", out string? amountText) &&
                         decimal.TryParse(amountText, out decimal amount) &&
                         amount >= 1000
            ? RiskLevel.High
            : RiskLevel.Low;

        string workflowName = risk == RiskLevel.High ? "ManualRefundWorkflow" : "RefundWorkflow";
        return Task.FromResult(new WorkflowSelection(workflowName, Type, $"Risk {risk} selected {workflowName}.", Key));
    }
}
