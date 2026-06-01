# Phase 02: AgentHost + Intent Router - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning

## Phase Boundary

实现 EntryPoint（LLM 意图识别 + 路由决策）和 RefundSkill（AgentClassSkill 完整注册）。Phase 2 专注于**意图识别和 Workflow 启动**，Resume 机制推迟到 Phase 3。

**输入：** Phase 1 完成的 5 个项目代码 + 42 条决策
**输出：** EntryPoint.RecognizeIntentAsync() 可用，RefundSkill 注册到 AgentSkillsProvider

## Implementation Decisions

### LLM 提供商
- **D-43:** Intent Router 使用 **DashScope Qwen**（通义千问）通过 OpenAI 兼容端点
- **D-44:** API Key 从环境变量 `DASHSCOPE_API_KEY` 读取
- **D-45:** 模型从环境变量 `DASHSCOPE_MODEL_NAME` 读取（默认 "qwen3.6-plus"）
- **D-46:** Endpoint: `https://dashscope.aliyuncs.com/compatible-mode/v1`
- **D-47:** 使用 `OpenAI` NuGet 包创建 `OpenAIClient` + `GetChatClient()` + `AsIChatClient()`

### IntentResult 输出结构
- **D-48:** IntentResult 为完整对象：`record IntentResult(string Intent, string? Workflow, string? OrderId)`
- **D-49:** LLM system prompt 要求返回 JSON: `{"intent": "refund"|"greeting"|"unknown", "workflow": "RefundWorkflow", "orderId": "<如果提到>"}`
- **D-50:** 使用 StructuredOutputParser<IntentResult> 解析 LLM 响应
- **D-51:** EntryPoint 根据 IntentResult.Workflow 或 Intent 映射到具体 Workflow 类型
- **D-52:** intent="refund" → new RefundIntent(orderId, userId) 作为 Workflow 初始消息
- **D-53:** intent="greeting"/"unknown" → 不走 Workflow，返回闲聊回复

### Resume 机制推迟
- **D-54:** Phase 2 只实现 **New Start** 路径（InProcessExecution.RunAsync(workflow, initialMessage)）
- **D-55:** Resume 路径（从 checkpoint 恢复）推迟到 Phase 3 ConsoleDemo 实现
- **D-56:** EntryPoint 的 activeWorkflow 检查在 Phase 2 只做"有 → 提示已在工作流中"，不做完整 Resume
- **D-57:** InMemorySessionStore 用于存储 activeWorkflow 标记（字符串），不做完整 Checkpoint 存储

### Skill 完整注册
- **D-58:** RefundSkill 按 PRD Section 4.1 完整实现 AgentClassSkill<RefundSkill>
- **D-59:** Frontmatter: name="refund", description=PRD 定义的完整描述
- **D-60:** Instructions: 告诉 LLM 退款流程的 3 个步骤
- **D-61:** Scripts 完整实现：
  - `get_recent_orders(userId)` → 调用 IOrderMcpClient.GetRecentOrdersAsync
  - `execute_refund(orderId, amount)` → 调用 IFinanceMcpClient.RefundAsync
- **D-62:** Scripts 通过 IServiceProvider 获取 DI 注入的 MCP Client
- **D-63:** RefundSkill 通过 AgentSkillsProviderBuilder.UseSkill(new RefundSkill()) 注册
- **D-64:** Intent Agent 使用 chatClient.AsAIAgent() + AIContextProviders=[skillsProvider]

### EntryPoint 设计
- **D-65:** EntryPoint 类构造函数：EntryPoint(IChatClient chatClient, InMemorySessionStore sessionStore)
- **D-66:** RecognizeIntentAsync(string userMessage, ct) → Task<IntentResult?> 调用 LLM 识别意图
- **D-67:** GetActiveWorkflow(sessionId) / SetActiveWorkflow(sessionId, workflowName) / ClearActiveWorkflow(sessionId) 使用 InMemorySessionStore
- **D-68:** ProcessAsync(sessionId, userMessage, ct) → 主入口方法，整合意图识别 + Workflow 启动/Resume 决策

### Claude's Discretion
- EntryPoint 的方法命名和内部结构由实现者决定
- RefundSkill 的 Scripts 中 IServiceProvider 的获取方式（通过参数传入或通过 DI 容器解析）
- 意图识别失败时的重试策略（最多重试几次，超时时间）

## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 1 Decisions
- `.planning/phases/01-foundation/01-CONTEXT.md` — Phase 1 全部 42 条决策（项目结构、MAF 引用、服务注入、Executor 输出类型、条件边路由、缺参数循环、Workflow 构建、Framework 组件）

### PRD & OpenSpec
- `Prd.md` — Section 5.1（用户请求处理流程），Section 5.2（RequestPort），Section 4.1（Skill 定义）
- `openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md` — Entry Point and Intent Routing requirement（行 75-112），AgentClassSkill Definition requirement（行 131-144）
- `openspec/changes/refund-workflow-demo/design.md` — Section 6（意图识别），Section 11（RefundSkill 完整设计）

### MAF Reference
- `../../../GitCode/agent-framework/dotnet/samples/03-workflows/ConditionalEdges/02_SwitchCase/Program.cs` — OpenAI client with DashScope endpoint, ChatClientAgent pattern
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Skills/Programmatic/AgentClassSkill.cs` — AgentClassSkill<TSelf> base class
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Skills/AgentSkillFrontmatter.cs` — Frontmatter constructor
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Skills/Programmatic/AgentSkillScriptAttribute.cs` — [AgentSkillScript] attribute
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/InProcessExecution.cs` — RunStreamingAsync, RunAsync
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/RequestPort.cs` — RequestPort.Create

### Phase 1 Code (after execution)
- `src/CallCenter.Shared/Mcp/IOrderMcpClient.cs` — Order MCP interface
- `src/CallCenter.Shared/Mcp/IFinanceMcpClient.cs` — Finance MCP interface
- `src/CallCenter.Shared/Mcp/IMemberMcpClient.cs` — Member MCP interface
- `src/CallCenter.Shared/Services/MockOrderService.cs` — Mock implementation
- `src/CallCenter.Shared/Services/MockFinanceService.cs` — Mock implementation
- `src/CallCenter.Shared/Services/MockMemberService.cs` — Mock implementation
- `src/CallCenter.Framework/Session/InMemorySessionStore.cs` — Session store
- `src/CallCenter.Framework/Parsing/StructuredOutputParser.cs` — JSON parser
- `src/CallCenter.Framework/EventBus/InMemoryBusinessEventBus.cs` — Event bus
- `src/CallCenter.Workflows/Refund/RefundWorkflow.cs` — Workflow builder
- `src/CallCenter.Workflows/Refund/RefundMessages.cs` — Message types

## Existing Code Insights

### Reusable Assets (after Phase 1)
- **InMemorySessionStore**: 用于存储 activeWorkflow 标记
- **StructuredOutputParser<IntentResult>**: 用于解析 LLM JSON 响应
- **RefundWorkflow.Build()**: 创建退款 Workflow 实例
- **Mock Services**: IOrderMcpClient/IFinanceMcpClient/IMemberMcpClient 的 Mock 实现

### Established Patterns
- 构造函数注入服务接口（D-07）
- ServiceCollection 做 DI 容器（D-08）
- MAF samples 使用 OpenAI SDK + DashScope endpoint 创建 IChatClient

### Integration Points
- EntryPoint 接收 IChatClient 用于 LLM 调用
- RefundSkill 通过 IServiceProvider 获取 MCP Client
- EntryPoint 通过 InMemorySessionStore 管理 activeWorkflow 状态
- RefundWorkflow.Build() 接受 4 个服务接口参数

## Specific Ideas

- Intent Router 的 system prompt 应该简洁明确，只要求 JSON 输出
- RefundSkill 的 Scripts 虽然 Executor 已经直接调用 MCP Client，但保留 Script 定义是为了 LLM 可以直接调用（不走 Workflow 时的快速路径）
- Phase 2 完成后，Phase 3 ConsoleDemo 可以直接使用 EntryPoint.ProcessAsync() 方法

## Deferred Ideas

- Resume Workflow 完整实现（checkpoint 加载、恢复执行）— Phase 3
- 多意图处理（用户同时有多个意图）— 后续扩展
- 对话 Agent（自由对话模式）— 后续扩展
- ExchangeSkill / LogisticsSkill — 后续扩展

---

*Phase: 02-agenthost-intent-router*
*Context gathered: 2026-06-01*
