using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace CallCenter.Framework.Safety;

/// <summary>
/// 安全管道代理客户端。对输入和输出同时应用安全过滤器。
/// 作为 StandardPipeline 的最外层（输入）和最内层（输出）使用。
/// 注意：StandardPipelineFactory 现在使用分离的 SafetyInputDelegatingClient 和
/// SafetyOutputDelegatingClient 替代此类，以实现更细粒度的管道控制。
/// 此类保留用于向后兼容。
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
