---
name: iris-memory
description: Use when Iris project memory must be read, updated, reconciled, or protected from accidental writes during reviews, verification, planning, or implementation.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: memory
  output_type: memory_guidance
---

# Iris Memory Skill

## Purpose

Use this skill to read or update Iris agent memory deliberately.

Project memory is not a scratchpad. It records factual state that future agents need.

Authoritative hard rules live in `.opencode/rules/memory.md`.

## When To Use

Use this skill when:

- the user asks to update memory;
- a meaningful implementation or planning iteration completed;
- a bug, blocker, or verification failure must be recorded;
- the active phase or next step changed;
- a durable product/architecture decision was made.

Do not use it during read-only review, verification, audit, spec, design, or plan unless the user explicitly asks for memory updates.

## Memory Mode Decision Table

| Situation | Mode | Allowed writes |
|---|---|---|
| Need current phase | Read memory | None |
| Need product meaning | Read `mem_library` | None |
| Review/audit asks "is memory stale?" | Read/check | None |
| Completed implementation | Update memory | `PROJECT_LOG`, maybe `overview`, `log_notes`, debt |
| Verification failed | Update memory if meaningful | `log_notes`, maybe `PROJECT_LOG` |
| New technical debt accepted | Update memory | `debt_tech_backlog` |
| User says "save this decision" | Update memory | appropriate memory file |
| User asks only to save spec/design/plan | Artifact write | memory only if explicitly requested |

If mode is not clear, default to read-only.

## Canonical Paths

For Iris:

- `.agent` is canonical.
- `.agents` is fallback only when `.agent` does not exist.
- `PROJECT_LOG.md` records completed iterations.
- `overview.md` records current phase/status/next step.
- `log_notes.md` records bugs, failures, suspicious behavior, and unresolved notes.
- `local_notes.md` is fallback only if it already exists.
- `debt_tech_backlog.md` records deferred debt.
- `mem_library/**` stores stable product meaning, not task logs.

Do not create `.agents` when `.agent` exists.

Do not create `local_notes.md` while `.agent/log_notes.md` exists.

## Memory Directory Resolution

Use this precedence:

1. `.agent` if it exists.
2. `.agents` only if `.agent` does not exist.
3. Stop and report if neither exists and a memory update is required.

When reading, mention which directory was used.

When writing, never create the fallback directory if the canonical directory exists.

## Reading Memory

Before asking the user about project state, inspect relevant memory:

- `overview.md` for active phase;
- `architecture.md` for boundary rules;
- `first-vertical-slice.md` for chat-slice decisions;
- `PROJECT_LOG.md` for recent completed work;
- `log_notes.md` for known problems;
- `debt_tech_backlog.md` for deferred debt;
- `mem_library/**` for durable product context.

Read only what is relevant. Do not flood context with the whole memory library unless the task requires it.

## Read Scopes

Use minimal read scopes:

- Status: `overview.md`, recent `PROJECT_LOG.md`, recent `log_notes.md`.
- Architecture: `architecture.md`, relevant `mem_library`, recent decisions in `PROJECT_LOG.md`.
- First vertical slice: `first-vertical-slice.md`, `overview.md`, recent Phase logs.
- Product direction: relevant `mem_library` files, not task logs.
- Failure investigation: `log_notes.md`, recent `PROJECT_LOG.md`, changed files.

If memory conflicts with the repo, trust the repo for current file facts and record/update memory after resolution.

## Write Policy

Allowed memory-writing workflows:

- `/update-memory`;
- explicit user request to update memory;
- an implementation workflow after meaningful completed work, when project convention requires it.

Read-only memory workflows:

- `/status`;
- `/spec`;
- `/design`;
- `/plan`;
- `/verify`;
- `/review`;
- `/architecture-review`;
- `/audit`.

These may inspect memory, but must not update it without explicit user direction.

## Write Routing

Route facts to exactly one primary file:

| Fact | Primary file |
|---|---|
| Completed iteration | `PROJECT_LOG.md` |
| Current phase/status/next step | `overview.md` |
| Failed command or unresolved problem | `log_notes.md` |
| Deferred debt or missing future work | `debt_tech_backlog.md` |
| Stable product/architecture meaning | `mem_library/**` |

Do not duplicate the same paragraph across files. Link concepts by short references instead.

## PROJECT_LOG.md

Record completed meaningful iterations.

Use dated append-only entries:

