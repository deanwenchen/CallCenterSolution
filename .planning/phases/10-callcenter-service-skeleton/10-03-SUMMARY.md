---
phase: 10-callcenter-service-skeleton
plan: 03
subsystem: CallCenter.AgentHost
tags: [skeleton, workflow-execution, event-handling, user-interaction, saga-compensation, partial-class]

requires: []
provides:
  - "CallCenterService.Execution.cs — RunWorkflowAsync, ResumeWorkflowAsync, DriveLoopAsync, HandleEventAsync (9 event types), ExecutionContext, EventResult enum"
  - "CallCenterService.Interaction.cs — HandleRequestAsync (RefundSignal + ConfirmRefundRequest + unknown type NotSupportedException)"
affects:
  - "src/CallCenter.AgentHost/CallCenterService.Intent.cs (removed RunWorkflowAsync/ResumeWorkflowAsync placeholders)"

tech-stack:
  added: []
  patterns:
    - "ExecutionContext class for mutable shared state across async methods (C# async ref constraint workaround)"
    - "DriveLoopAsync shared event loop called by both RunWorkflowAsync and ResumeWorkflowAsync"
    - "HandleEventAsync switch on 9 WorkflowEvent types with audit logging and session store operations"
    - "Saga compensation in HandleEventAsync for ExecuteRefund failures (SagaBuilder fluent API)"
    - "isResumeMode flag to differentiate Run vs Resume RequestInfoEvent handling"

key-files:
  created:
    - path: src/CallCenter.AgentHost/CallCenterService.Execution.cs
      purpose: "Workflow execution partial — RunWorkflowAsync (new workflow), ResumeWorkflowAsync (from checkpoint), DriveLoopAsync (shared event loop), HandleEventAsync (9 event types), ExecutionContext (mutable state container), EventResult enum"
    - path: src/CallCenter.AgentHost/CallCenterService.Interaction.cs
      purpose: "User interaction partial — HandleRequestAsync for RefundSignal.NeedOrderId (orderId collection), ConfirmRefundRequest (confirm/cancel with intent re-recognition per IR-05), unknown type NotSupportedException"
  modified:
    - path: src/CallCenter.AgentHost/CallCenterService.Intent.cs
      purpose: "Removed RunWorkflowAsync/ResumeWorkflowAsync placeholder methods (moved to Execution.cs), added ConfigureAwait(false) to async calls"

decisions:
  - "Used ExecutionContext class instead of ref parameters — C# async methods cannot have ref/in/out parameters (CS1988). The sealed class holds LastCheckpoint, NeedsOrderId, and CurrentMessage as mutable fields shared across DriveLoopAsync and HandleEventAsync"
  - "WorkflowWarningEvent stores message in Data property (inherited from WorkflowEvent), not as Message — unlike the plan's initial pseudocode, fixed via deviation Rule 1"
  - "Saga compensation triggered when executorId == 'ExecuteRefund' without requiring separate restoreCoupon reference — the SagaBuilder compensation logic is self-contained in Execution.cs (per D-10-02)"
  - "RunWorkflowAsync returns empty string when workflow completes without output data — matches ProcessAsync contract"
  - "ResumeWorkflowAsync returns empty string after event stream ends — checkpoint-based resume doesn't have explicit return value from Program.cs equivalent"

metrics:
  duration_seconds: ~120
  completed_date: "2026-06-03T00:00:00Z"
  tasks_completed: 2
  files_created: 2
  files_modified: 1
  build_errors: 0
  build_warnings: 1
---

# Phase 10 Plan 03: CallCenterService Execution and Interaction Summary

**One-liner:** Created CallCenterService.Execution.cs (workflow event loop with 9 event types, Saga compensation, shared DriveLoopAsync) and CallCenterService.Interaction.cs (HandleRequestAsync for RefundSignal and ConfirmRefundRequest), replacing Intent.cs placeholders with full implementations.

## Tasks Completed

