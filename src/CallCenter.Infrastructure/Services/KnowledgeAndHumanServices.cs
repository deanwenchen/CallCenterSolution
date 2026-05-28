using System.Collections.Concurrent;
using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 本地内存版知识库服务，提供少量固定知识条目用于验证知识检索链路。
/// </summary>
public sealed class InMemoryKnowledgeService : IKnowledgeService
{
    private static readonly KnowledgeEntry[] Entries =
    [
        new("refund-policy", "Refund policy", "Paid orders below 1000 can usually be refunded automatically after user confirmation.", ["refund", "policy"]),
        new("invoice-policy", "Invoice policy", "Invoices require an order id and invoice title before the invoice workflow can submit a request.", ["invoice", "tax"]),
        new("logistics-help", "Logistics help", "Logistics status is queried by order id and returned with carrier and tracking number.", ["logistics", "delivery"]),
        new("coupon-help", "Coupon help", "Coupons can be restored or issued through the member/coupon system when policy allows it.", ["coupon", "member"])
    ];

    /// <summary>
    /// 根据查询文本匹配知识标签，并返回摘要。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="query">检索文本。</param>
    /// <param name="entities">已抽取实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>知识检索结果。</returns>
    public Task<KnowledgeSearchResult> SearchAsync(
        SessionContext session,
        string query,
        Dictionary<string, string> entities,
        CancellationToken cancellationToken = default)
    {
        string normalized = query.ToLowerInvariant();
        KnowledgeEntry[] matches = Entries
            .Where(entry =>
                entry.Tags.Any(tag => normalized.Contains(tag, StringComparison.OrdinalIgnoreCase)) ||
                normalized.Contains(entry.Title, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToArray();

        if (matches.Length == 0)
        {
            matches = Entries.Take(1).ToArray();
        }

        string summary = string.Join(" ", matches.Select(match => match.Content));
        return Task.FromResult(new KnowledgeSearchResult(matches, summary));
    }
}

/// <summary>
/// 本地内存版人工客服服务，用于创建模拟人工工单。
/// </summary>
public sealed class InMemoryHumanAgentService : IHumanAgentService
{
    private readonly ConcurrentDictionary<string, HumanAgentTicket> _tickets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 创建并保存一个模拟人工客服工单。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="reason">转人工原因。</param>
    /// <param name="data">当前业务数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>人工客服工单。</returns>
    public Task<HumanAgentTicket> CreateTicketAsync(
        SessionContext session,
        string reason,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        string ticketId = $"HUM-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var ticket = new HumanAgentTicket(ticketId, session.SessionId, session.UserId, reason, "open");
        _tickets[ticketId] = ticket;
        return Task.FromResult(ticket);
    }
}
