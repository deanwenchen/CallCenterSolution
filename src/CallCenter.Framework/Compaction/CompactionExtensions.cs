using System;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Framework.Compaction;

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

public class CompactionOptions
{
    public int TokenThreshold { get; set; } = 8000;
    public int PreserveRecentTurns { get; set; } = 8;
    public string SmallModel { get; set; } = "qwen-plus";
}
