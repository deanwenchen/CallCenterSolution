# Phase 14: SSE 流式 + 会话管理 - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

将 `/chat` 阻塞式端点升级为 SSE 流式输出，新增 `/chat/stream` 端点，支持会话生命周期管理（sessionId 生成/恢复、60 分钟超时惰性清理）。

涉及 Requirements: WA-03, WA-04

**Success Criteria（来自 ROADMAP.md）:**
1. POST /chat/stream 返回 SSE 事件流，用户可实时看到工作流中间输出
2. 自动 sessionId 生成，后续请求复用同一会话
3. 过期会话（60 分钟无活动）自动清理
4. 前端可用 EventSource 或 fetch + ReadableStream 消费

</domain>

<spec_lock>
No SPEC.md for this phase — requirements from ROADMAP.md above.
</spec_lock>

<decisions>
## Implementation Decisions

### 端点策略
- **D-14-01:** 保留现有 `/chat`（阻塞式 JSON 响应），新增 `/chat/stream`（SSE 事件流）。两者并存，互不替换。

### SSE 事件格式
- **D-14-02:** SSE 推送原始 JSON — 每个 WorkflowEvent 直接序列化为 `data: {"type":"...", "data":{...}}\n\n` 格式。不额外包装信封。
- **D-14-03:** 全量推送 9 种 WorkflowEvent（WorkflowStarted, ExecutorInvoked, ExecutorCompleted, RequestInfo, WorkflowOutput, WorkflowError, ExecutorFailed, SuperStepCompleted, WorkflowWarning）。前端自行过滤不关心的事件。

### 服务层适配
- **D-14-04:** 新建 `ProcessStreamingAsync(string sessionId, string userMessage, CancellationToken ct)` 方法，返回 `IAsyncEnumerable<string>`。不修改现有 `ProcessAsync` 签名。
- **D-14-05:** `ProcessStreamingAsync` 内部复用现有 `ResolveWorkflow` 路由逻辑 + `DriveLoopAsync` 事件循环，将每个 WorkflowEvent 序列化为 SSE 格式字符串 yield return。
- **D-14-06:** 方法签名放在 `CallCenterService` 的 partial class 中（新建文件如 `CallCenterService.Streaming.cs`）。

### 会话超时清理
- **D-14-07:** 使用惰性清理策略 — 不设后台定时器。在 `/chat/stream` 端点入口检查 `lastActivity`，超过 60 分钟无活动则调用 `InMemorySessionStore.ClearScopeAsync(sessionId)` 并返回 SSE 错误事件。
- **D-14-08:** 超时通知通过 SSE 推送特殊事件: `data: {"type":"SessionExpired","data":{"reason":"60 minutes of inactivity"}}\n\n`

### Claude's Discretion
- CORS 配置沿用 Phase 13 的 `AllowAll` 策略
- SSE 端点使用 `IHostEnvironment.IsDevelopment()` 判断是否启用，开发阶段无认证
- `IAsyncEnumerable<string>` 在 ASP.NET Core 中通过 `Results.Ok()` 或自定义 `IResult` 输出，planner 需确认最佳写法
- `sessionId` 生成逻辑与现有 `/chat` 一致：客户端不传则 `Guid.NewGuid().ToString()`

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Requirements
- `.planning/ROADMAP.md` — Phase 14 goal, success criteria
- `.planning/REQUIREMENTS.md` — WA-03, WA-04 requirement definitions
- `.planning/PROJECT.md` — Tech stack (.NET 10.0, MAF SDK), key decisions, constraints

### Codebase Architecture
- `src/CallCenter.WebApi/Program.cs` — 现有 `/chat` 端点实现，SSE 端点在此新增
- `src/CallCenter.WebApi/ChatRequest.cs` — 请求模型（message + sessionId），SSE 端点复用
- `src/CallCenter.AgentHost/CallCenterService.Core.cs` — 现有字段声明（_sessionStore, _refundWorkflow 等）
- `src/CallCenter.AgentHost/CallCenterService.Execution.cs` — DriveLoopAsync 事件循环，ProcessStreamingAsync 需复用
- `src/CallCenter.AgentHost/CallCenterService.Intent.cs` — ProcessAsync 入口签名参考
- `src/CallCenter.AgentHost/CallCenterService.Routing.cs` — ResolveWorkflow 路由 + 超时检测逻辑
- `src/CallCenter.Framework/Session/InMemorySessionStore.cs` — 会话存储实现（GetAsync/SetAsync/ClearScopeAsync）

### MAF SDK
- `D:\GitCode\agent-framework\dotnet\src\` — Microsoft Agent Framework 源码引用
- `D:\GitCode\agent-framework\dotnet\src\Workflows\` — Workflow 事件类型定义参考

No external ADRs or SPECs — requirements captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **DriveLoopAsync**: 现有事件循环（Execution.cs），遍历 WatchStreamAsync() 的每个 WorkflowEvent → ProcessStreamingAsync 只需将 Console.WriteLine 替换为 yield return SSE 字符串
- **ResolveWorkflow**: 路由逻辑已处理 sessionId → lastActivity 更新 + 超时检查，ProcessStreamingAsync 可直接复用
- **InMemorySessionStore**: 已有 GetAsync/SetAsync/RemoveAsync/ClearScopeAsync，60 分钟超时检测在 Routing.cs 的 CheckTimeoutAsync 中已实现
- **CheckpointManager**: 断点管理已就绪，SSE 流式不影响断点机制

### Established Patterns
- **partial class 拆分**: CallCenterService 按职责拆分为 Core/Execution/Intent/Interaction/Routing 文件，Streaming.cs 遵循同一模式
- **环境变量配置**: API Key 通过 `DASHSCOPE_API_KEY` 环境变量注入
- **DI 注册**: WebApi 通过 `services.AddScoped<CallCenterService>()` 注入
- **.NET 10.0 target**: 所有项目统一 `net10.0`，nullable enable，implicit usings

### Integration Points
- SSE 端点需在 `Program.cs` 的 `app.MapPost("/chat/stream", ...)` 中注册
- `IAsyncEnumerable<string>` 在 ASP.NET Core 10 中可直接 return，框架自动处理流式输出
- 不需要修改现有 `/chat` 端点、`ProcessAsync` 或任何 Execution/Intent 逻辑

</code_context>

<specifics>
## Specific Ideas

用户选择：
- 流式推送 WorkflowEvent 原始数据，不做格式包装
- 新建独立的 ProcessStreamingAsync 方法，保持 ProcessAsync 不变
- 会话超时采用惰性清理（请求时检查），不加后台定时器
- 前端用 fetch + ReadableStream 消费（比 EventSource 更灵活）
- SSE 端点独立路由 /chat/stream，保留原有 /chat

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 14-SSE 流式 + 会话管理*
*Context gathered: 2026-06-04*
