# Specification: Skill iris-debug

## 1. Problem Statement

When something breaks in Iris — a test fails, a build fails, a runtime crash occurs — the agent has no systematic debugging methodology. Without a skill, the agent falls back on its default behavior: guess a fix, try it, guess another fix, try again. This wastes time and introduces new bugs (e.g., P1-001 timer cancellation regression, where a refactoring fix introduced a worse invariant violation).

The Superpowers `systematic-debugging` skill demonstrates that methodical debugging — 4 phases, Iron Law of "no fixes without root cause", pattern analysis, 3+ failures → question architecture — prevents this. Iris needs its own version that adds:

- **Iris-specific diagnostic commands**: `dotnet build`, `dotnet test`, `dotnet list reference`, `Select-String` for architecture boundary checks.
- **Iris-specific failure patterns**: boundary violations (EF Core outside Persistence), DI registration gaps, parallel build/test file locks, flaky headless tests, implicit usings missing, `--no-restore` stale assets.
- **Iris-specific architecture-aware root cause tracing**: when an error appears in Desktop, trace backward through facade → Application handler → adapter to find actual owner.

The current Iris workflow has `verify` (checking known state) and `review` (finding issues), but nothing between "something broke" and "write a fix plan". The debugging skill fills this gap as a **diagnostic stage** — read-only root cause investigation before any fix is proposed.

## 2. Goal

Add an `iris-debug` workflow skill that:
- lives in `.opencode/skills/iris-debug/SKILL.md`;
- activates when the user reports a bug, test failure, build failure, or unexpected behavior;
- uses a 4-phase debugging methodology adapted to Iris Clean/Hexagonal architecture:
  1. Root cause investigation (with Iris-specific diagnostic commands);
  2. Pattern analysis (with Iris-specific failure pattern catalog);
  3. Hypothesis and minimal test (read-only until root cause confirmed);
  4. Fix proposal (without implementing — implementation is a separate stage);
- enforces the Iron Law: no fix proposals without confirmed root cause;
- produces a Debug Report: symptom, reproduction, root cause, affected layers, hypothesis confidence, recommended fix approach, recommended next workflow stage (typically `plan`);
- integrates into Iris stage separation as a diagnostic stage between "something broke" and "fix it";
- never proposes fixes that violate Iris architecture (no shortcuts);
- can run read-only diagnostic commands (`dotnet build`, `dotnet test`, `dotnet list reference`, `Select-String`, `git diff`, `git log`) but never edits files.

## 3. Scope

### In Scope

- A new `.opencode/skills/iris-debug/SKILL.md` file.
- 4-phase debugging methodology adapted from Superpowers `systematic-debugging`, with Iris-specific enhancements:
  - Phase 1: diagnostic commands for Iris (.NET build/test/reference checks, architecture boundary checks, git history, DI registration inspection).
  - Phase 2: Iris-specific failure pattern catalog (boundary violations, DI problems, build environment issues, test quality issues, platform issues).
  - Phase 3: hypothesis formation with Iris architecture constraint check — "this fix must not violate `Shared ← Domain ← Application ← Adapters ← Hosts`".
  - Phase 4: fix proposal with recommended next stage (`/plan` for code fix, `/implement` for trivial, or "escalate to architecture discussion" for 3+ failed hypotheses).
- Debug Report output format: symptom, reproduction steps, evidence gathered, root cause, affected layers, hypothesis confidence, recommended fix approach, next stage.
- Integration into `iris-engineering` stage selection table: "There's a bug / test failure / build failure" → `/debug`.
- Integration into `AGENTS.md` workflow: appropriate placement (diagnostic stage, not on the main spec→design→plan→implement chain).
- Integration into `AGENTS.md` skills list.
- Integration into gate logic: debugging is a diagnostic stage, not a gate. It feeds into `/plan` but does not replace it.
- Reference to Superpowers `systematic-debugging` as conceptual inspiration only (no dependency, no import).

### Out of Scope

- Implementing bug fixes — the debugging skill proposes fixes, it does not implement them.
- Changing `.opencode/rules/workflow.md` (debugging is diagnostic, not a gate).
- Creating a new Iris project, test project, or code file.
- Automated debugging tools (no symbolic debugger, no profiler, no memory dump analysis).
- Runtime monitoring or observability infrastructure.
- Formal root cause analysis reports for production incidents.
- Integration with external bug tracking systems.

