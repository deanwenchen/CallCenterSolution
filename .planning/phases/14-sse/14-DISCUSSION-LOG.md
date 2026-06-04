# Phase 14: SSE 流式 + 会话管理 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 14-sse
**Areas discussed:** 端点策略, SSE格式, 服务层适配, 返回类型, 推送范围, 会话超时清理, 前端消费方式

---

## 端点策略

| Option | Description | Selected |
|--------|-------------|----------|
| 保留 /chat + 新增 /chat/stream | ROADMAP 写的是 /chat/stream，原有 /chat 保留作为阻塞式回退 | ✓ |
| 替换 /chat 为 SSE | 不再需要阻塞式响应，直接替换 | |

**User's choice:** 保留 /chat + 新增 /chat/stream
**Notes:** 用户明确选择保留两个端点并存

## SSE 格式

| Option | Description | Selected |
|--------|-------------|----------|
| 原始 JSON | 每个 WorkflowEvent 序列化后作为独立 SSE event: `data: {"type":...}\n\n` | ✓ |
| 统一包装信封 | 统一包装: `data: {eventType, payload, timestamp, sessionId}` | |

**User's choice:** 原始 JSON
**Notes:** 直接推送原始数据，前端自行解析

## 推送范围

| Option | Description | Selected |
|--------|-------------|----------|
| 全量推送 | 全量推送所有 9 种事件，前端自行过滤 | ✓ |
| 只推用户可见事件 | 只推送 WorkflowOutput 和 RequestInfo 两种 | |

**User's choice:** 全量推送
**Notes:** 不筛选，所有 WorkflowEvent 都推送

## 返回类型

| Option | Description | Selected |
|--------|-------------|----------|
| IAsyncEnumerable\<string\> | 直接返回 SSE 格式字符串，最简单 | ✓ |
| IAsyncEnumerable\<object\> | 返回结构化对象，在端点层序列化 | |

**User's choice:** IAsyncEnumerable\<string\>
**Notes:** 最简单的方案，直接 yield return SSE 格式字符串

## 会话超时清理

**User's choice:** Claude 给建议
**Recommendation:** 惰性清理 — 请求时检查 lastActivity，超过 60 分钟调用 ClearScopeAsync。不设后台定时器。
**User accepted recommendation.**
**Notes:** demo 阶段内存占用不是问题，惰性清理零开销

## 前端消费方式

**User's choice:** Claude 给建议
**Recommendation:** fetch POST + ReadableStream — 支持 POST JSON body，未来可扩展 header（认证等），比 EventSource 更灵活。
**User accepted recommendation.**
**Notes:** 保持与现有 /chat 端点一致的 POST 风格

## Claude's Discretion

以下由 Claude 自行决定：
- CORS 沿用 Phase 13 的 AllowAll 策略
- SSE 端点开发阶段无认证
- sessionId 生成逻辑与 /chat 一致（不传则 Guid.NewGuid()）
- ProcessStreamingAsync 放在新的 partial class 文件（CallCenterService.Streaming.cs）

## Deferred Ideas

None — all discussion within phase scope.
