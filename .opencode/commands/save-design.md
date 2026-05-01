---
description: Save an approved architecture design to the repository
agent: builder
---

# /save-design

Use the `iris-engineering` skill.
Use the `save-design` skill.

Save the approved architecture design for:

$ARGUMENTS

If `$ARGUMENTS` is empty, stop and report that the design source or task name is missing.

## Hard Rules

Do not create a new design from scratch.  
Do not redesign the solution.  
Do not implement anything.  
Do not edit source code.  
Do not modify project configuration.  
Do not change tests.  
Do not modify unrelated documentation.  
Do not update snapshots or golden outputs.  
Do not update `.agent` / `.agents` memory unless repository convention clearly requires it or the user explicitly asks.  
Do not run destructive commands.

Save only an existing approved or draft architecture design supplied in the conversation, referenced by path, or already present in the repository.

If the approved/draft design content is not available, do not invent it. Report the evidence gap.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/save-design/SKILL.md`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Required Behavior

Before saving, inspect:

- `AGENTS.md` if present;
- `.opencode/skills/save-design/SKILL.md` if present;
- relevant `.opencode/rules/*.md` if present;
- existing `docs/` structure;
- existing design/documentation naming conventions;
- `.agent/overview.md` or `.agents/overview.md` if present;
- `.agent/PROJECT_LOG.md` or `.agents/PROJECT_LOG.md` if present;
- current git status and changed files.

Choose the save path by repository convention:

1. If existing architecture designs use a clear folder and naming convention, follow it.
2. If no convention exists, save to `docs/designs/`.
3. Use a stable lowercase kebab-case filename.
4. Prefer filename format: `YYYY-MM-DD-kebab-case-task-name.design.md`.
5. Create the target directory only if needed.
6. Do not update indexes unless an existing convention clearly requires it.

## Allowed Changes

Allowed:

- create one new design file;
- create `docs/designs/` if no suitable folder exists;
- lightly normalize Markdown formatting;
- add a title if missing;
- add metadata only if existing designs use metadata.

Not allowed:

- implementation changes;
- source code changes;
- test changes;
- config changes;
- project reference changes;
- unrelated documentation edits;
- changing architecture decisions without explicit user approval;
- silently resolving open questions;
- adding spec sections;
- adding implementation-plan sections;
- updating memory unless explicitly requested or clearly required by repository convention.

## Post-Save Verification

After saving, verify:

- the target file exists;
- only the expected design file and, if needed, its target directory were created;
- no source, test, config, or project files were modified;
- `git status --short` matches the expected documentation-only change.

Use read-only inspection commands for verification.

## Output Format

# Design Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the design file was created.
- No implementation files were modified.

## Notes

- ...

## Execution Note

No implementation was performed.
No source files were modified.

## If Saving Cannot Be Completed

# Design Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
