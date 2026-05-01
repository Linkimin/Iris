# IrisEngineering v2 Phase 4 Command Level 4 Rewrite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite all 14 `.opencode/commands/*.md` templates to Level 4 by using Iris skills, canonical rules, and shared Phase 3 scripts instead of large inline PowerShell blocks.

**Architecture:** Commands remain thin workflow selectors. Skills hold methodology, rules hold hard constraints, and `.opencode/scripts/*.ps1` provide factual context. Phase 4 rewrites command templates only; it must not change script behavior, rules, skills, agents, plugins, product code, or `AGENTS.md`.

**Tech Stack:** OpenCode markdown command files, official `!` shell output injection syntax, `$ARGUMENTS`, `.opencode/scripts/*.ps1`, Iris canonical skills/rules, PowerShell verification, Node JSON parse check.

---

## Level 4 Definition

For IrisEngineering v2, a Level 4 command is:

- workflow-specific;
- skill-backed;
- rule-backed through `opencode.jsonc` canonical rules;
- context-backed through shared scripts;
- short enough to audit;
- free of large copied PowerShell blocks;
- free of hard-coded local repo paths;
- explicit about read/write boundaries.

Level 4 does not mean every command has identical context. Each command should include only the scripts it needs.

## Current State

Command inventory:

| Command | Current lines | Inline PowerShell blocks | Main issue |
|---|---:|---:|---|
| `architecture-review.md` | 314 | 7 | large duplicated context blocks |
| `audit.md` | 349 | 7 | large duplicated context blocks |
| `design.md` | 164 | 7 | large duplicated context blocks |
| `implement.md` | 212 | 7 | large duplicated context blocks |
| `plan.md` | 150 | 5 | large duplicated context blocks |
| `review.md` | 224 | 4 | large duplicated context blocks |
| `save-audit.md` | 153 | 4 | large duplicated context blocks |
| `save-design.md` | 150 | 4 | large duplicated context blocks |
| `save-plan.md` | 157 | 5 | large duplicated context blocks |
| `save-spec.md` | 139 | 4 | large duplicated context blocks |
| `spec.md` | 160 | 4 | large duplicated context blocks |
| `status.md` | 111 | 11 | old backtick shell blocks and hard-coded repo fallback |
| `update-memory.md` | 274 | 5 | large duplicated context blocks |
| `verify.md` | 233 | 4 | large duplicated context blocks |

Phase 3 scripts available:

| Script | Use for |
|---|---|
| `.opencode/scripts/resolve-repo.ps1` | small one-line repo-root normalization before direct git/doc commands |
| `.opencode/scripts/git-context.ps1` | repository status, changed files, staged files, untracked files, stats, recent commits |
| `.opencode/scripts/agent-memory-context.ps1` | `.agent` preferred memory context and `.agents` fallback |
| `.opencode/scripts/project-guidance-context.ps1` | `AGENTS.md`, canonical loaded rules, requested skills |
| `.opencode/scripts/dotnet-discovery.ps1` | solution/project/test/build config discovery |
| `.opencode/scripts/architecture-context.ps1` | project references, DI/host files, architecture tests, suspicious boundary references |

## Out Of Scope

Do not modify:

- `.opencode/scripts/*.ps1`
- `.opencode/rules/*.md`
- `.opencode/skills/**/*.md`
- `.opencode/agents/**`
- `.opencode/plugins/**`
- `opencode.jsonc`
- `AGENTS.md`
- product source code under `src/**`
- tests under `tests/**`

Do not add new scripts in Phase 4. If a missing script is discovered, record it as Phase 5 debt and use a short direct command with `resolve-repo.ps1` for now.

## Command Injection Standard

Use official OpenCode shell injection syntax:

```markdown
!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`
```

For direct commands that need repository root:

```markdown
!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`
```

Do not use large inline resolver blocks.

Do not use hard-coded fallbacks such as:

```text
E:\Work\Iris
```

## Shared Context Blocks

Use these exact blocks when rewriting commands.

### Repository Context

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`
```

### Agent Memory Context

```markdown
## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`
```

### Project Guidance Context

Use command-specific `-SkillPath` values.

```markdown
## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath <skill-paths>`
```

Examples:

```markdown
!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/spec/SKILL.md`
```

