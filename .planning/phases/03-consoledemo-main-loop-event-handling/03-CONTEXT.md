# Phase 3: ConsoleDemo — Main Loop + Event Handling - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning

<domain>
## Phase Boundary

实现控制台主循环，集成 EntryPoint、RefundWorkflow、EventBus、Mock Services 所有组件，端到端跑通退款流程。包含断点恢复、意图切换、超时检测、错误恢复等交互细节。

</domain>

<decisions>
## Implementation Decisions

### 断点恢复机制
- **D-01:** 自动恢复 — 检测到 activeWorkflow 后，直接加载上次 checkpoint，把用户新输入作为对 RequestPort 的响应，不需要额外确认
- **D-02:** 不需要用户确认"是否继续"，直接推进流程

### 意图切换处理
- **D-03:** 直接终止 — 检测到新意图与当前 activeWorkflow 不同，清除 activeWorkflow，打印"已终止旧流程"，启动新流程
- **D-04:** Phase 3 只做退款，换货是 v2 需求，所以不需要挂起保留机制

### 超时与会话管理
- **D-05:** 被动检查 — 每次用户输入时，检查 lastActivity 时间戳，超过 30 分钟提示并清除 activeWorkflow
- **D-06:** 不引入后台计时器线程，避免控制台输出竞争

### 错误恢复交互
- **D-07:** 工作流失败后清除 activeWorkflow，打印简单错误信息（执行器名 + 原因），返回主循环
- **D-08:** 不需要额外错误上下文（如失败前的状态、重试选项等）
- **D-09:** 程序不退出，用户可以继续输入新意图

### Claude's Discretion
- 具体 checkpoint 数据结构由 planner 根据 MAF SDK 的 Workflow checkpoint API 决定
- 时间戳存储位置（InMemorySessionStore vs 独立字段）由 planner 决定

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — CD-01, CD-02, CD-03, CD-04, IR-03, IR-04, IR-05 requirements
- `.planning/ROADMAP.md` — Phase 3 goal and success criteria

### Existing Code
- `src/CallCenter.ConsoleDemo/Program.cs` — 当前主循环实现（需补充的部分）
- `src/CallCenter.AgentHost/EntryPoint.cs` — 意图识别和 ProcessAsync 路由
- `src/CallCenter.Workflows/Refund/RefundWorkflow.cs` — 退款工作流图构建
- `src/CallCenter.Framework/EventBus/InMemoryBusinessEventBus.cs` — 事件发布/订阅
- `src/CallCenter.Framework/Session/InMemorySessionStore.cs` — 会话存储
- `src/CallCenter.Framework/Parsing/StructuredOutputParser.cs` — JSON 解析
- `src/CallCenter.Shared/Mcp/IOrderMcpClient.cs` — 订单 MCP 接口
- `src/CallCenter.Shared/Mcp/IFinanceMcpClient.cs` — 财务 MCP 接口
- `src/CallCenter.Shared/Mcp/IMemberMcpClient.cs` — 会员 MCP 接口
- `src/CallCenter.Shared/Services/MockOrderService.cs` — 模拟订单服务
- `src/CallCenter.Shared/Services/MockFinanceService.cs` — 模拟退款服务
- `src/CallCenter.Shared/Services/MockMemberService.cs` — 模拟会员服务

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Program.cs` main loop — 已有 while(true) 读输入 → ProcessAsync → pattern match 框架，需补充 Resume 分支
- `EntryPoint.ProcessAsync()` — 完整的意图识别 + activeWorkflow 检查逻辑，可直接使用
- `InProcessExecution.RunStreamingAsync()` — MAF SDK 工作流流式执行，配合 WatchStreamAsync() 处理事件
- `HandleRequest()` — 已有的 RequestInfoEvent 交互处理，支持 NeedOrderId 和 ConfirmRefundRequest
- `InMemoryBusinessEventBus` — 已订阅 RefundCompletedEvent，可扩展 RiskAlertEvent
- `InMemorySessionStore` — 支持 GetAsync/SetAsync/RemoveAsync，可用于 lastActivity 时间戳

### Established Patterns
- `ProcessResult` 模式：ResumeExistingResult / StartWorkflowResult / NoIntentResult 三态路由
- `RequestPort` 模式：RefundInfoPort（参数收集）+ RefundConfirmPort（确认）两种交互类型
- `WorkflowEvent` 模式：RequestInfoEvent / WorkflowOutputEvent / WorkflowErrorEvent / ExecutorFailedEvent
- Mock 服务注入：所有 MCP Client 接口对应 Mock 实现

### Integration Points
- `EntryPoint` 需要 `IChatClient`（DashScope OpenAI 兼容接口）
- `RefundWorkflow.Build()` 需要 4 个依赖：IOrderMcpClient, IFinanceMcpClient, IMemberMcpClient, IBusinessEventBus
- `Program.cs` 依赖 DASHSCOPE_API_KEY 环境变量

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 3-ConsoleDemo — Main Loop + Event Handling*
*Context gathered: 2026-06-01*
