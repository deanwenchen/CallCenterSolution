# CallCenter 框架重构需求文档

> 2026-06-03 生成
> 背景：退款工作流（Refund）能正常运行，但代码组织臃肿，调用方耦合严重，需要框架化重构。

---

## 一、意图识别层

### 1.1 意图注册表（IntentRegistry）

**现状**：意图和参数硬编码在 `EntryPoint.cs` 的 System Prompt 字符串里，加新意图需要修改 prompt 字符串。

**目标**：
- 集中定义意图字典：`IReadOnlyDictionary<string, IntentDef>`
- `IntentDef` 是 `record(Description, Parameters?)`
- `BuildSystemPrompt()` 通过 `JsonSerializer.Serialize(All)` 自动生成 prompt，不再手拼
- `IsBusinessIntent(intent)` 判断是否为业务意图（有 Parameters 的才算）

**加新意图只改一处**：在 `IntentRegistry.All` 字典加一行

**状态**：✅ `IntentRegistry.cs` 已写好，文件保留中

### 1.2 IntentResult 通用参数

**现状**：
```csharp
record IntentResult(string Intent, string? OrderId)  // OrderId 硬编码
```

**目标**：
```csharp
record IntentResult(string Intent, Dictionary<string, string?> Parameters)
```

**状态**：✅ `EntryPoint.cs` 已改

### 1.3 路由保持 if/switch 硬编码

**原则**：每个 case 自己知道取什么参数，不做 `IntentFactory`、`WorkflowName` 等过度抽象。

**示例**：
```csharp
case "refund":
    var orderId = newIntent.Parameters.GetValueOrDefault("OrderId");
    return ProcessResult.StartWorkflow(new RefundIntent(orderId, "U100"));
```

**状态**：✅ `EntryPoint.cs` 已改

---

## 二、技能注册层

### 2.1 技能注册表（SkillRegistry）

**现状**：
```csharp
new AgentSkillsProvider(new RefundSkill(), new ExchangeSkill())  // 手动 new
```

**目标**：
```csharp
new AgentSkillsProvider(SkillRegistry.All)  // 集中注册
```

加新技能只改 `SkillRegistry.All` 加一行。

**状态**：✅ `Skills/SkillRegistry.cs` 已写好，文件保留中

---

## 三、框架封装层

### 3.1 Program.cs 不该有工作流知识

**现状问题**：
- `Program.cs` 有 300+ 行 `RunWorkflow`/`ResumeWorkflow`/`HandleRequestAsync`
- 这些是框架内部的事（工作流驱动、端口交互、审计、断点恢复），不该暴露给调用方

**目标**：调用方只需要：
```csharp
var framework = new CallCenterService();
var reply = await framework.ProcessAsync(sessionId, userMessage);
Console.WriteLine(reply);
```

### 3.2 所有基础设施应该框架内部初始化

**现状问题**：`Program.cs` 在：
- 创建 `IChatClient`（OpenAI/DashScope）
- 组装聊天管道（`StandardPipelineFactory.CreatePipeline`）
- 创建 `InMemorySessionStore`、`AuditLogger`、`InMemoryBusinessEventBus`
- 订阅事件（`eventBus.Subscribe<RefundCompletedEvent>`）

**目标**：框架内部自动初始化所有基础设施，调用方只需提供配置参数（如 API key、userId），或全部默认。

### 3.3 环境变量只在一处处理

**现状问题**：`Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")` 散落在多处。

**目标**：只在 `CallCenterOptions` 的默认值处理一次，其他地方只读 options。

### 3.4 DI 支持

**需求**：
- 提供 `IServiceCollection.AddCallCenter()` 扩展方法
- 自动注册所有依赖（LLM、会话、审计、技能、业务服务）
- 生产环境可先注册真实服务实现覆盖默认 Mock：
  ```csharp
  builder.Services.AddCallCenterOrderService<RealOrderService>();
  builder.Services.AddCallCenter();
  ```

**状态**：❌ 代码丢失，未实现

### 3.5 代码可读性 — partial class 拆分

**按职责拆文件**：

| 文件 | 职责 |
|------|------|
| `CallCenterService.Core.cs` | 主类定义、构造函数、DI 注入、初始化 |
| `CallCenterService.Intent.cs` | 意图识别、统一入口 ProcessAsync、超时检查 |
| `CallCenterService.Routing.cs` | 工作流注册、意图↔工作流映射 |
| `CallCenterService.Execution.cs` | 工作流执行、所有事件处理、端口交互、Saga 补偿 |
| `CallCenterService.Extensions.cs` | DI 扩展方法（AddCallCenter 等） |
| `IntentRegistry.cs` | 意图定义 + Prompt 生成 |
| `Skills/SkillRegistry.cs` | 技能注册 |

