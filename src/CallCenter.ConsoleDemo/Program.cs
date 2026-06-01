#pragma warning disable MAAI001
using System.ClientModel;
using CallCenter.AgentHost;
using CallCenter.AgentHost.Skills;
using CallCenter.Framework;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Session;
using CallCenter.Shared.Mcp;
using CallCenter.Shared.Services;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")
    ?? throw new InvalidOperationException("DASHSCOPE_API_KEY not set. Set it to your DashScope API key.");
var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_MODEL_NAME") ?? "qwen3.6-plus";

// Create DashScope ChatClient (OpenAI compatible)
IChatClient chatClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
    .GetChatClient(modelName)
    .AsIChatClient();

// Register services
var orderService = new MockOrderService();
var financeService = new MockFinanceService();
var memberService = new MockMemberService();
var eventBus = new InMemoryBusinessEventBus();
var sessionStore = new InMemorySessionStore();
var checkpointManager = CheckpointManager.Default;

// Subscribe to events
eventBus.Subscribe<RefundCompletedEvent>(async e =>
{
    Console.WriteLine($"\n[EVENT] 退款完成: 订单{e.OrderId}, 金额 {e.RefundAmount:C}");
    await Task.CompletedTask;
});

// Build workflow
var refundWorkflow = RefundWorkflow.Build(orderService, financeService, memberService, eventBus);

// Create SkillsProvider with RefundSkill registered
var skillsProvider = new AgentSkillsProvider(new RefundSkill());

// Create EntryPoint
var entryPoint = new EntryPoint(chatClient, sessionStore, skillsProvider);

// Main chat loop
Console.WriteLine("=== CallCenter AI Demo ===");
Console.WriteLine("输入消息开始（如'我要退款，订单A001'），输入'quit'退出。\n");

var sessionId = "demo-session";

while (true)
{
    Console.Write("用户: ");
    var userMessage = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    // Process through EntryPoint
    var result = await entryPoint.ProcessAsync(sessionId, userMessage, refundWorkflow);

    switch (result)
    {
        case ResumeExistingResult:
            await ResumeWorkflow(refundWorkflow, userMessage, checkpointManager, sessionStore, sessionId);
            break;

        case TimeoutResult timeout:
            if (timeout.IsWarning)
            {
                Console.WriteLine($"\n[超时警告] {timeout.Message}");
                Console.WriteLine("您可以继续对话，或开始新的流程。");
            }
            else
            {
                Console.WriteLine($"\n[会话终止] {timeout.Message}");
            }
            break;

        case IntentSwitchResult switchResult:
            Console.WriteLine($"\n[系统] 已终止 {switchResult.OldWorkflow} 流程，正在处理新意图: {switchResult.NewIntent}");
            Console.WriteLine($"[系统] 意图 '{switchResult.NewIntent}' 暂未实现（Phase 3 仅支持退款流程）");
            break;

        case NoIntentResult noIntent:
            Console.WriteLine($"系统: {noIntent.Response}");
            break;

        case StartWorkflowResult start:
            Console.WriteLine($"[意图: refund, 订单: {start.InitialMessage.OrderId ?? "(未提供)"}]");
            await RunWorkflow(refundWorkflow, start.InitialMessage, checkpointManager, sessionStore, sessionId);
            break;
    }
}

static async Task RunWorkflow(Workflow workflow, RefundIntent initialMessage, CheckpointManager checkpointManager, InMemorySessionStore sessionStore, string sessionId)
{
    CheckpointInfo? lastCheckpoint = null;

    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, initialMessage, checkpointManager);

    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case RequestInfoEvent reqEvt:
                var response = HandleRequest(reqEvt.Request);
                await run.SendResponseAsync(response);
                break;

            case WorkflowOutputEvent outputEvt:
                Console.WriteLine($"\n[结果] {outputEvt.Data}");
                // Save final checkpoint and clear activeWorkflow on success
                if (lastCheckpoint != null)
                {
                    await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                }
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                return;

            case SuperStepCompletedEvent ssc:
                // Capture checkpoint from each super step
                if (ssc.CompletionInfo?.Checkpoint != null)
                {
                    lastCheckpoint = ssc.CompletionInfo.Checkpoint;
                    await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                }
                break;

            case WorkflowErrorEvent errEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                var executorId = errEvt.Exception?.Source ?? "workflow";
                var reason = errEvt.Exception?.Message ?? "未知错误";
                Console.WriteLine($"\n[错误] 工作流失败: {executorId} - {reason}");
                Console.ResetColor();
                // Clear activeWorkflow on error
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                // Leave checkpoint for debugging
                return;

            case ExecutorFailedEvent failEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[错误] 工作流失败: {failEvt.ExecutorId} - {failEvt.Data}");
                Console.ResetColor();
                // Clear activeWorkflow on error
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                // Leave checkpoint for debugging
                return;
        }
    }
}

