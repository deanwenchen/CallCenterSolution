---
phase: 01-foundation
plan: 01
subsystem: infra
tags: [dotnet, cpm, slnx, csproj, net10.0]

# Dependency graph
requires: []
provides:
  - Solution structure with 5 CallCenter projects + 4 MAF source projects
  - Central Package Management with 8 package versions
  - Project reference chain: ConsoleDemo -> AgentHost -> Workflows + Framework -> Shared
affects:
  - All subsequent phases depend on this foundation

# Tech tracking
tech-stack:
  added: [Directory.Packages.props CPM, .NET 10.0 target framework]
  patterns:
    - Central Package Management (ManagePackageVersionsCentrally=true)
    - Source project references to MAF via relative paths from GitCode repo
    - Consistent csproj properties: net10.0, Nullable=enable, ImplicitUsings=enable

key-files:
  created:
    - Directory.Packages.props
    - src/CallCenter.Shared/CallCenter.Shared.csproj
    - src/CallCenter.Framework/CallCenter.Framework.csproj
    - src/CallCenter.Workflows/CallCenter.Workflows.csproj
    - src/CallCenter.AgentHost/CallCenter.AgentHost.csproj
    - src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj
  modified:
    - CallCenterSolution.slnx

key-decisions:
  - "System.Text.Json omitted from Framework.csproj — transitive pinning via CentralPackageTransitivePinningEnabled makes explicit ref redundant (NU1510 warning avoided)"
  - "Microsoft.Extensions.AI added to AgentHost.csproj — core AI abstractions required by Microsoft.Extensions.AI.OpenAI"

requirements-completed: []

# Metrics
duration: 0min (prior execution verified)
completed: 2026-06-01
---

# Phase 01 Plan 01: Project Structure + Solution + Package Management Summary

**.NET 10.0 solution with 5 CallCenter projects, 4 MAF source references, and centralized package management (8 packages)**

## Performance

- **Duration:** Prior execution (verified 2026-06-01)
- **Tasks:** 9/9 complete
- **Files modified:** 7 (5 .csproj + Directory.Packages.props + CallCenterSolution.slnx)
- **Directories created:** 5 project dirs + 15 subdirectories

## Accomplishments
- Directory.Packages.props with CPM enabled (ManagePackageVersionsCentrally=true), 8 package versions
- CallCenterSolution.slnx updated with 5 CallCenter project refs + 4 existing MAF refs (9 total)
- Complete src/ directory structure with all 15 required subdirectories
- 5 .csproj files created with net10.0, Nullable=enable, ImplicitUsings=enable
- Correct project reference chain: ConsoleDemo -> AgentHost -> Workflows + Framework -> Shared
- dotnet build succeeds: 0 errors, 0 warnings

## Task Commits

All tasks were completed in a single combined commit from prior execution:

1. **Tasks 1.1-1.9: All foundation tasks** - `6536f88` (feat: implement Phase 1 (Foundation) + Phase 2 (AgentHost/Intent Router))

The System.Text.Json redundancy cleanup was done in a later commit:
- `2011bb6` (chore(04-01): remove redundant System.Text.Json from Framework and Shared csproj)

## Files Created/Modified
- `Directory.Packages.props` - Central Package Management with 8 packages (OpenAI 2.10.0, ME.AI 10.5.1, ME.AI.OpenAI 10.5.1, ME.DependencyInjection 10.0.1, ME.Configuration 10.0.1, ME.Configuration.EnvironmentVariables 10.0.1, ME.Configuration.Json 10.0.1, System.Text.Json 10.0.6)
- `CallCenterSolution.slnx` - Added 5 CallCenter project refs, kept 4 MAF source refs
- `src/CallCenter.Shared/CallCenter.Shared.csproj` - Library, net10.0, no project refs
- `src/CallCenter.Framework/CallCenter.Framework.csproj` - Library, refs Shared, ME.DI, ME.Configuration
- `src/CallCenter.Workflows/CallCenter.Workflows.csproj` - Library, refs Shared + Framework + MAF Workflows
- `src/CallCenter.AgentHost/CallCenter.AgentHost.csproj` - Library, refs Workflows + Framework + MAF AI, OpenAI + ME.AI packages
- `src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj` - Exe, refs 4 local + MAF Workflows, ME.Configuration packages

## Decisions Made
- System.Text.Json omitted from Framework.csproj — CentralPackageTransitivePinningEnabled pins the transitive version from Shared, avoiding NU1510 "redundant package" warning
- Microsoft.Extensions.AI added to AgentHost.csproj — this is the core AI abstraction layer that Microsoft.Extensions.AI.OpenAI depends on; required for correct dependency resolution

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Omitted redundant System.Text.Json from Framework.csproj**
- **Found during:** Task 1.5 (Create CallCenter.Framework.csproj) and Task 1.4 (Create CallCenter.Shared.csproj)
- **Issue:** Plan specified PackageReference to System.Text.Json in Framework.csproj, but with CentralPackageTransitivePinningEnabled enabled, the transitive reference from Shared is already pinned to the same version. Adding an explicit reference would produce NU1510 warning.
- **Fix:** Did not include explicit System.Text.Json PackageReference in Framework.csproj or Shared.csproj. Cleaned up in commit 2011bb6.
- **Files modified:** src/CallCenter.Framework/CallCenter.Framework.csproj, src/CallCenter.Shared/CallCenter.Shared.csproj
- **Verification:** dotnet build produces 0 warnings
- **Committed in:** 2011bb6

**2. [Rule 2 - Missing Critical] Added Microsoft.Extensions.AI to AgentHost.csproj**
- **Found during:** Task 1.7 (Create CallCenter.AgentHost.csproj)
- **Issue:** Plan specified OpenAI and Microsoft.Extensions.AI.OpenAI package references, but Microsoft.Extensions.AI.OpenAI depends on Microsoft.Extensions.AI as its core abstraction layer. Without explicit reference, version resolution could be ambiguous.
- **Fix:** Added Microsoft.Extensions.AI PackageReference alongside OpenAI and Microsoft.Extensions.AI.OpenAI.
- **Files modified:** src/CallCenter.AgentHost/CallCenter.AgentHost.csproj
- **Verification:** dotnet build succeeds, all types resolve correctly
- **Committed in:** 6536f88

---

**Total deviations:** 2 auto-fixed (2 missing critical — both correctness/dependency resolution)
**Impact on plan:** Both deviations improve build cleanliness (0 warnings) and dependency correctness. No scope creep.

## Known Stubs

None — this plan creates project infrastructure only, no application code stubs.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: external_source_ref | CallCenterSolution.slnx, *.csproj | Relative paths to D:\GitCode\agent-framework\dotnet — MAF source projects are outside this repo. If MAF repo is unavailable or at wrong commit, build fails. |

## Issues Encountered

None during this verification pass. Prior execution completed cleanly.

## Next Phase Readiness

- Foundation complete: all 5 projects compile, dependency chain correct
- Ready for Phase 2 (AgentHost + Intent Router) — already implemented in prior session
- MAF source dependency is the only external requirement

---
*Phase: 01-foundation*
*Completed: 2026-06-01*
