# Spec: Refund Workflow

## ADDED Requirements

### Requirement: Refund Workflow Execution

系统 SHALL 支持退款流程的完整执行，基于 MAF Workflow 编排以下 6 个步骤：
1. 查询订单信息（GetOrderExecutor）
2. 校验退款资格（CheckRefundRuleExecutor）：7 天内、已签收、非特殊品类
3. 等待用户确认（RequestPort: WAIT_USER_CONFIRM）
4. 执行退款（ExecuteRefundExecutor → Finance MCP）
5. 恢复优惠券（RestoreCouponExecutor → Member MCP）
6. 发送退款通知（SendNotificationExecutor）

**场景：完整退款流程（订单 A001）**
- GIVEN 用户提供了有效订单号 A001
- WHEN 用户发起退款请求
- THEN 系统查询到订单信息：订单 A001, 蓝牙耳机 ¥299, 3 天前购买, 已签收
- AND 校验通过（3 天内 ✓ 已签收 ✓ 普通品类 ✓）
- AND 提示用户确认退款金额 ¥299.00
- AND 用户确认后执行退款，返回退款编号 REF-xxx
- AND 恢复优惠券
- AND 发送成功通知 "退款 REF-xxx 已处理完成"
- AND 通过 EventBus 发布 RefundCompletedEvent

### Requirement: Dynamic Parameter Request

当 Workflow 执行步骤缺少必需参数时，系统 SHALL 自动暂停流程并通过 RequestPort 向用户询问缺失参数，用户回复后从暂停处恢复执行。

**场景：用户未提供订单号**
- GIVEN 用户说"我要退款"但未提供订单号
- WHEN GetOrderExecutor 检测到 orderId 为空
- THEN 发送 RefundSignal.NeedOrderId 回到 RefundInfoPort
- AND 控制台提示"请提供订单号："
- AND 用户提供订单号后流程恢复继续执行

### Requirement: Refund Rule Validation

系统 SHALL 按以下规则校验退款资格：
- 购买时间在 7 天内
- 订单状态为"已签收"（delivered）
- 非特殊品类（custom 等不可退品类）

退款金额 SHALL = 商品原价 - 优惠券分摊金额。

**场景：超过 7 天不可退（订单 A002）**
- GIVEN 订单 A002（30 天前购买，已签收）
- WHEN 校验退款资格
- THEN 返回 IsEligible=false, Reason="超过 7 天退货期"
- AND 流程终止，输出拒绝原因

**场景：未签收不可退（订单 A003）**
- GIVEN 订单 A003（1 天前购买，状态=shipped）
- WHEN 校验退款资格
- THEN 返回 IsEligible=false, Reason="订单未签收"
- AND 流程终止，输出拒绝原因

### Requirement: User Confirmation

系统 SHALL 在执行退款前通过 RequestPort 暂停流程，向用户展示订单信息、商品名称、退款金额，等待用户确认或取消。

**场景：用户确认退款**
- GIVEN 退款规则校验通过，退款金额 ¥299.00
- WHEN 系统展示确认信息 "订单 A001: 蓝牙耳机 ¥299.00，确认退款？(回复'确认'或'取消')"
- AND 用户回复"确认"
- THEN 流程继续执行退款

**场景：用户取消退款**
- GIVEN 退款规则校验通过
- WHEN 系统展示确认信息
- AND 用户回复"取消"
- THEN 发送 RefundSignal.Cancelled 信号
- AND 流程终止，输出取消通知 "退款已取消"

### Requirement: Entry Point and Intent Routing

系统 SHALL 实现统一的 Entry Point 作为 AIAgent 和 Workflow 的桥梁，处理每次用户请求：

1. 检查 Session.StateBag["activeWorkflow"]
   - 有且意图匹配 → Resume Workflow（从 checkpoint 恢复）
   - 有但意图不匹配 → 终止旧流程 → 走 Intent Router
   - 无 → 走 Intent Router
2. Intent Router 使用 LLM Agent 识别意图，StructuredOutputParser 转为强类型 IntentResult
   - 有意图 → 匹配 Workflow → 构造初始消息类型
   - 无意图（闲聊/问候）→ 走对话 Agent 自由回复

**场景：新启动 Workflow**
- GIVEN 用户首次说"我要退款，订单 A001"
- WHEN Entry Point 处理请求
- AND StateBag["activeWorkflow"] 为空
- AND Intent Router 识别为 refund 意图
- THEN StateBag["activeWorkflow"] = "RefundWorkflow"
- AND InProcessExecution.RunAsync(workflow, RefundIntent{OrderId="A001"})

**场景：Resume Workflow**
- GIVEN 用户正在退款流程中（StateBag["activeWorkflow"] = "RefundWorkflow"）
- WHEN 用户提供订单号补参
- AND Entry Point 检测到意图仍为 refund，匹配当前 activeWorkflow
- THEN 从 RedisSessionStore 加载 checkpoint
- AND InProcessExecution.RunAsync(workflow, userMessage, checkpoint)

