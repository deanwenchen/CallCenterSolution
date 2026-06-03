## Context

CallCenter 项目已完成退款工作流 Demo 的搭建（refund-workflow-demo 变更），包含 5 个项目：Shared、Framework、Workflows、AgentHost、ConsoleDemo。退款流程 6 步（GetOrder → CheckRefundRule → WaitConfirm → ExecuteRefund → RestoreCoupon → SendNotification）能正常运行。

当前所有基础设施组装、工作流执行、事件处理、用户交互都写死在 `ConsoleDemo/Program.cs`（439 行）中。`AgentHost/EntryPoint.cs`（192 行）负责意图识别和路由，但 AIAgent 在构造函数中硬编码创建。

未来需要支持 Web API 调用，当前的控制台内联代码无法复用。

## Goals / Non-Goals

**Goals:**
- Program.cs 精简到 ~20 行消息循环
- 抽出 `CallCenterService` 作为统一服务入口，控制台和 Web API 共用
- 所有基础设施（IChatClient、SessionStore、AuditLogger、EventBus、McpClient）通过 DI 注册
- AIAgent 不直接 DI，通过 AIAgentFactory 按场景动态创建
- 环境变量统一在 CallCenterOptions.ApplyDefaults() 一处处理

**Non-Goals:**
- **不改变任何业务流程逻辑** — 退款 6 步流程、事件处理、Saga 补偿、断点恢复完全保持不变
- **不改变 workflows/ 目录下的任何代码** — 保持当前清晰易懂的代码结构
- 不实现 Session 持久化（仍用 InMemorySessionStore）
- 不实现真实 MCP 调用（仍用 Mock 服务）
- 不新增业务工作流（仅做框架重构）

## Decisions

### 1. CallCenterService 使用 partial class 拆分

**Decision:** 按职责拆成 5 个文件：Core（构造+字段）、Intent（ProcessAsync 入口）、Routing（工作流映射）、Execution（事件循环）、Interaction（用户交互）

**Rationale:** 每个文件职责清晰，新增业务时只需在对应文件加代码，不需要理解整个 400+ 行文件。

**Alternatives considered:** 单一文件 — 可读性差；每个职责独立类 — 增加类间依赖复杂度。

### 2. AIAgent 不直接 DI，使用工厂模式

**Decision:** AIAgentFactory 接收 IChatClient（pipeline-wrapped），提供 CreateIntentAgent() 和 CreateDialogAgent() 方法。AIAgentFactory 本身 DI 为 Singleton。

**Rationale:** 意图识别和工作流对话需要不同的 System Prompt 和 Tools 配置。DI 成 Singleton 会锁死配置。工厂模式保持了灵活性，同时 IChatClient 底层是 DI 的。

### 3. DI 容器使用 Microsoft.Extensions.DependencyInjection

**Decision:** 使用 Microsoft.Extensions.DependencyInjection 的 ServiceCollection，而非第三方 DI 框架。

**Rationale:** 项目已引用此包，保持一致性。

### 4. CallCenterService 无参构造函数自建 DI 容器

**Decision:** 无参构造函数内部自建 ServiceCollection → AddCallCenter() → BuildServiceProvider → Resolve。

**Rationale:** 满足"开箱即用"的控制台场景。Web 场景可传 options 或用 DI 容器注入。

### 5. ProcessAsync 返回 string，阻塞到终态

**Decision:** ProcessAsync(sessionId, userMessage) → string，内部驱动完整工作流循环直到终态。

**Rationale:** 控制台和 Web API 都只需要最终结果。中间的用户交互（问订单号、确认退款）在服务内部完成。

### 6. RunWorkflow/ResumeWorkflow 去重

**Decision:** 抽取共享的 DriveLoopAsync() 事件循环，两个函数只负责初始化差异（RunStreamingAsync vs ResumeStreamingAsync）。

**Rationale:** 当前两个函数 ~50% 代码重复，都是遍历同样的 5 种事件类型。

### 7. 用户交互层使用 Console.In 直接读取

**Decision:** Interaction.cs 直接从 Console.In.ReadLineAsync() 读取用户输入。

**Rationale:** 当前只有控制台场景，保持简单。未来 Web API 的交互机制完全不同（HTTP 请求/响应），需要单独处理，不适合现在就抽象。

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| ProcessAsync 阻塞调用，长工作流占用线程 | 工作流本身是 async 的，不会阻塞线程池线程 |
| Console.In 耦合到服务层 | 仅控制台场景使用，Web API 需要独立的交互机制 |
| pipeline 的 sessionId 写死为 "pipeline" | 日志仍正常工作，未来可改为 session-aware |
| 大重构可能引入回归 | 逐文件创建，每步编译验证，保持业务流程不变 |
| 两个 IChatClient 注册冲突（原始 vs pipeline） | 使用 AddKeyedSingleton("base") 注册原始客户端 |
