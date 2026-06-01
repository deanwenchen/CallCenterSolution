# Requirements: CallCenter AI

**Defined:** 2026-06-01
**Core Value:** 用户说出业务意图后，系统能自动识别、启动对应流程、在需要时追问缺失参数、最终完成业务操作

## v1 Requirements

### Workflow Execution

- [ ] **WF-01**: 退款流程支持 6 步执行：GetOrder → CheckRefundRule → WaitConfirm → ExecuteRefund → RestoreCoupon → SendNotification
- [ ] **WF-02**: 流程缺少参数时自动回 RequestPort 询问用户（动态追问机制）
- [ ] **WF-03**: 用户确认退款前暂停流程，展示订单信息和退款金额
- [ ] **WF-04**: 用户取消退款时流程终止并输出取消通知
- [ ] **WF-05**: 退款规则校验：7 天内 + 已签收 + 非特殊品类

### Intent Recognition

- [ ] **IR-01**: Entry Point 实现 LLM 意图识别（DashScope → StructuredOutputParser → IntentResult）
- [ ] **IR-02**: Entry Point 检查 activeWorkflow 决定 Resume 或新启动
- [ ] **IR-03**: 无意图消息走闲聊回复，不启动 Workflow

### MCP Client

- [ ] **MC-01**: IOrderMcpClient 接口定义（GetOrderAsync, GetRecentOrdersAsync）
- [ ] **MC-02**: IFinanceMcpClient 接口定义（RefundAsync）
- [ ] **MC-03**: IMemberMcpClient 接口定义（GetCouponAsync, RestoreCouponAsync）

### AgentClassSkill

- [ ] **SK-01**: RefundSkill 通过 AgentClassSkill 定义（Frontmatter + Instructions + Scripts）
- [ ] **SK-02**: Skill 注册到 AgentSkillsProvider

### Framework

- [ ] **FW-01**: EventBus 实现业务事件发布/订阅（RefundCompletedEvent）
- [ ] **FW-02**: StructuredOutputParser 将 LLM JSON 转为强类型对象
- [ ] **FW-03**: Safety Pipeline 实现 PII 脱敏（手机号/身份证/银行卡）
- [ ] **FW-04**: Agent Pipeline 6 层管道（简化实现）

### Data

- [ ] **DT-01**: MockOrderService 支持 3 个测试订单（A001 可退 / A002 超期 / A003 未签收）
- [ ] **DT-02**: MockFinanceService 返回模拟退款结果
- [ ] **DT-03**: MockMemberService 返回模拟优惠券数据

### Console Demo

- [ ] **CD-01**: 控制台主循环：读输入 → 意图识别 → 启动/恢复 Workflow → 事件处理
- [ ] **CD-02**: RequestInfoEvent 处理：RefundInfoPort 参数收集 + RefundConfirmPort 确认
- [ ] **CD-03**: WorkflowOutputEvent / ErrorEvent 展示

## v2 Requirements

### Multi-Business Modules

- **WF-10**: Exchange Workflow（换货流程）
- **WF-11**: Logistics Workflow（物流查询）
- **SK-10**: ExchangeSkill / LogisticsSkill

### Observability

- **OB-01**: OpenTelemetry 集成
- **OB-02**: Audit Trail 审计日志
- **OB-03**: Langfuse 集成

### Session Persistence

- **SP-01**: RedisSessionStore 三合一实现
- **SP-02**: Checkpoint 外部持久化
- **SP-03**: TTL 自动过期

### Safety

- **SF-01**: KeywordFilter 关键词拦截
- **SF-02**: PromptInjectionDetector 注入检测
- **SF-03**: SafetyOutputFilter 输出过滤

## Out of Scope

| Feature | Reason |
|---------|--------|
| Web/Gateway 接入层 | Demo 阶段用控制台替代 |
| 真实 MCP Server 调用 | Mock 服务验证流程，接口签名一致 |
| Knowledge Layer (FAQ/RAG) | 旁路系统，后续实现 |
| Human Agent Layer | 人工客服接管，后续实现 |
| Compaction 消息压缩 | 控制台交互消息量小 |
| Saga 补偿机制 | Workflow 异常处理直接写在 Executor |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| WF-01 | Phase 1 | Pending |
| WF-02 | Phase 1 | Pending |
| WF-03 | Phase 1 | Pending |
| WF-04 | Phase 1 | Pending |
| WF-05 | Phase 1 | Pending |
| IR-01 | Phase 1 | Pending |
| IR-02 | Phase 1 | Pending |
| IR-03 | Phase 1 | Pending |
| MC-01 | Phase 1 | Pending |
| MC-02 | Phase 1 | Pending |
| MC-03 | Phase 1 | Pending |
| SK-01 | Phase 1 | Pending |
| SK-02 | Phase 1 | Pending |
| FW-01 | Phase 1 | Pending |
| FW-02 | Phase 1 | Pending |
| FW-03 | Phase 1 | Pending |
| FW-04 | Phase 1 | Pending |
| DT-01 | Phase 1 | Pending |
| DT-02 | Phase 1 | Pending |
| DT-03 | Phase 1 | Pending |
| CD-01 | Phase 1 | Pending |
| CD-02 | Phase 1 | Pending |
| CD-03 | Phase 1 | Pending |

**Coverage:**
- v1 requirements: 23 total
- Mapped to phases: 23
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-01*
*Last updated: 2026-06-01 after initial definition*
