# Requirements: CallCenter AI

**Defined:** 2026-06-01
**Core Value:** 用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作

## v1.1 Requirements

### Intent Switching

- [ ] **IR-04**: 用户中途切换意图（退款中改换货）→ 终止旧流程 → 清除 activeWorkflow → 启动新流程
- [ ] **IR-05**: 用户回复不在预期范围（如确认时回复"我要投诉"）→ 当前流程挂起 → 重新意图识别

### Agent Pipeline

- [ ] **FW-05**: Agent Pipeline 6 层管道（SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput）
- [ ] **FW-07**: Compaction 扩展方法（8000 token 阈值，保留 8 轮，小模型摘要）
- [ ] **FW-08**: Audit Logger 自动捕获 Workflow Step 输入/输出
- [ ] **FW-09**: Saga 补偿（失败补偿 + 重试策略 1min/5min/30min）

### Console Demo

- [ ] **CD-04**: 30 分钟超时提示（Gateway 检测最后活跃时间 → 提醒 → 再 30 分钟终止）

### Business Extensibility

- [ ] **BE-01**: 新增业务模块 7 步流程（复制 → 重命名 → 修改 Workflow → 修改 Executors → 新增 Skill → 注册 → 完成）

## v2 Requirements

### Multi-Business Modules

- **WF-10**: Exchange Workflow（换货流程）
- **WF-11**: Logistics Workflow（物流查询）
- **SK-10**: ExchangeSkill / LogisticsSkill

### Observability

- **OB-01**: OpenTelemetry 集成
- **OB-02**: Audit Trail 防篡改审计存储
- **OB-03**: Langfuse 集成

### Session Persistence

- **SP-01**: RedisSessionStore 三合一实现（聊天历史 + Session 序列化 + Checkpoint）
- **SP-02**: TTL 自动过期（30/90 天）
- **SP-03**: 分布式多实例共享 Redis

### Safety

- **SF-01**: KeywordFilter 关键词拦截
- **SF-02**: PromptInjectionDetector 注入检测
- **SF-03**: SafetyOutputFilter 输出过滤（高风险转人工）

## Out of Scope

| Feature | Reason |
|---------|--------|
| Web/Gateway 接入层 | Demo 阶段用控制台替代 |
| 真实 MCP Server 调用 | Mock 服务验证流程，接口签名一致 |
| Knowledge Layer (FAQ/RAG) | 旁路系统，后续实现 |
| Human Agent Layer | 人工客服接管，后续实现 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| IR-04 | Phase 5 | Complete |
| IR-05 | Phase 5 | Complete |
| CD-04 | Phase 5 | Complete |
| FW-05 | Phase 6 | Complete |
| FW-07 | Phase 6 | Complete |
| FW-08 | Phase 7 | Pending |
| FW-09 | Phase 7 | Pending |
| BE-01 | Phase 8 | Pending |

**Coverage:**
- v1.1 requirements: 8 total
- Mapped to phases: 8
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-01*
*Last updated: 2026-06-01 after v1.1 milestone definition*
