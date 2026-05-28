# CallCenter.Workflows

Workflow project layout:

```text
Definitions/
  Static workflow definitions and registry.

Maf/
  Factory/
    Builds MAF workflows from definitions.
  Runtime/
    Runs and resumes MAF workflows.
  Executors/
    Adapts workflow steps to business actions.
  Agents/
    Future MAF-native agents.
  Skills/
    Future MAF-native skill adapters.
```
