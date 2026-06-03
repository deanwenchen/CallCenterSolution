# Milestones

## v2.0 Framework 提取 (Shipped: 2026-06-03)

**Phases completed:** 10 phases, 26 plans, 57 tasks

**Key accomplishments:**

- .NET 10.0 solution with 5 CallCenter projects, 4 MAF source references, and centralized package management (8 packages)
- 状态：已完成
- One-liner:
- Task Consolidation:
- Problem:
- One-liner:
- One-liner:
- One-liner:

---

## v1.0 Refund Workflow Demo (Shipped: 2026-06-01)

**Phases completed:** 4 phases, 13 plans, 34 tasks

**Key accomplishments:**

- .NET 10.0 solution with 5 CallCenter projects (Shared/Framework/Workflows/AgentHost/ConsoleDemo) and centralized package management
- RefundWorkflow: complete 6-step graph execution (GetOrder → CheckRule → WaitConfirm → ExecuteRefund → RestoreCoupon → Notify) with 2 RequestPorts for parameter collection and confirmation
- LLM intent recognition: DashScope Qwen + StructuredOutputParser for typed IntentResult (refund/greeting/unknown)
- RefundSkill registered via AgentSkillsProvider — LLM auto-discovers skills through Frontmatter description
- Session management: InMemorySessionStore with activeWorkflow tracking, timeout detection (30min warning / 60min termination), and intent switch handling
- Checkpoint-based workflow resume: CheckpointManager for super-step persistence across session restarts
- Mock services: 3 test scenarios (A001 refundable / A002 expired / A003 unsigned) for Order/Finance/Member MCP interfaces
- EventBus: publish/subscribe for RefundCompletedEvent with context (sessionId, userId, orderId, amount)
- ~1400 LOC C# across 5 projects, building with 0 errors and 0 warnings

---
