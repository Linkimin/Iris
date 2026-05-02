---
name: iris-debug
description: Systematic root cause investigation for bugs, test failures, build failures, and unexpected behavior in Iris. No fixes without confirmed root cause.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: debugging
  output_type: debug_report
---

# Iris Debug Skill

## Purpose

Use this skill to systematically debug failures in Iris — bugs, test failures, build failures, runtime crashes, or unexpected behavior. Debugging is a **diagnostic stage**: read-only root cause investigation before any fix is proposed or implemented.

This skill adapts the Superpowers `systematic-debugging` 4-phase methodology to Iris, with Iris-specific diagnostic commands, failure patterns, architecture-aware root cause tracing, and rationalization defense.

## Iron Law

**NO FIX PROPOSAL WITHOUT CONFIRMED ROOT CAUSE.**

If the agent has not completed Phase 1 (root cause investigation), it must not propose fixes. If the agent has not formed and tested at least one hypothesis (Phase 3), it must not proceed to Phase 4. Symptom ≠ root cause. Guessing ≠ debugging.

This rule is non-negotiable. The P1-001 timer cancellation regression was caused by exactly this pattern: a refactoring fix was applied without tracing root cause, and the new code introduced a worse invariant violation (dead `CancellationTokenSource`, unreachable cancellation, disposal leak).

## When to Use

**Activate this skill when:**

- the user reports a bug: "this test failed", "build is broken", "there's a bug", "X stopped working", "why does Y happen?", "the error says...";
- the agent discovers a failure during verification (build failure, test failure);
- unexpected runtime behavior is observed;
- the user asks "debug this" or "investigate this".

**Do NOT use debugging when:**

- the user asks for a review (use `/review`);
- the user asks for verification (use `/verify`);
- the user asks for a status report (use `/status`);
- the issue is a trivial typo or obvious single-character fix;
- the user explicitly waives debugging and demands a quick speculative fix (proceed but flag the P1-001 risk).

## Phase 1 — Root Cause Investigation

**Rule (FR-003):** Before any hypothesis or fix, complete these steps in order:

### 1.1 — Read Error Messages Completely

Read every line of the error output. Extract:

- Error code (CS1234, AVLN2000, MSB9008, etc.)
- File path and line number
- Stack trace summary (first 5-10 frames)
- Inner exception chain
- Which project or layer the error originates in

### 1.2 — Reproduce the Failure

Run the minimal command that triggers the failure:

- **Build failure:** `dotnet build <project-or-solution>`
- **Test failure:** `dotnet test --filter "FullyQualifiedName~<TestName>" --no-build`
- **Runtime crash:** Read the exception from startup output or logs

Run reproduction at least once. If reproduction fails (transient), document the attempt and note the fragility.

### 1.3 — Check Recent Changes

```powershell
git diff --stat
git log --oneline -10
```

Identify which files changed recently and whether the failure corresponds to a known change window.

### 1.4 — Trace Data Flow for Multi-Layer Failures

If the error crosses layers, trace backward to find the actual owner:

```text
Desktop (ViewModel) → IrisApplicationFacade → Application Handler → Adapter → External system
```

For each layer, check: Does this layer own the failing behavior, or is it just propagating an error from below?

### 1.5 — Run Iris-Specific Diagnostic Checks

Based on where the error manifests:

| Error location | Diagnostic command |
|---|---|
| Build failure | `dotnet build <project>` — read output, check missing types/usings |
| Build failure (references) | `dotnet list <project> reference` — check for forbidden refs |
| Test failure | `dotnet test --filter "<test>" --no-build` — isolate the test |
| Runtime crash | Inspect `DependencyInjection.cs` — check DI registration |
| Architecture smell | `Select-String -Path <dir> -Pattern '<forbidden import>'` (e.g., EF Core outside Persistence) |
| Format violation | `dotnet format --verify-no-changes` |

Do not run mutating commands. All diagnostics must be read-only.

## Phase 2 — Pattern Analysis

**Rule (FR-004):** After root cause is understood, compare against known Iris failure patterns. This accelerates diagnosis by recognizing recurring failure categories.

### Iris-Specific Failure Pattern Catalog

