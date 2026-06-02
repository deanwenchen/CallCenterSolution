# Design: Refund Workflow Demo

## 架构决策

### 1. 项目结构

严格按 `Prd.md` Section 四 定义的目录结构：

```
src/
├── CallCenter.Shared/           (公共 DTO + Mock 服务)
├── CallCenter.Framework/        (框架层 9 个组件)
├── CallCenter.Workflows/        (退款流程 + Executors)
├── CallCenter.AgentHost/        (EntryPoint + Skills)
└── CallCenter.ConsoleDemo/      (控制台调试入口)
```

### 2. MAF SDK 引用方式

直接引用 agent-framework 源码（当前 `.slnx` 方式），不使用 NuGet 包。
- `Microsoft.Agents.AI.Abstractions`
- `Microsoft.Agents.AI`
- `Microsoft.Agents.AI.Workflows`
- `Microsoft.Agents.AI.Workflows.Generators`

### 3. 退款流程设计

**两个 RequestPort：**

| 端口 | 请求类型 | 响应类型 | 用途 |
|---|---|---|---|
| RefundInfoPort | `RefundSignal` | `RefundIntent` | 参数收集（订单号、用户信息等） |
| RefundConfirmPort | `ConfirmRefundRequest` | `UserConfirmation` | 用户确认退款 |

**动态追问机制：**

当 GetOrderExecutor 发现缺少 orderId 时，发送 `RefundSignal.NeedOrderId`，通过 `.ForwardMessage<RefundSignal>(getOrder, infoPort)` 路由回 RefundInfoPort，形成循环。用户补完参数后，Workflow 从暂停处恢复继续执行。

```
[RefundInfoPort] ──→ [GetOrderExecutor]
                         │
                    缺 orderId?
                    ┌──┴──┐
                    │ 是  │ 否 (OrderFound)
                    └──┬──┘
                       │         ↓
                       └── [CheckRefundRuleExecutor]
```

### 4. 消息类型定义

```
RefundIntent          — 初始输入 {OrderId?, UserId?}
RefundSignal          — 信号枚举 {Init, NeedOrderId, OrderFound, Ineligible, Cancelled}
OrderFound            — 查到订单 {OrderInfo}
RefundRuleResult      — 规则校验 {IsEligible, Reason?, RefundAmount}
ConfirmRefundRequest  — 确认请求 {Amount, OrderId, ProductName}
UserConfirmation      — 用户确认 {Confirmed}
RefundExecuted        — 退款执行结果 {RefundResult}
CouponRestored        — 优惠券恢复 {CouponId?}
RefundNotification    — 最终输出 {Message}
```

### 5. Executor 设计

| Executor | 输入 | 输出 | 职责 |
|---|---|---|---|
| GetOrderExecutor | RefundIntent | OrderFound | 查 MockOrderService，缺 orderId 发 NeedOrderId 信号 |
| CheckRefundRuleExecutor | OrderFound | RefundRuleResult | 校验 7 天/签收/品类规则，计算退款金额 |
| WaitUserConfirmExecutor | RefundRuleResult | — | 发 ConfirmRefundRequest 到确认端口 |
| ExecuteRefundExecutor | UserConfirmation | RefundExecuted | 用户取消发 Cancelled，否则调 MockFinanceService |
| RestoreCouponExecutor | RefundExecuted | CouponRestored | 调 MockMemberService 恢复优惠券 |
| SendNotificationExecutor | CouponRestored | RefundNotification | YieldOutput + EventBus.Publish |
| RefundDeniedExecutor | RefundRuleResult | RefundNotification | 输出拒绝原因 |

### 6. 意图识别

