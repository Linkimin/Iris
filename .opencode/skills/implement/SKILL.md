
---
name: implement
description: Implement an approved plan with minimal, architecture-compliant changes. Use only after a specification, design, and implementation plan exist or the user explicitly authorizes a small direct implementation.
compatibility: opencode
metadata:
  workflow_stage: implementation
  output_type: code_changes
---

# Implement Skill

## Purpose

Use this skill to implement an approved implementation plan.

Implementation means changing repository files to satisfy the approved plan while preserving architecture, contracts, tests, documentation, and project intent.

This skill must not invent a new specification, redesign the solution, or expand scope.

## When to Use

Use this skill when:

- an implementation plan exists;
- the user explicitly asks to implement;
- the affected scope is understood;
- required files and project boundaries have been inspected;
- the change can be verified.

For trivial local changes, this skill may be used without a full spec/design/plan only if the user explicitly requests direct implementation.

## Required Inputs

Before editing, inspect:

1. Approved or draft specification
2. Approved or draft architecture design
3. Approved or draft implementation plan
4. `AGENTS.md`
5. Relevant `.opencode/rules/*.md`
6. Existing source structure
7. Existing tests
8. Existing build/test commands
9. `.agents/overview.md` if present
10. `.agents/PROJECT_LOG.md` if present
11. `.agents/mem_library/**` if relevant

If the plan references files or abstractions, verify that they actually exist before editing.

## Core Rules

### 1. Implement the Plan Only

Do not:

- introduce new requirements;
- change acceptance criteria;
- redesign the architecture;
- change public contracts beyond the plan;
- add unrelated refactors;
- perform broad cleanup;
- add speculative abstractions;
- change formatting across unrelated files.

If the plan is unsafe, incomplete, or conflicts with the repository, stop and report the issue.

### 2. Keep the Diff Minimal

Every changed file must be justified by the plan.

Prefer:

- small focused changes;
- existing abstractions;
- existing naming conventions;
- existing folder structure;
- existing testing patterns.

Avoid:

- parallel duplicate abstractions;
- broad rewrites;
- large formatting changes;
- touching files outside the affected area;
- adding dependencies without explicit plan approval.

### 3. Inspect Before Creating Files

Before creating a new file, check whether a suitable file, folder, abstraction, placeholder, or convention already exists.

Create new files only when:

- the plan requires it;
- no suitable existing location exists;
- the new file has a clear owner;
- the new file follows project naming conventions.

### 4. Preserve Architecture Boundaries

Do not implement shortcuts such as:

- UI directly calls persistence;
- UI directly calls model providers;
- Domain references infrastructure;
- Application references concrete adapters;
- Tools own permission decisions;
- Voice owns chat orchestration;
- Perception owns memory extraction;
- hosts own business logic.

If a requested implementation requires a boundary violation, stop and report it.

### 5. Respect Dependency Ownership

New dependencies must be added only when necessary and approved by the plan.

Before adding a dependency:

- check whether an existing dependency already solves the problem;
- check central package management if present;
- add it in the correct project only;
- document why it is needed;
- verify restore/build.

Do not add packages casually.

### 6. Tests Are Part of Implementation

If behavior changes, add or update tests.

Test changes must be tied to the implemented behavior.

Prefer:

- unit tests for domain/application behavior;
- adapter tests for persistence/model/tools/voice/perception behavior;
- integration tests for wiring and real infrastructure seams;
- architecture tests for dependency rules;
- regression tests for fixed defects.

Avoid:

- tests without assertions;
- over-mocking;
- tests that mirror implementation details;
- broad snapshot tests without clear value;
- deleting tests to make the suite pass.

### 7. Error Handling Must Match Design

Implement failure behavior as specified.

Do not:

- swallow exceptions silently;
- leak provider/infrastructure exceptions through application contracts;
- replace typed failures with generic strings unless the project convention uses them;
- ignore cancellation tokens where async operations are involved;
- log sensitive data.

### 8. Documentation and Memory

Update documentation when implementation changes:

- public behavior;
- architecture;
- setup;
- configuration;
- contracts;
- commands;
- persistence schema;
- operational behavior.

Update `.agents/` memory files when present and required by project convention.

Do not create or update docs merely to appear productive.

### 9. Verification Is Required

After implementation, run the narrowest useful verification first.

Typical order:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
````

Use repository-specific commands when available.

If verification cannot be run, state why.

Do not claim success unless the command was actually run and passed.

### 10. Be Honest About Partial Completion

If implementation is incomplete, say so clearly.

Report:

* completed work;
* skipped work;
* failed checks;
* known risks;
* required next step.

