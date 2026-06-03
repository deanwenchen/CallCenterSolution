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

// Program.cs 是整个演示程序的启动入口。
// 主要作用：组装所有基础设施（LLM、技能、工作流、日志、审计、会话存储），
// 然后进入一个命令行对话循环，让用户可以直接体验“我要退款”这样的业务流程。

var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")
    ?? throw new InvalidOperationException("DASHSCOPE_API_KEY not set. Set it to your DashScope API key.");

var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_MODEL_NAME") ?? "qwen3-vl-flash";

// 创建 DashScope 的聊天客户端（OpenAI 兼容接口）。
// 这是所有意图识别和摘要压缩最终调用的大模型底座。
IChatClient chatClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
    .GetChatClient(modelName)
    .AsIChatClient();

// 注册演示用服务。
// 这里全部使用 Mock 服务，避免依赖真实订单、财务、会员系统。
var orderService = new MockOrderService();
var financeService = new MockFinanceService();
var memberService = new MockMemberService();
var eventBus = new InMemoryBusinessEventBus();
var sessionStore = new InMemorySessionStore();
var checkpointManager = CheckpointManager.Default;

// 订阅退款完成事件。
// 主要作用：演示 EventBus 解耦能力 —— 工作流只发布事件，控制台单独消费并展示。
eventBus.Subscribe<RefundCompletedEvent>(async e =>
{
    Console.WriteLine($"\n[EVENT] 退款完成: 订单{e.OrderId}, 金额 {e.RefundAmount:C}");
    await Task.CompletedTask;
});

// 构建退款工作流图。
// 这里拿到的是一条完整的业务链路定义，还没有真正执行。
var refundWorkflow = RefundWorkflow.Build(orderService, financeService, memberService, eventBus);

// 注册 Agent 技能。
// 主要作用：让 AIAgent 可以通过技能描述自动发现“退款 / 换货”能力。
var skillsProvider = new AgentSkillsProvider(SkillRegistry.All);

// 构建标准 6 层聊天管道。
// 主要作用：把安全过滤、日志、压缩、工具审批统一包在一次 LLM 调用链里。
var sessionId = "demo-session";

var summarizerClient = StandardPipelineFactory.CreateSummarizerClient(
    new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") }),
    modelName);

var logger = new JsonlLogger();
var pipelineClient = StandardPipelineFactory.CreatePipeline(chatClient, summarizerClient, sessionId, logger);

// 审计日志记录器。
// 主要作用：把每一步工作流输入/输出落盘，便于事后追踪和审计验证。
var auditLogger = new AuditLogger(".audit");

// Saga 相关依赖（当前 demo 不注入真实 executor，只保留扩展位）。
// 主要作用：后续可用于演示“失败重试 + 补偿回滚”能力。
object? refundExecutor = null;
object? restoreCoupon = null;

// 构建带审计能力的退款工作流实例。
// 主要作用：给主循环实际执行使用，和前面的工作流定义保持一致。
var refundWorkflowWithAudit = RefundWorkflow.Build(orderService, financeService, memberService, eventBus);

// 创建用户输入入口。
// 主要作用：统一承接用户消息，做意图识别、超时检查和流程路由。
var entryPoint = new EntryPoint(pipelineClient, sessionStore, skillsProvider);

// 创建单一 stdin 读取通道。
// 主要作用：把控制台输入和事件循环解耦，避免 Console.ReadLine() 被多个地方抢占。
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

// 主聊天循环。
// 主要作用：持续读取用户输入，交给 EntryPoint 做路由，再根据结果决定启动、恢复或终止工作流。
Console.WriteLine("=== CallCenter AI Demo ===");
Console.WriteLine("输入消息开始（如'我要退款，订单A001'），输入'quit'退出。\n");

while (true)
{
    Console.Write("用户: ");
    var userMessage = await inputChannel.Reader.ReadAsync();
    if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    // 通过 EntryPoint 处理当前用户输入。
// 它会返回一个 ProcessResult，告诉主循环下一步该怎么做。
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

// 执行退款工作流。
// 主要作用：驱动工作流运行、处理 RequestPort 交互、记录审计日志，并在需要订单号时自动重跑流程。
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
        await using var run2 = await InProcessExecution.RunStreamingAsync(workflow, currentMessage, checkpointManager);

        bool needsOrderId = false;

        await foreach (WorkflowEvent evt in run2.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent reqEvt:
                    await AuditTrailMiddleware.CaptureStepStart(auditLogger!, sessionId, "RequestInfo", reqEvt);
                    if (reqEvt.Request.TryGetDataAs<RefundSignal>(out var signal) && signal == RefundSignal.NeedOrderId)
                    {
                        Console.Write("请提供订单号: ");
                        var orderId = await inputChannel.Reader.ReadAsync();
                        await sessionStore.SetAsync("pendingOrderId", orderId, sessionId);
                        currentMessage = currentMessage with { OrderId = orderId };
                        needsOrderId = true;
                        // Send the response to the port so the workflow can continue
                        var orderIdResponse = reqEvt.Request.CreateResponse(new RefundIntent(orderId, "U100"));
                        await run2.SendResponseAsync(orderIdResponse);
                    }
                    else
                    {
                        var response = await HandleRequestAsync(reqEvt.Request, recognizeIntent, sessionStore, sessionId, inputChannel);
                        await run2.SendResponseAsync(response);
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

// 从断点恢复工作流。
// 主要作用：当用户继续之前中断的流程时，从 lastCheckpoint 续跑，并把当前输入注入到等待中的端口。
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

// 处理工作流对外请求（RequestPort）。
// 主要作用：把工作流发出的“请提供订单号 / 请确认退款”这类请求，转成控制台上的用户交互。
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
