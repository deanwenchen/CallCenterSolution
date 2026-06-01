using System.ClientModel;
using CallCenter.AgentHost;
using CallCenter.Framework;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Session;
using CallCenter.Shared.Mcp;
using CallCenter.Shared.Services;
using CallCenter.Workflows.Refund;
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

// Subscribe to events
eventBus.Subscribe<RefundCompletedEvent>(async e =>
{
    Console.WriteLine($"\n[EVENT] 退款完成: 订单{e.OrderId}, 金额 {e.RefundAmount:C}");
    await Task.CompletedTask;
});

// Build workflow
var refundWorkflow = RefundWorkflow.Build(orderService, financeService, memberService, eventBus);

// Create EntryPoint
var entryPoint = new EntryPoint(chatClient, sessionStore);

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
            Console.WriteLine("[系统] 退款流程已在进行中，请继续当前流程。");
            // Phase 3 will implement full Resume from checkpoint
            break;

        case NoIntentResult noIntent:
            Console.WriteLine($"系统: {noIntent.Response}");
            break;

        case StartWorkflowResult start:
            Console.WriteLine($"[意图: refund, 订单: {start.InitialMessage.OrderId ?? "(未提供)"}]");
            await RunWorkflow(refundWorkflow, start.InitialMessage);
            break;
    }
}

static async Task RunWorkflow(Workflow workflow, RefundIntent initialMessage)
{
    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, initialMessage);

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
                goto EndWorkflow;

            case WorkflowErrorEvent errEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[错误] {errEvt.Exception?.Message}");
                Console.ResetColor();
                goto EndWorkflow;

            case ExecutorFailedEvent failEvt:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[执行器失败] {failEvt.ExecutorId}: {failEvt.Data}");
                Console.ResetColor();
                goto EndWorkflow;
        }
    }

EndWorkflow:
    Console.WriteLine();
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
                return request.CreateResponse(new RefundIntent(orderId, "U100"));
        }
    }

    // Try ConfirmRefundRequest (from ConfirmPort)
    if (request.TryGetDataAs<ConfirmRefundRequest>(out var confirmReq))
    {
        Console.WriteLine($"订单 {confirmReq.OrderId}: {confirmReq.ProductName} ¥{confirmReq.Amount:F2}");
        Console.Write("确认退款？(回复'确认'或'取消'): ");
        var reply = Console.ReadLine();
        return request.CreateResponse(new UserConfirmation(reply == "确认"));
    }

    throw new NotSupportedException($"Unknown request type: {request.PortInfo.PortId}");
}
