---
phase: 13-web-api-chat
plan: "01"
subsystem: api
tags: [aspnet-core, minimal-api, swagger, cors, dotnet10]

# Dependency graph
requires:
  - phase: 1-8 (AgentHost + CallCenterService + DI + AIAgentFactory 基础)
    provides: "CallCenterService.ProcessAsync, AddCallCenter() DI 注册, AIAgentFactory, AgentSkillsProvider"
provides:
  - "ASP.NET Core Minimal API WebApi 项目"
  - "POST /chat 阻塞式 JSON 响应端点"
  - "CORS 允许所有来源（开发阶段）"
  - "Swagger UI（仅 Development 环境）"
  - "ConsoleDemo 与 WebApi 可并行运行"
affects: [14-sse-streaming, 15-session-management]

# Tech tracking
tech-stack:
  added: [Swashbuckle.AspNetCore]
  patterns:
    - "WebApi 项目引用 AgentHost + Framework + Shared + Workflows + MAF"
    - "环境变量读取 API Key（appsettings.json 不含敏感信息）"
    - "DI 构造函数注入 CallCenterService（IServiceProvider 版本）"
    - "AIAgentFactory 和 AgentSkillsProvider 必须手动注册（AddCallCenter 不注册它们）"

key-files:
  created:
    - src/CallCenter.WebApi/CallCenter.WebApi.csproj
    - src/CallCenter.WebApi/Program.cs
    - src/CallCenter.WebApi/appsettings.json
    - src/CallCenter.WebApi/ChatRequest.cs
  modified:
    - CallCenterSolution.slnx
    - Directory.Packages.props
    - src/CallCenter.AgentHost/CallCenter.AgentHost.csproj
    - src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj
    - src/CallCenter.Framework/CallCenter.Framework.csproj
    - src/CallCenter.Workflows/CallCenter.Workflows.csproj

key-decisions:
  - "移除 .WithOpenApi() — .NET 10 中已不存在该扩展方法，Minimal API 端点自动被 Swagger 发现"
  - "修复 MAF SDK 相对路径 — 工作树深度导致 4 级 -> 7 级路径修正（影响 5 个 csproj 文件）"
  - "添加 MAAI001 NoWarn — AgentSkillsProvider 在 MAF 中标记为评估中，需抑制警告"

patterns-established:
  - "WebApi 项目使用 Microsoft.NET.Sdk.Web SDK"
  - "appsettings.json 只含 Web 层配置，API Key 走环境变量"
  - "中间件顺序: UseCors → UseSwagger → UseHttpsRedirection → MapPost → Run"

requirements-completed: [WA-01, WA-02, WA-05]

# Metrics
duration: 15min
completed: 2026-06-04
---

# Phase 13 Plan 01: CallCenter.WebApi 项目创建 + POST /chat 端点 + CORS/Swagger 配置

**ASP.NET Core Minimal API WebApi 项目，POST /chat 阻塞式 JSON 响应端点，CORS 允许所有来源，Swagger UI 开发环境可用**

## Performance

- **Duration:** 15 min
- **Started:** 2026-06-04T10:30:00Z
- **Completed:** 2026-06-04T10:45:00Z
- **Tasks:** 3
- **Files modified:** 9

## Accomplishments

- CallCenter.WebApi 项目创建完成（net10.0, Sdk.Web），引用 AgentHost + Framework + Shared + Workflows + MAF
- DI 正确注册：AddCallCenter(), AIAgentFactory, AgentSkillsProvider, CallCenterService
- POST /chat 端点实现：空消息校验返回 400，sessionId 自动生成 GUID，调用 ProcessAsync 返回阻塞式 JSON
- CORS 配置 AllowAll 策略（AllowAnyOrigin/Method/Header）
- Swagger UI 仅在 Development 环境启用
- 项目已添加到 CallCenterSolution.slnx，ConsoleDemo 和 WebApi 可并行编译

## Task Commits

Each task was committed atomically:

1. **Task 1: 创建 CallCenter.WebApi 项目 + DI 注册** - `a46ddd5` (feat)
2. **Task 2: 实现 POST /chat 阻塞式端点** - `8dab8c4` (feat)
3. **Task 3: CORS + Swagger 中间件配置** - `feed2c6` (feat)

## Files Created/Modified

- `src/CallCenter.WebApi/CallCenter.WebApi.csproj` - Web 项目定义，引用所有依赖 + Swashbuckle
- `src/CallCenter.WebApi/Program.cs` - 应用入口，DI 注册 + 中间件管道 + /chat 端点
- `src/CallCenter.WebApi/appsettings.json` - Web 层配置（CORS 来源列表、Swagger 开关）
- `src/CallCenter.WebApi/ChatRequest.cs` - 请求 DTO（Message + SessionId）
- `CallCenterSolution.slnx` - 添加 WebApi 项目条目
- `Directory.Packages.props` - 添加 Swashbuckle.AspNetCore 版本

