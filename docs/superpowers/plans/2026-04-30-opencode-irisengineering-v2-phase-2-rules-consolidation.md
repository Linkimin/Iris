# IrisEngineering v2 Phase 2 Rules Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate `.opencode/rules` so IrisEngineering v2 has one canonical rule layer, while legacy numbered rule files remain as short compatibility pointers only.

**Architecture:** Rules are hard constraints, not workflow tutorials. Skills explain how to act; commands choose workflows; rules prevent systemic mistakes. Phase 2 must remove duplicate rule content from the instruction path without rewriting commands, scripts, agents, or plugins.

**Tech Stack:** OpenCode `opencode.jsonc`, project `.opencode/rules/*.md`, `.opencode/docs/*.md`, PowerShell verification, Node JSON parse check, Iris `.agent` memory conventions.

---

## File Structure

Create:

- `.opencode/docs/irisengineering-v2-phase-2-rules-consolidation.md` — factual consolidation map and final rule loading decision.
- `.opencode/rules/README.md` — human-facing rules index and ownership map.
- `.opencode/rules/workflow.md` — canonical workflow/stage/gate hard rules.
- `.opencode/rules/security.md` — canonical secrets, destructive command, data safety, supply-chain rules.
- `.opencode/rules/review-audit.md` — canonical review/audit hard rules.

Modify:

- `.opencode/rules/iris-architecture.md` — keep as canonical architecture ownership/dependency rule.
- `.opencode/rules/no-shortcuts.md` — keep as canonical absolute shortcut prohibition rule.
- `.opencode/rules/memory.md` — keep as canonical memory rule.
- `.opencode/rules/verification.md` — keep as canonical verification rule.
- `.opencode/rules/dotnet.md` — keep as canonical .NET rule.
- `.opencode/rules/00-core-workflow.md` — reduce to compatibility pointer.
- `.opencode/rules/10-architecture-boundaries.md` — reduce to compatibility pointer.
- `.opencode/rules/20-dotnet-style.md` — reduce to compatibility pointer.
- `.opencode/rules/30-testing-verification.md` — reduce to compatibility pointer.
- `.opencode/rules/40-agent-memory.md` — reduce to compatibility pointer.
- `.opencode/rules/50-security-safety.md` — reduce to compatibility pointer.
- `.opencode/rules/60-audit.md` — reduce to compatibility pointer.
- `opencode.jsonc` — load only canonical rules, not legacy numbered compatibility files.

Do not modify:

- `.opencode/commands/*.md`
- `.opencode/scripts/*.ps1`
- `.opencode/skills/**/*.md`
- `.opencode/agents/*.md`
- `.opencode/plugins/*.ts`
- `AGENTS.md`

## Final Canonical Rule Set

After Phase 2, canonical loaded rules are:

1. `.opencode/rules/workflow.md`
2. `.opencode/rules/iris-architecture.md`
3. `.opencode/rules/no-shortcuts.md`
4. `.opencode/rules/memory.md`
5. `.opencode/rules/verification.md`
6. `.opencode/rules/dotnet.md`
7. `.opencode/rules/security.md`
8. `.opencode/rules/review-audit.md`

Legacy numbered files remain in `.opencode/rules` because current commands and `AGENTS.md` may still reference them. They must not contain independent rules after this phase.

---

### Task 1: Create Rules Consolidation Baseline

**Files:**

- Create: `.opencode/docs/irisengineering-v2-phase-2-rules-consolidation.md`

- [ ] **Step 1: Inspect current rule inventory**

Run:

```powershell
Get-ChildItem .\.opencode\rules -File |
  ForEach-Object {
    [PSCustomObject]@{
      Name = $_.Name
      Lines = (Get-Content $_.FullName).Count
      Length = $_.Length
    }
  } |
  Sort-Object Name
```

Expected:

- canonical v2 rules are present;
- numbered legacy files are present;
- total loaded instruction set is still duplicated before implementation.

- [ ] **Step 2: Inspect current `opencode.jsonc` instruction loading**

Run:

```powershell
Get-Content -Raw .\opencode.jsonc
```

Expected:

- `instructions` contains both canonical v2 rules and numbered legacy rules.

- [ ] **Step 3: Create consolidation baseline document**

Create `.opencode/docs/irisengineering-v2-phase-2-rules-consolidation.md`:

```markdown
# IrisEngineering v2 Phase 2 Rules Consolidation

## Goal

Make canonical v2 rules the only loaded OpenCode instruction layer.

## Current Problem

`opencode.jsonc` loads both canonical rules and legacy numbered rules. Even when legacy files are short, this still creates two apparent rule layers and invites future drift.

## Final Loading Decision

`opencode.jsonc` should load only:

1. `.opencode/rules/workflow.md`
2. `.opencode/rules/iris-architecture.md`
3. `.opencode/rules/no-shortcuts.md`
4. `.opencode/rules/memory.md`
5. `.opencode/rules/verification.md`
6. `.opencode/rules/dotnet.md`
7. `.opencode/rules/security.md`
8. `.opencode/rules/review-audit.md`

## Compatibility Decision

Keep numbered files as compatibility pointers because existing command templates and `AGENTS.md` may still reference them before Phase 3 command rewrite.

## Rule Ownership

| Rule | Owns | Must not own |
|---|---|---|
| `workflow.md` | stage boundaries, gates, command modes | architecture details, .NET details |
| `iris-architecture.md` | layer ownership, dependency direction | command workflow |
| `no-shortcuts.md` | absolute forbidden shortcuts | explanatory playbooks |
| `memory.md` | `.agent` memory file roles and write policy | product memory content |
| `verification.md` | verification evidence policy | implementation fixes |
| `dotnet.md` | .NET project/package/test conventions | general workflow |
| `security.md` | secrets, destructive commands, supply-chain safety | architecture ownership |
| `review-audit.md` | review/audit severity and readiness rules | implementation steps |

## Legacy Mapping

| Legacy file | Canonical target |
|---|---|
| `00-core-workflow.md` | `workflow.md` |
| `10-architecture-boundaries.md` | `iris-architecture.md`, `no-shortcuts.md` |
| `20-dotnet-style.md` | `dotnet.md` |
| `30-testing-verification.md` | `verification.md` |
| `40-agent-memory.md` | `memory.md` |
| `50-security-safety.md` | `security.md` |
| `60-audit.md` | `review-audit.md` |

## Out Of Scope

- Command rewrite.
- Shared PowerShell scripts.
- Skill deepening.
- Plugin changes.
- AGENTS.md rewrite.
```

- [ ] **Step 4: Verify baseline document**

Run:

```powershell
Test-Path .\.opencode\docs\irisengineering-v2-phase-2-rules-consolidation.md
Get-Content .\.opencode\docs\irisengineering-v2-phase-2-rules-consolidation.md -TotalCount 60
```

Expected:

- `Test-Path` returns `True`;
- document names canonical loaded rule set and legacy mapping.

### Task 2: Create Rules Index

**Files:**

- Create: `.opencode/rules/README.md`

- [ ] **Step 1: Create `.opencode/rules/README.md`**

Use this content:

```markdown
# Iris OpenCode Rules

This directory contains hard constraints for OpenCode agents working on Iris.

Skills explain how to work.
Commands choose a workflow.
Rules define what must not be violated.

## Canonical Rules

These files are loaded by `opencode.jsonc`:

1. `workflow.md`
2. `iris-architecture.md`
3. `no-shortcuts.md`
4. `memory.md`
5. `verification.md`
6. `dotnet.md`
7. `security.md`
8. `review-audit.md`

## Compatibility Rules

Numbered files are compatibility pointers for older command templates and `AGENTS.md` references.

They must not contain independent rules.

## Ownership

If a rule grows into a tutorial, move method guidance into an Iris skill.

If a rule duplicates another rule, keep the stricter canonical rule and replace the duplicate with a pointer.

If a rule belongs to a command-specific workflow, keep it in the command.
```

- [ ] **Step 2: Verify index**

Run:

```powershell
Get-Content .\.opencode\rules\README.md -TotalCount 80
```

Expected:

- index clearly separates canonical rules from compatibility rules.

### Task 3: Add Missing Canonical Rules

**Files:**

- Create: `.opencode/rules/workflow.md`
- Create: `.opencode/rules/security.md`
- Create: `.opencode/rules/review-audit.md`

- [ ] **Step 1: Create `workflow.md`**

Use this content:

```markdown
# Iris Workflow Rules

## Stage Separation

For non-trivial work, keep stages separate:

```text
Spec -> Design -> Plan -> Implement -> Verify -> Review -> Audit -> Memory update
```

Do not implement during spec/design/plan.
Do not fix findings during review/audit unless the user explicitly asks.
Do not update memory outside `/update-memory` or an explicitly allowed workflow.

## Required Gates

- Spec is required for new behavior unless the task is trivial/local.
- Design is required for architecture-affecting work.
- Plan is required before multi-file implementation.
- Verification evidence is required before readiness claims.
- Architecture review is required for boundary-sensitive changes.
- Audit is required before merge/readiness decisions.
- Memory update is required after meaningful completed work.

## Dirty Tree Rule

Before editing, inspect git state.

Do not overwrite, revert, stage, or normalize unrelated user changes.

## File Creation Rule

Before creating a file, check existing files, placeholders, ownership, and phase scope.

No speculative files.
No duplicate responsibilities.
```