**场景：识别退款意图**
- GIVEN 用户消息包含退款意图（如"我要退款"）
- WHEN Intent Router 分析消息
- THEN 返回 IntentResult { intent="refund", workflow="RefundWorkflow", entities: {orderId} }

**场景：识别闲聊意图**
- GIVEN 用户消息无业务意图（如"你好"或"今天天气怎么样"）
- WHEN Intent Router 分析消息
- THEN 返回 IntentResult { intent="no_intent" }
- AND 不启动任何 Workflow
- AND 路由到对话 Agent 自然回复

### Requirement: MCP Client Layer

系统 SHALL 在 Shared/Mcp/ 目录下定义 MCP Client 接口，作为外部系统调用的统一封装层。多个 Workflow 共享同一套后端服务 SDK。

MCP Client 接口 SHALL 包括：
- IOrderMcpClient.GetOrderAsync(orderId) → OrderInfo
- IOrderMcpClient.GetRecentOrdersAsync(userId) → List<OrderInfo>
- IFinanceMcpClient.RefundAsync(orderId, amount) → RefundResult
- IMemberMcpClient.GetCouponAsync(userId) → CouponInfo
- IMemberMcpClient.RestoreCouponAsync(userId, couponId) → bool

**场景：Demo 使用 Mock 实现**
- GIVEN 这是 Demo 阶段
- WHEN Executor 调用 MCP Client
- THEN 使用内存 Mock 实现替代真实后端
- AND 接口签名与 PRD 定义一致

### Requirement: AgentClassSkill Definition

系统 SHALL 为每个业务模块定义 AgentClassSkill，包含 Frontmatter（元数据）、Instructions（LLM 使用指南）、Scripts（可执行操作）。

**RefundSkill 定义：**
- Frontmatter: name="refund", description="处理用户退款请求。当用户要求退款、退货、取消订单时使用。支持查询订单、校验退款资格、计算退款金额、执行退款。"
- Instructions: 告诉 LLM 退款流程步骤
- Scripts:
  - get_recent_orders(userId) → 调用 IOrderMcpClient 获取最近订单列表
  - execute_refund(orderId, amount) → 调用 IFinanceMcpClient 执行退款

**Skill 注册：**
- Skill 通过 AgentSkillsProviderBuilder.UseSkill() 注册
- LLM 通过 Skill 的 Frontmatter description 自动发现和选择合适的 Workflow

### Requirement: Agent Pipeline

系统 SHALL 实现标准 Agent 中间件管道，用于 AIAgent（意图识别/对话）的中间件处理。管道顺序为：

1. SafetyInputFilter — PII 脱敏、关键词拦截、Prompt Injection 检测
2. LoggingAgent — 记录操作日志
3. CompactionProvider — 超过 token 阈值时压缩历史消息
4. ToolApprovalAgent — 工具调用审批规则检查
5. LLM + StructuredOutputParser — 实际的模型调用，LLM 输出自动转为强类型
6. SafetyOutputFilter — 输出 PII 脱敏、高风险内容拦截

注意：此管道用于 AIAgent，不用于 Workflow 执行。Workflow 有自己的执行流（Executor → Edge → Executor）。

### Requirement: Safety Pipeline

系统 SHALL 实现输入/输出双路安全过滤：

**Input Filter（用户消息 → 过滤 → 传给下游）：**
- PII 脱敏：手机号（1[3-9]\d）\d{4}(\d{4}) → $1****$2，身份证、银行卡脱敏
- 关键词拦截：辱骂、恶意攻击等关键词
- Prompt Injection 检测："忽略之前指令"等注入模式

**Output Filter（AI 回复 → 过滤 → 返回用户）：**
- PII 脱敏：内部系统地址、内部工号脱敏
- 敏感回复拦截：不确定/高风险内容转人工
- 格式规范化：统一输出格式

### Requirement: BusinessEventBus

系统 SHALL 实现业务事件发布/订阅机制，所有业务模块通过 EventBus 对外发布事件。

**事件 SHALL 携带上下文：** sessionId、userId、orderId、金额等

**RefundCompletedEvent：**
- 字段：SessionId, UserId, OrderId, RefundAmount
- 订阅者示例：发短信通知用户 "您的退款 ¥X 已处理完成"

**RiskAlertEvent：**
- 字段：SessionId, UserId, OrderId, AlertType, Details
- 订阅者示例：通知主管 "高风险退款告警"

**场景：订阅退款完成事件**
- GIVEN 退款流程执行完成
- WHEN SendNotificationExecutor 调用 EventBus.PublishAsync(RefundCompletedEvent)
- THEN 所有订阅者收到事件
- AND ConsoleDemo 订阅者输出 "[EVENT] 退款完成: 订单A001, 金额 ¥299.00"

### Requirement: Session and Checkpoint

系统 SHALL 通过 Session Store 管理会话状态和 Workflow Checkpoint：

- Session.StateBag 保存 activeWorkflow、currentStep 等状态
- Checkpoint 支持流程状态持久化，崩溃恢复、跨请求持久化
- Demo 阶段使用 InMemorySessionStore，生产使用 RedisSessionStore

