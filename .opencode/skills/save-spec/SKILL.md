---
name: save-spec
description: Save an approved engineering specification to the repository. Use only after a spec has already been created or provided and the user explicitly asks to save it.
compatibility: opencode
metadata:
  workflow_stage: specification_persistence
  output_type: saved_spec_document
---

# Save Spec Skill

## Purpose

Use this skill to save an approved engineering specification into the repository.

This skill does not create a new specification from scratch.  
It persists an existing specification in the correct location, with a stable filename, correct formatting, and minimal project disruption.

## When to Use

Use this skill only when:

- a specification already exists in the conversation or in a file;
- the user explicitly asks to save the specification;
- the target repository structure is available;
- saving the spec does not require changing implementation code.

Do not use this skill to:

- design the solution;
- create an implementation plan;
- edit source code;
- change project architecture;
- create unrelated documentation;
- rewrite the spec unless the user asks for revision.

## Required Context

Before saving the spec, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Existing documentation structure
4. Existing spec/documentation naming conventions
5. Existing `docs/`, `.agents/`, or project planning folders
6. `.agents/overview.md` if present
7. `.agents/PROJECT_LOG.md` if present

Before creating a new folder or file, check whether a suitable location already exists.

## Default Save Location

Prefer existing project conventions.

If no convention exists, use:

```text
docs/specs/

Default filename format:

YYYY-MM-DD-kebab-case-task-name.spec.md

Example:

docs/specs/2026-04-30-sqlite-conversation-persistence.spec.md

If the repository already uses another convention, follow the existing convention.

##File Naming Rules

The filename must be:

lowercase;
kebab-case;
descriptive;
stable;
without spaces;
without vague names like spec.md, new-spec.md, feature.md;
prefixed with date unless project convention says otherwise.

Good examples:

2026-04-30-ollama-model-gateway.spec.md
2026-04-30-sqlite-conversation-persistence.spec.md
2026-04-30-chat-viewmodel-first-slice.spec.md

Bad examples:

spec.md
final.md
new-feature.md
task.md
documentation.md

##Save Procedure

Follow this sequence:

Identify the spec to save.
Verify that it is a specification, not a design or implementation plan.
Inspect existing documentation/spec folders.
Select the correct save location.
Select a stable filename.
Create the directory only if needed.
Save the spec with clean Markdown formatting.
Do not modify source code.
Do not modify unrelated docs.
Report the saved path.

##Allowed Edits

Allowed:

create one new spec file;
create docs/specs/ if no suitable spec folder exists;
lightly normalize Markdown formatting;
add a title if the spec lacks one;
add a short metadata block if the project convention uses metadata.

Not allowed:

changing implementation files;
rewriting architecture;
changing requirements;
adding design sections;
adding implementation plan sections;
changing acceptance criteria without explicit user approval;
editing multiple unrelated docs;
modifying project configuration;
changing .csproj, .sln, .slnx, package files, migrations, or source code.

##Optional Metadata

If the project uses metadata blocks, add one at the top.

Default metadata format:

---
title: "<Spec Title>"
status: draft
created: YYYY-MM-DD
type: specification
---

Do not add metadata if existing project specs do not use it.

##Spec Integrity Rules

When saving the spec:

preserve the approved meaning;
preserve scope;
preserve non-goals;
preserve acceptance criteria;
preserve open questions;
do not silently resolve open questions;
do not silently expand scope;
do not add implementation details.

If the spec appears incomplete, save it as draft and state what is missing.

If the spec contains mixed design or plan content, either:

preserve it if the user explicitly approved that text;
or ask whether to split it before saving if the split is blocking.

##Documentation Index Rules

If the repository has an existing spec index, such as:

docs/specs/README.md
docs/README.md
docs/index.md

then update the index only if existing convention clearly requires it.

Do not create a new index unless explicitly asked.

##Agent Memory Rules

If .agents/PROJECT_LOG.md exists, update it only when project convention requires documentation changes to be logged.

If updating project memory, add a minimal entry:

## YYYY-MM-DD — Saved specification

### Changed
- Saved `<spec title>` to `<path>`.

### Verified
- Documentation-only change.

Do not update memory files if the user only asked to save the spec and the repository convention does not require memory updates.

##Verification

For documentation-only save, verification is usually:

confirm file exists;
confirm path;
confirm Markdown was saved;
optionally run no build/test unless project convention requires docs checks.

Do not run dotnet build or dotnet test for a pure documentation save unless explicitly requested.

##Output Format

After saving, respond with:

# Spec Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Notes

- ...

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the spec file was created.
- No implementation files were modified.

##Failure Handling

If saving cannot be completed, report:

# Spec Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...

Do not claim the spec was saved unless the file was actually written.

##Final Response Requirements

Final response must include:

saved path;
file status;
list of modified files;
whether implementation files were untouched;
whether any project memory or index files were updated.