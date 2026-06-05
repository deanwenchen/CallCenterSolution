# CallCenter AI

基于 Microsoft Agent Framework (MAF) .NET SDK 构建的智能客服系统。用户通过聊天窗口发起业务请求（如"我要退款"），系统自动识别意图、启动对应工作流、在需要时追问缺失参数、最终完成业务操作 — 整个链路无需人工干预。

> **设计理念**：今天加退款，明天加换货，互不影响，结构清晰。

**当前版本**: v3.0 — 2026-06-05

---

## 一、核心特性

| 特性 | 说明 |
|------|------|
| **LLM 意图识别** | 使用 DashScope（通义千问）识别用户意图（退款/换货/闲聊等） |
| **工作流编排** | 基于 MAF Workflow 构建有向图，每个节点是确定性的 Executor |
| **RequestPort 人工介入** | 流程需要用户确认时自动暂停，等用户回复后恢复执行 |
| **意图切换** | 用户在流程中途切换意图时，自动终止旧流程、启动新流程 |
| **超时管理** | 30 分钟警告、60 分钟终止，防止会话永远挂起 |
| **6 层安全 Pipeline** | 输入安全 → 日志 → 压缩 → 工具审批 → LLM → 输出安全 |
| **审计日志** | 每步操作写入 SHA256 哈希链，支持完整性校验 |
| **Saga 补偿** | 支持失败重试 + 补偿回滚（如退款失败后恢复优惠券） |
| **事件总线** | 业务事件解耦发布与订阅，方便对接通知、分析等外部系统 |
| **Web API + SSE** | RESTful `/chat` 端点 + SSE `/chat/stream` 实时推送 9 种工作流事件 |

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
[1] CallCenterService.ProcessAsync()
  │  ├── 更新 lastActivity
  │  ├── 检查超时（30min 警告 / 60min 终止）
  │  ├── 检查是否有活跃工作流 → 没有
  │  └── 调用 LLM 意图识别
  │        └─ 返回: {"intent": "refund", "workflow": "RefundWorkflow", "OrderId": "A001"}
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
| [1] | `AgentHost/CallCenterService.Intent.cs` | 意图识别 + 统一入口 |
| [1] | `AgentHost/CallCenterService.Routing.cs` | 路由决策 + 超时检测 |
| [2] | `Workflows/Refund/RefundWorkflow.cs` | 工作流图定义 |
| [3] | `Workflows/Refund/Executors/GetOrderExecutor.cs` | 订单查询 |
| [4] | `Workflows/Refund/Executors/CheckRefundRuleExecutor.cs` | 规则校验 |
| [5] | `Workflows/Refund/Executors/WaitUserConfirmExecutor.cs` | 用户确认 |
| [7] | `Workflows/Refund/Executors/ExecuteRefundExecutor.cs` | 执行退款 |
| [8] | `Workflows/Refund/Executors/RestoreCouponExecutor.cs` | 恢复优惠券 |
| [9] | `Workflows/Refund/Executors/SendNotificationExecutor.cs` | 通知用户 |
| 事件循环 | `AgentHost/CallCenterService.Execution.cs` | DriveLoopAsync + HandleEventAsync |
| 交互处理 | `AgentHost/CallCenterService.Interaction.cs` | HandleRequestAsync |
| SSE 流式 | `AgentHost/CallCenterService.Streaming.cs` | ProcessStreamingAsync + SSE 序列化 |
| 端口 | `Workflows/Refund/RefundMessages.cs` | 消息类型定义 |
| Mock 数据 | `Shared/Services/MockOrderService.cs` | 三个测试订单（A001/A002/A003） |

---

## 三、系统架构

```
┌─────────────────────────────────────────────────────────────────────┐
│                    CallCenter AI 架构                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐        │
│  │ ConsoleDemo  │    │  WebApi      │    │ AgentHost    │        │
│  │ (终端演示)   │    │ (REST + SSE) │    │ (DI 编排)    │        │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘        │
│         │                   │                   │                  │
│         └───────────────────┴───────────────────┘                  │
│                             │                                      │
│                             ▼                                      │
│  ┌───────────────────────────────────────────────────────────┐   │
│  │              CallCenterService (partial class)            │   │
│  │  Core.cs · Intent.cs · Routing.cs · Execution.cs ·       │   │
│  │  Interaction.cs · Streaming.cs                            │   │
│  └───────────────────────────────────────────────────────────┘   │
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
│   ├── CallCenterService.Core.cs     # 基础骨架、DI 字段
│   ├── CallCenterService.Intent.cs   # ProcessAsync 统一入口
│   ├── CallCenterService.Routing.cs  # 意图路由 + 超时检测
│   ├── CallCenterService.Execution.cs # 工作流驱动 + 事件循环
│   ├── CallCenterService.Interaction.cs # 用户交互处理
│   ├── CallCenterService.Streaming.cs   # SSE 流式输出
│   ├── Extensions.cs                 # DI 注册扩展
│   ├── AIAgentFactory.cs             # Intent/Dialog Agent 工厂
│   └── Skills/
│       ├── RefundSkill.cs            # 退款技能
│       └── ExchangeSkill.cs          # 换货技能
│
├── CallCenter.Framework/            # 框架核心（横切关注点）
│   ├── Audit/                        # 审计日志（SHA256 哈希链）
│   ├── Compaction/                   # 对话压缩（8000 token / 8 轮）
│   ├── EventBus/                     # 业务事件总线
│   ├── Logging/                      # JSONL 操作日志
│   ├── Parsing/                      # LLM 结构化输出解析
│   ├── Pipeline/                     # 6 层聊天管道工厂
│   ├── Safety/                       # 安全过滤（PII/关键词/注入/输出拦截）
│   ├── Saga/                         # 失败重试 + 补偿框架
│   ├── Session/                      # 会话存储（内存 / Redis 占位）
│   └── ToolApproval/                 # 工具调用审批
│
├── CallCenter.Workflows/            # 业务流程
│   ├── Refund/                       # 退款流程（已完成）
│   │   ├── RefundWorkflow.cs         # 工作流图定义
│   │   ├── RefundMessages.cs         # 消息类型
│   │   └── Executors/                # 7 个执行器
│   └── Exchange/                     # 换货流程（骨架）
│       ├── ExchangeWorkflow.cs       # 工作流定义
│       ├── ExchangeMessages.cs       # 消息类型
│       └── Executors/                # 7 个执行器（骨架）
│
├── CallCenter.WebApi/               # Web API（v3.0 新增）
│   ├── Program.cs                    # /chat + /chat/stream 端点
│   ├── ChatRequest.cs                # 请求模型
│   └── appsettings.json              # 安全配置 + DI
│
├── CallCenter.Shared/               # 共享模型与接口
│   ├── Models/                       # OrderInfo / RefundResult / CouponInfo
│   ├── Mcp/                          # MCP Client 接口
│   └── Services/                     # Mock 实现（订单/财务/会员）
│
└── CallCenter.ConsoleDemo/          # 演示入口
    └── Program.cs                   # 主循环 + 工作流执行驱动

tests/
└── CallCenter.AgentHost.Tests/       # 集成测试
    ├── Phase10.Uat.Tests.cs          # Phase 10 UAT（4/4 通过）
    ├── Safety.Tests.cs               # 安全组件测试
    └── CallCenterService.Streaming.Tests.cs # SSE 序列化测试
```

