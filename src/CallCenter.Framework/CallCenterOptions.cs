using CallCenter.Framework.EventBus;

namespace CallCenter.Framework;

/// <summary>
/// CallCenter 框架配置选项。
/// 主要作用：集中管理 API 密钥、模型名称、端点等配置，
/// 并提供 ApplyDefaults() 作为唯一读取环境变量 DASHSCOPE_API_KEY 的入口。
/// </summary>
public class CallCenterOptions
{
    /// <summary>API 密钥。默认 null，ApplyDefaults() 会从环境变量读取。</summary>
    public string? ApiKey { get; set; }

    /// <summary>模型名称。默认 "qwen3-vl-flash"。</summary>
    public string ModelName { get; set; } = "qwen3-vl-flash";

    /// <summary>API 端点。默认 "https://dashscope.aliyuncs.com/compatible-mode/v1"。</summary>
    public string Endpoint { get; set; } = "https://dashscope.aliyuncs.com/compatible-mode/v1";

    /// <summary>是否使用 Mock 服务。默认 true（演示模式）。</summary>
    public bool UseMockServices { get; set; } = true;

    /// <summary>退款完成事件回调。可选。</summary>
    public Action<RefundCompletedEvent>? OnRefundCompleted { get; set; }

    /// <summary>
    /// 应用默认配置。这是唯一读取 DASHSCOPE_API_KEY 环境变量的地方。
    /// - 如果 ApiKey 未设置，从环境变量 DASHSCOPE_API_KEY 读取。
    /// - 如果 ModelName 仍为默认值，检查环境变量 DASHSCOPE_MODEL_NAME。
    /// </summary>
    public void ApplyDefaults()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            ApiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
        }

        if (ModelName == "qwen3-vl-flash")
        {
            var envModel = Environment.GetEnvironmentVariable("DASHSCOPE_MODEL_NAME");
            if (!string.IsNullOrEmpty(envModel))
            {
                ModelName = envModel;
            }
        }
    }
}
