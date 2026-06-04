---
phase: 12-cleanup-and-verification
plan: 01
subsystem: infra
tags: [dotnet, using-cleanup, dead-code-removal]

# Dependency graph
requires:
  - phase: 11-execution-entry
    provides: CallCenterService partial files with working DI wiring
provides:
  - Dead code removal: Framework ServiceCollectionExtensions.cs deleted
  - All AgentHost source files cleaned of unused using directives
  - Compilation verified with zero new errors
affects: [future phases adding new workflows or services]

# Tech tracking
tech-stack:
  added: []
  patterns: [ImplicitUsings relied upon for System/System.Collections.Generic]

key-files:
  created: []
  modified:
    - src/CallCenter.Framework/ServiceCollectionExtensions.cs (deleted)
    - src/CallCenter.AgentHost/CallCenterService.Core.cs
    - src/CallCenter.AgentHost/CallCenterService.Execution.cs
    - src/CallCenter.AgentHost/CallCenterService.Intent.cs
    - src/CallCenter.AgentHost/CallCenterService.Interaction.cs
    - src/CallCenter.AgentHost/CallCenterService.Routing.cs
    - src/CallCenter.AgentHost/EntryPoint.cs
    - src/CallCenter.AgentHost/Extensions.cs
    - src/CallCenter.AgentHost/IntentRegistry.cs

key-decisions:
  - "Deleted entire ServiceCollectionExtensions.cs file rather than removing just the method — file served no other purpose"
  - "Retained conservative usings where indirect usage was uncertain (per plan guidance: don't over-clean)"

patterns-established: []

requirements-completed: []

# Metrics
duration: 12min
completed: 2026-06-04
---

# Phase 12 Plan 01: 死代码清理与 using 指令精简

删除 Framework 中废弃的旧 AddCallCenter 方法（整个文件删除），精简 8 个 AgentHost 源文件共 29 行未使用的 using 指令。

## Performance

- **Duration:** 12 min
- **Started:** 2026-06-04T00:36:59Z
- **Completed:** 2026-06-04T00:48:00Z
- **Tasks:** 2
- **Files modified:** 9 (1 deleted, 8 modified)

## Accomplishments

- 删除 `src/CallCenter.Framework/ServiceCollectionExtensions.cs` — 旧版无 options 参数的 AddCallCenter 方法，无任何调用者
- 清理 8 个 AgentHost 源文件的未使用 using 指令，共删除 29 行：
  - Core.cs: 19 → 10（删除 8 个）
  - Execution.cs: 8 → 4（删除 4 个）
  - Intent.cs: 3 → 0（删除 3 个，所有类型在同命名空间）
  - Interaction.cs: 5 → 2（删除 3 个）
  - Routing.cs: 7 → 3（删除 4 个）
  - EntryPoint.cs: 9 → 7（删除 2 个）
  - Extensions.cs: 14 → 12（删除 2 个）
  - IntentRegistry.cs: 2 → 1（删除 1 个）
- 编译验证：6 个错误全部为预存在的（缺少 Microsoft.Agents.AI 外部项目引用），零新增错误

## Task Commits

Each task was committed atomically:

1. **Task 1: 删除 ServiceCollectionExtensions.cs** - `f377447` (chore)
2. **Task 2: 清理未使用的 using 指令** - `e7c9821` (refactor)

**Plan metadata:** pending final commit

## Files Created/Modified

- `src/CallCenter.Framework/ServiceCollectionExtensions.cs` — 删除（旧 AddCallCenter，无调用者）
- `src/CallCenter.AgentHost/CallCenterService.Core.cs` — 删除 8 个未使用 using
- `src/CallCenter.AgentHost/CallCenterService.Execution.cs` — 删除 4 个未使用 using
- `src/CallCenter.AgentHost/CallCenterService.Intent.cs` — 删除 3 个未使用 using
- `src/CallCenter.AgentHost/CallCenterService.Interaction.cs` — 删除 3 个未使用 using
- `src/CallCenter.AgentHost/CallCenterService.Routing.cs` — 删除 4 个未使用 using
- `src/CallCenter.AgentHost/EntryPoint.cs` — 删除 2 个未使用 using
- `src/CallCenter.AgentHost/Extensions.cs` — 删除 2 个未使用 using
- `src/CallCenter.AgentHost/IntentRegistry.cs` — 删除 1 个未使用 using

## Decisions Made

- 删除整个 ServiceCollectionExtensions.cs 文件而非仅删除方法体 — 该文件唯一内容就是 AddCallCenter 方法
- 保留保守策略：不确定的 using 保留，不删除通过间接引用可能使用的命名空间

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None — this plan only removes code, no new stubs introduced.

## Threat Flags

None — no new security-relevant surface introduced.

## Next Phase Readiness

- Plan 12-02 (E2E verification) 可正常执行
- 代码库更干净，后续维护成本降低

---
*Phase: 12-cleanup-and-verification*
*Completed: 2026-06-04*
