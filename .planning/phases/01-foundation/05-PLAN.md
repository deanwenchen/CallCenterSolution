---
wave: 5
depends_on: ["02", "03", "04"]
files_modified:
  - src/CallCenter.AgentHost/EntryPoint.cs
  - src/CallCenter.AgentHost/Skills/RefundSkill.cs
requirements: [IR-01, IR-02, IR-03, SK-01, SK-02]
autonomous: true
---

# 计划 05：AgentHost 层 — 入口点 + 退款技能

## 目标

实现基于 LLM 的意图识别入口点（EntryPoint）和 AgentClassSkill 形式的 RefundSkill。

## 任务

### 任务 5.1：创建 EntryPoint

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（IR-01~IR-05：LLM 意图识别、StateBag 检查、Resume vs 新启动）
- Prd.md Section 5.1（入口点：检查 activeWorkflow → Resume 或意图路由）
- Prd.md Section 四（意图路由：LLM Agent + StructuredOutputParser）
- openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md（入口点和意图路由需求，4 种场景）
- MAF 参考：ConditionalEdges/02_SwitchCase/Program.cs（使用 DashScope 端点的 OpenAI 客户端创建，ChatClientAgent 模式）
- MAF 参考：InProcessExecution.cs（RunStreamingAsync, RunAsync, ResumeAsync）
- CallCenter.Framework.Session.InMemorySessionStore
- CallCenter.Framework.Parsing.StructuredOutputParser
</read_first>

<acceptance_criteria>
- src/CallCenter.AgentHost/EntryPoint.cs 存在
- Class EntryPoint，包含：
  - 构造函数：EntryPoint(IChatClient chatClient, InMemorySessionStore sessionStore)
  - record IntentResult(string Intent, string? Workflow, string? OrderId)
  - Task<IntentResult?> RecognizeIntentAsync(string userMessage, CancellationToken ct) — 使用 chatClient 调用 LLM，系统提示要求 JSON 输出 {"intent": "...", "orderId": "..."}
  - 方法：string? GetActiveWorkflow(string sessionId) — 检查会话存储
  - 方法：Task SetActiveWorkflow(string sessionId, string workflowName) — 设置会话存储
  - 方法：Task ClearActiveWorkflow(string sessionId) — 清除会话存储
- 系统提示："你是一个意图识别助手。分析用户消息，判断意图。返回JSON格式: {\"intent\": \"refund\"|\"greeting\"|\"unknown\", \"workflow\": \"RefundWorkflow\", \"orderId\": \"<如果提到订单号>\"}. 只返回JSON，不要其他内容。"
- 使用 StructuredOutputParser<IntentResult> 解析 LLM 响应
- record ProcessResult，包含子类型：ResumeExisting, StartWorkflow(RefundIntent), NoIntent(string)
- ProcessAsync(sessionId, userMessage, refundWorkflow, ct) — 主入口方法：
  - 步骤1：从会话获取 activeWorkflow
  - 步骤2：如果 activeWorkflow 存在且 == "RefundWorkflow"：返回 ProcessResult.ResumeExisting（Phase 3 实现完整 Resume）
  - 步骤3：如果 activeWorkflow 存在但 != "RefundWorkflow"：清除它，继续进行意图识别
  - 步骤4：调用 RecognizeIntentAsync(userMessage, ct)
  - 步骤5：如果 intent == null 或 intent.Intent == "unknown"/"greeting"：返回 ProcessResult.NoIntent(回复消息)
  - 步骤6：如果 intent.Intent == "refund"：SetActiveWorkflowAsync(sessionId, "RefundWorkflow")，返回 ProcessResult.StartWorkflow(new RefundIntent(intent.OrderId, "U100"))
- 命名空间：CallCenter.AgentHost
</acceptance_criteria>

<action>
创建 EntryPoint.cs。构造函数接收 IChatClient 和 SessionStore。RecognizeIntentAsync 使用系统提示调用 LLM，通过 StructuredOutputParser 解析 JSON 响应。会话管理方法使用 InMemorySessionStore。ProcessAsync 整合意图识别 + Workflow 启动/Resume 决策。
</action>

### 任务 5.2：创建 RefundSkill

<read_first>
- Prd.md Section 4.1（RefundSkill AgentClassSkill 定义，包含 Frontmatter、Instructions、Scripts）
- openspec/changes/refund-workflow-demo/specs/refund-workflow/spec.md（AgentClassSkill 定义需求）
- MAF 参考：AgentClassSkill.cs（AgentClassSkill<TSelf> 基类）
- MAF 参考：AgentSkillFrontmatter.cs（Frontmatter 构造函数）
- MAF 参考：AgentSkillScriptAttribute.cs（[AgentSkillScript] 属性）
</read_first>

<acceptance_criteria>
- src/CallCenter.AgentHost/Skills/RefundSkill.cs 存在
- Class RefundSkill : AgentClassSkill<RefundSkill>
- Frontmatter：name="refund", description="处理用户退款请求。当用户要求退款、退货、取消订单时使用。支持查询订单、校验退款资格、计算退款金额、执行退款。"
- Instructions：告诉 LLM 退款流程的 3 个步骤
- [AgentSkillScript("get_recent_orders")] 方法：调用 IOrderMcpClient.GetRecentOrdersAsync
- [AgentSkillScript("execute_refund")] 方法：调用 IFinanceMcpClient.RefundAsync
- 方法接收 IServiceProvider 用于 DI
- 命名空间：CallCenter.AgentHost.Skills
</acceptance_criteria>

<action>
按照 Prd.md Section 4.1 精确创建 RefundSkill.cs。AgentClassSkill 包含 Frontmatter、Instructions 和 2 个脚本。
</action>

### 任务 5.3：验证 AgentHost 层编译

<acceptance_criteria>
- `dotnet build src/CallCenter.AgentHost/CallCenter.AgentHost.csproj` 成功，0 错误
</acceptance_criteria>

<action>
对 AgentHost 项目执行 dotnet build。修复任何编译错误。
</action>
