namespace CallCenter.Framework.EventBus;

/// <summary>
/// 退款完成事件。工作流成功执行后发布。
/// 订阅者（如通知服务、分析系统）可响应此事件，无需紧耦合到工作流。
/// </summary>
public record RefundCompletedEvent(
    string SessionId,
    string UserId,
    string OrderId,
    decimal RefundAmount);

/// <summary>
/// 风险预警事件。任何工作流检测到风险条件时发布。
/// 用于监控、告警和合规跟踪。
/// </summary>
public record RiskAlertEvent(
    string SessionId,
    string UserId,
    string OrderId,
    string AlertType,
    string Details);
