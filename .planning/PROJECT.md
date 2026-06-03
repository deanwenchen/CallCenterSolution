# CallCenter AI

## What This Is

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。用户通过聊天窗口发起业务请求（如"我要退款"），系统识别意图后进入对应的 Workflow 流程，流程执行中需要用户补充信息时自动暂停并询问，收到回复后恢复执行。今天加退款，明天加换货，互不影响，结构清晰。

v1.1 完成后，系统已具备：意图切换、超时管理、6 层安全 Pipeline、审计日志、Saga 补偿、以及新业务模块 7 步扩展指南。

v2.0 框架提取后，系统已具备：CallCenterService 统一服务入口、DI 容器支持、AIAgentFactory 动态创建 Agent、Program.cs 精简到 ~20 行。控制台和未来 Web API 共用同一框架。

## Core Value

用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作 — 整个链路无需人工干预。

## Current State: v1.1 Shipped

**Shipped:** 2026-06-01
**Phases:** 8 (v1.0: 1-4, v1.1: 5-8)
**Build:** 0 errors, 0 warnings across 5 projects
**Projects:** Shared, Framework, Workflows, AgentHost, ConsoleDemo

v1.0 → v1.1 additions:
- Intent switching with workflow termination (IR-04/IR-05)
- 30/60 min timeout tiers (CD-04)
- 6-layer Agent Pipeline with PII redaction, keyword blocking, injection detection (FW-05)
- Compaction via MAF CompactionProvider, 8000 token threshold, 8 turns (FW-07)
- Audit Logger with SHA256 chain, VerifyChainAsync (FW-08)
- Saga Builder with retry + compensation framework (FW-09)
- Business Extensibility Guide (7 steps) + Exchange skeleton (BE-01)

## Current State: v2.0 Framework 提取 (In Progress)

**Goal:** 将 Program.cs（439 行）的框架复杂度抽离到可复用的 CallCenterService 中
**OpenSpec:** extract-callcenter-service
**Tasks:** 47 个任务，12 个实施阶段

v2.0 范围:
- CallCenterService partial 类（Core/Intent/Routing/Execution/Interaction/Extensions）
- AIAgentFactory 按场景动态创建 AIAgent
- 所有基础设施 DI 支持
- CallCenterOptions 环境变量统一处理
- Program.cs 精简到 ~20 行
- **不改变**任何业务流程逻辑和 workflows/ 代码

## Requirements

### Validated

- ✓ **WF-01~WF-05**: RefundWorkflow 完整 6 步执行 + 规则校验 — v1.0
- ✓ **IR-01~IR-03**: LLM 意图识别 + activeWorkflow 检查 + 闲聊处理 — v1.0
- ✓ **MC-01~MC-03**: MCP Client 接口层 — v1.0
- ✓ **SK-01~SK-02**: RefundSkill AgentClassSkill + AgentSkillsProvider 注册 — v1.0
- ✓ **FW-01~FW-04**: EventBus + StructuredOutputParser + InMemorySessionStore + Safety Pipeline 空壳 — v1.0
- ✓ **FW-05**: Agent Pipeline 6 层管道 — Phase 6
- ✓ **FW-06**: InMemorySessionStore — v1.0
- ✓ **FW-07**: Compaction 扩展方法（8000 token 阈值）— Phase 6
- ✓ **FW-08**: Audit Logger（自动捕获 Workflow Step 输入/输出）— Phase 7
- ✓ **FW-09**: Saga 补偿（失败补偿 + 重试策略）— Phase 7
- ✓ **DT-01~DT-03**: Mock 订单/财务/会员服务 — v1.0
- ✓ **CD-01~CD-03**: 控制台主循环 + RequestInfoEvent 处理 + 事件展示 — v1.0
- ✓ **CD-04**: 30 分钟超时提示 — Phase 5
- ✓ **IR-04**: 用户中途切换意图 → 终止旧流程 → 启动新流程 — Phase 5
- ✓ **IR-05**: 用户回复不在预期范围 → 重新意图识别 — Phase 5
- ✓ **BE-01**: 新增业务模块 7 步流程 — Phase 8

### Active

<!-- v2.0 框架提取需求 -->

