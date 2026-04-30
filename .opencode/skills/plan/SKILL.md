---
name: plan
description: Create a phased implementation plan after a specification and architecture design. Use when the work must be broken into safe, verifiable implementation steps before code changes.
compatibility: opencode
metadata:
  workflow_stage: implementation_planning
  output_type: implementation_plan
---

# Plan Skill

## Purpose

Use this skill to create a phased implementation plan after a specification and design exist.

A plan defines **how to safely implement the approved design** through small, verifiable steps.

The plan must not change the specification or redesign the architecture.

## When to Use

Use this skill when the task involves:

- non-trivial implementation;
- multiple files or modules;
- architecture-sensitive code changes;
- public contract changes;
- persistence or migration work;
- model/provider integration;
- UI-to-application wiring;
- background jobs;
- permission or security logic;
- tests that must be added or updated;
- documentation or agent-memory updates;
- refactor with risk of behavior drift.

Do not use this skill for:

- trivial typo fixes;
- formatting-only changes;
- one-line local corrections;
- pure documentation save operations;
- exploratory brainstorming without intent to implement.

## Required Inputs

Before writing the plan, inspect relevant context:

1. Approved or draft specification
2. Approved or draft architecture design
3. `AGENTS.md`
4. Relevant `.opencode/rules/*.md`
5. Existing architecture documentation
6. Existing source structure
7. Existing tests
8. Existing build/test commands
9. `.agents/overview.md` if present
10. `.agents/PROJECT_LOG.md` if present
11. `.agents/mem_library/**` if relevant

Do not ask the user a question until relevant project context has been inspected.

Ask only if:

- the spec/design has a blocking ambiguity;
- existing docs conflict;
- implementation sequencing depends on a product-level decision;
- a risky migration or destructive operation is required.

## Core Rules

Hard prohibitions live in `.opencode/rules/workflow.md`, `.opencode/rules/iris-architecture.md`, and `.opencode/rules/no-shortcuts.md`.

### 1. Follow the Design

The plan implements the approved design. Do not introduce new requirements, change acceptance criteria, or change architecture decisions. If the design is unsafe, stop and report.

### 2. Small Safe Phases

Each phase must be narrow, reviewable, reversible, and tied to verification. Avoid "implement everything" phases.

### 3. Identify Files and Verification

For each phase: list files to inspect, files likely to edit, files not to touch. Each phase must have a verification step (build, test, architecture test, manual check, or diff review).

### 4. Include Tests, Docs, and Rollback

Tests are first-class work — specify level, target project, positive/negative/regression cases. Documentation updates are required when behavior, architecture, setup, or contracts change. Each phase must include rollback guidance.

### 5. Stay in Planning Stage

Avoid: exact line numbers until inspected, speculative class names, excessive micro-steps, implementation code, or unrelated commands. The plan must be actionable, not bureaucratic.

## Output Format

Use this exact structure unless the user explicitly requests another format.

```md
# Implementation Plan: <Task Name>

## 1. Plan Goal

Describe what this plan implements and which spec/design it follows.

## 2. Inputs and Assumptions

### Inputs

- Specification: ...
- Design: ...
- Relevant rules/docs: ...

### Assumptions

- ...

If no formal spec or design exists, state that explicitly and list the assumptions.

## 3. Scope Control

### In Scope

- ...

### Out of Scope

- ...

### Forbidden Changes

- ...

## 4. Implementation Strategy

Summarize the implementation approach in a few paragraphs.

Explain the order of work and why it is safe.

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Inspect existing structure and confirm assumptions before editing.

#### Files to Inspect

- ...

#### Files Likely to Edit

- None.

#### Steps

1. ...

#### Verification

- ...

#### Rollback

No code changes.

---

### Phase 1 — <Phase Name>

#### Goal

...

#### Files to Inspect

- ...

#### Files Likely to Edit

- ...

#### Files That Must Not Be Touched

- ...

#### Steps

1. ...

#### Verification

- ...

#### Rollback

- ...

---

### Phase 2 — <Phase Name>

#### Goal

...

#### Files to Inspect

- ...

#### Files Likely to Edit

- ...

#### Files That Must Not Be Touched

- ...

#### Steps

1. ...

#### Verification

- ...

#### Rollback

- ...

Continue phases as needed.

## 6. Testing Plan

### Unit Tests

- ...

### Integration Tests

- ...

### Architecture Tests

- ...

### Regression Tests

- ...

### Manual Verification

- ...

## 7. Documentation and Memory Plan

### Documentation Updates

- ...

### Agent Memory Updates

- ...

If not required, state that explicitly.

## 8. Verification Commands

List expected commands.

Example:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes

Only include commands relevant to the repository and task.

## 9. Risk Register
Risk	Impact	Mitigation
...	...	...

## 10. Implementation Handoff Notes

Provide concise notes for the implementation agent.

Include:

critical constraints;
risky areas;
expected final state;
checks that must not be skipped.
11. Open Questions

List only questions that block safe implementation.

If there are no blocking questions, write:

No blocking open questions.


## Quality Checklist

Before finalizing the plan, verify:

- [ ] The plan follows the spec.
- [ ] The plan follows the design.
- [ ] The plan does not introduce new requirements.
- [ ] Scope and forbidden changes are explicit.
- [ ] Phases are small and safe.
- [ ] Each phase has verification.
- [ ] Each phase has rollback guidance.
- [ ] Files to inspect/edit are identified.
- [ ] Existing structure must be checked before creating files.
- [ ] Architecture boundaries are preserved.
- [ ] Test work is included.
- [ ] Documentation/memory work is addressed.
- [ ] Verification commands are listed.
- [ ] Risks and mitigations are stated.
- [ ] Open questions are truly blocking.

## Anti-Patterns

Avoid:
- implementing inside the plan;
- changing the design or spec;
- hiding risky steps or one giant phase;
- vague "add tests" wording;
- drive-by refactoring;
- creating new files before checking existing placeholders;
- skipping rollback notes or verification;
- treating docs/memory as optional when behavior changes.

## Final Response Requirements

When using this skill, final response must include:

1. the full implementation plan;
2. assumptions made;
3. blocking open questions, if any;
4. explicit statement that no implementation was performed.

Do not modify files unless the user explicitly asks to save the plan.