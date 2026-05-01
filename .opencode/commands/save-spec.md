---
description: Save an approved engineering specification to the repository
agent: builder
---

# /save-spec

Use the `iris-engineering` skill.
Use the `save-spec` skill.

Save the approved or draft engineering specification for:

$ARGUMENTS

If `$ARGUMENTS` does not contain the actual specification text, a clear source path, or an unambiguous reference to an existing approved/draft specification, do not invent a specification. Report that the source spec is missing.

## Hard Rules

Do not create a new specification from scratch.  
Do not redesign the specification.  
Do not implement anything.  
Do not edit source code.  
Do not modify project configuration.  
Do not change tests.  
Do not change project references.  
Save only an existing approved or draft specification.  
Inspect existing documentation structure before choosing a path.  
Follow existing repository naming conventions if present.  
If no convention exists, save to `docs/specs/`.  
Use a stable lowercase kebab-case filename.  
Prefer filename format: `YYYY-MM-DD-kebab-case-task-name.spec.md`.  
Create the target directory only if needed.  
Do not update indexes unless an existing convention clearly requires it.  
Do not update `.agent` / `.agents` memory unless repository convention requires it or the user explicitly asks.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/save-spec/SKILL.md`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Save Procedure

1. Determine whether `$ARGUMENTS` contains:
   - the full approved/draft specification text;
   - a path to an existing specification file;
   - or an unambiguous reference to an existing approved/draft specification in the repository.
2. If the source specification is missing or ambiguous, stop and return `# Spec Save Failed`.
3. Inspect existing `docs/` conventions before selecting a target path.
4. Prefer an existing spec directory if the repository already uses one.
5. If no convention exists, use `docs/specs/`.
6. Use a stable lowercase kebab-case filename.
7. Prefer `YYYY-MM-DD-kebab-case-task-name.spec.md`.
8. Preserve the approved/draft specification content.
9. Lightly normalize Markdown only when it does not change requirements.
10. Add a title only if missing.
11. Add metadata only if existing specs use metadata.
12. Create only one spec file unless the user explicitly requested otherwise.
13. Do not update `.agent` / `.agents` memory unless required by existing repository convention or explicitly requested.
14. After saving, inspect `git status --short` and verify only the intended documentation file/directory changed.

## Allowed Changes

Allowed:

- create one new spec file;
- create `docs/specs/` if no suitable folder exists;
- lightly normalize Markdown formatting;
- add a title if missing;
- add metadata only if existing specs use metadata.

Not allowed:

- implementation changes;
- source code changes;
- test changes;
- config changes;
- project reference changes;
- unrelated documentation edits;
- changing requirements without explicit user approval;
- silently resolving open questions;
- adding design sections;
- adding implementation plan sections;
- updating `.agent` / `.agents` memory unless explicitly requested or required by repository convention.

## Output Format

# Spec Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the spec file was created.
- Confirmed only intended documentation files changed.
- No implementation files were modified.

## Notes

- ...

## If Saving Cannot Be Completed

# Spec Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Evidence Gap

- ...

## Safe Next Step

- ...
