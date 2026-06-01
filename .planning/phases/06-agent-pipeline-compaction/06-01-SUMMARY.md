# Phase 6 Plan 01 — Summary

## Objective

Implement 6-layer Pipeline components: KeywordFilter, PromptInjectionDetector, SafetyInputFilter, SafetyOutputFilter, Compaction wiring, ToolApproval shell, JsonlLogger, StandardPipelineFactory.

## What was built

### Task 1: KeywordFilter + PromptInjectionDetector
- KeywordFilter: 12 blocked keywords (投诉/诈骗/暴力 etc.) + IsBlocked + GetBlockedKeyword
- PromptInjectionDetector: 9 injection patterns (忽略之前指令/ignore previous etc.) + Detect

### Task 2: SafetyInputFilter + SafetyOutputFilter + SafetyPipelineAgent
- SafetyInputFilter.cs: ProcessInput — PII redact → keyword block → injection detect → throws SafetyViolationException
- SafetyOutputFilter.cs: ProcessOutput — PII redact only
- SafetyPipelineAgent.cs: DelegatingChatClient wrapper applying both filters

### Task 3: ToolApprovalAgent shell
- IToolApprovalAgent interface + DefaultToolApprovalAgent (always returns true)
- ToolApprovalOptions.cs: empty options class
- ToolApprovalDelegatingClient.cs: wraps IChatClient, checks approval before forwarding

### Task 4: JsonlLogger
- JsonlLogger.cs: writes JSONL to .logs/{sessionId}.jsonl, thread-safe via SemaphoreSlim

### Task 5: Compaction wiring
- CompactionExtensions.cs: UseCallCenterCompaction extension on ChatClientBuilder
- PipelineCompactionStrategy: SummarizationCompactionStrategy (8000 tokens) + SlidingWindowCompactionStrategy (8 turns)
- Uses MAF CompactionProvider — no custom implementation needed

### Task 6: StandardPipelineFactory
- CreatePipeline: assembles 6 layers — SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput
- Three internal wrapper classes: SafetyInputDelegatingClient, LoggingDelegatingClient, SafetyOutputDelegatingClient
- CreateSummarizerClient helper for qwen-plus model

## Key files created/modified

- NEW: SafetyInputFilter.cs, SafetyOutputFilter.cs, SafetyViolationException
- NEW: ToolApproval/ToolApprovalAgent.cs, ToolApprovalOptions.cs, ToolApprovalDelegatingClient.cs
- NEW: Logging/JsonlLogger.cs
- MODIFIED: KeywordFilter.cs, PromptInjectionDetector.cs, SafetyPipelineAgent.cs, CompactionExtensions.cs, StandardPipelineFactory.cs
- MODIFIED: CallCenter.Framework.csproj — added MAF + AI package references, NoWarn MAAI001

## Self-Check: PASSED

- Build: 0 errors, 0 warnings