### Non-Goals

- Replicating Superpowers `systematic-debugging` exactly. Iris-debug adapts the methodology to .NET/Clean Architecture with specific diagnostic commands and failure patterns.
- Making debugging mandatory. It is an available stage. If the user wants a quick speculative fix, they can waive debugging and jump to `implement` — but the agent must flag the risk.
- Replacing `verify` (verification checks known acceptance criteria) or `review` (review finds issues in existing code). Debugging is reactive — it starts from a known failure.

## 4. Current State

### Existing Workflow

Current Iris workflow: `Spec → Design → Plan → Implement → Verify → Review → Audit`.

There is no diagnostic/debugging stage. When something breaks:
- `verify` reports that tests/build fail, but doesn't investigate why.
- `review` finds issues in code, but doesn't trace root causes.
- The agent jumps directly from "this test failed" to "let me try changing X".

### Known Failure Patterns (from Iris History)

The `log_notes.md` file documents 20+ resolved and open issues. These reveal recurring Iris-specific failure categories that the debug skill should recognize:

| Category | Examples | Diagnostic command |
|----------|----------|-------------------|
| Architecture boundary violations | Production refs to test projects, EF Core in non-Persistence | `dotnet list reference`, `Select-String` for forbidden imports |
| DI/registration issues | Service not registered, wrong lifetime, service locator | Inspect `DependencyInjection.cs`, check startup exceptions |
| Build environment issues | Parallel file locks, stale `--no-restore`, bundled `rg` denied | Sequential build, explicit restore, fallback commands |
| Test quality issues | Tests pass for incidental reasons, missing assertions, flaky in headless | Isolated test run, check assertions, check environment differences |
| Platform-specific issues | Windows file locks, working directory paths, implicit usings | Check `.csproj` for `<ImplicitUsings>`, check working directory |
| Shortcut-induced regressions | Refactoring that "simplified" code broke invariants (P1-001) | Check spec FRs against changed code |
| Contract mismatch | Method signature not updated (sync vs async), XAML bindings broken | Build errors, diff review |

### Existing Diagnostic Commands Available

The Iris repository already has these read-only diagnostic commands:

```powershell
dotnet build .\Iris.slnx                  # Full solution build
dotnet test .\Iris.slnx                    # Full solution test suite
dotnet test <project> --filter <test>      # Focused test run
dotnet list <project> reference            # Project references
dotnet format --verify-no-changes          # Format check
git diff                                    # Changed files
git log --oneline -10                       # Recent commits
Select-String -Path <path> -Pattern <pat>  # Content search
```

The debug skill should leverage these as its diagnostic toolkit.

## 5. Affected Areas

| Area | Impact |
|------|--------|
| `.opencode/skills/iris-debug/SKILL.md` | New file — primary artifact |
| `AGENTS.md` | Update workflow to include debugging stage. Update skills list. |
| `.opencode/skills/iris-engineering/SKILL.md` | Update stage selection table: add debug row. Update workflow stages table. |
| `.agent/log_notes.md` | Debugging sessions may produce entries for unresolved findings (read-only skill cannot write, but the agent may recommend logging). |

Files explicitly NOT affected:
- `.opencode/rules/workflow.md` — debugging is diagnostic, gates remain A-G.
- Iris source code, tests, project references, configuration.
- Any `.csproj`, `.slnx`, `.cs` files.

## 6. Functional Requirements

- **FR-001: Activation trigger.** The skill activates when the user reports a bug, test failure, build failure, unexpected behavior, or when the agent discovers a failure during verification. Trigger phrases include "this test failed", "build is broken", "there's a bug", "X stopped working", "why does Y happen?", "the error says...". The agent invokes the skill BEFORE proposing any fix.

- **FR-002: Iron Law.** The skill enforces: **no fix proposal without confirmed root cause.** If the agent has not completed Phase 1 (root cause investigation), it must not propose fixes. The skill must explicitly state this rule early in its content.

