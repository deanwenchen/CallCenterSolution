using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace CallCenter.Framework.ToolApproval;

public class ToolApprovalDelegatingClient : DelegatingChatClient
{
    private readonly IToolApprovalAgent _approvalAgent;
    private readonly string _sessionId;

    public ToolApprovalDelegatingClient(IChatClient inner, IToolApprovalAgent approvalAgent, string sessionId)
        : base(inner)
    {
        _approvalAgent = approvalAgent;
        _sessionId = sessionId;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Check tool approval before forwarding
        if (options?.Tools is { Count: > 0 })
        {
            foreach (var tool in options.Tools)
            {
                var toolName = tool.ToString() ?? "unknown";
                var approved = await _approvalAgent.IsApprovedAsync(toolName, options, _sessionId, cancellationToken);
                if (!approved)
                {
                    return new ChatResponse([new ChatMessage(ChatRole.Assistant, "[Tool call blocked by approval policy]")]);
                }
            }
        }

        return await base.GetResponseAsync(messages, options, cancellationToken);
    }
}
