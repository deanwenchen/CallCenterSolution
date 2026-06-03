# Phase 10: CallCenterService 骨架 - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning

## Domain

创建 `CallCenterService` partial class 骨架，将 Program.cs 中的 RunWorkflow、ResumeWorkflow、HandleRequestAsync 等逻辑迁移到服务层。Core.cs、Intent.cs、Routing.cs、Execution.cs、Interaction.cs 五个文件各司其职。

## Implementation Decisions

### 构造函数设计

- **D-10-01:** 两个构造函数并存：
  - **无参构造函数** `CallCenterService(CallCenterOptions? options = null)` — 内部自建 ServiceCollection → `AddCallCenter(options)` → `services.AddSingleton<AIAgentFactory>()` → BuildServiceProvider → resolve 所有依赖。控制台场景开箱即用。
  - **DI 注入构造函数** `CallCenterService(IServiceProvider provider)` — 接受外部 DI 容器，不自建 ServiceCollection。Web API 场景注入，`_provider = null`（不 Build，外部管理生命周期）。

### Saga 补偿归属

- **D-10-02:** Saga 补偿逻辑放在 `Execution.cs` 的 `HandleEventAsync` 内部。当 `WorkflowErrorEvent` 的 executor 为 "ExecuteRefund" 时，直接在服务层触发 Saga 补偿（恢复优惠券）。CallCenterService 自包含完整错误处理能力，不需要调用者关心补偿逻辑。

## Carrying Forward from Phase 9

- **D-01:** Pipeline sessionId 写死为 "pipeline"（常量）
- **D-02:** EntryPoint 构造函数使用 AIAgentFactory（已迁移）
- **D-03:** Extensions.cs 默认注册 Mock 服务
- `CallCenterOptions.cs` 已存在，`ApplyDefaults()` 是唯一 DASHSCOPE_API_KEY 读取点
- `AIAgentFactory.cs` 已存在，`CreateIntentAgent()` + `CreateDialogAgent()`
- `Extensions.cs` 已存在，`AddCallCenter()` + 3 个 override 方法

## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### OpenSpec 工件
- `openspec/changes/extract-callcenter-service/specs/callcenter-service/spec.md` — CallCenterService 规格（4 个 requirement，含场景定义）
- `openspec/changes/extract-callcenter-service/design.md` — 技术设计、决策（7 个 design decisions）
- `openspec/changes/extract-callcenter-service/proposal.md` — 变更动机、范围、影响

### 代码库参照
- `src/CallCenter.ConsoleDemo/Program.cs` — RunWorkflow/ResumeWorkflow/HandleRequestAsync 的当前实现（待迁移）
- `src/CallCenter.AgentHost/Extensions.cs` — AddCallCenter() DI 注册
- `src/CallCenter.AgentHost/AIAgentFactory.cs` — 意图 Agent 工厂
- `src/CallCenter.Framework/CallCenterOptions.cs` — 配置类
- `src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs` — 6 层管道创建
- `src/CallCenter.Framework/Saga/SagaBuilder.cs` — Saga 补偿框架
- `src/CallCenter.Workflows/Refund/RefundWorkflow.cs` — 退款工作流定义

### 规划文档
- `.planning/ROADMAP.md` — Phase 10 目标
- `.planning/REQUIREMENTS.md` — v2.0 需求 (CS-03)
- `.planning/phases/09-framework-extraction/09-CONTEXT.md` — Phase 9 上下文

## Existing Code Insights

### Reusable Assets

- `StandardPipelineFactory` — 管道创建（已在 Phase 9  Extensions.cs 中注册）
- `SagaBuilder` — Saga 补偿框架（Phase 7 已实现，Program.cs 中在用）
- `AuditTrailMiddleware` — 审计日志中间件（CaptureStepStart/CaptureStepEnd/CaptureError）
- `CheckpointManager.Default` — 断点管理器
- `InMemoryExecution` — `RunStreamingAsync` / `ResumeStreamingAsync` 执行器
- `IntentRegistry` — 意图识别（EntryPoint 已用）
- `AIAgentFactory` — Agent 工厂（Phase 9 已创建）

### Established Patterns

- Program.cs 的 RunWorkflow 和 ResumeWorkflow ~50% 代码重复 — 遍历同样的 5 种事件类型
- Saga 补偿只在 Program.cs 的 RunWorkflow 中处理 ExecuteRefund 失败
- HandleRequestAsync 处理 RefundSignal 和 ConfirmRefundRequest 两种请求
- Console.In 通过 Channel 解耦读取（避免 ReadLineAsync 被抢占）

### Integration Points

- CallCenterService 替代 EntryPoint + Program.cs 中的工作流驱动逻辑
- 保持 EntryPoint 的意图识别和路由能力（Phase 10 不修改 EntryPoint）
- Program.cs 的主循环保留，但调用方从 `entryPoint.ProcessAsync()` 变为 `callCenterService.ProcessAsync()`

## Deferred Ideas

- Console.In Channel 解耦是否迁移到 Interaction.cs — 后续 Phase 决定
- Web API 交互机制 — v2 范围
- Pipeline session-aware — 后续 Phase 改进

---

*Phase: 10-CallCenterService-skeleton*
*Context gathered: 2026-06-03*
