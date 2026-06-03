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

### Top Lessons (Verified Across Milestones)

1. **Plan checker before execution catches real issues** — sessionId gap (v2.0) saved a compile-fix cycle
2. **Wave-sequential execution on shared files avoids merge conflicts** — partial class files written in order (Core→Intent/Routing→Execution/Interaction)
3. **Static verification catches code-level issues but runtime tests are still needed** — 5 behavior items deferred at v2.0 close
