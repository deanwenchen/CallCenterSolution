using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Framework.Parsing;
using CallCenter.Framework.Session;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CallCenter.AgentHost;

/// <summary>
/// 意图识别结果。由 LLM 返回的 JSON 反序列化得到。
/// <example>
/// {"intent": "refund", "workflow": "RefundWorkflow", "orderId": "A001"}
/// </example>
/// </summary>
public record IntentResult(
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("orderId")] string? OrderId);

/// <summary>
/// EntryPoint.ProcessAsync 的返回类型，表示对用户输入的不同处理结果。
/// </summary>
public record ProcessResult
{
    /// <summary>恢复已存在的工作流（用户继续之前中断的流程）。</summary>
    public static ProcessResult ResumeExisting() => new ResumeExistingResult();
    /// <summary>启动新工作流（识别到业务意图，如"我要退款"）。</summary>
    public static ProcessResult StartWorkflow(RefundIntent initialMessage) => new StartWorkflowResult(initialMessage);
    /// <summary>无业务意图（闲聊或无法识别），返回自然语言回复。</summary>
    public static ProcessResult NoIntent(string response) => new NoIntentResult(response);
    /// <summary>超时警告（30 分钟无操作）。</summary>
    public static ProcessResult TimeoutWarning(string msg) => new TimeoutResult(true, msg);
    /// <summary>超时终止（60 分钟无操作，清除工作流状态）。</summary>
    public static ProcessResult TimeoutTerminate(string msg) => new TimeoutResult(false, msg);
    /// <summary>意图切换（用户在有活跃工作流时输入其他意图）。</summary>
    public static ProcessResult IntentSwitch(string oldWf, string newIntent) => new IntentSwitchResult(oldWf, newIntent);
}

/// <summary>用户回复"确认"或"取消"时的响应。</summary>
public record ResumeExistingResult : ProcessResult;
/// <summary>启动退款工作流的响应，携带初始意图信息。</summary>
public record StartWorkflowResult(RefundIntent InitialMessage) : ProcessResult;
/// <summary>无业务意图的响应，携带给用户的自然语言消息。</summary>
public record NoIntentResult(string Response) : ProcessResult;
/// <summary>超时响应，区分警告和终止两种状态。</summary>
public record TimeoutResult(bool IsWarning, string Message) : ProcessResult;
/// <summary>意图切换响应，记录旧工作流和新意图名称。</summary>
public record IntentSwitchResult(string OldWorkflow, string NewIntent) : ProcessResult;

/// <summary>
/// 用户输入处理入口。
/// 职责：1) 通过 LLM 识别用户意图 2) 管理会话超时 3) 路由到对应工作流。
/// 路由逻辑：
///   - 无活跃工作流 → 识别意图 → refund 启动 RefundWorkflow，否则回复自然语言
///   - 有活跃工作流 → 识别意图 → 相同意图则恢复，不同意图则终止旧流程，闲聊则回复不终止
/// </summary>
[Experimental("MAAI001")]
public class EntryPoint
{
    private readonly AIAgent _intentAgent;
    private readonly InMemorySessionStore _sessionStore;

    public EntryPoint(IChatClient chatClient, InMemorySessionStore sessionStore, AgentSkillsProvider? skillsProvider = null)
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
                },
                AIContextProviders = skillsProvider != null ? [skillsProvider] : null,
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
            return ProcessResult.TimeoutWarning("Warning: 30 minutes of inactivity. Session will terminate after 60 minutes of inactivity.");
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
