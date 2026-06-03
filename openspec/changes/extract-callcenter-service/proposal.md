## Why

Program.cs（439 行）承载了所有基础设施组装、工作流执行、事件处理、用户交互和断点保存，代码臃肿且难以复用。未来需要支持 Web API 调用，当前的控制台内联代码无法直接复用。需要抽出一个可复用的 `CallCenterService` 框架层。

## What Changes

- **新增** `CallCenterService` partial 类（Core/Intent/Routing/Execution/Interaction/Extensions），作为统一服务入口
- **新增** `AIAgentFactory` 工厂类，按场景动态创建 AIAgent（意图识别 vs 工作流对话）
- **新增** `CallCenterOptions` 配置类，环境变量统一在一处处理
- **新增** `AddCallCenter()` DI 扩展方法，自动注册所有依赖
- **修改** `EntryPoint.cs` 构造函数改用 AIAgentFactory（不再直接创建 AIAgent）
- **修改** `Program.cs` 精简到 ~20 行消息循环
- **移除** `CallCenter.Framework/ServiceCollectionExtensions.cs` 中的旧 `AddCallCenter`

**不改变**：
- 退款工作流 6 步流程（GetOrder → CheckRefundRule → WaitConfirm → ExecuteRefund → RestoreCoupon → SendNotification）
- 事件处理逻辑（RequestInfo、Output、Error、Checkpoint、Saga 补偿）
- workflows/ 目录下的任何代码
- EntryPoint 的路由和超时逻辑

## Capabilities

### New Capabilities

- `callcenter-service`: 统一服务入口，ProcessAsync(sessionId, userMessage) → string，内部完成意图识别→工作流执行→返回结果
- `di-support`: DI 容器支持，AddCallCenter() 注册所有基础设施，提供覆盖方法（AddCallCenterOrderService 等）
- `agent-factory`: AIAgentFactory 按场景动态创建 AIAgent，AIAgent 本身不直接 DI

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- `src/CallCenter.AgentHost/` — 新增 7 个文件，修改 EntryPoint.cs
- `src/CallCenter.ConsoleDemo/Program.cs` — 从 439 行精简到 ~20 行
- `src/CallCenter.Framework/` — 新增 CallCenterOptions.cs，移除旧 DI 扩展
- 控制台和 Web API 共用同一 CallCenterService
- 现有退款流程行为完全保持不变