使用 DashScope (通义千问) 的 OpenAI 兼容接口：
- Endpoint: `https://dashscope.aliyuncs.com/compatible-mode/v1`
- API Key: 环境变量 `DASHSCOPE_API_KEY`
- Model: 环境变量 `DASHSCOPE_MODEL_NAME` (默认 qwen3.6-plus)
- 通过 `OpenAI` NuGet 包 + `Microsoft.Extensions.AI.OpenAI` 创建 `IChatClient`
- System prompt 要求返回 JSON: `{"intent": "refund"|"greeting"|"unknown", "orderId": "..."}`

### 7. Mock 数据

**3 个测试订单：**
- A001: 蓝牙耳机 ¥299, 3天前, 已签收 → **可退**
- A002: 定制T恤 ¥159, 30天前, 已签收 → **超过7天不可退**
- A003: 手机壳 ¥39, 1天前, 运输中 → **未签收不可退**

### 8. 控制台交互

纯 `Console.ReadLine/WriteLine`。主循环：
1. 读用户输入
2. LLM 意图识别
3. refund 意图 → 启动/恢复 Workflow
4. 事件循环处理 RequestInfoEvent / WorkflowOutputEvent / Error

### 9. Framework 层实现策略

| 组件 | Demo 状态 |
|---|---|
| EventBus | 内存实现 InMemoryBusinessEventBus，ConsoleDemo 会订阅事件 |
| Parsing | StructuredOutputParser 实现 JSON 反序列化包装 |
| Builder | BusinessModuleBuilder 空壳（Demo 直接调用 Workflow.Build） |
| Session | InMemorySessionStore 基础实现，RedisSessionStore 空壳 |
| Safety / Compaction / Audit / Saga / Pipeline | 空壳文件 + TODO |

### 10. MCP Client 接口定义

**Shared/Mcp/ 目录：**

```csharp
// CallCenter.Shared/Mcp/IOrderMcpClient.cs
public interface IOrderMcpClient
{
    Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default);
    Task<List<OrderInfo>> GetRecentOrdersAsync(string userId, CancellationToken ct = default);
}

// CallCenter.Shared/Mcp/IFinanceMcpClient.cs
public interface IFinanceMcpClient
{
    Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default);
}

// CallCenter.Shared/Mcp/IMemberMcpClient.cs
public interface IMemberMcpClient
{
    Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default);
    Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default);
}
```

**MCP 注册方式（Program.cs）：**
```csharp
builder.Services.AddMcpClients(mcp =>
{
    mcp.AddOrderClient(config["Mcp:OrderEndpoint"]);     // Demo: MockOrderService
    mcp.AddFinanceClient(config["Mcp:FinanceEndpoint"]); // Demo: MockFinanceService
    mcp.AddMemberClient(config["Mcp:MemberEndpoint"]);   // Demo: MockMemberService
});
```

Demo 阶段 MCP Client 接口由 Mock 服务实现，接口签名与 PRD 定义完全一致。Executor 通过 DI 注入 MCP Client 接口调用。

### 11. RefundSkill 完整设计

**CallCenter.AgentHost/Skills/RefundSkill.cs：**

```csharp
internal sealed class RefundSkill : AgentClassSkill<RefundSkill>
{
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "refund",
        "处理用户退款请求。当用户要求退款、退货、取消订单时使用。"
        "支持查询订单、校验退款资格、计算退款金额、执行退款。");

    protected override string Instructions => """
        当用户要求退款时使用此技能。

        1. 获取订单号（如果用户未提供，使用 get_recent_orders 脚本获取最近订单）
        2. 系统将自动处理退款流程，包括资格校验、金额计算、用户确认
        3. 退款完成后通知用户结果
        """;

    [AgentSkillScript("get_recent_orders")]
    [Description("获取用户最近的订单列表。")]
    private static async Task<string> GetRecentOrders(
        string? userId,
        IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IOrderMcpClient>();
        var orders = await client.GetRecentOrdersAsync(userId);
        return JsonSerializer.Serialize(orders);
    }

    [AgentSkillScript("execute_refund")]
    [Description("执行退款操作。")]
    private static async Task<string> ExecuteRefund(
        string orderId,
        decimal amount,
        IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IFinanceMcpClient>();
        var result = await client.RefundAsync(orderId, amount);
        return JsonSerializer.Serialize(result);
    }
}
```

