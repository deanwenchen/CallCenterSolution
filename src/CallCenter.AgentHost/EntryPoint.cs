using System.ClientModel;
using System.Text.Json;
using CallCenter.Framework.Parsing;
using CallCenter.Framework.Session;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CallCenter.AgentHost;

public record IntentResult(string Intent, string? Workflow, string? OrderId);

public record ProcessResult
{
    public static ProcessResult ResumeExisting() => new ResumeExistingResult();
    public static ProcessResult StartWorkflow(RefundIntent initialMessage) => new StartWorkflowResult(initialMessage);
    public static ProcessResult NoIntent(string response) => new NoIntentResult(response);
}

public record ResumeExistingResult : ProcessResult;
public record StartWorkflowResult(RefundIntent InitialMessage) : ProcessResult;
public record NoIntentResult(string Response) : ProcessResult;

public class EntryPoint
{
    private readonly AIAgent _intentAgent;
    private readonly InMemorySessionStore _sessionStore;

    public EntryPoint(IChatClient chatClient, InMemorySessionStore sessionStore)
    {
        _sessionStore = sessionStore;

        var systemPrompt = """
            你是一个意图识别助手。分析用户消息，判断意图。返回JSON格式: {"intent": "refund"|"greeting"|"unknown", "workflow": "RefundWorkflow", "orderId": "<如果提到订单号>"}. 只返回JSON，不要其他内容。
            """;

        _intentAgent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = systemPrompt,
                }
            });
    }

    public async Task<IntentResult?> RecognizeIntentAsync(string userMessage, CancellationToken ct = default)
    {
        try
        {
            var response = await _intentAgent.RunAsync(userMessage, cancellationToken: ct);

            if (string.IsNullOrWhiteSpace(response?.Text))
                return null;

            var parser = new StructuredOutputParser<IntentResult>();
            return parser.Parse(response.Text);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetActiveWorkflowAsync(string sessionId, CancellationToken ct = default)
    {
        return await _sessionStore.GetAsync<string>("activeWorkflow", sessionId, ct);
    }

    public async Task SetActiveWorkflowAsync(string sessionId, string workflowName, CancellationToken ct = default)
    {
        await _sessionStore.SetAsync("activeWorkflow", workflowName, sessionId, ct);
    }

    public async Task ClearActiveWorkflowAsync(string sessionId, CancellationToken ct = default)
    {
        await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct);
    }

    public async Task<ProcessResult> ProcessAsync(
        string sessionId,
        string userMessage,
        Workflow refundWorkflow,
        CancellationToken ct = default)
    {
        var activeWorkflow = await GetActiveWorkflowAsync(sessionId, ct);

        if (!string.IsNullOrEmpty(activeWorkflow))
        {
            if (activeWorkflow == "RefundWorkflow")
            {
                return ProcessResult.ResumeExisting();
            }
            await ClearActiveWorkflowAsync(sessionId, ct);
        }

        var intent = await RecognizeIntentAsync(userMessage, ct);

        if (intent == null || intent.Intent is "unknown" or "greeting")
        {
            var response = intent?.Intent switch
            {
                "greeting" => "你好！有什么可以帮助你的？",
                _ => "抱歉，我不太明白。你可以说'我要退款'来开始退款流程。",
            };
            return ProcessResult.NoIntent(response);
        }

        if (intent.Intent == "refund")
        {
            await SetActiveWorkflowAsync(sessionId, "RefundWorkflow", ct);
            return ProcessResult.StartWorkflow(new RefundIntent(intent.OrderId, "U100"));
        }

        return ProcessResult.NoIntent("抱歉，我不太明白。你可以说'我要退款'来开始退款流程。");
    }
}
