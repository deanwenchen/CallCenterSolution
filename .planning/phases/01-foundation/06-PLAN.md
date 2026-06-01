---
wave: 6
depends_on: ["02", "03", "04", "05"]
files_modified:
  - src/CallCenter.ConsoleDemo/Program.cs
requirements: [CD-01, CD-02, CD-03, CD-04, IR-03]
autonomous: true
---

# 计划 06：ConsoleDemo — 主循环 + 事件处理

## 目标

创建控制台应用程序入口点：DI 设置、LLM 客户端初始化、工作流构建、包含意图识别的主聊天循环、流式工作流执行、以及 RequestInfoEvent/ExternalResponse 处理。

## 任务

### 任务 6.1：创建 Program.cs

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-07~D-09：通过 ServiceCollection 做 DI，D-22~D-25：GetOrderExecutor 作为起点，D-18~D-21：RequestInfoEvent 处理）
- .planning/phases/01-foundation/01-CONTEXT.md（CD-01~CD-04：控制台主循环、两个端口的 RequestInfoEvent 处理、错误事件展示、30 分钟超时）
- MAF 参考：HumanInTheLoopBasic/Program.cs（主循环模式：WatchStreamAsync、RequestInfoEvent 处理、HandleExternalRequest、ExternalResponse 创建）
- MAF 参考：ConditionalEdges/02_SwitchCase/Program.cs（使用 DashScope 端点的 OpenAI 客户端：new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })）
- MAF 参考：InProcessExecution.cs（RunStreamingAsync）
- MAF 参考：ExternalRequest.cs（TryGetDataAs<T>, CreateResponse<T>）
- CallCenter.Workflows.Refund.RefundWorkflow
- CallCenter.Workflows.Refund.RefundMessages（所有消息类型）
- CallCenter.AgentHost.EntryPoint
- CallCenter.Framework.EventBus.InMemoryBusinessEventBus
- CallCenter.Framework.Session.InMemorySessionStore
- CallCenter.Shared.Services.MockOrderService 等
- CallCenter.Shared.Mcp.IOrderMcpClient 等
</read_first>

<acceptance_criteria>
- src/CallCenter.ConsoleDemo/Program.cs 存在
- 主入口点，包含 async Task Main()
- 初始化：
  - 从环境变量读取 DASHSCOPE_API_KEY（未设置则抛出异常）
  - 从环境变量读取 DASHSCOPE_MODEL_NAME（默认 "qwen3.6-plus"）
  - 通过 OpenAI SDK + DashScope 端点创建 IChatClient
  - 创建 ServiceCollection，注册服务：
    - services.AddSingleton<IOrderMcpClient>(new MockOrderService())
    - services.AddSingleton<IFinanceMcpClient>(new MockFinanceService())
    - services.AddSingleton<IMemberMcpClient>(new MockMemberService())
    - services.AddSingleton<InMemorySessionStore>()
    - services.AddSingleton<IBusinessEventBus>(new InMemoryBusinessEventBus())
  - 创建 EntryPoint(chatClient, sessionStore)
  - 通过 RefundWorkflow.Build(...) 创建 RefundWorkflow
  - 订阅 EventBus 到 RefundCompletedEvent：Console.WriteLine($"[EVENT] 退款完成: 订单{e.OrderId}, 金额 {e.RefundAmount:C}")
- 主循环：
  - Console.WriteLine("=== CallCenter AI Demo ===")
  - Console.WriteLine("输入消息开始（如'我要退款，订单A001'），输入'quit'退出。")
  - while (true)：
    - Console.Write("用户: ")
    - var userMessage = Console.ReadLine()
    - 如果 null/空白/"quit" → 退出循环
    - 调用 entryPoint.ProcessAsync(sessionId, userMessage, refundWorkflow)
    - 根据 ProcessResult 类型分支：
      - ResumeExistingResult：打印"退款流程已在进行中"
      - NoIntentResult：打印 noIntent.Response
      - StartWorkflowResult：打印意图信息，调用 RunWorkflow
- RunWorkflow 方法：
  - await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, initialMessage)
  - await foreach (WorkflowEvent evt in run.WatchStreamAsync())：
    - case RequestInfoEvent：调用 HandleRequest，发送 response
    - case WorkflowOutputEvent：打印结果，跳转到 EndWorkflow
    - case WorkflowErrorEvent：打印错误（红色），跳转到 EndWorkflow
    - case ExecutorFailedEvent：打印执行器失败（红色），跳转到 EndWorkflow
- HandleRequest 辅助方法：
  - 如果 request.TryGetDataAs<RefundSignal>(out var signal)：
    - case RefundSignal.NeedOrderId：Console.Write("请提供订单号: ")，读取 orderId，返回 request.CreateResponse(new RefundIntent(orderId, "U100"))
  - 如果 request.TryGetDataAs<ConfirmRefundRequest>(out var confirmReq)：
    - 打印订单详情，Console.Write("确认退款？(回复'确认'或'取消'): ")，读取 reply，返回 request.CreateResponse(new UserConfirmation(reply == "确认"))
  - 未知类型抛出 NotSupportedException
- 必要的 using 语句：CallCenter.* 命名空间、Microsoft.Agents.AI.Workflows、Microsoft.Extensions.AI、OpenAI、System.ClientModel、Microsoft.Extensions.DependencyInjection
</acceptance_criteria>

<action>
创建 Program.cs，包含完整主循环。关键点：
1. 使用 ServiceCollection 做 DI（不需要 Host builder）
2. 通过 OpenAI SDK + DashScope 创建 ChatClient
3. RefundWorkflow.Build() 使用解析的服务
4. 循环前订阅 EventBus
5. 主循环：读取输入 → 意图识别 → 如果是退款，运行工作流
6. 事件循环：处理两种端口类型的 RequestInfoEvent，处理输出/错误事件
7. HandleRequest 辅助方法通过 TryGetDataAs 区分 RefundSignal vs ConfirmRefundRequest

关键：RequestInfoEvent 处理器需要通过 TryGetDataAs 模式区分哪个端口发送的请求。
RefundInfoPort 的响应类型是 RefundIntent（匹配端口的 Response 类型）。
RefundConfirmPort 的响应类型是 UserConfirmation。
</action>

### 任务 6.2：全量构建验证

<acceptance_criteria>
- 从仓库根目录执行 `dotnet build` 成功，0 错误
- 所有 5 个项目编译通过
- 无未使用变量警告（或已抑制）
</acceptance_criteria>

<action>
从仓库根目录执行 dotnet build。修复所有编译错误和警告。
</action>

### 任务 6.3：端到端测试准备

<acceptance_criteria>
- 设置 DASHSCOPE_API_KEY 后控制台应用正常启动
- 显示欢迎消息："=== CallCenter AI Demo ==="
- 接受用户输入并回复
- 输入"我要退款，订单A001"：走完整工作流
- 输入"你好"：回复问候，不启动工作流
</acceptance_criteria>

<action>
此任务标记手动测试就绪。无需代码变更。测试标准为：设置 DASHSCOPE_API_KEY 后运行 `dotnet run --project src/CallCenter.ConsoleDemo`。
</action>
