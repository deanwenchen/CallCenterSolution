// TODO: PRD Section 7.3.3 — Compaction (8000 token threshold, preserve 8 turns, small model summary)
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Framework.Compaction;

public static class CompactionExtensions
{
    public static IServiceCollection AddCallCenterCompaction(this IServiceCollection services)
    {
        // TODO: Implement compaction with MAF CompactionProvider
        // Default: 8000 token threshold, preserve 8 recent turns, small model summary
        return services;
    }

    public static CompactionBuilder UseSummarization(this IServiceCollection services, Action<CompactionOptions>? configure = null)
    {
        var options = new CompactionOptions();
        configure?.Invoke(options);
        // TODO: Configure MAF CompactionProvider with options
        return new CompactionBuilder();
    }
}

public class CompactionBuilder
{
    public CompactionBuilder WithTokenThreshold(int threshold) => this;
    public CompactionBuilder PreserveRecentTurns(int turns) => this;
    public CompactionBuilder WithSmallModel(string model) => this;
}

public class CompactionOptions
{
    public int TokenThreshold { get; set; } = 8000;
    public int PreserveRecentTurns { get; set; } = 8;
    public string SmallModel { get; set; } = "gpt-4o-mini";
}
