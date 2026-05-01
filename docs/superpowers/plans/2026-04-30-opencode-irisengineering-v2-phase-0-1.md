# IrisEngineering v2 Phase 0-1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the IrisEngineering v2 foundation for OpenCode by auditing the current `.opencode` system and adding canonical Iris skills/rules without rewriting all commands yet.

**Architecture:** Phase 0 is a factual baseline and migration map. Phase 1 adds stable methodology files: Iris skills for workflow behavior and Iris rules for hard constraints. Commands remain mostly unchanged until a later phase, but Phase 1 must remove obvious memory naming conflicts and make the new v2 layer canonical.

**Tech Stack:** OpenCode command/skill/rule Markdown files, `opencode.jsonc`, PowerShell inspection commands, Iris `.agent` memory conventions, Clean Architecture rules for the .NET Iris repository.

---

## File Structure

Create:

- `.opencode/docs/irisengineering-v2-phase-0-baseline.md` — factual inventory of current commands, skills, rules, plugins, repeated context blocks, and migration risks.
- `.opencode/skills/iris-engineering/SKILL.md` — central Iris engineering workflow skill.
- `.opencode/skills/iris-architecture/SKILL.md` — Iris Clean Architecture boundary skill.
- `.opencode/skills/iris-memory/SKILL.md` — Iris `.agent` memory usage skill.
- `.opencode/skills/iris-verification/SKILL.md` — Iris verification and evidence skill.
- `.opencode/skills/iris-review/SKILL.md` — Iris review/audit readiness skill.
- `.opencode/rules/iris-architecture.md` — canonical dependency and ownership rules.
- `.opencode/rules/memory.md` — canonical `.agent` memory rules.
- `.opencode/rules/verification.md` — canonical verification rules.
- `.opencode/rules/no-shortcuts.md` — forbidden shortcut rules.
- `.opencode/rules/dotnet.md` — .NET/Iris build, test, project, and package rules.

Modify:

- `opencode.jsonc` — register the canonical v2 rule files.
- `.opencode/rules/00-core-workflow.md` — shrink to a compatibility pointer or align with v2 workflow.
- `.opencode/rules/10-architecture-boundaries.md` — shrink to a compatibility pointer or align with `iris-architecture.md`.
- `.opencode/rules/20-dotnet-style.md` — shrink to a compatibility pointer or align with `dotnet.md`.
- `.opencode/rules/30-testing-verification.md` — shrink to a compatibility pointer or align with `verification.md`.
- `.opencode/rules/40-agent-memory.md` — fix `.agent/log_notes.md` as canonical and align with `memory.md`.
- `.opencode/rules/50-security-safety.md` — keep security rules, but avoid duplicating `no-shortcuts.md`.
- `.opencode/rules/60-audit.md` — keep audit rules, but align with `iris-review.md`.

Do not modify yet:

- `.opencode/commands/*.md` — command thinning belongs to Phase 3.
- `.opencode/scripts/*.ps1` — shared scripts belong to Phase 2.
- `.opencode/agents/*.md` — agent role tuning is not part of Phase 0-1.
- `.opencode/plugins/*.ts` — plugin/tool-backed safeguards are not part of Phase 0-1.

## Quality Bar For New Skills

Each new Iris skill must be detailed enough to guide an agent through real Iris work, but not overloaded.

Required shape:

- frontmatter with `name`, `description`, `compatibility: opencode`, and useful metadata;
- purpose and when-to-use;
- required context;
- workflow steps;
- stop conditions;
- output expectations;
- quality checklist;
- anti-patterns specific to Iris.

Hard limits:

- do not copy AGENTS.md wholesale;
- do not repeat long PowerShell context blocks;
- do not duplicate entire rules files;
- do not add generic advice that does not change agent behavior;
- prefer concrete Iris examples over broad software slogans;
- keep each skill readable in one pass.

Target size:

- `iris-engineering`: about 160-240 lines.
- `iris-architecture`: about 140-220 lines.
- `iris-memory`: about 120-190 lines.
- `iris-verification`: about 120-190 lines.
- `iris-review`: about 120-190 lines.

If a skill grows beyond the target, move hard prohibitions into rules or command-specific details into commands.

---

### Task 1: Phase 0 Baseline Inventory

**Files:**

- Create: `.opencode/docs/irisengineering-v2-phase-0-baseline.md`

- [ ] **Step 1: Inspect current OpenCode structure**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File | Select-Object Name,Length | Sort-Object Name
Get-ChildItem .\.opencode\skills -Directory | ForEach-Object {
  $skill = Join-Path $_.FullName 'SKILL.md'
  if (Test-Path $skill) {
    [PSCustomObject]@{ Name = $_.Name; Length = (Get-Item $skill).Length }
  }
} | Sort-Object Name
Get-ChildItem .\.opencode\rules -File | Select-Object Name,Length | Sort-Object Name
Get-ChildItem .\.opencode\agents -File | Select-Object Name,Length | Sort-Object Name
Get-ChildItem .\.opencode\plugins -File | Select-Object Name,Length | Sort-Object Name
```

Expected:

- 14 command files are present.
- Existing action skills are present.
- Existing numbered rules are present.
- Current agents/plugins are inventoried, but not edited.

- [ ] **Step 2: Inspect duplicated context and memory naming drift**

Run:

```powershell
Get-ChildItem .\.opencode\commands,.\.opencode\skills,.\.opencode\rules -Recurse -File -Include *.md |
  Select-String -Pattern 'E:\\Work\\Iris','local_notes','log_notes','powershell','\.agents','\.agent','git status','dotnet build','dotnet test' |
  Select-Object Path,LineNumber,Line
```

Expected:

- repeated inline PowerShell blocks are visible in commands;
- `.agents/local_notes.md` references are visible in older skills/rules;
- `.agent/log_notes.md` is confirmed as the target convention for Iris.

- [ ] **Step 3: Create the baseline document**

Create `.opencode/docs/irisengineering-v2-phase-0-baseline.md` with this structure:

```markdown
# IrisEngineering v2 Phase 0 Baseline

## Current Inventory

### Commands

| Command | Current role | Main risk |
|---|---|---|
| `/status` | Summarizes git and memory state | Hardcoded repo/context snippets |
| `/spec` | Produces specification | Duplicated context and legacy memory naming |
| `/design` | Produces design | Duplicated context and legacy memory naming |
| `/plan` | Produces implementation plan | Duplicated context and legacy memory naming |
| `/implement` | Implements approved plan | Duplicated context and must enforce gates later |
| `/verify` | Runs verification | Duplicated context and command policy should move to rules/scripts |
| `/review` | Read-only engineering review | Good output shape, duplicated context |
| `/architecture-review` | Boundary review | Good focus, duplicated context |
| `/audit` | Formal readiness audit | Duplicated context and should align with gates |
| `/update-memory` | Memory update workflow | Must use `.agent/log_notes.md` as canonical |
| `/save-spec` | Saves spec artifact | Must not update memory unless explicitly allowed |
| `/save-design` | Saves design artifact | Must not update memory unless explicitly allowed |
| `/save-plan` | Saves plan artifact | Must not update memory unless explicitly allowed |
| `/save-audit` | Saves audit artifact | Must not update memory unless explicitly allowed |

### Existing Skills

Existing action skills are useful and should not be deleted during Phase 0-1. The v2 Iris skills should become stable methodology above them.

### Existing Rules

Existing numbered rules are useful but contain naming drift and overlap. Phase 1 should add canonical named rules and make numbered rules compatible.

### Existing Plugins

Plugins already provide useful guardrails. Phase 0-1 must not modify plugin behavior.

## Confirmed Problems

- Commands duplicate large PowerShell context blocks.
- Some skills/rules prefer `.agents/local_notes.md`.
- Iris actual memory convention is `.agent/log_notes.md`.
- Current skills are action-specific, but there is no central Iris methodology skill.
- Current rules are useful, but not yet organized as v2 canonical guardrails.

