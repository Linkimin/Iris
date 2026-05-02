---
description: Implement an approved plan with minimal architecture-compliant changes
agent: builder
---

# /implement

Use the `iris-engineering` skill.
Use the `iris-tdd` skill.
Use the `implement` skill.

Implement the approved implementation plan for:

$ARGUMENTS

If `$ARGUMENTS` is empty, inspect the repository context and stop unless there is a clearly approved active implementation plan.

## Hard Rules

Implement only the approved plan.  
Do not create new requirements.  
Do not redesign the architecture.  
Do not expand scope.  
Do not perform unrelated refactoring.  
Do not change public contracts unless explicitly approved by the plan.  
Do not add dependencies unless explicitly approved by the plan.  
Do not create files before checking whether suitable files, folders, abstractions, or placeholders already exist.  
Do not delete tests to make verification pass.  
Do not modify secrets, credentials, production configs, or private keys.  
Do not run destructive commands.  
Do not create commits.  
Do not push to remote.

If no approved plan exists, stop unless the requested change is trivial, local, and explicitly requested.

If repository context is missing, empty, or points to the wrong directory, stop and report an evidence gap.

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

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/implement/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Approved Work Artifacts Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Candidate Spec Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.spec.md','*spec*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 20 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }; Write-Output ''; Write-Output '## Candidate Design Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.design.md','*design*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 20 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }; Write-Output ''; Write-Output '## Candidate Plan Files'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.plan.md','*plan*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 30 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`

## Required Pre-Edit Checks

Before editing:

- identify the approved specification, if present;
- identify the approved architecture design, if present;
- identify the approved implementation plan;
- inspect the plan sections that define allowed files and forbidden changes;
- inspect existing folders/files/placeholders before creating anything;
- inspect relevant tests before changing behavior;
- identify the narrowest useful verification commands;
- state if the plan is missing, stale, ambiguous, or contradicts the current repository state.

If the plan is missing or unsafe to execute, stop.

## Allowed Changes

Allowed:

- edit files directly required by the approved plan;
- create files required by the approved plan only after checking existing structure;
- add or update tests for changed behavior;
- update documentation directly related to changed behavior when the approved plan requires it;
- update `.agent` or `.agents` memory files only if project convention requires it or the user explicitly asks.

Not allowed:

- unrelated cleanup;
- broad formatting changes;
- speculative abstractions;
- silent contract changes;
- unplanned project reference changes;
- unplanned migration changes;
- unplanned package additions;
- unrelated documentation edits;
- source changes outside the approved scope.

## Verification

After implementation, run the narrowest useful verification first.

Use repository-specific commands if present.

For .NET repositories, prefer:

1. `dotnet build <solution-or-project>`
2. `dotnet test <solution-or-test-project>`
3. `dotnet format <solution-or-project> --verify-no-changes`

For the Iris repository, prefer the solution-level command when available:

- `dotnet build .\Iris.slnx`
- `dotnet test .\Iris.slnx`
- `dotnet format .\Iris.slnx --verify-no-changes`

If `.slnx` is not supported by the installed SDK or tooling, fall back to `.sln` or project-level verification and state that fallback explicitly.

Do not claim success unless verification was actually run and passed.

After verification, inspect whether tracked files changed:

- `git status --short`
- `git diff --name-status`
- `git diff --stat`

## Forbidden Commands

Do not run:

- `git push`
- `git clean`
- `git reset --hard`
- `rm -rf`
- `docker system prune`
- destructive database commands
- snapshot/golden update commands
- package update commands
- migration apply commands against non-disposable databases

## Output Format

# Implementation Result

## Summary

<what was implemented>

## Files Changed

- `<path>` — <reason>

## Behavior Implemented

- ...

## Tests Added or Updated

- ...

## Documentation / Memory Updates

- ...

## Verification

### Commands Run

```bash
<command>
```

### Result

- Passed:
- Failed:
- Not run:

## Deviations from Plan

No deviations from the approved plan.

## Risks / Follow-ups

No known remaining risks.

## Execution Note

No commits were created.
No remote operations were performed.

## If Implementation Is Blocked

# Implementation Blocked

## Reason

<clear reason>

## What Was Checked

- ...

## Conflict or Risk

- ...

## Safe Next Step

- ...
