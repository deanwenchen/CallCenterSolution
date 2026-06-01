# Phase 4：验证 + 边界情况 - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning

<domain>
## Phase Boundary

验收 + 清理阶段。验证 Phase 1-3 实现的正确性，消除构建警告，确认目录结构与 PRD 一致。本阶段**不编写新功能代码**。

</domain>

<decisions>
## Implementation Decisions

### 取消流程
- **D-69:** ExecuteRefundExecutor 内部处理 `UserConfirmation.Confirmed = false`：不调用财务服务，通过 `YieldOutputAsync(new RefundNotification("退款已取消"))` 输出取消通知，流程正常终止
- **D-70:** 不需要发送 RefundSignal.Cancelled — 用户说取消时工作流已经过了 ConfirmPort，直接输出即可

### 构建警告消除
- **D-71:** 从 CallCenter.Framework.csproj 和 CallCenter.Shared.csproj 中移除冗余的 `<PackageReference Include="System.Text.Json" />` — .NET 10.0 SDK 已内置

### 验证范围
- **D-72:** Phase 4 无新功能代码 — 所有需求（WF-04, FW-01, FW-03, FW-04）已在 Phase 1 实现
- **D-73:** Phase 4 工作限于：消除警告（4 个 NU1510）+ 取消路径验证 + 手动验收 5 个成功场景

### 目录结构
- **D-74:** 目录结构已完整，所有 Framework 空壳文件存在，Executor 全部实现

### Claude's Discretion
- 手动验收场景的具体测试步骤由实现者决定
- ExecuteRefundExecutor 取消路径的具体实现方式（YieldOutput vs SendMessage）由实现者根据 MAF API 决定

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — WF-04, FW-01, FW-03, FW-04 需求定义
- `.planning/ROADMAP.md` — Phase 4 goal and success criteria

### Prior Phase Context
- `.planning/phases/03-consoledemo-main-loop-event-handling/03-CONTEXT.md` — Phase 3 决策（断点恢复、意图切换、超时、错误恢复）
- `.planning/phases/01-foundation/01-CONTEXT.md` — Phase 1 决策（项目结构、Executor 模式、条件边路由）

### Existing Code
- `src/CallCenter.ConsoleDemo/Program.cs` — 主循环 + HandleRequest（含取消处理）
- `src/CallCenter.AgentHost/EntryPoint.cs` — 意图识别 + ProcessAsync 路由
- `src/CallCenter.Workflows/Refund/RefundWorkflow.cs` — 退款工作流图构建
- `src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs` — 需补充取消路径
- `src/CallCenter.Framework/EventBus/InMemoryBusinessEventBus.cs` — 事件发布/订阅
- `src/CallCenter.Framework/Parsing/StructuredOutputParser.cs` — JSON 解析
- `src/CallCenter.Framework/Safety/PiiRedactor.cs` — PII 脱敏（手机号正则）
- `src/CallCenter.Shared/Mcp/IOrderMcpClient.cs` — 订单 MCP 接口
- `src/CallCenter.Shared/Mcp/IFinanceMcpClient.cs` — 财务 MCP 接口
- `src/CallCenter.Shared/Mcp/IMemberMcpClient.cs` — 会员 MCP 接口
- `src/CallCenter.Shared/Services/MockOrderService.cs` — 模拟订单服务
- `src/CallCenter.Shared/Services/MockFinanceService.cs` — 模拟退款服务
- `src/CallCenter.Shared/Services/MockMemberService.cs` — 模拟会员服务
- `src/CallCenter.Shared/CallCenter.Shared.csproj` — 需移除 System.Text.Json
- `src/CallCenter.Framework/CallCenter.Framework.csproj` — 需移除 System.Text.Json

### MAF Reference
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/InProcessExecution.cs` — RunStreamingAsync / StreamingRun
- `../../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/IWorkflowContext.cs` — YieldOutputAsync 接口

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 所有 Phase 1-3 代码已实现并编译通过
- Program.cs 已包含 HandleRequest 的"取消"处理（`reply == "取消"` → `UserConfirmation(false)`）
- ExecuteRefundExecutor 已实现确认路径，需补充取消分支

### Established Patterns
- Executor 通过构造函数注入服务接口
- Executor 通过 YieldOutputAsync 输出最终结果
- 使用 YieldOutputAsync 而非 SendMessageAsync 做终端输出

### Integration Points
- ExecuteRefundExecutor 需要接收 UserConfirmation，检查 Confirmed 属性
- 取消时通过 YieldOutputAsync 输出 RefundNotification

</code_context>

<specifics>
## Specific Ideas

- ExecuteRefundExecutor 取消路径应打印清晰的中文提示
- 手动验收应覆盖 5 个 Phase 3 成功场景
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 04-verification-edge-cases*
*Context gathered: 2026-06-01*
