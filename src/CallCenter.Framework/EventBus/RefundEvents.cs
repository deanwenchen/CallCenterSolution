namespace CallCenter.Framework.EventBus;

/// <summary>
/// Emitted when a refund workflow completes successfully.
/// Subscribers (e.g., notification services, analytics) can react without being coupled to the workflow.
/// </summary>
public record RefundCompletedEvent(
    string SessionId,
    string UserId,
    string OrderId,
    decimal RefundAmount);

/// <summary>
/// Emitted when a risk condition is detected during any workflow.
/// Used for monitoring, alerting, and compliance tracking.
/// </summary>
public record RiskAlertEvent(
    string SessionId,
    string UserId,
    string OrderId,
    string AlertType,
    string Details);
