# CallCenterSolution

客服中心示例工程，当前目标是验证一套基于 MAF Workflow 的可插拔业务流程结构。

核心原则：

- 业务流程按文件夹隔离，不按每个流程单独建工程。
- 新增业务流程不修改 `WorkflowDefinitionRegistry`，不修改核心配置。
- 意图、能力路由、Workflow 路由、Workflow 权限都通过 provider 抽象提供，后续可以替换成后台或数据库配置。
- BusinessAction 只能通过 Workflow Step 调用，Workflow 权限控制可调用的 action 和 tool。
- 业务模块不直接依赖 MAF 细节，MAF 适配放在 `CallCenter.Workflows`。

## 项目结构

```text
src/
  CallCenter.Api/
    HTTP API Host。

  CallCenter.ConsoleClient/
    控制台调用入口，用于本地验证对话和 Workflow。

  CallCenter.Core/
    Models/          领域模型、请求响应、Workflow 定义、状态模型。
    Contracts/       核心接口，例如 IConversationGateway、IWorkflowRuntime、IBusinessAction。
    Conversation/    会话入口编排实现。

  CallCenter.BusinessActions/
    Modules/         业务模块，每个业务流程一个文件夹。
    Shared/          多个流程复用的业务动作，例如 QueryOrder、WaitUserConfirm。
    Notifications/   通知动作。
    Registry/        BusinessAction 注册表。

  CallCenter.Infrastructure/
    Gateway/         会话上下文、认证、鉴权、限流、黑名单等入口基础设施。
    Intent/          配置驱动的意图识别和 planner。
    Mcp/             外部系统和 MCP tool 网关。
    Persistence/     会话状态存储。
    Services/        知识库、人工客服等本地实现。

  CallCenter.Workflows/
    Definitions/     WorkflowDefinitionRegistry 和 DSL。
    Maf/             MAF runtime、factory、executor 适配。

  CallCenter.Composition/
    统一依赖注入入口 AddCallCenter()。
```

当前业务模块位于：

```text
CallCenter.BusinessActions/Modules/
  Refund/
  ProductReturn/
  Invoice/
  Logistics/
  Crm/
  Subscribe/
  Member/
  Coupon/
  HumanAgent/
```

## 运行方式

构建：

```powershell
dotnet build CallCenterSolution.slnx
```

启动控制台：

```powershell
dotnet run --project src\CallCenter.ConsoleClient\CallCenter.ConsoleClient.csproj
```

控制台常用命令：

```text
/catalog
/new
/exit
```

示例消息：

```text
refund order ORD-10001 amount 99
product return order ORD-10001 amount 99
yes
create invoice for order ORD-20001 invoice title ACME amount 100
track logistics for order ORD-30001
show my member points
issue coupon for me
human agent please
```

启动 API：

```powershell
dotnet run --project src\CallCenter.Api\CallCenter.Api.csproj
```

## 业务接入流程

新增一个业务流程，例如 `Exchange`，只需要在 `CallCenter.BusinessActions/Modules/Exchange/` 下新增自己的文件。

推荐文件结构：

```text
CallCenter.BusinessActions/Modules/Exchange/
  ExchangeConfiguration.cs
  ExchangeCapability.cs
  ExchangeWorkflowDefinitions.cs
  ExchangeBusinessActions.cs
```

### 1. 定义意图、路由和权限

`ExchangeConfiguration` 实现这些接口：

```text
IIntentDefinitionProvider
IIntentCapabilityRouteProvider
ICapabilityWorkflowRouteProvider
IWorkflowPermissionProvider
```

这里负责提供：

- 哪些关键词命中该意图。
- 意图路由到哪个 capability。
- capability 路由到哪个 workflow。
- workflow 允许调用哪些 BusinessAction 和 tool。

权限配置要收紧到当前 workflow 需要的最小集合，避免 A 流程调用到 B 流程的 action 或 tool。

### 2. 定义 Capability

`ExchangeCapability` 实现 `ICapability`。

Capability 负责选择具体 Workflow。简单流程可以固定返回一个 Workflow；复杂流程可以根据金额、风险、会员等级等业务数据选择不同 Workflow。

### 3. 定义 Workflow

`ExchangeWorkflowDefinitions` 实现 `IWorkflowDefinitionProvider`。

WorkflowDefinition 只描述流程图：

- Workflow 名称。
- Step 列表。
- Step 调用的 BusinessAction 名称。
- Step 之间的 Edge。
- 是否有人机协同节点。

不要把业务实现写进 WorkflowDefinition。业务实现放到 BusinessAction。

### 4. 实现 BusinessAction

`ExchangeBusinessActions` 里实现一个或多个 `IBusinessAction`。

BusinessAction 是 Workflow Step 的原子业务动作，例如：

- 查询订单。
- 校验规则。
- 创建售后单。
- 发送通知。

如果动作会访问外部系统，应通过 `IExternalSystemGateway`，不要在 BusinessAction 里直接写 HTTP、数据库或 SDK 调用。

### 5. 不需要改注册表

`CallCenter.Composition` 会扫描业务工程中的实现类型，并注册：

```text
ICapability
IBusinessAction
IWorkflowDefinitionProvider
IIntentDefinitionProvider
IIntentCapabilityRouteProvider
ICapabilityWorkflowRouteProvider
IWorkflowPermissionProvider
```

因此新增模块不需要修改：

```text
WorkflowDefinitionRegistry
Program.cs
CallCenterServiceCollectionExtensions
核心配置文件
```

## 配置化边界

当前意图、路由、权限还是代码 provider，但已经通过接口隔离：

```text
IIntentDefinitionProvider
IIntentCapabilityRouteProvider
ICapabilityWorkflowRouteProvider
IWorkflowPermissionProvider
```

后续接后台配置时，替换 provider 实现即可。业务代码仍然可以保留本地 provider 作为默认配置或测试配置。

Workflow 图目前仍建议代码定义。原因是 Workflow 图配置化涉及：

- Step 类型校验。
- Edge 条件表达式。
- 版本发布和回滚。
- 权限联动。
- 审计和灰度。

在没有后台编排能力前，先用模块内代码定义 Workflow 更稳。

## Core 边界

`CallCenter.Core` 放稳定抽象和模型：

- 请求响应模型。
- Workflow 定义和状态。
- 会话上下文。
- 核心接口。
- 会话入口编排。

不建议在 Core 里直接放 OpenAI、千问、MAF SDK 等具体实现。正确边界是：

```text
Core              定义 IModelClient / IMessageCompressor 等接口。
Infrastructure    实现 OpenAIModelClient / QwenModelClient / MessageCompressor。
Composition        根据配置注册具体实现。
BusinessActions    只依赖接口。
```

这样后续替换模型供应商或压缩策略时，不影响业务模块。

## MAF 边界

`CallCenter.Workflows/Maf` 只做运行时适配：

- 把 `WorkflowDefinition` 转成 MAF Workflow。
- 把 MAF Executor 调用转成 `IBusinessAction` 执行。
- 处理 checkpoint、resume、human-in-the-loop。

业务模块不直接引用 MAF 类型。

## 当前验证命令

快速验证 catalog 和主流程：

```powershell
'/catalog','/new','refund order ORD-10001 amount 99','yes','/new','product return order ORD-10001 amount 99','yes','/exit' |
  dotnet run --project src\CallCenter.ConsoleClient\CallCenter.ConsoleClient.csproj --no-build
```