**Skill 注册：**
```csharp
var skillsProvider = new AgentSkillsProviderBuilder()
    .UseSkill(new RefundSkill())
    .Build();

var intentAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "客服意图识别",
    AIContextProviders = [skillsProvider],
}, services: serviceProvider);
```

### 12. Agent Pipeline 设计

AIAgent（意图识别/对话）的中间件管道，按顺序执行：

```
用户消息（通过 Gateway → Entry Point）
  │
  ▼
[1] SafetyInputFilter    ← 框架层: PII 脱敏、关键词拦截、Prompt Injection 检测
  │
[2] LoggingAgent         ← MAF 原生: 记录操作日志（不记 Trace 级别内容）
  │
[3] CompactionProvider   ← MAF 原生: 超过 token 阈值时压缩历史消息
  │
[4] ToolApprovalAgent    ← MAF 原生: 工具调用审批规则检查
  │
[5] LLM + StructuredOutputParser  ← MAF 原生 + 框架: 实际模型调用 + 强类型输出
  │
[6] SafetyOutputFilter   ← 框架层: 输出 PII 脱敏、高风险内容拦截、格式规范化
  │
  ▼
返回给 Entry Point（决定启动 Workflow 或回复用户）
```

**注意：** 此管道用于 AIAgent，Workflow 执行不经过此管道。Workflow 有自己的执行流（Executor → Edge → Executor）。

Demo 阶段管道简化实现：仅保留 Logging + LLM + StructuredOutputParser，Safety/Compaction/ToolApproval 为空壳。

### 13. Safety Pipeline 设计

**PII 脱敏规则（Demo 阶段简化）：**
```json
// safety/pii-rules.json
{
  "patterns": [
    {
      "name": "phone",
      "regex": "(1[3-9]\\d)\\d{4}(\\d{4})",
      "replace": "$1****$2",
      "description": "手机号中间 4 位脱敏"
    },
    {
      "name": "idcard",
      "regex": "(\\d{6})\\d{8}(\\d{4})",
      "replace": "$1********$2",
      "description": "身份证生日 8 位脱敏"
    }
  ]
}
```

Demo 阶段 PiiRedactor 有基础正则实现，KeywordFilter 和 PromptInjectionDetector 为空壳。

### 14. Checkpoint 持久化设计

PRD 定义了 CheckpointManager 支持流程状态持久化。Demo 阶段使用 MAF 内置的内存 Checkpoint（InProcessExecution 自带），不做外部持久化存储。

**生产环境：** RedisSessionStore 存储 checkpoint，支持崩溃恢复、跨请求持久化。
```
RedisSessionStore 三合一：
  1. 聊天历史：按 SessionId 精确存取 List<ChatMessage>
  2. AgentSession 序列化：Session → JSON → Redis
  3. Workflow Checkpoint：流程状态 → Redis，支持断点恢复
  4. TTL 自动过期：超过 30/90 天自动清理
```

### 15. 外部系统调用桥接设计

**Executor → MCP Client 调用链：**

```
GetOrderExecutor
  ↓ DI 注入
  IOrderMcpClient.GetOrderAsync(orderId)
  ↓ Demo: MockOrderService 实现
  ↓ 生产: McpClient → MCP Server → Order Service

ExecuteRefundExecutor
  ↓ DI 注入
  IFinanceMcpClient.RefundAsync(orderId, amount)
  ↓ Demo: MockFinanceService 实现
  ↓ 生产: McpClient → MCP Server → Finance Service

RestoreCouponExecutor
  ↓ DI 注入
  IMemberMcpClient.RestoreCouponAsync(userId, couponId)
  ↓ Demo: MockMemberService 实现
  ↓ 生产: McpClient → MCP Server → Member Service
```

