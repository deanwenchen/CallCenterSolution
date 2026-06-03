# Roadmap: CallCenter AI

## Milestones

- ✅ **v1.0 Refund Workflow Demo** — Phases 1-4 (shipped 2026-06-01)
- ✅ **v1.1 Technical Debt Closure** — Phases 5-8 (shipped 2026-06-01)
- ✅ **v2.0 Framework 提取** — Phases 9-10 (shipped 2026-06-03)
- 📋 **v2.1 Execution & Cleanup** — Phases 11-12 (planned)

## Phases

<details>
<summary>✅ v1.0 Refund Workflow Demo (Phases 1-4) — SHIPPED 2026-06-01</summary>

- [x] Phase 1: Foundation (6/6 plans) — completed 2026-06-01
- [x] Phase 2: AgentHost + Intent Router (3/3 plans) — completed 2026-06-01
- [x] Phase 3: ConsoleDemo — Main Loop + Event Handling (2/2 plans) — completed 2026-06-01
- [x] Phase 4: Verification + Edge Cases (2/2 plans) — completed 2026-06-01

</details>

<details>
<summary>✅ v1.1 Technical Debt Closure (Phases 5-8) — SHIPPED 2026-06-01</summary>

- [x] Phase 5: Intent Switching + Timeout (1/1 plans) — completed 2026-06-01
- [x] Phase 6: Agent Pipeline + Compaction (2/2 plans) — completed 2026-06-01
- [x] Phase 7: Audit Logger + Saga Compensation (2/2 plans) — completed 2026-06-01
- [x] Phase 8: Business Extensibility Guide (2/2 plans) — completed 2026-06-01

See: [.planning/milestones/v1.1-TECHNICAL-DEBT-CLOSURE.md](.planning/milestones/v1.1-TECHNICAL-DEBT-CLOSURE.md)

</details>

<details>
<summary>✅ v2.0 Framework 提取 (Phases 9-10) — SHIPPED 2026-06-03</summary>

- [x] Phase 9: 基础配置与工厂 — CallCenterOptions + AIAgentFactory + EntryPoint 修改
  - **Plans:** 3 plans
  - [x] 09-01-PLAN.md — CallCenterOptions 配置类 + Extensions.cs DI 扩展方法
  - [x] 09-02-PLAN.md — AIAgentFactory 工厂类（CreateIntentAgent + CreateDialogAgent）
  - [x] 09-03-PLAN.md — EntryPoint 构造函数迁移 + Program.cs 同步
- [x] Phase 10: CallCenterService 骨架 — Core/Routing/Interaction/Extensions partial 类 (completed 2026-06-03)
  - **Plans:** 3 plans
  - [x] 10-01-PLAN.md — Core.cs 骨架：partial class 定义、双构造函数模式、IDisposable
  - [x] 10-02-PLAN.md — Intent.cs + Routing.cs：ProcessAsync 入口、意图→工作流映射
  - [x] 10-03-PLAN.md — Execution.cs + Interaction.cs：DriveLoopAsync 事件循环、9 种事件处理、HandleRequestAsync

See: [.planning/milestones/v2.0-REQUIREMENTS.md](.planning/milestones/v2.0-REQUIREMENTS.md)

</details>

### 📋 v2.1 Execution & Cleanup (Planned)

- [x] Phase 11: 执行层与入口 — Execution/Intent partial 类 + Program.cs 精简 (completed 2026-06-03)
  - **Plans:** 1 plan
  - [x] 11-01-PLAN.md — Core.cs EventBus 订阅 + Program.cs 精简为 ~25-30 行主循环
- [ ] Phase 12: 清理与验证 — 清理旧代码 + 端到端测试

OpenSpec: `extract-callcenter-service` (47 tasks — phases 9-12)

---

*Roadmap updated: 2026-06-03 after Phase 11 planning*
