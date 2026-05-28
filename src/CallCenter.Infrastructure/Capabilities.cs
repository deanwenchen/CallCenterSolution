using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

public sealed class CapabilityRegistry(IEnumerable<ICapability> capabilities) : ICapabilityRegistry
{
    private readonly Dictionary<CapabilityType, ICapability> _capabilities =
        capabilities.ToDictionary(capability => capability.Type);

    public ICapability Resolve(CapabilityType capability)
    {
        if (_capabilities.TryGetValue(capability, out ICapability? resolved))
        {
            return resolved;
        }

        return _capabilities[CapabilityType.HumanAgent];
    }
}

public sealed class RefundCapability : ICapability
{
    public CapabilityType Type => CapabilityType.Refund;

    // Capability 层负责策略和 Workflow 选择，Planner 不参与业务规则判断。
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
        return Task.FromResult(new WorkflowSelection(workflowName, Type, $"Risk {risk} selected {workflowName}."));
    }
}

public sealed class LogisticsCapability : StaticWorkflowCapability
{
    public LogisticsCapability() : base(CapabilityType.Logistics, "LogisticsWorkflow")
    {
    }
}

public sealed class CrmCapability : StaticWorkflowCapability
{
    public CrmCapability() : base(CapabilityType.Crm, "CrmWorkflow")
    {
    }
}

public sealed class HumanAgentCapability : StaticWorkflowCapability
{
    public HumanAgentCapability() : base(CapabilityType.HumanAgent, "HumanHandoffWorkflow")
    {
    }
}

public abstract class StaticWorkflowCapability(CapabilityType type, string workflowName) : ICapability
{
    public CapabilityType Type { get; } = type;

    public Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WorkflowSelection(workflowName, Type, $"Capability {Type} selected {workflowName}."));
    }
}
