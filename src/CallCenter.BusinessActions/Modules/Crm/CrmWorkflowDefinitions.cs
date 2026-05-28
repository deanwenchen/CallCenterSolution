using CallCenter.Core;

namespace CallCenter.BusinessActions.Modules.Crm;

/// <summary>
/// CRM 模块的流程定义。
/// </summary>
public sealed class CrmWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        new WorkflowDefinition(
            "CrmWorkflow",
            CapabilityType.Crm,
            [new WorkflowStepDefinition("ADD_CRM_TAG", "AddCrmTag", TimeSpan.FromSeconds(30), 3)],
            [],
            nameof(WorkflowState))
    ];
}
