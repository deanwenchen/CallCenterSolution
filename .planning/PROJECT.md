# CallCenter AI

## What This Is

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。用户通过聊天窗口发起业务请求（如"我要退款"），系统识别意图后进入对应的 Workflow 流程，流程执行中需要用户补充信息时自动暂停并询问，收到回复后恢复执行。今天加退款，明天加换货，互不影响，结构清晰。

v2.1 完成后，系统已具备：意图切换、超时管理、6 层安全 Pipeline、审计日志、Saga 补偿、以及新业务模块 7 步扩展指南。CallCenterService 统一服务入口已就绪，ConsoleDemo 和 WebApi 共用同一框架。

## Core Value

用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作 — 整个链路无需人工干预。

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
- ✓ **CS-01~CS-04**: CallCenterService 完整骨架（ProcessAsync、partial class、9 种事件处理、业务流程不变）— v2.0
- ✓ **DI-01~DI-04**: DI 扩展方法、IChatClient 分层注册、CallCenterOptions、服务覆盖方法 — v2.0
- ✓ **AF-01~AF-03**: AIAgentFactory 创建意图/对话 Agent + EntryPoint 改用 — v2.0
- ✓ **审计链修复**: SHA256 链初始化 bug 修复 — v3.0
- ✓ **quit 命令修复**: 工作流等待确认阶段支持 quit 退出 — v3.0

### Active

<!-- v3.0 Web API + Safety 增强 -->

- [ ] **WA-01**: 新增 CallCenter.WebApi 项目，ASP.NET Core Minimal API
- [ ] **WA-02**: POST /chat 端点，接收消息返回 SSE 流式响应
- [ ] **WA-03**: SSE 端点复用 CallCenterService.ProcessAsync
- [ ] **WA-04**: 会话管理（sessionId 生成/恢复、超时清理）
- [ ] **WA-05**: CORS 配置，允许前端跨域访问
- [ ] **SI-01**: Pipeline SafetyInput 层 — PII 脱敏（邮箱/手机号/身份证）
- [ ] **SI-02**: Pipeline SafetyInput 层 — 关键词黑名单检测
- [ ] **SI-03**: Pipeline SafetyInput 层 — Prompt injection 检测
- [ ] **SI-04**: KeywordFilter 配置化（从硬编码到 JSON 配置）
- [ ] **SO-01**: Pipeline SafetyOutput 层 — 敏感内容拦截（暴力/色情/政治等）

### Out of Scope (v3.0)

- JWT 认证 / API Key — v4
- Exchange Workflow 完整业务逻辑 — v4 (WF-10)
- Session 持久化存储（Redis）— InMemorySessionStore 替代
- ToolApproval 具体审批规则 → v4
- 真实 MCP Server 调用 — Mock 服务替代

## Context

- 基于 Microsoft Agent Framework .NET SDK（源码引用方式）
- MAF 仓库位于 D:\GitCode\agent-framework\dotnet
- .NET 10.0 目标框架
- 意图识别使用 DashScope（通义千问）OpenAI 兼容接口
- v2.1 代码量增长：新增 ~600 LOC C#（Pipeline、Audit、Saga、Exchange skeleton）
- Exchange 骨架已就绪：Workflow + 7 Executors + Messages + Skill，编译通过
- v2.0 框架提取：OpenSpec change `extract-callcenter-service`（已完成）
- ConsoleDemo 保持不变，WebApi 作为新增入口

## Constraints

- **[Tech stack]**: .NET 10.0 + Microsoft Agent Framework — PRD 明确要求
- **[Data]**: Demo 阶段全部 Mock，不依赖真实后端
- **[Interface]**: 纯控制台交互，不需要 API 输出
- **[Source]**: 源码调用必须参照 D:\GitCode\agent-framework\dotnet 样本模式
- **[Refactoring]**: v3.0 不改变现有业务流程逻辑，只增加入口和增强安全层

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
| AIAgent 不直接 DI，使用工厂模式 | 不同场景需要不同 System Prompt 和 Tools 配置 | ✓ Good |
| CallCenterService partial class 拆分 | 按职责拆分，每文件职责清晰，新增业务只需改对应文件 | ✓ Good |
| ProcessAsync 返回 string 阻塞到终态 | 控制台和 Web API 都只需要最终结果 | ✓ Good |

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

*Last updated: 2026-06-04 after v3.0 milestone start*
