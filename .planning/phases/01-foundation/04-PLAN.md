---
wave: 4
depends_on: ["02"]
files_modified:
  - src/CallCenter.Workflows/Refund/RefundMessages.cs
  - src/CallCenter.Workflows/Refund/Executors/GetOrderExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/CheckRefundRuleExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/WaitUserConfirmExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/RestoreCouponExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/SendNotificationExecutor.cs
  - src/CallCenter.Workflows/Refund/Executors/RefundDeniedExecutor.cs
  - src/CallCenter.Workflows/Refund/RefundWorkflow.cs
requirements: [WF-01, WF-02, WF-03, WF-04, WF-05]
autonomous: true
---

# 计划 04：Workflows 层 — 退款工作流 + 7 个执行器

## 目标

实现完整的退款工作流：消息类型、7 个执行器、以及带有正确图拓扑的工作流构建器（2 个 RequestPort、条件边、参数循环）。

## 任务

### 任务 4.1：创建 RefundMessages.cs

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-37~D-39：C# record 定义消息类型，RefundSignal 枚举）
- .planning/phases/01-foundation/01-CONTEXT.md（D-22~D-25：GetOrderExecutor 作为起点，初始输入通过 RunAsync）
- .planning/phases/01-foundation/01-CONTEXT.md（D-18~D-21：参数循环通过 RefundSignal 回到 InfoPort）
- Prd.md Section 5.1（用户请求处理流程）
- MAF 参考：HumanInTheLoopBasic/WorkflowFactory.cs（NumberSignal 枚举模式）
- MAF 参考：ConditionalEdges/02_SwitchCase/Program.cs（record 类型：DetectionResult, EmailResponse）
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/RefundMessages.cs 存在
- 在命名空间 CallCenter.Workflows.Refund 中定义以下类型：
  - record RefundIntent(string? OrderId, string? UserId)
  - enum RefundSignal { Init, NeedOrderId, OrderFound, Ineligible, Cancelled }
  - record OrderFound(OrderInfo Order)
  - record RefundRuleResult(bool IsEligible, string? Reason, decimal RefundAmount, string? OrderId, string? ProductName)
  - record ConfirmRefundRequest(decimal Amount, string OrderId, string ProductName)
  - record UserConfirmation(bool Confirmed)
  - record RefundExecuted(RefundResult Result)
  - record CouponRestored(string? CouponId)
  - record RefundNotification(string Message)
- 所有类型使用 C# record
- OrderInfo、RefundResult、CouponInfo 来自 CallCenter.Shared.Models（使用 using 指令）
</acceptance_criteria>

<action>
创建 RefundMessages.cs，包含全部 9 个消息类型。使用 C# record。添加 using CallCenter.Shared.Models 引用跨项目类型。RefundRuleResult 增加 OrderId 和 ProductName 字段供下游使用。
</action>

### 任务 4.2：创建 GetOrderExecutor

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-07~D-09：构造函数注入服务接口，D-10~D-14：Executor<TInput,TOutput> 返回值，D-18~D-21：SendMessage(RefundSignal.NeedOrderId) 回到 InfoPort，D-14：[SendsMessage] 多消息类型标记）
- MAF 参考：ConditionalEdges/02_SwitchCase/Program.cs（SpamDetectionExecutor 模式：Executor<ChatMessage, DetectionResult>，构造函数注入，context.ReadStateAsync，context.QueueStateUpdateAsync）
- MAF 参考：HumanInTheLoopBasic/WorkflowFactory.cs（JudgeExecutor：Executor<int>，SendMessageAsync，YieldOutputAsync）
- MAF 参考：SendsMessageAttribute.cs（[SendsMessage] 属性）
- MAF 参考：YieldsOutputAttribute.cs（[YieldsOutput] 属性）
- IOrderMcpClient 接口来自 Shared/Mcp/
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/GetOrderExecutor.cs 存在
- Class GetOrderExecutor : Executor<RefundIntent>
- [SendsMessage(typeof(OrderFound))] 和 [SendsMessage(typeof(RefundSignal))] 属性
- 构造函数：GetOrderExecutor(IOrderMcpClient orderService)
- HandleAsync 逻辑：
  - 如果 message.OrderId 为 null/空：context.SendMessageAsync(RefundSignal.NeedOrderId) 并返回
  - 否则：调用 orderService.GetOrderAsync(message.OrderId)，找到则返回 OrderFound(order)，否则 context.SendMessageAsync(RefundSignal.NeedOrderId) 循环回
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 GetOrderExecutor。构造函数接收 IOrderMcpClient。HandleAsync 检查 OrderId，调用服务，发送相应消息。使用 context.SendMessageAsync 做分支输出。
</action>

