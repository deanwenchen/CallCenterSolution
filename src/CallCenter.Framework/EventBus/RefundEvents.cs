namespace CallCenter.Framework.EventBus;

public record RefundCompletedEvent(
    string SessionId,
    string UserId,
    string OrderId,
    decimal RefundAmount);

public record RiskAlertEvent(
    string SessionId,
    string UserId,
    string OrderId,
    string AlertType,
    string Details);
