# CallCenter AI

## What This Is

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。用户通过聊天窗口发起业务请求（如"我要退款"），系统识别意图后进入对应的 Workflow 流程，流程执行中需要用户补充信息时自动暂停并询问，收到回复后恢复执行。今天加退款，明天加换货，互不影响，结构清晰。

## Core Value

用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作 — 整个链路无需人工干预。

## Requirements

### Validated

- ✓ **WF-01~WF-05**: RefundWorkflow 完整 6 步执行 + 规则校验 — v1.0
- ✓ **IR-01~IR-03**: LLM 意图识别 + activeWorkflow 检查 + 闲聊处理 — v1.0
- ✓ **MC-01~MC-03**: MCP Client 接口层（IOrderMcpClient/IFinanceMcpClient/IMemberMcpClient）— v1.0
- ✓ **SK-01~SK-02**: RefundSkill AgentClassSkill + AgentSkillsProvider 注册 — v1.0
- ✓ **FW-01~FW-04**: EventBus + StructuredOutputParser + InMemorySessionStore + Safety Pipeline 空壳 — v1.0
- ✓ **FW-06**: InMemorySessionStore — v1.0
- ✓ **DT-01~DT-03**: Mock 订单/财务/会员服务（3 测试场景）— v1.0
- ✓ **CD-01~CD-03**: 控制台主循环 + RequestInfoEvent 处理 + 事件展示 — v1.0

### Active

- [ ] **IR-04**: 用户中途切换意图 → 终止旧流程 → 启动新流程
- [ ] **IR-05**: 用户回复不在预期范围 → 重新意图识别
- [ ] **FW-05**: Agent Pipeline 6 层管道
- [ ] **FW-07~FW-09**: Compaction / Audit Logger / Saga 补偿 — 空壳
- [ ] **CD-04**: 30 分钟超时提示
- [ ] **BE-01**: 新增业务模块 7 步流程（复制 → 重命名 → 修改 → 完成）

### Out of Scope

- 真实 MCP Server 调用 — Mock 服务替代
- Session 持久化存储（Redis）— InMemorySessionStore 替代
- Knowledge Layer / Observability Layer / Human Agent Layer — 旁路系统后续实现
- Web/Gateway 接入层 — 控制台调试入口替代

## Context

- 基于 Microsoft Agent Framework .NET SDK（源码引用方式）
- MAF 仓库位于 D:\GitCode\agent-framework\dotnet
- .NET 10.0 目标框架
- 参考样本：03-workflows/HumanInTheLoop, 03-workflows/ConditionalEdges
- 意图识别使用 DashScope（通义千问）OpenAI 兼容接口
- 代码量：~1400 LOC C#，5 个项目（Shared/Framework/Workflows/AgentHost/ConsoleDemo）
- v1.0 已实现端到端退款流程：用户输入 → LLM 意图识别 → Workflow 执行 → 结果输出

## Constraints

- **[Tech stack]**: .NET 10.0 + Microsoft Agent Framework — PRD 明确要求
- **[Data]**: Demo 阶段全部 Mock，不依赖真实后端
- **[Interface]**: 纯控制台交互，不需要 API 输出
- **[Source]**: 源码调用必须参照 D:\GitCode\agent-framework\dotnet 样本模式

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 两个 RequestPort（RefundInfoPort + RefundConfirmPort） | PRD 定义参数收集和确认是不同交互类型 | ✓ Good |
| 意图识别使用 LLM 而非关键词匹配 | 更接近 PRD 真实场景，验证 MAF AIAgent 能力 | ✓ Good |
| MAF SDK 直接引用源码而非 NuGet | 可调试 MAF 内部实现 | ✓ Good |
| Framework 9 组件全建但仅 4 个可用 | 按 PRD 目录结构，Demo 阶段核心先用 | ✓ Good |
| 退款流程 6 步完整实现 | 验证 PRD 定义的完整链路 | ✓ Good |
| AgentSkillsProvider 通过 DI 传入 EntryPoint | MAF 框架提供的标准方式，ChatClientAgentOptions.AIContextProviders | ✓ Good |
| 会话超时：30 分钟警告 + 60 分钟终止 | 用户可配置的超时策略 | ✓ Good |
| Checkpoint 管理使用 CheckpointManager.Default | MAF 框架标准模式 | ✓ Good |

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
*Last updated: 2026-06-01 after v1.0 Refund Workflow Demo milestone*
