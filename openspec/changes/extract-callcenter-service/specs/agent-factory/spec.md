## ADDED Requirements

### Requirement: AIAgentFactory 创建意图识别 Agent
AIAgentFactory SHALL 提供 CreateIntentAgent(skillsProvider?) 方法，创建配置了 IntentRegistry.BuildSystemPrompt() 作为 System Prompt 的 AIAgent，用于意图识别。

#### Scenario: 创建意图识别 Agent
- **WHEN** 调用 factory.CreateIntentAgent(skillsProvider)
- **THEN** 返回的 AIAgent 使用 IntentRegistry.BuildSystemPrompt() 作为 Instructions，并注入 skillsProvider

### Requirement: AIAgentFactory 创建工作流对话 Agent
AIAgentFactory SHALL 提供 CreateDialogAgent(skillsProvider?) 方法，创建配置了工作流对话 System Prompt 的 AIAgent，用于工作流中的 LLM 对话交互。

#### Scenario: 创建工作流对话 Agent
- **WHEN** 调用 factory.CreateDialogAgent(skillsProvider)
- **THEN** 返回的 AIAgent 使用工作流对话 System Prompt 作为 Instructions，并注入 skillsProvider

### Requirement: AIAgentFactory 依赖 IChatClient
AIAgentFactory 构造函数 SHALL 接收 IChatClient pipelineClient 参数，所有创建的 AIAgent 都使用同一个 pipeline 包装的客户端。

#### Scenario: 工厂复用 pipeline 客户端
- **WHEN** 通过 DI 创建 AIAgentFactory
- **THEN** 工厂使用 DI 注册的默认 IChatClient（包含 6 层 pipeline），所有子 Agent 共享同一管道

### Requirement: EntryPoint 改用 AIAgentFactory
EntryPoint 构造函数 SHALL 接收 AIAgentFactory 参数（替代原来的 IChatClient），通过 factory.CreateIntentAgent() 创建内部使用的意图识别 Agent。

#### Scenario: EntryPoint 使用工厂创建 Agent
- **WHEN** DI 容器构造 EntryPoint 实例
- **THEN** EntryPoint 从 AIAgentFactory 获取意图识别 Agent，而非自行创建
