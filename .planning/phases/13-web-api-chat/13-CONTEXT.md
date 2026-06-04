# Phase 13: Web API 基础 — 新项目搭建 + /chat 端点 - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

新增 CallCenter.WebApi 项目（ASP.NET Core Minimal API），实现基础 HTTP 聊天入口 POST /chat。Phase 13 交付：项目搭建、阻塞式响应、CORS 配置、Swagger UI。ConsoleDemo 和 WebApi 可并行运行，互不影响。

涉及 Requirements: WA-01, WA-02, WA-05

</domain>

<decisions>
## Implementation Decisions

### 配置文件策略
- **D-01:** DashScope API Key 和其他 LLM 配置通过**环境变量**传入（如 `DASHSCOPE_API_KEY`），与现有 AgentHost 保持一致。appsettings.json 只放 Web 层专属配置（CORS、Swagger 开关等）。
- **D-02:** 不新建共享配置 Include 机制 —— WebApi 独立读取环境变量，避免两处维护。

### 响应模式
- **D-03:** Phase 13 做**纯阻塞式响应**，不预留 SSE 端点签名。POST /chat → 调用 CallCenterService.ProcessAsync → 返回 JSON `{ response: "..." }`。Phase 14 新增 /chat/stream SSE 端点。
- **D-04:** 当前不修改 ProcessAsync 签名 —— 它返回 string，WebApi 直接包装成 JSON。

### Claude's Discretion
- Swagger 默认启用，开发阶段无认证
- CORS 默认允许所有来源（`AllowAnyOrigin`），符合 REQUIREMENTS.md WA-05
- 项目命名遵循现有模式：`CallCenter.WebApi`
- 端口号由 planner 决定（建议 5000+ 避免与 ConsoleDemo 冲突）

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Requirements
- `.planning/ROADMAP.md` — Phase 13 goal, success criteria
- `.planning/REQUIREMENTS.md` — WA-01, WA-02, WA-05 requirement definitions
- `.planning/PROJECT.md` — Tech stack (.NET 10.0, MAF SDK), key decisions, constraints

### Codebase Architecture
- `src/CallCenter.Shared/` — ICallCenterService, SessionInfo, shared models
- `src/CallCenter.AgentHost/CallCenterService.Core.cs` — ProcessAsync 入口签名
- `src/CallCenter.AgentHost/Extensions.cs` — DI 注册模式
- `src/CallCenter.ConsoleDemo/Program.cs` — 现有入口参考（svc.ProcessAsync 调用方式）
- `src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj` — 项目引用参考

### MAF SDK
- `D:\GitCode\agent-framework\dotnet\src\` — Microsoft Agent Framework 源码引用

No external ADRs or SPECs — requirements captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **CallCenterService.ProcessAsync**: 已有的核心处理入口，WebApi 直接 DI 注入调用
- **DI 扩展方法 (Extensions.cs)**: AgentHost 已注册所有服务的 DI 模式，WebApi 复用同一组注册
- **AIAgentFactory**: 工厂模式创建 Agent，WebApi 无需直接调用，通过 ProcessAsync 间接使用
- **InMemorySessionStore**: 已有会话存储实现

### Established Patterns
- **partial class 拆分**: CallCenterService 按职责拆分为 Core/Execution/Intent/Interaction/Routing 文件
- **环境变量配置**: ConsoleDemo 通过 Environment Variables + JSON 配置加载 API Key
- **源码引用 MAF**: 所有项目通过相对路径引用 `..\..\..\..\GitCode\agent-framework\dotnet\src\`
- **.NET 10.0 target**: 所有项目统一 `net10.0`，nullable enable，implicit usings

### Integration Points
- 新项目需添加到 `CallCenterSolution.slnx`
- WebApi 需引用 CallCenter.AgentHost（包含 CallCenterService）
- WebApi 需引用 MAF Workflows 项目（同 ConsoleDemo 模式）
- 环境变量 `DASHSCOPE_API_KEY` 和 `DASHSCOPE_ENDPOINT` 需被读取

</code_context>

<specifics>
## Specific Ideas

用户选择保持最小改动：
- 配置走环境变量（与 AgentHost 一致）
- 响应走简单阻塞 JSON 返回（不预留 SSE）
- 目标是快速让 Web API 跑起来，Phase 14 再做流式增强

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 13-Web API 基础 — 新项目搭建 + /chat 端点*
*Context gathered: 2026-06-04*
