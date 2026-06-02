namespace CallCenter.Shared.Models;

/// <summary>
/// Represents an order in the system.
/// Used by workflow executors to validate refund/exchange eligibility.
/// </summary>
public record OrderInfo(
    string OrderId,
    string UserId,
    string ProductName,
    decimal Amount,
    DateTime OrderDate,
    string Status,        // "delivered", "shipped", "pending", etc.
    string Category,      // "electronics", "custom", etc. Custom items are non-refundable.
    bool HasCoupon = false);
