## ADDED Requirements

### Requirement: 统一服务入口 ProcessAsync
系统 SHALL 提供 CallCenterService.ProcessAsync(sessionId, userMessage) 方法，返回 string 类型结果。该方法 SHALL 内部完成意图识别、工作流启动/恢复、事件循环驱动，直到达到终态（输出、错误、超时）后返回。

#### Scenario: 用户发起新退款流程
- **WHEN** 用户输入 "我要退款，订单A001" 且无活跃工作流
- **THEN** ProcessAsync 识别 refund 意图，启动 RefundWorkflow，驱动完整流程，返回最终结果字符串

#### Scenario: 用户恢复已有工作流
- **WHEN** 用户输入 "确认" 且有活跃的 RefundWorkflow 等待确认
- **THEN** ProcessAsync 识别为恢复已有工作流，从断点继续执行，返回最终结果字符串

#### Scenario: 非业务意图输入
- **WHEN** 用户输入 "你好"
- **THEN** ProcessAsync 返回问候语，不启动任何工作流

#### Scenario: 会话超时
- **WHEN** 用户输入消息且距上次活动超过 60 分钟
- **THEN** ProcessAsync 返回会话终止消息，清除活跃工作流

### Requirement: CallCenterService partial class 拆分
CallCenterService SHALL 使用 C# partial class 机制拆分为以下文件：Core.cs（类定义、构造函数、字段、Dispose）、Intent.cs（ProcessAsync、意图识别委托）、Routing.cs（工作流注册、意图映射）、Execution.cs（工作流执行、事件循环、事件处理）、Interaction.cs（用户交互、HandleRequestAsync）。

#### Scenario: 无参构造函数
- **WHEN** 调用 new CallCenterService()
- **THEN** 内部自建 ServiceCollection，调用 AddCallCenter()，resolve 所有依赖，服务可正常使用

#### Scenario: 带 options 构造函数
- **WHEN** 调用 new CallCenterService(options)
- **THEN** 使用提供的 CallCenterOptions 配置，默认值由 ApplyDefaults 补充

### Requirement: 工作流事件处理完整性
Execution.cs 的 HandleEventAsync SHALL 列出所有可访问的 WorkflowEvent 类型：RequestInfoEvent（用户交互）、WorkflowOutputEvent（返回结果）、SuperStepCompletedEvent（保存断点）、WorkflowErrorEvent（错误+Saga）、ExecutorFailedEvent（错误）、WorkflowStartedEvent（日志）、ExecutorInvokedEvent（日志）、ExecutorCompletedEvent（日志）、WorkflowWarningEvent（日志）。

#### Scenario: RequestInfoEvent 处理
- **WHEN** 工作流发出 RequestInfoEvent
- **THEN** 调用 HandleRequestAsync 与用户交互，返回 EventResult.Retry 或 Continue

#### Scenario: WorkflowErrorEvent 触发 Saga 补偿
- **WHEN** ExecuteRefund executor 抛出异常
- **THEN** 记录审计错误日志，触发 Saga 补偿（恢复优惠券），返回错误消息

#### Scenario: WorkflowOutputEvent 返回结果
- **WHEN** 工作流完成并输出结果
- **THEN** 返回最终结果字符串，保存断点，清除活跃工作流，验证审计链

### Requirement: 业务流程不变
重构 SHALL 不改变退款工作流的 6 步流程（GetOrder → CheckRefundRule → WaitConfirm → ExecuteRefund → RestoreCoupon → SendNotification），不改变事件处理逻辑，不改变 workflows/ 目录的任何代码。

#### Scenario: 完整退款流程行为一致
- **WHEN** 输入 "我要退款，订单A001" → 提供订单号 → 确认退款
- **THEN** 输出结果与重构前完全一致：查询成功 → 校验通过 → 确认 → 退款完成