| Category | Pattern | Real Iris Example | Diagnostic Command |
|---|---|---|---|
| **Shortcut regression** | Refactoring "simplified" code broke invariants | P1-001: Timer replaced with `Task.Run`; dead `CancellationTokenSource`, unreachable cancellation | `git diff` on changed file; check spec FRs against changed code |
| **Flaky headless test** | Test passes locally, fails in CI or xUnit headless | T-04: `Dispatcher.UIThread.Post` throws in headless xUnit; state transition never visible | `dotnet test --filter "<test>" --no-build`; check for Avalonia dispatcher use |
| **Parallel build/test file lock** | Concurrent processes contend for output artifacts | CS2012: DLL in use during parallel build+test; SQLite temp DB lock in test cleanup | Run build sequentially; add `SqliteConnection.ClearAllPools()` |
| **Architecture boundary violation** | Production project references test project, or forbidden import | MSB9008: `Iris.Application` → test project ref; EF Core imported outside Persistence | `dotnet list <project> reference`; `Select-String -Path <dir> -Pattern 'Microsoft.EntityFrameworkCore'` |
| **DI/registration gap** | Service not registered, wrong lifetime, service locator | CS0246: `Task<>` not found because `IrisApplicationFacade` missing `using` (implicit usings not enabled in Desktop) | Inspect `DependencyInjection.cs`; check startup exceptions |
| **Missing implicit usings** | BCL types unresolved in projects without `<ImplicitUsings>` | `Iris.Desktop` missing `using System.Threading;` / `using System.Threading.Tasks;`; CS0246 for `Task<>` and `CancellationToken` | Check `.csproj` for `<ImplicitUsings>enable</ImplicitUsings>` |
| **Contract mismatch** | Method signature not updated (sync vs async), XAML binding broken | CS4032: `SendMessageAsync` not marked `async` after adding `await`; AVLN2000: XAML binds to removed `Greeting` property | Build output; `git diff` on related files |
| **Stale restore assets** | `--no-restore` uses outdated package cache after interrupted restore | Missing `Microsoft.Extensions.DependencyInjection` types after parallel test started before restore completed | `dotnet restore <project>` then retry |
| **Bundled tool access denied** | `rg.exe` blocked by app package execution policy | Multiple occurrences: `rg` access-denied throughout Phase 5 | Use `Get-ChildItem`, `Get-Content`, `Select-String` fallbacks |
| **XAML/placeholder build error** | Empty `.axaml` files compiled by Avalonia | AVLN1001: root element missing for zero-length XAML placeholders | `Get-ChildItem -Recurse -Filter '*.axaml' \| Where-Object { $_.Length -eq 0 }` |
| **Regex escape in PowerShell** | `Select-String` pattern contains regex metacharacters | `???` treated as regex quantifier; scan failed | Use `-SimpleMatch` switch |

## Phase 3 — Hypothesis and Verification

**Rule (FR-005):** Form a single, specific hypothesis. Test it before proposing a fix.

### 3.1 — Hypothesis Format

```
I think <X> is the root cause because <Y>.
Evidence: <Z>.
```

### 3.2 — Architecture Constraint Check

Before testing the hypothesis, verify the proposed fix direction does not violate Iris boundaries:

- Will this fix require Domain → EF Core? → Reject.
- Will this fix require Desktop → database directly? → Reject.
- Will this fix require Application → concrete adapter? → Reject.
- Will this fix put product behavior in Shared? → Reject.

### 3.3 — Minimal Verification

Propose the smallest diagnostic command to validate the hypothesis:

- "Let me add the missing `using` and rebuild that project."
- "Let me run just this one test in isolation."
- "Let me check if the project references are clean."

### 3.4 — Hypothesis Failure

If the hypothesis is disproven:

1. Document what was tried and why it failed.
2. Return to Phase 1 with the new evidence.
3. Do NOT accumulate unverified fixes. Each failed attempt is data, not debris.

### 3.5 — 3+ Failed Hypotheses → Escalate

**If 3 or more fix hypotheses fail**, stop debugging and escalate:

> 3 or more fix hypotheses have failed. This pattern suggests an architectural issue rather than a local bug. The bug may be a symptom of a design problem at the boundary level. Let's discuss the architecture before attempting more fixes.

This prevents the P1-001 cascade: guess → fail → guess → fail → guess → fail while shipping broken refactors.

## Phase 4 — Fix Proposal

**Rule (FR-006):** Only after root cause is confirmed.

### 4.1 — Describe Fix Approach

Describe **what** needs to change, not exact code. For example:

- "Restore the `CancellationTokenSource` wiring in `StartSuccessTimer`."
- "Add `using System.Threading.Tasks;` to `IrisApplicationFacade.cs`."
- "Remove the test project reference from `Iris.Application.csproj`."

