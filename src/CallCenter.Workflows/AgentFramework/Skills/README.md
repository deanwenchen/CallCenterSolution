# Agent Framework Skills

Put Agent Framework-native skills and adapters here.

Expected pattern:

```text
Agent Framework Workflow / Agent
  -> Agent Framework Skill adapter
    -> IBusinessAction / IBusinessActionRegistry
      -> IExternalSystemGateway
```

Business behavior remains in `CallCenter.BusinessActions`. Agent Framework skills only adapt runtime calls into application/business abstractions.
