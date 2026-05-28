# Workflow 模块接入方式

新增一条业务流程时，不要改 `WorkflowDefinitionRegistry`。

一个模块自己提供：

```text
IIntentDefinitionProvider   -> 定义意图关键词
IIntentCapabilityRouteProvider -> 定义意图到能力的路由
ICapabilityWorkflowRouteProvider -> 定义能力到流程的路由
IWorkflowPermissionProvider -> 定义流程可调用的动作和工具
IWorkflowDefinitionProvider  -> 定义流程图
ICapability                  -> 选择该流程
IBusinessAction              -> 实现流程里的 Step
```

启动时 Host 把模块程序集传给 `CallCenter.Composition`，Composition 会扫描并注册这些类型。

商品退货示例：

```text
CallCenter.Modules/ProductReturn/ProductReturnConfiguration.cs
CallCenter.Modules/ProductReturn/ProductReturnCapability.cs
CallCenter.Modules/ProductReturn/ProductReturnWorkflowDefinitions.cs
CallCenter.Modules/ProductReturn/ProductReturnBusinessActions.cs
```

这样新增流程不需要改总注册表和核心配置，影响范围收在自己的模块项目里。
