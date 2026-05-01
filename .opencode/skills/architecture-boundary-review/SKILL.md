---
name: architecture-boundary-review
description: Review architecture boundaries, dependency direction, layer ownership, forbidden shortcuts, project references, and long-term structural drift. Use before or after implementation when a change may affect system architecture.
compatibility: opencode
metadata:
  workflow_stage: architecture_review
  output_type: boundary_review_report
---

# Architecture Boundary Review Skill

## Purpose

Use this skill to review whether proposed or implemented changes preserve project architecture boundaries.

This skill focuses on structural correctness:

- dependency direction;
- layer ownership;
- project references;
- abstraction boundaries;
- forbidden shortcuts;
- infrastructure leakage;
- host/application/domain separation;
- long-term architecture drift.

This skill is narrower than a full audit.  
It may be used before implementation, after implementation, or as part of audit preparation.

## When to Use

Use this skill when the task touches:

- project references;
- dependency injection;
- domain model;
- application interfaces;
- persistence adapters;
- model/provider adapters;
- UI/application wiring;
- background workers;
- tool execution;
- voice/perception integration;
- shared primitives;
- architecture tests;
- public contracts between layers;
- refactoring across projects or modules.

Do not use this skill for:

- pure typo fixes;
- formatting-only changes;
- isolated test naming changes;
- documentation-only edits with no architecture impact;
- saving spec/design/plan/audit documents.

## Required Context

Before reviewing boundaries, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Architecture documentation if present
4. Approved spec/design/plan if present
5. Current git status
6. Current git diff
7. Project/solution structure
8. Project reference files
9. Dependency injection registration files
10. Changed source files
11. Changed tests
12. Architecture tests if present
13. `.agents/overview.md` if present
14. `.agents/mem_library/**` if relevant

Do not ask the user a question until relevant project context has been inspected.

Ask only if project ownership rules are absent or conflicting.

## Core Rules

### 1. Preserve Architecture

Use the authoritative architecture definitions in:
- `.opencode/rules/iris-architecture.md` (dependency direction, project reference rules, DI rules)
- `.opencode/rules/no-shortcuts.md` (forbidden shortcuts)
- `.opencode/skills/iris-architecture/SKILL.md` (placement decision table, boundary smell checklist, project reference checks, DI checks)

The expected direction is: `Shared ← Domain ← Application ← Adapters ← Hosts`.

Key checks for this review:
- **Domain:** Must not contain EF Core, HTTP, UI, infrastructure. Pure concepts only.
- **Application:** Must not depend on concrete adapters (Persistence, ModelGateway, Perception, Tools, Voice, Infrastructure). Owns ports, use cases, policies.
- **Adapters:** Implement Application abstractions. Must not own business rules, product workflow, or permission decisions.
- **Hosts:** Compose and present. Must not own domain rules, application logic, persistence, or provider logic.
- **Shared:** Neutral primitives only. Must not contain product/domain behavior.

### 2. Detect Shortcuts

Flag: UI→database, UI→provider, Domain→infrastructure, Application→concrete adapter, Tools owns policy, Voice owns orchestration, Perception owns memory, host owns business logic, Shared contains product logic, adapter→adapter references not approved.

### 3. Review Project References and DI

Check: forbidden upward references, circular references, adapter→host, host→host, Domain→adapter, accidental test reference in production code. DI: adapter registers own implementations, host composes, Application registers only Application services, Domain has no registration.

## Review Procedure

Follow this sequence:

1. Inspect project architecture rules.
2. Inspect solution/project structure.
3. Inspect changed files.
4. Inspect project references.
5. Inspect DI registration.
6. Inspect changed namespaces/imports/usings.
7. Inspect tests and architecture checks.
8. Identify boundary violations.
9. Identify architecture drift risks.
10. Recommend minimal remediation.

## Output Format

Use this exact structure unless the user explicitly requests another format.

````md
# Architecture Boundary Review: <Task Name>

## 1. Summary

### Review Status

Use one:

- Passed
- Passed with P2 notes
- Blocked by P1 issues
- Blocked by P0 issues
- Partial / Evidence insufficient

### Architecture Readiness

Use one:

- Ready
- Ready after P2 backlog
- Not ready
- Cannot determine

### High-Level Result

Briefly summarize the outcome.

## 2. Context Reviewed

- Architecture rules:
- Spec/design/plan:
- Git diff:
- Project references:
- DI registration:
- Source files:
- Test files:
- Architecture tests:

If something was unavailable, state that explicitly.

## 3. Dependency Direction Review

Expected direction:

```text
<project-specific dependency direction>
````

### Findings

* ...

## 4. Layer Ownership Review

| Layer / Module | Expected Ownership | Observed Concern |
| -------------- | ------------------ | ---------------- |
| Domain         | ...                | ...              |
| Application    | ...                | ...              |
| Adapters       | ...                | ...              |
| Hosts          | ...                | ...              |
| Shared         | ...                | ...              |

## 5. Forbidden Shortcut Review

Check each relevant shortcut.

| Shortcut                       | Status            | Evidence |
| ------------------------------ | ----------------- | -------- |
| UI → Database                  | Pass / Fail / N/A | ...      |
| UI → Model Provider            | Pass / Fail / N/A | ...      |
| Domain → Infrastructure        | Pass / Fail / N/A | ...      |
| Application → Concrete Adapter | Pass / Fail / N/A | ...      |
| Host Owns Business Logic       | Pass / Fail / N/A | ...      |
| Shared Contains Product Logic  | Pass / Fail / N/A | ...      |

## 6. Project Reference Review

### Findings

* ...

## 7. Dependency Injection Review

### Findings

* ...

## 8. Contract and Abstraction Review

### Findings

* ...

## 9. Test Boundary Review

### Findings

* ...

## 10. Consolidated Findings

### P0 — Must Fix

```md
#### P0-001: <Title>

- Evidence:
- Impact:
- Recommended fix:
```

If none:

No P0 issues.

### P1 — Should Fix

```md
#### P1-001: <Title>

- Evidence:
- Impact:
- Recommended fix:
```

If none:

No P1 issues.

### P2 — Backlog

```md
#### P2-001: <Title>

- Evidence:
- Impact:
- Recommended fix:
```

If none:

No P2 issues.

## 11. Suggested Fix Order

If issues exist, list the safest order to address them.

If no fixes are required, write:

No fixes required.

## 12. Final Decision

Use one:

* Approved
* Approved with P2 backlog
* Changes requested
* Blocked
* Cannot determine from available evidence

Explain briefly.

```

## Severity Guidance

### P0

Use P0 for:

- domain depends on infrastructure;
- application depends on concrete adapter;
- build broken due to reference cycle;
- public contract broken unintentionally;
- security boundary bypass;
- host bypasses application for core workflow;
- data loss risk due to architecture shortcut.

### P1

Use P1 for:

- new coupling that will block planned extension;
- missing abstraction for a real seam;
- DI registration in wrong layer;
- adapter owns application policy;
- missing architecture regression tests after boundary-sensitive change;
- unclear ownership likely to cause drift.

### P2

Use P2 for:

- naming/folder inconsistency;
- small duplication of mapping logic;
- minor test organization issue;
- documentation gap for boundary rule;
- optional architecture test improvement.

## Quality Checklist

Before finalizing, verify:

- [ ] Project-specific architecture rules were inspected.
- [ ] Dependency direction was checked.
- [ ] Project references were checked if relevant.
- [ ] DI registration was checked if relevant.
- [ ] Changed source files were reviewed.
- [ ] Layer ownership was assessed.
- [ ] Forbidden shortcuts were checked.
- [ ] Shared remained neutral.
- [ ] Domain remained infrastructure-free.
- [ ] Application remained adapter-independent.
- [ ] Hosts remained composition/presentation layers.
- [ ] Tests were checked for boundary protection.
- [ ] Findings include evidence.
- [ ] Severities are justified.
- [ ] Recommended fixes are minimal.
- [ ] No code was changed unless explicitly requested.

## Anti-Patterns

Avoid:
- generic architecture advice not tied to the project;
- treating every abstraction gap as a blocker;
- approving boundary violations because they are convenient;
- demanding speculative abstractions without a clear seam;
- ignoring project references or DI registration;
- collapsing review and implementation;
- changing files during review;
- hiding uncertainty.

## Final Response Requirements

When using this skill, final response must include:

1. review status;
2. architecture readiness decision;
3. dependency direction assessment;
4. layer ownership assessment;
5. forbidden shortcut assessment;
6. P0/P1/P2 findings;
7. suggested fix order;
8. explicit statement that no fixes were implemented unless requested.

Do not modify files unless the user explicitly asks for fixes.
```