- **FR-003: Phase 1 — Root cause investigation.** The agent must complete these steps before proceeding:
  1. **Read error messages completely** — stack traces, line numbers, file paths, error codes.
  2. **Reproduce the failure** — run the minimal command that triggers it (e.g., `dotnet test --filter "FullyQualifiedName~TestName"`).
  3. **Check recent changes** — `git diff`, `git log --oneline -10`.
  4. **Trace data flow for multi-layer failures** — if the error is in Desktop, check: ViewModel → Facade → Handler → Adapter → External system. Identify which layer fails.
  5. **Run Iris-specific diagnostic checks** based on the layer:
     - Build failure: check `dotnet build` output, project references (`dotnet list reference`), missing usings.
     - Test failure: isolate the test, check environment differences (headless vs desktop), check test assertions.
     - Runtime crash: check startup DI registration, check configuration files.
     - Architecture smell: run `Select-String` for forbidden imports (EF Core outside Persistence, Avalonia outside Desktop, etc.).

- **FR-004: Phase 2 — Pattern analysis.** After root cause is understood, the agent compares against known Iris failure patterns:
  1. **Architecture boundary violation**: Does the failure involve a forbidden dependency or shortcut?
  2. **DI/registration issue**: Is a service missing or wrongly scoped?
  3. **Build environment issue**: Is it a parallel lock, stale restore, platform tooling?
  4. **Test quality issue**: Does the test pass for the wrong reason? Does it lack a proper assertion?
  5. **Shortcut regression**: Was a recent "simplification" actually breaking invariants?

  The skill should include a reference table of these patterns with diagnostic commands.

- **FR-005: Phase 3 — Hypothesis and verification.** The agent forms a single, specific hypothesis:
  1. **Format**: "I think X is the root cause because Y. Evidence: Z."
  2. **Architecture constraint check**: Before testing the hypothesis, verify the proposed fix direction does not violate Iris boundaries.
  3. **Minimal test**: Propose the smallest diagnostic command to validate (e.g., "let me check if adding this using fixes the build", "let me run just this one test in isolation").
  4. **If hypothesis fails**: Return to Phase 1 with new evidence. Do not accumulate unverified fixes.
  5. **If 3+ hypotheses fail**: Stop. Flag as potential architectural issue. Recommend discussion. "This pattern suggests the architecture itself may be wrong."

- **FR-006: Phase 4 — Fix proposal.** Only after root cause is confirmed:
  1. **Propose fix approach** — describe what needs to change, not exact code.
  2. **Identify affected layers** — which projects, classes, or contracts.
  3. **Verify architecture compliance** — confirm the fix does not create new boundary violations.
  4. **Recommended next stage**: `/plan` (for non-trivial), `/implement` (for trivial/local fix with explicit user authorization), or "escalate" (for architectural issues).
  5. The skill must NOT implement the fix. Implementation belongs to `implement` (with a plan).

- **FR-007: Debug Report output format.** The skill produces a Debug Report at the end of investigation:

  ```markdown
  ## Debug Report: <Bug/Issue>

  ### Symptom
  Exact error message, stack trace summary.

  ### Reproduction
  Exact command or steps.

  ### Evidence Gathered
  - Diagnostics run and their results.
  - Which layers were inspected.

  ### Root Cause
  Confirmed or hypothesized (with confidence level).

  ### Affected Layers
  Which Iris layers/projects are affected.

  ### Architecture Check
  Whether the root cause or proposed fix violates any Iris boundary rule.

  ### Recommended Fix Approach
  What needs to change (not exact code).

  ### Recommended Next Stage
  Typically `/plan`. May be `/implement` for trivial fixes, or "escalate to architecture discussion" for 3+ failed hypotheses.
  ```

- **FR-008: Read-only diagnostic commands.** The skill may run read-only commands:
  - `dotnet build` (non-mutating — it produces output, does not change source).
  - `dotnet test` (executes tests, does not modify source).
  - `dotnet list <project> reference` (reads project files).
  - `dotnet format --verify-no-changes` (checks formatting, does not apply changes).
  - `git diff`, `git log` (reads repository history).
  - `Select-String` / `Get-Content` (reads file content).
  
  The skill must NOT run: `dotnet format` (mutating), `dotnet test` with `--blame` that modifies outputs in unexpected ways, `git checkout`, `git reset`, file deletions.

