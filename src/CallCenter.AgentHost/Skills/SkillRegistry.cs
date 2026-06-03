#pragma warning disable MAAI001
using CallCenter.AgentHost.Skills;
using Microsoft.Agents.AI;

namespace CallCenter.AgentHost;

/// <summary>
/// 技能注册表。
/// 主要作用：集中管理所有 Skill 实例，其他地方统一从此处读取，避免多处 new。
/// 以后加新技能只改这一个 List。
/// </summary>
public static class SkillRegistry
{
    public static AgentSkill[] All { get; } =
    [
        new RefundSkill(),
        new ExchangeSkill(),
    ];
}
