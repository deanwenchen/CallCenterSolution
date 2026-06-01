# Phase 1: Foundation - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning

## Phase Boundary

建立项目基础结构（5 个项目），实现 Shared DTO/Mock 服务、Framework 核心组件、RefundWorkflow 完整 6 步执行。所有 v1 的 32 条需求中有 31 条映射到此阶段。

**输入：** OpenSpec spec.md (18 Requirements)，REQUIREMENTS.md (32 v1 需求)
**输出：** dotnet build 成功，退款流程可在控制台端到端跑通

## Implementation Decisions

### 项目结构
- **D-01:** 5 个项目严格按 PRD Section 四 目录结构：CallCenter.Shared / CallCenter.Framework / CallCenter.Workflows / CallCenter.AgentHost / CallCenter.ConsoleDemo
- **D-02:** 项目引用链：ConsoleDemo → AgentHost → Workflows → Framework → Shared，AgentHost 也直接引用 MAF.AI
- **D-03:** CallCenterSolution.slnx 保留现有 4 个 MAF source project 引用 + 新增 5 个 CallCenter 项目
- **D-04:** 目标框架 .NET 10.0，Nullable=enable，ImplicitUsings=enable

### MAF SDK 引用
- **D-05:** 直接引用 agent-framework 源码（ProjectReference），不使用 NuGet 包
- **D-06:** 引用路径：../../GitCode/agent-framework/dotnet/src/{Microsoft.Agents.AI.Abstractions, Microsoft.Agents.AI, Microsoft.Agents.AI.Workflows, Microsoft.Agents.AI.Workflows.Generators}

### 服务注入
- **D-07:** Executor 通过**构造函数注入**服务接口（如 `new GetOrderExecutor(orderService)`），不使用硬编码实例化
- **D-08:** 使用 Microsoft.Extensions.DependencyInjection ServiceCollection 做 DI 容器
- **D-09:** Mock 服务通过 DI 注册：`services.AddSingleton<IOrderService>(new MockOrderService())`

### Executor 输出类型
- **D-10:** 使用 **Executor\<TInput, TOutput\>** 泛型基类，HandleAsync 返回 TOutput，MAF 默认 `AutoSendMessageHandlerResultObject=true` 自动传给下游 executor
- **D-11:** Executor 返回值通过 `.ForwardMessage<T>()` 路由到下游（类型安全边匹配）
- **D-12:** WaitUserConfirmExecutor 无返回值（Executor\<TInput\>），内部用 `context.SendMessageAsync()` 发请求到 ConfirmPort
- **D-13:** SendNotificationExecutor 用 `[YieldsOutput(typeof(RefundNotification))]` 标记最终输出
- **D-14:** GetOrderExecutor 需要发两种消息（OrderFound → CheckRule, RefundSignal → 回到 InfoPort），用 `[SendsMessage(typeof(OrderFound))]` + `[SendsMessage(typeof(RefundSignal))]` 标记，内部用 `context.SendMessageAsync()` 分别指定

### 条件边路由
- **D-15:** 条件路由使用 **AddSwitch 分支模式**（参照 MAF samples 03-workflows/ConditionalEdges/02_SwitchCase）
- **D-16:** 简单无条件路由用 `.AddEdge(source, target)` 或 `.ForwardMessage<T>(source, target)`
- **D-17:** 条件路由用 `.AddSwitch(source, sw => sw.AddCase(condition, target).WithDefault(fallback))`

### 缺参数循环机制
- **D-18:** GetOrderExecutor 缺 orderId 时通过 `context.SendMessageAsync(RefundSignal.NeedOrderId)` 回到 RefundInfoPort
- **D-19:** `.ForwardMessage<RefundSignal>(getOrder, infoPort)` 边条件路由回端口
- **D-20:** 端口发出 RequestInfoEvent → 控制台提示用户 → 用户回复 → ExternalResponse → 恢复执行回到 GetOrderExecutor
- **D-21:** WaitUserConfirmExecutor 通过 `context.SendMessageAsync(new ConfirmRefundRequest(...))` 到 ConfirmPort，`.ForwardMessage<ConfirmRefundRequest>(waitConfirm, confirmPort)` 路由

### Workflow 构建
- **D-22:** WorkflowBuilder 的 start executor 是 **GetOrderExecutor**（不是端口）
- **D-23:** 初始输入直接传给 RunAsync(workflow, RefundIntent)，不经过端口
- **D-24:** 两个 RequestPort（RefundInfoPort + RefundConfirmPort）作为图中的中间节点，不是 start
- **D-25:** RefundWorkflow.Build() 返回 Workflow 对象，ConsoleDemo 直接调用

### MCP Client 接口层
- **D-26:** Shared/Mcp/ 目录定义接口（IOrderMcpClient, IFinanceMcpClient, IMemberMcpClient）
- **D-27:** Shared/Services/ 目录放 Mock 实现（MockOrderService, MockFinanceService, MockMemberService）
- **D-28:** 接口与实现分离，Executor 只依赖接口，不关心是 Mock 还是真实 MCP
- **D-29:** Mock 服务注册到 DI：`services.AddSingleton<IOrderMcpClient>(new MockOrderService())`