Do not hide failures.

## Pre-Implementation Gate Check

Before editing, verify that required audit gates are satisfied.

Use the gate definitions in `iris-engineering/SKILL.md` for A-G criteria.

### Check procedure

1. Is the task trivial/local? If yes, skip formal gates.
2. Gate A — spec: Is an approved/draft spec present? If the task is non-trivial and no spec exists, stop.
3. Gate B — design: Is the change architecture-affecting? If yes, is a design present?
4. Gate C — plan: Is the change multi-file? If yes, is an approved plan present? **If no plan and multi-file, hard stop.**
5. Gate D — verification: Not checked before implementation (runs after).
6. Gate E — architecture review: Not checked before implementation (runs after for boundary changes).
7. Gate F — audit: Not checked before implementation (runs after for readiness).
8. Gate G — memory: Not checked before implementation (runs after meaningful work).

### Gate C Hard Stop

If the planned change touches more than one file and no approved implementation plan exists:

```md
# Implementation Blocked

## Reason

Gate C failed: no approved implementation plan exists for a multi-file change.

## What Was Checked

- Task scope assessed
- No plan found in docs/plans/ or docs/superpowers/plans/

## Safe Next Step

Run `/plan <task>` to create an approved implementation plan.
```

Do not proceed to editing. Do not create an ad-hoc plan inline.

## Implementation Procedure

Follow this sequence unless the plan requires a different safe order:

1. Read the approved plan.
2. Inspect affected files.
3. Confirm existing structure and conventions.
4. Identify the smallest safe change set.
5. Edit files according to the current phase.
6. Add or update tests.
7. Run targeted verification.
8. Repeat for the next phase if safe.
9. Run broader verification.
10. Review the final diff.
11. Update docs/memory if required.
12. Summarize results.

## Editing Rules

Allowed:

* edit files directly required by the plan;
* add files required by the plan;
* add or update tests required by the change;
* update documentation directly related to changed behavior;
* update agent memory files if required.

Not allowed:

* destructive file operations without explicit approval;
* deleting tests to pass verification;
* changing unrelated formatting;
* modifying secrets or production credentials;
* changing CI/release configuration unless required by the plan;
* changing project references unless required by the plan;
* changing migrations unless required by the plan;
* pushing commits;
* force-resetting repository state.

## Output Format

After implementation, respond using this structure:

````md
# Implementation Result

## Summary

Briefly describe what was implemented.

## Files Changed

- `<path>` — <reason>
- `<path>` — <reason>

## Behavior Implemented

- ...

## Tests Added or Updated

- ...

## Documentation / Memory Updates

- ...

## Verification

### Commands Run

```bash
<command>
````

### Result

* Passed:
* Failed:
* Not run:

## Deviations from Plan

* ...

If there were no deviations, write:

No deviations from the approved plan.

## Risks / Follow-ups

* ...

If there are no known risks, write:

No known remaining risks.

## Execution Note

Implementation was performed only within the approved scope.

````

## Failure Output Format

If implementation cannot safely proceed, respond with:

```md
# Implementation Blocked

## Reason

<clear reason>

## What Was Checked

- ...

## Conflict or Risk

- ...

## Safe Next Step

- ...
````

## Quality Checklist

Before finalizing, verify:

* [ ] The implementation follows the plan.
* [ ] No new requirements were introduced.
* [ ] Architecture boundaries are preserved.
* [ ] The diff is minimal.
* [ ] Existing structure was checked before creating files.
* [ ] New dependencies are justified or avoided.
* [ ] Tests were added or updated where behavior changed.
* [ ] Error handling matches the design.
* [ ] Documentation/memory updates are included if required.
* [ ] Verification commands were run or skipped with a stated reason.
* [ ] Failures are reported honestly.
* [ ] No secrets or destructive operations were used.
* [ ] No unrelated files were modified.

## Anti-Patterns

Avoid:

* “while I was here” refactors;
* implementing beyond the plan;
* creating duplicate abstractions;
* bypassing layers to make the feature work faster;
* hiding failing tests;
* removing tests instead of fixing behavior;
* claiming commands passed without running them;
* adding packages without need;
* changing public contracts silently;
* updating docs without actual behavior change;
* broad formatting changes unrelated to the task;
* using implementation as a reason to rewrite the design.

## Final Response Requirements

When using this skill, final response must include:

1. what changed;
2. files changed;
3. tests added or updated;
4. verification commands and results;
5. deviations from the plan;
6. remaining risks or follow-ups.

Do not claim the implementation is complete unless verification supports that claim.

```