- **FR-009: Architecture-aware debugging.** Every diagnostic investigation must consider Iris layer ownership:
  - Desktop error → check: is Desktop calling something it shouldn't? (direct DB, direct provider)
  - Application error → check: is Application referencing a concrete adapter?
  - Build error → check: are project references clean? (`dotnet list reference`)
  - Test failure → check: is the test in the right project? Does it test through the right layer?

- **FR-010: Rationalization defense.** The skill must include a rationalization table for when the agent is tempted to skip debugging:
  | Rationalization | Response |
  |---|---|
  | "I can see the problem, let me just fix it." | Seeing symptoms ≠ understanding root cause. Phase 1 first. |
  | "This is a simple bug." | Simple bugs have root causes too. Minimum: Phase 1 + Phase 3. |
  | "I'll fix it and if it doesn't work I'll debug." | First fix sets the pattern. Do it right from the start. |
  | "It's probably just X." | Hypothesis without evidence is guessing. Run the diagnostic command first. |
  | "The user wants speed." | Systematic debugging is faster than guess-and-check thrashing. |

- **FR-011: Handoff to plan/implement.** The Debug Report serves as input to `plan` or `implement`. The plan/implement agent uses it as context but starts its own independent workflow. Debug report is NOT a substitute for a plan.

## 7. Architecture Constraints

- **AC-001: Skill file location.** `iris-debug/SKILL.md` lives in `.opencode/skills/`. Follows existing Iris skill YAML frontmatter convention.

- **AC-002: No dependency on Superpowers.** The skill references Superpowers `systematic-debugging` only as conceptual inspiration. It does not import, load, or delegate to Superpowers skills.

- **AC-003: Diagnostic stage, not a gate.** Debugging is an optional diagnostic stage between "something broke" and "fix plan". It does not change the gate system (A-G). It feeds into Gate C (Plan) — the debugging output provides context for the fix plan but does not itself satisfy any gate.

- **AC-004: Read-only enforcement.** The skill must state explicitly: "Do not edit source files, tests, configuration, or documentation during debugging." Phase 4's fix proposal describes what to change, not the actual change. The skill enforces this with its own stop condition: if the user says "just fix it" during Phase 1-3, respond with "Root cause is not yet confirmed. Let me finish the investigation first."

- **AC-005: Architecture boundaries preserved in fix proposals.** Every fix proposal must be validated against Iris architecture:
  - No UI → database or UI → provider shortcuts.
  - No Application → concrete adapter dependencies.
  - No Domain → infrastructure dependencies.
  - No Shared gaining Iris product behavior.
  - No adapter-to-adapter dependencies without approved design.

- **AC-006: Iris-specific failure patterns must reference real Iris concepts.** The skill's pattern catalog must use Iris names: `Iris.Desktop`, `Iris.Application`, `Iris.Domain`, `Iris.Persistence`, `Iris.ModelGateway`, `DependencyInjection.cs`, `appsettings.json`, `AvatarViewModel`, `SendMessageHandler`, etc. No generic examples.

- **AC-007: No diagnostic commands that mutate source.** Allowed: `dotnet build`, `dotnet test`, `dotnet list reference`, `dotnet format --verify-no-changes`, `git diff`, `git log`, `Select-String`, `Get-Content`. Forbidden: `dotnet format` (mutating), `git checkout`, `git reset`, file deletion, package add/remove.

## 8. Contract Requirements

No Iris source-code contracts are affected. The only contract changes:

| Contract | Current behavior | Required behavior | Compatibility |
|----------|-----------------|-------------------|---------------|
| `iris-engineering` stage selection table | No debug row | "Bug / test failure / build failure" → `/debug` | Extended. |
| `AGENTS.md` workflow list | No debug stage | Debug added as diagnostic stage | Extended. |
| `AGENTS.md` skills list | Lists 8+ skills | Adds `iris-debug` | Extended. |

## 9. Data and State Requirements

No persisted data. No in-memory state beyond the conversation context. No database changes. No file system changes (diagnostic commands produce terminal output only).

