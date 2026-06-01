# CallCenter AI — 架构设计文档

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。

## 一、设计目标

1. **用户通过客服聊天窗口发起请求**（如"我要退款"），系统识别意图后进入对应业务流程
2. **流程执行中需要用户补充信息时，自动暂停并询问用户**，收到回复后恢复执行
3. **新增业务快速扩展** — 今天加退款，明天加换货，互不影响，结构清晰
4. **框架关注横切关注点** — 会话持久化、安全管道、审计、事件总线，业务规则留在业务模块内

---

## 二、简化架构图

```text
┌─────────────────────────────────────────────────────────────────────┐
│                           客户接入层                                │
├─────────────────────────────────────────────────────────────────────┤
│ Web客服 │ APP │ H5 │ 小程序 │ 企业微信 │ 公众号 │ WhatsApp │ API   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                    Conversation Gateway（会话网关）                  │
├─────────────────────────────────────────────────────────────────────┤
│ • Session 管理 (AgentSession)                                       │
│ • 用户身份识别 (UserId / OpenId / UnionId)                          │
│ • Token 校验 / 权限控制 / RateLimit / 黑名单                        │
│ • 消息路由 + 上下文管理 + 审计日志                                  │
│                                                                     │
│ MAF 对应: AgentSession + StateBag                                   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                    Entry Point（统一入口 · 核心）                   │
├─────────────────────────────────────────────────────────────────────┤
│ 作用：                                                              │
│                                                                     │
│ 1. 检查 Session.StateBag → 当前是否有 active workflow？             │
│    YES → Resume Workflow（直接恢复执行）                             │
│    NO  → 进入 Intent Router                                         │
│                                                                     │
│ 2. Intent Router（简化版）：                                        │
│    • 主路：LLM Agent 识别意图 → 直接选择对应 Workflow/Agent         │
│    • 辅路：Keyword Rule fallback（兜底，零成本）                    │
│                                                                     │
│ MAF 对应: AIAgent (意图识别) + WorkflowHost (流程编排)              │
│                                                                     │
│ 输出（有意图）:                                                           │
│ {                                                                   │
│   "intent": "refund",                                               │
│   "workflow": "RefundWorkflow",                                     │
│   "entities": { "orderId": "A001" }                                 │
│ }                                                                   │
│                                                                     │
│ 输出（无意图）: 走对话 Agent（自由对话模式，不启动 Workflow）        │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                    ┌──────────┼──────────┐
                    ▼          ▼          ▼

   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
   │ 对话 Agent   │  │ Refund       │  │ Exchange     │
   │ (自由对话)   │  │ Workflow     │  │ Workflow     │
   │              │  │              │  │              │
   │ 闲聊/问候    │  │ + Skill      │  │ + Skill      │
   │ 无法识别意图 │  │ + Executors  │  │ + Executors  │
   │              │  │ + MCPs       │  │ + MCPs       │
   └──────────────┘  └──────┬───────┘  └──────┬───────┘
          │                 │                 │
          └─────────────────┼─────────────────┘
                            │
                            ▼

┌─────────────────────────────────────────────────────────────────────┐
│                    Workflow Engine（MAF 原生）                      │
├─────────────────────────────────────────────────────────────────────┤
│ • WorkflowBuilder 构建有向图                                        │
│ • Executor 作为节点，Edge 作为连线                                  │
│ • RequestPort 实现人工介入（暂停 → 询问用户 → 恢复）               │
│ • Checkpoint 支持流程状态持久化                                     │
│ • Subworkflow 支持流程组合                                          │
│                                                                     │
│ 退款流程示例:                                                       │
│                                                                     │
│ [INIT]                                                              │
│    ↓                                                                │
│ [GetOrderExecutor] ────→ 查询订单信息                               │
│    ↓                                                                │
│ [CheckRefundRuleExecutor] ──→ 校验退款资格                          │
│    ↓                                                                │
│ [RequestPort: WAIT_USER_CONFIRM] ──→ ⏸ 暂停，询问用户确认           │
│    │  (发 RequestInfoEvent → 聊天窗口 → 等用户回复)                │
│    │  (收到 ExternalResponse → 恢复执行)                            │
│    ↓                                                                │
│ [ExecuteRefundExecutor] ────→ 调用 Finance MCP 执行退款             │
│    ↓                                                                │
│ [RestoreCouponExecutor] ────→ 恢复优惠券（如有）                    │
│    ↓                                                                │
│ [SendNotificationExecutor] ──→ 通知用户退款结果                     │
│    ↓                                                                │
│ [DONE]                                                              │
│                                                                     │
│ MAF 对应: Workflow + Executor + RequestPort + ExternalResponse      │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                    ▼

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ Executor Layer   │  │ Skill Layer      │  │ MCP Layer        │
├──────────────────┤  ├──────────────────┤  ├──────────────────┤
│ 确定性业务逻辑   │  │ LLM 领域知识     │  │ 外部系统调用     │
│                  │  │                  │  │                  │
│ GetOrderExecutor │  │ RefundSkill      │  │ Order MCP        │
│ CheckRuleExec    │  │ ExchangeSkill    │  │ Finance MCP      │
│ WaitConfirmExec  │  │ LogisticsSkill   │  │ Member MCP       │
│ ExecuteRefundExec│  │ CRMSkill         │  │ CRM MCP          │
│ SendNotifyExec   │  │ MemberSkill      │  │ WeCom MCP        │
│                  │  │ InvoiceSkill     │  │ Invoice MCP      │
└──────────────────┘  └──────────────────┘  └──────────────────┘

MAF 对应:
• Executor    → Microsoft.Agents.AI.Workflows.Executor<TMessage>
• Skill       → Microsoft.Agents.AI.AgentSkill (SKILL.md / AgentInlineSkill)
• MCP         → Microsoft.Agents.AI.Mcp
```

---

## 三、旁路系统

