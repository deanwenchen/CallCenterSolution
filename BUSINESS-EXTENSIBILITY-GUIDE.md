# 新增业务模块 7 步扩展指南

> 本指南指导开发者通过复制现有 Refund 模块来快速添加新的业务模块（如 Exchange 换货）。
> 完成全部 7 步后，新模块骨架应能成功编译。

## 前置条件

- .NET 10.0 SDK 已安装
- 现有 CallCenter 项目可编译通过（`dotnet build` 返回 0 errors）
- 了解 C# 基础和 MAF（Microsoft Agent Framework）Workflow 概念

## 预期结果

完成本指南后，你将拥有一个与 Refund 结构相同的新业务模块骨架，所有 Handler 抛出 `NotImplementedException`，项目可以编译通过。

---

## 第 1 步：复制目录

将 Refund 目录完整复制一份，命名为新模块名（以下以 Exchange 为例）。

### 操作

在项目根目录执行：

```bash
# Windows (PowerShell)
xcopy /E /I src\CallCenter.Workflows\Refund src\CallCenter.Workflows\Exchange
```

### 验证

确认以下目录结构已存在：

```
src/CallCenter.Workflows/Exchange/
├── Executors/
│   ├── GetOrderExecutor.cs
│   ├── CheckRefundRuleExecutor.cs
│   ├── WaitUserConfirmExecutor.cs
│   ├── ExecuteRefundExecutor.cs
│   ├── RestoreCouponExecutor.cs
│   ├── SendNotificationExecutor.cs
│   └── RefundDeniedExecutor.cs
├── RefundWorkflow.cs
└── RefundMessages.cs
```

---

## 第 2 步：重命名命名空间

将所有文件中的 `Refund` 引用替换为 `Exchange`。

### 操作

#### 2.1 重命名文件

将以下文件重命名：
- `RefundWorkflow.cs` → `ExchangeWorkflow.cs`
- `RefundMessages.cs` → `ExchangeMessages.cs`
- `Executors/ExecuteRefundExecutor.cs` → `Executors/ExecuteExchangeExecutor.cs`
- `Executors/CheckRefundRuleExecutor.cs` → `Executors/CheckExchangeRuleExecutor.cs`
- `Executors/RefundDeniedExecutor.cs` → `Executors/ExchangeDeniedExecutor.cs`
- `Executors/WaitUserConfirmExecutor.cs` → `Executors/WaitExchangeConfirmExecutor.cs`
- `Executors/GetOrderExecutor.cs` → `Executors/GetExchangeOrderExecutor.cs`
- `Executors/SendNotificationExecutor.cs` → `Executors/ExchangeSendNotificationExecutor.cs`

`RestoreCouponExecutor.cs` 可以保留原名（退款和换货共享优惠券恢复逻辑），或重命名为 `ExchangeRestoreCouponExecutor.cs`。

#### 2.2 全局替换

在每个 Exchange 目录下的文件中执行以下替换：
- `CallCenter.Workflows.Refund` → `CallCenter.Workflows.Exchange`
- `Refund` → `Exchange`（类名、变量名、端口名、执行器名）
- `refund` → `exchange`（小写引用）

### 验证

在 Exchange/ 目录中搜索 `Refund`，确认没有残留引用：

```bash
grep -r "Refund" src/CallCenter.Workflows/Exchange/
# 应返回空（或在共享的 RestoreCouponExecutor.cs 中有合理引用）
```

---

## 第 3 步：修改 Workflow

修改 `ExchangeWorkflow.cs`，更新端口类型、消息类型和执行器实例。

### 参照文件

`src/CallCenter.Workflows/Refund/RefundWorkflow.cs`

### 操作

打开 `ExchangeWorkflow.cs`，对照 RefundWorkflow.cs 的完整代码进行以下修改：

```csharp
// 原始（Refund）：
var infoPort = RequestPort.Create<RefundSignal, RefundIntent>("RefundInfoPort");
var confirmPort = RequestPort.Create<ConfirmRefundRequest, UserConfirmation>("RefundConfirmPort");

// 修改后（Exchange）：
var infoPort = RequestPort.Create<ExchangeSignal, ExchangeIntent>("ExchangeInfoPort");
var confirmPort = RequestPort.Create<ConfirmExchangeRequest, UserConfirmation>("ExchangeConfirmPort");
```