**状态**：❌ 代码丢失，未实现

---

## 四、工作流执行层

### 4.1 所有 WorkflowEvent 都处理

**需求**：`switch (evt)` 列出所有可访问的事件类型：
- 已实现的走业务逻辑（RequestInfo → 交互、Output → 返回、Error → 处理）
- 未实现的输出日志方便追踪（WorkflowStarted、ExecutorInvoked、ExecutorCompleted、Warning 等）
- 不会漏掉任何框架行为

**事件类型清单**（可访问的）：
- `WorkflowStartedEvent` → 日志
- `RequestInfoEvent` → 端口交互
- `WorkflowOutputEvent` → 返回最终结果
- `SuperStepStartedEvent` → 日志
- `SuperStepCompletedEvent` → 保存断点
- `ExecutorInvokedEvent` → 日志
- `ExecutorCompletedEvent` → 日志
- `ExecutorFailedEvent` → 错误处理
- `WorkflowErrorEvent` → 错误 + Saga 补偿
- `WorkflowWarningEvent` → 日志

**状态**：❌ 代码丢失，未实现

### 4.2 工作流执行上下文

**问题**：异步方法不能使用 `ref` 参数。

**解决**：用 `WorkflowCtx` 类代替 `ref` 参数传递 `CheckpointInfo` 和 `CurrentMessage`。

---

## 五、验收标准

### 5.1 流程能跑通
- 输入 "我要退款，订单A001" → 查询订单 → 规则校验 → 用户确认 → 执行退款 → 输出结果
- 断点恢复正常
- 意图切换正常

### 5.2 调用方简洁
- 控制台：`new CallCenterService()` + `ProcessAsync()` 两行搞定
- Web：`builder.Services.AddCallCenter()` + `app.MapPost()` 两行搞定

### 5.3 加新意图只改一处
- `IntentRegistry.All` 加一行定义
- `EntryPoint` 的 switch 加一个 case（路由）
- `Program.cs` 不需要改

### 5.4 加新工作流只改两处
- `SkillRegistry.All` 加一行
- `EntryPoint` 注册工作流构建函数

---

## 六、当前保留的文件

| 文件 | 状态 |
|------|------|
| `IntentRegistry.cs` | ✅ 保留 |
| `Skills/SkillRegistry.cs` | ✅ 保留 |
| `EntryPoint.cs` | ⚠️ 部分保留（通用参数 + switch 路由），但 `ProcessAsync` 仍需要 workflow 参数 |
| `Program.cs` | ❌ 恢复原版（300+ 行内联函数） |

## 七、待实现

| 需求 | 编号 | 状态 |
|------|------|------|
| CallCenterService 主类（partial） | 3.1, 3.2, 3.4, 3.5 | ❌ 未实现 |
| DI 扩展 AddCallCenter | 3.4 | ❌ 未实现 |
| 完整事件处理（Execution） | 4.1 | ❌ 未实现 |
| Program.cs 精简到消息循环 | 3.1 | ❌ 未实现 |
| 环境变量只在一处处理 | 3.3 | ❌ 未实现 |
| 流程跑通测试 | 5.1 | ✅ 原版能跑 |

---

## 八、实施步骤

### 阶段 1：搭建 partial class 骨架

**目标**：创建 `CallCenterService` partial 类文件骨架，从 `EntryPoint` 迁移核心字段。

**步骤**：

1. **创建 `CallCenterService.Core.cs`** — 主类定义 + 构造函数
   - 定义私有字段：`_sessionStore`, `_auditLogger`, `_inputChannel`, `_intentAgent`, `_orderService`, `_financeService`, `_memberService`, `_eventBus`, `_scope`
   - 无参构造函数 `CallCenterService()` — 内部构建最小 DI 容器，统一走 DI 通道初始化
   - DI 构造函数 `CallCenterService(InMemorySessionStore, AuditLogger, AIAgent, IOrderMcpClient, ...)` — 适合 Web 容器注入
   - `Dispose()` 方法释放 scope
   - stdin 读取通道初始化（后台 Task）

2. **创建 `CallCenterService.Intent.cs`** — 意图识别 + ProcessAsync
   - `RecognizeIntentAsync()` — 调用 LLM 识别意图
   - `CheckTimeoutAsync()` — 会话超时检查
   - `ProcessAsync(sessionId, userMessage)` — **统一入口**，内部完成：意图识别 → 超时检查 → 工作流启动/恢复 → 返回字符串

3. **创建 `CallCenterService.Routing.cs`** — 工作流注册 + 意图映射
   - `_workflows` 字典：`Dictionary<string, Workflow>`
   - `_workflowToIntent` 字典：`Dictionary<string, string>`
   - `RegisterDefaults()` — 注册 refund、exchange
   - `GetOrBuildWorkflow(workflowName)` — 延迟构建，缓存实例
   - `GetWorkflowForIntent(intent)` — 意图名 → 工作流名
   - `GetIntentForWorkflow(workflowName)` — 工作流名 → 意图名
   - `CreateIntentMessage(intent, parameters)` — 参数 → 初始消息对象

