namespace CallCenter.Shared.Models;

public record RefundResult(
    string RefundId,
    string OrderId,
    decimal Amount,
    string Status,
    DateTime RefundDate,
    string Message = "");