更新执行器实例化（类名已重命名）：

```csharp
// 原始（Refund）：
var getOrder = new GetOrderExecutor(orderService);
var checkRule = new CheckRefundRuleExecutor();
// ...

// 修改后（Exchange）：
var getOrder = new GetExchangeOrderExecutor(orderService);
var checkRule = new CheckExchangeRuleExecutor();
// ...
```

更新消息类型引用：

```csharp
// 原始（Refund）：
builder.ForwardMessage<OrderFound>(getOrder, checkRule);
builder.AddEdge<RefundRuleResult>(checkRule, waitConfirm, r => r?.IsEligible == true);

// 修改后（Exchange）：
builder.ForwardMessage<ExchangeOrderFound>(getOrder, checkRule);
builder.AddEdge<ExchangeRuleResult>(checkRule, waitConfirm, r => r?.IsEligible == true);
```

保持 edge 拓扑不变（与 Refund 完全相同的流程结构）。

### 验证

运行 `dotnet build`，此时应出现类型错误（消息类型还未定义）——这是预期行为。

---

## 第 4 步：修改 Executors

将每个 Executor 的业务逻辑替换为 `NotImplementedException`。

### 参照文件

- `src/CallCenter.Workflows/Refund/Executors/GetOrderExecutor.cs`
- `src/CallCenter.Workflows/Refund/Executors/CheckRefundRuleExecutor.cs`
- `src/CallCenter.Workflows/Refund/Executors/WaitUserConfirmExecutor.cs`
- `src/CallCenter.Workflows/Refund/Executors/ExecuteRefundExecutor.cs`
- `src/CallCenter.Workflows/Refund/Executors/RestoreCouponExecutor.cs`
- `src/CallCenter.Workflows/Refund/Executors/SendNotificationExecutor.cs`
- `src/CallCenter.Workflows/Refund/Executors/RefundDeniedExecutor.cs`

### 操作

对每个 Executor 文件，将 `HandleAsync` 方法体替换为：

```csharp
throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
```

保留以下内容不变：
- `[SendsMessage]` 属性（更新为 Exchange 消息类型）
- 构造函数（服务注入 + 无参数回退）
- `HandleAsync` 方法签名

示例（GetExchangeOrderExecutor.cs）：

```csharp
using CallCenter.Framework.EventBus;
using CallCenter.Shared.Mcp;
using CallCenter.Workflows.Exchange;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows.Exchange.Executors;

[SendsMessage(typeof(ExchangeOrderFound))]
internal sealed class GetExchangeOrderExecutor : Executor<ExchangeSignal, ExchangeOrderFound>
{
    private readonly IOrderMcpClient _orderService;

    public GetExchangeOrderExecutor(IOrderMcpClient orderService) : base("GetExchangeOrder")
    {
        _orderService = orderService;
    }

    public GetExchangeOrderExecutor() : this(null!) { }

    public override ValueTask<ExchangeOrderFound> HandleAsync(ExchangeSignal message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Exchange workflow not implemented — this is a skeleton created via the Business Extensibility Guide");
    }
}
```

对其余 6 个 Executor 执行相同的操作。

### 验证

确认所有 7 个 Executor 的 `HandleAsync` 方法都抛出 `NotImplementedException`。

---

## 第 5 步：新增 Skill

创建 `ExchangeSkill.cs`，参照 `RefundSkill.cs` 的结构。

### 参照文件

`src/CallCenter.AgentHost/Skills/RefundSkill.cs`

### 操作

创建 `src/CallCenter.AgentHost/Skills/ExchangeSkill.cs`：

