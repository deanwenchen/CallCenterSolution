## 1. 基础配置层

- [ ] 1.1 创建 `CallCenter.Framework/CallCenterOptions.cs`，包含 ApiKey/ModelName/Endpoint/OnRefundCompleted/UseMockServices 属性和 ApplyDefaults() 方法
- [ ] 1.2 验证 ApplyDefaults() 是唯一读取 `Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")` 的地方

## 2. AIAgentFactory

- [ ] 2.1 创建 `CallCenter.AgentHost/AIAgentFactory.cs`，构造函数接收 IChatClient pipelineClient
- [ ] 2.2 实现 CreateIntentAgent(skillsProvider?) — 使用 IntentRegistry.BuildSystemPrompt() 作为 Instructions
- [ ] 2.3 实现 CreateDialogAgent(skillsProvider?) — 使用工作流对话 System Prompt

## 3. 修改 EntryPoint

- [ ] 3.1 修改 EntryPoint 构造函数：接收 AIAgentFactory（替代 IChatClient）
- [ ] 3.2 内部 _intentAgent 改由 factory.CreateIntentAgent(skillsProvider) 创建
- [ ] 3.3 验证编译通过，原有意图识别和路由逻辑不变

## 4. CallCenterService 核心骨架

- [ ] 4.1 创建 `CallCenterService.Core.cs` — 类定义、私有字段（agentFactory/entryPoint/sessionStore/auditLogger/eventBus/refundWorkflow/options/scope/provider）
- [ ] 4.2 实现无参构造函数 `CallCenterService()` → 内部调 `this(null)`
- [ ] 4.3 实现 `CallCenterService(CallCenterOptions?)` — 自建 ServiceCollection，调 AddCallCenter()，resolve 依赖，订阅 OnRefundCompleted 事件
- [ ] 4.4 实现 `IDisposable.Dispose()` 释放 scope 和 provider

## 5. CallCenterService.Routing.cs

- [ ] 5.1 创建 Routing.cs partial 文件
- [ ] 5.2 实现 GetIntentForWorkflow(workflowName) — 工作流名反查意图名
- [ ] 5.3 实现 GetWorkflowForIntent(intentName) — 意图名获取工作流实例

## 6. CallCenterService.Interaction.cs

- [ ] 6.1 创建 Interaction.cs partial 文件
- [ ] 6.2 实现 HandleRequestAsync(request, sessionId, ct) — 从 Console.In 读取用户输入
- [ ] 6.3 处理 RefundSignal.NeedOrderId — 问订单号
- [ ] 6.4 处理 ConfirmRefundRequest — 展示订单详情，等确认/取消
- [ ] 6.5 处理未知回复 — 重新识别意图，处理意图切换/问候/取消
- [ ] 6.6 验证：交互行为与当前 Program.cs 的 HandleRequestAsync 完全一致

## 7. CallCenterService.Execution.cs

- [ ] 7.1 创建 Execution.cs partial 文件
- [ ] 7.2 定义 WorkflowCtx 内部类（替代 ref 参数）
- [ ] 7.3 定义 EventResult 内部类（Continue / Retry / Terminal）
- [ ] 7.4 实现 DriveNewWorkflowAsync(sessionId, initialMessage, ct) — RunStreamingAsync + DriveLoopAsync
- [ ] 7.5 实现 DriveResumeWorkflowAsync(sessionId, userMessage, ct) — ResumeStreamingAsync + DriveLoopAsync
- [ ] 7.6 实现 DriveLoopAsync() — 共享事件循环，await foreach + HandleEventAsync
- [ ] 7.7 实现 HandleEventAsync() — switch 所有 9 种事件类型
- [ ] 7.8 实现 HandleRequestEventAsync — 调用 Interaction.HandleRequestAsync
- [ ] 7.9 实现 HandleOutputEventAsync — 返回结果 + 审计验证
- [ ] 7.10 实现 HandleCheckpointEventAsync — 保存断点
- [ ] 7.11 实现 HandleErrorEventAsync — 审计错误 + Saga 补偿
- [ ] 7.12 实现 HandleExecutorFailedEventAsync — 审计错误
- [ ] 7.13 实现 ExecuteSagaCompensationAsync — Saga 补偿逻辑

## 8. CallCenterService.Intent.cs

- [ ] 8.1 创建 Intent.cs partial 文件
- [ ] 8.2 实现 ProcessAsync(sessionId, userMessage, ct) → string
- [ ] 8.3 switch ProcessResult 分发：StartWorkflow → DriveNewWorkflowAsync, ResumeExisting → DriveResumeWorkflowAsync, 其他直接返回字符串

## 9. DI 扩展（Extensions.cs）

- [ ] 9.1 创建 `CallCenterService.Extensions.cs`
- [ ] 9.2 实现 AddCallCenter(options?) — 注册 IChatClient（keyed base + pipeline 默认）、SessionStore、AuditLogger、EventBus
- [ ] 9.3 实现 Mock 服务注册（IOrderMcpClient/IFinanceMcpClient/IMemberMcpClient）
- [ ] 9.4 实现 AIAgentFactory、EntryPoint、Workflow 注册
- [ ] 9.5 实现 AddCallCenterOrderService<T>() 覆盖方法
- [ ] 9.6 实现 AddCallCenterFinanceService<T>() 覆盖方法
- [ ] 9.7 实现 AddCallCenterMemberService<T>() 覆盖方法

## 10. 精简 Program.cs

- [ ] 10.1 删除 Program.cs 中的 RunWorkflow/ResumeWorkflow/HandleRequestAsync 函数
- [ ] 10.2 删除 IChatClient 创建、Pipeline 组装、AuditLogger 初始化、eventBus.Subscribe 代码
- [ ] 10.3 删除 inputChannel + 后台 Task
- [ ] 10.4 删除 refundWorkflow / refundWorkflowWithAudit 变量
- [ ] 10.5 保留：创建 CallCenterService → while 循环读输入 → ProcessAsync → 打印结果
- [ ] 10.6 清理未使用的 using 引用

## 11. 清理旧代码

- [ ] 11.1 移除 `CallCenter.Framework/ServiceCollectionExtensions.cs` 中的旧 AddCallCenter 方法
- [ ] 11.2 验证无编译错误和未使用引用

## 12. 端到端验证

- [ ] 12.1 编译通过，无新增警告
- [ ] 12.2 测试 T1：输入 "我要退款，订单A001" → 确认 → 退款完成，输出结果与重构前一致
- [ ] 12.3 测试 T2：输入 "我要退款，订单A002" → 规则拒绝（定制商品）
- [ ] 12.4 测试 T3：输入 "你好" → 问候语回复，不启动工作流
- [ ] 12.5 测试 T4：输入 "我要退款"（无订单号）→ 追问订单号 → 用户提供 → 继续流程
