# Phase 16: SafetyOutput + Exchange 骨架确认 - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

实现 SafetyOutput 层内容审核（SO-01：暴力/色情/政治敏感关键词拦截），复用与输入端一致的关键词匹配策略。确认 Exchange 换货工作流骨架结构符合预期（7 个 Executors 已编译通过，不做业务实现）。违规话术按类别区分，复用 SafetyOptions 配置。

</domain>

<decisions>
## Implementation Decisions

### 内容审核策略
- **D-01:** 使用关键词匹配（正则 + 字符串匹配），与 SafetyInput 端保持一致策略。不使用 LLM 语义判断，v3.0 不需要这么重
- **D-02:** 覆盖三类：暴力/恐怖、色情/低俗、政治敏感
- **D-03:** 审核发生在 LLM 响应到达用户之前（SafetyOutputFilter.ProcessOutput 扩展）
- **D-04:** 触发拦截后按类别返回不同话术（暴力类、色情类、政治类各有独立模板）

### 配置化方案
- **D-05:** 复用已有的 SafetyOptions 类，新增输出端配置属性：
  - `BlockedOutputCategories` — 启用的审核类别（暴力、色情、政治）
  - `ViolenceMessageTemplate` — 暴力类拦截话术
  - `PornographyMessageTemplate` — 色情类拦截话术
  - `PoliticsMessageTemplate` — 政治类拦截话术
- **D-06:** 各类别关键词列表同样从 appsettings.json 的 Safety 配置节读取，不硬编码

### 异常处理
- **D-07:** 输出端拦截同样抛出 SafetyViolationException，ViolationType 为 `output_content_blocked`
- **D-08:** Pipeline 层捕获异常后返回对应话术给用户，不记录到审计日志（与输入端保持一致）

### Exchange 骨架确认
- **D-09:** Exchange 工作流骨架已就绪（ExchangeWorkflow + 7 Executors + Messages），编译通过即视为确认
- **D-10:** Phase 16 不实现 Exchange 业务逻辑，属于 v4 范围

### Claude's Discretion
- 具体关键词列表由 planner 根据国内客服场景合理补充
- 话术模板默认值由 planner 根据现有 BlockedMessageTemplate 风格确定
- SafetyOutputFilter 扩展的具体代码结构（新增类还是修改现有类）由 planner 根据代码整洁度决定

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 需求与路线图
- `.planning/ROADMAP.md` — Phase 16 goal, SO-01 需求
- `.planning/REQUIREMENTS.md` — SO-01: Pipeline SafetyOutput 层 — 敏感内容拦截

### 现有代码（必须阅读）
- `src/CallCenter.Framework/Safety/SafetyInputFilter.cs` — 输入端安全过滤器，参考其拦截模式
- `src/CallCenter.Framework/Safety/SafetyOptions.cs` — 配置类，需扩展输出端属性
- `src/CallCenter.Framework/Safety/PiiRedactor.cs` — PII 脱敏，SafetyOutputFilter 当前唯一功能
- `src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs` — 6 层管道组装，SafetyOutputDelegatingClient 所在处
- `src/CallCenter.WebApi/appsettings.json` — Safety 配置节，需扩展输出端关键词

### Exchange 骨架（确认用）
- `src/CallCenter.Workflows/Exchange/ExchangeWorkflow.cs` — 换货工作流定义
- `src/CallCenter.Workflows/Exchange/ExchangeMessages.cs` — 换货消息类型
- `src/CallCenter.Workflows/Exchange/Executors/` — 7 个 Executor 目录

### 参考模式
- `src/CallCenter.Framework/Safety/KeywordFilter.cs` — 输入端关键词过滤器的实例 + 静态双 API 模式，输出端可参照

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SafetyInputFilter` — 输入端已实现 PII 脱敏 → 关键词拦截 → 注入检测三阶段模式，输出端可参照：PII 脱敏 → 内容审核
- `SafetyOptions` — 已有 EnableKeywordFilter、EnableInjectionDetection、BlockedKeywords 等属性，扩展输出端配置保持一致的风格
- `KeywordFilter` — 实例 + 静态双 API 模式已被验证有效，输出端可复用同一类或新建 OutputContentFilter
- `SafetyViolationException` — 携带 ViolationType，输入端输出端共用同一异常类型
- `SafetyOutputDelegatingClient` — 已在 StandardPipelineFactory.cs 中创建，目前只调用 SafetyOutputFilter.ProcessOutput()（仅 PII 脱敏）

### Established Patterns
- SafetyInput 管道顺序：PII 脱敏 → 关键词拦截 → 注入检测，任一阶段抛出 SafetyViolationException 短路
- 配置通过 appsettings.json 的 "Safety" 节读取，DI 注册为单例
- Executor 空壳模式：Exchange 的 7 个 Executor 已验证骨架可用，仅待业务逻辑填充

### Integration Points
- `SafetyOutputFilter.ProcessOutput()` 需要扩展，在 PII 脱敏之后增加内容审核
- `SafetyOutputDelegatingClient` 需要捕获 SafetyViolationException 并返回话术
- appsettings.json 的 "Safety" 节需要新增输出端配置
- Exchange 骨架确认仅需 `dotnet build` 验证编译通过

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

- Exchange 工作流完整业务实现（Executor 填充真实逻辑）— v4 范围
- LLM 语义级内容审核 — 未来需要时可升级，当前关键词方案足够

None — discussion stayed within phase scope

</deferred>

---

*Phase: 16-SafetyOutput + Exchange 骨架确认*
*Context gathered: 2026-06-04*
