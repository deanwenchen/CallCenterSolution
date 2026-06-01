# Phase 6: Agent Pipeline + Compaction - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning
**Source:** discuss-phase analysis + MAF framework capability confirmation

<domain>
## Phase Boundary

实现 6 层 Agent Pipeline（SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput）和 Compaction 扩展方法。Phase 6 完成后，ConsoleDemo 的 `HandleRequestAsync` 调用应经过完整管道。

**不在本阶段**:
- 真实 PII 规则配置化（正则已存在，配置化留 v2）
- ToolApproval 具体审批规则（仅框架，规则留 v2）
- 审计日志持久化（Phase 7）

</domain>

<decisions>
## Implementation Decisions

### D-01: Pipeline 架构 — ChatClientBuilder 中间件链 (方案A)
- **Locked decision:** 使用 MAF 的 `ChatClientBuilder` 扩展方法构建 6 层管道，每层作为独立的 `DelegatingHandler` 或 `IChatClient` 中间件
- 通过 `.UseAIContextProviders()` 注册 `CompactionProvider`
- SafetyInput/SafetyOutput 作为独立的 `DelegatingChatClient` 包裹在 LLM 调用前后
- 参考 MAF 样本模式：`Agent_Step18_CompactionPipeline` 的 `ChatClientBuilder` 链式调用
- **Why:** MAF 内置中间件机制主要用于框架内部，而 ChatClientBuilder 是推荐的扩展点

### D-02: Compaction 使用 MAF CompactionProvider + PipelineCompactionStrategy
- **Locked decision:** MAF 框架已有完整的 `CompactionProvider` + 4 种策略（SlidingWindow, Summarization, ToolResult, Truncation）+ `PipelineCompactionStrategy` 组合
- Phase 6 配置：`PipelineCompactionStrategy` 组合 2 层策略：
  1. `SummarizationCompactionStrategy` — token 阈值 8000，使用小模型生成摘要
  2. `SlidingWindowCompactionStrategy` — 保留最近 8 轮对话
- 小模型摘要使用 `qwen-plus`（比主模型 `qwen3.6-plus` 更便宜更快）
- **Why:** MAF 能力已确认，无需自建。样本 `Agent_Step18_CompactionPipeline` 展示了完整用法
- **Trigger:** `CompactionTriggers.TokensExceed(8000)` — 与 PRD 要求一致

### D-03: ToolApproval 框架空壳，规则留 v2
- **Locked decision:** Phase 6 仅定义 `ToolApprovalAgent` 接口和默认"全部放行"行为
- 接口方法：`Task<bool> IsApprovedAsync(string toolName, object arguments, string sessionId)`
- 默认实现：始终返回 `true`（不拦截）
- 配置点：`AddCallCenterToolApproval(Action<ToolApprovalOptions>)` 注册接口，v2 接入具体规则
- **Why:** v1.1 demo 阶段不需要实际审批，但框架必须就位供 v2 使用

### D-04: Logging — 写入 JSON 日志文件（为 Phase 7 铺垫）
- **Locked decision:** 请求/响应日志写入 `.logs/{sessionId}.jsonl` 文件
- 每行一条 JSON 记录：`{timestamp, sessionId, direction: "request"|"response", tool, content, tokenCount}`
- Phase 7 Audit Logger 读取此日志文件并增强为不可篡改审计存储
- **Why:** 控制台输出无法被后续审计消费；JSONL 格式是 Phase 7 Audit Logger 的天然输入源

### D-05: SafetyInput — 组合已有 PII/Keyword/Injection 组件
- **Locked decision:** `SafetyInputFilter` 组合调用已有的 `PiiRedactor.Redact()`, `KeywordFilter.IsBlocked()`, `PromptInjectionDetector.Detect()`
- 执行顺序：PII 脱敏 → 关键词拦截 → 注入检测
- KeywordFilter 和 PromptInjectionDetector 从 TODO 空壳变为实际实现（基础规则硬编码）
- **Why:** 这些组件在 Phase 1 已创建但只有 PiiRedactor 是完整的