## Phase 1 Migration Decision

- Add canonical Iris skills.
- Add canonical Iris rules.
- Align old numbered rules with canonical rules.
- Register v2 rules in `opencode.jsonc`.
- Do not rewrite commands yet.
- Do not create scripts yet.

## Out Of Scope

- Command thinning.
- Shared PowerShell scripts.
- Plugin changes.
- Repo-level architecture tests.
- CI changes.
```

- [ ] **Step 4: Verify baseline document**

Run:

```powershell
Test-Path .\.opencode\docs\irisengineering-v2-phase-0-baseline.md
Get-Content .\.opencode\docs\irisengineering-v2-phase-0-baseline.md -TotalCount 40
```

Expected:

- `Test-Path` returns `True`.
- The document has the sections shown above.

### Task 2: Create `iris-engineering` Central Skill

**Files:**

- Create: `.opencode/skills/iris-engineering/SKILL.md`

- [ ] **Step 1: Create skill directory and file**

Create `.opencode/skills/iris-engineering/SKILL.md`.

- [ ] **Step 2: Add frontmatter and purpose**

Use this frontmatter:

```markdown
---
name: iris-engineering
description: Central Iris engineering workflow skill for OpenCode. Use for non-trivial Iris work to keep spec, design, plan, implementation, verification, review, audit, and memory updates separated.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: cross_cutting
  output_type: engineering_guidance
---
```

- [ ] **Step 3: Add the skill body**

The body must define:

- Iris identity in 5-8 bullets;
- workflow stages: `status`, `spec`, `design`, `plan`, `implement`, `verify`, `review`, `architecture-review`, `audit`, `update-memory`, `save-*`;
- which stages are read-only;
- which stages may edit code;
- which stages may update memory;
- dirty working tree policy;
- file creation policy;
- readiness gates A-G;
- stop conditions.

Minimum required stop conditions:

```markdown
## Stop Conditions

Stop and report instead of continuing when:

- repository root cannot be resolved;
- `.agent` and `.agents` are both missing and memory is required;
- the working tree is dirty and the task would edit unrelated files;
- implementation is requested but no approved plan exists;
- a requested shortcut violates Iris architecture;
- a command would update memory outside `/update-memory` or an explicitly allowed save workflow;
- a new file would duplicate an existing placeholder or responsibility;
- verification is required but the command cannot be identified.
```

- [ ] **Step 4: Add skill quality checklist**

Add this checklist:

```markdown
## Quality Checklist

- [ ] The current workflow stage is explicit.
- [ ] The repository root is known.
- [ ] Relevant `.agent` or `.agents` context was inspected when needed.
- [ ] Dirty git state was considered before edits.
- [ ] Spec/design/plan boundaries were not mixed.
- [ ] Iris architecture boundaries were preserved.
- [ ] Existing files/placeholders were checked before creating files.
- [ ] Memory was not updated unless the workflow allows it.
- [ ] Verification expectations are explicit.
- [ ] Remaining uncertainty is reported instead of invented.
```

- [ ] **Step 5: Review skill size**

Run:

```powershell
(Get-Content .\.opencode\skills\iris-engineering\SKILL.md).Count
```

Expected:

- output is roughly between `160` and `240`;
- if much larger, remove duplicated rule content and keep only workflow guidance.

### Task 3: Create Focused Iris Skills

**Files:**

- Create: `.opencode/skills/iris-architecture/SKILL.md`
- Create: `.opencode/skills/iris-memory/SKILL.md`
- Create: `.opencode/skills/iris-verification/SKILL.md`
- Create: `.opencode/skills/iris-review/SKILL.md`

- [ ] **Step 1: Create `iris-architecture`**

Create `.opencode/skills/iris-architecture/SKILL.md` with:

```markdown
---
name: iris-architecture
description: Iris Clean Architecture boundary skill. Use when work may affect layers, project references, dependency direction, DI wiring, adapters, hosts, or shared abstractions.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: architecture
  output_type: architecture_guidance