static async Task ResumeWorkflow(Workflow workflow, string userMessage, CheckpointManager checkpointManager, InMemorySessionStore sessionStore, string sessionId)
{
    // Load checkpoint from session store
    var checkpoint = await sessionStore.GetAsync<CheckpointInfo>("lastCheckpoint", sessionId);
    if (checkpoint == null)
    {
        Console.WriteLine("[系统] 未找到断点记录，请重新启动流程");
        await sessionStore.RemoveAsync("activeWorkflow", sessionId);
        return;
    }

    Console.WriteLine("[系统] 从断点恢复退款流程...");
    CheckpointInfo? lastCheckpoint = checkpoint;

    await using StreamingRun run = await InProcessExecution.ResumeStreamingAsync(workflow, checkpoint, checkpointManager);

    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case RequestInfoEvent reqEvt:
                // Per D-01/D-02: User input directly becomes the RequestPort response
                // The user's current input (userMessage) is sent as the response
                Console.WriteLine($"[DEBUG] 发送用户输入到工作流: {userMessage}");
                var response = reqEvt.Request.CreateResponse(new RefundIntent(userMessage, "U100"));
                await run.SendResponseAsync(response);
                break;

            case WorkflowOutputEvent outputEvt:
                Console.WriteLine($"\n[结果] {outputEvt.Data}");
                // Save final checkpoint and clear activeWorkflow on success
                if (lastCheckpoint != null)
                {
                    await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                }
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                return;

            case SuperStepCompletedEvent ssc:
                // Capture checkpoint from each super step
                if (ssc.CompletionInfo?.Checkpoint != null)
                {
                    lastCheckpoint = ssc.CompletionInfo.Checkpoint;
                    await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                }
                break;

            case WorkflowErrorEvent errEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                var executorId = errEvt.Exception?.Source ?? "workflow";
                var reason = errEvt.Exception?.Message ?? "未知错误";
                Console.WriteLine($"\n[错误] 工作流失败: {executorId} - {reason}");
                Console.ResetColor();
                // Clear activeWorkflow on error
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                // Leave checkpoint for debugging
                return;

            case ExecutorFailedEvent failEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[错误] 工作流失败: {failEvt.ExecutorId} - {failEvt.Data}");
                Console.ResetColor();
                // Clear activeWorkflow on error
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                // Leave checkpoint for debugging
                return;
        }
    }
}

static ExternalResponse HandleRequest(ExternalRequest request)
{
    // Try RefundSignal (from InfoPort)
    if (request.TryGetDataAs<RefundSignal>(out var signal))
    {
        switch (signal)
        {
            case RefundSignal.NeedOrderId:
                Console.Write("请提供订单号: ");
                var orderId = Console.ReadLine() ?? "";
                Console.WriteLine($"[DEBUG] 收到订单号: {orderId}");
                return request.CreateResponse(new RefundIntent(orderId, "U100"));
        }
    }

    // Try ConfirmRefundRequest (from ConfirmPort)
    if (request.TryGetDataAs<ConfirmRefundRequest>(out var confirmReq))
    {
        Console.WriteLine($"订单 {confirmReq.OrderId}: {confirmReq.ProductName} ¥{confirmReq.Amount:F2}");
        Console.Write("确认退款？(回复'确认'或'取消'): ");
        var reply = Console.ReadLine();
        var confirmed = reply == "确认";
        if (reply == "取消")
        {
            Console.WriteLine("[系统] 已取消退款");
        }
        else if (!confirmed)
        {
            Console.WriteLine($"[系统] 未识别回复 '{reply}'，视为取消");
        }
        return request.CreateResponse(new UserConfirmation(confirmed));
    }

    throw new NotSupportedException($"Unknown request type: {request.PortInfo.PortId}");
}
