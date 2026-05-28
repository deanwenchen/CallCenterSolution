using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Workflows;

/// <summary>
/// 当前系统内置的流程清单。
/// 后面接后台配置时，这里可以替换成数据库或配置中心读取。
/// </summary>
public sealed class BuiltInWorkflowDefinitions : IWorkflowDefinitionProvider
{
    public IReadOnlyCollection<WorkflowDefinition> GetDefinitions() =>
    [
        RefundWorkflowDefinitions.StandardRefund(),
        RefundWorkflowDefinitions.ManualRefund(),
        CustomerWorkflowDefinitions.Logistics(),
        CustomerWorkflowDefinitions.Crm(),
        CustomerWorkflowDefinitions.Invoice(),
        CustomerWorkflowDefinitions.Subscribe(),
        CustomerWorkflowDefinitions.Member(),
        CustomerWorkflowDefinitions.Coupon(),
        CustomerWorkflowDefinitions.HumanHandoff()
    ];
}