4. **创建 `CallCenterService.Execution.cs`** — 工作流执行（见阶段 2）

5. **创建 `CallCenterService.Extensions.cs`** — DI 扩展（见阶段 3）

6. **修改 `EntryPoint.cs`** — 改为 thin wrapper
   - 内部委托给 `CallCenterService.ProcessAsync()`
   - 保留对外兼容的构造函数签名

**依赖**：无

**验收**：编译通过

---

### 阶段 2：实现完整事件处理（Execution.cs）

**目标**：`HandleEvent` 方法列出所有可访问的 `WorkflowEvent` 类型，不遗漏任何框架行为。

**步骤**：

1. **定义 `WorkflowCtx` 内部类**（替代 ref 参数）
   ```csharp
   private sealed class WorkflowCtx {
       public CheckpointInfo? LastCheckpoint;
       public object CurrentMessage;
   }
   ```

2. **实现 `RunWorkflowAsync(workflow, initialMessage, sessionId, ct)`**
   - 使用 `InProcessExecution.RunStreamingAsync()` 驱动工作流
   - `await foreach` 监听所有事件
   - `WorkflowCtx` 跟踪断点和当前消息
   - 重试循环（需要端口交互时重跑）

3. **实现 `ResumeWorkflowAsync(workflow, sessionId, ct)`**
   - 从 `_sessionStore` 读取 lastCheckpoint
   - 使用 `InProcessExecution.ResumeStreamingAsync()` 恢复执行

4. **实现 `HandleEvent(evt, run, sessionId, ct, ctx)` — switch 所有事件类型**

   | 事件类型 | 处理方式 |
   |----------|----------|
   | `RequestInfoEvent` | 调用 `HandleRequestAsync` 与用户交互 → `EventResult.Retry` |
   | `WorkflowOutputEvent` | 返回最终结果 + 审计验证 → `EventResult.Terminal(msg)` |
   | `SuperStepCompletedEvent` | 保存断点到 sessionStore + 审计 → `EventResult.Continue` |
   | `WorkflowErrorEvent` | 审计错误 + Saga 补偿 → `EventResult.Terminal(error)` |
   | `ExecutorFailedEvent` | 审计错误 → `EventResult.Terminal(error)` |
   | `WorkflowStartedEvent` | 不处理 → `EventResult.Continue` |
   | `ExecutorInvokedEvent` | 不处理 → `EventResult.Continue` |
   | `ExecutorCompletedEvent` | 不处理 → `EventResult.Continue` |
   | `WorkflowWarningEvent` | 不处理 → `EventResult.Continue` |

5. **实现 `HandleRequestAsync(request, sessionId, ct)`** — 端口交互
   - `RefundSignal.NeedOrderId` → 控制台问用户要订单号 → 返回 `RefundIntent`
   - `ConfirmRefundRequest` → 展示订单详情 → 等待用户确认/取消 → 返回 `UserConfirmation`
   - 未知回复 → 重新识别意图 → 处理意图切换/问候/取消

6. **实现 `HandleSagaCompensation(errEvt)`** — Saga 补偿

7. **定义 `EventResult` 内部类**
   ```csharp
   private sealed class EventResult {
       public bool IsTerminal { get; set; }
       public bool IsRetry { get; set; }
       public string? Message { get; set; }
   }
   ```

**依赖**：阶段 1 完成

**验收**：`echo "我要退款，订单A001" | echo "确认"` 能走完退款流程

---

### 阶段 3：实现 DI 扩展（Extensions.cs）

**目标**：提供 `AddCallCenter()` 扩展方法，自动注册所有依赖。

**步骤**：

1. **创建静态类 `CallCenterServiceExtensions`**
   ```csharp
   public static class CallCenterServiceExtensions {
       public static IServiceCollection AddCallCenter(this IServiceCollection services,
           Action<CallCenterOptions>? configure = null);
   }
   ```

2. **`AddCallCenter` 内部注册顺序**：
   ```
   1. 合并 CallCenterOptions（处理 ApiKey 默认值）
   2. TryAddSingleton<IChatClient>() — 用 options.ApiKey 创建
   3. TryAddSingleton<AIAgent>() — 用 IChatClient + IntentRegistry.BuildSystemPrompt()
   4. TryAddSingleton<IOrderMcpClient>() — 默认 MockOrderService
   5. TryAddSingleton<IFinanceMcpClient>() — 默认 MockFinanceService
   6. TryAddSingleton<IMemberMcpClient>() — 默认 MockMemberService
   7. TryAddSingleton<IBusinessEventBus>() — InMemoryBusinessEventBus
   8. TryAddSingleton<InMemorySessionStore>()
   9. TryAddSingleton<AuditLogger>()
   10. TryAddSingleton<AgentSkillsProvider?>() — 自动发现 Skills
   11. AddSingleton<CallCenterService>() — 从 DI 解析所有依赖构造
   ```