```text
┌─────────────────────────────────────────────────────────────────────┐
│                    Knowledge Layer                                  │
├─────────────────────────────────────────────────────────────────────┤
│ FAQ / RAG / 产品知识库 / 退款规则库 / 帮助中心                      │
│ MAF 对应: AgentFileSkillsSource (从磁盘扫描知识文件)                │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    Observability Layer                              │
├─────────────────────────────────────────────────────────────────────┤
│ OpenTelemetry / Langfuse / Application Insights                     │
│ MAF 对应: WorkflowBuilder.WithTelemetry() + OpenTelemetry 集成      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    Human Agent Layer                                │
├─────────────────────────────────────────────────────────────────────┤
│ • 人工客服接管 / 人工审批 / 人工兜底 / 高风险审核                   │
│ MAF 对应: RequestPort (工作流暂停) + ExternalResponse (人工回复)    │
│          或 AIAgentHostExecutor (LLM Agent 托管在 Workflow 中)      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 四、目录结构

```
src/
│
├── CallCenter.Framework/                # === 框架核心库（横切关注点）===
│   ├── Session/                         # RedisSessionStore（三合一）
│   ├── Safety/                          # SafetyPipelineAgent + PII 脱敏
│   ├── Compaction/                      # Compaction 一键配置封装
│   ├── Audit/                           # AuditLogger + AuditTrailMiddleware
│   ├── Saga/                            # SagaBuilder 补偿机制
│   ├── Parsing/                         # StructuredOutputParser
│   ├── EventBus/                        # BusinessEventBus
│   ├── Builder/                         # BusinessModuleBuilder
│   ├── Pipeline/                        # StandardPipelineFactory
│   ├── ExternalRequestHandler.cs        # 处理 RequestInfoEvent → 聊天消息
│   └── ServiceCollectionExtensions.cs   # builder.Services.AddCallCenter()
│
├── CallCenter.Gateway/                  # HTTP/WebSocket 接入层
│   ├── Program.cs
│   ├── ChatHub.cs
│   └── GatewayMiddleware.cs
│
├── CallCenter.AgentHost/                # Agent 托管与编排服务
│   ├── Program.cs
│   ├── AgentRegistry.cs
│   ├── EntryPoint.cs                    # 统一入口：检查 session → intent → workflow
│   └── Skills/                          # 业务 Skill 集中管理（AgentClassSkill）
│       ├── RefundSkill.cs
│       ├── ExchangeSkill.cs
│       └── LogisticsSkill.cs
│
├── CallCenter.Workflows/                # 业务流程（业务规则在这里）
│   ├── Refund/
│   │   ├── RefundWorkflow.cs
│   │   ├── RefundConfiguration.cs
│   │   ├── RefundMessages.cs
│   │   └── Executors/
│   ├── Exchange/
│   └── Shared/
│
└── CallCenter.Shared/
    ├── Mcp/                             # MCP Client 统一封装
    │   ├── OrderMcpClient.cs
    │   ├── FinanceMcpClient.cs
    │   └── ...
    ├── Models/                          # 全局公共 DTO
    └── Extensions/
```

### 目录设计说明

**1. Skills 为什么用 `AgentClassSkill` 代码定义而不是 SKILL.md 文件？**

Skill 需要调用 `OrderMcpClient.GetOrder()`、`FinanceMcpClient.Refund()` 等业务服务，脚本必须是 C# 方法才能通过 DI 直接注入 MCP Client。SKILL.md 的脚本只能是 Python/Shell，无法直接调用后端服务。`AgentClassSkill<T>` 提供了类型安全、编译时检查、DI 支持，最适合业务入口类 Skill。

MAF 提供了 5 种 Skill 定义方式，我们的选择对比：

| 方式 | 脚本语言 | DI 支持 | 类型安全 | 适用场景 | 我们用它？ |
|---|---|---|---|---|---|
| 文件 Skill（SKILL.md） | Python/Shell | 不支持 | 无 | 运营维护的知识库类技能 | ❌ |
| 代码定义（AgentInlineSkill） | C# 委托 | 支持 | 中 | 快速原型 | ❌ |
| **类定义（AgentClassSkill）** | **C# 方法** | **支持** | **高** | **业务入口类技能** | **✅** |
| 混合（Mixed） | 多种 | 支持 | 中 | 多来源共存 | ❌ |
| DI 增强（SkillsWithDI） | C# 方法 | 完整 | 高 | 需要访问业务服务的技能 | ✅（合并到类定义） |

**2. MCP Client 为什么放在 `Shared/Mcp/`？**

MCP Client 是后端服务的 SDK 封装，不属于业务逻辑也不属于框架。多个 Workflow 可能共享同一个 MCP Client（如退款和换货都要调 OrderMcpClient），所以放在 Shared 避免重复。

**3. ExternalRequestHandler 为什么移到 Framework？**

`ExternalRequestHandler` 负责将 Workflow 的 `RequestInfoEvent` 转为聊天消息，将用户回复转为 `ExternalResponse`。这是所有业务模块共用的横切逻辑，不应放在 Gateway（Gateway 只负责 HTTP 接入）。

### 目录设计原则

1. **框架独立项目** — `CallCenter.Framework` 包含所有横切关注点（会话、安全、审计等）
2. **业务模块自包含** — Workflow + Executors + Messages + Configuration 在 `Workflows/Refund/` 同一文件夹
3. **Skill 集中管理** — 放在 `AgentHost/Skills/`，方便 LLM 统一发现和路由
4. **MCP Client 放 Shared** — 多个 Workflow 共享同一套后端服务 SDK
5. **ExternalRequestHandler 放 Framework** — 所有业务共用的横切逻辑
6. **新增业务 = 复制一个模块文件夹** — 改 Workflow 和 Executors，不影响现有模块
7. **共享 Executor 放 Shared** — 避免重复（如 GetOrder 退款换货都要用）

### 4.1 Skill 定义方式详解

每个业务 Skill 通过 `AgentClassSkill<T>` 定义，包含 Frontmatter（元数据）、Instructions（LLM 使用指南）、以及可选的 Resources（参考数据）和 Scripts（可执行操作）。

**退款 Skill 示例：**

```csharp
// CallCenter.AgentHost/Skills/RefundSkill.cs
internal sealed class RefundSkill : AgentClassSkill<RefundSkill>
{
    // 1. Frontmatter — LLM 用来发现和路由
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "refund",
        "处理用户退款请求。当用户要求退款、退货、取消订单时使用。"
        "支持查询订单、校验退款资格、计算退款金额、执行退款。");

    // 2. Instructions — 告诉 LLM 如何使用这个技能
    protected override string Instructions => """
        当用户要求退款时使用此技能。

        1. 获取订单号（如果用户未提供，使用 get_recent_orders 脚本获取最近订单）
        2. 系统将自动处理退款流程，包括资格校验、金额计算、用户确认
        3. 退款完成后通知用户结果
        """;

    // 3. Script — 可执行操作（直接调用 MCP Client）
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

