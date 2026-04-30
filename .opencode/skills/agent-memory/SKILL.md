
---
name: agent-memory
description: Maintain project agent memory files after meaningful work. Use to update PROJECT_LOG, overview, log_notes, mem_library, checkpoints, and durable project state without changing source behavior.
compatibility: opencode
metadata:
  workflow_stage: project_memory
  output_type: memory_update
---

# Agent Memory Skill

## Purpose

Use this skill to maintain project memory files that help future agent sessions continue work without losing context.

Agent memory is not product documentation.  
It is an operational continuity layer for agents and maintainers.

This skill must preserve factual project state, current phase, decisions, known issues, verification results, and next safe steps.

## When to Use

Use this skill when:

- meaningful implementation work was completed;
- spec/design/plan/audit documents were saved;
- project status changed;
- verification was run;
- a bug, risk, or blocker was discovered;
- architecture decision was made;
- a phase or checkpoint was completed;
- the user explicitly asks to update agent memory;
- the next session needs durable continuation context.

Do not use this skill for:

- trivial typo fixes;
- purely conversational discussion;
- speculative ideas not accepted by the user;
- private chain-of-thought;
- sensitive secrets;
- unverified claims;
- duplicating full specs/designs/plans into memory.

## Required Context

Before updating memory, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Existing `.agent/` directory, falling back to `.agents/` only if `.agent` is absent
4. `<agentDir>/PROJECT_LOG.md` if present
5. `<agentDir>/overview.md` if present
6. `<agentDir>/log_notes.md`, falling back to `local_notes.md` only if it already exists
7. `<agentDir>/mem_library/**` if present and relevant
8. Current git status if available
9. Recently changed files
10. Verification results if available
11. Relevant saved spec/design/plan/audit paths

Before creating a memory file, check whether a suitable existing file already exists.

## Default Memory Structure

Prefer existing project conventions.

If no convention exists, use:

```text
.agents/
  PROJECT_LOG.md
  overview.md
  log_notes.md
  mem_library/
````

Do not create the full structure unless needed.

## File Responsibilities

### `.agents/PROJECT_LOG.md`

Chronological project activity log.

Use for:

* completed iterations;
* saved artifacts;
* implementation changes;
* verification results;
* audit results;
* meaningful decisions;
* phase transitions.

Entries must be prepended, newest first.

### `.agents/overview.md`

Current project state summary.

Use for:

* active phase;
* current goal;
* completed milestones;
* next safe step;
* current blockers;
* last verification result;
* key architecture constraints;
* known working assumptions.

This file should remain compact and current.

### `<agentDir>/log_notes.md`

Working notes and unresolved issues.

Use for:

* discovered bugs;
* risks;
* blockers;
* TODOs;
* warnings;
* suspicious architecture drift;
* skipped verification;
* environment problems;
* follow-up tasks.

Do not use it as a random scratchpad.

### `.agents/mem_library/**`

Durable semantic memory.

Use for:

* stable architectural decisions;
* project identity;
* long-term constraints;
* recurring workflow rules;
* domain knowledge;
* accepted conventions;
* roadmaps.

Do not store transient task notes here.

## Core Rules

Hard rules live in `.opencode/rules/memory.md`.

### 1. Facts Only

Record factual, traceable entries. Never: "probably works", "should be fine", "architecture is perfect". Store artifact paths, status, key decisions, unresolved risks. Do not paste full specs/designs/plans into memory.

### 2. Keep Memory Compact and Chronological

`PROJECT_LOG.md` is newest-first. `overview.md` stays current. `log_notes.md` tracks unresolved issues. `mem_library/` stores stable product meaning only. Route each fact to exactly one primary file.

### 3. Security and Privacy

Never store: API keys, tokens, credentials, private keys, production connection strings, personal data, real customer data, raw prompts containing private content. Use `<REDACTED>`, `<API_KEY>`, `<CONNECTION_STRING>` placeholders.

### 4. Minimal Updates, Honest Verification

Only update files relevant to the change. If verification was run, record exact commands and result. If not run, state "Verification not run." Do not imply success.

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

If implementation was performed, include affected area:

```md
### Files / Areas
- `src/...`
- `tests/...`
```

If verification failed:

```md
### Verification
- `dotnet test` failed: <short reason>
```

### overview.md Format

```md
# Agent Overview

## Project

<One-paragraph project identity.>

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

### log_notes.md Format

```md
# Log Notes

## Open Issues

### P1 — <Issue Title>

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

### mem_library Entry Format

Use only when the decision is durable.

```md
# <Memory Topic>

## Summary

<Stable project knowledge.>

## Decision / Constraint

- ...

## Rationale

- ...

## Applies To

- ...

## Last Updated

YYYY-MM-DD
```

## Update Procedure

Follow this sequence:

1. Inspect existing memory structure.
2. Identify what changed or needs persistence.
3. Decide which memory files require updates.
4. Preserve existing format.
5. Apply minimal edits.
6. Verify the files were updated.
7. Report modified memory files.

## Allowed Edits

Allowed:

* append/prepend a `PROJECT_LOG.md` entry;
* update current status in `overview.md`;
* add/update unresolved issue in `log_notes.md`;
* add/update durable project decision in `mem_library/**`;
* create missing memory file only if needed;
* lightly normalize formatting in edited section.

Not allowed:

* changing source code;
* changing implementation behavior;
* rewriting unrelated memory history;
* deleting old entries without explicit request;
* storing secrets;
* storing private reasoning;
* inventing completed work;
* claiming verification that did not happen;
* turning memory into full documentation dump.

## Output Format

After updating memory, respond with:

```md
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
```

## Failure Handling

If memory cannot be updated, respond with:

```md
# Agent Memory Update Failed

## Reason

<reason>

## What Was Checked

- ...

## Safe Next Step

- ...
```

Do not claim memory was updated unless files were actually written.

## Quality Checklist

Before finalizing, verify:

* [ ] Existing memory structure was inspected.
* [ ] Only relevant memory files were changed.
* [ ] Facts were recorded, not guesses.
* [ ] `PROJECT_LOG.md` remains newest-first.
* [ ] `overview.md` reflects current state if updated.
* [ ] unresolved issues are visible in `log_notes.md` if relevant.
* [ ] Durable decisions only went to `mem_library`.
* [ ] No secrets were stored.
* [ ] No private reasoning was stored.
* [ ] Verification results are honest.
* [ ] Modified files are listed.

## Anti-Patterns

Avoid:
- updating all memory files every time;
- dumping full specs/designs/plans into memory;
- vague entries like "worked on stuff";
- recording unverified success or hiding unresolved issues;
- overwriting history or storing sensitive data;
- storing private reasoning;
- duplicating docs in memory;
- mixing transient notes into durable memory.

## Final Response Requirements

When using this skill, final response must include:

1. files modified;
2. what was recorded;
3. whether implementation files were untouched;
4. whether verification results were recorded;
5. any unresolved issues added or updated.

```

