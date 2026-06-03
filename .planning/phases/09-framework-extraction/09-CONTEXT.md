# Phase 9: 基础配置与工厂 - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning

## Phase Boundary

建立 CallCenterOptions 配置类、AIAgentFactory 工厂类、修改 EntryPoint 构造函数使用 AIAgentFactory。不涉及 CallCenterService 的创建（Phase 10），不涉及工作流执行逻辑的改动（Phase 11）。

## OpenSpec 约束

本 Phase 受 OpenSpec change `extract-callcenter-service` 约束。所有决策不得违背 OpenSpec specs 中定义的要求：
- 不改变业务流程逻辑
- 不改变 workflows/ 代码
- AIAgent 不直接 DI，通过工厂创建
- CallCenterOptions.ApplyDefaults() 统一处理环境变量

## Implementation Decisions

### Pipeline sessionId 处理

- **D-01:** Pipeline 注册为 singleton 时，sessionId 写死为 "pipeline"（快速方案）。LoggingDelegatingClient 的 sessionId 参数传常量字符串。后续 Phase 可改为 session-aware。

### EntryPoint 构造函数迁移

- **D-02:** EntryPoint 构造函数直接改为 `EntryPoint(AIAgentFactory, InMemorySessionStore, AgentSkillsProvider?)`，删除旧的 `IChatClient` 参数。Phase 9 同步修改 Program.cs 中的调用代码。不使用 [Obsolete] 兼容层。

### Mock 服务注册

- **D-03:** Extensions.cs 默认注册 Mock 服务（MockOrderService、MockFinanceService、MockMemberService）。UseMockServices = true 为默认。Web API 场景通过 AddCallCenterOrderService<T>() 等方法覆盖。

### Claude's Discretion

- CallCenterOptions 的具体属性名和默认值由实施者决定，参考现有 Program.cs 中的变量名
- AIAgentFactory 的 CreateDialogAgent System Prompt 内容待定（Phase 9 只注册，暂不使用）

## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### OpenSpec 工件
- `openspec/changes/extract-callcenter-service/proposal.md` — 变更动机、范围、影响
- `openspec/changes/extract-callcenter-service/design.md` — 技术设计、决策理由
- `openspec/changes/extract-callcenter-service/specs/callcenter-service/spec.md` — CallCenterService 规格
- `openspec/changes/extract-callcenter-service/specs/di-support/spec.md` — DI 支持规格
- `openspec/changes/extract-callcenter-service/specs/agent-factory/spec.md` — AIAgentFactory 规格
- `openspec/changes/extract-callcenter-service/tasks.md` — 47 个实施任务

### 代码库参照
- `src/CallCenter.AgentHost/EntryPoint.cs` — 需要修改构造函数
- `src/CallCenter.AgentHost/IntentRegistry.cs` — 意图注册表，AIAgentFactory 依赖
- `src/CallCenter.AgentHost/Skills/SkillRegistry.cs` — 技能注册表
- `src/CallCenter.ConsoleDemo/Program.cs` — 需要修改 EntryPoint 调用
- `src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs` — Pipeline 创建参照
- `src/CallCenter.Framework/ServiceCollectionExtensions.cs` — 旧 DI 扩展，后续移除

### 规划文档
- `.planning/ROADMAP.md` — Phase 9 目标
- `.planning/REQUIREMENTS.md` — Phase 9 需求 (DI-01~04, AF-01~03)
- `.planning/PROJECT.md` — 项目上下文

## Existing Code Insights

### Reusable Assets

- `StandardPipelineFactory.CreatePipeline()` — 6 层管道组装，Extensions.cs 直接复用
- `StandardPipelineFactory.CreateSummarizerClient()` — 压缩客户端创建，Extensions.cs 复用
- `IntentRegistry.BuildSystemPrompt()` — AIAgentFactory.CreateIntentAgent 的系统 Prompt 来源
- `SkillRegistry.All` — AgentSkillsProvider 注册来源
- `InMemorySessionStore`、`AuditLogger`、`InMemoryBusinessEventBus` — 已有实现，DI 直接注册

### Established Patterns

- 所有 Mock 服务实现 `IMcpClient` 接口，构造函数无参数
- EntryPoint 当前使用 `new ChatClientAgent(chatClient, ChatClientAgentOptions)` 创建 Agent
- Program.cs 使用 `Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")` 直接读取

### Integration Points

- EntryPoint 构造函数是 Phase 9 的唯一修改点（参数从 IChatClient → AIAgentFactory）
- Program.cs 的 `new EntryPoint(...)` 调用需要同步修改
- Extensions.cs 新增的 `AddCallCenter()` 替代旧的 `ServiceCollectionExtensions.AddCallCenter()`

## Deferred Ideas

- Pipeline session-aware — 后续 Phase 改进
- AIAgentFactory.CreateDialogAgent 的实际 System Prompt 内容 — 后续 Phase 使用时确定
- Web API 交互机制 — v2 范围

---

*Phase: 9-基础配置与工厂*
*Context gathered: 2026-06-03*