### Framework 空壳
- **D-30:** 6 个空壳组件（Safety/Compaction/Audit/Saga/Pipeline/Session-Redis）写**骨架类 + TODO 注释**
- **D-31:** 骨架类包含类定义和空方法，不抛异常，调用即 no-op
- **D-32:** TODO 注释标注 PRD 定义的完整职责

### Framework 可用组件
- **D-33:** EventBus 用内存实现 InMemoryBusinessEventBus，支持发布/订阅
- **D-34:** StructuredOutputParser 实现 JSON 反序列化包装（JsonSerializer.Deserialize）
- **D-35:** Agent Pipeline 简化实现：仅 Logging + LLM，Safety/Compaction/ToolApproval 为空壳
- **D-36:** InMemorySessionStore 基础实现（字典存储），RedisSessionStore 为空壳

### 消息类型
- **D-37:** 所有消息使用 C# record 定义（RefundIntent, OrderFound, RefundRuleResult, ConfirmRefundRequest, UserConfirmation, RefundExecuted, CouponRestored, RefundNotification）
- **D-38:** RefundSignal 为 enum（Init, NeedOrderId, OrderFound, Ineligible, Cancelled）
- **D-39:** 消息类型命名与 PRD 保持一致

### Mock 数据
- **D-40:** 3 个测试订单硬编码在 MockOrderService 中（A001 可退 / A002 超期 / A003 未签收）
- **D-41:** MockFinanceService 返回固定 RefundResult（RefundId: "RF-xxx"）
- **D-42:** MockMemberService 返回固定 CouponInfo（CouponId: "CPN-2024", Discount: 20.00）

## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### PRD & OpenSpec
- `Prd.md` — 完整架构设计文档，Section 二/四/五/六/七 是核心参考
- `openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md` — 18 条需求规格（304 行）
- `openspec/changes/refund-workflow-demo/design.md` — 20 节技术设计（391 行）
- `openspec/changes/refund-workflow-demo/proposal.md` — 变更提案
- `openspec/changes/refund-workflow-demo/tasks.md` — 任务清单

### Planning
- `.planning/REQUIREMENTS.md` — 32 条 v1 需求，31 条映射到 Phase 1
- `.planning/ROADMAP.md` — 4 阶段路线图

### MAF Reference (agent-framework)
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/HumanInTheLoop/HumanInTheLoopBasic/Program.cs` — RequestPort 事件循环模式
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/HumanInTheLoop/HumanInTheLoopBasic/WorkflowFactory.cs` — Workflow 构建 + Executor 定义
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/ConditionalEdges/02_SwitchCase/Program.cs` — AddSwitch 分支模式
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/ConditionalEdges/01_EdgeCondition/Program.cs` — AddEdge + ForwardMessage 模式
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/_StartHere/01_Streaming/Program.cs` — StreamingRun + WatchStreamAsync 模式
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/WorkflowBuilder.cs` — WorkflowBuilder API
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/WorkflowBuilderExtensions.cs` — ForwardMessage / AddSwitch / AddEdge 扩展
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/Executor.cs` — Executor 基类
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/RequestPort.cs` — RequestPort 定义
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/ExternalRequest.cs` — ExternalRequest / ExternalResponse
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/InProcessExecution.cs` — RunStreamingAsync / StreamingRun
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/IWorkflowContext.cs` — IWorkflowContext 接口
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/` — AgentClassSkill / AgentSkillFrontmatter / AgentSkillScript

### MAF Package Management
- `../../../GitCode/agent-framework/dotnet/Directory.Packages.props` — 中央包版本管理参考
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/Directory.Build.props` — 示例项目构建配置

## Existing Code Insights

### Reusable Assets
- 无现有代码（greenfield 项目）

### Established Patterns
- MAF samples 使用相对路径引用 source project（`../../../../src/...`）
- MAF samples 显式设置 `TargetFrameworks=net10.0`，`Nullable=enable`，`ImplicitUsings=enable`
- MAF samples 使用 `Console.ReadLine/WriteLine` 做控制台交互

### Integration Points
- MAF Workflows 通过 `InProcessExecution.RunStreamingAsync()` 启动
- 控制台主循环通过 `StreamingRun.WatchStreamAsync()` 监听事件
- RequestPort 通过 `RequestInfoEvent` 暂停，`ExternalResponse` 恢复

## Specific Ideas

- 退款流程严格按照 PRD Section 二 的 6 步执行
- 动态追问机制按照 PRD Section 5.1/5.2 定义：RequestPort → RequestInfoEvent → ExternalRequest → 控制台 → 用户回复 → ExternalResponse → 恢复
- Intent Router 使用 DashScope（通义千问）OpenAI 兼容接口
- 所有源码调用必须参照 D:\GitCode\agent-framework\dotnet 样本模式

## Deferred Ideas

- Knowledge Layer (FAQ/RAG) — 旁路系统，后续实现
- Human Agent Layer — 人工客服接管
- Exchange/Logistics 业务模块 — 后续扩展
- RedisSessionStore 真实实现 — 后续实现

---

*Phase: 1-Foundation*
*Context gathered: 2026-06-01*
