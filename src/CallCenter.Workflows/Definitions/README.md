# Workflow 模块接入方式

新增一条业务流程时，不要改 `WorkflowDefinitionRegistry`。

一个模块自己提供：

```text
IWorkflowDefinitionProvider  -> 定义流程图
ICapability                  -> 选择该流程
IBusinessAction              -> 实现流程里的 Step
```

启动时 `CallCenter.Composition` 会自动扫描并注册这些类型。

商品退货示例：

```text
CallCenter.Infrastructure/Capabilities/ProductReturnCapability.cs
CallCenter.Workflows/Definitions/ProductReturnWorkflowDefinitions.cs
CallCenter.BusinessActions/ProductReturn/ProductReturnBusinessActions.cs
```

这样新增流程不会改 API、Console、总注册表，也不会影响别的流程定义。
