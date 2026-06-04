# Requirements: CallCenter AI v3.0

**Defined:** 2026-06-04
**Core Value:** 用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作

## v3.0 Requirements

### Web API 接入

- [ ] **WA-01**: 新增 CallCenter.WebApi 项目（ASP.NET Core Minimal API，.NET 10.0）
- [ ] **WA-02**: POST /chat 端点，接收 {message, sessionId?} 返回 SSE 流式响应
- [ ] **WA-03**: SSE 端点复用 CallCenterService.ProcessAsync，将工作流中间输出通过 SSE 事件推送
- [ ] **WA-04**: 会话管理（自动生成 sessionId、恢复已有会话、超时清理）
- [ ] **WA-05**: CORS 配置，默认允许所有来源（开发阶段）

### Safety Pipeline 实现

- [ ] **SI-01**: PII 脱敏 — 识别并脱敏邮箱/手机号/身份证号码（替换为 ***）
- [ ] **SI-02**: 关键词黑名单 — 拦截包含禁用词的输入，返回友好拒绝消息
- [ ] **SI-03**: Prompt injection 检测 — 识别系统提示注入攻击（如 "ignore previous instructions"），拦截可疑输入
- [ ] **SI-04**: KeywordFilter 配置化 — 从 appsettings.json 读取关键词列表和拦截规则，不硬编码
- [ ] **SO-01**: SafetyOutput 敏感内容拦截 — 检测 LLM 输出中的暴力/色情/政治等敏感内容，拦截不当输出

### Exchange Workflow 骨架

- [ ] **EX-01**: ExchangeWorkflow 骨架保留（已有 Workflow + 7 Executors + Messages + Skill，编译通过）

## Out of Scope

| Feature | Reason |
|---------|--------|
| JWT 认证 / API Key | v4 处理，v3.0 公开接口 |
| Exchange 完整业务逻辑 | v4 处理，WF-10 |
| Session 持久化（Redis）| InMemorySessionStore 替代 |
| ToolApproval 具体审批规则 | v4 处理 |
| 真实 MCP Server 调用 | Mock 服务替代 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| WA-01 | Phase 13 | Pending |
| WA-02 | Phase 13 | Pending |
| WA-03 | Phase 14 | Pending |
| WA-04 | Phase 14 | Pending |
| WA-05 | Phase 13 | Pending |
| SI-01 | Phase 15 | Pending |
| SI-02 | Phase 15 | Pending |
| SI-03 | Phase 15 | Pending |
| SI-04 | Phase 15 | Pending |
| SO-01 | Phase 16 | Pending |
| EX-01 | Phase 16 | Pending |

**Coverage:**
- v3.0 requirements: 11 total
- Mapped to phases: 11
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-04*
*Last updated: 2026-06-04 after initial definition*