**换货 Skill 示例：**

```csharp
// CallCenter.AgentHost/Skills/ExchangeSkill.cs
internal sealed class ExchangeSkill : AgentClassSkill<ExchangeSkill>
{
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "exchange",
        "处理用户换货请求。当用户要求换货、更换商品时使用。"
        "支持查询订单、校验换货资格、生成换货单、安排物流取件。");

    protected override string Instructions => """
        当用户要求换货时使用此技能。

        1. 获取订单号和换货原因
        2. 系统将自动处理换货流程
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
}
```

**Skill 注册：**

```csharp
// Program.cs
var skillsProvider = new AgentSkillsProviderBuilder()
    .UseSkill(new RefundSkill())
    .UseSkill(new ExchangeSkill())
    .UseSkill(new LogisticsSkill())
    .Build();

var intentAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "客服意图识别",
    AIContextProviders = [skillsProvider],
}, services: serviceProvider);
```

**为什么不用 SKILL.md 文件方式？**

| 维度 | SKILL.md 文件 | AgentClassSkill 代码定义 |
|---|---|---|
| 脚本语言 | Python/Shell | C# 方法 |
| 能否直接调用 MCP Client | ❌ 需要额外 HTTP 调用 | ✅ DI 直接注入 |
| 类型安全 | ❌ 运行时检查 | ✅ 编译时检查 |
| 脚本执行 | 需要子进程执行器 | 直接在进程中执行 |
| 适合场景 | 运营维护的知识库/FAQ | 需要调用后端服务的业务入口 |

**结论：** 知识库类技能（退款政策解读、FAQ）可以用 SKILL.md，但业务入口类技能（退款、换货、物流）必须用 `AgentClassSkill`。

---

## 五、核心机制详解

### 5.1 用户请求处理流程

```
用户: "我要退款"
  │
  ▼
Gateway 接收消息 → 鉴权 / 识别用户 → 构造消息对象
  │
  ▼
EntryPoint 处理:
  1. 从 RedisSessionStore 加载 Session（含聊天历史 + Workflow Checkpoint）
  2. 检查 StateBag["activeWorkflow"]
     → 有且意图匹配 → Resume Workflow
     → 有但意图不匹配 → 终止旧流程 → 走 Intent Router
     → 无 → 走 Intent Router
  │
  ▼
Intent Router（AIAgent + StructuredOutputParser）:
  • LLM 分析用户意图 → StructuredOutputParser 转为强类型
  • 有意图 → 匹配 Workflow → 构造初始消息类型
  • 无意图（闲聊/问候）→ 走对话 Agent 自由回复
  │
  ▼
如果启动了 Workflow:
  StateBag["activeWorkflow"] = "RefundWorkflow"
  StateBag["currentStep"] = "GetOrder"
  InProcessExecution.RunAsync(workflow, initialMessage)
如果恢复 Workflow（Resume）:
  从 RedisSessionStore 加载 checkpoint
  InProcessExecution.RunAsync(workflow, userMessage, checkpoint)
  │
  ▼
Workflow 执行:
  GetOrderExecutor → CheckRefundRuleExecutor → ...
  遇到 RequestPort → 发出 RequestInfoEvent → 外部处理
  遇到 Executor → 执行业务逻辑 → 发消息到下一个节点
  完成 → YieldOutput → Session.StateBag 清除 activeWorkflow
  │
  ▼
结果返回给 Gateway → 推送给用户
```

**关键说明：**

- **Entry Point 是 AIAgent 和 Workflow 的桥梁** — 意图识别通过 AIAgent，流程执行通过 Workflow
- **新启动 Workflow** — `InProcessExecution.RunAsync(workflow, initialMessage)`，消息类型为 Workflow 初始消息
- **恢复 Workflow（Resume）** — `InProcessExecution.RunAsync(workflow, userMessage, checkpoint)`，checkpoint 从 RedisSessionStore 加载
- **Intent Router 的输出是强类型对象** — `StructuredOutputParser` 将 LLM JSON 转为 `IntentResult`
- **无意图时不启动任何 Workflow** — 直接交给对话 AIAgent 自然回复
- **对话 Agent 和业务 Workflow 是并行分支** — Entry Point 根据意图决定走哪条路

### 5.2 工作流暂停与用户交互（RequestPort）

```
Workflow 执行到需要用户确认的步骤:

  ExecuteRefundExecutor
    ↓
  WaitUserConfirmExecutor 发消息到 RequestPort("ConfirmRefund")
    ↓
  ⏸ Workflow 暂停，发出 RequestInfoEvent
    ↓
  ExternalRequestHandler 收到 RequestInfoEvent
    ↓
  通过聊天窗口发送给用户:
    "订单 A001，退款金额 ¥100.00，确认退款？(回复'确认'或'取消')"
    ↓
  用户回复: "确认"
    ↓
  ExternalRequestHandler 构造 ExternalResponse("确认")
  发送到 Workflow
    ↓
  ⏸ Workflow 恢复执行
    ↓
  ExecuteRefundExecutor 继续 → 调用 Finance MCP 执行退款
```

### 5.3 新增业务模块步骤

以"新增换货 Exchange"为例：

```
Step 1: 复制 CallCenter.Workflows/Refund/ → CallCenter.Workflows/Exchange/
Step 2: 重命名所有文件中的 Refund → Exchange
Step 3: 修改 ExchangeWorkflow.cs 的流程步骤
Step 4: 修改 Executors 中的业务逻辑
Step 5: 新增 CallCenter.AgentHost/Skills/ExchangeSkill.cs
Step 6: 在 Program.cs 中添加一行 builder.Services.AddCallCenterBusinessModule("exchange")...
Step 7: 完成 — 退款模块完全不受影响
```

### 5.4 异常场景处理

