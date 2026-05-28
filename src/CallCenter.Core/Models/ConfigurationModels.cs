namespace CallCenter.Core;

/// <summary>
/// 一条可配置意图规则。
/// </summary>
public sealed record IntentDefinition(
    string Key,
    IntentType Intent,
    IReadOnlyCollection<string> Keywords,
    double Confidence);

/// <summary>
/// 意图到能力的可配置路由。
/// </summary>
public sealed record IntentCapabilityRoute(
    string IntentKey,
    string CapabilityKey,
    CapabilityType Capability,
    string Reason);

/// <summary>
/// 能力到 Workflow 的可配置路由。
/// </summary>
public sealed record CapabilityWorkflowRoute(
    string CapabilityKey,
    string WorkflowName,
    string Reason);

/// <summary>
/// Workflow 可以调用哪些 BusinessAction 和 Tool 的配置。
/// </summary>
public sealed record WorkflowPermissionDefinition(
    string WorkflowName,
    IReadOnlyCollection<string> BusinessActions,
    IReadOnlyCollection<string> Tools);
