# Phase 8: Business Extensibility Guide - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning
**Source:** discuss-phase analysis

<domain>
## Phase Boundary

编写完整的 7 步扩展指南文档，验证到能指导新手复制出 ExchangeSkill 空壳（编译通过）。ExchangeSkill 是验证文档正确性的 proof-of-concept。

**不在本阶段**:
- Exchange Workflow 完整业务逻辑（v2 WF-10）
- dotnet new 脚手架脚本
- 真实 MCP Server 集成

</domain>

<decisions>
## Implementation Decisions

### D-01: 交付形式 — 纯 Markdown 文档
- **Locked decision:** 扩展指南是 `BUSINESS-EXTENSIBILITY-GUIDE.md` Markdown 文件
- 每步包含：目标、操作指令、代码示例、验证点（编译/运行检查）
- 不包含脚手架脚本（留 v2）
- **Why:** 文档本身就是要验证的交付物，脚本是锦上添花

### D-02: ExchangeSkill 空壳 — 编译通过
- **Locked decision:** ExchangeSkill 必须编译通过才算验证成功
- 包含完整骨架：
  - `src/CallCenter.Workflows/Exchange/ExchangeWorkflow.cs` — 骨架 edges/ports
  - `src/CallCenter.Workflows/Exchange/ExchangeMessages.cs` — 空 records
  - `src/CallCenter.Workflows/Exchange/Executors/` — 空 Executors（throw NotImplementedException）
  - `src/CallCenter.AgentHost/Skills/ExchangeSkill.cs` — 骨架 Skill 注册
  - `src/CallCenter.Workflows/Exchange/ExchangeSkillRegistration.cs` — DI 注册方法
- 不实现业务逻辑：所有 Handler 抛出 `NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide")`
- **Why:** 编译通过验证了目录结构、命名空间、依赖引用全部正确

### D-03: 文档风格 — 对照 Refund 现有模式
- **Locked decision:** 每步对照 Refund 的具体文件，读者逐文件复制
- 7 步与 BE-01 定义一致：复制 → 重命名 → 修改 Workflow → 修改 Executors → 新增 Skill → 注册 → 完成
- 每步包含 Refund 的对应文件作为参考（如"参照 RefundWorkflow.cs 创建 ExchangeWorkflow.cs"）
- **Why:** 新手有现有代码可参照，降低出错率

### D-04: 7 步流程具体定义
- **Locked decision:** 7 步为：
  1. **复制目录** — 复制 `Refund/` → `Exchange/`
  2. **重命名命名空间** — 全局替换 `Refund` → `Exchange`
  3. **修改 Workflow** — 定义 Exchange 的 ports、edges、executors
  4. **修改 Executors** — 创建空 executor 骨架（NotImplementedException）
  5. **新增 Skill** — 创建 `ExchangeSkill.cs`（参照 RefundSkill.cs）
  6. **注册** — 在 DI 容器中注册 ExchangeWorkflow 和 ExchangeSkill
  7. **验证** — `dotnet build` 编译通过 + 验证目录结构正确

### D-05: 文档位置 — 项目根目录
- **Locked decision:** `BUSINESS-EXTENSIBILITY-GUIDE.md` 放在项目根目录（与 CLAUDE.md 同级）
- 不是放在 `.planning/` — 这是开发者文档，不是规划文档
- ExchangeSkill 代码放在对应的现有目录结构下（与 Refund 平级）
- **Why:** 根目录可见性高，新开发者一眼能看到

### D-06: 验证标准 — 编译通过 + 目录结构正确
- **Locked decision:** Phase 8 成功的两个硬性标准：
  1. `dotnet build` 编译通过（0 errors）
  2. Exchange 目录结构符合文档第 1-6 步预期
- 不要求：Exchange 能实际运行（业务逻辑留 v2）
- **Why:** 编译通过证明了扩展流程的完整性

### Claude's Discretion
- 文档中的代码示例是完整代码还是片段（建议：完整代码，方便直接复制）
- Exchange 的 Mock 服务是否需要注册（建议：复用现有的 MockOrderService 等，不新增）

</decisions>

<canonical_refs>
## Canonical References

### Phase Requirements
- `.planning/ROADMAP.md` — Phase 8 goal, success criteria, requirement ID (BE-01)
- `.planning/REQUIREMENTS.md` — BE-01 definition (7-step process)

### Reference Implementation (Refund — used as template for Exchange)
- `src/CallCenter.Workflows/Refund/RefundWorkflow.cs` — workflow structure template
- `src/CallCenter.Workflows/Refund/RefundMessages.cs` — message records template
- `src/CallCenter.Workflows/Refund/Executors/` — executor structure template
- `src/CallCenter.AgentHost/Skills/RefundSkill.cs` — skill template

### Integration Points
- `src/CallCenter.ConsoleDemo/Program.cs` — where ExchangeSkill will be registered
- `src/CallCenter.Workflows/CallCenter.Workflows.csproj` — project file for new directory
- `src/CallCenter.AgentHost/CallCenter.AgentHost.csproj` — project file for new skill

### Current Project Structure
- `src/CallCenter.Workflows/Refund/` — existing workflow to copy/modify
- `src/CallCenter.AgentHost/Skills/` — existing skill directory

</canonical_refs>

<specifics>
## Specific Ideas

- 7 步流程直接映射到 Refund 的具体文件
- ExchangeWorkflow.cs 骨架：定义 infoPort + confirmPort（与 Refund 相同结构，不同消息类型）
- ExchangeMessages.cs 骨架：ExchangeIntent, ExchangeRuleResult, ExchangeExecuted 等空 records
- Executors：GetExchangeOrderExecutor, CheckExchangeRuleExecutor 等空骨架
- ExchangeSkill.cs 骨架：Frontmatter 描述换货流程，空脚本
- DI 注册：`services.AddExchangeWorkflow()` 扩展方法
- 文档放在项目根目录，标题"新增业务模块 7 步扩展指南"

</specifics>

<deferred>
## Deferred Ideas

- Exchange Workflow 完整业务逻辑 → v2（WF-10）
- dotnet new 脚手架脚本 → v2
- 真实 MCP Server 集成 → v2
- ExchangeSkill 实际功能测试 → v2
- 多模块并行开发支持 → v2

</deferred>

---

*Phase: 08-business-extensibility-guide*
*Context gathered: 2026-06-01 via discuss-phase*