#### 场景 1: 用户中途换意图

```
用户: "我要退款"
系统: 进入 RefundWorkflow → 展示订单 → 等待用户确认
用户: "我不想退了，我要换货"

处理:
1. 系统识别到新意图 "exchange"
2. 终止当前 RefundWorkflow（标记为 UserCancelled）
3. 清除 Session.StateBag["activeWorkflow"]
4. 进入 ExchangeWorkflow
5. RedisSessionStore 中记录意图切换（用于审计）
```

**框架支持：** 入口处的 Intent Router 在检测到新意图时，如果 Session.StateBag["activeWorkflow"] 存在但意图不匹配，自动终止旧流程并启动新流程。

#### 场景 2: Workflow 执行失败（如支付网关超时）

```
执行到 ExecuteRefundExecutor → 调用 Finance MCP 超时

处理:
1. Executor 抛出异常 → MAF 发出 ExecutorFailedEvent
2. Saga 补偿机制自动触发:
   - 恢复优惠券（如果已扣减）
   - 通知用户 "退款处理中，请稍后查看"
   - 标记流程为 PendingRetry
3. 后台重试策略:
   - 第 1 次重试: 1 分钟后
   - 第 2 次重试: 5 分钟后
   - 第 3 次重试: 30 分钟后
   - 超过最大重试次数 → 标记为 Failed → 转人工
```

**框架支持：** `SagaBuilder` 定义补偿规则，`BusinessEventBus` 发布失败事件触发告警。

#### 场景 3: Workflow 超时未响应

```
系统发送 "确认退款 ¥100？" → 等待用户回复
用户 30 分钟未回复

处理:
1. Gateway 检测到 Session 最后活跃时间 > 30 分钟
2. 自动发送 "您还在吗？回复'确认'继续退款流程，回复其他重新开始"
3. 再过 30 分钟未回复 → 终止流程，标记为 Expired
4. 通知用户 "您的退款请求已超时，如需退款请重新发起"
```

**框架支持：** Gateway 中间件中内置超时检测逻辑，定期扫描 Redis Session。

#### 场景 4: 用户同时有多个意图

```
用户: "我要退款，顺便查下物流"

处理:
1. LLM Intent Router 识别为多意图: ["refund", "logistics"]
2. 优先处理主意图（退款），副意图（物流）进入队列
3. 退款流程完成后，继续处理物流查询
4. 回复用户: "退款已处理完成。物流信息如下: ..."
```

**框架支持：** Intent Router 输出支持多意图列表（`List<IntentResult>`），Entry Point 按优先级顺序调度。

#### 场景 5: 用户回复不在预期范围内

```
系统: "确认退款 ¥100？回复'确认'或'取消'"
用户: "你们这服务太差了，我要投诉"

处理:
1. 收到用户消息，发现不是预期的 "确认" / "取消"
2. LLM 识别为新意图 "complaint"
3. 当前流程挂起（不终止，保留状态）
4. 进入投诉流程（转人工或走投诉 Workflow）
5. 投诉处理完后可回到退款流程
```

**框架支持：** RequestPort 收到非预期输入时，Entry Point 的 Intent Router 会重新识别意图，决定是继续当前流程还是切换到新流程。

#### 场景 6: 用户无意图（闲聊、问候、纯抱怨）

```
用户: "你好"  或  "今天天气怎么样"  或  "你们这什么破服务"

处理:
1. LLM Intent Router 识别为 "no_intent"
2. 不走任何 Workflow
3. 路由到对话 Agent（自由对话模式）
4. 对话 Agent 根据上下文自然回复
5. 用户下次发送有业务意图的消息时，重新走正常流程
```

**框架支持：** Entry Point 检测到意图列表为空或只有 "no_intent"，直接交给对话 AIAgent 处理，不启动任何 Workflow。

---

## 六、MAF 核心概念映射

| 业务概念 | MAF 类/API | 作用 |
|---|---|---|
| 会话状态 | `AgentSession` + `StateBag` | 保存 active workflow、当前步骤、中间数据 |
| 会话持久化 | `RedisSessionStore`（框架层） | 聊天历史 + Session 序列化 + Checkpoint 三合一 |
| 意图识别 | `AIAgent` + `AgentSkillsProvider` | LLM 分析用户意图，Skill 描述触发条件 |
| LLM 输出强类型化 | `StructuredOutputParser`（框架层） | 包装 LLM 调用，将 JSON 输出转为强类型对象 |
| 流程定义 | `WorkflowBuilder` → `Workflow` | 有向图：Executor 为节点，Edge 为连线 |
| 流程执行 | `InProcessExecution.RunAsync()` / `StreamingRun` | 执行 Workflow，监听事件 |
| 节点逻辑 | `Executor<TMessage>` | 确定性业务逻辑（查订单、算金额等） |
| LLM 节点 | `AIAgentHostExecutor` | 将 AIAgent 嵌入 Workflow 作为节点 |
| 人工介入 | `RequestPort<TRequest, TResponse>` | 工作流暂停，等待外部输入 |
| 外部请求 | `ExternalRequest` / `RequestInfoEvent` | 工作流发出的"我需要信息"事件 |
| 外部响应 | `ExternalResponse` | 外部世界（用户/人工）的回复 |
| 流程状态持久化 | `CheckpointManager` / `DurableTask` | 崩溃恢复、跨请求持久化 |
| 子流程 | `SubworkflowBinding` | 大流程嵌套小流程 |
| 技能声明 | `AgentClassSkill<T>` | 告诉 LLM 这个业务能力是什么、何时触发，支持 DI 注入 MCP Client |
| 外部系统调用 | `McpClient` → `TaskAwareMcpClientAIFunction` | 通过 MCP 协议调用后端服务 |
| 可观测性 | `WorkflowBuilder.WithTelemetry()` | OpenTelemetry 集成 |
| 业务事件 | `BusinessEventBus`（框架层） | 发布/订阅业务事件（退款完成、高风险告警等） |
| 内容安全 | `SafetyPipelineAgent`（框架层） | 输入/输出双路过滤（PII 脱敏、关键词拦截、注入检测） |

---

## 七、框架层设计（CallCenter AI Framework）