### 任务 4.3：创建 CheckRefundRuleExecutor

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-10~D-11：Executor<TInput,TOutput> 返回值通过 ForwardMessage 自动传递）
- Prd.md Section 5.4（退款规则：7 天内、已签收、非特殊品类）
- Prd.md Section 二（退款金额 = 原价 - 优惠券分摊）
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/CheckRefundRuleExecutor.cs 存在
- Class CheckRefundRuleExecutor : Executor<OrderFound, RefundRuleResult>
- 构造函数：CheckRefundRuleExecutor()（无外部依赖，纯逻辑）
- HandleAsync 校验：
  - OrderDate 距今 <= 7 天
  - Status == "delivered"
  - Category != "custom"
- 任一规则不通过：返回 RefundRuleResult(IsEligible=false, Reason="...", RefundAmount=0, OrderId, ProductName)
- 全部通过：计算金额 = Amount - 优惠券折扣（如有优惠券减 20），返回 RefundRuleResult(IsEligible=true, Reason=null, RefundAmount=计算值, OrderId, ProductName)
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 CheckRefundRuleExecutor。纯逻辑执行器，无外部依赖。校验 3 条规则，计算退款金额。返回 RefundRuleResult（含 OrderId 和 ProductName 供下游使用）。
</action>

### 任务 4.4：创建 WaitUserConfirmExecutor

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-12：WaitUserConfirmExecutor 使用 Executor<TInput>，内部 SendMessageAsync 到 ConfirmPort）
- .planning/phases/01-foundation/01-CONTEXT.md（D-20：SendMessage(new ConfirmRefundRequest) 到 ConfirmPort，ForwardMessage 路由到端口）
- MAF 参考：HumanInTheLoopBasic/WorkflowFactory.cs（JudgeExecutor 通过 SendMessageAsync 发送 NumberSignal）
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/WaitUserConfirmExecutor.cs 存在
- Class WaitUserConfirmExecutor : Executor<RefundRuleResult>
- [SendsMessage(typeof(ConfirmRefundRequest))] 属性
- 构造函数：WaitUserConfirmExecutor()
- HandleAsync：如果 message.IsEligible，context.SendMessageAsync(new ConfirmRefundRequest(message.RefundAmount, message.OrderId, message.ProductName))
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 WaitUserConfirmExecutor。接收 RefundRuleResult（含 ProductName）。通过 context.SendMessageAsync 发送 ConfirmRefundRequest 到 ConfirmPort。
</action>

### 任务 4.5：创建 ExecuteRefundExecutor

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-07：构造函数注入，D-13：RefundExecuted 输出类型）
- IFinanceMcpClient 接口来自 Shared/Mcp/
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs 存在
- Class ExecuteRefundExecutor : Executor<UserConfirmation, RefundExecuted>
- [SendsMessage(typeof(RefundSignal))] 属性（用于取消路径）
- 构造函数：ExecuteRefundExecutor(IFinanceMcpClient financeService)
- HandleAsync：如果 !message.Confirmed，context.SendMessageAsync(RefundSignal.Cancelled) 并返回；否则调用 financeService.RefundAsync，返回 RefundExecuted(result)
- orderId 和 amount 通过 context 状态传递（由 WaitUserConfirmExecutor 存储）
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 ExecuteRefundExecutor。通过 context 状态读取 orderId 和 amount（WaitUserConfirmExecutor 存储）。用户确认则调用退款，取消则发送 RefundSignal.Cancelled。
</action>

### 任务 4.6：创建 RestoreCouponExecutor

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-07：构造函数注入）
- IMemberMcpClient 接口来自 Shared/Mcp/
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/RestoreCouponExecutor.cs 存在
- Class RestoreCouponExecutor : Executor<RefundExecuted, CouponRestored>
- 构造函数：RestoreCouponExecutor(IMemberMcpClient memberService)
- HandleAsync：调用 memberService.RestoreCouponAsync("U100", "CPN-2024")，返回 CouponRestored("CPN-2024")
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 RestoreCouponExecutor。构造函数接收 IMemberMcpClient。调用恢复优惠券，返回 CouponRestored。
</action>