---

## 五、技术栈

| 项目 | 技术 |
|------|------|
| 框架 | .NET 10.0 |
| Agent SDK | Microsoft Agent Framework (MAF) .NET SDK（源码引用） |
| LLM | DashScope（通义千问）OpenAI 兼容接口 |
| 构建 | 6 个项目 + 1 个测试项目，0 errors |
| 语言 | C#（70 个源文件） |
| 测试 | xUnit（37 个测试通过） |

---

## 六、如何运行

### 前置条件

1. .NET 10.0 SDK
2. DashScope API Key（[获取地址](https://dashscope.console.aliyun.com/)）

### 终端演示

```bash
# 设置环境变量
export DASHSCOPE_API_KEY="sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# 可选：设置模型名称（默认 qwen3-vl-flash）
export DASHSCOPE_MODEL_NAME="qwen-plus"

# 运行演示
dotnet run --project src/CallCenter.ConsoleDemo
```

### Web API

```bash
# 启动 Web API
dotnet run --project src/CallCenter.WebApi

# Swagger UI
open https://localhost:<port>/swagger

# 测试 /chat 端点
curl -X POST https://localhost:<port>/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "我要退款，订单A001"}'

# 测试 /chat/stream SSE 端点
curl -N -X POST https://localhost:<port>/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "我要退款，订单A001"}'
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

## 八、版本历史

### v3.0 Web API + Safety 增强（2026-06-05）

**4 个阶段，4 个计划，8 个任务**

- **Phase 13**: Web API Chat 端点 — `POST /chat` RESTful 接口 + DI 注册 + 安全配置
- **Phase 14**: SSE 流式 + 会话管理 — `POST /chat/stream` 实时推送 9 种 WorkflowEvent，sessionId 自动生成/复用，60 分钟惰性清理
- **Phase 15**: Safety Pipeline 实现 — 6 层管道完整接入（安全输入 → 日志 → 压缩 → 工具审批 → LLM → 安全输出）
- **Phase 16**: SafetyOutput 输出端拦截 — 暴力/色情/政治 3 类关键词过滤，友好话术替换；Exchange 骨架编译验证通过

**测试**: 37 个单元测试通过，Phase 10 UAT 4/4 通过

### v2.1 Execution & Cleanup（2026-06-04）

- Program.cs 精简为 18 行主循环
- CallCenterService 完整 DI 接入
- 全解决方案 0 错误编译

### v2.0 Framework 提取（2026-06-03）

- CallCenter.Framework 独立项目，包含安全/审计/Saga/会话等横切关注点
- CallCenter.AgentHost 独立项目，负责 Agent 编排

### v1.1 Technical Debt Closure（2026-06-01）

- 意图切换 + 超时管理 + 6 层安全 Pipeline + 对话压缩 + 审计日志 + Saga 补偿

### v1.0 Refund Workflow Demo（2026-06-01）

- 完整退款工作流（查单 → 规则 → 确认 → 退款 → 优惠券 → 通知）

---

## 九、路线图

| 版本 | 状态 | 计划内容 |
|------|------|----------|
| v1.0 | ✅ 已发布 | 退款工作流 Demo |
| v1.1 | ✅ 已发布 | 技术债务清理 + 能力增强 |
| v2.0 | ✅ 已发布 | Framework 提取 |
| v2.1 | ✅ 已发布 | 执行整合 + 清理验证 |
| v3.0 | ✅ 已发布 | Web API + Safety 增强 |
| v4.0 | 📋 规划中 | Exchange 换货实现 + 更多工作流 |

**已知债务：**
- Phase 13 UAT: 4 项人工测试（Swagger 可访问性 / POST /chat 响应 / 400 错误 / CORS）
- Phase 14 UAT: SSE 端到端 curl 验证

---

## 十、相关文档

- [PRD / 架构设计](Prd.md)
- [MILESTONES](.planning/MILESTONES.md)
- [PROJECT.md](.planning/PROJECT.md)
- [ROADMAP](.planning/ROADMAP.md)
