using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace CallCenter.Framework.Safety;

/// <summary>
/// A DelegatingChatClient that applies safety filters to both input and output.
/// Applied as part of the StandardPipeline (outermost layer for input, innermost for output).
/// Note: StandardPipelineFactory now uses separate SafetyInputDelegatingClient and
/// SafetyOutputDelegatingClient wrappers instead of this class for finer-grained pipeline control.
/// This class is retained for backward compatibility.
/// </summary>
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
