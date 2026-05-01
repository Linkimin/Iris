# Phase 5–6: Iris Audit Gates and Skill Tightening Pass — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add formal A-G audit gates into rules, skills, and command output formats (Phase 5), then tighten every Iris skill by removing rules duplication, vague wording, and boilerplate (Phase 6).

**Architecture:** Two sequential phases on shared `.opencode/` infrastructure. Phase 5 adds gate definitions and output format changes to rules, skills, and commands. Phase 6 performs editorial tightening on all 17 skills — removing rules duplication, vague language, and boilerplate while preserving concrete checks, stop conditions, examples, and output formats.

**Tech Stack:** Markdown, OpenCode command/skill/rule files, PowerShell for verification.

---

## File Structure

### Phase 5 files

| File | Role |
|---|---|
| `.opencode/rules/workflow.md` | Add explicit A-G gate labels to Required Gates table |
| `.opencode/skills/iris-engineering/SKILL.md` | Add formal gate definitions with A-G IDs, stop conditions, and gate decision table |
| `.opencode/skills/implement/SKILL.md` | Add pre-implementation gate check procedure |
| `.opencode/commands/implement.md` | Add explicit gate check section before editing |
| `.opencode/commands/spec.md` | Gate A status in output |
| `.opencode/commands/design.md` | Gate B status in output |
| `.opencode/commands/plan.md` | Gate C status in output |
| `.opencode/commands/verify.md` | Gate D status in output |
| `.opencode/commands/architecture-review.md` | Gate E status in output |
| `.opencode/commands/audit.md` | Gate F status in output |
| `.opencode/commands/update-memory.md` | Gate G status in output |

### Phase 6 files

| File | Line count (before) | Issues to fix |
|---|---|---|
| `.opencode/skills/spec/SKILL.md` | 353 | Core Rules 1-7 duplicate workflow.md rules; Anti-Patterns has vague items |
| `.opencode/skills/design/SKILL.md` | 428 | Core Rules 1-8 duplicate multiple rule files |
| `.opencode/skills/plan/SKILL.md` | 450 | Core Rules 1-9 duplicate rules |
| `.opencode/skills/implement/SKILL.md` | 387 | Core Rules 1-10 overlap rules; already edited in Phase 5 |
| `.opencode/skills/verify/SKILL.md` | 396 | Moderate rules overlap in Core Rules 3-8 |
| `.opencode/skills/audit/SKILL.md` | 559 | Major rules duplication; sections 1-10 rephrase review-audit.md and architecture rules |
| `.opencode/skills/architecture-boundary-review/SKILL.md` | 582 | Sections 1-7 duplicate iris-architecture skill and rules verbatim |
| `.opencode/skills/agent-memory/SKILL.md` | 525 | Core Rules 1-10 duplicate memory.md rules |
| `.opencode/skills/iris-architecture/SKILL.md` | 329 | Already reasonable; minor cleanup |
| `.opencode/skills/iris-memory/SKILL.md` | 378 | Already reasonable |
| `.opencode/skills/iris-review/SKILL.md` | 407 | Already reasonable; minor cleanup |
| `.opencode/skills/iris-verification/SKILL.md` | 341 | Already reasonable |
| `.opencode/skills/save-spec/SKILL.md` | 262 | Minor cleanup |
| `.opencode/skills/save-design/SKILL.md` | 265 | Minor cleanup |
| `.opencode/skills/save-plan/SKILL.md` | 297 | Minor cleanup |
| `.opencode/skills/save-audit/SKILL.md` | 302 | Minor cleanup |
| `.opencode/skills/iris-engineering/SKILL.md` | ~400 | Already edited in Phase 5 for gates |
| `.opencode/rules/workflow.md` | 36 | Already edited in Phase 5 |

---

## Phase 5 — Formal Audit Gates

### Task 5.1: Add A-G gate labels to workflow rules

**Files:**
- Modify: `.opencode/rules/workflow.md`

- [ ] **Step 1: Replace Required Gates section with gate-labeled version**

Replace the "Required Gates" section (lines 15-23) with:

```md
## Required Gates

| Gate | Name | Required when | Satisfied by |
|---|---|---|---|
| A | Spec exists | New features, behavior changes, architecture changes, persistence changes, provider changes, UI flows. Not required for typos or trivial local fixes. | `/spec` output or explicit user statement that task is trivial/local |
| B | Design exists | Architecture-affecting changes (new dependencies, public contracts, DI composition, persistence schema, adapter seams, host wiring, memory/tool/voice/perception behavior) | `/design` output or explicit approval that design is not needed |
| C | Plan exists | Any multi-file change | `/plan` output or explicit user authorization for small direct implementation |
| D | Verification completed | Before readiness claims | `/verify` output or explicit reason for skipped verification |
| E | Architecture review completed | Boundary changes (project references, DI, ports, adapters, hosts, Shared) | `/architecture-review` output |
| F | Audit completed | Before merge/readiness claim | `/audit` output |
| G | Memory updated | After meaningful completed work | `/update-memory` output or confirmed memory write during `/implement` |

`/implement` must stop if Gate C is missing.
```

- [ ] **Step 2: Verify the edit is correct**

```powershell
Select-String -Path .opencode\rules\workflow.md -Pattern 'Gate A','Gate B','Gate C','Gate D','Gate E','Gate F','Gate G'
```

Expected: 7 matches, one per gate.

- [ ] **Step 3: Commit**

```bash
git add .opencode/rules/workflow.md
git commit -m "feat: add A-G gate labels to workflow rules"
```

---

### Task 5.2: Add formal gate definitions to iris-engineering skill

**Files:**
- Modify: `.opencode/skills/iris-engineering/SKILL.md`

- [ ] **Step 1: Read the current readiness gates section**

The current `iris-engineering/SKILL.md` contains a "Readiness Gates" section with prose descriptions of gates. We need to replace it with formal A-G gate definitions, a gate decision table, and a stop condition.

- [ ] **Step 2: Replace the existing Readiness Gates and Gate Details sections**

Locate the sections "## Readiness Gates" and "## Gate Details" and replace them entirely with:

```md
## Audit Gates

These gates control whether implementation can safely proceed. Each gate has a label (A-G), a triggering condition, and a satisfying artifact.

### Gate A — Spec

**Required when:** new features, behavior changes, architecture changes, persistence changes, provider changes, UI flows.

**Not required when:** typos, local docs clarification, single obvious config correction.

**Satisfied by:** a `/spec` output produced in the current workflow, OR an explicit user statement that the task is trivial/local.

**Stop condition for `/implement`:** If Gate A is missing and the task is not trivially local, `/implement` must report the gap and stop.

### Gate B — Design

**Required when:** new dependencies, public contracts, DI composition, persistence schema, adapter seams, host wiring, memory/tool/voice/perception behavior.

**Satisfied by:** a `/design` output that covers the affected areas, OR explicit user approval that a design is not needed.

**Stop condition for `/implement`:** If Gate B is missing and the change is architecture-affecting, `/implement` must report the gap and offer to run `/design`.

### Gate C — Plan

**Required when:** any multi-file change.

**Satisfied by:** an approved `/plan` output, OR explicit user authorization for a small direct implementation.

**Stop condition for `/implement`:** If Gate C is missing and the change touches more than one file, `/implement` must stop. This is a hard stop — it must not proceed even if the user is impatient.

### Gate D — Verification

**Required when:** implementation has been performed and readiness is claimed.

**Satisfied by:** a `/verify` output with commands actually run, OR an explicit statement that verification was skipped with a reason and residual risk named.

**Not satisfied by:** "should pass", "looks good", or any claim without command evidence.

### Gate E — Architecture Review

**Required when:** project references, DI, ports, adapters, hosts, or Shared are touched.

**Satisfied by:** an `/architecture-review` output.

**Stop condition for `/implement`:** If Gate E was required and not run after implementation, `/audit` must report it as missing evidence.

### Gate F — Audit

**Required when:** a merge/readiness decision is needed.

**Satisfied by:** an `/audit` output with an explicit readiness decision and verification evidence.

**Stop condition:** No merge/readiness claim may be made without Gate F.

### Gate G — Memory

**Required when:** meaningful implementation work was completed.

**Satisfied by:** an `/update-memory` output, OR a confirmed memory write during `/implement` (prepend to `PROJECT_LOG.md`, update `overview.md` if status changed).

**Stop condition:** Gate G does not block implementation. It is a trailing obligation — if skipped, the next `/status` must flag it as debt.

### Gate Decision Table

| User asks | Minimum gates required | What to check |
|---|---|---|
| "Implement this trivial fix" | Gate C waived (single file) | Confirm single file |
| "Implement this feature" | Gate A, B (if architecture), C | Spec/design/plan must exist |
| "Is this ready to merge?" | Gate D, E (if boundary), F | Verify + audit must exist |
| "Update memory after that work" | Gate G only | Memory files updated |
| "Review this diff" | None required | Review is diagnostic |

### Gate Check Procedure for /implement

Before editing any file, the implement agent must run this checklist:

1. Identify whether the task is trivial/local or non-trivial.
2. If non-trivial: Gate A — is a spec present or explicitly waived?
3. If architecture-affecting: Gate B — is a design present or explicitly waived?
4. If multi-file: Gate C — is an approved plan present?
5. If Gate C missing and multi-file: **Stop. Do not proceed.**

All gate checks must cite evidence (file path, user message, or explicit statement). Missing gates must be reported in the implementation output.
```

- [ ] **Step 3: Verify the edit does not leave duplicate content**

```powershell
Select-String -Path .opencode\skills\iris-engineering\SKILL.md -Pattern 'Readiness Gates','Gate Details'
```

Expected: If the old sections still appear, they need to be removed.

- [ ] **Step 4: Verify the file still has the required sections**

```powershell
Select-String -Path .opencode\skills\iris-engineering\SKILL.md -Pattern '^## '
```

Expected output should include: `## Audit Gates`, `## Gate Decision Table`, `## Gate Check Procedure for /implement` (and the original sections like `## Iris Identity`, `## Workflow Stages`, etc.).

- [ ] **Step 5: Commit**

```bash
git add .opencode/skills/iris-engineering/SKILL.md
git commit -m "feat: add formal A-G gate definitions to iris-engineering skill"
```

---

### Task 5.3: Add gate pre-check to implement skill

**Files:**
- Modify: `.opencode/skills/implement/SKILL.md`

- [ ] **Step 1: Insert gate check section before "Implementation Procedure"**

Locate the section `## Implementation Procedure` (around line 211). Insert this section before it:

```md
## Pre-Implementation Gate Check

Before editing, verify that required audit gates are satisfied.

Use the gate definitions in `iris-engineering/SKILL.md` for A-G criteria.

### Check procedure

1. Is the task trivial/local? If yes, skip formal gates.
2. Gate A — spec: Is an approved/draft spec present? If the task is non-trivial and no spec exists, stop.
3. Gate B — design: Is the change architecture-affecting? If yes, is a design present?
4. Gate C — plan: Is the change multi-file? If yes, is an approved plan present? **If no plan and multi-file, hard stop.**
5. Gate D — verification: Not checked before implementation (runs after).
6. Gate E — architecture review: Not checked before implementation (runs after for boundary changes).
7. Gate F — audit: Not checked before implementation (runs after for readiness).
8. Gate G — memory: Not checked before implementation (runs after meaningful work).

### Gate C Hard Stop

If the planned change touches more than one file and no approved implementation plan exists:

```md
# Implementation Blocked

## Reason

Gate C failed: no approved implementation plan exists for a multi-file change.

## What Was Checked

- Task scope assessed
- No plan found in docs/plans/ or docs/superpowers/plans/

## Safe Next Step

Run `/plan <task>` to create an approved implementation plan.
```

Do not proceed to editing. Do not create an ad-hoc plan inline.
```

- [ ] **Step 2: Verify the edit exists**

```powershell
Select-String -Path .opencode\skills\implement\SKILL.md -Pattern 'Pre-Implementation Gate Check','Gate C Hard Stop'
```

