namespace CallCenter.Shared.Models;

/// <summary>
/// Result of a refund operation. Returned by IFinanceMcpClient.RefundAsync.
/// </summary>
public record RefundResult(
    string RefundId,
    string OrderId,
    decimal Amount,
    string Status,
    DateTime RefundDate,
    string Message = "");