### 4.2 — Identify Affected Layers

Which Iris layers/projects need changes:

- `Iris.Desktop/ViewModels/AvatarViewModel.cs` — Desktop concern
- `Iris.Application/...` — Application concern
- `.csproj` references — project boundary concern

### 4.3 — Verify Architecture Compliance

Confirm the fix does not create new boundary violations. Use the same check from Phase 3.2.

### 4.4 — Recommended Next Stage

| Scenario | Next stage |
|---|---|
| Fix is non-trivial (multi-file, logic change) | `/plan` |
| Fix is trivial/local (single file, obvious) | `/implement` (user must authorize) |
| 3+ failed hypotheses | "Escalate to architecture discussion" |
| Root cause is external (Ollama, SQLite, Avalonia) | Document workaround; no Iris fix needed |

The skill must **NOT** implement the fix. Implementation belongs to `/implement` (with a plan via `/plan`).

## Debug Report Output Format

**Rule (FR-007):** At the end of investigation, produce a Debug Report:

```markdown
## Debug Report: <Bug/Issue>

### Symptom
Exact error message, stack trace summary, observed behavior.

### Reproduction
Exact command or steps to reproduce.

### Evidence Gathered
- Diagnostics run and their results.
- Which layers were inspected.
- Commands that succeeded and failed.

### Root Cause
Confirmed or hypothesized (with confidence: High / Medium / Low).

### Affected Layers
Which Iris layers/projects are affected.

### Architecture Check
Whether the root cause or proposed fix violates any Iris boundary rule.

### Recommended Fix Approach
What needs to change (not exact code).

### Recommended Next Stage
Typically `/plan`. May be `/implement` for trivial fixes, or "escalate to architecture discussion" for 3+ failed hypotheses.
```

## Diagnostic Command Catalog

**Rule (FR-008):** Allowed and forbidden commands during debugging.

### Allowed (Read-Only)

| Command | Purpose | Notes |
|---|---|---|
| `dotnet build <solution-or-project>` | Reproduce build failure | Output only; does not modify source |
| `dotnet test <solution-or-project>` | Reproduce test failure | Executes tests; does not modify source |
| `dotnet test --filter "<name>" --no-build` | Isolate single test | Fastest reproduction |
| `dotnet list <project> reference` | Inspect project references | Detects boundary violations |
| `dotnet format --verify-no-changes` | Check formatting | Does not apply changes |
| `git diff` | Inspect working tree changes | Read-only |
| `git diff --stat` | Summary of changed files | Read-only |
| `git log --oneline -10` | Recent commit history | Read-only |
| `Select-String -Path <path> -Pattern <pat>` | Content search (PowerShell fallback for `rg`) | Use `-SimpleMatch` for literal patterns |
| `Get-ChildItem -Recurse -Filter "*.csproj"` | Find project files | Read-only |
| `Get-Content <file>` | Read file content | Read-only |

### Forbidden (Mutating)

| Command | Why forbidden |
|---|---|
| `dotnet format` (without `--verify-no-changes`) | Applies formatting changes |
| `git checkout` | Modifies working tree |
| `git reset` | Destructive |
| `git clean` | Destructive |
| File deletion | Destructive |
| `dotnet add package` / `dotnet remove package` | Modifies project |
| `dotnet ef migrations add` | Modifies schema |
| Snapshot/golden update commands | Modifies test expectations |

### Fallback Commands

When `rg` fails with access denied (bundled binary execution policy), use:

- `Get-ChildItem -Recurse -Filter "*.cs"` for file discovery
- `Select-String -Path <dir> -Pattern <pat>` for content search
- `Get-Content <file>` for reading files

## Architecture-Aware Debugging

**Rule (FR-009):** Every diagnostic investigation considers Iris layer ownership.

### Layer Tracing

When an error appears, identify which layer owns the failure:

| Error location | Check |
|---|---|
| **Desktop** (`src/Iris.Desktop/`) | Is Desktop calling something it shouldn't? Direct `IrisDbContext`? Direct Ollama/LM Studio? |
| **Application** (`src/Iris.Application/`) | Is Application referencing a concrete adapter? `using Iris.Persistence.*`? |
| **Domain** (`src/Iris.Domain/`) | Is Domain importing EF Core or HTTP? |
| **Adapter** (`Iris.Persistence`, `Iris.ModelGateway`, etc.) | Is the adapter implementing its port interface correctly? Is it calling another adapter? |
| **Build** (`.csproj` references) | Are project references clean? Production → test? Host → host? |
| **Test** (`tests/`) | Is the test in the right project? Testing through the right layer? |