---
```

Required body sections:

- Purpose
- When to Use
- Required Context
- Dependency Direction
- Layer Ownership Checks
- Forbidden Shortcut Checks
- Project Reference Checks
- DI Composition Checks
- Architecture Review Output Expectations
- Quality Checklist

Must reference `.opencode/rules/iris-architecture.md` and `.opencode/rules/no-shortcuts.md` instead of copying every rule.

- [ ] **Step 2: Create `iris-memory`**

Create `.opencode/skills/iris-memory/SKILL.md` with:

```markdown
---
name: iris-memory
description: Iris agent memory skill. Use only when project memory must be read or explicitly updated after meaningful work.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: memory
  output_type: memory_guidance
---
```

Required decisions:

- `.agent` is canonical for Iris;
- `.agents` is fallback only;
- `log_notes.md` is canonical for Iris;
- `local_notes.md` is fallback only if already present;
- `mem_library/**` is stable product memory, not a task log;
- no memory updates during read-only review/verify/audit unless user explicitly requests update.

- [ ] **Step 3: Create `iris-verification`**

Create `.opencode/skills/iris-verification/SKILL.md` with:

```markdown
---
name: iris-verification
description: Iris verification skill. Use to select and report build, test, format, architecture, and manual verification evidence without silently fixing files.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: verification
  output_type: verification_guidance
---
```

Required behavior:

- exact commands must be reported;
- prefer `dotnet build .\Iris.slnx`, `dotnet test .\Iris.slnx`, `dotnet format .\Iris.slnx --verify-no-changes`;
- use narrower commands first for local changes;
- never claim checks passed if not run;
- classify failures as build/test/architecture/format/environment/pre-existing only with evidence.

- [ ] **Step 4: Create `iris-review`**

Create `.opencode/skills/iris-review/SKILL.md` with:

```markdown
---
name: iris-review
description: Iris review and audit readiness skill. Use for focused reviews, formal audits, architecture readiness, and merge readiness decisions.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: review
  output_type: review_guidance
---
```

Required behavior:

- distinguish review, architecture-review, and audit;
- use P0/P1/P2/Note severity;
- findings must cite evidence;
- review must be read-only;
- audit must include readiness decision;
- memory/doc impact must be checked but not silently updated.

- [ ] **Step 5: Check focused skill sizes**

Run:

```powershell
Get-ChildItem .\.opencode\skills\iris-* -Directory | ForEach-Object {
  $path = Join-Path $_.FullName 'SKILL.md'
  [PSCustomObject]@{
    Skill = $_.Name
    Lines = (Get-Content $path).Count
  }
}
```

Expected:

- `iris-engineering` is detailed but still readable.
- The four focused skills are shorter than `iris-engineering`.
- No focused skill copies AGENTS.md or long rule files wholesale.

### Task 4: Create Canonical v2 Rules

**Files:**

- Create: `.opencode/rules/iris-architecture.md`
- Create: `.opencode/rules/memory.md`
- Create: `.opencode/rules/verification.md`
- Create: `.opencode/rules/no-shortcuts.md`
- Create: `.opencode/rules/dotnet.md`

- [ ] **Step 1: Create `iris-architecture.md`**

Include:

- dependency direction;
- Domain/Application/Adapter/Host/Shared ownership;
- forbidden project references;
- host composition rule;
- adapters implement Application abstractions.

Keep this as a hard rules file, not a long tutorial.

- [ ] **Step 2: Create `memory.md`**

Include:

```markdown
# Iris Memory Rules

## Canonical Paths

- Prefer `.agent`.
- Use `.agents` only if `.agent` does not exist.
- For Iris notes, prefer `log_notes.md`.
- Read `local_notes.md` only if it already exists.
- Do not create `local_notes.md` while `.agent/log_notes.md` exists.

## Write Policy

- `/update-memory` may update memory.
- Explicitly approved save workflows may update memory only when their skill says so.
- `/review`, `/architecture-review`, `/verify`, `/spec`, `/design`, `/plan`, and `/audit` are read-only for memory unless the user explicitly asks for memory update.
```

- [ ] **Step 3: Create `verification.md`**

Include:

- exact command reporting;
- passed/failed/skipped distinction;
- no silent formatting fixes;
- full solution commands for Iris;
- narrower command policy;
- failure summary requirements.

- [ ] **Step 4: Create `no-shortcuts.md`**

Include absolute Iris shortcuts:

- Desktop must not call Ollama directly.
- Desktop must not call DbContext directly.
- Application must not reference Persistence.
- Application must not reference ModelGateway.
- Domain must not reference EF/HTTP/UI.
- Tools must not own permission decisions.
- Voice must not own chat orchestration.
- Perception must not extract memory directly.
- Shared must not become product logic storage.

- [ ] **Step 5: Create `dotnet.md`**

Include:

- prefer existing project patterns;
- avoid unapproved package/reference changes;
- prefer `.slnx` commands when supported;
- use test projects according to layer;
- architecture tests are required for boundary-sensitive work when available;
- do not edit migrations unless the plan explicitly requires it.

### Task 5: Align Legacy Numbered Rules

**Files:**

- Modify: `.opencode/rules/00-core-workflow.md`
- Modify: `.opencode/rules/10-architecture-boundaries.md`
- Modify: `.opencode/rules/20-dotnet-style.md`
- Modify: `.opencode/rules/30-testing-verification.md`
- Modify: `.opencode/rules/40-agent-memory.md`
- Modify: `.opencode/rules/50-security-safety.md`
- Modify: `.opencode/rules/60-audit.md`

- [ ] **Step 1: Replace conflicting memory naming**

Run before editing:

```powershell
Get-ChildItem .\.opencode\rules -File |
  Select-String -Pattern '\.agents/local_notes','\.agents\\local_notes','local_notes.md'
```

Edit legacy rules so they do not present `.agents/local_notes.md` as the primary Iris memory file.

Expected result:

- `.agent/log_notes.md` is canonical;
- `local_notes.md` appears only as compatibility fallback.

- [ ] **Step 2: Convert numbered rules into compatibility rules**

Each numbered rule should either:

- summarize the canonical rule in fewer than 80 lines; or
- explicitly point to the matching v2 rule.

Do not leave contradictions between old and new rules.

- [ ] **Step 3: Verify no conflicting memory rule remains**

Run:

```powershell
Get-ChildItem .\.opencode\rules -File |
  Select-String -Pattern '\.agents/local_notes','\.agents\\local_notes'
```

Expected:

- no results.

### Task 6: Register v2 Rules in `opencode.jsonc`

**Files:**

- Modify: `opencode.jsonc`

- [ ] **Step 1: Update instructions list**

Modify `instructions` so v2 rules are loaded first:

```json
"instructions": [
  ".opencode/rules/iris-architecture.md",
  ".opencode/rules/memory.md",
  ".opencode/rules/verification.md",
  ".opencode/rules/no-shortcuts.md",
  ".opencode/rules/dotnet.md",
  ".opencode/rules/00-core-workflow.md",
  ".opencode/rules/10-architecture-boundaries.md",
  ".opencode/rules/20-dotnet-style.md",
  ".opencode/rules/30-testing-verification.md",
  ".opencode/rules/40-agent-memory.md",
  ".opencode/rules/50-security-safety.md",
  ".opencode/rules/60-audit.md"
]
```

Reason:

- v2 rules become canonical immediately;
- legacy numbered rules remain available during migration;
- Phase 3 can remove legacy references after command rewrite.

- [ ] **Step 2: Validate JSON**

Run:

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"
```

Expected:

```text
opencode.jsonc ok
```

### Task 7: Phase 0-1 Verification

**Files:**

- Inspect all Phase 0-1 files.

- [ ] **Step 1: Verify required files exist**

Run:

```powershell
$paths = @(
  '.opencode/docs/irisengineering-v2-phase-0-baseline.md',
  '.opencode/skills/iris-engineering/SKILL.md',
  '.opencode/skills/iris-architecture/SKILL.md',
  '.opencode/skills/iris-memory/SKILL.md',
  '.opencode/skills/iris-verification/SKILL.md',
  '.opencode/skills/iris-review/SKILL.md',
  '.opencode/rules/iris-architecture.md',
  '.opencode/rules/memory.md',
  '.opencode/rules/verification.md',
  '.opencode/rules/no-shortcuts.md',
  '.opencode/rules/dotnet.md'
)
$paths | ForEach-Object { [PSCustomObject]@{ Path = $_; Exists = Test-Path $_ } }
```

Expected:

- every `Exists` value is `True`.

- [ ] **Step 2: Verify memory naming**

Run:

```powershell
Get-ChildItem .\.opencode\skills,.\.opencode\rules -Recurse -File -Include *.md |
  Select-String -Pattern '\.agents/local_notes','\.agents\\local_notes'
```

Expected:

- no results.

- [ ] **Step 3: Verify skills are not bloated**

Run:

```powershell
Get-ChildItem .\.opencode\skills\iris-* -Directory | ForEach-Object {
  $path = Join-Path $_.FullName 'SKILL.md'
  [PSCustomObject]@{
    Skill = $_.Name
    Lines = (Get-Content $path).Count
  }
}
```

Expected:

- `iris-engineering` is the largest;
- no skill is a copy of AGENTS.md;
- no skill contains long PowerShell command injection blocks.

- [ ] **Step 4: Verify no commands were rewritten**

Run:

```powershell
git diff --name-only -- .opencode/commands
```

Expected:

- no output.

- [ ] **Step 5: Inspect final diff**

Run:

```powershell
git diff --stat
git diff -- .opencode opencode.jsonc
```

Expected:

- diff contains only Phase 0 baseline, Phase 1 skills/rules, legacy rule alignment, and `opencode.jsonc`;
- no command rewrite;
- no script creation;
- no plugin changes.

## Acceptance Criteria

- Phase 0 baseline exists and describes current `.opencode` state.
- Five canonical Iris skills exist.
- Five canonical Iris rules exist.
- Skills are detailed, practical, and readable, but not overloaded.
- `.agent/log_notes.md` is canonical in new memory guidance.
- `.agents/local_notes.md` is not presented as primary Iris memory.
- `opencode.jsonc` loads v2 rules.
- Existing commands are not rewritten in this phase.
- No shared scripts are created in this phase.
- No plugin behavior changes in this phase.

## Commit Guidance

Recommended commit split:

```powershell
git add .opencode/docs/irisengineering-v2-phase-0-baseline.md
git commit -m "docs: capture opencode v2 baseline"

git add .opencode/skills/iris-engineering .opencode/skills/iris-architecture .opencode/skills/iris-memory .opencode/skills/iris-verification .opencode/skills/iris-review
git commit -m "docs: add Iris OpenCode methodology skills"

git add .opencode/rules opencode.jsonc
git commit -m "docs: add Iris OpenCode v2 rules"
```

Do not commit automatically unless the user asks.

## Self-Review Checklist

- [ ] Phase 0 does not mutate commands, scripts, agents, or plugins.
- [ ] Phase 1 does not rewrite commands.
- [ ] New skills avoid copying AGENTS.md wholesale.
- [ ] New skills do not contain duplicated PowerShell context.
- [ ] New rules are concise hard constraints.
- [ ] Legacy numbered rules no longer contradict v2 rules.
- [ ] Memory rules match Iris actual `.agent/log_notes.md` convention.
- [ ] Verification commands are exact.
- [ ] Remaining Phase 2 work is clearly deferred.
