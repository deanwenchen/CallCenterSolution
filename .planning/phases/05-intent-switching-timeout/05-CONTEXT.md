# Phase 5: Intent Switching + Timeout - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning
**Source:** Manual context extraction (ROADMAP + REQUIREMENTS + codebase analysis)

<domain>
## Phase Boundary

This phase delivers three capabilities:
1. **Intent switching** — User mid-refund says "我要换货" → terminate refund → prompt "换货流程暂未实现"
2. **Intent re-recognition during confirmation** — User confirms refund with "我要投诉" → suspend workflow → re-recognize → return chitchat reply
3. **Session timeout** — 30 min warning (no workflow clear) + 60 min termination (clear workflow)

</domain>

<decisions>
## Implementation Decisions

### D-01: 30-minute timeout should NOT clear activeWorkflow
- **Locked decision:** Current `CheckTimeoutAsync` clears workflow at 30 min — this is WRONG per CD-04 spec
- 30 min = warning only, activeWorkflow stays intact so user can continue
- 60 min = terminate AND clear activeWorkflow

### D-02: IntentSwitchResult handling
- Current code already clears activeWorkflow and returns IntentSwitchResult in `ProcessAsync`
- Console demo just prints a message — acceptable for v1.1 (exchange not implemented yet)
- Message should be user-facing Chinese: "已终止退款流程，新意图 '换货' 暂未实现"

### D-03: Confirm re-recognition
- HandleRequest is synchronous, cannot call async RecognizeIntentAsync directly
- Solution: pass intent recognition as callback (Func delegate) to HandleRequest
- Unrecognized confirm reply → call RecognizeIntentAsync → branch on result

### D-04: Timeout message grading
- 30 min: "⚠ 30 分钟无操作警告，会话将在 60 分钟无操作后终止"
- 60 min: "会话已终止，超过 60 分钟无操作"

### Claude's Discretion
- Exact message strings and formatting in console output
- Whether to use Chinese or English in exception messages (internal: English, user-facing: Chinese)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/ROADMAP.md` — Phase 5 goal, success criteria, requirement IDs (IR-04, IR-05, CD-04)
- `.planning/REQUIREMENTS.md` — IR-04, IR-05, CD-04 definitions

### Existing Implementation (source of truth)
- `src/CallCenter.AgentHost/EntryPoint.cs` — CheckTimeoutAsync, ProcessAsync, IntentSwitchResult
- `src/CallCenter.ConsoleDemo/Program.cs` — Main loop, HandleRequest, IntentSwitchResult handling
- `src/CallCenter.Framework/Session/InMemorySessionStore.cs` — Session key storage patterns

</canonical_refs>

<specifics>
## Specific Ideas

- `EntryPoint.CheckTimeoutAsync()` — lines 96-116, already has 30/60 min branches, just needs behavior fix
- `HandleRequest` — lines 228-262, ConfirmRefundRequest block needs re-recognition for unrecognized replies
- IntentSwitchResult case — lines 85-88 in Program.cs, needs improved Chinese messages
- Session store keys: `activeWorkflow`, `lastActivity`, `lastCheckpoint`

</specifics>

<deferred>
## Deferred Ideas

- None — Phase 5 requirements fully captured in plans
- Actual ExchangeWorkflow implementation → v2 (WF-10)
- Actual Complaint intent handling → v2

</deferred>

---

*Phase: 05-intent-switching-timeout*
*Context gathered: 2026-06-01 via manual analysis*