> **定位：** 在 MAF 之上构建一层薄框架，弥补 LangChain/LangGraph 式的便利性缺失，降低团队学习成本和开发门槛。
>
> **原则：** 只做 MAF 没有的、或 MAF 有但配置太繁琐的事。不封装 MAF 已做得好的东西。

### 7.1 MAF 已有 vs 框架需补充

| 能力 | MAF 现状 | 框架层要做的事 |
|---|---|---|
| **消息压缩/Compaction** | 已有 `CompactionProvider` + 多种策略 | **不需要做** — 直接封装一键配置 |
| **会话管理（三合一）** | 有 `ChatHistoryProvider`、`AgentSession.Serialize`、`CheckpointManager` 三个抽象，但无持久化层 | **需要补充** — `RedisSessionStore` 一次性解决聊天历史 + Session 序列化 + Workflow Checkpoint |
| **内容安全/脱敏** | 有 `Redactor` 仅用于日志 | **需要补充** — 输入/输出双路过滤管道 |
| **Tool 审批** | 有 `ToolApprovalAgent` | **不需要做** — 直接封装一键配置 |
| **日志/可观测** | 有 `LoggingAgent` + `OpenTelemetryAgent` | **不需要做** — 直接封装一键配置 |
| **审计日志** | 无可用的业务审计组件 | **需要补充** — 自动捕获每个 Workflow Step 输入/输出、防篡改存储 |
| **Saga 补偿** | Workflow 有基础 error handling | **需要补充** — 快捷补偿构建器 |
| **LLM 输出解析** | 无输出解析中间件 | **需要补充** — `StructuredOutputParser` 将 LLM JSON 转为强类型对象 |
| **业务事件总线** | 无可用的业务级事件系统 | **需要补充** — `BusinessEventBus` 发布/订阅业务事件 |
| **开发者体验** | 有 `AIAgentBuilder` / `WorkflowBuilder` | **需要补充** — CallCenter 场景的快捷 Builder |

### 7.2 框架层架构

```text
┌─────────────────────────────────────────────────────────────────────┐
│                CallCenter AI Framework（本层）                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │ Session Store   │  │ Safety Pipeline  │  │ Audit Logger     │   │
│  │ (三合一)        │  │                  │  │                  │   │
│  │ • 聊天历史      │  │ Input Filter     │  │ Auto-capture     │   │
│  │ • Session 持久化│  │   • PII Redact   │  │ Step In/Out      │   │
│  │ • Checkpoint    │  │   • Keyword Block│  │ Immutable store  │   │
│  │ • TTL 过期      │  │   • Prompt Inj.  │  └──────────────────┘   │
│  └────────┬────────┘  │ Output Filter    │                         │
│           │           │   • PII Redact   │  ┌──────────────────┐   │
│           ▼           └────────┬─────────┘  │ Saga Compensation│   │
│  ┌────────────────────────────┤            │                  │   │
│  │    Agent Pipeline          │            │ Step A fails     │   │
│  │                            │            │ → Step B undo    │   │
│  │  [SafetyInput] → [Logging] │            │                  │   │
│  │    → [Compaction] → [LLM]  │            └──────────────────┘   │
│  │    → [SafetyOutput]        │                                   │
│  └────────────────────────────┘                                   │
│           │                                                       │
│           ▼           ┌──────────────────┐                        │
│  ┌───────────────────┐│ DevX Builder     │                        │
│  │ Compaction 封装   ││ .AddCallCenter() │                        │
│  │ Token 阈值自动    ││ .AddBusinessMod. │                        │
│  │ 小模型摘要        ││   .WithSkill()   │                        │
│  └───────────────────┘│   .WithWorkflow()│                        │
│                       └──────────────────┘                        │
│                                                                     │
│  注: StructuredOutputParser 是 LLM 调用的包装器，不是独立管道层     │
│  注: EventBus 是 Executor 内部主动发布事件，不在 AIAgent 管道中     │
│                                                                     │
└─────────────────────────────────┬───────────────────────────────────┘
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Microsoft Agent Framework（MAF 原生）                   │
│  AIAgent / Workflow / Executor / AgentSkill / MCP / Session 等      │
└─────────────────────────────────────────────────────────────────────┘
```

### 7.3 核心组件详解

#### 7.3.1 Session Manager（Redis 会话管理）

MAF 有 `ChatHistoryProvider`、`AgentSession.Serialize`、`CheckpointManager` 三个抽象，但都需要自己实现持久化层。框架提供三合一的 `RedisSessionStore`：

```csharp
// 框架提供 — 一个类解决三个问题：
public class RedisSessionStore
{
    // 1. 聊天历史：按 SessionId 精确存取 List<ChatMessage>（非向量搜索）
    // 2. AgentSession 序列化：Session → JSON → Redis
    // 3. Workflow Checkpoint：流程状态 → Redis，支持断点恢复
    // 4. TTL 自动过期：超过 30/90 天自动清理
    // 5. 分布式支持：多实例共享同一 Redis，无状态部署
}
```

**开发者使用方式（一行配置）：**

```csharp
// Program.cs
builder.Services.AddCallCenter()
    .UseRedisSession(options =>
    {
        options.ConnectionString = config["Redis:Connection"];
        options.DefaultTtl = TimeSpan.FromDays(30);
        options.MaxMessagesPerSession = 100; // 超过自动截断
    });
```

#### 7.3.2 Safety Pipeline（内容安全管道）

客服场景对输入输出都有安全要求。MAF 有 `Redactor` 但只在日志中用，没有运行时的内容过滤管道。

```csharp
// 框架提供：
public class SafetyPipelineAgent : DelegatingAIAgent
{
    // Input Filter（用户消息 → 过滤 → 传给下游）
    //   • PII 脱敏（手机号、身份证、银行卡 → ***）
    //   • 关键词拦截（辱骂、恶意攻击 → 拦截+提示）
    //   • Prompt Injection 检测（"忽略之前指令" → 拦截）
    //
    // Output Filter（AI 回复 → 过滤 → 返回用户）
    //   • PII 脱敏（内部系统地址、内部工号 → ***）
    //   • 敏感回复拦截（不确定/高风险 → 转人工）
    //   • 格式规范化（统一输出格式）
}
```

