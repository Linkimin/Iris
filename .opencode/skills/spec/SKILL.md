---
name: spec
description: Create a strict engineering specification before design or implementation. Use when a task is non-trivial, changes behavior, touches architecture, public contracts, persistence, security, tests, or long-term project structure.
compatibility: opencode
metadata:
  workflow_stage: specification
  output_type: engineering_spec
---

# Spec Skill

## Purpose

Use this skill to produce a strict engineering specification before design or implementation.

A specification defines:

- what must be achieved;
- what must not be changed;
- which constraints apply;
- how completion will be judged.

The spec must not describe low-level implementation steps unless they are hard constraints.

## When to Use

Use this skill when the task involves:

- new feature;
- behavior change;
- architecture-sensitive change;
- public contract change;
- persistence or migration change;
- security or permissions logic;
- model/provider integration;
- UI flow with application impact;
- test strategy changes;
- refactor with possible behavior impact;
- unclear or broad user request.

Do not use this skill for trivial local edits such as typo fixes, formatting-only changes, or obvious one-line corrections.

## Required Context

Before writing the spec, inspect relevant context:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Existing docs related to the task
4. Existing source structure
5. Existing tests
6. `.agents/overview.md` if present
7. `.agents/PROJECT_LOG.md` if present
8. `.agents/mem_library/**` if relevant

Do not ask the user a question until relevant project context has been inspected.

Ask only if:

- the required decision is genuinely product-level or architecture-level;
- existing docs conflict;
- acceptance criteria cannot be inferred safely.

## Core Rules

These rules govern specification behavior. Hard prohibitions live in:
- `.opencode/rules/workflow.md`
- `.opencode/rules/iris-architecture.md`
- `.opencode/rules/no-shortcuts.md`

### 1. Separate Problem from Solution

The spec defines the required outcome, not the implementation sequence. Avoid premature decisions about classes, file names, algorithms, or library choices unless they are explicit constraints.

### 2. Define Scope Boundaries

Every spec must include: in scope, out of scope, non-goals, affected areas, explicitly forbidden changes.

### 3. Preserve Architecture

Respect project boundaries. Do not allow: bypassing Application, moving business logic to UI/infrastructure, adding infrastructure dependencies to Domain/Application, replacing architecture without approval, large drive-by refactors.

### 4. Define Acceptance Criteria

Acceptance criteria must be concrete and verifiable (e.g., "`dotnet test` passes", "Application does not reference EF Core"). Avoid vague criteria like "code should be clean" or "system should work better."

### 5. Define Contracts and Failure Modes

If the task touches a contract, document it (API, domain model, database schema, config, UI behavior, etc.). For each: state whether unchanged, extended backward-compatibly, changed, deprecated, or removed. Describe expected failure modes (invalid input, missing data, provider unavailable, cancellation, etc.).

### 6. Stay in Specification Stage

The spec must not become a design or plan. Avoid: step-by-step coding sequences, detailed class implementations, speculative abstractions, unrelated cleanup lists. Those belong in `/design` and `/plan`.

## Output Format

Use this exact structure unless the user explicitly requests another format.

```md
# Specification: <Task Name>

## 1. Problem Statement

Describe the problem in concrete engineering terms.

Explain why the change is needed and what current limitation, defect, or missing capability it addresses.

## 2. Goal

Define the desired end state.

The goal must be specific enough to verify.

## 3. Scope

### In Scope

- ...

### Out of Scope

- ...

### Non-Goals

- ...

## 4. Current State

Summarize the relevant current implementation, structure, contracts, or known constraints.

Only include facts supported by inspected project files or user-provided context.

## 5. Affected Areas

List affected projects, modules, layers, files, or contracts.

## 6. Functional Requirements

Use numbered requirements.

- FR-001: ...
- FR-002: ...

Each requirement must describe required behavior, not implementation preference.

## 7. Architecture Constraints

Use numbered constraints.

- AC-001: ...
- AC-002: ...

Each constraint must describe an architectural boundary, invariant, dependency rule, or ownership rule.

## 8. Contract Requirements

Document affected contracts.

For each contract:

- name;
- current behavior;
- required behavior;
- compatibility expectation.

If no public contracts are affected, state that explicitly.

## 9. Data and State Requirements

Describe persisted data, in-memory state, lifecycle rules, ordering rules, identity rules, and consistency expectations.

If no data/state changes are required, state that explicitly.

## 10. Error Handling and Failure Modes

List expected failure modes and required behavior.

## 11. Testing Requirements

Define required test coverage.

Include relevant categories:

- unit tests;
- integration tests;
- architecture tests;
- regression tests;
- negative-path tests;
- manual verification.

State which checks must pass.

## 12. Documentation and Memory Requirements

Specify docs or agent-memory files that must be updated if the implementation changes project status, architecture, setup, contracts, or behavior.

If no documentation updates are required, state that explicitly.

## 13. Acceptance Criteria

Use checkboxes.

- [ ] ...
- [ ] ...
- [ ] ...

Acceptance criteria must be concrete and verifiable.

## 14. Open Questions

List only questions that block safe design or implementation.

If there are no blocking questions, write:

No blocking open questions.

##Quality Checklist

Before finalizing the spec, verify:

 The problem is clearly stated.
 The goal is specific and verifiable.
 Scope and non-goals are explicit.
 Affected areas are listed.
 Requirements describe behavior, not implementation sequence.
 Architecture constraints are explicit.
 Contract compatibility is addressed.
 Data/state impact is addressed.
 Failure modes are included.
 Testing requirements are specific.
 Acceptance criteria are observable.
 The spec does not contain premature implementation detail.
 The spec does not authorize unrelated refactoring.
 Open questions are truly blocking.


## Anti-Patterns

Avoid:
- vague requirements or hidden scope expansion;
- combining spec, design, and plan into one document;
- inventing architecture not present in the project;
- omitting compatibility expectations or failure modes;
- adding implementation details too early;
- using the spec as permission for unrelated cleanup;
- writing acceptance criteria that cannot be tested.


##Final Response Requirements

When using this skill, final response must include:

the full specification;
assumptions made;
blocking open questions, if any;
explicit statement that no implementation was performed.

Do not modify files unless the user explicitly asks to save the spec.