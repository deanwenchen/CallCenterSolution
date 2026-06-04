# Phase 12 Plan 02: End-to-End Verification Results

**Date:** 2026-06-04
**Purpose:** Verify refactored CallCenterService behavior matches pre-refactor behavior.

## Task 1: Build + Source Assertions

### Build Result

| Metric | Expected | Actual | Status |
|--------|----------|--------|--------|
| Errors | 0 | 0 | PASS |
| Warnings | 0 (pre-existing: 1 CS8602) | 1 CS8602 | PASS (pre-existing) |

### Source Assertions

| Assertion | Expected | Actual | Status |
|-----------|----------|--------|--------|
| Program.cs lines | ≤ 35 | 18 | PASS |
| Program.cs no RunWorkflow/ResumeWorkflow/HandleRequestAsync | 0 matches | 0 | PASS |
| Program.cs contains `new CallCenterService()` | Yes | Yes (line 5) | PASS |
| Program.cs contains `svc.ProcessAsync(sessionId, userMessage)` | Yes | Yes (line 16) | PASS |
| Core.cs contains `Subscribe<RefundCompletedEvent>` (2 constructors) | 2 | 2 | PASS |
| Execution.cs contains `DriveLoopAsync` + `HandleEventAsync` | Present | Present (10 refs) | PASS |
| Execution.cs 9 event types | 9 | 9 | PASS |

## Task 2: E2E Smoke Test (4 Scenarios)

**Status:** Requires human verification — interactive console demo with LLM API calls cannot be reliably automated.

### Pre-execution Fix (Rule 1 - Bug)

**Issue:** `JsonlLogger` not registered in DI container. `Extensions.cs` `AddCallCenter()` was missing the `JsonlLogger` registration that existed in the old `ServiceCollectionExtensions.cs`.
**Fix:** Added `services.AddSingleton<CallCenter.Framework.Logging.JsonlLogger>()` to `Extensions.cs`.
**Result:** Build passes, application starts successfully (verified by successful startup with greeting output).

### Automated Testing Limitation

The console demo has two concurrent stdin readers:
1. Main loop: `Console.ReadLine()` (synchronous)
2. Background task: `Console.In.ReadLineAsync()` (async, feeds `_inputChannel`)

Piped input races between these two readers, making automated E2E testing unreliable. Manual interactive testing is required.

### T1: Business Intent (Refund)
- **Input:** "我要退款，订单A001"
- **Expected:** 显示订单信息 → 确认退款 → 显示退款结果 → 显示 [EVENT] 退款完成
- **Actual:** _requires human verification_

### T2: Rule Reject
- **Input:** "我要退款，订单A002"
- **Expected:** 返回规则拒绝（定制商品不可退款）
- **Actual:** _requires human verification_

### T3: Non-business Intent
- **Input:** "你好"
- **Expected:** 返回问候语，不启动工作流
- **Actual:** _requires human verification_

### T4: Missing Parameter Follow-up
- **Input:** "我要退款"
- **Expected:** 追问订单号 → 用户提供 → 继续流程
- **Actual:** _requires human verification_
