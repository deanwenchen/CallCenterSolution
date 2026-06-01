using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace CallCenter.AgentHost.Skills;

[Experimental("MAAI001")]
public sealed class ExchangeSkill : AgentClassSkill<ExchangeSkill>
{
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "exchange",
        "处理用户换货请求。当用户要求换货、退货、更换商品时使用。" +
        "支持查询订单、校验换货资格、计算换货金额、执行换货。");

    protected override string Instructions => """
        当用户要求换货时使用此技能。

        1. 获取订单号（如果用户未提供，使用 get_recent_orders 获取最近订单）
        2. 系统将自动处理换货流程，包括资格校验、金额计算、用户确认
        3. 换货完成后通知用户结果
        """;

    // Skeleton — scripts will be implemented in v2
}
