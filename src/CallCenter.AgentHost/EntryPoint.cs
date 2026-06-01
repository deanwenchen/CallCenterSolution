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
    public static ProcessResult TimeoutWarning(string msg) => new TimeoutResult(true, msg);
    public static ProcessResult TimeoutTerminate(string msg) => new TimeoutResult(false, msg);
    public static ProcessResult IntentSwitch(string oldWf, string newIntent) => new IntentSwitchResult(oldWf, newIntent);
}

public record ResumeExistingResult : ProcessResult;
public record StartWorkflowResult(RefundIntent InitialMessage) : ProcessResult;
public record NoIntentResult(string Response) : ProcessResult;
public record TimeoutResult(bool IsWarning, string Message) : ProcessResult;
public record IntentSwitchResult(string OldWorkflow, string NewIntent) : ProcessResult;

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

    public async Task<DateTime?> GetLastActivityAsync(string sessionId, CancellationToken ct = default)
    {
        return await _sessionStore.GetAsync<DateTime>("lastActivity", sessionId, ct);
    }

    public async Task<ProcessResult?> CheckTimeoutAsync(string sessionId, CancellationToken ct = default)
    {
        var lastActivity = await GetLastActivityAsync(sessionId, ct);
        if (lastActivity == null) return null; // First input, no timeout check

        var elapsed = DateTime.UtcNow - lastActivity.Value;

        if (elapsed.TotalMinutes >= 60)
        {
            await ClearActiveWorkflowAsync(sessionId, ct);
            return ProcessResult.TimeoutTerminate("Session terminated due to 60 minutes of inactivity.");
        }

        if (elapsed.TotalMinutes >= 30)
        {
            await ClearActiveWorkflowAsync(sessionId, ct);
            return ProcessResult.TimeoutWarning("Warning: 30 minutes of inactivity. Active workflow has been cleared. You can start a new conversation.");
        }

        return null;
    }

    public async Task<ProcessResult> ProcessAsync(
        string sessionId,
        string userMessage,
        Workflow refundWorkflow,
        CancellationToken ct = default)
    {
        // a. Set lastActivity timestamp (from Task 1)
        await _sessionStore.SetAsync("lastActivity", DateTime.UtcNow, sessionId, ct);

        // b. Check timeout first - if timeout result, return it immediately
        var timeoutResult = await CheckTimeoutAsync(sessionId, ct);
        if (timeoutResult != null)
        {
            return timeoutResult;
        }

        var activeWorkflow = await GetActiveWorkflowAsync(sessionId, ct);

        // c. Check if activeWorkflow exists
        if (!string.IsNullOrEmpty(activeWorkflow))
        {
            var intent = await RecognizeIntentAsync(userMessage, ct);

            // greeting/unknown during active workflow returns NoIntent without clearing workflow
            if (intent == null || intent.Intent is "unknown" or "greeting")
            {
                var response = intent?.Intent switch
                {
                    "greeting" => "你好！有什么可以帮助你的？",
                    _ => "抱歉，我不太明白。你可以说'我要退款'来开始退款流程。",
                };
                return ProcessResult.NoIntent(response);
            }

            // Same intent - resume existing workflow
            if (activeWorkflow == "RefundWorkflow" && intent.Intent == "refund")
            {
                return ProcessResult.ResumeExisting();
            }

            // Intent switch - terminate old workflow, return IntentSwitch result
            if (activeWorkflow == "RefundWorkflow" && intent.Intent != "refund")
            {
                await ClearActiveWorkflowAsync(sessionId, ct);
                return ProcessResult.IntentSwitch("RefundWorkflow", intent.Intent);
            }
        }

        // d. No activeWorkflow - recognize intent and start new workflow or reply naturally
        var newIntent = await RecognizeIntentAsync(userMessage, ct);

        if (newIntent == null || newIntent.Intent is "unknown" or "greeting")
        {
            var response = newIntent?.Intent switch
            {
                "greeting" => "你好！有什么可以帮助你的？",
                _ => "抱歉，我不太明白。你可以说'我要退款'来开始退款流程。",
            };
            return ProcessResult.NoIntent(response);
        }

        if (newIntent.Intent == "refund")
        {
            await SetActiveWorkflowAsync(sessionId, "RefundWorkflow", ct);
            return ProcessResult.StartWorkflow(new RefundIntent(newIntent.OrderId, "U100"));
        }

        return ProcessResult.NoIntent("抱歉，我不太明白。你可以说'我要退款'来开始退款流程。");
    }
}
