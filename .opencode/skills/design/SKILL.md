---
name: design
description: Create an architecture design after an engineering specification and before an implementation plan. Use when the solution structure, contracts, boundaries, data flow, failure handling, or integration approach must be defined.
compatibility: opencode
metadata:
  workflow_stage: design
  output_type: architecture_design
---

# Design Skill

## Purpose

Use this skill to create an architecture design after a specification has been created or provided.

A design defines **how the required behavior should be structured** while preserving the project architecture, contracts, boundaries, and long-term maintainability.

The design must not become a step-by-step implementation plan.

## When to Use

Use this skill when the task involves:

- architecture-sensitive change;
- new feature with multiple affected modules;
- new contract or interface;
- persistence or migration design;
- model/provider integration;
- UI-to-application flow;
- background process;
- security or permission flow;
- error handling strategy;
- refactor with boundary impact;
- complex testing strategy.

Do not use this skill for:

- trivial local edits;
- formatting-only changes;
- one-line bug fixes;
- pure documentation save operations;
- implementation sequencing.

## Required Inputs

Before writing the design, inspect relevant context:

1. Approved or draft specification
2. `AGENTS.md`
3. Relevant `.opencode/rules/*.md`
4. Existing architecture documentation
5. Existing source structure
6. Existing contracts/interfaces
7. Existing tests
8. `.agents/overview.md` if present
9. `.agents/PROJECT_LOG.md` if present
10. `.agents/mem_library/**` if relevant

Do not ask the user a question until relevant project context has been inspected.

Ask only if:

- the specification has a blocking ambiguity;
- existing docs conflict;
- a product-level decision is required;
- multiple viable designs have materially different long-term consequences.

## Core Rules

Hard prohibitions live in `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, and `.opencode/rules/workflow.md`.

### 1. Implement the Spec

The design traces back to the specification. Do not introduce new requirements, expand product scope, or change acceptance criteria.

### 2. Preserve Architecture

Do not design shortcuts (UI→persistence, UI→providers, Domain→infrastructure, Application→concrete adapters, Tools owning policy, Voice owning orchestration, Perception owning memory extraction, hosts owning business logic). Respect the project's existing boundaries.

### 3. Define Ownership, Contracts, and Data Flow

For each new behavior: assign a clear owner layer (Domain/Application/Adapter/Host/Shared). Define the contract shape, owner, consumers, compatibility, and error behavior. Describe how data moves through the system including entry point, orchestration, adapter calls, persistence, and error paths.

### 4. Define Failure Handling and Options

Describe error handling for each failure mode (invalid input, not found, timeout, permission denied, cancellation, etc.). When multiple viable approaches exist, include a short options comparison with benefits, drawbacks, and recommendation.

### 5. Stay in Design Stage

Do not produce step-by-step coding sequences, file-by-file checklists, or exact phase breakdowns. Those belong in `/plan`.

## Output Format

Use this exact structure unless the user explicitly requests another format.

```md
# Design: <Task Name>

## 1. Design Goal

Describe the technical design goal and link it to the specification.

## 2. Specification Traceability

Reference the spec this design implements.

List the key spec requirements this design addresses.

- FR-001 → ...
- FR-002 → ...
- AC-001 → ...

If no formal spec exists, state that the design is based on the user request and list assumptions.

## 3. Current Architecture Context

Summarize the relevant current architecture, modules, boundaries, and contracts.

Only include facts supported by inspected project files or user-provided context.

## 4. Proposed Design Summary

Describe the proposed structure at a high level.

Include the main components and their responsibilities.

## 5. Responsibility Ownership

Map responsibilities to layers/modules.

| Responsibility | Owner | Notes |
|---|---|---|
| ... | ... | ... |

## 6. Component Design

Describe each affected component.

For each component:

### `<Component Name>`

- Owner layer:
- Responsibility:
- Inputs:
- Outputs:
- Collaborators:
- Must not do:
- Notes:

## 7. Contract Design

Describe affected contracts.

For each contract:

### `<Contract Name>`

- Owner:
- Consumers:
- Shape:
- Compatibility:
- Error behavior:
- Stability:

Use small illustrative snippets only when they clarify the contract.

Do not include full implementation code.

## 8. Data Flow

Describe the primary successful flow.

Use a numbered list.

Then describe relevant alternative/error flows.

### Primary Flow

1. ...

### Error / Alternative Flows

- ...

## 9. Data and State Design

Describe persistence, identity, lifecycle, ordering, consistency, caching, or in-memory state.

If no data/state changes are required, state that explicitly.

## 10. Error Handling Design

Describe how errors are represented, converted, propagated, logged, retried, or surfaced.

Include cancellation behavior if relevant.

## 11. Configuration and Dependency Injection

Describe required configuration, options, dependency registration, and composition boundaries.

If no configuration changes are required, state that explicitly.

## 12. Security and Permission Considerations

Describe security-sensitive behavior, permission checks, secret handling, data exposure, or trust boundaries.

If not relevant, state that explicitly.

## 13. Testing Design

Describe required tests by level.

Include relevant categories:

- unit tests;
- integration tests;
- architecture tests;
- contract tests;
- regression tests;
- negative-path tests;
- manual verification.

## 14. Options Considered

If applicable, compare alternative designs.

If not applicable, write:

No material alternative designs were considered necessary.

## 15. Risks and Trade-offs

List known risks, trade-offs, and possible future pressure points.

## 16. Open Questions

List only questions that block safe implementation planning.

If there are no blocking questions, write:

No blocking open questions.

##Quality Checklist

Before finalizing the design, verify:

 The design traces back to the spec.
 No new requirements were silently introduced.
 Architecture boundaries are preserved.
 Responsibility ownership is explicit.
 Contract changes are documented.
 Data flow is clear.
 Error handling is defined.
 Data/state impact is addressed.
 Configuration/DI impact is addressed.
 Security impact is addressed.
 Testing design is specific.
 Alternatives are considered when materially relevant.
 Risks and trade-offs are stated.
 The design does not become an implementation plan.
 Open questions are truly blocking.

## Anti-Patterns

Avoid:
- designing without a spec or stated assumptions;
- hidden scope expansion or mixing design with implementation plan;
- inventing architecture that conflicts with the project;
- vague component ownership or duplicate responsibility across layers;
- bypassing existing abstractions;
- omitting error handling or testing design;
- adding speculative abstractions or large rewrites without approval.

## Final Response Requirements

When using this skill, final response must include:

the full design;
assumptions made;
blocking open questions, if any;
explicit statement that no implementation was performed.

Do not modify files unless the user explicitly asks to save the design.