Executor 只依赖接口，不关心是 Mock 还是真实 MCP 实现。

### 16. Framework vs Business Module 边界

判断标准（PRD Section 7.5.1）：

| 问题 | 放 Framework？ |
|---|---|
| 退款/换货/物流都需要吗？ | 是 → Framework；否 → 看是否通用 |
| 与安全/合规强相关？ | 是 → Framework；否 → 看是否通用 |
| 不同业务的实现逻辑完全不同？ | 是 → Business Module（如优惠券分摊规则） |
| 是降低开发门槛的抽象？ | 是 → Framework |

### 17. 旁路系统

**Knowledge Layer：** FAQ/RAG/产品知识库/退款规则库。MAF 对应 AgentFileSkillsSource（从磁盘扫描知识文件）。Demo 阶段不做。

**Observability Layer：** OpenTelemetry/Langfuse。MAF 对应 WorkflowBuilder.WithTelemetry()。Demo 阶段不做。

**Human Agent Layer：** 人工客服接管/审批/兜底。MAF 对应 RequestPort + ExternalResponse。Demo 阶段不做（RequestPort 仅用于用户确认和参数追问）。

### 18. Compaction 设计

MAF 已有 CompactionProvider + 多种策略，Demo 阶段封装为快捷配置：

```csharp
// 框架封装（简洁）：
builder.Services.AddCallCenterCompaction()
    .UseSummarization(options =>
    {
        options.TokenThreshold = 8000;     // 超过 8000 token 触发压缩
        options.PreserveRecentTurns = 8;   // 保留最近 8 轮不压缩
        options.SmallModel = "gpt-4o-mini"; // 用小模型做摘要，省钱
    });
```

Demo 阶段为空壳扩展方法。

### 19. 新增业务模块步骤

以"新增换货 Exchange"为例：

```
Step 1: 复制 Workflows/Refund/ → Workflows/Exchange/
Step 2: 重命名所有文件中的 Refund → Exchange
Step 3: 修改 ExchangeWorkflow.cs 的流程步骤
Step 4: 修改 Executors 中的业务逻辑
Step 5: 新增 AgentHost/Skills/ExchangeSkill.cs
Step 6: 在 Program.cs 中添加一行 builder.Services.AddCallCenterBusinessModule("exchange")...
Step 7: 完成 — 退款模块完全不受影响
```

### 20. 开发者接入目标体验

```csharp
// Program.cs — 完整配置示例

var builder = WebApplication.CreateBuilder(args);

// 1. 启动框架核心
builder.Services.AddCallCenter()
    .UseRedisSession(options => options.ConnectionString = config["Redis:Connection"])
    .WithSafetyPipeline()
    .WithCompaction()
    .WithTelemetry()
    .WithEventBus(events =>
    {
        events.On<RefundCompletedEvent>(async e =>
            await smsService.SendAsync(e.UserId, $"退款 {e.RefundAmount:C} 已完成"));
        events.On<RiskAlertEvent>(async e =>
            await dingtalk.NotifyAsync($"高风险告警: {e.OrderId}"));
    });

// 2. 注册 MCP Client（统一注册）
builder.Services.AddMcpClients(mcp =>
{
    mcp.AddOrderClient(config["Mcp:OrderEndpoint"]);
    mcp.AddFinanceClient(config["Mcp:FinanceEndpoint"]);
    mcp.AddMemberClient(config["Mcp:MemberEndpoint"]);
});

// 3. 注册业务模块（每个模块 3-5 行）
builder.Services.AddCallCenterBusinessModule("refund")
    .WithSkill<RefundSkill>()
    .WithWorkflow<RefundWorkflow>();

var app = builder.Build();
app.MapCallCenterGateway();
app.Run();
```

Demo 阶段不使用 WebApplication，而是 Console 主循环直接调用。但框架的 DI 注册方式和目标体验保持一致。

## 待决问题

无。所有设计决策已确定。
