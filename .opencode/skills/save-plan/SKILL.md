---
name: save-plan
description: Save an approved implementation plan to the repository. Use only after a plan has already been created or provided and the user explicitly asks to save it.
compatibility: opencode
metadata:
  workflow_stage: implementation_plan_persistence
  output_type: saved_implementation_plan_document
---

# Save Plan Skill

## Purpose

Use this skill to save an approved implementation plan into the repository.

This skill does not create a new implementation plan from scratch.  
It persists an existing plan in the correct location, with a stable filename, clean Markdown formatting, and minimal project disruption.

## When to Use

Use this skill only when:

- an implementation plan already exists in the conversation or in a file;
- the user explicitly asks to save the plan;
- the target repository structure is available;
- saving the plan does not require changing implementation code.

Do not use this skill to:

- create a specification;
- create an architecture design;
- edit source code;
- change project architecture;
- create unrelated documentation;
- rewrite the plan unless the user asks for revision;
- implement the plan.

## Required Context

Before saving the plan, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Existing documentation structure
4. Existing plan/documentation naming conventions
5. Existing `docs/`, `.agents/`, or project planning folders
6. `.agents/overview.md` if present
7. `.agents/PROJECT_LOG.md` if present

Before creating a new folder or file, check whether a suitable location already exists.

## Default Save Location

Prefer existing project conventions.

If no convention exists, use:

```text
docs/plans/
````

Default filename format:

```text
YYYY-MM-DD-kebab-case-task-name.plan.md
```

Example:

```text
docs/plans/2026-04-30-sqlite-conversation-persistence.plan.md
```

If the repository already uses another convention, follow the existing convention.

## File Naming Rules

The filename must be:

* lowercase;
* kebab-case;
* descriptive;
* stable;
* without spaces;
* without vague names like `plan.md`, `new-plan.md`, `implementation.md`, `tasks.md`;
* prefixed with date unless project convention says otherwise.

Good examples:

```text
2026-04-30-ollama-model-gateway.plan.md
2026-04-30-sqlite-conversation-persistence.plan.md
2026-04-30-chat-viewmodel-first-slice.plan.md
```

Bad examples:

```text
plan.md
final.md
new-feature.md
implementation.md
tasks.md
todo.md
```

## Save Procedure

Follow this sequence:

1. Identify the plan to save.
2. Verify that it is an implementation plan, not a specification or architecture design.
3. Inspect existing documentation/plan folders.
4. Select the correct save location.
5. Select a stable filename.
6. Create the directory only if needed.
7. Save the plan with clean Markdown formatting.
8. Do not modify source code.
9. Do not modify unrelated docs.
10. Report the saved path.

## Allowed Edits

Allowed:

* create one new plan file;
* create `docs/plans/` if no suitable plan folder exists;
* lightly normalize Markdown formatting;
* add a title if the plan lacks one;
* add a short metadata block if the project convention uses metadata.

Not allowed:

* changing implementation files;
* executing implementation steps;
* rewriting the plan meaning;
* changing requirements;
* changing architecture decisions;
* adding specification sections;
* adding architecture design sections;
* changing phase order without explicit user approval;
* changing acceptance or verification expectations without explicit user approval;
* editing multiple unrelated docs;
* modifying project configuration;
* changing `.csproj`, `.sln`, `.slnx`, package files, migrations, or source code.

## Optional Metadata

If the project uses metadata blocks, add one at the top.

Default metadata format:

```md
---
title: "<Plan Title>"
status: draft
created: YYYY-MM-DD
type: implementation-plan
---
```

Do not add metadata if existing project plans do not use it.

## Plan Integrity Rules

When saving the plan:

* preserve the approved meaning;
* preserve phase ordering;
* preserve scope boundaries;
* preserve forbidden changes;
* preserve files-to-inspect guidance;
* preserve files-likely-to-edit guidance;
* preserve verification commands;
* preserve rollback notes;
* preserve risks and mitigations;
* preserve open questions;
* do not silently resolve open questions;
* do not silently expand scope;
* do not add implementation code;
* do not execute the plan.

If the plan appears incomplete, save it as draft and state what is missing.

If the plan contains mixed specification or design content, either:

* preserve it if the user explicitly approved that text;
* or ask whether to split it before saving if the split is blocking.

## Documentation Index Rules

If the repository has an existing plan index, such as:

```text
docs/plans/README.md
docs/README.md
docs/index.md
```

then update the index only if existing convention clearly requires it.

Do not create a new index unless explicitly asked.

## Agent Memory Rules

If `.agents/PROJECT_LOG.md` exists, update it only when project convention requires documentation changes to be logged.

If updating project memory, add a minimal entry:

```md
## YYYY-MM-DD — Saved implementation plan

### Changed
- Saved `<plan title>` to `<path>`.

### Verified
- Documentation-only change.
```

Do not update memory files if the user only asked to save the plan and the repository convention does not require memory updates.

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
# Plan Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Notes

- ...

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the plan file was created.
- No implementation files were modified.
```

## Failure Handling

If saving cannot be completed, report:

```md
# Plan Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
```

Do not claim the plan was saved unless the file was actually written.

## Final Response Requirements

Final response must include:

* saved path;
* file status;
* list of modified files;
* whether implementation files were untouched;
* whether any project memory or index files were updated;
* explicit statement that the plan was not executed.

```