**场景：清除 activeWorkflow**
- GIVEN 退款流程执行完成
- WHEN Workflow 到达 DONE
- THEN StateBag["activeWorkflow"] = null
- AND 下次用户消息走 Intent Router

### Requirement: StructuredOutputParser

系统 SHALL 提供 StructuredOutputParser 将 LLM JSON 输出转为强类型对象：
- 自动在 system prompt 中注入 JSON Schema
- 自动解析 LLM 返回的 JSON → TOutput
- 解析失败自动重试

**场景：意图识别强类型输出**
- GIVEN Intent Router 调用 LLM
- WHEN LLM 返回 JSON: {"intent": "refund", "orderId": "A001"}
- THEN StructuredOutputParser 解析为 IntentResult { Intent="refund", OrderId="A001" }
- AND 调用方直接拿到类型安全的对象

### Requirement: Compaction

系统 SHALL 提供消息压缩功能，当聊天历史超过 token 阈值时自动压缩：
- 超过 8000 token 触发压缩
- 保留最近 8 轮对话不压缩
- 使用小模型（如 gpt-4o-mini）做摘要，降低成本

### Requirement: Audit Logger

系统 SHALL 自动捕获每个 Workflow Step 的输入/输出，写入防篡改审计存储。

### Requirement: Saga Compensation

系统 SHALL 支持 Saga 补偿机制：当 Workflow 某步骤失败时，自动触发补偿动作。

**场景：退款执行失败**
- GIVEN ExecuteRefundExecutor 调用 Finance MCP 超时
- WHEN Executor 抛出异常，发出 ExecutorFailedEvent
- THEN Saga 补偿机制自动触发：恢复优惠券（如果已扣减）、通知用户 "退款处理中，请稍后查看"、标记流程为 PendingRetry
- AND 后台重试策略：第 1 次重试 1 分钟后，第 2 次 5 分钟后，第 3 次 30 分钟后
- AND 超过最大重试次数 → 标记为 Failed → 转人工

### Requirement: Exception Handling — User Changes Intent Mid-Workflow

系统 SHALL 支持用户中途切换意图：

**场景：退款中改换货**
- GIVEN 用户正在 RefundWorkflow 中（等待确认）
- WHEN 用户说"我不想退了，我要换货"
- THEN 系统识别到新意图 "exchange"
- AND 终止当前 RefundWorkflow（标记为 UserCancelled）
- AND 清除 StateBag["activeWorkflow"]
- AND 进入 ExchangeWorkflow
- AND RedisSessionStore 中记录意图切换（用于审计）

### Requirement: Exception Handling — Workflow Timeout

系统 SHALL 支持 Workflow 超时未响应处理：

**场景：用户 30 分钟未回复**
- GIVEN 系统发送"确认退款 ¥100？"后等待用户回复
- WHEN Gateway 检测到 Session 最后活跃时间 > 30 分钟
- THEN 自动发送 "您还在吗？回复'确认'继续退款流程，回复其他重新开始"
- AND 再过 30 分钟未回复 → 终止流程，标记为 Expired
- AND 通知用户 "您的退款请求已超时，如需退款请重新发起"

### Requirement: Exception Handling — User Reply Out of Scope

系统 SHALL 支持用户回复不在预期范围内的情况：

**场景：确认时回复非预期内容**
- GIVEN 系统提示"确认退款 ¥100？回复'确认'或'取消'"
- WHEN 用户回复"你们这服务太差了，我要投诉"
- THEN LLM 识别为新意图 "complaint"
- AND 当前流程挂起（不终止，保留状态）
- AND 进入投诉流程（转人工或走投诉 Workflow）

### Requirement: Business Module Extensibility

系统 SHALL 支持新增业务模块，新增业务与现有业务互不影响。

**新增业务步骤（以换货 Exchange 为例）：**
1. 复制 Workflows/Refund/ → Workflows/Exchange/
2. 重命名所有文件中的 Refund → Exchange
3. 修改 ExchangeWorkflow.cs 的流程步骤
4. 修改 Executors 中的业务逻辑
5. 新增 AgentHost/Skills/ExchangeSkill.cs
6. 在 Program.cs 中添加一行注册代码
7. 完成 — 退款模块完全不受影响

### Requirement: Mock Data Service

系统 SHALL 提供 Mock 服务替代真实后端，支持以下测试场景：

**MockOrderService 数据：**
- A001: 蓝牙耳机 ¥299, 3 天前, 已签收, 普通品类 → **可退**
- A002: 定制 T 恤 ¥159, 30 天前, 已签收, 已签收 → **超过 7 天不可退**
- A003: 手机壳 ¥39, 1 天前, 运输中 (shipped) → **未签收不可退**

**MockFinanceService：**
- RefundAsync(orderId, amount) → RefundResult { RefundId: "RF-xxx", Amount: amount, Status: "success" }

**MockMemberService：**
- GetUserCouponAsync(userId) → CouponInfo { CouponId: "CPN-2024", Discount: 20.00 }
- RestoreCouponAsync(userId, couponId) → true
