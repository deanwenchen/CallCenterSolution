namespace CallCenter.Shared.Models;

/// <summary>
/// 订单信息。工作流执行器用它来校验退款/换货资格。
/// Status 状态："delivered"（已签收）、"shipped"（已发货）、"pending"（处理中）等。
/// Category 品类："custom"（定制商品，不可退）等。
/// HasCoupon：订单是否使用了优惠券（退款时需恢复）。
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
