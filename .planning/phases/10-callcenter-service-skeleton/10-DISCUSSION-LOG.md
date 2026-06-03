# Phase 10 Discussion Log

**Phase:** 10 — CallCenterService 骨架
**Date:** 2026-06-03

## Areas Discussed

### 1. CallCenterService 构造函数设计

**Question:** 构造函数应该如何处理依赖注入？

**Options:**
- A: 仅 self-bootstrapping
- B: 同时支持自建 + DI 注入 (Recommended) ✓
- C: 纯 DI 注入

**Decision:** Option B — 两个构造函数并存。无参的自建 ServiceCollection，带 IServiceProvider 的接受外部 DI。

**Rationale:** 覆盖当前控制台场景和未来 Web API 场景。

### 2. Saga 补偿归属

**Question:** Saga 补偿逻辑应该放在哪里？

**Options:**
- A: 放在 Execution.cs 内部 (Recommended) ✓
- B: 保留在调用者层

**Decision:** Option A — Saga 补偿逻辑放在 Execution.cs 的 HandleEventAsync 内部。

**Rationale:** CallCenterService 自包含完整错误处理能力，不需要调用者关心补偿逻辑。

## Deferred Ideas

- Console.In Channel 解耦是否迁移到 Interaction.cs
- Web API 交互机制 — v2 范围
- Pipeline session-aware

---

*Discussion complete: 2026-06-03*
