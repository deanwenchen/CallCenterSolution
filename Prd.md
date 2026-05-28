You are a senior Microsoft .NET architect.

You are implementing a production-grade AI Agent Platform using:

- Microsoft Agent Framework (MAF)
- .NET 8+
- MCP (Model Context Protocol)
- Workflow-based orchestration
- Skill-based execution model

You MUST follow strict architecture rules:

1. No business logic in Agents
2. All business operations must be implemented as Skills
3. All Skills must be executed inside Workflows
4. Workflows must be persisted and resumable (durable execution)
5. Intent layer only performs classification and entity extraction
6. Planner only selects Capability
7. Capability only selects Workflow
8. MCP Gateway is the only entry point to external systems

You are NOT allowed to:
- Call external systems directly from Agents
- Put business logic inside LLM prompts
- Mix Intent, Workflow, and Skill responsibilities





# 📘 1. System Overview（系统定义）



```
系统名称：Enterprise AI Customer Service Platform

核心目标：
基于 Microsoft Agent Framework (MAF) 构建企业级 AI 客服系统，
支持 Workflow 驱动的多业务能力编排（Refund / Subscribe / Logistics / CRM）
```



------

# 📘 2. Architecture Constraints（架构约束）

必须写死：



```
1. 所有业务必须通过 Capability 层暴露
2. 不允许 Agent 直接调用 MCP
3. 所有业务执行必须通过 Workflow Engine
4. Workflow 必须可持久化（Durable）
5. 所有 Step 必须是 Skill
6. Intent 只负责分类，不允许执行业务逻辑
```



👉 这一段非常关键，是防止 Codex 写崩的核心。

------

# 📘 3. Core Domain Model（核心模型）

让 Codex 明确对象：



```
SessionContext
WorkflowState
Intent
Capability
Workflow
Step
Skill
MCP Tool
```



------

# 📘 4. Layer Responsibilities（分层职责）

这是你架构的灵魂：



```
Conversation Gateway:
  - session
  - auth
  - context

Intent Layer:
  - classify intent
  - extract entities

Planner Agent:
  - map intent → capability

Capability Layer:
  - select workflow
  - enforce policy

Workflow Engine (MAF):
  - execute graph
  - maintain state
  - support retry / pause / resume

Skill Layer:
  - atomic business action

MCP Layer:
  - external system integration
```



------

# 📘 5. Workflow Definition Standard（必须写）

给 Codex 强约束：

### Workflow必须长这样：



```
Workflow:
  Name
  Steps[]
  Edges[]
  StateModel
```



或者 YAML：



```
name: RefundWorkflow
steps:
  - GetOrder
  - CheckRefundRule
  - WaitConfirm
  - ExecuteRefund
  - NotifyUser
```



------

# 📘 6. Skill Interface（必须统一）



```
public interface ISkill
{
    Task<SkillResult> Execute(SkillContext context);
}
```



------

# 📘 7. MCP Gateway Contract



```
所有外部系统必须通过 MCP Gateway
禁止 Skill 直接访问数据库 / HTTP API
```



------

# 📘 8. Failure Handling（非常重要）



```
Workflow必须支持：
- retry
- timeout
- compensation (Saga)
- human-in-the-loop
```





# 📘 9. 架构图

