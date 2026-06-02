using System;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Framework.Compaction;

/// <summary>
/// 对话压缩扩展。
/// 主要作用：在上下文过长时自动压缩历史对话，避免 LLM 超出 token 上限，同时尽量保留近期上下文。
/// 采用双策略：
///   1. Summarization — 超过 8000 token 时将旧对话压缩为摘要（温和策略）
///   2. SlidingWindow — 超过 8 轮时丢弃最早的消息（激进策略）
/// 使用较小的模型（qwen-plus）进行压缩，节省成本。
/// </summary>
public static class CompactionExtensions
{
    public static IServiceCollection AddCallCenterCompaction(
        this IServiceCollection services,
        string summarizerModel = "qwen-plus")
    {
        // CompactionProvider requires a ChatClient for summarization at runtime.
        // The actual wiring happens in StandardPipelineFactory where both clients are available.
        // This registration point exists for DI consistency but the provider is constructed inline.
        return services;
    }

    public static ChatClientBuilder UseCallCenterCompaction(
        this ChatClientBuilder builder,
        IChatClient summarizerClient,
        int tokenThreshold = 8000,
        int preserveRecentTurns = 8)
    {
        var pipeline = new PipelineCompactionStrategy(
            // Mild: summarize old conversation fragments
            new SummarizationCompactionStrategy(
                summarizerClient,
                CompactionTriggers.TokensExceed(tokenThreshold),
                summarizationPrompt: "请用一句话简洁总结以下对话内容："),
            // Aggressive: keep only recent turns
            new SlidingWindowCompactionStrategy(
                CompactionTriggers.TurnsExceed(preserveRecentTurns)));

        return builder.UseAIContextProviders(new CompactionProvider(pipeline));
    }
}

/// <summary>压缩配置。控制压缩阈值（8000 token）、保留轮数（8 轮）和压缩模型（qwen-plus）。</summary>
public class CompactionOptions
{
    public int TokenThreshold { get; set; } = 8000;
    public int PreserveRecentTurns { get; set; } = 8;
    public string SmallModel { get; set; } = "qwen-plus";
}
