---
plan_id: "02-03"
phase: "02-agenthost-intent-router"
status: complete
completed_at: 2026-06-01T12:30:00Z
---

# Plan 02-03: SK-02 Gap Closure — AgentSkillsProvider Registration

## Objective

Close the SK-02 verification gap: create `AgentSkillsProvider`, register `RefundSkill`, and wire it into the Intent Agent's `AIContextProviders` so the LLM can auto-discover skills via Frontmatter description.

## Summary

`AgentSkillsProvider` exists in the MAF framework (`Microsoft.Agents.AI` namespace) — it just needed to be instantiated and wired up. Three targeted changes closed the gap:

1. **EntryPoint.cs** — Constructor now accepts optional `AgentSkillsProvider?` parameter and wires it to `ChatClientAgentOptions.AIContextProviders`
2. **Program.cs** — Creates `new AgentSkillsProvider(new RefundSkill())` and passes it to EntryPoint
3. **Experimental suppression** — `AgentSkillsProvider` and `AgentClassSkill` are marked `[Experimental]` in MAF; added `[Experimental("MAAI001")]` to EntryPoint class and `#pragma warning disable MAAI001` to Program.cs

## Changes

| File | Change |
|------|--------|
| `src/CallCenter.AgentHost/EntryPoint.cs` | Added `using System.Diagnostics.CodeAnalysis`, `[Experimental("MAAI001")]` on class, `AgentSkillsProvider? skillsProvider` constructor param, `AIContextProviders` wiring |
| `src/CallCenter.ConsoleDemo/Program.cs` | Added `using CallCenter.AgentHost.Skills`, `using Microsoft.Agents.AI`, `#pragma warning disable MAAI001`, `new AgentSkillsProvider(new RefundSkill())`, updated EntryPoint construction |

## Build Verification

- `dotnet build src/CallCenter.AgentHost/` — 0 errors, 0 warnings
- `dotnet build src/CallCenter.ConsoleDemo/` — 0 errors, 0 warnings

## Self-Check: PASSED

- [x] AgentSkillsProvider instantiated with RefundSkill
- [x] Wired to EntryPoint's AIContextProviders
- [x] ConsoleDemo passes skillsProvider to EntryPoint
- [x] Both projects compile cleanly
- [x] Backward compatible (optional parameter defaults to null)