- [ ] **CS-01**: CallCenterService.ProcessAsync(sessionId, userMessage) → string，内部完成意图识别→工作流执行→返回结果
- [ ] **CS-02**: CallCenterService partial class 拆分（Core/Intent/Routing/Execution/Interaction）
- [ ] **CS-03**: 工作流事件处理完整性（所有 9 种事件类型）
- [ ] **CS-04**: 业务流程不变（退款 6 步流程、事件处理、Saga 补偿、断点恢复）
- [ ] **DI-01**: AddCallCenter() DI 扩展方法，自动注册所有依赖
- [ ] **DI-02**: IChatClient 分层注册（keyed base + pipeline 默认）
- [ ] **DI-03**: CallCenterOptions 环境变量统一处理（ApplyDefaults）
- [ ] **DI-04**: 服务覆盖方法（AddCallCenterOrderService 等）
- [ ] **AF-01**: AIAgentFactory 创建意图识别 Agent
- [ ] **AF-02**: AIAgentFactory 创建工作流对话 Agent
- [ ] **AF-03**: EntryPoint 改用 AIAgentFactory

### Out of Scope (v2.0)

- 真实 MCP Server 调用 — Mock 服务替代
- Session 持久化存储（Redis）— InMemorySessionStore 替代
- Web API 接入层 — 框架支持但具体实现后续
- 新增业务工作流 — 仅做框架重构，不加新业务
- ToolApproval 具体审批规则 → v2
- SafetyOutput 敏感内容拦截 → v2
- KeywordFilter 配置化 → v2
- Exchange Workflow 完整业务逻辑 → v2 (WF-10)

## Context

- 基于 Microsoft Agent Framework .NET SDK（源码引用方式）
- MAF 仓库位于 D:\GitCode\agent-framework\dotnet
- .NET 10.0 目标框架
- 意图识别使用 DashScope（通义千问）OpenAI 兼容接口
- v1.1 代码量增长：新增 ~600 LOC C#（Pipeline、Audit、Saga、Exchange skeleton）
- Exchange 骨架已就绪：Workflow + 7 Executors + Messages + Skill，编译通过
- v2.0 框架提取：OpenSpec change `extract-callcenter-service`

## Constraints

- **[Tech stack]**: .NET 10.0 + Microsoft Agent Framework — PRD 明确要求
- **[Data]**: Demo 阶段全部 Mock，不依赖真实后端
- **[Interface]**: 纯控制台交互，不需要 API 输出
- **[Source]**: 源码调用必须参照 D:\GitCode\agent-framework\dotnet 样本模式
- **[Refactoring]**: v2.0 不改变业务流程逻辑，不改变 workflows/ 代码

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 两个 RequestPort（RefundInfoPort + RefundConfirmPort） | PRD 定义参数收集和确认是不同交互类型 | ✓ Good |
| 意图识别使用 LLM 而非关键词匹配 | 更接近 PRD 真实场景，验证 MAF AIAgent 能力 | ✓ Good |
| MAF SDK 直接引用源码而非 NuGet | 可调试 MAF 内部实现 | ✓ Good |
| 30 分钟超时只警告不清 workflow | Phase 5 修复：原来 30 分钟分支错误地清除了 activeWorkflow | ✓ Good |
| 6 层 Pipeline 通过 StandardPipelineFactory 组装 | Phase 6 实现：SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput | ✓ Good |
| Compaction 使用 MAF CompactionProvider | Phase 6 实现：PipelineCompactionStrategy (8000 token, 8 turns, qwen-plus) | ✓ Good |
| AuditLogger 写入 .audit/ 带 SHA256 链 | Phase 7 实现：不可篡改审计日志，VerifyChainAsync 验证 | ✓ Good |
| SagaBuilder 自建框架 | Phase 7 实现：OnFailure → WithRetry → ExecuteAsync 链式 API | ✓ Good |
| AIAgent 不直接 DI，使用工厂模式 | 不同场景需要不同 System Prompt 和 Tools 配置 | — Pending |
| CallCenterService partial class 拆分 | 按职责拆分，每文件职责清晰，新增业务只需改对应文件 | — Pending |
| ProcessAsync 返回 string 阻塞到终态 | 控制台和 Web API 都只需要最终结果 | — Pending |

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

*Last updated: 2026-06-03 after v2.0 milestone start*
