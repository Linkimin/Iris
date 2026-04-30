---
description: Update project agent memory files after meaningful work, verification, audit, or checkpoint
agent: builder
---

# /update-memory

Use the iris-engineering skill. Use the iris-memory skill. Use the agent-memory skill.

Update project agent memory for:

$ARGUMENTS

If the update target is empty, infer the memory update from the current repository state, recent changes, and latest verification/audit evidence.

## Hard Rules

Do not implement source changes.  
Do not modify product behavior.  
Do not modify tests.  
Do not modify build configuration.  
Do not rewrite project history.  
Do not store private reasoning.  
Do not store secrets, credentials, tokens, keys, production configs, or real customer data.  
Do not dump full specs, designs, plans, audits, or diffs into memory.  
Record factual project state only.  
Keep memory updates minimal and relevant.  
Preserve existing memory file format.  
Update only the memory files relevant to the change.

Prefer the existing `.agent` directory.  
Use `.agents` only if `.agent` does not exist and `.agents` does exist.  
Do not create `.agents` when `.agent` already exists.  
Create missing memory files only when needed and only inside the detected agent memory directory.

If repository context is missing, empty, or clearly points to the wrong directory, stop and report an evidence gap.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-memory/SKILL.md,.opencode/skills/agent-memory/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Relevant Artifact Discovery

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Candidate Spec / Design / Plan / Audit Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -match '(spec|design|plan|audit|review|verification|architecture|checkpoint)' -and `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Select-Object -First 120 -ExpandProperty FullName } else { Write-Output 'docs directory not found' }; Write-Output ''; Write-Output '## Recent Markdown Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 40 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Diff Summary Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Current Diff Stat'; git diff --stat; Write-Output ''; Write-Output '## Current Diff Name Status'; git diff --name-status; Write-Output ''; Write-Output '## Staged Diff Stat'; git diff --cached --stat; Write-Output ''; Write-Output '## Staged Diff Name Status'; git diff --cached --name-status"`

## Required Context to Inspect

Before updating memory, consider:

- `AGENTS.md`;
- `.opencode/skills/agent-memory/SKILL.md` if present;
- relevant `.opencode/rules/*.md` if present;
- detected agent memory directory: `.agent` preferred, `.agents` fallback;
- `PROJECT_LOG.md` if present;
- `overview.md` if present;
- `local_notes.md` or `log_notes.md` if present;
- `mem_library/**` if present and relevant;
- current git status;
- recently changed files;
- latest verification result if available;
- saved spec/design/plan/audit paths if relevant.

## Memory File Responsibilities

### `<agentDir>/PROJECT_LOG.md`

Use for:

- completed iterations;
- saved artifacts;
- implementation changes;
- verification results;
- audit results;
- meaningful decisions;
- phase transitions.

Entries must be newest-first.

### `<agentDir>/overview.md`

Use for:

- current project status;
- active phase;
- current focus;
- last completed work;
- next safe step;
- blockers;
- last verification result;
- important constraints.

### `<agentDir>/local_notes.md` or `<agentDir>/log_notes.md`

Use the existing file name if one already exists.

Use for:

- unresolved bugs;
- risks;
- blockers;
- skipped verification;
- follow-up tasks;
- suspicious architecture drift.

### `<agentDir>/mem_library/**`

Use only for durable project knowledge:

- stable architectural decisions;
- accepted conventions;
- long-term constraints;
- project identity;
- roadmap-level facts.

Do not place short-lived iteration notes into `mem_library`.

## Default Formats

### PROJECT_LOG.md Entry

```md
## YYYY-MM-DD — <Short Title>

### Changed
- ...

### Verified
- ...

### Notes
- ...
```

### overview.md

```md
# Agent Overview

## Project

<compact project identity>

## Current Status

- Active phase: ...
- Last completed: ...
- Current focus: ...
- Next safe step: ...

## Last Verification

- Command: ...
- Result: ...
- Date: ...

## Current Blockers

- ...

## Architecture Constraints

- ...

## Important Notes for Next Session

- ...
```

### local_notes.md

```md
# Local Notes

## Open Issues

### <Severity> — <Issue Title>

- Status: open
- Found: YYYY-MM-DD
- Evidence: ...
- Impact: ...
- Suggested fix: ...

## Risks

- ...

## Follow-ups

- ...
```

## Allowed Changes

Allowed:

- prepend a `PROJECT_LOG.md` entry;
- update current status in `overview.md`;
- add or update unresolved issues in `local_notes.md` / `log_notes.md`;
- add or update durable decisions in `mem_library/**`;
- create a missing memory file only if needed;
- lightly normalize formatting in the edited section.

Not allowed:

- source code changes;
- test changes;
- config changes;
- unrelated documentation edits;
- changing implementation behavior;
- deleting old memory entries without explicit approval;
- rewriting unrelated history;
- storing secrets;
- storing private reasoning;
- claiming verification that did not happen.

## Post-Update Verification

After updating memory, run safe inspection commands:

- `git status --short`
- `git diff --name-status`
- `git diff -- <agentDir>/PROJECT_LOG.md <agentDir>/overview.md <agentDir>/local_notes.md <agentDir>/log_notes.md`

Confirm that only intended memory files changed.

## Output Format

# Agent Memory Updated

## Files Modified

- `<path>` — <what changed>

## Recorded

- ...

## Verification

- Confirmed memory files were updated.
- Implementation files were not modified.

## Notes

- ...

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

## If Memory Cannot Be Updated

# Agent Memory Update Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
