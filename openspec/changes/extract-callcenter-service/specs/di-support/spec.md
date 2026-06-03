## ADDED Requirements

### Requirement: AddCallCenter DI 扩展方法
系统 SHALL 提供 IServiceCollection.AddCallCenter(options?) 扩展方法，自动注册所有依赖服务：IChatClient（pipeline-wrapped）、InMemorySessionStore、AuditLogger、IBusinessEventBus、IOrderMcpClient（默认 Mock）、IFinanceMcpClient（默认 Mock）、IMemberMcpClient（默认 Mock）、AIAgentFactory、EntryPoint、Workflow（Refund）。

#### Scenario: 默认注册 Mock 服务
- **WHEN** 调用 services.AddCallCenter() 且 UseMockServices = true
- **THEN** 注册 MockOrderService、MockFinanceService、MockMemberService

#### Scenario: 覆盖默认服务
- **WHEN** 调用 services.AddCallCenterOrderService<RealOrderService>() 再调用 AddCallCenter()
- **THEN** IOrderMcpClient 解析为 RealOrderService 而非 MockOrderService

### Requirement: IChatClient 分层注册
DI 容器 SHALL 使用 AddKeyedSingleton 注册原始 IChatClient（key="base"），再注册 pipeline 包装的 IChatClient 作为默认实现。AIAgentFactory SHALL 从默认 IChatClient 解析获取 pipeline 客户端。

#### Scenario: 原始客户端可被工厂访问
- **WHEN** AIAgentFactory 需要创建 IntentAgent
- **THEN** 从 DI 解析默认的 IChatClient（已包含 Safety/Logging/Compaction/ToolApproval 层）

### Requirement: CallCenterOptions 环境变量统一处理
CallCenterOptions.ApplyDefaults() SHALL 是唯一读取 Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") 的地方。所有构造函数和 DI 扩展都通过 ApplyDefaults() 获取完整配置。

#### Scenario: 未提供 ApiKey 时自动从环境变量读取
- **WHEN** new CallCenterService() 且未设置 ApiKey
- **THEN** ApplyDefaults 读取 DASHSCOPE_API_KEY 环境变量

#### Scenario: 显式提供 ApiKey 时不使用环境变量
- **WHEN** new CallCenterService(new CallCenterOptions { ApiKey = "sk-xxx" })
- **THEN** 使用提供的 ApiKey，不读取环境变量

### Requirement: 服务覆盖方法
系统 SHALL 提供 AddCallCenterOrderService<T>()、AddCallCenterFinanceService<T>()、AddCallCenterMemberService<T>() 扩展方法，允许调用方覆盖默认的 Mock 服务实现。

#### Scenario: Web API 覆盖所有 Mock 服务
- **WHEN** Web API 项目调用 services.AddCallCenterOrderService<RealOrderService>().AddCallCenterFinanceService<RealFinanceService>().AddCallCenter()
- **THEN** 订单和财务服务使用真实实现，会员服务仍为 Mock
