# Tasks: Refund Workflow Demo

## Phase 1: 项目文件 + 包管理

- [ ] 1.1 创建 `Directory.Packages.props` (中央包版本管理)
- [ ] 1.2 更新 `CallCenterSolution.slnx` (添加 5 个新项目)
- [ ] 1.3 创建 5 个 `.csproj` 文件及正确的项目引用

## Phase 2: CallCenter.Shared

- [ ] 2.1 创建 Models: OrderInfo, RefundResult, CouponInfo
- [ ] 2.2 创建 Service 接口: IOrderService, IFinanceService, IMemberService
- [ ] 2.3 创建 Mock 实现: MockOrderService, MockFinanceService, MockMemberService
- [ ] 2.4 创建 MCP Client 接口: IOrderMcpClient, IFinanceMcpClient, IMemberMcpClient (Shared/Mcp/)

## Phase 3: CallCenter.Framework

- [ ] 3.1 EventBus: IBusinessEventBus, InMemoryBusinessEventBus, RefundEvents
- [ ] 3.2 Parsing: StructuredOutputParser
- [ ] 3.3 Builder: BusinessModuleBuilder (空壳)
- [ ] 3.4 Session: InMemorySessionStore, RedisSessionStore (空壳)
- [ ] 3.5 Safety: SafetyPipelineAgent, PiiRedactor (空壳)
- [ ] 3.6 Compaction: CompactionExtensions (空壳)
- [ ] 3.7 Audit: AuditLogger, AuditTrailMiddleware (空壳)
- [ ] 3.8 Saga: SagaBuilder, SagaExtensions (空壳)
- [ ] 3.9 Pipeline: StandardPipelineFactory (空壳)
- [ ] 3.10 ServiceCollectionExtensions.cs

## Phase 4: CallCenter.Workflows (核心)

- [ ] 4.1 RefundMessages.cs (所有消息类型)
- [ ] 4.2 GetOrderExecutor
- [ ] 4.3 CheckRefundRuleExecutor
- [ ] 4.4 WaitUserConfirmExecutor
- [ ] 4.5 ExecuteRefundExecutor
- [ ] 4.6 RestoreCouponExecutor
- [ ] 4.7 SendNotificationExecutor
- [ ] 4.8 RefundDeniedExecutor
- [ ] 4.9 RefundWorkflow.Build() (图构建)

## Phase 5: CallCenter.AgentHost

- [ ] 5.1 EntryPoint.cs (LLM 意图识别)
- [ ] 5.2 RefundSkill.cs (AgentClassSkill stub)

## Phase 6: CallCenter.ConsoleDemo

- [ ] 6.1 Program.cs (主循环 + 事件处理)
- [ ] 6.2 验证 dotnet build 成功
- [ ] 6.3 手动测试 6 个场景