### Boundary Violation Detection

Run these diagnostic searches when an architecture violation is suspected:

```powershell
# Does any non-Persistence project reference EF Core?
Select-String -Path src\Iris.Application\*.csproj -Pattern 'EntityFrameworkCore'
Select-String -Path src\Iris.Domain\*.csproj -Pattern 'EntityFrameworkCore'

# Does Desktop reference Persistence or ModelGateway directly?
Select-String -Path src\Iris.Desktop\*.csproj -Pattern 'Iris.Persistence|Iris.ModelGateway'

# Do production projects reference test projects?
dotnet list src\Iris.Application\Iris.Application.csproj reference
dotnet list src\Iris.Domain\Iris.Domain.csproj reference
```

## Rationalization Defense

**Rule (FR-010):** Table of resistance against the instinct to skip debugging:

| Rationalization | Correct Response |
|---|---|
| "I can see the problem, let me just fix it." | Seeing symptoms ≠ understanding root cause. Phase 1 first. |
| "This is a simple bug." | Simple bugs have root causes too. Minimum: Phase 1 + Phase 3. |
| "I'll fix it and if it doesn't work I'll debug." | First fix sets the pattern. The P1-001 regression was caused by exactly this — a quick refactor broke the timer cancellation invariant. Do it right from the start. |
| "It's probably just X." | Hypothesis without evidence is guessing. Run the diagnostic command first. |
| "The user wants speed." | Systematic debugging is faster than guess-and-check thrashing. One disciplined Phase 1 is cheaper than 3 failed guesses. |
| "I can reproduce it, that's enough." | Reproduction confirms the symptom, not the root cause. Trace data flow. |
| "The fix is obviously architecture-compliant." | Verify it. Silent boundary violations accumulate. One `using Iris.Persistence` in Application breaks the contract. |

## Handoff Rules

**Rule (FR-011):** The Debug Report serves as input to `/plan` or `/implement`.

- Debug Report is NOT a substitute for a plan. It provides context.
- When handing off, recommend the specific next stage with reasoning.
- If the user says "now fix it", proceed to `/plan` using the Debug Report as input, then `/implement`.
- If the user says "just implement the fix" for a trivial fix, verify it IS trivial (single file, obvious change) and proceed to `/implement` directly, flagging the risk.

## Stop Conditions

Stop or redirect debugging when:

- **User demands fix before root cause confirmed:** Respond with — "Root cause is not yet confirmed. The P1-001 regression was caused by fixing without debugging. Let me finish the investigation first." If user insists, proceed but flag the risk.
- **3+ fix hypotheses fail:** Escalate to architecture discussion (Phase 3.5).
- **Root cause is external (Ollama, SQLite, Avalonia):** Document the external component, version, evidence. Recommend workaround or configuration change. Do not propose Iris code changes for external bugs.
- **Cannot reproduce:** Document reproduction attempts. Note "not reproducible" with confidence. Recommend monitoring/logging for next occurrence.
- **Architecture violation detected in root cause:** Flag explicitly — "The root cause is an architecture boundary violation: [specific]. The correct fix is to [move responsibility to correct layer], not to patch the current location."
- **User abandons debugging:** No special recovery. Adapt naturally.

## Quality Checklist

Before concluding a debugging session, verify:

- [ ] Error messages were read completely (Phase 1.1).
- [ ] Failure was reproduced at least once (Phase 1.2).
- [ ] Recent changes were inspected (Phase 1.3).
- [ ] Data flow was traced for multi-layer failures (Phase 1.4).
- [ ] Iris-specific diagnostic checks were run (Phase 1.5).
- [ ] Root cause was compared against failure pattern catalog (Phase 2).
- [ ] At least one hypothesis was formed and tested (Phase 3).
- [ ] Architecture constraints were checked for every hypothesis (Phase 3.2).
- [ ] Fix approach was proposed without implementing (Phase 4).
- [ ] A Debug Report was produced with all 7 sections.
- [ ] No source files, tests, or configuration were modified.
- [ ] Only read-only diagnostic commands were run.
- [ ] The recommended next stage is explicit.
- [ ] The handoff to `/plan` or `/implement` is clear.

(This checklist is for the agent's internal quality control. Show "✅ Debug Complete — No files were modified." at the end of the debugging session.)