- [ ] **Step 2: Create `security.md`**

Use this content:

```markdown
# Iris Security Rules

## Secrets

Never read, print, store, or modify:

- `.env`
- `.env.*`
- production configs;
- private keys;
- API tokens;
- credentials;
- user secrets;
- real customer data.

Allowed:

- `.env.example`
- redacted sample configs
- documentation examples

## Destructive Commands

Never run without explicit approval:

- `git push`
- `git clean`
- `git reset --hard`
- destructive file removal
- `docker system prune`
- destructive database commands

## Data Handling

Do not copy secrets into docs, tests, logs, prompts, memory, or review output.

Use placeholders:

```text
<REDACTED>
<API_KEY>
<CONNECTION_STRING>
```

## Supply Chain

Do not add packages casually.

Before adding a dependency, verify existing alternatives, ownership project, central package management, and approved plan scope.
```

- [ ] **Step 3: Create `review-audit.md`**

Use this content:

```markdown
# Iris Review and Audit Rules

## Read-Only By Default

`/review`, `/architecture-review`, and `/audit` are read-only unless the user explicitly asks for fixes.

Do not edit code, tests, docs, memory, snapshots, or formatting during review/audit.

## Severity

- P0: must fix before merge.
- P1: should fix before merge.
- P2: acceptable backlog.
- Note: observation only.

Severity must be evidence-based.

## Required Evidence

Findings must cite:

- file/symbol/command evidence;
- impact;
- recommended minimal fix.

No generic findings.

## Audit Passes

Formal audit checks:

1. Spec compliance.
2. Test quality.
3. SOLID / architecture quality.
4. Clean code / maintainability.

Audit must include verification evidence and final readiness decision.

## Readiness

Do not approve readiness with unresolved P0/P1 issues or missing required verification.
```

- [ ] **Step 4: Verify missing canonical rules**

Run:

```powershell
$paths = @(
  '.opencode/rules/workflow.md',
  '.opencode/rules/security.md',
  '.opencode/rules/review-audit.md'
)
$paths | ForEach-Object { [PSCustomObject]@{ Path = $_; Exists = Test-Path $_; Lines = if (Test-Path $_) { (Get-Content $_).Count } else { 0 } } }
```

Expected:

- all `Exists` values are `True`;
- each rule is concise and constraint-focused.

### Task 4: Normalize Existing Canonical Rules

**Files:**

- Modify: `.opencode/rules/iris-architecture.md`
- Modify: `.opencode/rules/no-shortcuts.md`
- Modify: `.opencode/rules/memory.md`
- Modify: `.opencode/rules/verification.md`
- Modify: `.opencode/rules/dotnet.md`

- [ ] **Step 1: Check each canonical rule for ownership drift**

Run:

```powershell
Get-Content .\.opencode\rules\iris-architecture.md -Raw
Get-Content .\.opencode\rules\no-shortcuts.md -Raw
Get-Content .\.opencode\rules\memory.md -Raw
Get-Content .\.opencode\rules\verification.md -Raw
Get-Content .\.opencode\rules\dotnet.md -Raw
```

Expected:

- `iris-architecture.md` owns dependency direction and layer ownership.
- `no-shortcuts.md` owns absolute forbidden shortcuts.
- `memory.md` owns `.agent` memory policy.
- `verification.md` owns verification evidence policy.
- `dotnet.md` owns .NET-specific project/build/test/package rules.

- [ ] **Step 2: Remove tutorial-style content if present**

Keep canonical rules concise.

If a section explains how to perform a review, implementation, or investigation, move that guidance out of rules and leave a pointer to the relevant skill.

Allowed rule wording:

```markdown
Must not:
- ...

Required:
- ...
```

Avoid rule wording:

```markdown
To do this well, first think about...
Here is a long example...
```

- [ ] **Step 3: Add cross-reference headers where useful**

For each canonical rule, add a short "Related skills" section only if missing:

```markdown
## Related Skills

- `.opencode/skills/iris-engineering/SKILL.md`
- `.opencode/skills/<specific>/SKILL.md`
```

Do not add long explanations.

- [ ] **Step 4: Verify canonical rule sizes**