Expected: 2 matches.

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/implement/SKILL.md
git commit -m "feat: add pre-implementation gate check to implement skill"
```

---

### Task 5.4: Add gate enforcement to implement command

**Files:**
- Modify: `.opencode/commands/implement.md`

- [ ] **Step 1: Insert gate check section after "Hard Rules" and before "Repository Context"**

Locate the end of the "Hard Rules" block (around line 37: `If repository context is missing...`). Insert this section between the Hard Rules block and the Repository Context shell call:

```md
## Audit Gate Check

Before editing, verify the required gates. Use the gate definitions and check procedure from:

- `.opencode/rules/workflow.md` (A-G labels and conditions)
- `.opencode/skills/iris-engineering/SKILL.md` (gate definitions, decision table, check procedure)

### Minimum Check

1. **Gate A — Spec:** Is an approved or draft spec available for this task, or is the task trivially local? If neither, stop.
2. **Gate B — Design:** Is this change architecture-affecting? If yes, is an approved design available? If not, stop.
3. **Gate C — Plan:** Is an approved plan available, or was the user explicitly asked and the task is a single trivial file? If multi-file without a plan, **hard stop**.

If any required gate is missing, respond with the blocked output format from `implement/SKILL.md`.

If all required gates are satisfied, proceed to editing.

### Example: Gate C Hard Stop

```md
# Implementation Blocked

## Reason

Gate C failed: this change affects multiple files (`path/a`, `path/b`) and no approved implementation plan exists.

## What Was Checked

- `docs/plans/` directory inspected
- `docs/superpowers/plans/` directory inspected
- No plan found

## Safe Next Step

Run `/plan <this task>` to produce an approved implementation plan before implementing.
```
```

- [ ] **Step 2: Verify the edit**

```powershell
Select-String -Path .opencode\commands\implement.md -Pattern 'Audit Gate Check','Gate A — Spec','Gate C Hard Stop'
```

Expected: 3 matches.

- [ ] **Step 3: Commit**

```bash
git add .opencode/commands/implement.md
git commit -m "feat: add audit gate enforcement to implement command"
```

---

### Task 5.5: Add gate status to spec command output

**Files:**
- Modify: `.opencode/commands/spec.md`

- [ ] **Step 1: Add gate status section to output format**

In the "Output Format" section, after the "Final Response Requirements" subsection, add:

```md
## Gate Status

After producing the specification, append this section to the output:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | This specification |
| B — Design | ⬜ Not yet run | Run `/design` when ready |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |

All gate status entries must use exactly these emoji:

- ✅ Satisfied — gate condition met
- ⬜ Not yet run — gate not yet executed
- ⚠️ Skipped — gate intentionally skipped with reason
- ❌ Blocked — gate required but cannot be satisfied
```

- [ ] **Step 2: Verify**

```powershell
Select-String -Path .opencode\commands\spec.md -Pattern 'Audit Gate Status','Gate A.*Spec'
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/commands/spec.md
git commit -m "feat: add gate A status to spec command output"
```

---

### Task 5.6: Add gate status to design command output

**Files:**
- Modify: `.opencode/commands/design.md`

- [ ] **Step 1: Add gate status section to output format**

In the "Output Format" section, after the "Final Response Requirements" subsection, add:

```md
## Gate Status

After producing the design, append this section to the output:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `<path to spec>` |
| B — Design | ✅ Satisfied | This design |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |

Use the same emoji legend as `/spec`.
```

- [ ] **Step 2: Verify**

```powershell
Select-String -Path .opencode\commands\design.md -Pattern 'Audit Gate Status','Gate B.*Design'
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/commands/design.md
git commit -m "feat: add gate B status to design command output"
```

---

### Task 5.7: Add gate status to plan command output

**Files:**
- Modify: `.opencode/commands/plan.md`

- [ ] **Step 1: Add gate status section to output format**

In the output format area, after "Final Response Requirements", add:

```md
## Gate Status

After producing the plan, append this section to the output:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `<path to spec>` |
| B — Design | ✅ Satisfied | `<path to design>` |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |
```

- [ ] **Step 2: Verify**

```powershell
Select-String -Path .opencode\commands\plan.md -Pattern 'Audit Gate Status','Gate C.*Plan'
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/commands/plan.md
git commit -m "feat: add gate C status to plan command output"
```

---

### Task 5.8: Add gate status to verify command output

**Files:**
- Modify: `.opencode/commands/verify.md`

- [ ] **Step 1: Add gate status section to output format**

After the "Verification Limits" section in the output format, add:

```md
## Gate Status

After verification completes, append:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ⬜ Not checked | This command does not check Gate A |
| B — Design | ⬜ Not checked | This command does not check Gate B |
| C — Plan | ⬜ Not checked | This command does not check Gate C |
| D — Verify | ✅ Satisfied / ⚠️ Skipped / ❌ Failed | See verification results above |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |
```

- [ ] **Step 2: Verify**

```powershell
Select-String -Path .opencode\commands\verify.md -Pattern 'Audit Gate Status','Gate D.*Verif'
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/commands/verify.md
git commit -m "feat: add gate D status to verify command output"
```

---

### Task 5.9: Add gate status to architecture-review command output

**Files:**
- Modify: `.opencode/commands/architecture-review.md`

- [ ] **Step 1: Add gate status section to output format**

After "Execution Note", add:

```md
## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ⬜ Not checked | This review may reference a spec but does not produce one |
| B — Design | ⬜ Not checked | This review may reference a design but does not produce one |
| C — Plan | ⬜ Not checked | This review may reference a plan but does not produce one |
| D — Verify | ⬜ Not checked | Run `/verify` separately |
| E — Architecture Review | ✅ Satisfied | This review |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |
```

- [ ] **Step 2: Commit**

```bash
git add .opencode/commands/architecture-review.md
git commit -m "feat: add gate E status to architecture-review command output"
```

---

### Task 5.10: Add gate status to audit command output

**Files:**
- Modify: `.opencode/commands/audit.md`

- [ ] **Step 1: Add gate status section to output format**

After "Execution Note", add:

```md
## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | Reviewed / ⚠️ Missing | `<path>` or "Not available" |
| B — Design | Reviewed / ⚠️ Missing | `<path>` or "Not available" |
| C — Plan | Reviewed / ⚠️ Missing | `<path>` or "Not available" |
| D — Verify | Reviewed / ⚠️ Missing | Verification evidence section above |
| E — Architecture Review | In audit / ⚠️ Not run | Architecture pass findings above |
| F — Audit | ✅ Satisfied | This audit |
| G — Memory | Checked / ⚠️ Not updated | Memory review above |

