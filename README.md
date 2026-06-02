# CallCenter AI

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。用户通过聊天窗口发起业务请求（如"我要退款"），系统自动识别意图、启动对应工作流、在需要时追问缺失参数、最终完成业务操作 — 整个链路无需人工干预。

> **设计理念**：今天加退款，明天加换货，互不影响，结构清晰。

---

## 一、核心特性

| 特性 | 说明 |
|------|------|
| **LLM 意图识别** | 使用 DashScope（通义千问）识别用户意图（退款/换货/闲聊等） |
| **工作流编排** | 基于 MAF Workflow 构建有向图，每个节点是确定性的 Executor |
| **RequestPort 人工介入** | 流程需要用户确认时自动暂停，等用户回复后恢复执行 |
| **意图切换** | 用户在流程中途切换意图时，自动终止旧流程、启动新流程 |
| **超时管理** | 30 分钟警告、60 分钟终止，防止会话永远挂起 |
| **6 层安全 Pipeline** | 用户输入经过 PII 脱敏 → 关键词拦截 → 注入检测 → LLM → 输出脱敏 |
| **审计日志** | 每步操作写入 SHA256 哈希链，支持完整性校验 |
| **Saga 补偿** | 支持失败重试 + 补偿回滚（如退款失败后恢复优惠券） |
| **事件总线** | 业务事件解耦发布与订阅，方便对接通知、分析等外部系统 |

---

## 二、以"退款"为例，看完整流程

### 用户视角：一次对话长什么样

```
用户: 我要退款，订单A001

系统: 订单 A001: 蓝牙耳机 ¥299.00
      确认退款？(回复'确认'或'取消'):

用户: 确认

系统: 退款已处理完成，预计 3-5 个工作日到账
```

如果用户一开始没说订单号：

```
用户: 我要退款

系统: 请提供订单号:

用户: A001

系统: 订单 A001: 蓝牙耳机 ¥299.00
      确认退款？(回复'确认'或'取消'):

用户: 确认

系统: 退款已处理完成，预计 3-5 个工作日到账
```

### 系统视角：数据是怎么流转的

```
用户输入: "我要退款，订单A001"
  │
  ▼
[1] EntryPoint.ProcessAsync()
  │  ├── 更新 lastActivity
  │  ├── 检查超时（30min 警告 / 60min 终止）
  │  ├── 检查是否有活跃工作流 → 没有
  │  └── 调用 LLM 意图识别
  │        └─ 返回: {"intent": "refund", "workflow": "RefundWorkflow", "orderId": "A001"}
  │
  ▼
[2] 识别到 refund 意图 → 启动 RefundWorkflow
  │  ├── 设置 activeWorkflow = "RefundWorkflow"
  │  └── 构建初始消息: RefundIntent(OrderId="A001", UserId="U100")
  │
  ▼
[3] GetOrderExecutor — 查询订单
  │  ├── 调用 IOrderMcpClient.GetOrderAsync("A001")
  │  ├── 保存到 state: order = { 蓝牙耳机, ¥299.00, delivered, 3天前 }
  │  └── 发送消息 → CheckRefundRuleExecutor
  │
  ▼
[4] CheckRefundRuleExecutor — 校验退款资格
  │  ├── 规则1: 是否在 7 天内？→ ✅（3 天前下单）
  │  ├── 规则2: 是否已签收？→ ✅（delivered）
  │  ├── 规则3: 是否定制商品？→ ✅（electronics，不是 custom）
  │  ├── 计算退款金额: ¥299 - ¥20(优惠券) = ¥279
  │  └── 发送消息 → WaitUserConfirmExecutor
  │
  ▼
[5] WaitUserConfirmExecutor — 发送确认请求
  │  └── 通过 ConfirmPort 发出: ConfirmRefundRequest("A001", "蓝牙耳机", ¥279)
  │        └─ RequestPort → RequestInfoEvent → 控制台显示给用户
  │
  ▼
[6] 用户回复"确认" → 外部响应注入
  │  └── 创建 ExternalResponse: UserConfirmation(Confirmed=true)
  │
  ▼
[7] ExecuteRefundExecutor — 执行退款
  │  ├── 调用 IFinanceMcpClient.RefundAsync("A001", ¥279)
  │  ├── 保存 refundResult 到 state
  │  └── 发送消息 → RestoreCouponExecutor
  │
  ▼
[8] RestoreCouponExecutor — 恢复优惠券
  │  ├── 调用 IMemberMcpClient.RestoreCouponAsync("U100", "CPN-2024")
  │  └── 发送消息 → SendNotificationExecutor
  │
  ▼
[9] SendNotificationExecutor — 通知用户
  │  ├── 发布 EventBus: RefundCompletedEvent
  │  └── 输出: "退款 RF-xxx 已处理完成，预计 3-5 个工作日到账"
  │
  ▼
[DONE] 流程结束，清除 activeWorkflow
```