```markdown
!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-review/SKILL.md,.opencode/skills/audit/SKILL.md`
```

### .NET Discovery Context

```markdown
## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`
```

### Architecture Context

```markdown
## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`
```

### Current Diff Context

```markdown
## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`
```

### Staged Diff Context

```markdown
## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`
```

### Diff Summary Context

```markdown
## Diff Summary Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Current Diff Stat'; git diff --stat; Write-Output ''; Write-Output '## Current Diff Name Status'; git diff --name-status; Write-Output ''; Write-Output '## Staged Diff Stat'; git diff --cached --stat; Write-Output ''; Write-Output '## Staged Diff Name Status'; git diff --cached --name-status"`
```

### Documentation Discovery Context

Use this only in `spec.md`, `design.md`, `plan.md`, and save commands where existing docs/artifacts matter.

```markdown
## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach ($dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path $dir) { Write-Output $dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`
```

This is intentionally short and read-only. Do not recreate the old long discovery blocks.

## Command Skill Map

Each command must explicitly name `iris-engineering` plus its focused skill(s) near the top.

| Command | Required skill lines |
|---|---|
| `status.md` | `Use the iris-engineering skill.` |
| `spec.md` | `Use the iris-engineering skill.` and `Use the spec skill.` |
| `design.md` | `Use the iris-engineering skill.`, `Use the iris-architecture skill.`, `Use the design skill.` |
| `plan.md` | `Use the iris-engineering skill.` and `Use the plan skill.` |
| `implement.md` | `Use the iris-engineering skill.` and `Use the implement skill.` |
| `verify.md` | `Use the iris-engineering skill.`, `Use the iris-verification skill.`, `Use the verify skill.` |
| `review.md` | `Use the iris-engineering skill.` and `Use the iris-review skill.` |
| `architecture-review.md` | `Use the iris-engineering skill.`, `Use the iris-architecture skill.`, `Use the iris-review skill.`, `Use the architecture-boundary-review skill.` |
| `audit.md` | `Use the iris-engineering skill.`, `Use the iris-review skill.`, `Use the audit skill.` |
| `update-memory.md` | `Use the iris-engineering skill.`, `Use the iris-memory skill.`, `Use the agent-memory skill.` |
| `save-spec.md` | `Use the iris-engineering skill.` and `Use the save-spec skill.` |
| `save-design.md` | `Use the iris-engineering skill.` and `Use the save-design skill.` |
| `save-plan.md` | `Use the iris-engineering skill.` and `Use the save-plan skill.` |
| `save-audit.md` | `Use the iris-engineering skill.` and `Use the save-audit skill.` |

Use backticked skill names in command text if local convention prefers:

```markdown
Use the `iris-engineering` skill.
Use the `spec` skill.
```

## Command Context Map

| Command | Required script-backed context |
|---|---|
| `status.md` | Repository Context, Agent Memory Context |
| `spec.md` | Repository Context, Project Guidance Context, Agent Memory Context, Documentation Discovery Context, .NET Discovery Context |
| `design.md` | Repository Context, Project Guidance Context, Agent Memory Context, Documentation Discovery Context, .NET Discovery Context, Architecture Context, Current Diff Context |
| `plan.md` | Repository Context, Project Guidance Context, Agent Memory Context, Documentation Discovery Context, .NET Discovery Context |
| `implement.md` | Repository Context, Project Guidance Context, Agent Memory Context, Documentation Discovery Context, .NET Discovery Context, Current Diff Context, Staged Diff Context |
| `verify.md` | Repository Context, Project Guidance Context, Agent Memory Context, .NET Discovery Context |
| `review.md` | Repository Context, Agent Memory Context, Current Diff Context, Staged Diff Context |
| `architecture-review.md` | Repository Context, Project Guidance Context, Agent Memory Context, Architecture Context, Current Diff Context, Staged Diff Context |
| `audit.md` | Repository Context, Project Guidance Context, Agent Memory Context, Architecture Context, Documentation Discovery Context, Current Diff Context, Staged Diff Context |
| `update-memory.md` | Repository Context, Project Guidance Context, Agent Memory Context, Documentation Discovery Context, Diff Summary Context |
| `save-spec.md` | Repository Context, Project Guidance Context, Documentation Discovery Context, Agent Memory Context |
| `save-design.md` | Repository Context, Project Guidance Context, Documentation Discovery Context, Agent Memory Context |
| `save-plan.md` | Repository Context, Project Guidance Context, Documentation Discovery Context, Agent Memory Context |
| `save-audit.md` | Repository Context, Project Guidance Context, Documentation Discovery Context, Agent Memory Context |

## Command Length Targets

Do not chase arbitrary minimalism, but the rewrite should materially reduce command size.

Target maximums:

| Command group | Target |
|---|---:|
| `status.md` | under 80 lines |
| `spec.md`, `design.md`, `plan.md` | under 140 lines each |
| `implement.md`, `verify.md`, `review.md` | under 180 lines each |
| `architecture-review.md`, `audit.md`, `update-memory.md` | under 240 lines each |
| `save-*` commands | under 130 lines each |

If a command remains above target, report why.

---

### Task 1: Create Phase 4 Baseline And Migration Map

**Files:**

- Create: `.opencode/docs/irisengineering-v2-phase-4-command-level-4-rewrite.md`

- [ ] **Step 1: Capture current command metrics**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Sort-Object Name |
  ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    [PSCustomObject]@{
      Name = $_.Name
      Lines = (Get-Content $_.FullName).Count
      InlinePowershell = ([regex]::Matches($content, '!`?powershell|!powershell')).Count
      ContainsHardcodedRepo = $content -match 'E:\\Work\\Iris'
    }
  } |
  Format-Table -AutoSize
