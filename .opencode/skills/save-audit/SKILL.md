---
name: save-audit
description: Save an approved audit report to the repository. Use only after an audit report has already been created or provided and the user explicitly asks to save it.
compatibility: opencode
metadata:
  workflow_stage: audit_persistence
  output_type: saved_audit_report
---

# Save Audit Skill

## Purpose

Use this skill to save an approved audit report into the repository.

This skill does not create a new audit from scratch.  
It persists an existing audit report in the correct location, with a stable filename, clean Markdown formatting, and minimal project disruption.

## When to Use

Use this skill only when:

- an audit report already exists in the conversation or in a file;
- the user explicitly asks to save the audit;
- the target repository structure is available;
- saving the audit does not require changing implementation code.

Do not use this skill to:

- create a specification;
- create an architecture design;
- create an implementation plan;
- edit source code;
- fix audit findings;
- change project architecture;
- create unrelated documentation;
- rewrite the audit unless the user asks for revision.

## Required Context

Before saving the audit, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Existing documentation structure
4. Existing audit/report naming conventions
5. Existing `docs/`, `.agents/`, or project planning folders
6. `.agents/overview.md` if present
7. `.agents/PROJECT_LOG.md` if present

Before creating a new folder or file, check whether a suitable location already exists.

## Default Save Location

Prefer existing project conventions.

If no convention exists, use:

```text
docs/audits/
````

Default filename format:

```text
YYYY-MM-DD-kebab-case-task-name.audit.md
```

Example:

```text
docs/audits/2026-04-30-sqlite-conversation-persistence.audit.md
```

If the repository already uses another convention, follow the existing convention.

## File Naming Rules

The filename must be:

* lowercase;
* kebab-case;
* descriptive;
* stable;
* without spaces;
* without vague names like `audit.md`, `review.md`, `report.md`, `final.md`;
* prefixed with date unless project convention says otherwise.

Good examples:

```text
2026-04-30-ollama-model-gateway.audit.md
2026-04-30-sqlite-conversation-persistence.audit.md
2026-04-30-chat-viewmodel-first-slice.audit.md
```

Bad examples:

```text
audit.md
review.md
final.md
report.md
notes.md
task.md
```

## Save Procedure

Follow this sequence:

1. Identify the audit report to save.
2. Verify that it is an audit report, not a spec, design, or implementation plan.
3. Inspect existing documentation/audit folders.
4. Select the correct save location.
5. Select a stable filename.
6. Create the directory only if needed.
7. Save the audit with clean Markdown formatting.
8. Do not modify source code.
9. Do not fix audit findings.
10. Do not modify unrelated docs.
11. Report the saved path.

## Allowed Edits

Allowed:

* create one new audit file;
* create `docs/audits/` if no suitable audit folder exists;
* lightly normalize Markdown formatting;
* add a title if the audit lacks one;
* add a short metadata block if the project convention uses metadata.

Not allowed:

* changing implementation files;
* fixing audit findings;
* rewriting the audit meaning;
* changing severity classification without explicit user approval;
* adding or removing findings without explicit user approval;
* changing verification evidence;
* adding specification sections;
* adding design sections;
* adding implementation plan sections;
* editing multiple unrelated docs;
* modifying project configuration;
* changing `.csproj`, `.sln`, `.slnx`, package files, migrations, or source code.

## Optional Metadata

If the project uses metadata blocks, add one at the top.

Default metadata format:

```md
---
title: "<Audit Title>"
status: draft
created: YYYY-MM-DD
type: audit
---
```

Do not add metadata if existing project audits do not use it.

## Audit Integrity Rules

When saving the audit:

* preserve the approved meaning;
* preserve audit status;
* preserve merge/readiness decision;
* preserve all P0/P1/P2 findings;
* preserve evidence;
* preserve impact descriptions;
* preserve recommended fixes;
* preserve verification results;
* preserve verification gaps;
* preserve final decision;
* do not silently resolve findings;
* do not silently downgrade severity;
* do not silently remove uncomfortable issues;
* do not execute fixes.

If the audit appears incomplete, save it as draft and state what is missing.

If the audit contains mixed spec/design/plan content, either:

* preserve it if the user explicitly approved that text;
* or ask whether to split it before saving if the split is blocking.

## Documentation Index Rules

If the repository has an existing audit index, such as:

```text
docs/audits/README.md
docs/README.md
docs/index.md
```

then update the index only if existing convention clearly requires it.

Do not create a new index unless explicitly asked.

## Agent Memory Rules

If `.agents/PROJECT_LOG.md` exists, update it only when project convention requires documentation changes to be logged.

If updating project memory, add a minimal entry:

```md
## YYYY-MM-DD — Saved audit report

### Changed
- Saved `<audit title>` to `<path>`.

### Verified
- Documentation-only change.
```

If `.agent/log_notes.md` exists and the saved audit contains unresolved P0/P1 issues, add those issues only if project convention requires local tracking. Use `local_notes.md` only as an existing fallback.

Do not update memory files if the user only asked to save the audit and the repository convention does not require memory updates.

## Verification

For documentation-only save, verification is usually:

* confirm file exists;
* confirm path;
* confirm Markdown was saved;
* optionally run no build/test unless project convention requires docs checks.

Do not run `dotnet build` or `dotnet test` for a pure documentation save unless explicitly requested.

## Output Format

After saving, respond with:

```md
# Audit Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Notes

- ...

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the audit file was created.
- No implementation files were modified.
- No audit findings were fixed.
```

## Failure Handling

If saving cannot be completed, report:

```md
# Audit Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
```

Do not claim the audit was saved unless the file was actually written.

## Final Response Requirements

Final response must include:

* saved path;
* file status;
* list of modified files;
* whether implementation files were untouched;
* whether any audit findings were changed;
* whether any project memory or index files were updated;
* explicit statement that no fixes were implemented.

```