3. **提供覆盖方法**：
   ```csharp
   services.AddCallCenterOrderService<RealOrderService>();  // 覆盖 IOrderMcpClient
   services.AddCallCenterFinanceService<RealFinanceService>();  // 覆盖 IFinanceMcpClient
   services.AddCallCenterMemberService<RealMemberService>();  // 覆盖 IMemberMcpClient
   ```

4. **`CallCenterService` 无参构造函数**内部也走 DI：
   ```csharp
   public CallCenterService() {
       var services = new ServiceCollection();
       services.AddCallCenter(options);
       var provider = services.BuildServiceProvider();
       // 从 provider 解析所有依赖赋值给私有字段
   }
   ```

**依赖**：阶段 1、2 完成

**验收**：
```csharp
// 控制台
var framework = new CallCenterService();
var reply = await framework.ProcessAsync("demo-session", "我要退款，订单A001");

// Web
builder.Services.AddCallCenter();
app.MapPost("/chat", async (CallCenterService svc, ChatRequest req) =>
    Results.Ok(await svc.ProcessAsync(req.SessionId, req.Message)));
```

---

### 阶段 4：精简 Program.cs

**目标**：从 420 行精简到 20 行以内。

**步骤**：

1. **删除** `Program.cs` 中的：
   - `RunWorkflow()` 函数（~120 行）
   - `ResumeWorkflow()` 函数（~90 行）
   - `HandleRequestAsync()` 函数（~60 行）
   - `IChatClient` 创建代码
   - `StandardPipelineFactory` 组装代码
   - `AuditLogger` 初始化
   - `eventBus.Subscribe` 代码
   - `refundWorkflow` / `refundWorkflowWithAudit` 变量
   - `inputChannel` + 后台 Task

2. **保留**：
   - 读取用户输入（`Console.ReadLine()`）
   - 调用 `framework.ProcessAsync()`
   - 打印结果

3. **最终 Program.cs**：
   ```csharp
   var framework = new CallCenterService(new CallCenterOptions {
       OnRefundCompleted = e => Console.WriteLine($"\n[EVENT] 退款完成..."),
   });

   while (true) {
       var msg = await Console.In.ReadLineAsync();
       if (msg == "quit") break;
       var reply = await framework.ProcessAsync("demo-session", msg);
       Console.WriteLine($"系统: {reply}");
   }
   ```

**依赖**：阶段 1、2、3 完成

**验收**：编译通过 + 流程跑通

---

### 阶段 5：环境变量统一

**目标**：`Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")` 只在一处出现。

**步骤**：

1. **`CallCenterOptions` 默认值**中处理：
   ```csharp
   public string ApiKey { get; init; } = "";  // 不直接读环境变量
   ```

2. **`CallCenterOptions.ApplyDefaults(opt)`** 统一处理：
   ```csharp
   internal static CallCenterOptions ApplyDefaults(CallCenterOptions? opt) {
       if (opt == null) return new CallCenterOptions();
       return string.IsNullOrEmpty(opt.ApiKey)
           ? new CallCenterOptions {
               ApiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? "",
               ModelName = opt.ModelName,
               Endpoint = opt.Endpoint,
               OnRefundCompleted = opt.OnRefundCompleted,
               UseMockServices = opt.UseMockServices,
           }
           : opt;
   }
   ```

3. **所有构造函数和 DI 扩展**都调用 `ApplyDefaults()` 获取完整 options

**验收**：全局搜索 `Environment.GetEnvironmentVariable` 只在 `ApplyDefaults` 中出现一次

---

### 阶段 6：流程跑通测试

**测试用例**：

| 编号 | 输入 | 预期输出 |
|------|------|----------|
| T1 | `我要退款，订单A001` → `确认` | 查询成功 → 校验通过 → 确认 → 退款完成 → 输出结果 |
| T2 | `我要退款，订单A002` | 查询成功 → 规则拒绝（定制商品） → 输出拒绝原因 |
| T3 | `我要退款，订单A003` | 查询成功 → 规则拒绝（未签收） → 输出拒绝原因 |
| T4 | `我要退款`（无订单号） | 追问订单号 → 用户提供 → 继续流程 |
| T5 | `你好` | 回复问候语，不启动工作流 |
| T6 | `我要退款，订单A001` → `取消` | 查询成功 → 校验通过 → 取消 → 输出取消消息 |

**验收**：T1-T6 全部通过
