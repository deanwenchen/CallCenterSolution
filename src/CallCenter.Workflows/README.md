# CallCenter.Workflows

Workflow project layout:

```text
Definitions/
  Static workflow definitions and registry.

AgentFramework/
  Factory/
    Builds Agent Framework workflows from definitions.
  Runtime/
    Runs and resumes Agent Framework workflows.
  Executors/
    Adapts workflow steps to business actions.
  Agents/
    Future Agent Framework-native agents.
  Skills/
    Future Agent Framework-native skill adapters.
```
