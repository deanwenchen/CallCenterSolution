using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 本地内存配置。后面接后台时，只需要替换这些 provider。
/// </summary>
public sealed class InMemoryCallCenterConfiguration :
    IIntentDefinitionProvider,
    IIntentCapabilityRouteProvider,
    ICapabilityWorkflowRouteProvider,
    IWorkflowPermissionProvider
{
    private static readonly IntentDefinition[] IntentDefinitions =
    [
        new("Refund", IntentType.Refund, ["refund", "退款"], 0.9),
        new("Logistics", IntentType.Logistics, ["logistics", "tracking", "物流", "快递"], 0.9),
        new("Invoice", IntentType.Invoice, ["invoice", "发票"], 0.9),
        new("Crm", IntentType.Crm, ["crm", "tag", "标签"], 0.9),
        new("Subscribe", IntentType.Subscribe, ["subscribe", "subscription", "订阅"], 0.9),
        new("Member", IntentType.Member, ["member", "points", "会员", "积分"], 0.9),
        new("Coupon", IntentType.Coupon, ["coupon", "优惠券", "券"], 0.9)
    ];

    private static readonly IntentCapabilityRoute[] IntentCapabilityRoutes =
    [
        Route("Refund", "Refund", CapabilityType.Refund),
        Route("Logistics", "Logistics", CapabilityType.Logistics),
        Route("Invoice", "Invoice", CapabilityType.Invoice),
        Route("Crm", "Crm", CapabilityType.Crm),
        Route("Subscribe", "Subscribe", CapabilityType.Subscribe),
        Route("Member", "Member", CapabilityType.Member),
        Route("Coupon", "Coupon", CapabilityType.Coupon)
    ];

    private static readonly CapabilityWorkflowRoute[] CapabilityWorkflowRoutes =
    [
        Workflow("Logistics", "LogisticsWorkflow"),
        Workflow("Invoice", "InvoiceWorkflow"),
        Workflow("Crm", "CrmWorkflow"),
        Workflow("Subscribe", "SubscribeWorkflow"),
        Workflow("Member", "MemberWorkflow"),
        Workflow("Coupon", "CouponWorkflow"),
        Workflow("HumanAgent", "HumanHandoffWorkflow")
    ];

    private static readonly WorkflowPermissionDefinition[] WorkflowPermissions =
    [
        Permission("RefundWorkflow", ["QueryOrder", "CheckRefundRule", "WaitUserConfirm", "ExecuteRefund", "RestoreCoupon", "SendNotification"], ["Order.GetOrder", "Finance.Refund", "Member.RestoreCoupon", "WeCom.SendMessage"]),
        Permission("ManualRefundWorkflow", ["QueryOrder", "CheckRefundRule", "WaitUserConfirm", "HumanHandoff"], ["Order.GetOrder"]),
        Permission("LogisticsWorkflow", ["QueryLogistics", "SendNotification"], ["Logistics.Query", "WeCom.SendMessage"]),
        Permission("InvoiceWorkflow", ["CreateInvoice", "SendNotification"], ["Invoice.Create", "WeCom.SendMessage"]),
        Permission("CrmWorkflow", ["AddCrmTag"], ["CRM.AddTag"]),
        Permission("SubscribeWorkflow", ["UpdateSubscription", "SendNotification"], ["Subscribe.Update", "WeCom.SendMessage"]),
        Permission("MemberWorkflow", ["QueryMember"], ["Member.GetMember"]),
        Permission("CouponWorkflow", ["IssueCoupon", "SendNotification"], ["Coupon.Issue", "WeCom.SendMessage"]),
        Permission("HumanHandoffWorkflow", ["SearchKnowledge", "HumanHandoff"], [])
    ];

    public Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentDefinition>>(IntentDefinitions);
    }

    public Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<IntentCapabilityRoute>>(IntentCapabilityRoutes);
    }

    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> ICapabilityWorkflowRouteProvider.GetRoutesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityWorkflowRoute>>(CapabilityWorkflowRoutes);
    }

    public Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        WorkflowPermissionDefinition? permission = WorkflowPermissions.FirstOrDefault(
            item => string.Equals(item.WorkflowName, workflowName, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(permission);
    }

    private static IntentCapabilityRoute Route(string intentKey, string capabilityKey, CapabilityType capability)
    {
        return new IntentCapabilityRoute(intentKey, capabilityKey, capability, $"{intentKey} -> {capabilityKey}");
    }

    private static CapabilityWorkflowRoute Workflow(string capabilityKey, string workflowName)
    {
        return new CapabilityWorkflowRoute(capabilityKey, workflowName, $"{capabilityKey} -> {workflowName}");
    }

    private static WorkflowPermissionDefinition Permission(
        string workflowName,
        IReadOnlyCollection<string> businessActions,
        IReadOnlyCollection<string> tools)
    {
        return new WorkflowPermissionDefinition(workflowName, businessActions, tools);
    }
}