Each gate that was checked should cite the relevant spec/design/plan/verify/architecture-review path. Missing gates should be flagged as ⚠️.
```

- [ ] **Step 2: Commit**

```bash
git add .opencode/commands/audit.md
git commit -m "feat: add gate F status to audit command output"
```

---

### Task 5.11: Add gate status to update-memory command output

**Files:**
- Modify: `.opencode/commands/update-memory.md`

- [ ] **Step 1: Add gate status section to output format**

In the "Output Format" section, after "Agent Memory Updated" block and before "## If Memory Cannot Be Updated", add:

```md
## Gate Status

After memory update, append:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ⬜ Not applicable | Gate A is a pre-implementation gate |
| B — Design | ⬜ Not applicable | Gate B is a pre-implementation gate |
| C — Plan | ⬜ Not applicable | Gate C is a pre-implementation gate |
| D — Verify | ⬜ Not applicable | Gate D is verified separately |
| E — Architecture Review | ⬜ Not applicable | Gate E is reviewed separately |
| F — Audit | ⬜ Not applicable | Gate F is audited separately |
| G — Memory | ✅ Satisfied | This memory update |
```

- [ ] **Step 2: Commit**

```bash
git add .opencode/commands/update-memory.md
git commit -m "feat: add gate G status to update-memory command output"
```

---

## Phase 5 Verification

### Task 5.12: Verify all Phase 5 changes

- [ ] **Step 1: Run config validation**

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"
```

Expected: `opencode.jsonc ok`

- [ ] **Step 2: Inspect all modified commands for gate tables**

```powershell
Get-ChildItem .opencode\commands -Filter *.md | ForEach-Object {
    $matches = Select-String -Path $_.FullName -Pattern 'Audit Gate Status'
    if ($matches) { Write-Output "$($_.Name): has gate table" }
    else { Write-Output "$($_.Name): MISSING gate table" }
}
```

Expected: All 14 command files should report "has gate table" OR be exempt (review.md and status.md may not need gates).

- [ ] **Step 3: Verify gate A-G references exist in rules and skills**

```powershell
Select-String -Path .opencode\rules\workflow.md -Pattern 'Gate [A-G]'
Select-String -Path .opencode\skills\iris-engineering\SKILL.md -Pattern 'Gate [A-G]'
Select-String -Path .opencode\skills\implement\SKILL.md -Pattern 'Gate [A-G]'
Select-String -Path .opencode\commands\implement.md -Pattern 'Gate [A-G]'
```

Expected: 7+ matches in workflow.md, 14+ in iris-engineering, 7+ in implement skill, 7+ in implement command.

- [ ] **Step 4: Inspect git diff for sanity**

```powershell
git diff --stat
```

- [ ] **Step 5: Commit Phase 5 verification result**

```bash
git add .opencode/commands/spec.md .opencode/commands/design.md .opencode/commands/plan.md .opencode/commands/verify.md .opencode/commands/architecture-review.md .opencode/commands/audit.md .opencode/commands/update-memory.md
# These should already be committed from earlier tasks; if not, stage them
git commit -m "verify: confirm Phase 5 gate tables present in all 11 target command files"
```

---

## Phase 6 — Skill Tightening Pass

### Task 6.1: Tighten spec skill

**Files:**
- Modify: `.opencode/skills/spec/SKILL.md`

- [ ] **Step 1: Replace Core Rules 1-7 with condensed rules-referencing version**

Locate the `## Core Rules` section (lines 64–167). The current rules 1-7 describe behavior that is already defined in:
- `.opencode/rules/workflow.md` (Spec -> Design -> Plan -> Implement sequence)
- `.opencode/rules/iris-architecture.md` (preserve architecture boundaries)
- `.opencode/rules/no-shortcuts.md` (forbidden shortcuts)

Replace rules 1-7 (the entire `### 1. Separate Problem from Solution` through `### 7. Avoid Implementation Drift`) with:

```md
## Core Rules

These rules govern specification behavior. Hard prohibitions live in:
- `.opencode/rules/workflow.md`
- `.opencode/rules/iris-architecture.md`
- `.opencode/rules/no-shortcuts.md`

### 1. Separate Problem from Solution

The spec defines the required outcome, not the implementation sequence. Avoid premature decisions about classes, file names, algorithms, or library choices unless they are explicit constraints.

### 2. Define Scope Boundaries

Every spec must include: in scope, out of scope, non-goals, affected areas, explicitly forbidden changes.

### 3. Preserve Architecture

Respect project boundaries. Do not allow: bypassing Application, moving business logic to UI/infrastructure, adding infrastructure dependencies to Domain/Application, replacing architecture without approval, large drive-by refactors.

### 4. Define Acceptance Criteria

Acceptance criteria must be concrete and verifiable (e.g., "`dotnet test` passes", "Application does not reference EF Core"). Avoid vague criteria like "code should be clean" or "system should work better."

### 5. Define Contracts and Failure Modes

If the task touches a contract, document it (API, domain model, database schema, config, UI behavior, etc.). For each: state whether unchanged, extended backward-compatibly, changed, deprecated, or removed. Describe expected failure modes (invalid input, missing data, provider unavailable, cancellation, etc.).

### 6. Stay in Specification Stage

The spec must not become a design or plan. Avoid: step-by-step coding sequences, detailed class implementations, speculative abstractions, unrelated cleanup lists. Those belong in `/design` and `/plan`.
```

This replaces 104 lines of rules (1-7) with roughly 30 lines. The key content is preserved (Separate Problem from Solution, Define Scope, Preserve Architecture, Acceptance Criteria, Contracts, Stage Separation) while removing duplication from rule files.

- [ ] **Step 2: Clean up Anti-Patterns section**

Replace the Anti-Patterns section (lines ~328–341) with this trimmed version:

```md
## Anti-Patterns

Avoid:
- vague requirements or hidden scope expansion;
- combining spec, design, and plan into one document;
- inventing architecture not present in the project;
- omitting compatibility expectations or failure modes;
- adding implementation details too early;
- using the spec as permission for unrelated cleanup;
- writing acceptance criteria that cannot be tested.
```

