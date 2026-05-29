namespace CallCenter.Core;

/// <summary>
/// 模型消息角色。
/// </summary>
public enum ModelMessageRole
{
    System,
    User,
    Assistant
}

/// <summary>
/// 单条模型消息。
/// </summary>
/// <param name="Role">消息角色。</param>
/// <param name="Content">消息内容。</param>
public sealed record ModelChatMessage(ModelMessageRole Role, string Content);

/// <summary>
/// 模型聊天补全请求。
/// </summary>
/// <param name="Messages">上下文消息。</param>
/// <param name="ResponseFormat">期望响应格式，例如 json_object。</param>
public sealed record ModelChatRequest(
    IReadOnlyCollection<ModelChatMessage> Messages,
    string? ResponseFormat = null);

/// <summary>
/// 模型聊天补全结果。
/// </summary>
/// <param name="Succeeded">调用是否成功。</param>
/// <param name="Content">模型返回文本。</param>
/// <param name="FailureReason">失败原因，主要用于降级和排查。</param>
public sealed record ModelChatResponse(
    bool Succeeded,
    string? Content,
    string? FailureReason = null);