Run:

```powershell
Get-ChildItem .\.opencode\rules -File |
  Where-Object { $_.Name -in @('workflow.md','iris-architecture.md','no-shortcuts.md','memory.md','verification.md','dotnet.md','security.md','review-audit.md') } |
  ForEach-Object {
    [PSCustomObject]@{
      Name = $_.Name
      Lines = (Get-Content $_.FullName).Count
    }
  } |
  Sort-Object Name
```

Expected:

- canonical rules are concise enough to be loaded together;
- no canonical rule becomes a full skill.

### Task 5: Convert Legacy Numbered Rules To Pointers

**Files:**

- Modify: `.opencode/rules/00-core-workflow.md`
- Modify: `.opencode/rules/10-architecture-boundaries.md`
- Modify: `.opencode/rules/20-dotnet-style.md`
- Modify: `.opencode/rules/30-testing-verification.md`
- Modify: `.opencode/rules/40-agent-memory.md`
- Modify: `.opencode/rules/50-security-safety.md`
- Modify: `.opencode/rules/60-audit.md`

- [ ] **Step 1: Replace `00-core-workflow.md`**

Use:

```markdown
# Compatibility: Core Workflow

Canonical rule:

- `.opencode/rules/workflow.md`

Related skill:

- `.opencode/skills/iris-engineering/SKILL.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 2: Replace `10-architecture-boundaries.md`**

Use:

```markdown
# Compatibility: Architecture Boundaries

Canonical rules:

- `.opencode/rules/iris-architecture.md`
- `.opencode/rules/no-shortcuts.md`

Related skill:

- `.opencode/skills/iris-architecture/SKILL.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 3: Replace `20-dotnet-style.md`**

Use:

```markdown
# Compatibility: .NET Style

Canonical rule:

- `.opencode/rules/dotnet.md`

Related skill:

- `.opencode/skills/iris-verification/SKILL.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 4: Replace `30-testing-verification.md`**

Use:

```markdown
# Compatibility: Testing and Verification

Canonical rule:

- `.opencode/rules/verification.md`

Related skill:

- `.opencode/skills/iris-verification/SKILL.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 5: Replace `40-agent-memory.md`**

Use:

```markdown
# Compatibility: Agent Memory

Canonical rule:

- `.opencode/rules/memory.md`

Related skill:

- `.opencode/skills/iris-memory/SKILL.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 6: Replace `50-security-safety.md`**

Use:

```markdown
# Compatibility: Security and Safety

Canonical rule:

- `.opencode/rules/security.md`

Related rules:

- `.opencode/rules/no-shortcuts.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 7: Replace `60-audit.md`**

Use:

```markdown
# Compatibility: Audit

Canonical rule:

- `.opencode/rules/review-audit.md`

Related skill:

- `.opencode/skills/iris-review/SKILL.md`

This file exists only for older command and AGENTS references.
Do not add independent rules here.
```

- [ ] **Step 8: Verify legacy files are short**

Run:

```powershell
Get-ChildItem .\.opencode\rules -File |
  Where-Object { $_.Name -match '^\d\d-' } |
  ForEach-Object {
    [PSCustomObject]@{
      Name = $_.Name
      Lines = (Get-Content $_.FullName).Count
    }
  } |
  Sort-Object Name
```

Expected:

- every numbered compatibility file is under 20 lines.

### Task 6: Update `opencode.jsonc` Instructions

**Files:**

- Modify: `opencode.jsonc`

- [ ] **Step 1: Replace `instructions` with canonical rule list**

Use exactly:

```json
"instructions": [
  ".opencode/rules/workflow.md",
  ".opencode/rules/iris-architecture.md",
  ".opencode/rules/no-shortcuts.md",
  ".opencode/rules/memory.md",
  ".opencode/rules/verification.md",
  ".opencode/rules/dotnet.md",
  ".opencode/rules/security.md",
  ".opencode/rules/review-audit.md"
]
```

Do not include numbered compatibility files in `opencode.jsonc`.

- [ ] **Step 2: Validate JSON**