**开发者使用方式：**

```csharp
// Program.cs — 配置安全管道
builder.Services.AddCallCenterSafetyPipeline()
    .WithPiiRedaction()              // 手机号/身份证/银行卡自动脱敏
    .WithKeywordFilter("rules.json") // 关键词拦截规则
    .WithPromptInjectionDetection()  // Prompt 注入检测
    .WithOutputFilter();             // 输出安全过滤
```

**PII 脱敏规则示例：**

```json
// safety/pii-rules.json
{
  "patterns": [
    {
      "name": "phone",
      "regex": "(1[3-9]\\d)\\d{4}(\\d{4})",
      "replace": "$1****$2",
      "description": "手机号中间4位脱敏"
    },
    {
      "name": "idcard",
      "regex": "(\\d{6})\\d{8}(\\d{4})",
      "replace": "$1********$2",
      "description": "身份证生日8位脱敏"
    }
  ]
}
```

#### 7.3.3 Compaction 一键配置（MAF 已有，封装易用性）

MAF 的 `CompactionProvider` 功能很强，但配置繁琐。框架封装为快捷配置。

```csharp
// MAF 原生写法（繁琐）：
var compactionProvider = new CompactionProvider(
    new SummarizationCompactionStrategy(
        chatClient: smallModelClient,
        trigger: CompactionTriggers.WhenTokenCountExceeds(8000),
        minimumPreservedGroups: 8
    ),
    stateKey: "refund-compaction"
);

// 框架封装（简洁）：
builder.Services.AddCallCenterCompaction()
    .UseSummarization(options =>
    {
        options.TokenThreshold = 8000;    // 超过 8000 token 触发压缩
        options.PreserveRecentTurns = 8;  // 保留最近 8 轮不压缩
        options.SmallModel = "gpt-4o-mini"; // 用小模型做摘要，省钱
    });

// 或更简单 — 默认配置即可用：
builder.Services.AddCallCenterCompaction(); // 默认 8k token, 保留 8 轮, 小模型摘要
```

#### 7.3.4 DevX Builder（开发者体验 — 快速注册业务能力）

这是框架最重要的部分。目标是让团队新人 **5 分钟内注册一个新的业务能力**。

```csharp
// 开发者只需要写这些：

// 方式一：快捷注册（推荐，适合简单场景）
builder.Services.AddCallCenterBusinessModule("refund")
    .WithSkill<RefundSkill>()               // LLM 能力描述（AgentClassSkill）
    .WithWorkflow<RefundWorkflow>();         // 流程定义（自动注入 MCP Client）

// 方式二：自定义注册（适合复杂场景）
builder.Services.AddCallCenterBusinessModule("exchange")
    .ConfigureWorkflow((builder, services) =>
    {
        // 完全手动控制 WorkflowBuilder
        var orderPort = builder.AddExecutor<OrderQueryExecutor>("QueryOrder");
        var checkPort = builder.AddExecutor<CheckExchangeRuleExecutor>("CheckRule");
        var confirmPort = builder.AddRequestPort<ExchangeSignal, UserResponse>("UserConfirm");
        var execPort = builder.AddExecutor<ExecuteExchangeExecutor>("Execute");

        builder.AddEdge(orderPort, checkPort);
        builder.AddEdge(checkPort, confirmPort);
        builder.AddEdge(confirmPort, execPort);
        builder.WithOutputFrom(execPort);
    })
    .WithSkill<ExchangeSkill>();
```

**对比 MAF 原生写法：**

```csharp
// MAF 原生需要写的代码量（约 30+ 行）：
var workflow = new WorkflowBuilder(startExecutor)
    .AddEdge(executorA, executorB)
    .ForwardMessage<OrderFound>(executorB, executorC)
    .ForwardExcept<OrderNotFound>(executorB, errorHandler)
    .AddEdge(requestPort, executorC)
    .WithOutputFrom(executorC)
    .WithTelemetry(...)
    .Build();

var agent = new AIAgentBuilder(chatClient.AsAIAgent())
    .UseAIContextProviders(skillsProvider, compactionProvider, memoryProvider)
    .WithOpenTelemetry()
    .WithLogging(loggerFactory)
    .Build();

// 框架封装后（约 5 行）：
builder.Services.AddCallCenterBusinessModule("refund")
    .WithSkill<RefundSkill>()
    .WithWorkflow<RefundWorkflow>()
    .WithDefaults(); // 自动注入 compaction/telemetry/logging/safety/eventbus
```

### 7.4 Agent Pipeline 管道顺序

> **注意：** 这里描述的是 **AIAgent（意图识别/对话）的中间件管道**，不是 Workflow 的执行流程。
> Workflow 有自己的执行流（Executor → Edge → Executor），不经过这个管道。

```
用户消息（通过 Gateway → Entry Point）
  │
  ▼
┌─[1] SafetyInputFilter─────────────────┐  框架层
│  • PII 脱敏（手机号/身份证/银行卡）     │
│  • 关键词拦截                           │
│  • Prompt Injection 检测               │
└───────────────────────────────────────┘
  │
  ▼
┌─[2] LoggingAgent──────────────────────┐  MAF 原生
│  • 记录操作日志（不记 Trace 级别内容）  │
└───────────────────────────────────────┘
  │
  ▼
┌─[3] CompactionProvider────────────────┐  MAF 原生
│  • 超过 token 阈值时压缩历史消息        │
│  • 小模型做摘要，保留关键事实           │
└───────────────────────────────────────┘
  │
  ▼
┌─[4] ToolApprovalAgent─────────────────┐  MAF 原生
│  • 工具调用审批规则检查                 │
│  • "始终允许"的 MCP 自动放行            │
└───────────────────────────────────────┘
  │
  ▼
┌─[5] LLM (ChatClient) + StructuredOutputParser ──┐  MAF 原生 + 框架
│  • 实际的模型调用                                │
│  • Skills 作为工具暴露                            │
│  • StructuredOutputParser 包装调用 → 强类型输出   │
└──────────────────────────────────────────────────┘
  │
  ▼
┌─[6] SafetyOutputFilter────────────────┐  框架层
│  • 输出 PII 脱敏                        │
│  • 高风险内容拦截 → 转人工              │
│  • 格式规范化                           │
└───────────────────────────────────────┘
  │
  ▼
返回给 Entry Point（决定启动 Workflow 或回复用户）
```