```

Expected:

- all 14 commands are listed;
- current commands still contain inline PowerShell blocks;
- `status.md` contains a hard-coded repo fallback before rewrite.

- [ ] **Step 2: Create Phase 4 baseline document**

Create `.opencode/docs/irisengineering-v2-phase-4-command-level-4-rewrite.md`:

```markdown
# IrisEngineering v2 Phase 4 Command Level 4 Rewrite

## Goal

Rewrite the 14 OpenCode command templates so they use Iris skills, canonical rules, and Phase 3 shared scripts.

## Level 4 Definition

Level 4 command = workflow selector + focused skills + canonical loaded rules + shared script context.

## Source Inputs

- `.opencode/commands/*.md`
- `.opencode/scripts/*.ps1`
- `.opencode/rules/*.md`
- `.opencode/skills/**/*.md`
- `opencode.jsonc`

## Rewrite Decision

Commands should use official OpenCode shell injection syntax:

```markdown
!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`
```

Large inline PowerShell resolver blocks are forbidden in command templates after this phase.

## In Scope

- Rewrite `.opencode/commands/*.md`.
- Keep command behavior and output contracts.
- Add `iris-engineering` plus focused skill usage lines.
- Replace large context blocks with shared script calls.

## Out Of Scope

- Script behavior changes.
- Skill/rule rewrites.
- `opencode.jsonc` changes.
- Product code changes.
- `AGENTS.md` changes.

## Acceptance

- 14 commands still exist.
- No command contains `E:\Work\Iris`.
- No command contains copied `$repo = if (...)` resolver blocks.
- Commands call shared scripts for repository, memory, guidance, .NET, and architecture context.
- Commands keep read/write boundaries.
- Commands still preserve `$ARGUMENTS` where they used it before.
```

- [ ] **Step 3: Verify baseline document**

Run:

```powershell
Test-Path .\.opencode\docs\irisengineering-v2-phase-4-command-level-4-rewrite.md
Get-Content .\.opencode\docs\irisengineering-v2-phase-4-command-level-4-rewrite.md -TotalCount 80
```

Expected:

- `Test-Path` returns `True`;
- document states that command templates are the only `.opencode` files in scope.

### Task 2: Rewrite `/status`

**Files:**

- Modify: `.opencode/commands/status.md`

- [ ] **Step 1: Replace `status.md` with Level 4 content**

Use this full file content:

```markdown
---
description: Summarize current project status from git state and agent memory
agent: planner
---

# /status

Use the `iris-engineering` skill.

Summarize the current project status.

Do not implement.
Do not edit files.
Do not create files.
Do not update memory files.
Do not run verification commands unless explicitly requested.
Do not restate this command template.
Do not narrate reasoning.
Use only the factual context injected below.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Output Format

# Project Status

## 1. Summary

<compact factual summary>

## 2. Git State

- Branch:
- Working tree:
- Changed files:
- Untracked files:

## 3. Current Work

- Active task:
- Active phase:
- Last completed:
- Next safe step:

## 4. Recent Changes

- ...

## 5. Open Issues / Risks

- ...

## 6. Last Known Verification

- Command:
- Result:
- Date:

## 7. Recommended Next Step

- ...

## Execution Note

No implementation was performed.
No files were modified.
```

- [ ] **Step 2: Verify `/status` no longer has inline resolver blocks**

Run:

```powershell
Select-String -Path .\.opencode\commands\status.md -Pattern 'E:\\Work\\Iris','OPENCODE_PROJECT_ROOT','git status --short','Get-Content.*overview'
```

Expected:

- no output.

- [ ] **Step 3: Verify `/status` still has required contexts**

Run:

```powershell
Select-String -Path .\.opencode\commands\status.md -Pattern 'iris-engineering','git-context.ps1','agent-memory-context.ps1'
```

Expected:

- all three patterns are found.

### Task 3: Rewrite Read-Only Review Commands

**Files:**

- Modify: `.opencode/commands/review.md`
- Modify: `.opencode/commands/architecture-review.md`
- Modify: `.opencode/commands/audit.md`

- [ ] **Step 1: In `review.md`, replace context blocks**

Keep existing hard rules, review scope, severity rules, and output format.

Replace the current `## Repository Context`, `## Agent Memory Context`, `## Current Diff`, and `## Staged Diff` sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`
```

Near the top after `# /review`, ensure these lines exist:

```markdown
Use the `iris-engineering` skill.
Use the `iris-review` skill.
```

- [ ] **Step 2: In `architecture-review.md`, replace context blocks**

Keep existing hard rules, review scope, dependency direction, forbidden shortcuts, severity rules, and output format.

Replace:

- `## Repository Context`
- `## Project Guidance Context`
- `## Agent Memory Context`
- `## Project Structure Context`
- `## Source Boundary Search Context`
- `## Current Diff`
- `## Staged Diff`

with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-architecture/SKILL.md,.opencode/skills/iris-review/SKILL.md,.opencode/skills/architecture-boundary-review/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`
```

Near the top after `# /architecture-review`, ensure these lines exist:

```markdown
Use the `iris-engineering` skill.
Use the `iris-architecture` skill.
Use the `iris-review` skill.
Use the `architecture-boundary-review` skill.
```

- [ ] **Step 3: In `audit.md`, replace context blocks**

Keep existing hard rules, required audit passes, severity rules, approval rules, and output format.

Replace:

- `## Repository Context`
- `## Project Guidance Context`
- `## Agent Memory Context`
- `## Project Structure Context`
- `## Verification Evidence Discovery`
- `## Current Diff`
- `## Staged Diff`

with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-review/SKILL.md,.opencode/skills/audit/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach ($dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path $dir) { Write-Output $dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`
```

Near the top after `# /audit`, ensure these lines exist:

```markdown
Use the `iris-engineering` skill.
Use the `iris-review` skill.
Use the `audit` skill.
```

- [ ] **Step 4: Verify read-only review commands**

Run:

```powershell
foreach ($file in @('review.md','architecture-review.md','audit.md')) {
  Write-Output "### $file"
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'iris-engineering','git-context.ps1','agent-memory-context.ps1'
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'E:\\Work\\Iris','OPENCODE_PROJECT_ROOT','elseif \(Test-Path' 
}
```

Expected:

- skill/script matches exist;
- no `E:\Work\Iris`, `OPENCODE_PROJECT_ROOT`, or copied `elseif (Test-Path ...)` resolver logic remains.

### Task 4: Rewrite Spec, Design, And Plan Commands

**Files:**

- Modify: `.opencode/commands/spec.md`
- Modify: `.opencode/commands/design.md`
- Modify: `.opencode/commands/plan.md`

- [ ] **Step 1: Rewrite context in `spec.md`**

Keep existing hard rules, targeted inspection guidance, specification scope rules, and output format.

Ensure top skill lines:

```markdown
Use the `iris-engineering` skill.
Use the `spec` skill.
```

Replace existing context sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/spec/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach ($dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path $dir) { Write-Output $dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`
```

- [ ] **Step 2: Rewrite context in `design.md`**

Keep existing hard rules, design scope, review discipline, and output format.

Ensure top skill lines:

```markdown
Use the `iris-engineering` skill.
Use the `iris-architecture` skill.
Use the `design` skill.
```

Replace existing context sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-architecture/SKILL.md,.opencode/skills/design/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach ($dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path $dir) { Write-Output $dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`
```

- [ ] **Step 3: Rewrite context in `plan.md`**

Keep existing hard rules, required context, scope rules, planning requirements, and output format.

Ensure top skill lines:

```markdown
Use the `iris-engineering` skill.
Use the `plan` skill.
```

Replace existing context sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/plan/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach ($dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path $dir) { Write-Output $dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`
```

- [ ] **Step 4: Verify authoring commands**

Run:

```powershell
foreach ($file in @('spec.md','design.md','plan.md')) {
  Write-Output "### $file"
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'iris-engineering','project-guidance-context.ps1','agent-memory-context.ps1','dotnet-discovery.ps1'
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'E:\\Work\\Iris','OPENCODE_PROJECT_ROOT','elseif \(Test-Path'
}
```

Expected:

- script/skill matches exist;
- no copied resolver blocks remain.

### Task 5: Rewrite Implement And Verify Commands

**Files:**

- Modify: `.opencode/commands/implement.md`
- Modify: `.opencode/commands/verify.md`

- [ ] **Step 1: Rewrite context in `implement.md`**

Keep existing hard rules, required pre-edit checks, allowed changes, verification section, forbidden commands, and output format.

Ensure top skill lines:

```markdown
Use the `iris-engineering` skill.
Use the `implement` skill.
```

Replace existing context sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/implement/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Approved Work Artifacts Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Candidate Spec Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.spec.md','*spec*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 20 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }; Write-Output ''; Write-Output '## Candidate Design Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.design.md','*design*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 20 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }; Write-Output ''; Write-Output '## Candidate Plan Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.plan.md','*plan*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 30 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`
```

- [ ] **Step 2: Rewrite context in `verify.md`**

Keep existing hard rules, verification execution section, forbidden commands, verification scope, and output format.

Ensure top skill lines:

```markdown
Use the `iris-engineering` skill.
Use the `iris-verification` skill.
Use the `verify` skill.
```

Replace existing context sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-verification/SKILL.md,.opencode/skills/verify/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Build and Test Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`
```

- [ ] **Step 3: Verify implement/verify commands**

Run:

```powershell
foreach ($file in @('implement.md','verify.md')) {
  Write-Output "### $file"
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'iris-engineering','git-context.ps1','project-guidance-context.ps1','agent-memory-context.ps1'
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'E:\\Work\\Iris','OPENCODE_PROJECT_ROOT','elseif \(Test-Path'
}
```

Expected:

- script/skill matches exist;
- no copied resolver blocks remain.

### Task 6: Rewrite Save Commands

**Files:**

- Modify: `.opencode/commands/save-spec.md`
- Modify: `.opencode/commands/save-design.md`
- Modify: `.opencode/commands/save-plan.md`
- Modify: `.opencode/commands/save-audit.md`

- [ ] **Step 1: Apply common save command context**

For each save command, keep existing hard rules, save behavior, allowed changes, and output format.

Use this context pattern:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/<save-skill>/SKILL.md`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach ($dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path $dir) { Write-Output $dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`
```

Use these exact skill substitutions:

| Command | `<save-skill>` | Required top skill lines |
|---|---|---|
| `save-spec.md` | `save-spec` | `Use the iris-engineering skill.` and `Use the save-spec skill.` |
| `save-design.md` | `save-design` | `Use the iris-engineering skill.` and `Use the save-design skill.` |
| `save-plan.md` | `save-plan` | `Use the iris-engineering skill.` and `Use the save-plan skill.` |
| `save-audit.md` | `save-audit` | `Use the iris-engineering skill.` and `Use the save-audit skill.` |

- [ ] **Step 2: Verify save commands**

Run:

```powershell
foreach ($file in @('save-spec.md','save-design.md','save-plan.md','save-audit.md')) {
  Write-Output "### $file"
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'iris-engineering','project-guidance-context.ps1','Documentation Discovery Context','agent-memory-context.ps1'
  Select-String -Path ".\.opencode\commands\$file" -Pattern 'E:\\Work\\Iris','OPENCODE_PROJECT_ROOT','elseif \(Test-Path'
}
```

Expected:

- script/skill matches exist;
- no copied resolver blocks remain.

### Task 7: Rewrite `/update-memory`

**Files:**

- Modify: `.opencode/commands/update-memory.md`

- [ ] **Step 1: Rewrite context in `update-memory.md`**

Keep existing hard rules, memory file responsibilities, default formats, allowed changes, post-update verification, and output format.

Ensure top skill lines:

```markdown
Use the `iris-engineering` skill.
Use the `iris-memory` skill.
Use the `agent-memory` skill.
```

Replace existing context sections with:

```markdown
## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-memory/SKILL.md,.opencode/skills/agent-memory/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Relevant Artifact Discovery

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Candidate Spec / Design / Plan / Audit Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -match '(spec|design|plan|audit|review|verification|architecture|checkpoint)' -and $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Select-Object -First 120 -ExpandProperty FullName } else { Write-Output 'docs directory not found' }; Write-Output ''; Write-Output '## Recent Markdown Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 40 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Diff Summary Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Current Diff Stat'; git diff --stat; Write-Output ''; Write-Output '## Current Diff Name Status'; git diff --name-status; Write-Output ''; Write-Output '## Staged Diff Stat'; git diff --cached --stat; Write-Output ''; Write-Output '## Staged Diff Name Status'; git diff --cached --name-status"`
```

- [ ] **Step 2: Verify update-memory command**

Run:

```powershell
Select-String -Path .\.opencode\commands\update-memory.md -Pattern 'iris-memory','agent-memory','git-context.ps1','agent-memory-context.ps1','Diff Summary Context'
Select-String -Path .\.opencode\commands\update-memory.md -Pattern 'E:\\Work\\Iris','OPENCODE_PROJECT_ROOT','elseif \(Test-Path'
```

Expected:

- required matches exist;
- no copied resolver blocks remain.

### Task 8: Normalize Command Syntax And Metadata

**Files:**

- Modify: `.opencode/commands/*.md`

- [ ] **Step 1: Ensure all commands preserve frontmatter**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Sort-Object Name |
  ForEach-Object {
    $lines = Get-Content $_.FullName -TotalCount 8
    [PSCustomObject]@{
      Name = $_.Name
      StartsWithFrontmatter = $lines[0] -eq '---'
      HasDescription = ($lines -match '^description:').Count -gt 0
      HasAgent = ($lines -match '^agent:').Count -gt 0
    }
  } |
  Format-Table -AutoSize
```

Expected:

- every command starts with frontmatter;
- every command has `description`;
- every command has `agent`.

- [ ] **Step 2: Ensure `$ARGUMENTS` is preserved where needed**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Where-Object { $_.Name -ne 'status.md' } |
  ForEach-Object {
    [PSCustomObject]@{
      Name = $_.Name
      HasArguments = (Get-Content $_.FullName -Raw) -match '\$ARGUMENTS'
    }
  } |
  Format-Table -AutoSize
```

Expected:

- every command except `status.md` has `HasArguments = True`.

- [ ] **Step 3: Ensure official shell injection syntax is used**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern '^!powershell'
```

Expected:

- no output.

Then run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern '^!`powershell'
```

Expected:

- all shell injection lines use `!`powershell ...`` syntax.

### Task 9: Final Command Safety Verification

**Files:**

- Inspect: `.opencode/commands/*.md`

- [ ] **Step 1: Verify no hard-coded local paths remain**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern 'E:\\Work\\Iris','C:\\Users\\User'
```

Expected:

- no output.

- [ ] **Step 2: Verify no copied resolver blocks remain**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern 'OPENCODE_PROJECT_ROOT','elseif \(Test-Path','Set-Location \$repo'
```

Expected:

- no output.

- [ ] **Step 3: Verify commands use shared scripts**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    [PSCustomObject]@{
      Name = $_.Name
      UsesGitContext = $content -match 'git-context\.ps1'
      UsesResolveRepo = $content -match 'resolve-repo\.ps1'
      UsesMemoryContext = $content -match 'agent-memory-context\.ps1'
      UsesGuidanceContext = $content -match 'project-guidance-context\.ps1'
      UsesDotnetDiscovery = $content -match 'dotnet-discovery\.ps1'
      UsesArchitectureContext = $content -match 'architecture-context\.ps1'
    }
  } |
  Format-Table -AutoSize
```

Expected:

- every command uses `git-context.ps1` or a documented exception;
- every command except narrow exceptions uses `agent-memory-context.ps1`;
- architecture-sensitive commands use `architecture-context.ps1`;
- `.NET` work commands use `dotnet-discovery.ps1`.

- [ ] **Step 4: Verify no forbidden mutating shell commands were introduced**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern 'git push','git clean','git reset --hard','Remove-Item','Set-Content','Add-Content','Out-File','dotnet restore','dotnet add package','Update-Database'
```

Expected:

- no output, except command prose warning that such commands are forbidden.
- If prose warnings are matched, inspect and confirm they are in `Forbidden Commands` or `Hard Rules`, not shell injections.

- [ ] **Step 5: Verify command line counts improved**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Sort-Object Name |
  Select-Object Name,@{Name='Lines';Expression={(Get-Content $_.FullName).Count}} |
  Format-Table -AutoSize
```

Expected:

- command files are materially smaller than baseline;
- any command above target is explained in final notes.

### Task 10: Smoke Test Script References From Commands

**Files:**

- Inspect: `.opencode/commands/*.md`
- Execute: `.opencode/scripts/*.ps1`

- [ ] **Step 1: Extract script references from command files**

Run:

```powershell
$content = Get-ChildItem .\.opencode\commands -File | ForEach-Object { Get-Content $_.FullName -Raw }
$scriptRefs = [regex]::Matches(($content -join "`n"), '\.opencode/scripts/[A-Za-z0-9_.-]+\.ps1') |
  ForEach-Object { $_.Value } |
  Sort-Object -Unique

$scriptRefs | ForEach-Object {
  [PSCustomObject]@{
    Script = $_
    Exists = Test-Path $_
  }
} |
Format-Table -AutoSize
```

Expected:

- every referenced script exists.

- [ ] **Step 2: Run all referenced scripts once**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\resolve-repo.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\git-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\agent-memory-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\dotnet-discovery.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\architecture-context.ps1
```

Expected:

- every script exits successfully;
- output headings are readable.

- [ ] **Step 3: Run representative inline direct commands**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff --stat"
powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff --stat"
```

Expected:

- both commands exit successfully.

### Task 11: Update Agent Memory

**Files:**

- Modify: `.agent/PROJECT_LOG.md`
- Modify: `.agent/overview.md`

- [ ] **Step 1: Prepend PROJECT_LOG entry**

Add this entry near the top of `.agent/PROJECT_LOG.md`:

```markdown
## 2026-04-30 - OpenCode IrisEngineering v2 Phase 4 command Level 4 rewrite plan

### Changed
- Created the Phase 4 implementation plan for rewriting all 14 OpenCode command templates to Level 4.
- Defined Level 4 as workflow selector + focused skills + canonical loaded rules + shared script context.
- Mapped each command to required Iris skills and Phase 3 scripts.
- Kept script behavior, rules, skills, agents, plugins, `opencode.jsonc`, product code, and `AGENTS.md` out of scope.

### Files
- docs/superpowers/plans/2026-04-30-opencode-irisengineering-v2-phase-4-command-level-4-rewrite.md
- .agent/PROJECT_LOG.md
- .agent/overview.md

### Validation
- Inspected all 14 current command files and measured line counts plus inline PowerShell usage.
- Inspected Phase 3 shared scripts and baseline document.
- Used Context7 OpenCode docs to confirm command markdown, frontmatter, `$ARGUMENTS`, and shell output injection behavior.
- Did not run .NET build/test because this iteration only creates a plan and local memory updates.

### Next
- Execute Phase 4 command Level 4 rewrite, then smoke test representative OpenCode commands.
```

- [ ] **Step 2: Update overview**

Update `.agent/overview.md` so:

- current OpenCode target says Phase 4 command Level 4 rewrite plan is ready;
- next OpenCode step says execute Phase 4 command rewrite;
- product track Avatar v1 status remains intact.

- [ ] **Step 3: Verify memory diff**

Run:

```powershell
git diff -- .agent/PROJECT_LOG.md .agent/overview.md
```

Expected:

- only factual Phase 4 planning notes were added;
- Avatar/product track notes were not removed.

## Acceptance Criteria

- Phase 4 plan exists under `docs/superpowers/plans`.
- `.opencode/docs/irisengineering-v2-phase-4-command-level-4-rewrite.md` exists.
- All 14 command files still exist.
- Each command preserves frontmatter with `description` and `agent`.
- Every command except `status.md` preserves `$ARGUMENTS`.
- Every command explicitly uses `iris-engineering`.
- Focused commands explicitly use the relevant focused skill(s).
- Commands use official `!`powershell ...`` shell injection syntax.
- Commands call Phase 3 shared scripts for common context.
- No command contains `E:\Work\Iris` or `C:\Users\User`.
- No command contains copied repo resolver blocks with `OPENCODE_PROJECT_ROOT`, `elseif (Test-Path ...)`, or `Set-Location $repo`.
- No command introduces mutating shell injections.
- `.opencode/scripts`, `.opencode/rules`, `.opencode/skills`, `.opencode/agents`, `.opencode/plugins`, `opencode.jsonc`, `AGENTS.md`, `src/**`, and `tests/**` are not modified by Phase 4.
- `.agent` memory is updated with factual Phase 4 planning or implementation status.

## Validation Commands

Run at the end:

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"

Get-ChildItem .\.opencode\commands -File |
  Sort-Object Name |
  Select-Object Name,@{Name='Lines';Expression={(Get-Content $_.FullName).Count}} |
  Format-Table -AutoSize

Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern 'E:\\Work\\Iris','C:\\Users\\User'

Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern 'OPENCODE_PROJECT_ROOT','elseif \(Test-Path','Set-Location \$repo'

Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern '^!powershell'

Get-ChildItem .\.opencode\commands -File |
  ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    [PSCustomObject]@{
      Name = $_.Name
      UsesIrisEngineering = $content -match 'iris-engineering'
      HasArguments = if ($_.Name -eq 'status.md') { $true } else { $content -match '\$ARGUMENTS' }
      UsesSharedScript = $content -match '\.opencode/scripts/'
    }
  } |
  Format-Table -AutoSize

$content = Get-ChildItem .\.opencode\commands -File | ForEach-Object { Get-Content $_.FullName -Raw }
[regex]::Matches(($content -join "`n"), '\.opencode/scripts/[A-Za-z0-9_.-]+\.ps1') |
  ForEach-Object { $_.Value } |
  Sort-Object -Unique |
  ForEach-Object { [PSCustomObject]@{ Script = $_; Exists = Test-Path $_ } } |
  Format-Table -AutoSize

powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\git-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\agent-memory-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\dotnet-discovery.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\architecture-context.ps1

git diff --name-only -- .opencode/scripts .opencode/rules .opencode/skills .opencode/agents .opencode/plugins opencode.jsonc AGENTS.md src tests
```

For branch-completion confidence, run:

```powershell
dotnet build .\Iris.slnx --no-restore
dotnet test .\Iris.slnx --no-restore
```

If only command docs changed and .NET checks are skipped, report that explicitly.

## Commit Guidance

Recommended commit split:

```powershell
git add .opencode/docs/irisengineering-v2-phase-4-command-level-4-rewrite.md docs/superpowers/plans/2026-04-30-opencode-irisengineering-v2-phase-4-command-level-4-rewrite.md
git commit -m "docs: plan OpenCode command Level 4 rewrite"

git add .opencode/commands
git commit -m "docs: rewrite OpenCode commands to Level 4"
```

Do not commit automatically unless the user asks.

## Self-Review Checklist

- [ ] Plan covers all 14 commands.
- [ ] Plan keeps Phase 4 scoped to command templates.
- [ ] Plan does not modify shared scripts.
- [ ] Level 4 definition is explicit.
- [ ] Every command has required skills.
- [ ] Shared script usage is concrete.
- [ ] `$ARGUMENTS` preservation is verified.
- [ ] Official OpenCode shell injection syntax is verified.
- [ ] Hard-coded local paths are forbidden.
- [ ] Out-of-scope files are protected.
