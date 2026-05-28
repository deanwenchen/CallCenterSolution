# MAF Skills

Put MAF-native skills and adapters here.

Expected pattern:

```text
MAF Workflow / Agent
  -> MAF Skill adapter
    -> IBusinessAction / IBusinessActionRegistry
      -> IExternalSystemGateway
```

Business behavior remains in `CallCenter.BusinessActions`. MAF skills only adapt MAF runtime calls into application/business abstractions.
