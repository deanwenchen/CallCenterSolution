# Milestones

## v3.0 Web API + Safety 增强 (Shipped: 2026-06-05)

**Phases completed:** 4 phases, 4 plans, 8 tasks

**Key accomplishments:**

- SSE 流式端点 POST /chat/stream 实现，9 种 WorkflowEvent 实时推送 + sessionId 生命周期管理
- One-liner:

---

## v2.1 Execution & Cleanup (Shipped: 2026-06-04)

**Phases completed:** 2 phases (11-12), 3 plans

**Key accomplishments:**

- Program.cs 精简为 18 行主循环，使用 `new CallCenterService()` + `svc.ProcessAsync()` 替代 434 行旧代码
- CallCenterService 完整接入 EventBus：两个构造函数各注册 `Subscribe<RefundCompletedEvent>` 回调
- 删除废弃 `ServiceCollectionExtensions.cs`（旧版 AddCallCenter 无调用者）
- 清理 29 个未使用的 using 指令，修复 JsonlLogger DI 注册缺失的 runtime bug
- 全解决方案 0 错误编译，4 个 E2E 场景验证定义完成

---

## v2.0 Framework 提取 (Shipped: 2026-06-03)

**Phases completed:** 4 phases (9-10), 6 plans

**Key accomplishments:**

- CallCenterService partial class 拆分为 5 个文件（Core/Intent/Routing/Execution/Interaction）
- 双构造函数模式：自建 DI 容器 + 外部 DI 注入
- ProcessAsync 统一入口：意图识别 → 工作流路由 → 执行/恢复 → 返回结果
- 9 种 WorkflowEvent 类型的 HandleEventAsync 处理 + DriveLoopAsync 共享事件循环
- Saga 补偿机制：ExecuteRefund 失败时自动补偿（恢复优惠券）

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
