---
name: save-design
description: Save an approved architecture design to the repository. Use only after a design has already been created or provided and the user explicitly asks to save it.
compatibility: opencode
metadata:
  workflow_stage: design_persistence
  output_type: saved_design_document
---

# Save Design Skill

## Purpose

Use this skill to save an approved architecture design into the repository.

This skill does not create a new design from scratch.  
It persists an existing design in the correct location, with a stable filename, correct formatting, and minimal project disruption.

## When to Use

Use this skill only when:

- a design already exists in the conversation or in a file;
- the user explicitly asks to save the design;
- the target repository structure is available;
- saving the design does not require changing implementation code.

Do not use this skill to:

- create a specification;
- create an implementation plan;
- edit source code;
- change project architecture;
- create unrelated documentation;
- rewrite the design unless the user asks for revision.

## Required Context

Before saving the design, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Existing documentation structure
4. Existing design/documentation naming conventions
5. Existing `docs/`, `.agents/`, or project planning folders
6. `.agents/overview.md` if present
7. `.agents/PROJECT_LOG.md` if present

Before creating a new folder or file, check whether a suitable location already exists.

## Default Save Location

Prefer existing project conventions.

If no convention exists, use:

```text
docs/designs/

Default filename format:

YYYY-MM-DD-kebab-case-task-name.design.md

Example:

docs/designs/2026-04-30-sqlite-conversation-persistence.design.md

If the repository already uses another convention, follow the existing convention.

##File Naming Rules

The filename must be:

lowercase;
kebab-case;
descriptive;
stable;
without spaces;
without vague names like design.md, new-design.md, architecture.md;
prefixed with date unless project convention says otherwise.

Good examples:

2026-04-30-ollama-model-gateway.design.md
2026-04-30-sqlite-conversation-persistence.design.md
2026-04-30-chat-viewmodel-first-slice.design.md

Bad examples:

design.md
final.md
new-feature.md
architecture.md
task.md
documentation.md

##Save Procedure

Follow this sequence:

Identify the design to save.
Verify that it is an architecture design, not a specification or implementation plan.
Inspect existing documentation/design folders.
Select the correct save location.
Select a stable filename.
Create the directory only if needed.
Save the design with clean Markdown formatting.
Do not modify source code.
Do not modify unrelated docs.
Report the saved path.

##Allowed Edits

Allowed:

create one new design file;
create docs/designs/ if no suitable design folder exists;
lightly normalize Markdown formatting;
add a title if the design lacks one;
add a short metadata block if the project convention uses metadata.

Not allowed:

changing implementation files;
rewriting the design meaning;
changing requirements;
adding specification sections;
adding implementation plan sections;
changing contracts without explicit user approval;
editing multiple unrelated docs;
modifying project configuration;
changing .csproj, .sln, .slnx, package files, migrations, or source code.

##Optional Metadata

If the project uses metadata blocks, add one at the top.

Default metadata format:

---
title: "<Design Title>"
status: draft
created: YYYY-MM-DD
type: design
---

Do not add metadata if existing project designs do not use it.

##Design Integrity Rules

When saving the design:

preserve the approved meaning;
preserve architecture decisions;
preserve responsibility ownership;
preserve contract definitions;
preserve data flow;
preserve failure handling;
preserve risks and trade-offs;
preserve open questions;
do not silently resolve open questions;
do not silently expand scope;
do not add implementation sequencing.

If the design appears incomplete, save it as draft and state what is missing.

If the design contains mixed specification or implementation-plan content, either:

preserve it if the user explicitly approved that text;
or ask whether to split it before saving if the split is blocking.

##Documentation Index Rules

If the repository has an existing design index, such as:

docs/designs/README.md
docs/README.md
docs/index.md

then update the index only if existing convention clearly requires it.

Do not create a new index unless explicitly asked.

##Agent Memory Rules

If .agents/PROJECT_LOG.md exists, update it only when project convention requires documentation changes to be logged.

If updating project memory, add a minimal entry:

## YYYY-MM-DD — Saved design

### Changed
- Saved `<design title>` to `<path>`.

### Verified
- Documentation-only change.

Do not update memory files if the user only asked to save the design and the repository convention does not require memory updates.

##Verification

For documentation-only save, verification is usually:

confirm file exists;
confirm path;
confirm Markdown was saved;
optionally run no build/test unless project convention requires docs checks.

Do not run dotnet build or dotnet test for a pure documentation save unless explicitly requested.

##Output Format

After saving, respond with:

# Design Saved

## Path

`<saved-path>`

## Status

Saved as `<draft|approved|final>`.

## Notes

- ...

## Files Modified

- `<saved-path>`

## Verification

- Confirmed the design file was created.
- No implementation files were modified.
Failure Handling

If saving cannot be completed, report:

# Design Save Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...

Do not claim the design was saved unless the file was actually written.

##Final Response Requirements

Final response must include:

saved path;
file status;
list of modified files;
whether implementation files were untouched;
whether any project memory or index files were updated.