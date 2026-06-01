---
wave: 3
depends_on: ["02"]
files_modified:
  - src/CallCenter.Framework/EventBus/IBusinessEventBus.cs
  - src/CallCenter.Framework/EventBus/InMemoryBusinessEventBus.cs
  - src/CallCenter.Framework/EventBus/RefundEvents.cs
  - src/CallCenter.Framework/Parsing/StructuredOutputParser.cs
  - src/CallCenter.Framework/Builder/BusinessModuleBuilder.cs
  - src/CallCenter.Framework/Session/InMemorySessionStore.cs
  - src/CallCenter.Framework/Session/RedisSessionStore.cs
  - src/CallCenter.Framework/Safety/SafetyPipelineAgent.cs
  - src/CallCenter.Framework/Safety/PiiRedactor.cs
  - src/CallCenter.Framework/Safety/KeywordFilter.cs
  - src/CallCenter.Framework/Safety/PromptInjectionDetector.cs
  - src/CallCenter.Framework/Compaction/CompactionExtensions.cs
  - src/CallCenter.Framework/Audit/AuditLogger.cs
  - src/CallCenter.Framework/Audit/AuditTrailMiddleware.cs
  - src/CallCenter.Framework/Saga/SagaBuilder.cs
  - src/CallCenter.Framework/Saga/SagaExtensions.cs
  - src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs
  - src/CallCenter.Framework/ServiceCollectionExtensions.cs
requirements: [FW-01, FW-02, FW-03, FW-04, FW-05, FW-06, FW-07, FW-08, FW-09]
autonomous: true
---

# 计划 03：Framework 层 — 核心组件 + 空壳

## 目标

实现 3 个可用的 Framework 组件（EventBus、StructuredOutputParser、Session），创建 6 个空壳组件（Safety、Compaction、Audit、Saga、Pipeline、RedisSessionStore），包含骨架类 + TODO 注释。

## 任务

### 任务 3.1：创建 IBusinessEventBus 接口

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-33：InMemoryBusinessEventBus 发布/订阅）
- Prd.md Section 7.5.2.3（BusinessEventBus 定义）
- openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md（BusinessEventBus 需求，携带 sessionId/userId/orderId/amount 上下文）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/EventBus/IBusinessEventBus.cs 存在
- 接口包含：Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class
- 接口包含：IDisposable Subscribe<T>(Func<T, Task> handler) where T : class
- 命名空间：CallCenter.Framework.EventBus
</acceptance_criteria>

<action>
创建 IBusinessEventBus.cs，包含 PublishAsync 和 Subscribe 方法。使用泛型事件类型。
</action>

### 任务 3.2：创建 InMemoryBusinessEventBus

<acceptance_criteria>
- src/CallCenter.Framework/EventBus/InMemoryBusinessEventBus.cs 存在
- 实现 IBusinessEventBus
- 使用 ConcurrentDictionary<Type, List<Delegate>> 存储处理器
- PublishAsync 遍历事件类型对应的处理器并调用
- Subscribe 添加处理器到列表并返回 IDisposable（用于移除）
- 线程安全（使用 ConcurrentDictionary）
- 命名空间：CallCenter.Framework.EventBus
</acceptance_criteria>

<action>
创建 InMemoryBusinessEventBus。使用 ConcurrentDictionary 做线程安全的处理器存储。PublishAsync 按事件类型查找处理器并调用。Subscribe 返回一个 disposable 用于移除处理器。
</action>

### 任务 3.3：创建 RefundEvents

<read_first>
- openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md（RefundCompletedEvent: SessionId, UserId, OrderId, RefundAmount；RiskAlertEvent: SessionId, UserId, OrderId, AlertType, Details）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/EventBus/RefundEvents.cs 存在
- Record RefundCompletedEvent: string SessionId, string UserId, string OrderId, decimal RefundAmount
- Record RiskAlertEvent: string SessionId, string UserId, string OrderId, string AlertType, string Details
- 命名空间：CallCenter.Framework.EventBus
</acceptance_criteria>

<action>
创建 RefundEvents.cs，包含两个事件 record 类型。
</action>

### 任务 3.4：创建 StructuredOutputParser

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-34：JSON 反序列化包装器）
- Prd.md Section 7.5.2.2（StructuredOutputParser 定义）
- openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md（StructuredOutputParser：自动注入 JSON Schema，解析 LLM JSON → TOutput，解析失败自动重试）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/Parsing/StructuredOutputParser.cs 存在
- Class StructuredOutputParser<TOutput> where TOutput : class
- 方法：TOutput? Parse(string json, JsonSerializerOptions? options = null)
- 使用 System.Text.Json.JsonSerializer.Deserialize<TOutput>
- 命名空间：CallCenter.Framework.Parsing
</acceptance_criteria>

<action>
创建 StructuredOutputParser.cs。泛型类，包含 Parse 方法用于反序列化 JSON。对 JsonSerializer.Deserialize 的简单包装。
</action>

### 任务 3.5：创建 InMemorySessionStore

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-36：InMemorySessionStore 使用字典的基础实现）
- Prd.md Section 7.5.2.1（RedisSessionStore 三合一实现定义）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/Session/InMemorySessionStore.cs 存在
- Class InMemorySessionStore，包含方法：
  - Task<T?> GetAsync<T>(string key, string scope = default)
  - Task SetAsync<T>(string key, T value, string scope = default)
  - Task RemoveAsync(string key, string scope = default)
  - Task<HashSet<string>> GetKeysAsync(string scope = default)
- 使用 ConcurrentDictionary<string, object> 存储
- 命名空间：CallCenter.Framework.Session
</acceptance_criteria>

