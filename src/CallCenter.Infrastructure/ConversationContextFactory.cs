using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

public sealed class ConversationContextFactory : IConversationContextFactory
{
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
