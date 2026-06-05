# Roadmap: CallCenter AI

## Milestones

- ✅ **v1.0 Refund Workflow Demo** — Phases 1-4 (shipped 2026-06-01)
- ✅ **v1.1 Technical Debt Closure** — Phases 5-8 (shipped 2026-06-01)
- ✅ **v2.0 Framework 提取** — Phases 9-10 (shipped 2026-06-03)
- ✅ **v2.1 Execution & Cleanup** — Phases 11-12 (shipped 2026-06-04)
- ◆ **v3.0 Web API + Safety 增强** — Phases 13-16 (defining)

## Phases

<details>
<summary>✅ v1.0 Refund Workflow Demo (Phases 1-4) — SHIPPED 2026-06-01</summary>

- [x] Phase 1: Foundation (6/6 plans) — completed 2026-06-01
- [x] Phase 2: AgentHost + Intent Router (3/3 plans) — completed 2026-06-01
- [x] Phase 3: ConsoleDemo — Main Loop + Event Handling (2/2 plans) — completed 2026-06-01
- [x] Phase 4: Verification + Edge Cases (2/2 plans) — completed 2026-06-01

</details>

<details>
<summary>✅ v1.1 Technical Debt Closure (Phases 5-8) — SHIPPED 2026-06-01</summary>

- [x] Phase 5: Intent Switching + Timeout (1/1 plans) — completed 2026-06-01
- [x] Phase 6: Agent Pipeline + Compaction (2/2 plans) — completed 2026-06-01
- [x] Phase 7: Audit Logger + Saga Compensation (2/2 plans) — completed 2026-06-01
- [x] Phase 8: Business Extensibility Guide (2/2 plans) — completed 2026-06-01

</details>

<details>
<summary>✅ v2.0 Framework 提取 (Phases 9-10) — SHIPPED 2026-06-03</summary>

- [x] Phase 9: 基础配置与工厂 (3/3 plans) — completed 2026-06-03
- [x] Phase 10: CallCenterService 骨架 (3/3 plans) — completed 2026-06-03

</details>

<details>
<summary>✅ v2.1 Execution & Cleanup (Phases 11-12) — SHIPPED 2026-06-04</summary>

- [x] Phase 11: 执行层与入口 (1/1 plans) — completed 2026-06-03
- [x] Phase 12: 清理与验证 (2/2 plans) — completed 2026-06-04

</details>

<details>
<summary>◆ v3.0 Web API + Safety 增强 (Phases 13-16) — ACTIVE</summary>

### Phase 13: Web API 基础 — 新项目搭建 + /chat 端点

**Goal:** 新增 CallCenter.WebApi 项目，实现基础 HTTP 聊天入口。

**Requirements:** WA-01, WA-02, WA-05

**Success Criteria:**
1. `dotnet run` 启动 Web API 项目，访问 Swagger UI
2. POST /chat {message: "我要退款，订单A001"} 返回阻塞式响应（先不流式）
3. CORS 配置允许前端跨域请求
4. ConsoleDemo 和 WebApi 可并行运行

**Plans:** 1/1 plans complete
- [x] 13-01-PLAN.md — CallCenter.WebApi 项目 + /chat 端点 + DI 注册

### Phase 14: SSE 流式 + 会话管理

**Goal:** 将 /chat 改为 SSE 流式输出，支持会话生命周期管理。

**Requirements:** WA-03, WA-04

**Success Criteria:**
1. POST /chat/stream 返回 SSE 事件流，用户可实时看到工作流中间输出
2. 自动 sessionId 生成，后续请求复用同一会话
3. 过期会话（60 分钟无活动）自动清理
4. 前端可用 EventSource 或 fetch + ReadableStream 消费

**Plans:** 1 plan
- [x] 14-01-PLAN.md — SSE 流式端点 + 会话管理

### Phase 15: Safety Pipeline 实现

**Goal:** 补齐 6 层 Pipeline 中 SafetyInput 层的实际拦截能力。

**Requirements:** SI-01, SI-02, SI-03, SI-04

**Success Criteria:**
1. 输入含邮箱/手机号 → PII 脱敏后传给 LLM
2. 输入含黑名单关键词 → 返回友好拦截消息，不调 LLM
3. 输入含 prompt injection 模式 → 返回安全警告
4. 关键词列表在 appsettings.json 可配置，不改代码

**Plans:** 1/1 plans complete
- [x] 15-01-PLAN.md — PII 脱敏 + 关键词黑名单 + Prompt injection 检测 + 配置化

### Phase 16: SafetyOutput + Exchange 骨架确认

**Goal:** 实现 LLM 输出端敏感内容拦截，确认 Exchange 骨架就绪。

**Requirements:** SO-01, EX-01

**Success Criteria:**
1. LLM 输出含敏感内容 → 替换为安全消息返回
2. ExchangeWorkflow 编译通过，骨架文件齐全
3. 全解决方案 0 错误 0 警告编译

**Plans:** 1/1 plans complete
- [x] 16-01-PLAN.md — OutputContentFilter 按类关键词拦截 + SafetyOutputDelegatingClient 话术返回 + Exchange 骨架编译确认

</details>

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Foundation | v1.0 | 6/6 | Complete | 2026-06-01 |
| 2. AgentHost + Intent Router | v1.0 | 3/3 | Complete | 2026-06-01 |
| 3. ConsoleDemo | v1.0 | 2/2 | Complete | 2026-06-01 |
| 4. Verification + Edge Cases | v1.0 | 2/2 | Complete | 2026-06-01 |
| 5. Intent Switching + Timeout | v1.1 | 1/1 | Complete | 2026-06-01 |
| 6. Agent Pipeline + Compaction | v1.1 | 2/2 | Complete | 2026-06-01 |
| 7. Audit Logger + Saga Compensation | v1.1 | 2/2 | Complete | 2026-06-01 |
| 8. Business Extensibility Guide | v1.1 | 2/2 | Complete | 2026-06-01 |
| 9. 基础配置与工厂 | v2.0 | 3/3 | Complete | 2026-06-03 |
| 10. CallCenterService 骨架 | v2.0 | 3/3 | Complete | 2026-06-03 |
| 11. 执行层与入口 | v2.1 | 1/1 | Complete | 2026-06-03 |
| 12. 清理与验证 | v2.1 | 2/2 | Complete | 2026-06-04 |
| 13. Web API 基础 | v3.0 | 1/1 | Complete    | 2026-06-04 |
| 14. SSE 流式 + 会话管理 | v3.0 | 1/1 | Complete | 2026-06-04 |
| 15. Safety Pipeline 实现 | v3.0 | 1/1 | Complete    | 2026-06-04 |
| 16. SafetyOutput + Exchange | v3.0 | 1/1 | Complete    | 2026-06-04 |

---

*Roadmap updated: 2026-06-04 after v3.0 roadmap creation*
