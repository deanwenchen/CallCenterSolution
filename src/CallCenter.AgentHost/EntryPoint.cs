using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CallCenter.Framework.Parsing;
using CallCenter.Framework.Session;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.AgentHost;

/// <summary>意图识别结果。LLM 返回的通用 JSON 格式。</summary>
public record IntentResult(
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("parameters")] Dictionary<string, string?> Parameters);

/// <summary>EntryPoint.ProcessAsync 的返回类型。</summary>
public record ProcessResult
{
    public static ProcessResult ResumeExisting(Workflow workflow) => new ResumeExistingResult(workflow);
    public static ProcessResult StartWorkflow(RefundIntent initialMessage, Workflow workflow) => new StartWorkflowResult(initialMessage, workflow);
    public static ProcessResult NoIntent(string response) => new NoIntentResult(response);
    public static ProcessResult TimeoutWarning(string msg) => new TimeoutResult(true, msg);
    public static ProcessResult TimeoutTerminate(string msg) => new TimeoutResult(false, msg);
    public static ProcessResult IntentSwitch(string oldWf, string newIntent) => new IntentSwitchResult(oldWf, newIntent);
}

public record ResumeExistingResult(Workflow Workflow) : ProcessResult;
public record StartWorkflowResult(RefundIntent InitialMessage, Workflow Workflow) : ProcessResult;
public record NoIntentResult(string Response) : ProcessResult;
public record TimeoutResult(bool IsWarning, string Message) : ProcessResult;
public record IntentSwitchResult(string OldWorkflow, string NewIntent) : ProcessResult;

/// <summary>
/// 用户输入处理入口。
/// 主要作用：承接用户每次输入，完成意图识别、会话超时检查、工作流路由与切换控制。
/// 路由逻辑：直观 if/switch，好读好改。
/// </summary>
[Experimental("MAAI001")]
public class EntryPoint
{
    private readonly AIAgent _intentAgent;
    private readonly InMemorySessionStore _sessionStore;

    public EntryPoint(AIAgentFactory factory, InMemorySessionStore sessionStore, AgentSkillsProvider? skillsProvider = null)
    {
        _sessionStore = sessionStore;
        _intentAgent = factory.CreateIntentAgent(skillsProvider);
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

    public async Task<string?> GetActiveWorkflowAsync(string sessionId, CancellationToken ct = default) =>
        await _sessionStore.GetAsync<string>("activeWorkflow", sessionId, ct);

    public async Task SetActiveWorkflowAsync(string sessionId, string workflowName, CancellationToken ct = default) =>
        await _sessionStore.SetAsync("activeWorkflow", workflowName, scope: sessionId, ct: ct);

    public async Task ClearActiveWorkflowAsync(string sessionId, CancellationToken ct = default) =>
        await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct);

    public async Task<DateTime?> GetLastActivityAsync(string sessionId, CancellationToken ct = default) =>
        await _sessionStore.GetAsync<DateTime>("lastActivity", sessionId, ct);

    public async Task<ProcessResult?> CheckTimeoutAsync(string sessionId, CancellationToken ct = default)
    {
        var lastActivity = await GetLastActivityAsync(sessionId, ct);
        if (lastActivity == null) return null;

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
        await _sessionStore.SetAsync("lastActivity", DateTime.UtcNow, scope: sessionId, ct: ct);

        var timeoutResult = await CheckTimeoutAsync(sessionId, ct);
        if (timeoutResult != null) return timeoutResult;

        var activeWorkflow = await GetActiveWorkflowAsync(sessionId, ct);

        // 有活跃工作流时的处理
        if (!string.IsNullOrEmpty(activeWorkflow))
        {
            var intent = await RecognizeIntentAsync(userMessage, ct);

            // 非业务意图 — 不中断当前流程
            if (intent == null || !IntentRegistry.IsBusinessIntent(intent.Intent))
            {
                var response = intent?.Intent == "greeting"
                    ? "你好！有什么可以帮助你的？"
                    : "抱歉，我不太明白。";
                return ProcessResult.NoIntent(response);
            }

            // 意图与当前工作流匹配 — 恢复
            var currentIntent = GetIntentForWorkflow(activeWorkflow);
            if (currentIntent != null && intent.Intent == currentIntent)
            {
                return ProcessResult.ResumeExisting(refundWorkflow);
            }

            // 意图切换 — 终止旧流程
            await ClearActiveWorkflowAsync(sessionId, ct);
            return ProcessResult.IntentSwitch(activeWorkflow, intent.Intent);
        }

        // 无活跃工作流 — 识别意图并启动
        var newIntent = await RecognizeIntentAsync(userMessage, ct);

        if (newIntent == null || !IntentRegistry.IsBusinessIntent(newIntent.Intent))
        {
            var response = newIntent?.Intent == "greeting"
                ? "你好！有什么可以帮助你的？"
                : "抱歉，我不太明白。";
            return ProcessResult.NoIntent(response);
        }

        // 路由：根据意图名启动对应工作流
        switch (newIntent.Intent)
        {
            case "refund":
            {
                var orderId = newIntent.Parameters.GetValueOrDefault("OrderId");
                await SetActiveWorkflowAsync(sessionId, "RefundWorkflow", ct);
                return ProcessResult.StartWorkflow(new RefundIntent(orderId, "U100"), refundWorkflow);
            }
            // 新增意图：
            // case "complaint":
            //     var orderId2 = newIntent.Parameters.GetValueOrDefault("OrderId");
            //     var issue = newIntent.Parameters.GetValueOrDefault("Issue");
            //     await SetActiveWorkflowAsync(sessionId, "ComplaintWorkflow", ct);
            //     return ProcessResult.StartWorkflow(new ComplaintIntent(orderId2, issue, "U100"));
        }

        return ProcessResult.NoIntent("抱歉，无法启动该流程。");
    }

    /// <summary>根据工作流名称反查对应的意图名称。</summary>
    private static string? GetIntentForWorkflow(string workflowName) =>
        workflowName switch
        {
            "RefundWorkflow" => "refund",
            "ExchangeWorkflow" => "exchange",
            _ => null,
        };
}