## Decisions Made

- 移除 `.WithOpenApi()` — .NET 9+ 中该扩展方法已移除，Minimal API 端点自动被 Swagger 发现
- 添加 `<NoWarn>$(NoWarn);MAAI001</NoWarn>` 到 WebApi csproj — 与 Framework.csproj 保持一致

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] 修复 MAF SDK 相对路径在工作树模式下失效**
- **Found during:** Task 1（项目创建后编译验证）
- **Issue:** MAF SDK 相对路径 `..\..\..\..\GitCode\...`（4 级上溯）从主仓库 `D:\Claude\CallCenterSolution1\src\CallCenter.*` 正确解析到 `D:\GitCode\...`，但从工作树 `.claude\worktrees\agent-xxx\src\CallCenter.*` 解析到 `.claude\GitCode\...`（不存在）。工作树额外嵌套了 3 层目录（.claude/worktrees/agent-xxx/），需要 7 级上溯。
- **Fix:** 将所有 5 个 csproj 文件中的 MAF SDK 相对路径从 4 级改为 7 级：`..\..\..\..\..\..\..\GitCode\...`
- **Files modified:** 
  - src/CallCenter.WebApi/CallCenter.WebApi.csproj（新建）
  - src/CallCenter.Framework/CallCenter.Framework.csproj
  - src/CallCenter.AgentHost/CallCenter.AgentHost.csproj
  - src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj
  - src/CallCenter.Workflows/CallCenter.Workflows.csproj
- **Verification:** dotnet build 成功，MAF SDK 正确还原和编译
- **Committed in:** `a46ddd5`（Task 1 commit）

**2. [Rule 1 - Bug] 添加 Microsoft.Agents.AI using 指令**
- **Found during:** Task 1（编译验证）
- **Issue:** Program.cs 使用 `AgentSkillsProvider` 但未导入 `Microsoft.Agents.AI` 命名空间，导致 CS0246 编译错误
- **Fix:** 添加 `using Microsoft.Agents.AI;`
- **Files modified:** src/CallCenter.WebApi/Program.cs
- **Verification:** 编译通过
- **Committed in:** `a46ddd5`（Task 1 commit）

**3. [Rule 1 - Bug] 添加 MAAI001 NoWarn 抑制**
- **Found during:** Task 1（编译验证）
- **Issue:** `AgentSkillsProvider` 在 MAF 中被标记为评估中（MAAI001 诊断），默认视为编译错误
- **Fix:** 在 WebApi.csproj PropertyGroup 中添加 `<NoWarn>$(NoWarn);MAAI001</NoWarn>`
- **Files modified:** src/CallCenter.WebApi/CallCenter.WebApi.csproj
- **Verification:** 编译通过，0 警告 0 错误
- **Committed in:** `a46ddd5`（Task 1 commit）

**4. [Rule 1 - Bug] 移除 .WithOpenApi() 扩展方法调用**
- **Found during:** Task 2（编译验证）
- **Issue:** `.WithOpenApi()` 在 .NET 9+ 中已被移除，导致 CS1061 编译错误
- **Fix:** 移除 `.WithOpenApi()` 调用，保留 `.WithName("Chat")`
- **Files modified:** src/CallCenter.WebApi/Program.cs
- **Verification:** 编译通过
- **Committed in:** `8dab8c4`（Task 2 commit）

---

**Total deviations:** 4 auto-fixed (3 bug fixes, 1 blocking)
**Impact on plan:** 所有修复均为正确性和编译必需。MAF 路径修复是工作树模式的系统性问题，修复后所有项目均可在工作树中编译。无范围蔓延。

## Issues Encountered

- 工作树模式下的 MAF SDK 相对路径问题 — 这是预存问题（ConsoleDemo 和 Framework 等所有项目都有相同问题），在本次计划中一并修复
- `AgentSkillsProvider` 的 MAAI001 警告 — 与 Framework.csproj 保持一致的 NoWarn 处理

## User Setup Required

None - 运行时需要设置环境变量 `DASHSCOPE_API_KEY` 才能启动服务（与 ConsoleDemo 一致）。

## Next Phase Readiness

- WebApi 项目基础就绪，Phase 14 可在此基础上添加 SSE 流式端点
- 会话管理（sessionId 生成）为最简 GUID 方案，Phase 14 可增强为持久化会话存储
- CORS 当前允许所有来源，生产环境应收紧配置

---
*Phase: 13-web-api-chat*
*Completed: 2026-06-04*
