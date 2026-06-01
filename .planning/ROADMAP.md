# Roadmap: CallCenter AI — v1.1 Technical Debt Closure

## Milestones

- ✅ **v1.0 Refund Workflow Demo** — Phases 1-4 (shipped 2026-06-01)
- 🚧 **v1.1 Technical Debt Closure** — Phases 5-8 (in progress)

## Phases

<details>
<summary>✅ v1.0 Refund Workflow Demo (Phases 1-4) — SHIPPED 2026-06-01</summary>

- [x] Phase 1: Foundation (6/6 plans) — completed 2026-06-01
- [x] Phase 2: AgentHost + Intent Router (3/3 plans) — completed 2026-06-01
- [x] Phase 3: ConsoleDemo — Main Loop + Event Handling (2/2 plans) — completed 2026-06-01
- [x] Phase 4: Verification + Edge Cases (2/2 plans) — completed 2026-06-01

</details>

### 🚧 v1.1 Technical Debt Closure (In Progress)

---

### Phase 5: Intent Switching + Timeout

- [x] Phase 5: Intent Switching + Timeout (1/1 plans) — completed 2026-06-01

**Goal:** 实现意图切换（退款中改换货）、异常输入重新识别、30 分钟超时提示

**Success Criteria**:
1. 退款过程中输入"我要换货" → 终止退款流程 → 提示"换货流程暂未实现"
2. 确认退款时输入"我要投诉" → 流程挂起 → 重新意图识别 → 返回闲聊回复
3. 60 分钟无输入 → 会话终止提示
4. EntryPoint.CheckTimeoutAsync() 正确实现 30/60 分钟分级

**Requirements:** IR-04, IR-05, CD-04

---

### Phase 6: Agent Pipeline + Compaction

- [x] Phase 6: Agent Pipeline + Compaction (2/2 plans) — completed 2026-06-01

**Goal:** 实现 6 层 Agent Pipeline 和 Compaction 扩展方法

**Success Criteria**:
1. AgentPipeline 接口定义：6 层管道可配置，每层可插拔
2. SafetyInput：基础 PII 脱敏（手机号/身份证/银行卡正则）
3. Logging：请求/响应日志记录
4. Compaction：8000 token 阈值触发，保留最近 8 轮，使用小模型摘要
5. ToolApproval：工具调用审批框架
6. SafetyOutput：输出脱敏过滤
7. Pipeline 接入 ChatClientAgent 调用链

**Requirements:** FW-05, FW-07

---

### Phase 7: Audit Logger + Saga Compensation

- [x] Phase 7: Audit Logger + Saga Compensation (2/2 plans) — completed 2026-06-01

**Goal:** 实现 Workflow 步骤审计日志和失败补偿机制

**Success Criteria**:
1. AuditLogger 自动捕获 Workflow Step 输入/输出，写入日志文件
2. Saga 补偿接口：失败时触发补偿动作
3. 重试策略：3 级重试（1min/5min/30min）
4. 退款流程集成：ExecuteRefund 失败时自动触发 RestoreCoupon 补偿

**Requirements:** FW-08, FW-09

---

### Phase 8: Business Extensibility Guide

**Goal:** 建立新增业务模块 7 步流程文档化，验证扩展性

**Success Criteria**:
1. 编写完整的 7 步扩展指南文档
2. 在指南基础上创建 ExchangeSkill 空壳（不实现完整逻辑，验证结构正确）
3. 新增业务模块项目录结构符合 PRD 定义
4. 文档清晰到新手可复制完成

**Requirements:** BE-01

---

## Phase Order

```
Phase 5: Intent Switching + Timeout
    ↓
Phase 6: Agent Pipeline + Compaction
    ↓
Phase 7: Audit Logger + Saga Compensation
    ↓
Phase 8: Business Extensibility Guide
```

---
*Roadmap updated: 2026-06-01 for v1.1 milestone*
