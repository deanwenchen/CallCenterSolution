# CallCenter AI

## What This Is

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。用户通过聊天窗口发起业务请求（如"我要退款"），系统识别意图后进入对应的 Workflow 流程，流程执行中需要用户补充信息时自动暂停并询问，收到回复后恢复执行。今天加退款，明天加换货，互不影响，结构清晰。

## Core Value

用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作 — 整个链路无需人工干预。

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] **WF-01**: 退款流程支持 6 步执行：GetOrder → CheckRefundRule → WaitConfirm → ExecuteRefund → RestoreCoupon → SendNotification
- [ ] **WF-02**: 流程缺少参数时自动回 RequestPort 询问用户（动态追问机制）
- [ ] **WF-03**: 用户确认退款前暂停流程，展示订单信息和退款金额
- [ ] **IR-01**: Entry Point 实现意图识别（LLM → StructuredOutputParser → 强类型 IntentResult）
- [ ] **IR-02**: Entry Point 检查 activeWorkflow 决定 Resume 或新启动
- [ ] **MC-01**: MCP Client 接口层封装（OrderMcpClient / FinanceMcpClient / MemberMcpClient）
- [ ] **SK-01**: RefundSkill 通过 AgentClassSkill 定义（Frontmatter + Instructions + Scripts）
- [ ] **FW-01**: EventBus 实现业务事件发布/订阅（RefundCompletedEvent / RiskAlertEvent）
- [ ] **FW-02**: StructuredOutputParser 将 LLM JSON 转为强类型对象
- [ ] **FW-03**: Safety Pipeline 实现 PII 脱敏和关键词拦截
- [ ] **DT-01**: Mock 数据支持 3 个测试场景（A001 可退 / A002 超期 / A003 未签收）

### Out of Scope

- 多业务模块（Exchange/Logistics）— Demo 阶段只做退款，保留扩展结构
- 真实 MCP Server 调用 — Mock 服务替代
- Session 持久化存储（Redis）— InMemorySessionStore 替代
- Knowledge Layer / Observability Layer / Human Agent Layer — 旁路系统后续实现
- Web/Gateway 接入层 — 控制台调试入口替代

## Context

- 基于 Microsoft Agent Framework .NET SDK（源码引用方式）
- MAF 仓库位于 D:\GitCode\agent-framework\dotnet
- .NET 10.0 目标框架
- 参考样本：03-workflows/HumanInTheLoop, 03-workflows/ConditionalEdges, 03-workflows/_StartHere
- 意图识别使用 DashScope（通义千问）OpenAI 兼容接口
- OpenSpec 文档已创建：openspec/changes/refund-workflow-demo/

## Constraints

- **[Tech stack]**: .NET 10.0 + Microsoft Agent Framework — PRD 明确要求
- **[Data]**: Demo 阶段全部 Mock，不依赖真实后端
- **[Interface]**: 纯控制台交互，不需要 API 输出
- **[Source]**: 源码调用必须参照 D:\GitCode\agent-framework\dotnet 样本模式

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 两个 RequestPort（RefundInfoPort + RefundConfirmPort） | PRD 定义参数收集和确认是不同交互类型，分开更清晰 | ✓ Good |
| 意图识别使用 LLM 而非关键词匹配 | 更接近 PRD 真实场景，验证 MAF AIAgent 能力 | ✓ Good |
| MAF SDK 直接引用源码而非 NuGet | 可调试 MAF 内部实现，理解框架细节 | ✓ Good |
| Framework 9 组件全建但仅 3 个可用 | 按 PRD 目录结构，Demo 阶段核心先用 | ✓ Good |
| 退款流程 6 步完整实现 | 验证 PRD 定义的完整链路 | ✓ Good |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-01 after initialization*