**StructuredOutputParser 不在 Pipeline 中作为独立一层**，而是作为 LLM 调用的包装器：

```csharp
// 意图识别时内部使用 StructuredOutputParser：
var intentAgent = new AIAgentBuilder(chatClient.AsAIAgent())
    .WithStructuredOutput<IntentResult>()  // LLM 输出自动转为强类型
    .Build();

// 使用时直接拿到类型安全的对象：
var result = await intentAgent.RunAsync<IntentResult>("用户要退款");
// result.Intent == "refund", result.OrderId == "A001"
```

**EventBus 不在 Pipeline 中**，而是由 Executor 内部主动发布事件：

```csharp
// Executor 内部发布：
public override async ValueTask HandleAsync(RefundMessage msg, IWorkflowContext ctx)
{
    await _eventBus.PublishAsync(new RefundCompletedEvent { ... });
}
```

### 7.5 框架层目录结构

**核心原则：Framework 管横切，Business Module 管纵切。**

| 归属 | 示例 |
|---|---|
| **Framework** | 会话管理、安全管道、审计日志、消息压缩、Saga 补偿、开发者 Builder、输出解析、事件总线 |
| **Business Module** | 优惠券分摊、退款频率限制、品类规则、风控策略、具体补偿动作 |

```
src/
│
├── CallCenter.Framework/                # === 框架核心库（所有业务通用）===
│   ├── CallCenter.Framework.csproj
│   │
│   ├── Session/                         # 会话管理（三合一）
│   │   ├── RedisSessionStore.cs         # 聊天历史 + AgentSession 序列化 + Workflow Checkpoint
│   │   ├── SessionExtensions.cs         # IServiceCollection 扩展
│   │   └── SessionOptions.cs            # TTL、最大消息数、序列化配置
│   │
│   ├── Safety/                          # 内容安全管道
│   │   ├── SafetyPipelineAgent.cs       # 输入/输出双路过滤 Agent
│   │   ├── PiiRedactor.cs               # PII 脱敏（手机号/身份证/银行卡）
│   │   ├── KeywordFilter.cs             # 关键词拦截
│   │   ├── PromptInjectionDetector.cs   # Prompt 注入检测
│   │   ├── SafetyExtensions.cs          # IServiceCollection 扩展
│   │   └── SafetyOptions.cs             # 配置项
│   │
│   ├── Compaction/                      # 消息压缩（MAF 封装）
│   │   ├── CompactionExtensions.cs      # 一键配置扩展
│   │   └── CompactionOptions.cs         # Token 阈值、保留轮数等
│   │
│   ├── Audit/                           # 审计日志
│   │   ├── AuditLogger.cs               # 结构化审计日志
│   │   ├── AuditTrailMiddleware.cs      # 自动捕获每个 Workflow Step 的输入/输出
│   │   └── AuditExtensions.cs           # IServiceCollection 扩展
│   │
│   ├── Saga/                            # 容错补偿
│   │   ├── SagaBuilder.cs               # 定义 "如果 A 失败，执行 B 补偿"
│   │   └── SagaExtensions.cs            # Workflow 补偿扩展
│   │
│   ├── Parsing/                         # LLM 输出解析
│   │   ├── StructuredOutputParser.cs    # LLM JSON 输出 → 强类型对象
│   │   └── ParsingExtensions.cs         # IServiceCollection 扩展
│   │
│   ├── EventBus/                        # 业务事件
│   │   ├── BusinessEventBus.cs          # 发布/订阅业务事件
│   │   └── EventBusExtensions.cs        # IServiceCollection 扩展
│   │
│   ├── Builder/                         # 开发者体验
│   │   ├── BusinessModuleBuilder.cs     # AddCallCenterBusinessModule("refund")...
│   │   ├── DefaultPipelineBuilder.cs    # 标准管道一键构建
│   │   └── ModuleRegistrar.cs           # 模块自动发现/注册
│   │
│   ├── Pipeline/                        # Agent 中间件管道
│   │   ├── StandardPipelineFactory.cs   # 构建标准管道
│   │   └── PipelineOptions.cs           # 管道配置
│   │
│   └── ServiceCollectionExtensions.cs   # 统一入口: builder.Services.AddCallCenter()
│
├── CallCenter.Gateway/                  # 接入层
├── CallCenter.AgentHost/                # Agent 托管
├── CallCenter.Workflows/                # 业务流程（业务规则在这里）
└── CallCenter.Shared/                   # 公共组件
```

### 7.5.1 Framework vs Business Module 边界

```
CallCenter.Framework/                    ← 框架管的事（所有业务通用）
├── Session/       会话持久化、消息存储（三合一：聊天历史 + Session 序列化 + Checkpoint）
├── Safety/        PII 脱敏、Prompt 注入检测、关键词拦截
├── Compaction/    消息压缩一键配置
├── Audit/         审计日志自动捕获
├── Saga/          补偿机制通用模式
├── Parsing/       LLM 输出 → 强类型对象
├── EventBus/      业务事件发布/订阅
├── Builder/       开发者快捷注册
└── Pipeline/      标准 Agent 中间件管道

CallCenter.Workflows/Refund/             ← 业务模块管的事（退款专属）
├── RefundWorkflow.cs                    # 退款流程定义
├── RefundConfiguration.cs
├── Executors/
│   ├── CheckRefundRuleExecutor.cs       # ← 业务规则：7天内可退、品类限制
│   ├── CalculateRefundAmountExecutor.cs # ← 业务计算：优惠券分摊、运费处理
│   ├── RiskCheckExecutor.cs             # ← 业务风控：退款频率、异常金额
│   ├── WaitUserConfirmExecutor.cs
│   ├── ExecuteRefundExecutor.cs
│   └── RestoreCouponExecutor.cs         # ← 业务动作：券有效期判断、是否恢复
└── Compensation/                        # ← 退款特有的补偿逻辑
    ├── OnRefundFailedCompensation.cs    # 退款失败 → 恢复优惠券
    └── OnTimeoutCompensation.cs         # 支付超时 → 通知用户
```

