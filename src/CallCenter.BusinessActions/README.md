# CallCenter.BusinessActions

Business actions are the atomic business units invoked by workflow steps. Business modules also live here so a flow can keep its intent configuration, capability, workflow definition, permissions, and actions together.

Current folders:

```text
Modules/
Notifications/
Registry/
Shared/
```

Add a new business flow under `Modules/<FlowName>/`.

Keep this project free of MAF-specific types. MAF adapters belong in `CallCenter.Workflows/Maf`.
