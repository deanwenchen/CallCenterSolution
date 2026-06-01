# Phase 7: Audit Logger + Saga Compensation - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning
**Source:** discuss-phase analysis

<domain>
## Phase Boundary

实现 Workflow 步骤审计日志（AuditLogger）和失败补偿机制（Saga + 重试）。Phase 6 的 JsonlLogger 是操作日志层，Phase 7 的 AuditLogger 是审计层 — 两者独立。

**不在本阶段**:
- 真实 MCP Server 调用失败模拟（MockExecutor 模拟超时即可）
- 补偿规则配置化（硬编码退款补偿即可）
- OpenTelemetry 集成（v2 OB-01）

</domain>

<decisions>
## Implementation Decisions

### D-01: AuditLogger — 独立 .audit/ 存储
- **Locked decision:** AuditLogger 写入 `.audit/{sessionId}.jsonl`，与 Phase 6 的 `.logs/` 完全分离
- 每条记录包含 SHA256 校验和字段（`previousHash` 链式链接，防止篡改）
- AuditLogger 消费 Phase 6 的 JsonlLogger 数据流（订阅方式）或独立从 Workflow 层面捕获
- 写入目录：`.audit/`（不是 `.logs/`）
- **Why:** 审计日志需要不可篡改特性，操作日志不需要。混合会增加复杂度

### D-02: AuditTrailMiddleware — 从 AuditLogger.cs + AuditTrailMiddleware.cs TODO stub 实现
- **Locked decision:** 替换两个 TODO stub 为实际实现
- AuditTrailMiddleware 不依赖 MAF 中间件机制（不存在），而是作为 Workflow 执行前后的手动调用
- 在 ConsoleDemo 的 RunWorkflow/ResumeWorkflow 中，在每个 executor 执行前后调用 `AuditLogger.LogAsync`
- 或者：在 executor 基类/接口中添加审计 hook（如果 MAF 支持）

### D-03: SagaBuilder — 自建框架
- **Locked decision:** `SagaBuilder` 封装重试 + 补偿逻辑，独立于 MAF Workflow 运行
- API: `SagaBuilder.OnFailure(step, compensation)` → `WithRetry(maxRetries, delays)` → `ExecuteAsync()`
- 在 ConsoleDemo 层面使用：RunWorkflow 返回 `WorkflowErrorEvent` 时，触发 SagaBuilder 的补偿链
- 重试延迟可配置：demo 用 1s/2s/3s，生产用 1min/5min/30min
- **Why:** MAF Workflow 的 edges 是静态的，但 Saga 的动态补偿（重试 + 条件补偿）更适合独立框架

### D-04: 重试策略 — 可配置时间
- **Locked decision:** 重试延迟通过 `TimeSpan[]` 参数传入，不是硬编码
- ConsoleDemo 调用时用 `TimeSpan.FromSeconds(1/2/3)` 加速验证
- Framework 默认值设为 1min/5min/30min（生产值）
- **Why:** demo 环境不需要真实等待 36 分钟

### D-05: 退款流程集成 — ExecuteRefund 失败触发 RestoreCoupon 补偿
- **Locked decision:** ConsoleDemo 的 RunWorkflow 在收到 `WorkflowErrorEvent` 后：
  1. 检查失败是否来自 `ExecuteRefundExecutor`
  2. 如果是，调用 `RestoreCouponExecutor` 作为补偿
  3. 补偿失败则触发重试（按配置延迟）
- RefundWorkflow 本身不需要修改（补偿在调用层处理）
- **Why:** Saga 补偿是调用层职责，不是工作流定义层职责

### D-06: Audit 捕获点 — WorkflowErrorEvent 和 ExecutorFailedEvent
- **Locked decision:** AuditLogger 捕获：
  - 每个 Workflow 步骤的输入/输出（通过 RunWorkflow 中的事件流）
  - WorkflowErrorEvent（错误详情）
  - ExecutorFailedEvent（失败详情）
  - WorkflowOutputEvent（成功输出）
- 不需要修改 MAF 框架或 Workflow 定义
- **Why:** ConsoleDemo 的事件处理循环已经是所有 Workflow 事件的汇聚点

### Claude's Discretion
- JSONL 记录的具体字段结构（除了 timestamp/sessionId/direction/content）
- SHA256 校验和的计算范围（仅 content 还是整条记录）
- SagaBuilder 的错误分类逻辑（哪些错误需要重试，哪些直接补偿）

</decisions>

<canonical_refs>
## Canonical References

### Phase Requirements
- `.planning/ROADMAP.md` — Phase 7 goal, success criteria, requirement IDs (FW-08, FW-09)
- `.planning/REQUIREMENTS.md` — FW-08, FW-09 definitions

### Existing Skeleton Files (to replace TODO stubs)
- `src/CallCenter.Framework/Audit/AuditLogger.cs` — TODO stub → real implementation
- `src/CallCenter.Framework/Audit/AuditTrailMiddleware.cs` — TODO stub → real implementation
- `src/CallCenter.Framework/Saga/SagaBuilder.cs` — TODO stub → real implementation
- `src/CallCenter.Framework/Saga/SagaExtensions.cs` — TODO stub → real implementation

### Integration Targets
- `src/CallCenter.ConsoleDemo/Program.cs` — RunWorkflow/ResumeWorkflow event handling (where audit capture + saga compensation are wired)
- `src/CallCenter.Workflows/Refund/RefundWorkflow.cs` — workflow definition (not modified, compensation happens at caller level)
- `src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs` — failure source for saga demo
- `src/CallCenter.Workflows/Refund/Executors/RestoreCouponExecutor.cs` — compensation executor

### Prior Phase Context
- `src/CallCenter.Framework/Logging/JsonlLogger.cs` (Phase 6) — operation logging, separate from audit
- `.planning/phases/06-agent-pipeline-compaction/06-CONTEXT.md` — pipeline context for reference

</canonical_refs>

<specifics>
## Specific Ideas

- AuditLogger 写入 `.audit/{sessionId}.jsonl`，每条记录包含 `previousHash` 形成链式校验
- AuditTrailMiddleware 不是真正的中间件，而是 ConsoleDemo 事件循环中的审计 hook
- SagaBuilder API: `new SagaBuilder().OnFailure("ExecuteRefund", async () => await restoreCoupon.Execute()).WithRetry(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)).ExecuteAsync()`
- ConsoleDemo 的 RunWorkflow 在 WorkflowErrorEvent 处理中触发 SagaBuilder
- 加速验证：1s/2s/3s 重试延迟用于 demo，生产默认 1min/5min/30min
- MockExecutor 可以修改 ExecuteRefundExecutor 添加 `failOnce` 标志来触发补偿测试

</specifics>

<deferred>
## Deferred Ideas

- OpenTelemetry 集成 → v2（OB-01）
- 审计日志防篡改存储（区块链/不可变存储）→ v2（OB-02）
- 补偿规则配置化（从文件读取）→ v2
- 多业务模块通用 Saga 编排 → v2
- 补偿失败升级（通知人工介入）→ v2

</deferred>

---

*Phase: 07-audit-logger-saga-compensation*
*Context gathered: 2026-06-01 via discuss-phase*
