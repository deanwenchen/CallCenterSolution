namespace CallCenter.Domain;

public enum IntentType
{
    Unknown,
    Refund,
    Logistics,
    Invoice,
    Crm,
    Subscribe,
    Member,
    Coupon
}

public enum CapabilityType
{
    Unknown,
    Refund,
    Logistics,
    Invoice,
    Crm,
    Subscribe,
    Member,
    Coupon,
    HumanAgent
}

public enum WorkflowStatus
{
    NotStarted,
    Running,
    WaitingForHuman,
    Completed,
    Failed
}

public enum BusinessActionExecutionStatus
{
    Succeeded,
    Failed,
    AwaitingHumanInput,
    Skipped
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public sealed record SessionContext(
    string SessionId,
    string UserId,
    string Channel,
    string TenantId,
    bool IsAuthenticated,
    string CorrelationId,
    Dictionary<string, string>? Attributes = null);

public sealed record IntentResult(
    IntentType Intent,
    double Confidence,
    Dictionary<string, string> Entities);

public sealed record CapabilitySelection(
    CapabilityType Capability,
    string Reason);

public sealed record WorkflowSelection(
    string WorkflowName,
    CapabilityType Capability,
    string Reason);

public sealed record WorkflowDefinition(
    string Name,
    CapabilityType Capability,
    IReadOnlyList<WorkflowStepDefinition> Steps,
    IReadOnlyList<WorkflowEdgeDefinition> Edges,
    string StateModel);

public sealed record WorkflowStepDefinition(
    string Name,
    string BusinessActionName,
    TimeSpan Timeout,
    int MaxRetries,
    string? CompensationBusinessActionName = null,
    bool HumanInTheLoop = false);

public sealed record WorkflowEdgeDefinition(
    string FromStep,
    string ToStep,
    string? Condition = null);

public sealed record WorkflowState(
    string WorkflowInstanceId,
    string SessionId,
    string WorkflowName,
    WorkflowStatus Status,
    string? CurrentStep,
    string? CheckpointSessionId,
    string? CheckpointId,
    Dictionary<string, string> Data);

public sealed record ConversationRequest(
    string SessionId,
    string UserId,
    string Channel,
    string TenantId,
    string Message,
    string? AuthToken = null,
    Dictionary<string, string>? Metadata = null);

public sealed record ConversationResponse(
    string SessionId,
    string WorkflowInstanceId,
    WorkflowStatus Status,
    string Message,
    string? WorkflowName,
    string? CurrentStep,
    Dictionary<string, string> Data);

public sealed record WorkflowExecutionRequest(
    SessionContext Session,
    string WorkflowName,
    string Message,
    Dictionary<string, string> Entities,
    Dictionary<string, string> Data,
    string? StartStep = null);

public sealed record WorkflowExecutionResult(
    string WorkflowInstanceId,
    string WorkflowName,
    WorkflowStatus Status,
    string? CurrentStep,
    string? Message,
    string? CheckpointSessionId,
    string? CheckpointId,
    Dictionary<string, string> Data);

public sealed record BusinessActionContext(
    SessionContext Session,
    string WorkflowName,
    string StepName,
    string Message,
    Dictionary<string, string> Data);

public sealed record BusinessActionResult(
    string StepName,
    BusinessActionExecutionStatus Status,
    string? UserMessage,
    Dictionary<string, string> Data,
    SessionContext? Session = null,
    string? WorkflowName = null,
    bool RequiresHumanInput = false)
{
    public bool CanContinue => Status == BusinessActionExecutionStatus.Succeeded && !RequiresHumanInput;
}

public sealed record ExternalSystemCall<TRequest>(
    string SystemName,
    string OperationName,
    TRequest Request,
    string CorrelationId);

public sealed record OrderSnapshot(
    string OrderId,
    decimal Amount,
    string Status,
    bool Paid,
    bool CouponUsed);

public sealed record RefundDecision(
    bool Approved,
    RiskLevel RiskLevel,
    string Reason);

public sealed record RefundReceipt(
    string RefundId,
    string OrderId,
    decimal Amount,
    string Status);

public sealed record LogisticsSnapshot(
    string OrderId,
    string Carrier,
    string TrackingNo,
    string Status);

public sealed record NotificationReceipt(
    string MessageId,
    string Channel,
    string Status);

public sealed record CrmTagReceipt(
    string UserId,
    string Tag,
    string Status);
