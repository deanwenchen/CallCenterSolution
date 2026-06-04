---
phase: 10-callcenter-service-skeleton
plan: "01"
subsystem: CallCenter.AgentHost
tags: [callcenter-service, skeleton, di, partial-class]
dependency_graph:
  requires: []
  provides:
    - "CallCenterService partial class 骨架"
    - "双构造函数模式（自建 DI + 注入 DI）"
    - "IDisposable 实现"
  affects:
    - "后续 10-02, 10-03 partial 文件"
tech_stack:
  added: []
  patterns:
    - "partial class 拆分"
    - "双构造函数模式"
    - "自建 DI 容器 vs 外部注入"
key_files:
  created:
    - "src/CallCenter.AgentHost/CallCenterService.Core.cs"
  modified: []
decisions:
  - "D-10-01: 两个构造函数并存，无参构造函数自建 DI 容器，DI 构造函数 _provider = null"
  - "Dispose 中仅释放自建 ServiceProvider，不释放外部注入的"
metrics:
  duration: "已在上一次执行中完成"
  completed: "2026-06-04"
---

# Phase 10 Plan 01: CallCenterService.Core.cs 创建摘要

**One-liner:** 创建 CallCenterService.Core.cs — partial class 骨架、依赖字段声明、双构造函数模式（自建 DI + 外部注入）、IDisposable 实现

## 任务执行

| # | 任务 | 状态 | Commit | 文件 |
|---|------|------|--------|------|
| 1 | 创建 CallCenterService.Core.cs — partial class 定义、字段、双构造函数、IDisposable | 已完成 | `b8e1aa5` | `src/CallCenter.AgentHost/CallCenterService.Core.cs` |

## 验收标准验证

- [x] 文件 `src/CallCenter.AgentHost/CallCenterService.Core.cs` 存在，包含 `public partial class CallCenterService : IDisposable`
- [x] 两个构造函数签名：`CallCenterService(CallCenterOptions? options = null)` 和 `CallCenterService(IServiceProvider provider)`
- [x] 无参构造函数包含 `services.AddCallCenter(` 和 `services.AddSingleton<AIAgentFactory>()`
- [x] DI 构造函数包含 `_provider = null` 赋值
- [x] 文件包含 `public void Dispose()` 方法
- [x] Dispose 方法包含 `if (_provider is IDisposable disposable)` 条件检查
- [x] `dotnet build` 通过，0 个编译错误

## 文件内容概要

**字段声明（16 个）：**
- `_provider` — IServiceProvider?，DI 注入时为 null
- `_options` — CallCenterOptions
- `_chatClient` — IChatClient（pipeline-wrapped）
- `_agentFactory` — AIAgentFactory
- `_sessionStore` — InMemorySessionStore
- `_checkpointManager` — CheckpointManager（使用 CheckpointManager.Default）
- `_auditLogger` — AuditLogger
- `_eventBus` — IBusinessEventBus
- `_logger` — JsonlLogger
- `_refundWorkflow` — Workflow
- `_skillsProvider` — AgentSkillsProvider
- `_inputChannel` — Channel<string>
- `_inputCts` — CancellationTokenSource
- `_recognizeIntent` — Func<string, CancellationToken, Task<IntentResult?>>
- `_disposed` — bool

**无参构造函数：** 自建 ServiceCollection → AddCallCenter → AddSingleton<AIAgentFactory> → AddSingleton<AgentSkillsProvider> → BuildServiceProvider → resolve 所有依赖 → 启动 stdin 后台读取 Task → 构建意图识别委托

**DI 注入构造函数：** `_provider = null` → 从外部 provider resolve 所有依赖 → 构建意图识别委托

**IDisposable：** 取消 input CTS、完成 inputChannel、仅释放自建的 ServiceProvider（非 null 时）

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None.

## Self-Check: PASSED