Run:

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"
```

Expected:

```text
opencode.jsonc ok
```

- [ ] **Step 3: Verify every instruction path exists**

Run:

```powershell
$json = Get-Content -Raw .\opencode.jsonc | ConvertFrom-Json
$json.instructions | ForEach-Object {
  [PSCustomObject]@{
    Instruction = $_
    Exists = Test-Path $_
  }
}
```

Expected:

- every `Exists` value is `True`.

### Task 7: Rules Consolidation Verification

**Files:**

- Inspect: `.opencode/rules/*.md`
- Inspect: `opencode.jsonc`

- [ ] **Step 1: Verify no legacy numbered files are loaded**

Run:

```powershell
$json = Get-Content -Raw .\opencode.jsonc | ConvertFrom-Json
$json.instructions | Where-Object { $_ -match '/\d\d-' -or $_ -match '\\\d\d-' }
```

Expected:

- no output.

- [ ] **Step 2: Verify no forbidden memory naming drift**

Run:

```powershell
Get-ChildItem .\.opencode\rules -Recurse -File -Include *.md |
  Select-String -Pattern '\.agents/local_notes','\.agents\\local_notes'
```

Expected:

- no output.

- [ ] **Step 3: Verify compatibility files contain no independent rule language**

Run:

```powershell
Get-ChildItem .\.opencode\rules -File |
  Where-Object { $_.Name -match '^\d\d-' } |
  Select-String -Pattern 'Must not','Required:','Forbidden:','Never ','Always '
```

Expected:

- no output, except if those words appear in "Do not add independent rules here."

If the command returns only "Do not add independent rules here.", this is acceptable.

- [ ] **Step 4: Verify out-of-scope files were not modified**

Run:

```powershell
git diff --name-only -- .opencode/commands .opencode/scripts .opencode/skills .opencode/agents .opencode/plugins AGENTS.md
```

Expected:

- no output for Phase 2 changes.
- Existing unrelated `AGENTS.md` user changes may still appear in `git status`, but Phase 2 must not add a diff to it.

- [ ] **Step 5: Inspect final rules diff**

Run:

```powershell
git diff --stat -- .opencode/rules opencode.jsonc .opencode/docs
git diff -- .opencode/rules opencode.jsonc .opencode/docs
```

Expected:

- diff only contains rules consolidation, `opencode.jsonc` instruction changes, and the Phase 2 baseline doc.

## Acceptance Criteria

- Phase 2 consolidation baseline exists.
- `.opencode/rules/README.md` exists and explains canonical vs compatibility rules.
- Missing canonical rules exist: `workflow.md`, `security.md`, `review-audit.md`.
- `opencode.jsonc` loads only canonical rules.
- Numbered legacy rule files are compatibility pointers only.
- No duplicated independent rules remain in numbered files.
- No `.agents/local_notes` drift exists in `.opencode/rules`.
- `.opencode/commands`, `.opencode/scripts`, `.opencode/skills`, `.opencode/agents`, `.opencode/plugins`, and `AGENTS.md` are not modified by this phase.

## Validation Commands

Run at the end:

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"

$json = Get-Content -Raw .\opencode.jsonc | ConvertFrom-Json
$json.instructions | ForEach-Object { [PSCustomObject]@{ Instruction = $_; Exists = Test-Path $_ } }

$json.instructions | Where-Object { $_ -match '/\d\d-' -or $_ -match '\\\d\d-' }

Get-ChildItem .\.opencode\rules -File |
  Where-Object { $_.Name -match '^\d\d-' } |
  ForEach-Object { [PSCustomObject]@{ Name = $_.Name; Lines = (Get-Content $_.FullName).Count } }

Get-ChildItem .\.opencode\rules -Recurse -File -Include *.md |
  Select-String -Pattern '\.agents/local_notes','\.agents\\local_notes'

git diff --name-only -- .opencode/commands .opencode/scripts .opencode/skills .opencode/agents .opencode/plugins AGENTS.md
```

For branch-completion confidence, run:

```powershell
dotnet build .\Iris.slnx --no-restore
dotnet test .\Iris.slnx --no-restore
```

If only rules/docs changed and .NET checks are skipped, report that explicitly.

## Commit Guidance

Recommended commit split:

```powershell
git add .opencode/docs/irisengineering-v2-phase-2-rules-consolidation.md .opencode/rules/README.md
git commit -m "docs: map OpenCode rules consolidation"

git add .opencode/rules opencode.jsonc
git commit -m "docs: consolidate Iris OpenCode rules"
```

Do not commit automatically unless the user asks.

## Self-Review Checklist

- [ ] Canonical rules are hard constraints, not tutorials.
- [ ] Skills remain the place for methodology and playbooks.
- [ ] Commands remain untouched.
- [ ] Legacy numbered rules are compatibility pointers only.
- [ ] `opencode.jsonc` has no numbered compatibility rule paths.
- [ ] No `.agents/local_notes` drift remains.
- [ ] Validation commands are exact.
- [ ] Phase 3 command rewrite remains clearly deferred.