```text
┌─────────────────────────────────────────────────────────────────────┐
│                           客户接入层                                │
├─────────────────────────────────────────────────────────────────────┤
│ Web客服 │ APP │ H5 │ 小程序 │ 企业微信 │ 公众号 │ WhatsApp │ API   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                    Conversation Gateway（会话网关）                  │
├─────────────────────────────────────────────────────────────────────┤
│ 作用：                                                              │
│                                                                     │
│ • Session管理                                                       │
│ • 用户身份识别(UserId/OpenId/UnionId)                               │
│ • Token校验                                                         │
│ • 权限控制                                                          │
│ • RateLimit                                                         │
│ • 黑名单                                                            │
│ • 上下文管理                                                        │
│ • 消息路由                                                          │
│ • 多轮会话状态                                                      │
│ • 审计日志                                                          │
│                                                                     │
│ 输出：                                                              │
│ SessionContext                                                      │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                    Session State Router（核心）                     │
├─────────────────────────────────────────────────────────────────────┤
│ 作用：                                                              │
│                                                                     │
│ 判断当前用户是否“正在某个Workflow中”                                │
│                                                                     │
│ IF workflow active:                                                 │
│      → 直接进入Workflow Engine                                      │
│                                                                     │
│ ELSE:                                                               │
│      → 进入 Intent Router                                           │
│                                                                     │
│ Session状态示例：                                                   │
│                                                                     │
│ workflow = RefundWorkflow                                           │
│ step = WAIT_USER_CONFIRM                                            │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
                 ▼                           ▼

┌──────────────────────────────┐   ┌────────────────────────────────┐
│ Active Workflow               │   │ Intent Router                  │
│ （已有流程继续执行）          │   │ （无流程时才识别意图）         │
└──────────────┬───────────────┘   └──────────────┬─────────────────┘
               │                                   │
               ▼                                   ▼

┌─────────────────────────────────────────────────────────────────────┐
│                    Intent Recognition Layer                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ 第一层：Rule Router（最快）                                         │
│ • 关键词                                                            │
│ • Regex                                                             │
│ • 命令匹配                                                          │
│                                                                     │
│ 第二层：Embedding Intent Model                                      │
│ • 小模型分类                                                        │
│ • 向量匹配                                                          │
│ • 低成本                                                            │
│                                                                     │
│ 第三层：LLM Intent Agent                                            │
│ • 复杂语义理解                                                      │
│ • 多意图                                                            │
│ • 模糊表达                                                          │
│                                                                     │
│ 输出：                                                              │
│ intent + entities                                                   │
│                                                                     │
│ 例如：                                                              │
│ {                                                                   │
│   "intent":"refund",                                                │
│   "orderId":"A001"                                                  │
│ }                                                                   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                     Planner Agent（任务规划）                       │
├─────────────────────────────────────────────────────────────────────┤
│ 作用：                                                              │
│                                                                     │
│ • 选择 Capability                                                   │
│ • 规划业务任务                                                      │
│ • 判断是否需要人工                                                  │
│ • 判断风险等级                                                      │
│                                                                     │
│ 输出：                                                              │
│                                                                     │
│ Refund Capability                                                   │
│ Logistics Capability                                                │
│ Invoice Capability                                                  │
│ CRM Capability                                                      │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                  Capability Layer（业务能力层）                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ Refund Capability                                                   │
│ ├─ 定义退款业务能力                                                 │
│ ├─ 定义退款规则                                                     │
│ ├─ 定义退款权限                                                     │
│ ├─ 定义退款策略                                                     │
│ ├─ Workflow Selector                                                │
│ └─ Workflow Registry                                                │
│                                                                     │
│ Logistics Capability                                                │
│ Invoice Capability                                                  │
│ CRM Capability                                                      │
│ Member Capability                                                   │
│ Coupon Capability                                                   │
│ WeCom Capability                                                    │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                 Workflow Selector（流程选择器）                     │
├─────────────────────────────────────────────────────────────────────┤
│ 作用：                                                              │
│                                                                     │
│ 根据业务规则选择Workflow                                            │
│                                                                     │
│ 例如：                                                              │
│                                                                     │
│ 金额 < 100                                                          │
│   → StandardRefundWorkflow                                          │
│                                                                     │
│ 金额 > 1000                                                         │
│   → ManualApprovalWorkflow                                          │
│                                                                     │
│ VIP用户                                                             │
│   → VIPRefundWorkflow                                               │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                 Workflow Engine（流程执行引擎）                     │
├─────────────────────────────────────────────────────────────────────┤
│ 作用：                                                              │
│                                                                     │
│ • 执行Workflow                                                      │
│ • 执行Step                                                          │
│ • 状态机控制                                                        │
│ • 流程暂停/恢复                                                     │
│ • 超时处理                                                          │
│ • Retry                                                             │
│ • Saga补偿                                                          │
│ • 人工审批                                                          │
│ • 审计                                                              │
│                                                                     │
│ Workflow示例：                                                      │
│                                                                     │
│ [INIT]                                                              │
│    ↓                                                                │
│ [GET_ORDER]                                                         │
│    ↓                                                                │
│ [CHECK_REFUND_RULE]                                                 │
│    ↓                                                                │
│ [WAIT_USER_CONFIRM]                                                 │
│    ↓                                                                │
│ [EXECUTE_REFUND]                                                    │
│    ↓                                                                │
│ [RESTORE_COUPON]                                                    │
│    ↓                                                                │
│ [SEND_NOTIFICATION]                                                 │
│    ↓                                                                │
│ [DONE]                                                              │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                     Step / Skill Layer                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ 每个Step调用一个Skill                                                │
│                                                                     │
│ QueryOrderSkill                                                     │
│ • 查询订单                                                          │
│                                                                     │
│ CheckRefundSkill                                                    │
│ • 校验退款资格                                                      │
│                                                                     │
│ CreateRefundSkill                                                   │
│ • 发起退款                                                          │
│                                                                     │
│ RestoreCouponSkill                                                  │
│ • 恢复优惠券                                                        │
│                                                                     │
│ SendNotificationSkill                                               │
│ • 通知用户                                                          │
│                                                                     │
│ QueryLogisticsSkill                                                 │
│ • 查询物流                                                          │
│                                                                     │
│ AddCRMTagSkill                                                      │
│ • CRM标签                                                           │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                       MCP Gateway                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ 定义：统一业务系统接入层                                            │
│                                                                     │
│ Order MCP                                                           │
│ ├─ GetOrder                                                         │
│ ├─ GetOrders                                                        │
│ └─ UpdateOrder                                                      │
│                                                                     │
│ Finance MCP                                                         │
│ ├─ Refund                                                           │
│ └─ QueryRefund                                                      │
│                                                                     │
│ Member MCP                                                          │
│ ├─ GetMember                                                        │
│ ├─ AddPoints                                                        │
│ └─ RestoreCoupon                                                    │
│                                                                     │
│ CRM MCP                                                             │
│ ├─ AddTag                                                           │
│ └─ CreateTicket                                                     │
│                                                                     │
│ WeCom MCP                                                           │
│ ├─ SendMessage                                                      │
│ └─ AddContactTag                                                    │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼

┌─────────────────────────────────────────────────────────────────────┐
│                       Business Systems                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ 订单中心                                                            │
│ 支付中心                                                            │
│ ERP                                                                 │
│ CRM                                                                 │
│ 会员中心                                                            │
│ 企业微信                                                            │
│ 发票系统                                                            │
│ 库存系统                                                            │
│ 内容平台                                                            │
│ 数据中台                                                            │
│ 营销平台                                                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘



【旁路系统（生产环境必须）】

┌─────────────────────────────────────────────────────────────────────┐
│                      Knowledge Layer                                │
├─────────────────────────────────────────────────────────────────────┤
│ FAQ                                                                 │
│ RAG                                                                 │
│ 产品知识库                                                          │
│ 退款规则库                                                          │
│ 帮助中心                                                            │
└─────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│                    Observability Layer                              │
├─────────────────────────────────────────────────────────────────────┤
│ Prometheus                                                          │
│ OpenTelemetry                                                       │
│ Langfuse                                                            │
│ Application Insights                                                │
│ Trace                                                               │
│ 审计日志                                                            │
└─────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│                     Human Agent Layer                               │
├─────────────────────────────────────────────────────────────────────┤
│ • 人工客服接管                                                      │
│ • 人工审批                                                          │
│ • 人工兜底                                                          │
│ • 高风险审核                                                        │
└─────────────────────────────────────────────────────────────────────┘
```