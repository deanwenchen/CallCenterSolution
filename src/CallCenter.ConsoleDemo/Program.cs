#pragma warning disable MAAI001
using System.ClientModel;
using System.Threading.Channels;
using CallCenter.AgentHost;
using CallCenter.AgentHost.Skills;
using CallCenter.Framework;
using CallCenter.Framework.Audit;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Logging;
using CallCenter.Framework.Pipeline;
using CallCenter.Framework.Saga;
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

var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_MODEL_NAME") ?? "qwen3-vl-flash";

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

// Create SkillsProvider with RefundSkill and ExchangeSkill registered
var skillsProvider = new AgentSkillsProvider(new RefundSkill(), new ExchangeSkill());

// Build pipeline: SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput
var sessionId = "demo-session";

var summarizerClient = StandardPipelineFactory.CreateSummarizerClient(
    new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") }),
    modelName);

var logger = new JsonlLogger();
var pipelineClient = StandardPipelineFactory.CreatePipeline(chatClient, summarizerClient, sessionId, logger);

// Audit logging (Phase 7) — separate from operation logging
var auditLogger = new AuditLogger(".audit");

// Saga-enabled executor for testing (failOnce=false for normal operation)
object? refundExecutor = null;
object? restoreCoupon = null;

// Build workflow with saga-capable executor
var refundWorkflowWithAudit = RefundWorkflow.Build(orderService, financeService, memberService, eventBus);

// Create EntryPoint with piped client (not raw)
var entryPoint = new EntryPoint(pipelineClient, sessionStore, skillsProvider);

// Single stdin reader feeding a channel. Only ONE consumer reads from the channel at a time.
var inputChannel = Channel.CreateUnbounded<string>();
_ = Task.Run(async () =>
{
    while (true)
    {
        var line = await Console.In.ReadLineAsync();
        if (line == null) break;
        await inputChannel.Writer.WriteAsync(line);
    }
});

// Main chat loop
Console.WriteLine("=== CallCenter AI Demo ===");
Console.WriteLine("输入消息开始（如'我要退款，订单A001'），输入'quit'退出。\n");

while (true)
{
    Console.Write("用户: ");
    var userMessage = await inputChannel.Reader.ReadAsync();
    if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    // Process through EntryPoint
    var result = await entryPoint.ProcessAsync(sessionId, userMessage, refundWorkflow);

    switch (result)
    {
        case ResumeExistingResult:
            await ResumeWorkflow(refundWorkflow, userMessage, checkpointManager, sessionStore, sessionId, inputChannel);
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
            Console.WriteLine($"\n[系统] 已终止 {switchResult.OldWorkflow} 流程");
            Console.WriteLine($"[系统] 新意图 '{switchResult.NewIntent}' 暂未实现");
            break;

        case NoIntentResult noIntent:
            Console.WriteLine($"系统: {noIntent.Response}");
            break;

        case StartWorkflowResult start:
            Console.WriteLine($"[意图: refund, 订单: {start.InitialMessage.OrderId ?? "(未提供)"}]");
            await RunWorkflow(refundWorkflowWithAudit, start.InitialMessage, checkpointManager, sessionStore, sessionId, entryPoint.RecognizeIntentAsync, inputChannel, auditLogger, refundExecutor, restoreCoupon);
            break;
    }
}

static async Task RunWorkflow(
    Workflow workflow,
    RefundIntent initialMessage,
    CheckpointManager checkpointManager,
    InMemorySessionStore sessionStore,
    string sessionId,
    Func<string, CancellationToken, Task<IntentResult?>> recognizeIntent,
    Channel<string> inputChannel,
    AuditLogger? auditLogger = null,
    object? refundExecutor = null,
    object? restoreCoupon = null)
{
    CheckpointInfo? lastCheckpoint = null;

    // Retry loop: if workflow needs order ID, re-run after getting it
    var currentMessage = initialMessage;
    while (true)
    {
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, currentMessage, checkpointManager);

