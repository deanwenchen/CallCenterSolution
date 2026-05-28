# CallCenter.Infrastructure

Infrastructure project layout:

```text
Gateway/
  Auth, authorization, rate limit, blacklist, audit, observability, context creation.

Intent/
  Local intent recognition and planner implementations.

Capabilities/
  Capability registry and workflow selection policies.

Mcp/
  External system gateway and future MAF MCP adapter.

Persistence/
  Session state stores.

Services/
  Knowledge and human-agent service implementations.
```