### D-06: SafetyOutput — 仅 PII 脱敏
- **Locked decision:** `SafetyOutputFilter` 仅调用 `PiiRedactor.Redact()` 过滤 LLM 输出
- 不实现敏感内容拦截（规则复杂，留 v2）
- **Why:** v1.1 最小可用 — PII 脱敏是合规硬性要求

### D-07: Pipeline 接入 EntryPoint
- **Locked decision:** `EntryPoint` 构造函数改为接受 `IChatClient` pipeline（而非裸 `IChatClient`）
- ConsoleDemo 的 `Program.cs` 负责组装 pipeline 并传入 EntryPoint
- `EntryPoint` 本身不感知管道 — 职责单一
- **Why:** EntryPoint 的职责是意图识别和路由，不应耦合管道组装逻辑

### Claude's Discretion
- 日志文件具体目录位置（.logs/ vs logs/ vs data/logs/）
- 小模型名称（qwen-plus vs qwen-turbo vs gpt-4o-mini）— 已选定 qwen-plus
- 管道中间件的具体实现模式（DelegatingHandler vs DelegatingChatClient vs IChatClient decorator）

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/ROADMAP.md` — Phase 6 goal, success criteria, requirement IDs (FW-05, FW-07)
- `.planning/REQUIREMENTS.md` — FW-05, FW-07 definitions

### MAF Framework (source of truth for pipeline/compaction patterns)
- `D:/GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Compaction/CompactionProvider.cs`
- `D:/GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Compaction/PipelineCompactionStrategy.cs`
- `D:/GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Compaction/SummarizationCompactionStrategy.cs`
- `D:/GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Compaction/SlidingWindowCompactionStrategy.cs`
- `D:/GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Compaction/CompactionTriggers.cs`
- `D:/GitCode/agent-framework/dotnet/samples/02-agents/Agents/Agent_Step18_CompactionPipeline/Program.cs`
- `D:/GitCode/agent-framework/dotnet/samples/02-agents/CompactionDemo/Program.cs`

### Existing Skeleton Files (to replace TODO stubs)
- `src/CallCenter.Framework/Safety/SafetyPipelineAgent.cs` — TODO stub → real implementation
- `src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs` — TODO stub → real implementation
- `src/CallCenter.Framework/Compaction/CompactionExtensions.cs` — TODO stub → wire MAF CompactionProvider
- `src/CallCenter.Framework/Safety/KeywordFilter.cs` — TODO stub → basic keyword rules
- `src/CallCenter.Framework/Safety/PromptInjectionDetector.cs` — TODO stub → basic pattern detection
- `src/CallCenter.Framework/Safety/PiiRedactor.cs` — already complete, no changes needed

### Current EntryPoint (pipeline integration target)
- `src/CallCenter.AgentHost/EntryPoint.cs` — constructor accepts IChatClient, needs pipeline wiring
- `src/CallCenter.ConsoleDemo/Program.cs` — pipeline assembly point

</canonical_refs>

<specifics>
## Specific Ideas

- 6 层管道顺序：SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput
- Compaction 配置：8000 token 阈值，保留 8 轮，小模型 qwen-plus 摘要
- MAF 的 `ChatClientBuilder.UseAIContextProviders(new CompactionProvider(pipeline))` 是正确接入点
- `PipelineCompactionStrategy` 接受多个策略按顺序执行（从温和到激进）
- 日志使用 JSONL 格式，每行一条记录，便于 Phase 7 审计消费
- KeywordFilter 基础规则：暴力/色情/政治敏感词硬编码数组
- PromptInjectionDetector 基础规则：检测 "忽略", "ignore previous", "system prompt" 等关键词

</specifics>

<deferred>
## Deferred Ideas

- ToolApproval 具体审批规则 → v2（按角色/权限级别）
- SafetyOutput 敏感内容拦截 → v2
- KeywordFilter 配置化（从文件读取规则）→ v2
- 日志接入 OpenTelemetry → v2（OB-01）
- Compaction 使用更激进的 ToolResultCompactionStrategy → 后续按需

</deferred>

---

*Phase: 06-agent-pipeline-compaction*
*Context gathered: 2026-06-01 via discuss-phase*
