using System.ClientModel;
using CallCenter.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using MeaiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using MeaiChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

namespace CallCenter.Infrastructure;

/// <summary>
/// Qwen/DashScope model client built on Microsoft Agent Framework.
/// </summary>
public sealed class AgentFrameworkQwenModelClient(QwenModelClientOptions options) : IModelClient
{
    public async Task<ModelChatResponse> CompleteAsync(
        ModelChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new ModelChatResponse(false, null, "Qwen model client is disabled because ApiKey is empty.");
        }

        if (!Uri.TryCreate(NormalizeEndpoint(options.Endpoint), UriKind.Absolute, out Uri? endpoint))
        {
            return new ModelChatResponse(false, null, $"Invalid Qwen endpoint: {options.Endpoint}");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds)));

        try
        {
            AIAgent agent = CreateAgent(endpoint);
            AgentResponse response = await agent
                .RunAsync(ToChatMessages(request), options: CreateRunOptions(request), cancellationToken: timeoutCts.Token)
                .ConfigureAwait(false);

            return string.IsNullOrWhiteSpace(response.Text)
                ? new ModelChatResponse(false, null, "Qwen agent response did not contain text.")
                : new ModelChatResponse(true, response.Text);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ModelChatResponse(false, null, "Qwen model request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or ArgumentException)
        {
            return new ModelChatResponse(false, null, ex.Message);
        }
    }

    private AIAgent CreateAgent(Uri endpoint)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(options.ApiKey!),
            new OpenAIClientOptions { Endpoint = endpoint });

        return openAiClient
            .GetChatClient(options.Model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "callcenter-qwen-intent-agent",
                Description = "Classifies call center user intent and extracts entities with Qwen.",
                ChatOptions = new ChatOptions
                {
                    Temperature = (float)options.Temperature,
                    ResponseFormat = MeaiChatResponseFormat.Json
                }
            });
    }

    private static ChatClientAgentRunOptions CreateRunOptions(ModelChatRequest request)
    {
        return new ChatClientAgentRunOptions(new ChatOptions
        {
            ResponseFormat = request.ResponseFormat == "json_object"
                ? MeaiChatResponseFormat.Json
                : MeaiChatResponseFormat.Text
        });
    }

    private static IReadOnlyCollection<MeaiChatMessage> ToChatMessages(ModelChatRequest request)
    {
        return request.Messages.Select(message => new MeaiChatMessage(
            ToChatRole(message.Role),
            message.Content)).ToArray();
    }

    private static ChatRole ToChatRole(ModelMessageRole role)
    {
        return role switch
        {
            ModelMessageRole.System => ChatRole.System,
            ModelMessageRole.User => ChatRole.User,
            ModelMessageRole.Assistant => ChatRole.Assistant,
            _ => ChatRole.User
        };
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        string normalized = endpoint.Trim();
        const string chatCompletionsPath = "/chat/completions";
        return normalized.EndsWith(chatCompletionsPath, StringComparison.OrdinalIgnoreCase)
            ? normalized[..^chatCompletionsPath.Length]
            : normalized;
    }
}
