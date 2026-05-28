using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Modules.ProductReturn;

/// <summary>
/// 商品退货模块自己的意图、路由和权限配置。
/// </summary>
public sealed class ProductReturnConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<IntentDefinition> definitions =
        [
            new("ProductReturn", IntentType.Refund, ["product return", "return goods", "商品退货", "退货入库"], 0.9)
        ];

        return Task.FromResult(definitions);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<IntentCapabilityRoute> routes =
        [
            new("ProductReturn", "ProductReturn", CapabilityType.Unknown, "ProductReturn -> ProductReturn")
        ];

        return Task.FromResult(routes);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CapabilityWorkflowRoute> routes =
        [
            new("ProductReturn", "ProductReturnWorkflow", "ProductReturn -> ProductReturnWorkflow")
        ];

        return Task.FromResult(routes);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(workflowName, "ProductReturnWorkflow", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<WorkflowPermissionDefinition?>(null);
        }

        return Task.FromResult<WorkflowPermissionDefinition?>(new WorkflowPermissionDefinition(
            "ProductReturnWorkflow",
            ["QueryOrder", "CheckProductReturnRule", "WaitUserConfirm", "CreateProductReturnOrder", "SendNotification"],
            ["Order.GetOrder", "WeCom.SendMessage"]));
    }
}