<action>
创建 InMemorySessionStore.cs。使用 ConcurrentDictionary 做线程安全存储。实现基本的 get/set/remove/getKeys 方法。
</action>

### 任务 3.6：创建 RedisSessionStore 空壳

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-32：骨架类 + TODO）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/Session/RedisSessionStore.cs 存在
- 类包含与 InMemorySessionStore 相同的接口
- 所有方法抛出 NotImplementedException，并附 TODO 注释说明生产环境实现
- 命名空间：CallCenter.Framework.Session
</acceptance_criteria>

<action>
创建 RedisSessionStore.cs 作为骨架类。每个方法抛出 NotImplementedException 并附 TODO 注释。
</action>

### 任务 3.7：创建 BusinessModuleBuilder 空壳

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-01~D-04：项目结构决策，D-30~D-31：骨架 + TODO）
- Prd.md Section 7.4（DevX Builder 定义）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/Builder/BusinessModuleBuilder.cs 存在
- Class BusinessModuleBuilder，包含方法：WithSkill<T>(), WithWorkflow<T>(), WithDefaults()
- 方法返回 builder 以支持链式调用
- 所有方法附 TODO 注释
- 命名空间：CallCenter.Framework.Builder
</acceptance_criteria>

<action>
创建 BusinessModuleBuilder.cs。骨架 builder 类，链式方法全部附 TODO 注释。
</action>

### 任务 3.8：创建 Safety Pipeline 空壳

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-30：骨架 + TODO，D-35：Safety Pipeline 简化实现）
- Prd.md Section 7.3.2（Safety Pipeline：PII 脱敏、关键词拦截、注入检测）
- openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md（Safety Pipeline：手机号/身份证/银行卡正则脱敏）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/Safety/SafetyPipelineAgent.cs 存在 — 骨架 DelegatingAIAgent，附 TODO
- src/CallCenter.Framework/Safety/PiiRedactor.cs 存在 — 包含基础正则：手机号 (1[3-9]\d)\d{4}(\d{4}) → $1****$2
- src/CallCenter.Framework/Safety/KeywordFilter.cs 存在 — 骨架，附 TODO
- src/CallCenter.Framework/Safety/PromptInjectionDetector.cs 存在 — 骨架，附 TODO
- 全部在命名空间：CallCenter.Framework.Safety
</acceptance_criteria>

<action>
创建 4 个文件。PiiRedactor 包含实际手机号正则实现（Redact 方法）。其余 3 个为骨架类，附 TODO 注释。
</action>

### 任务 3.9：创建 Compaction 空壳

<acceptance_criteria>
- src/CallCenter.Framework/Compaction/CompactionExtensions.cs 存在
- 扩展方法 AddCallCenterCompaction(this IServiceCollection services) 返回 builder
- Builder 包含 UseSummarization 方法（no-op）
- TODO 注释说明 8000 token 阈值、保留 8 轮、小模型摘要
- 命名空间：CallCenter.Framework.Compaction
</acceptance_criteria>

<action>
创建 CompactionExtensions.cs，骨架扩展方法。
</action>

### 任务 3.10：创建 Audit 空壳

<acceptance_criteria>
- src/CallCenter.Framework/Audit/AuditLogger.cs 存在 — 骨架类，包含 LogAsync 方法（no-op）
- src/CallCenter.Framework/Audit/AuditTrailMiddleware.cs 存在 — 骨架类，附 TODO
- TODO 注释：自动捕获 Workflow Step 输入/输出，不可变存储
- 命名空间：CallCenter.Framework.Audit
</acceptance_criteria>

<action>
创建 2 个骨架文件，附 TODO 注释。
</action>

### 任务 3.11：创建 Saga 空壳

<acceptance_criteria>
- src/CallCenter.Framework/Saga/SagaBuilder.cs 存在 — 骨架，附 TODO：补偿机制、重试策略 1min/5min/30min
- src/CallCenter.Framework/Saga/SagaExtensions.cs 存在 — 骨架
- 命名空间：CallCenter.Framework.Saga
</acceptance_criteria>

<action>
创建 2 个骨架文件，附 TODO 注释，引用 spec 中的重试策略。
</action>

### 任务 3.12：创建 Pipeline 空壳

<acceptance_criteria>
- src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs 存在
- 骨架工厂类，包含 CreatePipeline 方法（返回空/no-op pipeline）
- TODO 注释说明 6 层 pipeline 顺序（SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput）
- 命名空间：CallCenter.Framework.Pipeline
</acceptance_criteria>

<action>
创建 StandardPipelineFactory.cs 骨架，附 TODO 注释引用 6 层 pipeline 顺序。
</action>

### 任务 3.13：创建 ServiceCollectionExtensions

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-33~D-36：EventBus、StructuredOutputParser、InMemorySessionStore 可用）
- Prd.md Section 7.6（开发者接入：AddCallCenter()）
</read_first>

<acceptance_criteria>
- src/CallCenter.Framework/ServiceCollectionExtensions.cs 存在
- 扩展方法 AddCallCenter(this IServiceCollection services)
- 注册：InMemoryBusinessEventBus 为 IBusinessEventBus（单例），InMemorySessionStore（单例）
- 返回 IServiceCollection 以支持链式调用
- 命名空间：CallCenter.Framework
</acceptance_criteria>

<action>
创建 ServiceCollectionExtensions.cs。AddCallCenter 扩展注册 EventBus 和 SessionStore 为单例。
</action>

### 任务 3.14：验证 Framework 层编译

<acceptance_criteria>
- `dotnet build src/CallCenter.Framework/CallCenter.Framework.csproj` 成功，0 错误
- 所有骨架类编译通过
</acceptance_criteria>

<action>
对 Framework 项目执行 dotnet build。修复任何编译错误。
</action>
