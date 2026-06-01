# Roadmap: CallCenter AI — Refund Workflow Demo

## Overview

**4 phases** | **23 requirements mapped** | All v1 requirements covered ✓

Build a console-based refund workflow demo using Microsoft Agent Framework .NET SDK, validating the complete链路 from intent recognition through workflow execution to business operation completion.

---

### Phase 1: Foundation — Shared + Framework + Workflows

**Goal:** 建立项目基础结构，实现 Shared DTO/Mock 服务、Framework 核心组件、RefundWorkflow 完整执行

**Success Criteria**:

1. dotnet build 成功，无编译错误
2. 7 个 Executor 按 PRD 定义正确实现（GetOrder/CheckRule/WaitConfirm/ExecuteRefund/RestoreCoupon/Notify/Denied）
3. RefundWorkflow.Build() 构建正确图结构（两个 RequestPort + 条件边路由）
4. 3 个 Mock 订单场景可正确返回数据
5. EventBus 可发布和订阅 RefundCompletedEvent

**Requirements:** WF-01, WF-02, WF-03, WF-04, WF-05, MC-01, MC-02, MC-03, SK-01, SK-02, FW-01, FW-02, FW-03, FW-04, DT-01, DT-02, DT-03

---

### Phase 2: AgentHost + Intent Router

**Goal:** 实现 EntryPoint LLM 意图识别、RefundSkill 注册、Workflow 启动/恢复机制

**Success Criteria**:

1. EntryPoint.RecognizeIntentAsync() 正确识别 refund / greeting / unknown 意图
2. RefundSkill 按 PRD 定义实现 Frontmatter/Instructions/Scripts
3. IntentResult 通过 StructuredOutputParser 强类型输出
4. Entry Point 检查 activeWorkflow 决定 Resume 或新启动

**Requirements:** IR-01, IR-02, IR-03, SK-01, SK-02

---

### Phase 3: ConsoleDemo — Main Loop + Event Handling

**Goal:** 实现控制台主循环，集成所有组件，端到端跑通退款流程

**Success Criteria**:

1. dotnet run 启动控制台，提示符正常
2. 输入"我要退款，订单 A001" → 完整流程：查订单 → 校验通过 → 确认 → 退款成功 → 输出结果
3. 输入"我要退款" → 提示"请提供订单号" → 补 A001 → 继续流程（动态追问验证）
4. 输入"我要退款，订单 A002" → 校验失败："超过 7 天不可退"
5. 输入"你好" → 闲聊回复，不启动流程

**Requirements:** CD-01, CD-02, CD-03, IR-03

**Plans:** 2/2 plans complete

Plans:
**Wave 1**

- [x] 03-01-PLAN.md — Session state tracking (lastActivity) + passive timeout + intent switch detection

**Wave 2** *(completed)*

- [x] 03-02-PLAN.md — Checkpoint resume + error recovery + main loop integration + event handling edge cases

---

### Phase 4: Verification + Edge Cases

**Goal:** 验证所有异常场景，补充缺失的 Framework 组件空壳，确保代码质量

**Success Criteria**:

1. 确认提示时输入"取消" → 流程终止，输出取消通知
2. Framework 9 个组件文件全部存在（即使部分为空壳）
3. 目录结构与 PRD 定义完全一致
4. OpenSpec spec.md 所有场景可手动验证通过
5. dotnet build 无警告

**Requirements:** WF-04, FW-01, FW-03, FW-04

**Plans:** 2 plans

Plans:
- [ ] 04-01-PLAN.md — ExecuteRefundExecutor cancel path fix + remove System.Text.Json from .csproj
- [ ] 04-02-PLAN.md — Framework component verification + build verification + manual acceptance testing

---

## Phase Order

```
Phase 1: Foundation (Shared + Framework + Workflows)
    ↓
Phase 2: AgentHost + Intent Router
    ↓
Phase 3: ConsoleDemo — Main Loop + Event Handling
    ↓
Phase 4: Verification + Edge Cases
```

---
*Roadmap created: 2026-06-01*
