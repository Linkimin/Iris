---
description: Save an approved audit report to the repository
agent: builder
---

# /save-audit

Use the `iris-engineering` skill.
Use the `save-audit` skill.

Save the approved audit report for:

$ARGUMENTS

If `$ARGUMENTS` does not contain or identify an existing draft/approved audit report, do not create a new audit. Report that the audit source is missing.

## Hard Rules

Do not create a new audit report from scratch.  
Do not rewrite the audit.  
Do not change audit findings.  
Do not change severity classification.  
Do not resolve or remove findings.  
Do not implement fixes.  
Do not edit source code.  
Do not modify project configuration.  
Do not change tests.  
Do not execute recommended fixes.  
Do not update snapshots.  
Do not accept golden outputs.  
Do not run destructive commands.  
Do not store secrets, credentials, tokens, keys, production configs, or real customer data.

Save only an existing approved or draft audit report.

Inspect the repository documentation structure before choosing a path.  
Follow existing repository naming conventions if present.  
If no convention exists, save to `docs/audits/`.  
Use a stable lowercase kebab-case filename.  
Prefer filename format: `YYYY-MM-DD-kebab-case-task-name.audit.md`.

Create the target directory only if needed.

Do not update indexes unless an existing convention clearly requires it.  
Do not update `.agent` / `.agents` memory unless repository convention requires it or the user asks.

If repository context is missing, empty, or points to the wrong directory, stop and report the evidence gap.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/save-audit/SKILL.md`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Save Behavior

When saving the audit:

1. Identify the existing approved or draft audit report from `$ARGUMENTS` or the current conversation context.
2. Inspect existing `docs/` conventions.
3. Choose the most consistent audit/report directory.
4. If no suitable convention exists, use `docs/audits/`.
5. Use lowercase kebab-case.
6. Prefer `YYYY-MM-DD-kebab-case-task-name.audit.md`.
7. Preserve audit findings and severity classifications exactly.
8. Lightly normalize Markdown only when it does not alter meaning.
9. Add a title only if missing.
10. Add metadata only if existing audits use metadata.
11. Save exactly one audit file unless repository convention clearly requires otherwise.
12. After saving, inspect `git status --short` and report the created/modified file.

## Allowed Changes

Allowed:

- create one new audit file;
- create `docs/audits/` if no suitable folder exists;
- lightly normalize Markdown formatting;
- add a title if missing;
- add metadata only if existing audits use metadata.

Not allowed:

- implementation changes;
- source code changes;
- test changes;
- config changes;
- project reference changes;
- unrelated documentation edits;
- changing findings without explicit user approval;
- changing severities without explicit user approval;
- silently resolving open issues;
- adding spec, design, or plan sections;
- executing recommended fixes;
- updating `.agent` / `.agents` memory unless explicitly requested or required by repository convention.

## Output Format

After saving, respond with:

```md
# Audit Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the audit file was created.
- No implementation files were modified.
- No audit findings were changed.
- No fixes were implemented.

## Notes

- ...
```

If saving cannot be completed, respond with:

```md
# Audit Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
```
