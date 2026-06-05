# Phase 17: Session 持久化存储 - Context

**Gathered:** 2026-06-05
**Status:** Ready for planning

<domain>
## Phase Boundary

把现有的 `InMemorySessionStore` 抽象为 `ISessionStore` 接口，实现 `RedisSessionStore`（基于 Leo.Data.Redis），让会话数据在进程重启后持久化。支持配置切换内存/Redis 存储，现有功能在内存模式下保持不变。

</domain>

<decisions>
## Implementation Decisions

### 接口设计
- **D-01:** `ISessionStore` 接口定义 5 个方法：`GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `GetKeysAsync`, `ClearScopeAsync`。`SetAsync` 增加可选 `TimeSpan? ttl` 参数（来自 SS-01）。
- **D-02:** 增加 `TryGetAsync<T>` 方法，区分「key 不存在」和「反序列化失败」两种场景。
- **D-03:** `InMemorySessionStore` 实现 `ISessionStore` 接口，TTL 参数忽略（内存存储不支持过期），保持现有 `ConcurrentDictionary` 行为不变。

### Redis 集成
- **D-04:** `RedisSessionStore` 直接复用 `Leo.Data.Redis` 的 `RedisHelper` 静态 API（`RedisHelper.Get<T>`, `RedisHelper.Set<T>`, `RedisHelper.SetValueAsync` 等）。不走 DI 注入连接。
- **D-05:** Redis 配置通过 Leo.Data.Redis 的 `redisconfig.json` 管理（provider name 方式）。CallCenter 的 `appsettings.json` 中 `SessionStore:Provider` 配置映射到 Leo.Data.Redis 的 provider name。

### 序列化方案
- **D-06:** 直接调用 `RedisHelper.Get<T>/Set<T>` 方法，序列化由 Leo.Data.Redis 内部处理（ServiceStack.Text）。CallCenter 层不做手动 JSON 转换。

### 迁移策略
- **D-07:** `CallCenterService.Core.cs` 的 `_sessionStore` 字段从 `InMemorySessionStore` 改为 `ISessionStore` 接口依赖。两个构造函数（参数less 和 DI 注入）都改为解析 `ISessionStore`。
- **D-08:** `Extensions.cs` 新增 `AddSessionStore(IConfiguration)` 扩展方法，读取 `appsettings.json` 中 `"SessionStore:Provider"` 决定注册 `InMemorySessionStore` 还是 `RedisSessionStore`。保留现有 `AddCallCenter` 的行为（默认注册 InMemory）。
- **D-09:** Leo.Data.Redis 需要在 `CallCenter.Framework.csproj` 中作为依赖引入（NuGet 包或项目引用）。

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 需求定义
- `.planning/REQUIREMENTS.md` §v4.0 Requirements — SS-01 ~ SS-07 完整需求定义
- `.planning/ROADMAP.md` §Phase 17 — Phase goal 和 success criteria

### 现有代码
- `src/CallCenter.Framework/Session/InMemorySessionStore.cs` — 现有内存实现，需改为实现 ISessionStore
- `src/CallCenter.Framework/Session/RedisSessionStore.cs` — 现有占位符（throw NotImplementedException），需替换为 Leo.Data.Redis 实现
- `src/CallCenter.AgentHost/CallCenterService.Core.cs` — 消费者，_sessionStore 字段需从 InMemorySessionStore 改为 ISessionStore
- `src/CallCenter.AgentHost/Extensions.cs` — DI 扩展方法，需新增 AddSessionStore 配置切换

### 外部依赖
- `D:\babybusworkplace\LeoSolution\Leo.Data.Redis` — Leo.Data.Redis 源码（内部 RedisHelper.Get<T>/Set<T> 使用 ServiceStack.Text 序列化，RedisConfigHelper 读取 redisconfig.json）

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `InMemorySessionStore`: 已有完整的 scope→key 两层字典实现（`ConcurrentDictionary<string, ConcurrentDictionary<string, object>>`），5 个方法签名可直接提取为接口
- `RedisSessionStore`: 已有占位符文件（含注释说明需存储的三项内容：聊天历史、Agent 会话、工作流断点），可直接替换实现

### Established Patterns
- DI 注册模式：`Extensions.cs` 使用 `AddCallCenter(IConfiguration)` 从 appsettings.json 读取配置段，新增 `AddSessionStore` 应遵循同一模式
- 消费者模式：`CallCenterService` 通过 DI 构造函数获取依赖，改为接口依赖后保持同一模式
- 配置化模式：Safety 选项通过 `appsettings.json` 读取 → 强类型 options 对象，SessionStore 配置应遵循同一模式

### Integration Points
- `src/CallCenter.Framework/CallCenter.Framework.csproj` — 需要添加 Leo.Data.Redis 依赖
- `src/CallCenter.WebApi/Program.cs` — 当前调用 `svc.GetLastActivityAsync` / `svc.ClearSessionScopeAsync`，这些方法底层使用 ISessionStore，接口改动后需确保兼容
- `appsettings.json`（WebApi 项目） — 需要新增 `SessionStore` 配置段

</code_context>

<specifics>
## Specific Ideas

- RedisSessionStore 注释中提到需存储三项内容（聊天历史、Agent 会话序列化、工作流断点），这些是后续扩展方向，但 Phase 17 聚焦在接口抽象 + 基础 Redis 读写 + TTL
- Leo.Data.Redis 使用 ServiceStack.Redis 底层，支持连接池、读写分离、多 provider 管理

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 17-Session 持久化存储*
*Context gathered: 2026-06-05*