```markdown
## YYYY-MM-DD — Short title

### Changed
- ...

### Files
- ...

### Validation
- ...

### Next
- ...
```

Do not rewrite the whole file.

Good entry:

```markdown
## 2026-04-30 — OpenCode Phase 0-1 skills deepened

### Changed
- Expanded Iris skills with pressure scenarios, decision tables, and output contracts.

### Files
- .opencode/skills/iris-*/SKILL.md

### Validation
- Skill line counts inspected.
- No command files changed.

### Next
- Implement Phase 2 shared scripts.
```

Bad entry:

```markdown
Worked on skills.
```

Why bad: no scope, files, validation, or next step.

## overview.md

Keep short and current:

- current phase;
- implementation target;
- working status;
- next immediate step;
- known blockers.

Update it when those facts change.

## log_notes.md

Record:

- failed commands;
- build/test/runtime errors;
- suspected bugs;
- unresolved follow-ups;
- environment issues;
- blocked verification.

Use factual language. Avoid vague “maybe later” notes.

Use `log_notes.md` for unresolved problems only. Resolved historical failures may remain there, but do not add noise for successful work.

Template:

```markdown
## YYYY-MM-DD — Problem title

### Symptom
- ...

### Cause / Hypothesis
- ...

### Action
- ...

### Status
- Open / Resolved / Deferred
```

## debt_tech_backlog.md

Record real debt:

- temporary workaround;
- missing test project or test coverage;
- deferred validation;
- incomplete mapper/policy;
- known cleanup that should not be hidden in comments.

Template:

```markdown
## Debt: Short title

### Area
...

### Problem
...

### Risk
...

### Proposed fix
...

### Priority
Low / Medium / High
```

## mem_library

Use only for stable product meaning:

- vision;
- persona;
- UX expectations;
- roadmap;
- security/privacy principles;
- long-term architecture meaning.

Do not use `mem_library` as a bug tracker.

## Stop Conditions

Stop when:

- memory directory cannot be found and update is required;
- it is unclear whether a fact is completed or merely proposed;
- the requested update would overwrite large memory sections;
- a write would create a conflicting memory filename;
- the user asks to store sensitive/private data that does not belong in project memory.

## Pressure Scenarios

### Scenario 1: Review finds P1 but user did not ask for memory update

Expected:

- report finding;
- do not update memory;
- mention memory update as optional follow-up if useful.

### Scenario 2: Implementation passes but one manual smoke gap remains

Expected:

- record completed implementation in `PROJECT_LOG.md`;
- record current manual gap in `overview.md` or `log_notes.md` if it affects next step;
- do not exaggerate validation.

### Scenario 3: User says "remember this idea"

Expected:

- determine whether it is durable product meaning, roadmap, or task note;
- write to `mem_library` only for stable meaning;
- otherwise use backlog/log/status files.

### Scenario 4: `.agent` missing but `.agents` exists

Expected:

- use `.agents` as fallback;
- do not create `.agent` unless explicitly requested as a migration.

## Rationalization Table

| Rationalization | Correct response |
|---|---|
| "I should update memory after every command." | Only meaningful completed work or explicit request. |
| "This note might be useful someday." | If not factual/current/durable, do not store it. |
| "local_notes is mentioned in old files." | For Iris, prefer `log_notes.md`; `local_notes.md` is fallback only. |
| "I'll summarize the whole project again." | Keep memory short and current; append facts only. |
| "Memory can store raw prompts for traceability." | Do not store private prompt content by default. |

## Output Expectations

After memory updates, report:

- memory directory used;
- files changed;
- facts recorded;
- verification status;
- anything intentionally not recorded.

## Update Output Template

```markdown
# Memory Update Result

## Directory
- `.agent`

## Files Updated
- `.agent/PROJECT_LOG.md` — completed iteration
- `.agent/overview.md` — next step changed

## Facts Recorded
- ...

## Not Recorded
- ...

## Verification
- ...
```

## Quality Checklist

- [ ] `.agent` was preferred over `.agents`.
- [ ] `log_notes.md` was preferred over `local_notes.md`.
- [ ] Memory update is factual and dated.
- [ ] No stable product memory was used as a task log.
- [ ] No unrelated memory files were changed.
- [ ] No secrets or sensitive data were recorded.

## Self-Test Checklist

- Did I choose read-only vs write mode correctly?
- Did I route each fact to only one primary file?
- Did I avoid storing proposals as completed facts?
- Did I preserve append-only history?
- Did I leave memory shorter and more useful than before?