```csharp
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace CallCenter.AgentHost.Skills;

[Experimental("MAAI001")]
public sealed class ExchangeSkill : AgentClassSkill<ExchangeSkill>
{
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "exchange",
        "处理用户换货请求。当用户要求换货、退货、更换商品时使用。" +
        "支持查询订单、校验换货资格、计算换货金额、执行换货。");

    protected override string Instructions => """
        当用户要求换货时使用此技能。

        1. 获取订单号（如果用户未提供，使用 get_recent_orders 获取最近订单）
        2. 系统将自动处理换货流程，包括资格校验、金额计算、用户确认
        3. 换货完成后通知用户结果
        """;

    // 骨架阶段不需要实际的 skill scripts
    // 实际实现时添加：
    // [AgentSkillScript("get_recent_orders")]
    // private static async Task<string> GetRecentOrders(...) { ... }
    //
    // [AgentSkillScript("execute_exchange")]
    // private static async Task<string> ExecuteExchange(...) { ... }
}
```

### 验证

确认文件存在且包含：
- `AgentClassSkill<ExchangeSkill>` 继承
- `Frontmatter` 属性，name 为 `"exchange"`
- 中文描述

---

## 第 6 步：注册

在 DI 容器中注册 ExchangeWorkflow 和 ExchangeSkill。

### 参照文件

- `src/CallCenter.ConsoleDemo/Program.cs`（查看现有注册方式）
- `src/CallCenter.Workflows/CallCenter.Workflows.csproj`
- `src/CallCenter.AgentHost/CallCenter.AgentHost.csproj`

### 操作

#### 6.1 注册 ExchangeWorkflow

在 `Program.cs` 中，找到现有的 RefundWorkflow 注册位置，在其旁边添加 ExchangeWorkflow 的骨架注册。由于 Exchange 的 Executor 都是骨架，暂时不需要实际注入服务，只需确保代码能通过编译。

#### 6.2 注册 ExchangeSkill

在 `Program.cs` 的 `AgentSkillsProvider` 构造函数中，在 `RefundSkill()` 旁边添加 `ExchangeSkill()`：

```csharp
// 原始：
var skillsProvider = new AgentSkillsProvider(new RefundSkill());

// 修改后：
var skillsProvider = new AgentSkillsProvider(new RefundSkill(), new ExchangeSkill());
```

### 验证

运行 `dotnet build`，确认无编译错误。

---

## 第 7 步：验证

确认整个项目可以成功编译，且新模块目录结构正确。

### 操作

```bash
dotnet build
```

### 预期结果

```
成功生成。
    0 个警告
    0 个错误
```

### 目录结构验证

确认最终目录结构符合预期：

```
src/
├── CallCenter.Workflows/
│   ├── Refund/
│   │   ├── RefundWorkflow.cs
│   │   ├── RefundMessages.cs
│   │   └── Executors/
│   │       ├── ... (7 个 Refund Executor)
│   └── Exchange/
│       ├── ExchangeWorkflow.cs
│       ├── ExchangeMessages.cs
│       └── Executors/
│           ├── ... (7 个 Exchange Executor)
└── CallCenter.AgentHost/
    └── Skills/
        ├── RefundSkill.cs
        └── ExchangeSkill.cs
```

---

## 故障排查

### 问题：编译出现 "找不到类型" 错误

**原因**：第 2 步的命名空间替换不完整。
**解决**：检查报错文件中的 `namespace` 和 `using` 语句，确认全部使用 `CallCenter.Workflows.Exchange`。

### 问题：编译出现 "[SendsMessage] 属性找不到类型" 错误

**原因**：第 4 步修改 Executor 时，`[SendsMessage]` 属性引用的消息类型还未在 `ExchangeMessages.cs` 中定义。
**解决**：检查 `ExchangeMessages.cs` 是否包含所有需要的消息类型记录。

### 问题：ExchangeSkill 编译失败

**原因**：`AgentClassSkill` 的继承方式不正确。
**解决**：参照 `RefundSkill.cs`，确保继承 `AgentClassSkill<ExchangeSkill>` 并重写 `Frontmatter` 和 `Instructions` 属性。

---

## 后续步骤

完成骨架后，下一步是实际实现业务逻辑：

1. 定义 `ExchangeMessages.cs` 中的具体消息类型
2. 实现每个 Executor 的实际业务逻辑
3. 添加 Skill 脚本（`get_recent_orders`、`execute_exchange`）
4. 连接到真实的 MCP Server（如果尚未连接）

> 完整业务逻辑实现请参考 v2 需求文档（WF-10: Exchange Workflow）。
