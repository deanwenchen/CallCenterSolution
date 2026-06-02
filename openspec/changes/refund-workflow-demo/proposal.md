# Change: Refund Workflow Demo

## 问题

CallCenter AI 项目需要基于 Microsoft Agent Framework (MAF) .NET SDK 构建一个可运行的退款流程 Demo，验证以下核心机制：
- Workflow + Executor + RequestPort 的人工介入模式（PRD Section 5.2）
- 流程中动态追问缺失参数（如用户没说订单号时主动询问）
- Entry Point + Intent Router 完整链路（PRD Section 5.1：检查 activeWorkflow → Resume 或新启动）
- Mock 数据替代真实后端服务（MCP Client 接口一致，Mock 实现）
- Framework 层横切关注点验证（EventBus、StructuredOutputParser、Safety Pipeline 等）

## 目标

1. 按 `Prd.md` Section 四 定义的完整目录结构创建 5 个项目（Shared/Framework/Workflows/AgentHost/ConsoleDemo）
2. 实现退款流程 6 步：GetOrder → CheckRefundRule → WaitConfirm → ExecuteRefund → RestoreCoupon → SendNotification
3. 支持参数缺失时自动回 RequestPort 询问用户（动态追问机制）
4. 控制台交互，数据全部 Mock（MCP Client 接口与 PRD 一致，Mock 实现）
5. 意图识别使用 DashScope LLM（通义千问，OpenAI 兼容接口）
6. MAF SDK 通过 source project 引用（参照 D:\GitCode\agent-framework\dotnet）
7. Framework 层按 PRD 建全部 9 个组件（EventBus/Parsing/Builder 可用，其余空壳）
8. AgentClassSkill (RefundSkill) 按 PRD 定义实现 Frontmatter/Instructions/Scripts
9. Agent Pipeline 按 PRD Section 7.4 定义简化实现
10. 支持 6 个异常场景的规范定义（中途换意图、执行失败、超时、多意图、回复不在预期、无意图闲聊）

## 非目标

- Framework 层 9 个组件中仅 EventBus/Parsing/Builder 为真实可用实现，其余为空壳（TODO）
- 不实现 Session 持久化（使用内存版 InMemorySessionStore）
- 不实现真实 MCP 调用（Mock 服务替代，接口签名一致）
- 不实现多业务模块（仅退款，Exchange 等后续扩展，但保留扩展结构）
- 不实现 Knowledge Layer / Observability Layer / Human Agent Layer（旁路系统）
- 不实现 Web/Gateway（控制台替代）