### 关键代码在哪里

| 步骤 | 文件 | 说明 |
|------|------|------|
| [1] | `AgentHost/EntryPoint.cs` | 意图识别 + 路由决策 |
| [2] | `Workflows/Refund/RefundWorkflow.cs` | 工作流图定义 |
| [3] | `Workflows/Refund/Executors/GetOrderExecutor.cs` | 订单查询 |
| [4] | `Workflows/Refund/Executors/CheckRefundRuleExecutor.cs` | 规则校验 |
| [5] | `Workflows/Refund/Executors/WaitUserConfirmExecutor.cs` | 用户确认 |
| [7] | `Workflows/Refund/Executors/ExecuteRefundExecutor.cs` | 执行退款 |
| [8] | `Workflows/Refund/Executors/RestoreCouponExecutor.cs` | 恢复优惠券 |
| [9] | `Workflows/Refund/Executors/SendNotificationExecutor.cs` | 通知用户 |
| 端口 | `Workflows/Refund/RefundMessages.cs` | 消息类型定义 |
| Mock 数据 | `Shared/Services/MockOrderService.cs` | 三个测试订单（A001/A002/A003） |

---

## 三、系统架构

```
┌─────────────────────────────────────────────────────────────────────┐
│                    CallCenter AI 架构                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐                                                  │
│  │ ConsoleDemo  │  ← 当前演示入口（终端对话）                      │
│  └──────┬───────┘                                                  │
│         │                                                          │
│         ▼                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐        │
│  │  EntryPoint  │───→│ SkillsProvider│───→│ RefundSkill  │        │
│  │  (意图路由)  │    │ (技能注册)   │    │ ExchangeSkill│        │
│  └──────┬───────┘    └──────────────┘    └──────────────┘        │
│         │                                                          │
│         ▼                                                          │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │              6 层 Agent Pipeline                          │   │
│  │  SafetyInput → Logging → Compaction → ToolApproval →     │   │
│  │  LLM (DashScope Qwen) → SafetyOutput                      │   │
│  └───────────────────────────────────────────────────────────┘   │
│         │                                                          │
│         ▼                                                          │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │              Workflow Engine (MAF)                        │   │
│  │  WorkflowBuilder → Executor → RequestPort → Checkpoint   │   │
│  └───────────────────────────────────────────────────────────┘   │
│         │                                                          │
│    ┌────┼────┐                                                    │
│    ▼    ▼    ▼                                                    │
│  Order  Finance  Member   ← Mock MCP Services                     │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│                    横切关注点（Framework）                           │
│  EventBus · AuditLogger · SagaBuilder · SafetyPipeline ·           │
│  Compaction · SessionStore · StructuredOutputParser                │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 四、项目结构

```
src/
├── CallCenter.AgentHost/            # Agent 托管与编排
│   ├── EntryPoint.cs                # 统一入口：意图识别 + 工作流路由
│   └── Skills/
│       ├── RefundSkill.cs           # 退款技能（已实现）
│       └── ExchangeSkill.cs         # 换货技能（骨架）
│
├── CallCenter.Framework/            # 框架核心（横切关注点）
│   ├── Audit/                       # 审计日志（SHA256 哈希链）
│   ├── Compaction/                  # 对话压缩（8000 token / 8 轮阈值）
│   ├── EventBus/                    # 业务事件总线（发布/订阅）
│   ├── Logging/                     # JSONL 操作日志
│   ├── Parsing/                     # LLM 结构化输出解析器
│   ├── Pipeline/                    # 6 层聊天管道工厂
│   ├── Safety/                      # 安全过滤（PII / 关键词 / 注入检测）
│   ├── Saga/                        # 失败重试 + 补偿框架
│   ├── Session/                     # 会话存储（内存 / Redis 占位）
│   └── ToolApproval/                # 工具调用审批（空壳）
│
├── CallCenter.Workflows/            # 业务流程（业务规则在这里）
│   ├── Refund/                      # 退款流程（已完成）
│   │   ├── RefundWorkflow.cs        # 工作流图定义
│   │   ├── RefundMessages.cs        # 消息类型
│   │   └── Executors/               # 7 个执行器
│   └── Exchange/                    # 换货流程（骨架，v2 实现）
│       ├── ExchangeWorkflow.cs
│       ├── ExchangeMessages.cs
│       └── Executors/               # 7 个执行器（骨架）
│
├── CallCenter.Shared/               # 共享模型与接口
│   ├── Models/                      # OrderInfo / RefundResult / CouponInfo
│   ├── Mcp/                         # MCP Client 接口
│   └── Services/                    # Mock 实现（订单/财务/会员）
│
└── CallCenter.ConsoleDemo/          # 演示入口
    └── Program.cs                   # 主循环 + 工作流执行驱动
