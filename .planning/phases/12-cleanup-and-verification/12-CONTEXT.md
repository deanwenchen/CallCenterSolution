# Phase 12: 清理与验证 - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning
**Source:** OpenSpec extract-callcenter-service Tasks 11 + 12

<domain>
## Phase Boundary

v2.0 框架提取的最后一步：
1. **清理旧代码** — 删除 ServiceCollectionExtensions.cs 中的旧 AddCallCenter 方法（Task 11.1-11.2）
2. **端到端验证** — 确认重构后的行为与重构前一致（Task 12.1-12.5）

### 清理范围（Task 11）
- 检查 `CallCenter.Framework/ServiceCollectionExtensions.cs` 是否有旧的 `AddCallCenter` 方法需要删除
- 验证 EntryPoint 是否还有用（Program.cs 不再引用，但 Routing.cs 的注释提到"替代 EntryPoint"）
- 检查是否有未使用的 using 引用
- 确认 _inputChannel 在 Core.cs 中的存在是必要的（Interaction.cs 仍通过它读取控制台输入）

### E2E 测试场景（Task 12）
- T1: "我要退款，订单A001" → 确认 → 退款完成 → 输出一致
- T2: "我要退款，订单A002" → 规则拒绝（定制商品）
- T3: "你好" → 问候语，不启动工作流
- T4: "我要退款"（无订单号）→ 追问订单号 → 用户提供 → 继续

## 包含的任务（OpenSpec）

- **Task 11:** 清理旧代码
- **Task 12:** 端到端验证

</domain>

<decisions>
## Implementation Decisions

### 清理策略
- **D-12-01:** 只删除明确无用的代码（无编译引用、无运行时调用）。不主动做架构改进或重命名。
- **D-12-02:** EntryPoint 保留。虽然 Program.cs 不再引用，但 Extensions.cs 仍在 DI 中注册它，Routing.cs 的设计文档提到它是"替代前的参照物"。删除需要确认没有其他组件依赖。

### E2E 验证策略
- **D-12-03:** E2E 测试以人工冒烟测试为主（控制台交互），配合源码断言验证。不引入测试框架（xUnit 等）—— v2 范围。
- **D-12-04:** 验证通过标准：编译无错误 + 4 个场景行为描述与重构前预期一致。

### Claude's Discretion
- 清理范围的具体文件需要扫描当前代码确认
- E2E 测试的具体输出格式参照现有 Program.cs 行为
- 如果清理过程发现某些文件"可能有用但不确定"，保留并记录

</decisions>

<scope_fence>
## Out of Scope（本 Phase 不做）

- 引入 xUnit/MSTest 测试框架
- Web API 端到端测试
- 真实 MCP Server 集成测试
- 性能测试 / 负载测试
- 新增业务工作流

## Risk Summary

清理阶段风险低（编译验证即可）。E2E 验证风险中等（需要 API key 才能跑通 LLM 路径）。

## Success Criteria

1. 旧 AddCallCenter 方法删除后编译仍通过
2. 4 个 E2E 场景行为一致
3. 无未使用的 using 引用
</scope_fence>

<canonical_refs>
## Canonical References

### OpenSpec 工件
- `openspec/changes/extract-callcenter-service/tasks.md` — 任务 11 + 12
- `openspec/changes/extract-callcenter-service/design.md` — 技术设计

### 代码库参照
- `src/CallCenter.Framework/ServiceCollectionExtensions.cs` — 旧 AddCallCenter（清理目标）
- `src/CallCenter.AgentHost/Extensions.cs` — 新 AddCallCenter
- `src/CallCenter.AgentHost/EntryPoint.cs` — 可能保留或清理
- `src/CallCenter.AgentHost/CallCenterService.Core.cs` — 确认 _inputChannel 用途
- `src/CallCenter.ConsoleDemo/Program.cs` — 当前精简后的入口

### 相前 Phase 上下文
- `.planning/phases/10-callcenter-service-skeleton/10-03-SUMMARY.md` — Execution/Interaction 完成
- `.planning/phases/11-execution-entry/11-01-SUMMARY.md` — Program.cs 精简完成

</canonical_refs>

<specifics>
## Specific Ideas

当前代码库状态（Phase 11 完成后）：
- Program.cs: 18 行，仅引用 `new CallCenterService()` + `ProcessAsync`
- CallCenterService: 6 个 partial 文件（Core/Intent/Routing/Execution/Interaction + 可能还有 Extensions）
- EntryPoint: 仍在 Extensions.cs 中注册，但 Program.cs 不再直接使用
- _inputChannel: Core.cs 中后台 Task 填充，Interaction.cs 中读取（用于订单号/确认输入）

需要检查的清理点：
1. ServiceCollectionExtensions.cs 是否存在？内容是什么？
2. EntryPoint 是否被 Extensions.cs 以外的地方引用？
3. 各文件是否有未使用的 using 指令？
</specifics>

<deferred>
## Deferred Ideas

- xUnit 测试框架 — v2
- Web API E2E — v2
- 性能测试 — v2

</deferred>

---

*Phase: 12-cleanup-and-verification*
*Context gathered: 2026-06-04*
