# Phase 13: Web API 基础 — 新项目搭建 + /chat 端点 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 13-Web API 基础 — 新项目搭建 + /chat 端点
**Areas discussed:** 配置文件策略, 响应模式

---

## 配置文件策略

| Option | Description | Selected |
|--------|-------------|----------|
| 独立 appsettings.json | 新建 appsettings.json 包含 DashScope Key、CORS 配置等 | |
| 环境变量优先 (推荐) | 环境变量（DASHSCOPE_API_KEY），appsettings.json 只放 Web 特定配置 | ✓ |
| 共享配置 + 覆盖 | WebApi 有自己的 appsettings.json，通过 Include 共享基础配置 | |

**User's choice:** 环境变量优先
**Notes:** 与现有 AgentHost 保持一致，API Key 通过环境变量传入，避免两处维护相同值。

## 响应模式

| Option | Description | Selected |
|--------|-------------|----------|
| 纯阻塞，不预留 (推荐) | POST /chat → ProcessAsync → 返回 JSON，Phase 14 新增 /chat/stream | ✓ |
| 预留 SSE 端点签名 | 现在就定义 /chat/stream 端点，内部先返回阻塞结果 | |

**User's choice:** 纯阻塞，不预留
**Notes:** 符合 CLAUDE.md 的 Simplicity First 原则，Phase 13 改动最小。

---

## Claude's Discretion

- Swagger 默认启用，开发阶段无认证
- CORS 默认允许所有来源（AllowAnyOrigin）
- 项目命名遵循现有模式：CallCenter.WebApi
- 端口号由 planner 决定（建议 5000+ 避免与 ConsoleDemo 冲突）

## Deferred Ideas

None — discussion stayed within phase scope.