Removes: "improve everything language" (vague itself), "writing acceptance criteria that cannot be tested" (kept, it's concrete).

- [ ] **Step 3: Verify line count reduction**

```powershell
(Get-Content .opencode\skills\spec\SKILL.md).Count
```

Expected: Reduction from 353 to approximately 280 lines.

- [ ] **Step 4: Commit**

```bash
git add .opencode/skills/spec/SKILL.md
git commit -m "refactor: tighten spec skill - remove rules duplication, trim anti-patterns"
```

---

### Task 6.2: Tighten design skill

**Files:**
- Modify: `.opencode/skills/design/SKILL.md`

- [ ] **Step 1: Replace Core Rules 1-8 with condensed version**

Replace `### 1. Design Implements the Spec` through `### 8. Avoid Implementation Planning` with:

```md
## Core Rules

Hard prohibitions live in `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, and `.opencode/rules/workflow.md`.

### 1. Implement the Spec

The design traces back to the specification. Do not introduce new requirements, expand product scope, or change acceptance criteria.

### 2. Preserve Architecture

Do not design shortcuts (UI→persistence, UI→providers, Domain→infrastructure, Application→concrete adapters, Tools owning policy, Voice owning orchestration, Perception owning memory extraction, hosts owning business logic). Respect the project's existing boundaries.

### 3. Define Ownership, Contracts, and Data Flow

For each new behavior: assign a clear owner layer (Domain/Application/Adapter/Host/Shared). Define the contract shape, owner, consumers, compatibility, and error behavior. Describe how data moves through the system including entry point, orchestration, adapter calls, persistence, and error paths.

### 4. Define Failure Handling and Options

Describe error handling for each failure mode (invalid input, not found, timeout, permission denied, cancellation, etc.). When multiple viable approaches exist, include a short options comparison with benefits, drawbacks, and recommendation.

### 5. Stay in Design Stage

Do not produce step-by-step coding sequences, file-by-file checklists, or exact phase breakdowns. Those belong in `/plan`.
```

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns with:

```md
## Anti-Patterns

Avoid:
- designing without a spec or stated assumptions;
- hidden scope expansion or mixing design with implementation plan;
- inventing architecture that conflicts with the project;
- vague component ownership or duplicate responsibility across layers;
- bypassing existing abstractions;
- omitting error handling or testing design;
- adding speculative abstractions or large rewrites without approval.
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/design/SKILL.md
git commit -m "refactor: tighten design skill - remove rules duplication, trim anti-patterns"
```

---

### Task 6.3: Tighten plan skill

**Files:**
- Modify: `.opencode/skills/plan/SKILL.md`

- [ ] **Step 1: Replace Core Rules 1-9 with condensed version**

Replace `### 1. Plan Implements the Design` through `### 9. Avoid Over-Planning` with:

```md
## Core Rules

Hard prohibitions live in `.opencode/rules/workflow.md`, `.opencode/rules/iris-architecture.md`, and `.opencode/rules/no-shortcuts.md`.

### 1. Follow the Design

The plan implements the approved design. Do not introduce new requirements, change acceptance criteria, or change architecture decisions. If the design is unsafe, stop and report.

### 2. Small Safe Phases

Each phase must be narrow, reviewable, reversible, and tied to verification. Avoid "implement everything" phases.

### 3. Identify Files and Verification

For each phase: list files to inspect, files likely to edit, files not to touch. Each phase must have a verification step (build, test, architecture test, manual check, or diff review).

### 4. Include Tests, Docs, and Rollback

Tests are first-class work — specify level, target project, positive/negative/regression cases. Documentation updates are required when behavior, architecture, setup, or contracts change. Each phase must include rollback guidance.

### 5. Stay in Planning Stage

Avoid: exact line numbers until inspected, speculative class names, excessive micro-steps, implementation code, or unrelated commands. The plan must be actionable, not bureaucratic.
```

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns with:

```md
## Anti-Patterns

Avoid:
- implementing inside the plan;
- changing the design or spec;
- hiding risky steps or one giant phase;
- vague "add tests" wording;
- drive-by refactoring;
- creating new files before checking existing placeholders;
- skipping rollback notes or verification;
- treating docs/memory as optional when behavior changes.
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/plan/SKILL.md
git commit -m "refactor: tighten plan skill - remove rules duplication, trim anti-patterns"
```

---

### Task 6.4: Tighten implement skill (post-Phase 5 edits)

**Files:**
- Modify: `.opencode/skills/implement/SKILL.md`

This file was already edited in Phase 5 (Task 5.3: gate check added). Post-Phase 5, remove the remaining rules duplication.

- [ ] **Step 1: Replace Core Rules 1-10 with condensed version**

Replace `### 1. Implement the Plan Only` through `### 10. Be Honest About Partial Completion` with:

```md
## Core Rules

Hard prohibitions live in `.opencode/rules/workflow.md`, `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/dotnet.md`, and `.opencode/rules/verification.md`.

### 1. Only the Plan

Do not introduce new requirements, change public contracts beyond the plan, add unrelated refactors, or add speculative abstractions. Every changed file must be justified by the plan.

### 2. Minimal Diff, Inspect First

Prefer small focused changes, existing abstractions, and existing patterns. Before creating a file, check existing structure. Before adding a package, check existing dependencies and central package management.

### 3. Architecture and Error Handling

Preserve boundaries: no UI→persistence, UI→providers, Domain→infrastructure, Application→concrete adapters. Implement failure behavior as specified — don't swallow exceptions, leak provider exceptions through application contracts, or log sensitive data.

### 4. Tests and Verification

Tests must be added or updated where behavior changes. Run the narrowest useful verification first. Report partial completion honestly — completed work, skipped work, failed checks, known risks.

### 5. Documentation and Memory

Update docs when implementation changes public behavior, architecture, setup, configuration, contracts, or persistence schema. Update `.agent/` memory files when required by project convention.
```

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns with:

```md
## Anti-Patterns

Avoid:
- "while I was here" refactors or implementing beyond the plan;
- creating duplicate abstractions;
- bypassing layers to make features work faster;
- hiding or removing failing tests;
- claiming commands passed without running them;
- adding packages without need;
- changing public contracts silently;
- broad formatting changes unrelated to the task;
- using implementation as a reason to rewrite the design.
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/implement/SKILL.md
git commit -m "refactor: tighten implement skill - remove rules duplication"
```

---

### Task 6.5: Tighten verify skill

**Files:**
- Modify: `.opencode/skills/verify/SKILL.md`

- [ ] **Step 1: Replace Core Rules 1-8 with condensed version**

Replace `### 1. Verify, Do Not Fix` through `### 8. No Destructive Operations` with:

```md
## Core Rules

Hard rules live in `.opencode/rules/verification.md` and `.opencode/rules/dotnet.md`.

### 1. Verify, Don't Fix

Verification must not modify files. Run build, tests, format checks in verify mode, dependency inspection, and diff inspection. Do not edit files, apply mutating formatters, update snapshots, or delete tests.

### 2. Use Repository Commands

Prefer commands from repo conventions (README, CI, build scripts, .sln/.slnx). For .NET: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`. Use narrower commands first for large repos.

### 3. Report Honestly

Report exact commands and results. Never say "tests pass" if only build ran. Never say "verified" when checks were skipped. Classify failures: build, test, architecture, formatting, environment, flaky, pre-existing. If verification is partial, say so.

### 4. No Destruction

Never run `git push`, `git clean`, `git reset --hard`, `rm -rf`, `docker system prune`, or destructive database commands.
```

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns with:

```md
## Anti-Patterns

Avoid:
- saying "looks good" without running checks;
- claiming tests passed when only build ran;
- running mutating formatters without permission;
- updating snapshots during verification;
- deleting failing tests;
- treating environment failures as success;
- calling failures pre-existing without evidence;
- hiding files changed by verification.
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/verify/SKILL.md
git commit -m "refactor: tighten verify skill - remove rules duplication"
```

---

### Task 6.6: Tighten audit skill (major trim)

**Files:**
- Modify: `.opencode/skills/audit/SKILL.md`

This skill is 559 lines and has heavy rules duplication. The Core Rules 1-10 rephrase content already in `review-audit.md`, `iris-architecture.md`, and `no-shortcuts.md`. Keep the audit procedure, output format, and finding format. Trim rules.

- [ ] **Step 1: Replace Core Rules 1-10 with condensed version**

Replace `### 1. Audit, Do Not Fix` through `### 10. Produce Actionable Output` with:

```md
## Core Rules

Hard rules live in `.opencode/rules/review-audit.md`, `.opencode/rules/iris-architecture.md`, and `.opencode/rules/no-shortcuts.md`.

### 1. Read-Only

The audit is read-only. Inspect files, diffs, tests, and documentation. Run safe verification if needed. Do not edit source, tests, docs, config, snapshots, or golden outputs. Do not commit or run destructive commands.

### 2. Four Required Passes

Every audit must include: (1) Spec Compliance, (2) Test Quality, (3) SOLID / Architecture Quality, (4) Clean Code / Maintainability.

### 3. Evidence-Based Severity

Findings must cite file/symbol/command evidence, impact, and recommended fix. Classify: P0 (must fix: correctness, data loss, security, broken build, architecture break), P1 (should fix: high-risk maintainability, incomplete tests, risky coupling), P2 (backlog: cleanup, naming, minor gaps), Note (observation only).

### 4. Check Architecture and Tests

Check for boundary violations (UI→persistence, UI→providers, Domain→infrastructure, Application→concrete adapters, Tools owning policy, Voice owning orchestration, Perception owning memory, hosts owning business logic, Shared containing product behavior). Assess tests for behavior coverage, meaningful assertions, positive/negative cases, and correct level. Test existence is not test quality.

### 5. Check Spec/Design/Plan Compliance

If spec/design/plan exist, audit against them: scope, non-goals, acceptance criteria, architecture constraints, contract compatibility, forbidden changes. If verification was not run, the audit must say so.
```

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns with:

```md
## Anti-Patterns

Avoid:
- rubber-stamp approval or vague review comments;
- severity inflation or minimization;
- ignoring missing verification;
- treating compile success as correctness or test existence as quality;
- auditing only style while missing architecture;
- proposing broad rewrites as first fix;
- inventing requirements not in the spec;
- hiding uncertainty;
- editing files during audit.
```

- [ ] **Step 3: Verify line count reduction**

```powershell
(Get-Content .opencode\skills\audit\SKILL.md).Count
```

Expected: Reduction from 559 to approximately 350 lines.

- [ ] **Step 4: Commit**

```bash
git add .opencode/skills/audit/SKILL.md
git commit -m "refactor: tighten audit skill - major trim, remove rules duplication"
```

---

### Task 6.7: Tighten architecture-boundary-review skill (major trim)

**Files:**
- Modify: `.opencode/skills/architecture-boundary-review/SKILL.md`

This skill is 582 lines. Core Rules 1-10 (lines 82-311) duplicate content from `iris-architecture/SKILL.md` (dependency direction, layer ownership, forbidden shortcuts) and `iris-architecture.md` rules. Sections 2-7 (Protect Domain, Protect Application, Protect Adapters, Protect Hosts, Protect Shared, Detect Forbidden Shortcuts, Review Project References) are largely copies.

- [ ] **Step 1: Replace Core Rules 1-10 with references**

Replace `### 1. Preserve Dependency Direction` through `### 10. Review Tests` with:

```md
## Core Rules

### 1. Preserve Architecture

Use the authoritative architecture definitions in:
- `.opencode/rules/iris-architecture.md` (dependency direction, project reference rules, DI rules)
- `.opencode/rules/no-shortcuts.md` (forbidden shortcuts)
- `.opencode/skills/iris-architecture/SKILL.md` (placement decision table, boundary smell checklist, project reference checks, DI checks)

The expected direction is: `Shared ← Domain ← Application ← Adapters ← Hosts`.

Key checks for this review:
- **Domain:** Must not contain EF Core, HTTP, UI, infrastructure. Pure concepts only.
- **Application:** Must not depend on concrete adapters (Persistence, ModelGateway, Perception, Tools, Voice, Infrastructure). Owns ports, use cases, policies.
- **Adapters:** Implement Application abstractions. Must not own business rules, product workflow, or permission decisions.
- **Hosts:** Compose and present. Must not own domain rules, application logic, persistence, or provider logic.
- **Shared:** Neutral primitives only. Must not contain product/domain behavior.

### 2. Detect Shortcuts

Flag: UI→database, UI→provider, Domain→infrastructure, Application→concrete adapter, Tools owns policy, Voice owns orchestration, Perception owns memory, host owns business logic, Shared contains product logic, adapter→adapter references not approved.

### 3. Review Project References and DI

Check: forbidden upward references, circular references, adapter→host, host→host, Domain→adapter, accidental test reference in production code. DI: adapter registers own implementations, host composes, Application registers only Application services, Domain has no registration.
```

- [ ] **Step 2: Keep concrete procedure, output format, and severity guidance**

The "Review Procedure" section (lines 312-326), "Output Format" (lines 328-495), "Severity Guidance" (lines 497-531), "Quality Checklist" (lines 533-549), and "Anti-Patterns" (lines 553-565) are procedural and should remain largely intact. Only trim vague wording from Anti-Patterns.

- [ ] **Step 3: Clean up Anti-Patterns**

Replace:

```md
## Anti-Patterns

Avoid:
- generic architecture advice not tied to the project;
- treating every abstraction gap as a blocker;
- approving boundary violations because they are convenient;
- demanding speculative abstractions without a clear seam;
- ignoring project references or DI registration;
- collapsing review and implementation;
- changing files during review;
- hiding uncertainty.
```

- [ ] **Step 4: Commit**

```bash
git add .opencode/skills/architecture-boundary-review/SKILL.md
git commit -m "refactor: tighten architecture-boundary-review skill - major trim, replace duplication with references"
```

---

### Task 6.8: Tighten agent-memory skill

**Files:**
- Modify: `.opencode/skills/agent-memory/SKILL.md`

- [ ] **Step 1: Replace Core Rules 1-10 with condensed version**

Replace `### 1. Record Facts, Not Guesses` through `### 10. Verification Must Be Honest` with:

```md
## Core Rules

Hard rules live in `.opencode/rules/memory.md`.

### 1. Facts Only

Record factual, traceable entries. Never: "probably works", "should be fine", "architecture is perfect". Store artifact paths, status, key decisions, unresolved risks. Do not paste full specs/designs/plans into memory.

### 2. Keep Memory Compact and Chronological

`PROJECT_LOG.md` is newest-first. `overview.md` stays current. `log_notes.md` tracks unresolved issues. `mem_library/` stores stable product meaning only. Route each fact to exactly one primary file.

### 3. Security and Privacy

Never store: API keys, tokens, credentials, private keys, production connection strings, personal data, real customer data, raw prompts containing private content. Use `<REDACTED>`, `<API_KEY>`, `<CONNECTION_STRING>` placeholders.

### 4. Minimal Updates, Honest Verification

Only update files relevant to the change. If verification was run, record exact commands and result. If not run, state "Verification not run." Do not imply success.
```

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns with:

```md
## Anti-Patterns

Avoid:
- updating all memory files every time;
- dumping full specs/designs/plans into memory;
- vague entries like "worked on stuff";
- recording unverified success or hiding unresolved issues;
- overwriting history or storing sensitive data;
- storing private reasoning;
- duplicating docs in memory;
- mixing transient notes into durable memory.
```

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/agent-memory/SKILL.md
git commit -m "refactor: tighten agent-memory skill - remove rules duplication"
```

---

### Task 6.9: Tighten iris-architecture skill (minor)

**Files:**
- Modify: `.opencode/skills/iris-architecture/SKILL.md`

This skill is already reasonable at 329 lines. Only minor cleanup needed.

- [ ] **Step 1: Check for vague wording**

```powershell
Select-String -Path .opencode\skills\iris-architecture\SKILL.md -Pattern 'be careful','should be fine','probably','generally','try to'
```

If any matches, those phrases should be removed or made concrete.

- [ ] **Step 2: Clean up Anti-Patterns**

Replace Anti-Patterns (lines ~313–320) with:

```md
## Anti-Patterns

Avoid:
- approving shortcuts because they are temporary;
- adding broad abstractions before a real seam exists;
- moving behavior to Shared because it's convenient;
- treating adapters as product-policy owners;
- calling a change architecture-safe without checking references.
```

Removes the duplicate "adding broad abstractions" / "performance" noise. (The original has 7 items; we keep 5 concrete ones.)

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/iris-architecture/SKILL.md
git commit -m "refactor: minor cleanup of iris-architecture skill anti-patterns"
```

---

### Task 6.10: Tighten remaining Iris skills (batch)

**Files:**
- Modify: `.opencode/skills/iris-memory/SKILL.md`
- Modify: `.opencode/skills/iris-review/SKILL.md`
- Modify: `.opencode/skills/iris-verification/SKILL.md`

These three skills are already reasonable. Only clean up any vague wording found.

- [ ] **Step 1: Search for vague phrases across all three files**

```powershell
$files = @(
    '.opencode\skills\iris-memory\SKILL.md',
    '.opencode\skills\iris-review\SKILL.md',
    '.opencode\skills\iris-verification\SKILL.md'
)
$patterns = @('be careful','should be fine','probably works','generally okay','try to avoid','make sure to','you might want to')
foreach ($f in $files) {
    foreach ($p in $patterns) {
        $hits = Select-String -Path $f -Pattern $p -SimpleMatch
        if ($hits) {
            Write-Output "$f : $($hits.LineNumber) : $p"
        }
    }
}
```

Expected: No matches, or if matches exist, they must be rewritten to concrete language.

- [ ] **Step 2: If any vague phrases found, rewrite them**

Replace vague phrases with concrete language:
- "be careful" → "check" or "verify"
- "should be fine" → remove entirely
- "probably works" → remove entirely
- "generally okay" → remove entirely
- "try to avoid" → "avoid"
- "make sure to" → "must" or remove
- "you might want to" → remove and state the requirement directly

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/iris-memory/SKILL.md .opencode/skills/iris-review/SKILL.md .opencode/skills/iris-verification/SKILL.md
git commit -m "refactor: remove vague wording from iris-memory, iris-review, iris-verification skills"
```

---

### Task 6.11: Tighten save-* skills (batch)

**Files:**
- Modify: `.opencode/skills/save-spec/SKILL.md`
- Modify: `.opencode/skills/save-design/SKILL.md`
- Modify: `.opencode/skills/save-plan/SKILL.md`
- Modify: `.opencode/skills/save-audit/SKILL.md`

These four skills are procedural and already clean. Only search for and remove vague wording.

- [ ] **Step 1: Search for vague phrases**

```powershell
$files = @(
    '.opencode\skills\save-spec\SKILL.md',
    '.opencode\skills\save-design\SKILL.md',
    '.opencode\skills\save-plan\SKILL.md',
    '.opencode\skills\save-audit\SKILL.md'
)
$patterns = @('be careful','should be fine','probably','generally','try to','make sure to','you might want to')
foreach ($f in $files) {
    foreach ($p in $patterns) {
        $hits = Select-String -Path $f -Pattern $p -SimpleMatch
        if ($hits) {
            Write-Output "$f : $($hits.LineNumber) : $p"
        }
    }
}
```

- [ ] **Step 2: Rewrite any vague phrases found**

Apply same rewrite rules as Task 6.10 Step 2.

- [ ] **Step 3: Verify that save skills still contain their procedural content**

```powershell
foreach ($f in $files) {
    $lines = (Get-Content $f).Count
    Write-Output "$f : $lines lines"
}
```

Expected: All files should have similar line counts to before (minor decreases for removed vague wording). If a file dropped below 200 lines, something was accidentally removed — inspect.

- [ ] **Step 4: Commit**

```bash
git add .opencode/skills/save-spec/SKILL.md .opencode/skills/save-design/SKILL.md .opencode/skills/save-plan/SKILL.md .opencode/skills/save-audit/SKILL.md
git commit -m "refactor: remove vague wording from save-* skills"
```

---

## Phase 6 Verification

### Task 6.12: Final verification for Phase 5+6

- [ ] **Step 1: Verify all skill files are valid (no duplicate sections)**

```powershell
$skillFiles = Get-ChildItem .opencode\skills -Recurse -Filter SKILL.md
foreach ($f in $skillFiles) {
    $headings = Select-String -Path $f.FullName -Pattern '^## ' | ForEach-Object { $_.Line.Trim() }
    $dupes = $headings | Group-Object | Where-Object { $_.Count -gt 1 }
    if ($dupes) {
        Write-Output "$($f.FullName): DUPLICATE HEADING: $($dupes.Name)"
    }
}
Write-Output "Skill heading check complete."
```

Expected: No duplicate headings.

- [ ] **Step 2: Verify no skill references deleted sections**

```powershell
Select-String -Path .opencode\skills\*\SKILL.md,.opencode\skills\*\*\SKILL.md -Pattern 'Core Rules' | ForEach-Object { "$($_.Filename):$($_.LineNumber)" }
```

Expected: Each skill that had "Core Rules" before should still have it (just with condensed content). Skills that had their Core Rules replaced entirely (like implement) should still reference the relevant rule files.

- [ ] **Step 3: Verify rules files unchanged outside Phase 5 scope**

```powershell
git diff --name-only -- .opencode/rules/
```

Expected: Only `.opencode/rules/workflow.md` should appear (edited in Task 5.1). No other rule files were modified.

- [ ] **Step 4: Verify all gate definitions are consistent**

```powershell
Select-String -Path .opencode\rules\workflow.md -Pattern 'Gate [A-G]'
Select-String -Path .opencode\skills\iris-engineering\SKILL.md -Pattern '### Gate [A-G]'
Select-String -Path .opencode\commands\implement.md -Pattern 'Gate [A-C]'
```

Expected: All A-G gates appear in workflow.md, all A-G definitions in iris-engineering, and A-C enforcement in implement.md.

- [ ] **Step 5: Inspect full diff**

```powershell
git diff --stat
```

- [ ] **Step 6: Run config validation**

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"
```

Expected: `opencode.jsonc ok`

- [ ] **Step 7: Report verification summary**

No .NET build/test is required — this is pure `.opencode` infrastructure work. Verification is manual file inspection.

---

## Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| Skill tightening removes something another skill depends on | Broken cross-references | Task 6.12 Step 2 verifies no broken references |
| Gate definitions in iris-engineering conflict with workflow.md rules | Gate confusion | Both files edited in Phase 5; Task 5.12 Step 3 verifies consistency |
| Some skills have hidden vague wording not caught by regex | Quality gap | `be careful`, `should be fine`, `probably`, `generally`, `try to`, `make sure to` patterns are comprehensive |
| Major skill trim (audit, architecture-boundary-review) accidentally removes procedure content | Skill becomes useless | Task 6.6 and 6.7 explicitly preserve procedure and output format sections |

---

## Implementation Handoff Notes

**Order matters:** Phase 5 must complete before Phase 6 starts. Phase 6 tightens skills that Phase 5 edited.

**Critical constraints:**
- `.opencode/rules/` files other than `workflow.md` must not be edited.
- Commands other than the 11 listed must not be edited.
- No new files should be created.
- All edits are to existing `.opencode/` infrastructure files only.
- No product code (`src/`, `tests/`), no `AGENTS.md`, no `opencode.jsonc` (already has skills.paths).

**Expected final state:**
- All 11 command outputs include gate status table.
- `workflow.md` has gate-labeled table.
- `iris-engineering/SKILL.md` has formal A-G gate definitions.
- `implement/SKILL.md` and `implement.md` enforce Gate C hard stop.
- All 17 skill files are tighter: shorter core rules, anti-patterns trimmed, no vague wording.
- Rules duplication is eliminated from skills (skills reference rules, don't repeat them).
- Command-specific behavior stays in commands, methodology in skills, prohibitions in rules.

**Verification commands:** Use PowerShell inspection commands from each task. No `dotnet build` required for docs-only work.

**Manual gaps:** None. All verification is automated file inspection.

---

## Open Questions

No blocking open questions.