### Task 1: 创建 CallCenterService.Execution.cs — RunWorkflowAsync, ResumeWorkflowAsync, DriveLoopAsync, HandleEventAsync

**Commit:** `1149dd4`
**Files:** `src/CallCenter.AgentHost/CallCenterService.Execution.cs`

**What was built:**
- `public partial class CallCenterService` workflow execution methods in `CallCenter.AgentHost` namespace
- **EventResult enum:** `Continue`, `Return`, `Retry` — internal control flow for event loop
- **ExecutionContext sealed class:** `LastCheckpoint`, `NeedsOrderId`, `CurrentMessage` — mutable shared state (replaces ref parameters due to C# async constraint)
- **RunWorkflowAsync:** Replaces Program.cs RunWorkflow (line 159-286). Initializes ExecutionContext, enters while(true) loop, creates StreamingRun via `InProcessExecution.RunStreamingAsync`, calls DriveLoopAsync. Handles needsOrderId retry loop (restart while loop when orderId collected).
- **ResumeWorkflowAsync:** Replaces Program.cs ResumeWorkflow (line 290-375). Reads checkpoint from session store, creates StreamingRun via `InProcessExecution.ResumeStreamingAsync`, calls DriveLoopAsync with isResumeMode=true. Returns error string if no checkpoint found.
- **DriveLoopAsync:** Shared event loop. Iterates `run.WatchStreamAsync()` events, calls HandleEventAsync for each, returns EventResult. Called by both RunWorkflowAsync and ResumeWorkflowAsync.
- **HandleEventAsync:** Handles 9 event types:
  1. **RequestInfoEvent:** Audit CaptureStepStart → RefundSignal.NeedOrderId (Run mode only) collects orderId via _inputChannel → else calls HandleRequestAsync → Audit CaptureStepEnd
  2. **WorkflowOutputEvent:** Print result → Audit CaptureStepEnd → save checkpoint → remove activeWorkflow → VerifyChainAsync → Return
  3. **SuperStepCompletedEvent:** Save checkpoint → Audit CaptureStepEnd → Continue
  4. **WorkflowErrorEvent:** Print error → Audit CaptureError → Saga compensation for ExecuteRefund → remove activeWorkflow → Return
  5. **ExecutorFailedEvent:** Print error → Audit CaptureError → remove activeWorkflow → Return
  6. **WorkflowStartedEvent:** Audit LogAsync → Continue
  7. **ExecutorInvokedEvent:** Audit LogAsync → Continue
  8. **ExecutorCompletedEvent:** Audit LogAsync → Continue
  9. **WorkflowWarningEvent:** Audit LogAsync → Continue
- **Saga compensation:** When WorkflowErrorEvent executorId == "ExecuteRefund", uses SagaBuilder with OnFailure + WithRetry to execute compensation (restore coupon). Catches SagaCompensationException.
- `#pragma warning disable MAAI001` at file head for experimental API

**Verification:**
- `dotnet build` — 0 errors, 1 warning (CS8602 nullable on ctx.CurrentMessage, safe due to initialization)
- `grep -c` for 9 event types returns 10 (some types referenced in both case and break)
- All method signatures include `string sessionId` parameter

### Task 2: 创建 CallCenterService.Interaction.cs — HandleRequestAsync 用户交互处理

**Commit:** `1149dd4`
**Files:** `src/CallCenter.AgentHost/CallCenterService.Interaction.cs`

**What was built:**
- `public partial class CallCenterService` user interaction methods in `CallCenter.AgentHost` namespace
- **HandleRequestAsync(ExternalRequest request, string sessionId, CancellationToken ct):**
  - **Branch 1 (RefundSignal.NeedOrderId):** Console prompt → read orderId from _inputChannel → store in session → return RefundIntent response
  - **Branch 2 (ConfirmRefundRequest):** Print order info → read reply → "确认" → UserConfirmation(true) / "取消" → UserConfirmation(false) / unrecognized → re-recognize intent via _recognizeIntent → greeting/new intent → UserConfirmation(false) + clear activeWorkflow
  - **Branch 3 (Unknown):** `throw new NotSupportedException($"Unknown request type: {request.PortInfo.PortId}")`
- `#pragma warning disable MAAI001` at file head for experimental API

**Verification:**
- `dotnet build` — 0 errors, 1 warning
- `grep -c "HandleRequestAsync(ExternalRequest request, string sessionId"` returns 1

### Intent.cs Cleanup

**Files:** `src/CallCenter.AgentHost/CallCenterService.Intent.cs`
- Removed `RunWorkflowAsync` and `ResumeWorkflowAsync` placeholder methods (NotImplementedException stubs)
- Updated ProcessAsync switch cases to use `.ConfigureAwait(false)` on async calls
- Both methods now resolved from Execution.cs (same partial class)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] C# async ref parameter constraint (CS1988)**
- **Found during:** Task 1 — initial build
- **Issue:** Plan specified `HandleEventAsync` and `DriveLoopAsync` with `ref CheckpointInfo?`, `ref bool`, `ref RefundIntent?` parameters. C# does not allow ref/in/out parameters on async methods.
- **Fix:** Created `ExecutionContext` sealed class to hold mutable state (`LastCheckpoint`, `NeedsOrderId`, `CurrentMessage`), passed by reference (class instance reference) instead of ref parameters. Behavior is identical.
- **Files modified:** `src/CallCenter.AgentHost/CallCenterService.Execution.cs`
- **Commit:** Same commit (fixed before committing)

