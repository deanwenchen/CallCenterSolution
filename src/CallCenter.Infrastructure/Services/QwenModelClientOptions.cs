namespace CallCenter.Infrastructure;

/// <summary>
/// Qwen model client configuration.
/// </summary>
public sealed class QwenModelClientOptions
{
    internal const string DefaultEndpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1";

    /// <summary>DashScope OpenAI-compatible API endpoint.</summary>
    public string Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>DashScope API key.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Qwen model name.</summary>
    public string Model { get; set; } = "qwen3.6-plus";

    /// <summary>Sampling temperature. Use zero for deterministic intent classification.</summary>
    public double Temperature { get; set; } = 0;

    /// <summary>Single request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
