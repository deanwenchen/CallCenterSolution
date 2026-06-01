using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace CallCenter.Framework.Safety;

public class SafetyPipelineAgent : DelegatingChatClient
{
    public SafetyPipelineAgent(IChatClient inner) : base(inner) { }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Apply safety input filter to all user messages
        var safeMessages = new List<ChatMessage>();
        foreach (var msg in messages)
        {
            if (msg.Role == ChatRole.User && msg.Text is not null)
            {
                var safeText = SafetyInputFilter.ProcessInput(msg.Text, "pipeline");
                safeMessages.Add(new ChatMessage(ChatRole.User, safeText));
            }
            else
            {
                safeMessages.Add(msg);
            }
        }

        var response = await base.GetResponseAsync(safeMessages, options, cancellationToken);

        // Apply safety output filter to assistant response
        if (response.Text is not null)
        {
            var safeOutput = SafetyOutputFilter.ProcessOutput(response.Text);
            return new ChatResponse([new ChatMessage(ChatRole.Assistant, safeOutput)])
            {
                FinishReason = response.FinishReason,
                Usage = response.Usage,
            };
        }

        return response;
    }
}
