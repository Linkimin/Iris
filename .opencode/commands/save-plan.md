---
description: Save an approved implementation plan to the repository
agent: builder
---

# /save-plan

Use the `iris-engineering` skill.
Use the `save-plan` skill.

Save the approved implementation plan for:

$ARGUMENTS

## Hard Rules

Do not create a new implementation plan from scratch.  
Do not redesign the solution.  
Do not rewrite the specification.  
Do not implement anything.  
Do not execute the plan.  
Do not edit source code.  
Do not edit tests.  
Do not modify project configuration.  
Do not change project references.  
Do not update snapshots.  
Do not accept golden outputs.  
Do not update `.agent` / `.agents` memory unless repository convention clearly requires it or the user explicitly asks.  
Do not run destructive commands.

Save only an existing approved or draft implementation plan provided in the prompt, referenced by path, or clearly present in the repository context.

If no approved or draft implementation plan content is available, stop and report that the save cannot be completed.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/save-plan/SKILL.md`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Save Rules

Before saving:

1. Inspect the repository documentation structure.
2. Identify existing implementation-plan naming conventions.
3. Use the existing convention if it is clear.
4. If no convention exists, save to `docs/plans/`.
5. Use a stable lowercase kebab-case filename.
6. Prefer filename format:

```text
YYYY-MM-DD-kebab-case-task-name.plan.md
```

Create the target directory only if needed.

Do not update documentation indexes unless an existing convention clearly requires it.

Do not update `.agent` / `.agents` memory unless repository convention clearly requires it or the user explicitly asks.

## Allowed Changes

Allowed:

- create one new plan file;
- create `docs/plans/` if no suitable folder exists;
- lightly normalize Markdown formatting;
- add a title if missing;
- add metadata only if existing plans use metadata.

Not allowed:

- implementation changes;
- source code changes;
- test changes;
- config changes;
- project reference changes;
- unrelated documentation edits;
- changing requirements without explicit user approval;
- changing architecture decisions without explicit user approval;
- changing phase order without explicit user approval;
- silently resolving open questions;
- adding specification sections;
- adding architecture design sections;
- executing implementation steps.

## Content Rules

Preserve the approved or draft plan content.

Allowed normalization is limited to:

- consistent Markdown headings;
- stable title if missing;
- metadata only if the repository already uses metadata;
- trimming obvious prompt wrappers that are not part of the plan;
- fixing broken Markdown fences only when the intended content is unambiguous.

Do not add new requirements, phases, checkpoints, tasks, acceptance criteria, or architectural decisions.

If the provided content contains unresolved open questions, preserve them as open questions.

## Output Format

# Plan Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the plan file was created.
- No implementation files were modified.
- The plan was not executed.

## Notes

- ...

## If Saving Cannot Be Completed

# Plan Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
