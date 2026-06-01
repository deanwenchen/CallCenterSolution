---
phase: 03-consoledemo-main-loop-event-handling
plan: 02
subsystem: ConsoleDemo
autonomous: true
type: execute
tags: [checkpoint, resume, error-recovery, event-handling]
dependency_graph:
  requires:
    - 03-01
  provides:
    - CheckpointManager integration in Program.cs
    - ResumeExisting branch with ResumeStreamingAsync
    - WorkflowErrorEvent/ExecutorFailedEvent error recovery
    - TimeoutResult and IntentSwitchResult display
    - HandleRequest edge cases (confirm/cancel/out-of-range)
  affects:
    - src/CallCenter.ConsoleDemo/Program.cs
tech_stack:
  added: []
  patterns:
    - CheckpointManager.Default for checkpoint storage
    - ResumeStreamingAsync for workflow resumption
    - SuperStepCompletedEvent checkpoint capture
    - SendResponseAsync for RequestPort responses
key_files:
  created: []
  modified:
    - src/CallCenter.ConsoleDemo/Program.cs (+132 lines, -13 lines)
decisions:
  - D-01: Auto-resume from checkpoint without user confirmation
  - D-02: User input directly becomes RequestPort response
  - D-03: Intent switch clears activeWorkflow immediately
  - D-07: Error recovery prints executor+reason, clears activeWorkflow
  - D-09: Program does not exit on error
metrics:
  duration: 12 minutes
  completed_date: "2026-06-01"
  tasks: 2
  commits: 1
---

# Phase 03 Plan 02: ConsoleDemo Main Loop Integration Summary

One-liner: Integrated all components in Program.cs with CheckpointManager, implemented checkpoint-based resume with ResumeStreamingAsync, fixed error recovery to clear activeWorkflow on errors, wired timeout/intent switch display, and handled RequestInfoEvent edge cases including confirm/cancel/out-of-range replies.

## What Was Built

### Task 1: ResumeExisting with CheckpointManager
- Added `CheckpointManager.Default` initialization at top of Program.cs
- Implemented `ResumeWorkflow` method that:
  - Loads checkpoint from sessionStore using key "lastCheckpoint"
  - If no checkpoint found, prints message and clears activeWorkflow
  - Calls `InProcessExecution.ResumeStreamingAsync(workflow, checkpoint, checkpointManager)`
  - Processes event stream with WatchStreamAsync
  - Per D-01/D-02: User input during resume goes directly to pending RequestPort via SendResponseAsync
- Modified `RunWorkflow` to save checkpoints from SuperStepCompletedEvent to sessionStore
- Updated main switch statement to handle ResumeExistingResult by calling ResumeWorkflow

### Task 2: Error Recovery + Timeout Display + HandleRequest Edge Cases
- Fixed WorkflowErrorEvent handler to:
  - Print error with executor name and reason
  - Clear activeWorkflow via `sessionStore.RemoveAsync("activeWorkflow", sessionId)`
  - Not exit program (per D-09)
- Fixed ExecutorFailedEvent handler with same pattern
- Added TimeoutResult case in main switch:
  - IsWarning=true: prints warning message, allows continuation
  - IsWarning=false: prints terminate message
- Added IntentSwitchResult case:
  - Prints termination message with old/new workflow names
  - Phase 3 limitation: only refund supported, prints "暂未实现" for other intents
- Enhanced HandleRequest for ConfirmRefundRequest:
  - "确认" -> UserConfirmation(true)
  - "取消" -> UserConfirmation(false) with "[系统] 已取消退款" message
  - Out-of-range reply -> UserConfirmation(false) with "[系统] 未识别回复 '{reply}'，视为取消" message
- Added debug output for NeedOrderId: "[DEBUG] 收到订单号: {orderId}"

## Deviations from Plan

**Task Consolidation:** Tasks 1 and 2 were implemented together in a single edit/commit because they both modify the same file (Program.cs) and are interdependent. The checkpoint-based resume (Task 1) requires proper error handling (Task 2) to be complete.

## Verification Results

| Check | Status | Details |
|-------|--------|---------|
| ConsoleDemo project builds | PASS | 0 errors, 4 warnings (NU1510 - harmless) |
| CheckpointManager references | PASS | 15 occurrences in Program.cs |
| ResumeStreamingAsync usage | PASS | Line 166: `InProcessExecution.ResumeStreamingAsync(...)` |
| lastCheckpoint storage | PASS | Lines 115-116, 125-126, 185-186, 195-196 |
| activeWorkflow cleanup | PASS | 7 occurrences of `RemoveAsync.*activeWorkflow` |
| TimeoutResult handling | PASS | Lines 67-77: switch case with IsWarning check |
| IntentSwitchResult handling | PASS | Lines 79-82: prints termination message |
| HandleRequest confirm/cancel | PASS | Lines 244-252: distinguishes "确认", "取消", out-of-range |

## Commits

| Hash | Message |
|------|---------|
| 632b3a1 | feat(03-02): implement ResumeExisting with CheckpointManager and checkpoint-aware workflow execution |

## Files Modified

| File | Changes |
|------|---------|
| src/CallCenter.ConsoleDemo/Program.cs | +132 lines, -13 lines |

## Implementation Notes

- Checkpoint storage: InMemorySessionStore with key "lastCheckpoint"
- Resume pattern: Uses ResumeStreamingAsync (new run from checkpoint) matching CheckpointAndRehydrate sample
- Error handling: Both WorkflowErrorEvent and ExecutorFailedEvent clear activeWorkflow and return to main loop
- Timeout display: Warning shows "您可以继续对话，或开始新的流程", terminate just shows message
- Intent switch: Old workflow terminated immediately per D-03, new intent starts fresh (only refund supported in Phase 3)
- HandleRequest: Out-of-range replies treated as cancellation (UserConfirmation(false)) with informative message

## Known Stubs

None - all functionality fully implemented.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| None | - | No new security-relevant surface introduced |

---

*Phase: 03-consoledemo-main-loop-event-handling*
*Plan: 02*
*Completed: 2026-06-01*
