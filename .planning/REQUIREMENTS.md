# Requirements: CallCenter AI

**Defined:** 2026-06-04
**Core Value:** 用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作

## v3.0 Requirements ✅ Complete

### Web API 接入

- [x] **WA-01**: 新增 CallCenter.WebApi 项目（ASP.NET Core Minimal API，.NET 10.0）— v3.0
- [x] **WA-02**: POST /chat 端点，接收 {message, sessionId?} 返回 JSON 响应 — v3.0
- [x] **WA-03**: SSE 端点复用 CallCenterService.ProcessStreamingAsync，将工作流中间输出通过 SSE 事件推送 — v3.0
- [x] **WA-04**: 会话管理（自动生成 sessionId、恢复已有会话、60 分钟惰性清理）— v3.0
- [x] **WA-05**: CORS 配置，默认允许所有来源（开发阶段）— v3.0

### Safety Pipeline 实现

- [x] **SI-01**: PII 脱敏 — 识别并脱敏邮箱/手机号/身份证号码 — v3.0
- [x] **SI-02**: 关键词黑名单 — 拦截包含禁用词的输入 — v3.0
- [x] **SI-03**: Prompt injection 检测 — 识别系统提示注入攻击 — v3.0
- [x] **SI-04**: KeywordFilter 配置化 — 从 appsettings.json 读取 — v3.0
- [x] **SO-01**: SafetyOutput 敏感内容拦截 — 3 类关键词过滤（暴力/色情/政治）— v3.0

### Exchange Workflow 骨架

- [x] **EX-01**: ExchangeWorkflow 骨架保留（编译通过）— v3.0

### v3.0 Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| WA-01 | Phase 13 | Complete |
| WA-02 | Phase 13 | Complete |
| WA-03 | Phase 14 | Complete |
| WA-04 | Phase 14 | Complete |
| WA-05 | Phase 13 | Complete |
| SI-01 | Phase 15 | Complete |
| SI-02 | Phase 15 | Complete |
| SI-03 | Phase 15 | Complete |
| SI-04 | Phase 15 | Complete |
| SO-01 | Phase 16 | Complete |
| EX-01 | Phase 16 | Complete |

**v3.0 Coverage: 11/11 complete ✓**

## v4.0 Requirements

Requirements for v4.0 milestone: Session persistence + production readiness.

### Session Store

- [ ] **SS-01**: 创建 ISessionStore 接口，定义 5 个方法（GetAsync/SetAsync/RemoveAsync/GetKeysAsync/ClearScopeAsync），SetAsync 支持可选 TTL 参数
- [ ] **SS-02**: InMemorySessionStore 实现 ISessionStore，保持现有 ConcurrentDictionary 行为，TTL 参数忽略
- [ ] **SS-03**: RedisSessionStore 完整实现，使用 StackExchange.Redis，支持 JSON 序列化、TTL 过期、scope:key 命名格式
- [ ] **SS-04**: DI 扩展方法 `AddSessionStore(IConfiguration)`，读取 `appsettings.json` 中 `"SessionStore:Provider"` 决定注册哪个实现
- [ ] **SS-05**: 所有消费者（CallCenterService、Extensions.cs 等）改用 `ISessionStore` 接口依赖，不再依赖具体实现
- [ ] **SS-06**: appsettings.json 新增 SessionStore 配置段，支持 Provider、ConnectionString、DefaultTtlMinutes
- [ ] **SS-07**: 全解决方案编译通过，内存模式现有功能不变，Redis 模式可配置连接并读写

### Out of Scope (v4.0)

| Feature | Reason |
|---------|--------|
| JWT 认证 / API Key | v4.x 后期 |
| Exchange Workflow 完整实现 | v4.x 业务模块 |
| ToolApproval 具体规则 | v4.x 审批规则 |
| 真实 MCP Server 调用 | 仍使用 Mock 服务 |

### v4.0 Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SS-01 | Phase 17 | Pending |
| SS-02 | Phase 17 | Pending |
| SS-03 | Phase 17 | Pending |
| SS-04 | Phase 17 | Pending |
| SS-05 | Phase 17 | Pending |
| SS-06 | Phase 17 | Pending |
| SS-07 | Phase 17 | Pending |

**v4.0 Coverage:**
- v4.0 requirements: 7 total
- Mapped to phases: 7
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-04*
*Last updated: 2026-06-05 after v4.0 milestone start*
