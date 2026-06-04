---
phase: "16"
plan: "01"
subsystem: SafetyOutput + Exchange
tags: [safety, output-filter, exchange-skeleton]
requires: ["15-01"]
provides: ["SO-01: Output-end content filtering", "EX-01: Exchange skeleton compilation"]
affects: ["SafetyOptions", "OutputContentFilter", "SafetyOutputFilter", "SafetyOutputDelegatingClient"]
tech-stack:
  added: ["OutputContentFilter class", "SafetyOptions output properties", "ProcessOutput overload"]
  patterns: ["Instance-mode keyword filtering (consistent with KeywordFilter)", "SafetyViolationException for content blocking"]
key-files:
  created:
    - src/CallCenter.Framework/Safety/OutputContentFilter.cs
  modified:
    - src/CallCenter.Framework/Safety/SafetyOptions.cs
    - src/CallCenter.Framework/Safety/SafetyInputFilter.cs
    - src/CallCenter.Framework/Pipeline/StandardPipelineFactory.cs
    - src/CallCenter.WebApi/appsettings.json
    - src/CallCenter.AgentHost/Extensions.cs
decisions:
  - "OutputContentFilter follows instance pattern (via SafetyOptions) matching KeywordFilter style, not hardcoded keywords"
  - "SafetyOutputDelegatingClient constructor requires SafetyOptions (non-optional) to enforce output filtering awareness"
  - "CreatePipeline accepts SafetyOptions? with default null, falling back to new SafetyOptions() for backward compatibility"
  - "Extensions.cs binds all output-end config properties from appsettings.json Safety section"
metrics:
  duration_minutes: 5
  completed: "2026-06-04"
---

# Phase 16 Plan 01: SafetyOutput 输出端敏感内容拦截 + Exchange 骨架编译确认 Summary

**One-liner:** 实现 LLM 输出端按类别（violence/pornography/politics）关键词拦截，3 类话术模板替换；确认 Exchange 7 个 Executor 骨架编译通过。

## Tasks Completed

| Task | Name        | Commit | Files Modified/Created                          |
| ---- | ----------- | ------ | ----------------------------------------------- |
| 1    | SO-01 输出端敏感内容拦截 | bbac141 | OutputContentFilter.cs (new), SafetyOptions.cs, SafetyInputFilter.cs, StandardPipelineFactory.cs, appsettings.json, Extensions.cs |
| 2    | EX-01 Exchange 骨架编译确认 | n/a (no changes) | Verified: ExchangeWorkflow.cs, ExchangeMessages.cs, 7 Executors |

## Implementation Details

### OutputContentFilter (new file)
- `IsBlocked(string output)` — checks all 3 category keyword arrays
- `GetMatchedCategory(string output)` — returns "violence" / "pornography" / "politics" / null, checked in order
- `GetFirstMatchedKeyword(string output)` — returns the first matched keyword string
- Constructor accepts `SafetyOptions`, reads keyword arrays from config (no hardcoding)

### SafetyOptions (extended)
- `BlockedOutputCategories` — enabled categories, default `["violence", "pornography", "politics"]`
- `ViolenceMessageTemplate` / `PornographyMessageTemplate` / `PoliticsMessageTemplate` — 3 fallback messages
- `ViolenceKeywords` / `PornographyKeywords` / `PoliticsKeywords` — keyword arrays for matching

### SafetyOutputFilter (extended)
- Original `ProcessOutput(string)` preserved for backward compatibility
- New overload `ProcessOutput(string, SafetyOptions?, OutputContentFilter?)` — PII redact first, then content filter
- On match: throws `SafetyViolationException("output_content_blocked", messageTemplate)`

### SafetyOutputDelegatingClient (modified)
- Constructor now requires `SafetyOptions` + optional `OutputContentFilter`
- `GetResponseAsync` catches `SafetyViolationException` and returns ChatResponse with fallback message

### StandardPipelineFactory.CreatePipeline (updated)
- New parameter `SafetyOptions? safetyOptions = null`
- Creates `OutputContentFilter` and passes to `SafetyOutputDelegatingClient`

### DI Wiring (Extensions.cs)
- Binds all output-end properties from `configuration.GetSection("Safety")`
- Resolves `SafetyOptions` from DI and passes to `CreatePipeline`

### appsettings.json (updated)
- Added: BlockedOutputCategories, ViolenceKeywords, PornographyKeywords, PoliticsKeywords, 3 MessageTemplate properties

## Verification Results

| Check | Result |
|-------|--------|
| Framework build (0 errors) | PASS |
| WebApi build (0 errors) | PASS |
| Workflows build (0 errors, 0 warnings) | PASS |
| OutputContentFilter methods >= 3 | PASS (4 matches) |
| Exchange 7 Executors present | PASS |

## Decisions Made

1. OutputContentFilter uses instance pattern matching KeywordFilter style for consistency
2. SafetyOutputDelegatingClient constructor requires SafetyOptions to enforce awareness
3. CreatePipeline uses optional SafetyOptions? for backward compatibility with existing callers
4. All output-end properties bound from configuration in Extensions.cs

## Deviations from Plan

None - plan executed exactly as written.

**Auto-applied (Rule 2):**
- **Missing DI configuration binding** — Extensions.cs only bound input-side SafetyOptions properties; added binding for all output-end properties (BlockedOutputCategories, ViolenceKeywords, PornographyKeywords, PoliticsKeywords, 3 MessageTemplates) to ensure configuration flows from appsettings.json through DI to the filter.
- **SafetyOptions DI resolution in CreatePipeline** — Updated the DI lambda in Extensions.cs to resolve SafetyOptions from the service provider and pass it to CreatePipeline.

## Known Stubs

None. All functionality is wired end-to-end: configuration → DI → pipeline → filter → exception → fallback message.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: tampering | src/CallCenter.Framework/Safety/OutputContentFilter.cs | LLM response content filter — new trust boundary at SafetyOutputFilter.ProcessOutput |
| threat_flag: config | src/CallCenter.WebApi/appsettings.json | BlockedOutputCategories can be modified to disable categories |