**2. [Rule 1 - Bug] WorkflowWarningEvent uses Data property, not Message**
- **Found during:** Task 1 — initial build
- **Issue:** Plan pseudocode used `warningEvt.Message`, but `WorkflowWarningEvent` stores its message in the inherited `Data` property from `WorkflowEvent` (passed via base constructor).
- **Fix:** Changed to `warningEvt.Data?.ToString() ?? ""` for audit logging.
- **Files modified:** `src/CallCenter.AgentHost/CallCenterService.Execution.cs`
- **Commit:** Same commit (fixed before committing)

## Threat Surface Scan

| Flag | File | Description |
|------|------|-------------|
| threat_flag: workflow-execution | CallCenterService.Execution.cs | HandleEventAsync processes WorkflowErrorEvent with Saga compensation — only triggers for executorId == "ExecuteRefund" (per T-10-05 mitigation) |
| threat_flag: audit-logging | CallCenterService.Execution.cs | All 9 event types logged via AuditTrailMiddleware or _auditLogger.LogAsync — WorkflowOutputEvent calls VerifyChainAsync to validate chain integrity (per T-10-04 mitigation) |
| threat_flag: console-input | CallCenterService.Interaction.cs | Console.WriteLine of order details (orderId, productName, amount) — same trust boundary as Program.cs (per T-10-07 accept) |

## Key Decisions

1. **ExecutionContext class over ref parameters** — C# language constraint (CS1988) prevents ref parameters on async methods. A sealed class with public fields provides equivalent mutable shared state without language limitation. The class is private to CallCenterService, minimizing surface.
2. **Saga compensation self-contained** — Unlike Program.cs which received `restoreCoupon` as a parameter, Execution.cs embeds the compensation logic directly in HandleEventAsync. This matches D-10-02 design: "CallCenterService 自包含完整错误处理能力"。
3. **DriveLoopAsync as shared event loop** — Per design.md Decision 6, both RunWorkflowAsync and ResumeWorkflowAsync call DriveLoopAsync, eliminating ~50% code duplication present in Program.cs RunWorkflow/ResumeWorkflow.
4. **isResumeMode flag** — Differentiates RequestInfoEvent handling between Run (orderId retry loop) and Resume (simpler, no retry) modes.

## Self-Check: PASSED

All files and commits verified:
- Execution.cs exists
- Interaction.cs exists
- SUMMARY.md exists
- Commit 1149dd4 exists in git log
- dotnet build passes with 0 errors
