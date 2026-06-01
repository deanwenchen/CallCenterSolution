using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CallCenter.Shared.Mcp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.AgentHost.Skills;

[Experimental("MAAI001")]
public sealed class RefundSkill : AgentClassSkill<RefundSkill>
{
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "refund",
        "处理用户退款请求。当用户要求退款、退货、取消订单时使用。" +
        "支持查询订单、校验退款资格、计算退款金额、执行退款。");

    protected override string Instructions => """
        当用户要求退款时使用此技能。

        1. 获取订单号（如果用户未提供，使用 get_recent_orders 脚本获取最近订单）
        2. 系统将自动处理退款流程，包括资格校验、金额计算、用户确认
        3. 退款完成后通知用户结果
        """;

    [AgentSkillScript("get_recent_orders")]
    [Description("获取用户最近的订单列表。")]
    private static async Task<string> GetRecentOrders(
        string? userId,
        IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IOrderMcpClient>();
        var orders = await client.GetRecentOrdersAsync(userId ?? "U100");
        return JsonSerializer.Serialize(orders);
    }

    [AgentSkillScript("execute_refund")]
    [Description("执行退款操作。")]
    private static async Task<string> ExecuteRefund(
        string orderId,
        decimal amount,
        IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IFinanceMcpClient>();
        var result = await client.RefundAsync(orderId, amount);
        return JsonSerializer.Serialize(result);
    }
}