```

---

## 五、技术栈

| 项目 | 技术 |
|------|------|
| 框架 | .NET 10.0 |
| Agent SDK | Microsoft Agent Framework (MAF) .NET SDK（源码引用） |
| LLM | DashScope（通义千问）OpenAI 兼容接口 |
| 构建 | 5 个项目，0 errors, 0 warnings |
| 语言 | C# |

---

## 六、如何运行

### 前置条件

1. .NET 10.0 SDK
2. DashScope API Key（[获取地址](https://dashscope.console.aliyun.com/)）

### 启动

```bash
# 设置环境变量
export DASHSCOPE_API_KEY="sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# 可选：设置模型名称（默认 qwen3-vl-flash）
export DASHSCOPE_MODEL_NAME="qwen-plus"

# 运行演示
dotnet run --project src/CallCenter.ConsoleDemo
```

### 测试场景

| 场景 | 输入 | 预期结果 |
|------|------|----------|
| 完整退款 | `我要退款，订单A001` → `确认` | 退款成功 |
| 追问订单号 | `我要退款` → `A001` → `确认` | 退款成功 |
| 超期拒绝 | `我要退款，订单A002` | "超过 7 天退货期" |
| 未签收拒绝 | `我要退款，订单A003` | "订单未签收" |
| 取消流程 | `我要退款，订单A001` → `取消` | "退款流程已结束" |
| 闲聊 | `你好` | 自然回复，不启动流程 |

---

## 七、Mock 订单数据

| 订单号 | 商品 | 金额 | 下单时间 | 状态 | 品类 | 有优惠券 | 退款结果 |
|--------|------|------|----------|------|------|----------|----------|
| A001 | 蓝牙耳机 | ¥299 | 3 天前 | delivered | electronics | ✅ | ✅ 可退（¥279） |
| A002 | 定制T恤 | ¥159 | 30 天前 | delivered | custom | ❌ | ❌ 超期 + 定制 |
| A003 | 手机壳 | ¥39 | 1 天前 | shipped | electronics | ❌ | ❌ 未签收 |

---

## 八、已实现功能清单

### v1.0 退款工作流

- ✅ 6 步工作流执行（查单 → 规则校验 → 用户确认 → 执行退款 → 恢复优惠券 → 通知）
- ✅ LLM 意图识别（DashScope Qwen + 结构化 JSON 解析）
- ✅ Session 管理（活跃工作流跟踪、超时检测）
- ✅ 断点恢复（CheckpointManager 超步骤持久化）
- ✅ Mock 服务（订单/财务/会员 3 个测试场景）
- ✅ EventBus（RefundCompletedEvent 发布/订阅）

### v1.1 能力增强

- ✅ 意图切换（中途换意图 → 终止旧流程 → 启动新流程）
- ✅ 超时管理（30 分钟警告 / 60 分钟终止）
- ✅ 6 层安全 Pipeline（PII 脱敏 + 关键词拦截 + 注入检测）
- ✅ 对话压缩（CompactionProvider，8000 token / 8 轮阈值）
- ✅ 审计日志（SHA256 哈希链，VerifyChainAsync 验证）
- ✅ Saga 补偿（失败重试 + 补偿回滚框架）
- ✅ 新业务扩展指南（换货骨架已就绪）

---

## 九、路线图

| 版本 | 计划内容 |
|------|----------|
| v2.0 | Redis Session 持久化 + SafetyOutput 敏感内容拦截 |
| v2.x | 真实 MCP Server 接入 + Web/Gateway 接入层 |
| v3.x | 换货/物流/发票等业务模块完整实现 |

---

## 十、相关文档

- [PRD / 架构设计](Prd.md)
- [MILESTONES](.planning/MILESTONES.md)
- [PROJECT.md](.planning/PROJECT.md)
- [ROADMAP](.planning/ROADMAP.md)
