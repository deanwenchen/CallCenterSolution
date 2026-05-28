using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// 根据 ConversationRequest 创建 SessionContext 的默认实现。
/// </summary>
public sealed class ConversationContextFactory : IConversationContextFactory
{
    /// <summary>
    /// 从入口请求提取会话、租户、渠道、认证和链路追踪上下文。
    /// </summary>
    /// <param name="request">会话消息请求。</param>
    /// <returns>会话上下文。</returns>
    public SessionContext Create(ConversationRequest request)
    {
        return new SessionContext(
            request.SessionId,
            request.UserId,
            request.Channel,
            request.TenantId,
            IsAuthenticated: !string.IsNullOrWhiteSpace(request.AuthToken),
            CorrelationId: request.Metadata?.GetValueOrDefault("correlationId") ?? Guid.NewGuid().ToString("N"),
            Attributes: request.Metadata);
    }
}
