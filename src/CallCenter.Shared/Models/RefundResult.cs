namespace CallCenter.Shared.Models;

/// <summary>
/// 退款操作结果。由 IFinanceMcpClient.RefundAsync 返回。
/// 包含退款单号、金额、状态和消息。
/// </summary>
public record RefundResult(
    string RefundId,
    string OrderId,
    decimal Amount,
    string Status,
    DateTime RefundDate,
    string Message = "");
