namespace CallCenter.Shared.Models;

public record OrderInfo(
    string OrderId,
    string UserId,
    string ProductName,
    decimal Amount,
    DateTime OrderDate,
    string Status,
    string Category,
    bool HasCoupon = false);