## 10. Error Handling and Failure Modes

| Failure mode | Required behavior |
|-------------|-------------------|
| Cannot reproduce the failure | Agent documents reproduction attempts, states "not reproducible", recommends monitoring/logging for next occurrence. |
| Diagnostic commands fail (e.g., `rg` access denied) | Use fallback commands (`Get-ChildItem`, `Select-String`, `Get-Content`). Continue with available tools. |
| Root cause is in external system (Ollama, SQLite, Avalonia) | Document the external component, version, and evidence. Recommend workaround or configuration change. |
| Multiple root causes interact | Document each independently, state interaction pattern (e.g., "DI issue causes service to be null, null propagates to Desktop as NRE"). |
| User demands immediate fix without debugging | Agent responds: "I can do a quick fix, but without root cause investigation the risk of introducing a new bug is high. The P1-001 regression was caused by exactly this pattern." If user insists, proceed but flag the risk. |
| 3+ fix hypotheses fail | Stop. "3 or more fix hypotheses have failed. This pattern suggests an architectural issue. Let's discuss before attempting more fixes." |
| Architecture violation detected in root cause | Flag explicitly: "The root cause is an architecture boundary violation: [specific]. The correct fix is to [move responsibility to correct layer], not to patch the current location." |

## 11. Testing Requirements

This is a workflow skill — testing is manual/behavioral.

- **T-001: Debug flow.** Simulate: "The test `StateBecomesSuccessThenIdle` is failing with timeout." Agent: (a) loads iris-debug, (b) reads error details, (c) runs reproduction command, (d) checks recent changes, (e) forms hypothesis, (f) produces Debug Report.

- **T-002: Architecture violation detection.** Simulate a bug where Desktop directly calls `IrisDbContext`. Agent must: (a) run `Select-String` for forbidden imports, (b) flag as architecture violation, (c) recommend moving through Application/persistence abstraction.

- **T-003: Iron Law enforcement.** During Phase 1, ask "so what's the fix?" Agent must respond: "Root cause is not yet confirmed. Let me finish investigation first."

- **T-004: 3+ failures → architecture discussion.** After 3 failed fix proposals, agent must stop and recommend architecture discussion.

- **T-005: Debug Report format.** After a completed investigation, verify the Debug Report includes all required sections.

- **T-006: No file modification.** During a debugging session, verify no files were edited.

- **T-007: Stage selection.** Say "the build is broken." Verify the agent selects the debug stage.

## 12. Documentation and Memory Requirements

After implementation:
- Update `.agent/PROJECT_LOG.md` with the completed iteration.
- Update `.agent/overview.md` if debugging becomes the current active work.
- The skill itself documents Iris-specific failure patterns — this is self-documenting.

## 13. Acceptance Criteria

- [ ] `iris-debug/SKILL.md` exists at `.opencode/skills/iris-debug/SKILL.md` with valid YAML frontmatter.
- [ ] The skill loads correctly when referenced (no parse errors).
- [ ] `AGENTS.md` workflow includes debugging as a diagnostic stage.
- [ ] `AGENTS.md` skills list includes `iris-debug`.
- [ ] `iris-engineering/SKILL.md` stage selection table includes a row for debug with correct routing.
- [ ] `iris-engineering/SKILL.md` workflow stages table includes debug row.
- [ ] The skill contains a 4-phase debugging methodology: root cause → pattern analysis → hypothesis → fix proposal.
- [ ] The skill contains an Iris-specific failure pattern catalog (boundary violations, DI issues, build environment, test quality, shortcut regressions).
- [ ] The skill enforces Iron Law: "no fix proposal without confirmed root cause."
- [ ] The skill lists allowed and forbidden diagnostic commands.
- [ ] Every fix proposal example respects Iris architecture boundaries.
- [ ] The skill produces a Debug Report with all required sections.
- [ ] Manual test T-001 passes (debug flow).
- [ ] Manual test T-002 passes (architecture violation detection).
- [ ] Manual test T-003 passes (Iron Law enforcement).
- [ ] Manual test T-004 passes (3+ failures escalation).
- [ ] All existing Iris skills continue to load and function without regression.

## 14. Open Questions

No blocking open questions.