### 任务 4.7：创建 SendNotificationExecutor

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-13：[YieldsOutput(typeof(RefundNotification))]）
- IBusinessEventBus 来自 Framework/EventBus/
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/SendNotificationExecutor.cs 存在
- Class SendNotificationExecutor : Executor<CouponRestored>
- [YieldsOutput(typeof(RefundNotification))] 属性
- 构造函数：SendNotificationExecutor(IBusinessEventBus eventBus)
- HandleAsync：通过 eventBus 发布 RefundCompletedEvent，然后 YieldOutputAsync(new RefundNotification("退款已处理完成"))
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 SendNotificationExecutor。构造函数接收 EventBus。发布事件并输出通知。
</action>

### 任务 4.8：创建 RefundDeniedExecutor

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/Executors/RefundDeniedExecutor.cs 存在
- Class RefundDeniedExecutor : Executor<RefundRuleResult>
- [YieldsOutput(typeof(RefundNotification))] 属性
- 构造函数：RefundDeniedExecutor()
- HandleAsync：YieldOutputAsync(new RefundNotification($"退款被拒绝: {message.Reason}"))
- 命名空间：CallCenter.Workflows.Refund
</action>

<action>
创建 RefundDeniedExecutor。简单终端执行器，输出拒绝原因。
</action>

### 任务 4.9：创建 RefundWorkflow.Build()

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-15~D-17：AddSwitch 条件路由，ForwardMessage 类型安全路由）
- .planning/phases/01-foundation/01-CONTEXT.md（D-22~D-25：GetOrderExecutor 作为起点，不是端口）
- .planning/phases/01-foundation/01-CONTEXT.md（D-18~D-21：ForwardMessage<RefundSignal>(getOrder, infoPort) 参数循环）
- MAF 参考：HumanInTheLoopBasic/WorkflowFactory.cs（WorkflowBuilder 模式，RequestPort.Create，AddEdge，WithOutputFrom）
- MAF 参考：ConditionalEdges/02_SwitchCase/Program.cs（AddSwitch 模式）
- MAF 参考：WorkflowBuilderExtensions.cs（ForwardMessage<T>，AddEdge<T> 带条件，AddSwitch）
- MAF 参考：RequestPort.cs（RequestPort.Create<TRequest, TResponse>）
</read_first>

<acceptance_criteria>
- src/CallCenter.Workflows/Refund/RefundWorkflow.cs 存在
- 静态类 RefundWorkflow，包含方法：public static Workflow Build(IOrderMcpClient orderService, IFinanceMcpClient financeService, IMemberMcpClient memberService, IBusinessEventBus eventBus)
- 创建 2 个 RequestPort：
  - RequestPort.Create<RefundSignal, RefundIntent>("RefundInfoPort") — 不作为起点
  - RequestPort.Create<ConfirmRefundRequest, UserConfirmation>("RefundConfirmPort")
- 创建 7 个执行器，注入服务
- 图结构：
  - WorkflowBuilder 以 getOrder 开始（不是端口）
  - getOrder → ForwardMessage<OrderFound>(getOrder, checkRule)
  - getOrder → ForwardMessage<RefundSignal>(getOrder, infoPort) — 参数循环
  - checkRule → AddEdge<RefundRuleResult>(checkRule, waitConfirm, r => r?.IsEligible == true)
  - checkRule → AddEdge<RefundRuleResult>(checkRule, denied, r => r?.IsEligible == false)
  - waitConfirm → ForwardMessage<ConfirmRefundRequest>(waitConfirm, confirmPort)
  - confirmPort → AddEdge(confirmPort, doRefund)
  - doRefund → ForwardMessage<RefundExecuted>(doRefund, restoreCoupon)
  - restoreCoupon → ForwardMessage<CouponRestored>(restoreCoupon, notify)
  - WithOutputFrom(notify, denied)
- 命名空间：CallCenter.Workflows.Refund
</acceptance_criteria>

<action>
创建 RefundWorkflow.cs，包含静态 Build 方法。接收 4 个服务接口参数。创建端口、执行器，使用 ForwardMessage 和条件边构建图。
</action>

### 任务 4.10：验证 Workflows 层编译

<acceptance_criteria>
- `dotnet build src/CallCenter.Workflows/CallCenter.Workflows.csproj` 成功，0 错误
- 所有执行器和工作流编译通过
</acceptance_criteria>

<action>
对 Workflows 项目执行 dotnet build。修复任何编译错误。
</action>