        bool needsOrderId = false;

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent reqEvt:
                    await AuditTrailMiddleware.CaptureStepStart(auditLogger!, sessionId, "RequestInfo", reqEvt);
                    if (reqEvt.Request.TryGetDataAs<RefundSignal>(out var signal) && signal == RefundSignal.NeedOrderId)
                    {
                        Console.Write("请提供订单号: ");
                        var orderId = await inputChannel.Reader.ReadAsync() ?? "";
                        await sessionStore.SetAsync("pendingOrderId", orderId, sessionId);
                        currentMessage = currentMessage with { OrderId = orderId };
                        needsOrderId = true;
                    }
                    else
                    {
                        var response = await HandleRequestAsync(reqEvt.Request, recognizeIntent, sessionStore, sessionId, inputChannel);
                        await run.SendResponseAsync(response);
                    }
                    await AuditTrailMiddleware.CaptureStepEnd(auditLogger!, sessionId, "RequestInfo", reqEvt);
                    break;

                case WorkflowOutputEvent outputEvt:
                    Console.WriteLine($"\n[结果] {outputEvt.Data}");
                    await AuditTrailMiddleware.CaptureStepEnd(auditLogger!, sessionId, "WorkflowOutput", outputEvt);
                    if (lastCheckpoint != null)
                    {
                        await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                    }
                    await sessionStore.RemoveAsync("activeWorkflow", sessionId);

                    if (auditLogger != null)
                    {
                        var verifyResult = await auditLogger.VerifyChainAsync(sessionId);
                        Console.WriteLine($"\n[审计] {verifyResult.Message}");
                    }
                    return;

                case SuperStepCompletedEvent ssc:
                    if (ssc.CompletionInfo?.Checkpoint != null)
                    {
                        lastCheckpoint = ssc.CompletionInfo.Checkpoint;
                        await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                    }
                    await AuditTrailMiddleware.CaptureStepEnd(auditLogger!, sessionId, "SuperStep", ssc);
                    break;

                case WorkflowErrorEvent errEvt:
                    Console.ForegroundColor = ConsoleColor.Red;
                    var executorId = errEvt.Exception?.Source ?? "workflow";
                    var reason = errEvt.Exception?.Message ?? "未知错误";
                    Console.WriteLine($"\n[错误] 工作流失败: {executorId} - {reason}");
                    Console.ResetColor();

                    await AuditTrailMiddleware.CaptureError(auditLogger!, sessionId, executorId, errEvt.Exception ?? new Exception(reason));

                    if (executorId == "ExecuteRefund" && restoreCoupon != null)
                    {
                        Console.WriteLine("\n[补偿] 检测到 ExecuteRefund 失败，触发 Saga 补偿...");
                        var compensationTriggered = false;
                        try
                        {
                            var saga = new SagaBuilder()
                                .OnFailure("ExecuteRefund", async ct =>
                                {
                                    Console.WriteLine("[补偿] 执行补偿: 恢复优惠券...");
                                    compensationTriggered = true;
                                })
                                .WithRetry(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));

                            await saga.ExecuteAsync(async _ => throw errEvt.Exception!, default);

                            if (compensationTriggered)
                            {
                                Console.WriteLine("[补偿] 补偿完成");
                                await AuditTrailMiddleware.CaptureStepEnd(auditLogger!, sessionId, "SagaCompensation", new { Step = "ExecuteRefund", Compensation = "RestoreCoupon" });
                            }
                        }
                        catch (SagaCompensationException sagaEx)
                        {
                            Console.WriteLine($"[补偿] 补偿失败: {sagaEx.Message}");
                            await AuditTrailMiddleware.CaptureError(auditLogger!, sessionId, "SagaCompensation", sagaEx);
                        }
                    }

                    await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                    return;

                case ExecutorFailedEvent failEvt:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[错误] 工作流失败: {failEvt.ExecutorId} - {failEvt.Data}");
                    Console.ResetColor();

                    await AuditTrailMiddleware.CaptureError(auditLogger!, sessionId, failEvt.ExecutorId ?? "unknown", new Exception($"Executor {failEvt.ExecutorId} failed: {failEvt.Data}"));

                    await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                    return;
            }
        }

        if (!needsOrderId) break;
    }
}

