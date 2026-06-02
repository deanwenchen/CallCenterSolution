---
phase: 04-verification-edge-cases
plan: 01
subsystem: Refund Workflow
tags:
  - refund
  - cancel-path
  - csproj-cleanup
requires: []
provides:
  - "ExecuteRefundExecutor cancel path (YieldOutputAsync-based)"
  - "NU1510 warnings eliminated (4 → 0)"
affects:
  - src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs
  - src/CallCenter.Workflows/Refund/RefundMessages.cs
  - src/CallCenter.Workflows/Refund/Executors/RestoreCouponExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/SendNotificationExecutor.cs
  - src/CallCenter.Framework/CallCenter.Framework.csproj
  - src/CallCenter.Shared/CallCenter.Shared.csproj
tech-stack:
  added: []
  patterns:
    - "YieldOutputAsync for terminal output"
    - "Nullable record property for dual-path return type"
key-files:
  created: []
  modified:
    - "src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs"
    - "src/CallCenter.Workflows/Refund/RefundMessages.cs"
    - "src/CallCenter.Workflows/Refund/Executors/RestoreCouponExecutor.cs"
    - "src/CallCenter.Workflows/Refund/Executors/SendNotificationExecutor.cs"
    - "src/CallCenter.Framework/CallCenter.Framework.csproj"
    - "src/CallCenter.Shared/CallCenter.Shared.csproj"
decisions:
  - "RefundExecuted.Result made nullable (RefundResult?) to support cancel path without new record type"
  - "RestoreCouponExecutor returns CouponRestored(null) when Result is null — no coupon restoration on cancellation"
  - "SendNotificationExecutor checks refundResult == null to skip success event and output generic completion message"
metrics:
  duration: "~5 minutes"
  completed: "2026-06-01"
---

# Phase 04 Plan 01: Cancel Path Fix + NU1510 Cleanup Summary

## One-liner

Fixed ExecuteRefundExecutor cancel path to use YieldOutputAsync for "退款已取消" output instead of sending RefundSignal.Cancelled + throwing exception, and removed redundant System.Text.Json PackageReference from Framework and Shared projects (eliminating 4 NU1510 warnings).

## Tasks Executed

| Task | Name | Commit | Status |
| ---- | ---- | ------ | ------ |
| 1 | Fix ExecuteRefundExecutor cancel path | e574ab6 | Complete |
| 2 | Remove redundant System.Text.Json | 2011bb6 | Complete |

## Task 1: Fix ExecuteRefundExecutor cancel path

**Problem:** When `Confirmed=false`, the executor sent `RefundSignal.Cancelled` + threw `InvalidOperationException`. Per D-69/D-70 decisions, this was incorrect — user cancellation is not an error and should not send a signal back to the workflow.

**Changes made:**

| File | Change |
|------|--------|
| `ExecuteRefundExecutor.cs` | Replaced `SendMessageAsync(RefundSignal.Cancelled)` + `throw` with `YieldOutputAsync(new RefundNotification("退款已取消"))` + `return new RefundExecuted(null)` |
| `RefundMessages.cs` | Made `RefundExecuted.Result` nullable (`RefundResult?`) to allow cancel path to return null |
| `RestoreCouponExecutor.cs` | Added null check: when `Result == null`, skip `RestoreCouponAsync` and return `CouponRestored(null)` |
| `SendNotificationExecutor.cs` | Added null check: when `refundResult == null`, skip `RefundCompletedEvent` publish and output generic completion message |

**Verification:**
- `RefundSignal.Cancelled` removed from ExecuteRefundExecutor.cs (0 matches)
- `YieldOutputAsync` present in ExecuteRefundExecutor.cs (line 25)
- No exception thrown on cancel path
- Full solution build: 0 errors, 0 warnings

## Task 2: Remove redundant System.Text.Json PackageReference

**Problem:** .NET 10 SDK has System.Text.Json built-in. Explicit `<PackageReference>` caused NU1510 warnings in Framework and Shared projects (4 total warnings across dependency graph).

**Changes made:**

| File | Change |
|------|--------|
| `CallCenter.Framework.csproj` | Removed `<PackageReference Include="System.Text.Json" />` |
| `CallCenter.Shared.csproj` | Removed `<PackageReference Include="System.Text.Json" />` |

**Verification:**
- Framework.csproj: 0 references to System.Text.Json
- Shared.csproj: 0 references to System.Text.Json
- Full solution build: 0 warnings (down from 4), 0 errors
- All `using System.Text.Json;` statements remain valid (SDK provides them)

## Deviations from Plan

### Auto-added missing functionality (Rule 2)

**RestoreCouponExecutor null handling:** The plan only mentioned ExecuteRefundExecutor.cs, but since `RefundExecuted` is consumed by RestoreCouponExecutor, adding null handling was required for correctness. Without it, `RestoreCouponAsync` would be called even on cancelled refunds, incorrectly restoring coupons when no refund occurred.

**SendNotificationExecutor null handling:** Similarly, SendNotificationExecutor reads `refundResult` from state. On the cancel path, this state is null (ExecuteRefundExecutor no longer calls `QueueStateUpdatesAsync("refundResult", ...)` on cancellation). Without null handling, the executor would publish a `RefundCompletedEvent` with `"unknown"` order ID and `$0` amount, which is incorrect behavior.

## Known Stubs

None. All code paths are fully wired.

## Threat Flags

None. No new network endpoints, auth paths, or trust boundary changes introduced.

## Self-Check

- [x] ExecuteRefundExecutor.cs exists and compiles
- [x] RefundMessages.cs exists and compiles
- [x] RestoreCouponExecutor.cs exists and compiles
- [x] SendNotificationExecutor.cs exists and compiles
- [x] Framework.csproj exists without System.Text.Json
- [x] Shared.csproj exists without System.Text.Json
- [x] Commit e574ab6 exists (Task 1)
- [x] Commit 2011bb6 exists (Task 2)
- [x] Full solution build: 0 errors, 0 warnings

## Self-Check: PASSED
