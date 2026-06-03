#pragma warning disable MAAI001
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CallCenter.AgentHost;

/// <summary>
/// AI Agent 工厂。
/// 主要作用：根据场景创建不同配置的 AIAgent 实例，
/// 共享同一个 IChatClient pipeline（安全、日志、压缩层保持一致）。
/// </summary>
public class AIAgentFactory
{
    private readonly IChatClient _pipelineClient;

    /// <summary>
    /// 创建工厂实例。
    /// </summary>
    /// <param name="pipelineClient">6 层管道客户端（安全 → 日志 → 压缩 → 工具审批 → LLM → 安全输出）</param>
    public AIAgentFactory(IChatClient pipelineClient)
    {
        _pipelineClient = pipelineClient;
    }

    /// <summary>
    /// 创建意图识别 Agent。
    /// System Prompt 由 IntentRegistry.BuildSystemPrompt() 生成，用于识别用户输入的业务意图。
    /// </summary>
    /// <param name="skillsProvider">可选的技能提供者，用于暴露技能描述给 LLM</param>
    /// <returns>配置好 IntentRegistry System Prompt 的 AIAgent</returns>
    public AIAgent CreateIntentAgent(AgentSkillsProvider? skillsProvider = null)
    {
        return new ChatClientAgent(
            _pipelineClient,
            new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = IntentRegistry.BuildSystemPrompt(),
                },
                AIContextProviders = skillsProvider != null ? [skillsProvider] : null,
            });
    }

    /// <summary>
    /// 创建工作流对话 Agent。
    /// 用于在工作流执行过程中与用户进行自然语言对话（如确认退款、补充订单信息等）。
    /// </summary>
    /// <param name="skillsProvider">可选的技能提供者</param>
    /// <returns>配置好工作流对话 System Prompt 的 AIAgent</returns>
    public AIAgent CreateDialogAgent(AgentSkillsProvider? skillsProvider = null)
    {
        return new ChatClientAgent(
            _pipelineClient,
            new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = GetDialogSystemPrompt(),
                },
                AIContextProviders = skillsProvider != null ? [skillsProvider] : null,
            });
    }

    private static string GetDialogSystemPrompt()
    {
        return "你是一个专业的客服助手，帮助用户完成退款、换货等业务操作。请按照工作流程的指引，逐步协助用户完成操作。";
    }
}
