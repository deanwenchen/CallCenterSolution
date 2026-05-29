# CallCenterSolution

An example call-center workflow system built around Microsoft Agent Framework workflows.

The code is organized around stable business boundaries:

- `CallCenter.Core`: domain models and interfaces. No provider SDKs, no Agent Framework runtime details.
- `CallCenter.Infrastructure`: gateway services, session stores, local/mock external systems, intent recognition, and Qwen model access.
- `CallCenter.Workflows`: Agent Framework workflow adapters and workflow definition registry.
- `CallCenter.BusinessActions`: business modules, capabilities, workflow definitions, permissions, and atomic business actions.
- `CallCenter.Composition`: dependency injection wiring.
- `CallCenter.Api`: HTTP host.
- `CallCenter.ConsoleClient`: local interactive runner.

## Intent Flow

New user messages go through:

```text
ConversationGateway
  -> HybridIntentRecognizer
      -> KeywordIntentRecognizer
      -> AgentFrameworkQwenModelClient when keyword confidence is low
  -> IntentCapabilityPlanner
  -> Capability
  -> AgentFrameworkWorkflowRuntime
```

The model only classifies intent and extracts entities. It does not choose tools or execute business actions.

## Qwen Configuration

The current model implementation uses Qwen through DashScope's OpenAI-compatible API, matching the Agent Framework sample in:

```text
D:\GitCode\agent-framework\dotnet\samples\01-get-started\01_hello_agent
```

Environment variables:

```powershell
$env:DASHSCOPE_API_KEY = "<your DashScope API key>"
$env:DASHSCOPE_MODEL_NAME = "qwen3.6-plus"
```

If `DASHSCOPE_API_KEY` is not set, the system falls back to keyword intent recognition.

## Run

Build:

```powershell
dotnet build CallCenterSolution.slnx
```

Console client:

```powershell
dotnet run --project src\CallCenter.ConsoleClient\CallCenter.ConsoleClient.csproj
```

API host:

```powershell
dotnet run --project src\CallCenter.Api\CallCenter.Api.csproj
```

Useful console messages:

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

## Adding A Business Flow

Add a folder under `CallCenter.BusinessActions/Modules/<FlowName>/` with:

```text
<FlowName>Configuration.cs
<FlowName>Capability.cs
<FlowName>WorkflowDefinitions.cs
<FlowName>BusinessActions.cs
```

The module should provide:

- `IIntentDefinitionProvider`
- `IIntentCapabilityRouteProvider`
- `ICapabilityWorkflowRouteProvider`
- `IWorkflowPermissionProvider`
- one or more `IBusinessAction` implementations

Dependency injection scans module implementations automatically, so new flows should not require edits to registries or host startup code.

## Naming Notes

`AgentFramework*` types are runtime adapters around Microsoft Agent Framework. Business modules should not reference Agent Framework types directly.

`KeywordIntentRecognizer` is the deterministic local recognizer. `HybridIntentRecognizer` combines local recognition with Qwen fallback. `IntentCapabilityPlanner` maps recognized intent keys to business capabilities through configured routes.
