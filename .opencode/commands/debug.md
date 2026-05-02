---
description: Systematic root cause investigation for bugs, test failures, build failures, and unexpected behavior — no fixes without confirmed root cause
agent: builder
---

# /debug

Use the `iris-engineering` skill.
Use the `iris-debug` skill.

Run a systematic debugging investigation for:

$ARGUMENTS

If the bug or failure description is empty, stop and ask what to investigate.

## Hard Rules

Do not implement fixes.
Do not edit source files.
Do not edit tests.
Do not edit configuration.
Do not update memory files.
Do not update snapshots.
Do not run destructive commands.
Do not propose a fix before completing Phase 1 (root cause investigation).
Do not run mutating commands (`dotnet format`, `git checkout`, `git reset`, file deletions, package changes).

Read-only diagnostic commands are allowed: `dotnet build`, `dotnet test`, `dotnet list reference`, `dotnet format --verify-no-changes`, `git diff`, `git log`, `Select-String`, `Get-Content`, `Get-ChildItem`.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-debug/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Build and Test Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Debugging Methodology

The debugging investigation must follow the `iris-debug` skill exactly — 4 phases, Iron Law enforced:

### Phase 1 — Root Cause Investigation

1. **Read error messages completely.** Stack traces, line numbers, error codes, inner exceptions.
2. **Reproduce the failure.** Run the minimal command that triggers it (e.g., `dotnet test --filter "FullyQualifiedName~TestName"`).
3. **Check recent changes.** `git diff --stat`, `git log --oneline -10`.
4. **Trace data flow** for multi-layer failures: Desktop → Facade → Handler → Adapter → External system.
5. **Run Iris-specific diagnostic checks** based on where the error manifests.

### Phase 2 — Pattern Analysis

Compare the root cause against known Iris failure patterns from the skill catalog:
- Shortcut regression (P1-001)
- Flaky headless test (T-04)
- Parallel build/test file locks
- Architecture boundary violations
- DI/registration gaps
- Missing implicit usings (Desktop)
- Contract mismatch (async signature, XAML bindings)
- Stale restore assets
- Other patterns as listed in `.opencode/skills/iris-debug/SKILL.md`

### Phase 3 — Hypothesis and Verification

1. Form a single, specific hypothesis: "I think X is the root cause because Y. Evidence: Z."
2. Check that the proposed fix direction respects Iris architecture boundaries.
3. Run the smallest diagnostic command to validate the hypothesis.
4. If hypothesis is disproven: return to Phase 1. Do NOT accumulate unverified fixes.
5. **If 3+ hypotheses fail:** stop and escalate to architecture discussion.

### Phase 4 — Fix Proposal (no implementation)

Only after root cause is confirmed:
1. Describe what needs to change (not exact code).
2. Identify affected layers/projects.
3. Verify architecture compliance.
4. Recommend next stage: `/plan` (non-trivial), `/implement` (trivial/local, user must authorize), or "escalate" (architectural issue).

### Iron Law

**NO FIX PROPOSAL WITHOUT CONFIRMED ROOT CAUSE.**

If the user asks "what's the fix?" during Phase 1–3, respond: "Root cause is not yet confirmed. The P1-001 regression was caused by exactly this — fixing without debugging. Let me finish the investigation first."

## Diagnostic Command Catalog

### Allowed (Read-Only)

| Command | Purpose |
|---|---|
| `dotnet build <solution-or-project>` | Reproduce build failure |
| `dotnet test <solution-or-project>` | Reproduce test failure |
| `dotnet test --filter "<name>" --no-build` | Isolate single test |
| `dotnet list <project> reference` | Inspect project references |
| `dotnet format --verify-no-changes` | Check formatting |
| `git diff` / `git diff --stat` | Inspect working tree |
| `git log --oneline -10` | Recent commits |
| `Select-String -Path <path> -Pattern <pat>` | Content search |
| `Get-ChildItem -Recurse -Filter "*.csproj"` | Find project files |
| `Get-Content <file>` | Read file content |

### Forbidden (Mutating)

Do not run: `dotnet format` (without `--verify-no-changes`), `git checkout`, `git reset`, `git clean`, file deletions, `dotnet add/remove package`, `dotnet ef migrations add`, snapshot/golden updates.

### Fallback Commands

If `rg` fails with access denied, use `Get-ChildItem`, `Select-String`, and `Get-Content` instead.

## Architecture-Aware Debugging

Every investigation must consider Iris layer ownership:

| Error location | Check |
|---|---|
| `src/Iris.Desktop/` | Is Desktop calling DB or providers directly? |
| `src/Iris.Application/` | Is Application referencing concrete adapters? |
| `src/Iris.Domain/` | Is Domain importing EF Core or HTTP? |
| Adapters | Is the adapter implementing its port correctly? |
| Build (.csproj) | Are project references clean? Production → test? |
| Tests | Is the test in the right project? |

If an architecture boundary violation is detected, flag it explicitly. The fix is to move responsibility to the correct layer — not to patch the current location.

## Debug Report Output

At the end of investigation, produce a Debug Report in this exact format:

```markdown
## Debug Report: <Bug/Issue>

### Symptom
<Exact error message, stack trace summary.>

### Reproduction
<Exact command or steps.>

### Evidence Gathered
- <Diagnostics run and results.>
- <Which layers were inspected.>

### Root Cause
<Confirmed or hypothesized, with confidence: High / Medium / Low.>

### Affected Layers
<Which Iris layers/projects are affected.>

### Architecture Check
<Whether the root cause or proposed fix violates any Iris boundary rule.>

### Recommended Fix Approach
<What needs to change — not exact code.>

### Recommended Next Stage
<Typically `/plan`. May be `/implement` for trivial fixes, or "escalate to architecture discussion" for 3+ failed hypotheses.>
```

## Output Rules

End every debugging session with:

```markdown
---

✅ Debug Complete — No files were modified during this investigation.
```

## Final Response Requirements

After the Debug Report is produced:

## Execution Note

No implementation was performed.
No files were modified.

## Next Stage

Run `/plan <issue>` (or `/implement` for trivial fixes) using the Debug Report as context.