**判断标准：**

| 问题 | 放 Framework？ |
|---|---|
| 退款/换货/物流都需要吗？ | 是 → Framework；否 → Business Module |
| 与安全/合规强相关？ | 是 → Framework；否 → 看是否通用 |
| 不同业务的实现逻辑完全不同？ | 是 → Business Module（如优惠券分摊规则） |
| 是降低开发门槛的抽象？ | 是 → Framework |

### 7.5.2 新增组件详解

#### 7.5.2.1 RedisSessionStore（三合一）

MAF 有 `ChatHistoryProvider`、`AgentSession.Serialize`、`CheckpointManager` 三个抽象，但都需要自己实现持久化层。RedisSessionStore 一次性搞定三件事：

```csharp
// 框架提供 — 一个类解决三个问题：
public class RedisSessionStore
{
    // 1. 聊天历史：按 SessionId 精确存取 List<ChatMessage>
    // 2. AgentSession 序列化：Session → JSON → Redis
    // 3. Workflow Checkpoint：流程状态 → Redis，支持断点恢复
    // 4. TTL 自动过期：超过 30/90 天自动清理
    // 5. 分布式支持：多实例共享同一 Redis，无状态部署
}
```

**开发者使用：**

```csharp
builder.Services.AddCallCenter()
    .UseRedisSession(options =>
    {
        options.ConnectionString = config["Redis:Connection"];
        options.DefaultTtl = TimeSpan.FromDays(30);
        options.MaxMessagesPerSession = 100;
    });
```

**为什么不用 DB 版？**

- 客服场景是精确按 SessionId 存取，不需要 SQL 查询
- 历史对话归档通过 Redis RDB 持久化即可
- DB 版等后台真的需要"全文搜索历史消息"时再加

#### 7.5.2.2 StructuredOutputParser

Intent Router、Executor、MCP 调用等场景都需要从 LLM 输出中拿到强类型结果。MAF 没有输出解析中间件，但这是 LangGraph 的标配。

```csharp
// 框架提供：
public class StructuredOutputParser<TOutput> : DelegatingAIAgent
    where TOutput : class
{
    // • 自动在 system prompt 中注入 JSON Schema
    // • 自动解析 LLM 返回的 JSON → TOutput
    // • 解析失败自动重试
}
```

**开发者使用：**

```csharp
// 意图识别直接拿到强类型：
var intent = await agent.RunAsync<RefundIntent>("用户要退款，订单A001");
// intent.OrderId == "A001"
// intent.Reason == "changed_mind"

// 定义意图类：
public record RefundIntent(
    string? OrderId,
    string? Reason,
    bool? PartialRefund
);
```

#### 7.5.2.3 BusinessEventBus

所有业务模块都需要对外发布事件，比如退款完成 → 发短信、高风险 → 通知主管。没有它每个模块都自己搞事件发布，代码重复。

```csharp
// 框架提供：
public class BusinessEventBus
{
    // • 发布业务事件（退款完成、流程超时、高风险等）
    // • 订阅事件触发下游动作（发短信、通知主管、写日志）
    // • 事件带上下文（sessionId、userId、orderId、金额等）
}
```

**开发者使用：**

```csharp
// 业务模块中发布事件：
await eventBus.PublishAsync(new RefundCompletedEvent
{
    SessionId = "sess-123",
    UserId = "user-456",
    OrderId = "A001",
    RefundAmount = 81.81m,
});

// Program.cs 中订阅：
builder.Services.AddCallCenter()
    .WithEventBus(events =>
    {
        events.On<RefundCompletedEvent>(async e =>
        {
            await smsService.SendAsync(e.UserId,
                $"您的退款 {e.RefundAmount:C} 已处理完成，订单 {e.OrderId}");
        });

        events.On<RiskAlertEvent>(async e =>
        {
            await dingtalk.NotifyAsync($"高风险退款告警: {e.OrderId}");
        });
    });
```

### 7.6 开发者接入指南（目标体验）

```csharp
// ============ Program.cs — 完整配置示例 ============

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

builder.Services.AddCallCenterBusinessModule("exchange")
    .WithSkill<ExchangeSkill>()
    .WithWorkflow<ExchangeWorkflow>();

builder.Services.AddCallCenterBusinessModule("logistics")
    .WithSkill<LogisticsSkill>()
    .WithWorkflow<LogisticsWorkflow>();

var app = builder.Build();
app.MapCallCenterGateway();
app.Run();
```

**说明：**
- MCP Client 统一通过 `AddMcpClients` 注册，不在业务模块中重复绑定
- Skill 通过 `AgentClassSkill<T>` 代码定义，集中注册到 `AgentHost/Skills/`
- 业务模块通过 `WithSkill<T>()` + `WithWorkflow<T>()` 声明自己需要什么能力
- LLM 通过 Skill 的 Frontmatter description 自动发现和选择合适的 Workflow

---

## 八、与原版 PRD 的主要差异

| 原设计 | 新设计 | 原因 |
|---|---|---|
| Intent Router (Rule → Embedding → LLM 三层) | LLM Agent + Keyword fallback | 客服场景不需要三层，LLM 足够，keyword 仅兜底 |
| Planner Agent 独立层 | 合并到 Intent Router | Intent 识别直接输出 workflow 选择，减少延迟 |
| Capability 独立层 | 合并到 Skill (AgentClassSkill) | Capability 就是 Skill 的元数据部分，不需要两层 |
| Step / Skill 两层 | Executor (确定性) + Skill (LLM 知识) | 明确区分：Executor 跑代码，Skill 指导 LLM |
| Session State Router 独立模块 | 合并到 Entry Point | 检查 active workflow 是入口方法的一个 if 分支 |
| Skill 指具体步骤 | Skill 指 LLM 领域知识 | 与 MAF AgentSkill 概念对齐，避免混淆 |
| ChatHistory 存向量库 | RedisSessionStore 三合一 | 客服场景需要精确按 SessionId 存取，不是语义搜索 |
| 无 LLM 输出解析 | StructuredOutputParser | Intent Router / Executor / MCP 都需要强类型输出 |
| 无业务事件系统 | BusinessEventBus | 退款完成 → 发短信、高风险 → 告警，所有业务都需要 |
