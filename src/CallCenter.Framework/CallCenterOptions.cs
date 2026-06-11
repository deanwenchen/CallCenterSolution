using CallCenter.Framework.EventBus;

namespace CallCenter.Framework;

/// <summary>
/// LLM 厂商枚举。
/// </summary>
public enum LLMProvider
{
    /// <summary>通义千问（默认）</summary>
    DashScope,
    /// <summary>OpenAI GPT 系列</summary>
    OpenAI,
    /// <summary>Azure OpenAI（需 Azure.AI.OpenAI 包）</summary>
    AzureOpenAI,
    /// <summary>本地模型（Ollama 等）</summary>
    Ollama,
    /// <summary>DeepSeek</summary>
    DeepSeek,
    /// <summary>硅基流动</summary>
    SiliconFlow,
    /// <summary>自定义 OpenAI 协议端点</summary>
    Custom,
}

/// <summary>
/// CallCenter 框架配置选项。
/// 主要作用：集中管理 API 密钥、模型名称、端点、厂商等配置，
/// 并提供 ApplyDefaults() 作为唯一读取环境变量的入口。
/// </summary>
public class CallCenterOptions
{
    /// <summary>LLM 厂商。默认 DashScope，ApplyDefaults() 会按此自动填端点。</summary>
    public LLMProvider Provider { get; set; } = LLMProvider.DashScope;

    /// <summary>API 密钥。默认 null，ApplyDefaults() 会从环境变量读取。</summary>
    public string? ApiKey { get; set; }

    /// <summary>模型名称。默认 "qwen3-vl-flash"。</summary>
    public string ModelName { get; set; } = "qwen3-vl-flash";

    /// <summary>API 端点。null 时 ApplyDefaults() 按 Provider 自动填默认值。</summary>
    public string? Endpoint { get; set; }

    /// <summary>是否使用 Mock 服务。默认 true（演示模式）。</summary>
    public bool UseMockServices { get; set; } = true;

    /// <summary>退款完成事件回调。可选。</summary>
    public Action<RefundCompletedEvent>? OnRefundCompleted { get; set; }

    /// <summary>
    /// 应用默认配置。这是唯一读取 LLM 相关环境变量的地方。
    /// - ApiKey 未设置 → LLM_API_KEY → DASHSCOPE_API_KEY
    /// - ModelName 为默认值 → LLM_MODEL_NAME → DASHSCOPE_MODEL_NAME
    /// - Endpoint 为 null → 按 Provider 自动填默认端点
    /// </summary>
    public void ApplyDefaults()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            ApiKey = Environment.GetEnvironmentVariable("LLM_API_KEY")
                  ?? Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
        }

        if (ModelName == "qwen3-vl-flash")
        {
            var envModel = Environment.GetEnvironmentVariable("LLM_MODEL_NAME")
                        ?? Environment.GetEnvironmentVariable("DASHSCOPE_MODEL_NAME");
            if (!string.IsNullOrEmpty(envModel))
            {
                ModelName = envModel;
            }
        }

        // 按 Provider 自动填默认端点
        if (string.IsNullOrEmpty(Endpoint))
        {
            Endpoint = Provider switch
            {
                LLMProvider.DashScope    => "https://dashscope.aliyuncs.com/compatible-mode/v1",
                LLMProvider.OpenAI       => "https://api.openai.com/v1",
                LLMProvider.Ollama       => "http://localhost:11434/v1",
                LLMProvider.DeepSeek     => "https://api.deepseek.com/v1",
                LLMProvider.SiliconFlow  => "https://api.siliconflow.cn/v1",
                LLMProvider.AzureOpenAI  => null,
                LLMProvider.Custom       => null,
                _ => throw new NotSupportedException($"Provider {Provider} 未实现默认端点映射"),
            };

            if (string.IsNullOrEmpty(Endpoint) && Provider is LLMProvider.AzureOpenAI or LLMProvider.Custom)
            {
                throw new InvalidOperationException($"Provider {Provider} 需要显式设置 Endpoint。");
            }
        }
    }
}
