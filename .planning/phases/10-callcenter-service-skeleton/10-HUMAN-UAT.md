---
status: partial
phase: 10-callcenter-service-skeleton
source: [10-VERIFICATION.md]
started: 2026-06-04T03:40:00Z
updated: 2026-06-04T03:40:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. ProcessAsync Greeting Flow
expected: Call `ProcessAsync("session-1", "你好")` returns greeting response ("你好！有什么可以帮助你的？") without starting any workflow
result: [pending]

### 2. ProcessAsync Refund Workflow End-to-End
expected: Call `ProcessAsync("session-1", "我要退款")` starts RefundWorkflow, prompts for orderId, processes through all 6 steps, returns final result string
result: [pending]

### 3. Session Timeout Detection
expected: Set session lastActivity to 65 minutes ago, call `ProcessAsync("session-1", "test")` with active workflow → returns timeout termination message
result: [pending]

### 4. Saga Compensation on ExecuteRefund Failure
expected: Trigger WorkflowErrorEvent with executorId="ExecuteRefund" → Saga compensation runs, restores coupon, logs "[补偿] 补偿完成"
result: [pending]

### 5. Business Flow Equivalence (CS-04)
expected: Run same refund scenario through old Program.cs path and new CallCenterService path → identical behavior (event order, audit logs, Saga, checkpoint)
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps
