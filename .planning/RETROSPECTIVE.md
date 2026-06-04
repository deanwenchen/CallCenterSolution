# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v2.0 — Framework 提取

**Shipped:** 2026-06-03
**Phases:** 2 (9-10) | **Plans:** 6 | **Sessions:** 3

### What Was Built
- CallCenterOptions + Extensions.cs — DI registration with keyed IChatClient, pipeline wrapping, mock service toggling
- AIAgentFactory — intent and dialog agent creation, replacing hardcoded construction in EntryPoint
- EntryPoint migration — constructor accepts factory instead of raw IChatClient
- CallCenterService (5 partial files) — Core.cs (dual constructors + IDisposable), Intent.cs (ProcessAsync), Routing.cs (ResolveWorkflow + timeout detection), Execution.cs (DriveLoopAsync shared event loop, 9 event types, Saga compensation), Interaction.cs (HandleRequestAsync for user interaction)
- Build: 0 errors, 0 warnings across all files

### What Worked
- OpenSpec design.md decisions captured upfront → no mid-phase rework
- Partial class pattern kept each file under 260 lines, easy to navigate
- Plan checker caught `sessionId` data contract gap before execution → saved a compile-fix cycle
- Wave-sequential execution (1→2→3) avoided merge conflicts on partial class files

### What Was Inefficient
- Phase 9 and 10 were planned/executed separately but could have been one phase (both are "skeleton" work)
- Requirements (REQUIREMENTS.md) still exist alongside OpenSpec spec.md — dual sources of truth
- No runtime tests written — verification is purely static (code-level), 5 behavior items deferred

### Patterns Established
- Dual constructor pattern (self-build DI + external DI injection) for services usable both in console and Web API
- `#pragma warning disable MAAI001` on all files using Microsoft.Agents.AI experimental APIs
- DriveLoopAsync shared event loop extracted from RunWorkflow/ResumeWorkflow — eliminates ~50% code duplication
- EventResult enum + ExecutionContext class for async ref-parameter workaround (CS1988)

### Key Lessons
1. **Data contract review across plans catches real bugs** — the sessionId parameter omission was a genuine compile failure waiting to happen; plan checker paid for itself
2. **OpenSpec design decisions are worth capturing before planning** — all 7 design.md decisions were directly referenced in CONTEXT.md and honored in plans
3. **Partial class decomposition works well for service extraction** — each file has a single responsibility, but the shared Core.cs fields provide the glue

### Cost Observations
- Model mix: ~30% opus (planning), ~60% sonnet (execution + verification), ~10% haiku (orchestration)
- Sessions: 3 (plan-phase 9 → execute-phase 9 → plan-phase 10 + execute-phase 10 + complete-milestone)
- Notable: Phase 10 plan+execute+verify completed in a single session with auto-advance

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Sessions | Phases | Key Change |
|-----------|----------|--------|------------|
| v1.0 | 5 | 4 | Established GSD workflow, OpenSpec adoption |
| v1.1 | 4 | 4 | Technical debt closure, pattern maturity |
| v2.0 | 3 | 2 (of 4) | OpenSpec-driven design, plan checker catching data contract bugs |
| v2.1 | 2 | 2 (of 2) | Parallel worktree execution, post-merge build gate catching over-aggressive cleanup |

### Top Lessons (Verified Across Milestones)

1. **Plan checker before execution catches real issues** — sessionId gap (v2.0) saved a compile-fix cycle
2. **Wave-sequential execution on shared files avoids merge conflicts** — partial class files written in order (Core→Intent/Routing→Execution/Interaction)
3. **Static verification catches code-level issues but runtime tests are still needed** — 5 behavior items deferred at v2.0 close
4. **Using 清理必须有编译验证兜底** — 过度清理导致的 CS0246 错误在静态分析阶段不可见 (v2.1)
5. **DI 注册完整性需要运行时验证** — JsonlLogger 缺失注册仅在 startup 时崩溃 (v2.1)

---

## Milestone: v2.1 — Execution & Cleanup

**Shipped:** 2026-06-04
**Phases:** 2 (11-12) | **Plans:** 3 | **Sessions:** 2

### What Was Built
- Program.cs 精简为 18 行：从 434 行手动装配代码 → `new CallCenterService()` + `svc.ProcessAsync()`
- CallCenterService EventBus 集成：两个构造函数各注册 `Subscribe<RefundCompletedEvent>` 回调
- 删除废弃 `ServiceCollectionExtensions.cs`（旧版无 options 参数的 AddCallCenter，0 调用者）
- 清理 29 个未使用的 using 指令，修复 JsonlLogger DI 注册缺失的 runtime bug
- E2E 验证：编译 + 源码断言（7 项）+ 4 个 E2E 场景定义

### What Worked
- Phase 11 plan+execute 一气呵成：Program.cs 从 434 行直接精简到 18 行，效果显著
- 工作树并行执行：两个 plan 同时执行，merge 时自动检测冲突
- Post-merge build gate 发现了 2 个被过度清理的 using 指令（InMemorySessionStore、Mock*Service），及时修复

### What Was Inefficient
- 清理 agent 过度删除了 2 个仍在使用的 using 指令 — 静态分析无法完全替代编译验证
- E2E 测试无法自动化：ConsoleDemo 的并发 stdin readers 导致 piped input 竞争
- 5 个 runtime 验证项从 v2.0 遗留至今仍未实际执行

### Patterns Established
- Program.cs 职责单一：仅负责初始化 + 读取用户输入 + 调用 ProcessAsync
- DI 扩展方法 `AddCallCenter()` 作为唯一的服务注册入口，替代手动装配
- Post-merge build gate 作为并行执行的验证点，补全了工作树隔离的盲区

### Key Lessons
1. **Using 清理需要编译验证作为兜底** — 静态 grep 无法识别间接类型引用（如 DI 注册的服务类型），必须在 dotnet build 之后确认
2. **ConsoleDemo 的 stdin 竞争是架构限制** — 同步 Console.ReadLine() + 异步 Console.In.ReadLineAsync() 无法可靠 piped 测试，需要交互式人工验证
3. **Runtime bug 发现于验证阶段** — JsonlLogger 的 DI 注册缺失在编译期不可见（类型存在），仅在运行时崩溃，说明 DI 容器注册完整性需要额外检查

### Cost Observations
- Model mix: ~20% opus (planning), ~70% sonnet (execution), ~10% haiku (orchestration)
- Sessions: 2 (plan+execute 11 → plan+execute 12 + complete-milestone)
- Notable: Phase 12 的验证 plan 发现了 runtime bug（JsonlLogger），修复仅需 1 行代码