static async Task ResumeWorkflow(Workflow workflow, string userMessage, CheckpointManager checkpointManager, InMemorySessionStore sessionStore, string sessionId, Channel<string> inputChannel)
{
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
                if (reqEvt.Request.TryGetDataAs<RefundSignal>(out _))
                {
                    Console.Write("请提供订单号: ");
                    var orderId = await inputChannel.Reader.ReadAsync() ?? "";
                    var orderIdResponse = reqEvt.Request.CreateResponse(new RefundIntent(orderId, "U100"));
                    await run.SendResponseAsync(orderIdResponse);
                }
                else if (reqEvt.Request.TryGetDataAs<ConfirmRefundRequest>(out var confirmReq))
                {
                    Console.WriteLine($"订单 {confirmReq.OrderId}: {confirmReq.ProductName} ¥{confirmReq.Amount:F2}");
                    Console.Write("确认退款？(回复'确认'或'取消'): ");
                    var reply = await inputChannel.Reader.ReadAsync();
                    ExternalResponse confirmResponse;
                    if (reply == "确认")
                    {
                        Console.WriteLine("已确认");
                        confirmResponse = reqEvt.Request.CreateResponse(new UserConfirmation(true));
                    }
                    else
                    {
                        Console.WriteLine("已取消");
                        confirmResponse = reqEvt.Request.CreateResponse(new UserConfirmation(false));
                    }
                    await run.SendResponseAsync(confirmResponse);
                }
                else
                {
                    Console.WriteLine($"[ERROR] 未知的 RequestPort 类型");
                }
                break;

            case WorkflowOutputEvent outputEvt:
                Console.WriteLine($"\n[结果] {outputEvt.Data}");
                if (lastCheckpoint != null)
                {
                    await sessionStore.SetAsync("lastCheckpoint", lastCheckpoint, sessionId);
                }
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                return;

            case SuperStepCompletedEvent ssc:
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
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                return;

            case ExecutorFailedEvent failEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[错误] 工作流失败: {failEvt.ExecutorId} - {failEvt.Data}");
                Console.ResetColor();
                await sessionStore.RemoveAsync("activeWorkflow", sessionId);
                return;
        }
    }
}

static async Task<ExternalResponse> HandleRequestAsync(
    ExternalRequest request,
    Func<string, CancellationToken, Task<IntentResult?>> recognizeIntent,
    InMemorySessionStore sessionStore,
    string sessionId,
    Channel<string> inputChannel,
    CancellationToken ct = default)
{
    // Try RefundSignal (from InfoPort)
    if (request.TryGetDataAs<RefundSignal>(out var signal))
    {
        switch (signal)
        {
            case RefundSignal.NeedOrderId:
                Console.Write("请提供订单号: ");
                var orderId = await inputChannel.Reader.ReadAsync(ct) ?? "";
                // Write to session store so GetOrderExecutor can pick it up
                await sessionStore.SetAsync("pendingOrderId", orderId, sessionId, ct);
                return request.CreateResponse(new RefundIntent(orderId, "U100"));
        }
    }

    // Try ConfirmRefundRequest (from ConfirmPort)
    if (request.TryGetDataAs<ConfirmRefundRequest>(out var confirmReq))
    {
        Console.WriteLine($"订单 {confirmReq.OrderId}: {confirmReq.ProductName} ¥{confirmReq.Amount:F2}");
        Console.Write("确认退款？(回复'确认'或'取消'): ");
        var reply = await inputChannel.Reader.ReadAsync(ct);
        if (reply == "确认")
        {
            return request.CreateResponse(new UserConfirmation(true));
        }
        if (reply == "取消")
        {
            Console.WriteLine("[系统] 已取消退款");
            return request.CreateResponse(new UserConfirmation(false));
        }

        // Unrecognized reply — re-recognize intent (IR-05)
        var intent = await recognizeIntent(reply ?? "", ct);
        if (intent == null || intent.Intent == "unknown")
        {
            Console.WriteLine($"[系统] 未识别回复 '{reply}'，已取消退款");
            return request.CreateResponse(new UserConfirmation(false));
        }
        if (intent.Intent == "greeting")
        {
            Console.WriteLine("\n[系统] 你好！有什么可以帮助你的？");
            Console.WriteLine("[系统] 退款流程已挂起，请确认后重新开始");
            await sessionStore.RemoveAsync("activeWorkflow", sessionId, ct);
            return request.CreateResponse(new UserConfirmation(false));
        }

        // New workflow intent — suspend current workflow, switch intent
        Console.WriteLine($"\n[系统] 已终止 RefundWorkflow 流程");
        Console.WriteLine($"[系统] 新意图 '{intent.Intent}' 暂未实现");
        await sessionStore.RemoveAsync("activeWorkflow", sessionId, ct);
        return request.CreateResponse(new UserConfirmation(false));
    }

    throw new NotSupportedException($"Unknown request type: {request.PortInfo.PortId}");
}
