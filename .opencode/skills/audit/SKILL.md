
---
name: audit
description: Run a structured engineering audit after implementation or before merge. Use to review spec compliance, test quality, architecture/SOLID, clean code, maintainability, security-sensitive risks, and verification evidence.
compatibility: opencode
metadata:
  workflow_stage: audit
  output_type: audit_report
---

# Audit Skill

## Purpose

Use this skill to perform a structured engineering audit of completed or proposed changes.

An audit determines whether the implementation is safe, correct, architecture-compliant, testable, maintainable, and ready for merge or next iteration.

This skill must not implement fixes unless the user explicitly asks for fixes after the audit.

## When to Use

Use this skill when:

- implementation is complete;
- verification has been run;
- the user asks for review;
- a merge/readiness decision is needed;
- architecture-sensitive changes were made;
- public contracts changed;
- persistence, security, permissions, provider integration, or background behavior changed;
- tests were added or modified;
- the diff is large enough to require structured review.

Do not use this skill for:

- creating a spec;
- creating a design;
- creating an implementation plan;
- saving documentation;
- making code changes directly;
- trivial typo-only changes unless the user explicitly requests an audit.

## Required Context

Before auditing, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Approved specification if present
4. Approved design if present
5. Approved implementation plan if present
6. Verification report if present
7. Current git status
8. Current git diff
9. Changed source files
10. Changed test files
11. Changed documentation files
12. Existing architecture documentation
13. `.agent/overview.md` if present, falling back to `.agents/overview.md`
14. `.agent/PROJECT_LOG.md` if present, falling back to `.agents/PROJECT_LOG.md`
15. `.agent/log_notes.md` if present, falling back to existing `local_notes.md`

Do not ask the user a question until relevant project context has been inspected.

Ask only if a blocking ambiguity prevents a fair audit.

## Core Rules

Hard rules live in `.opencode/rules/review-audit.md`, `.opencode/rules/iris-architecture.md`, and `.opencode/rules/no-shortcuts.md`.

### 1. Read-Only

The audit is read-only. Inspect files, diffs, tests, and documentation. Run safe verification if needed. Do not edit source, tests, docs, config, snapshots, or golden outputs. Do not commit or run destructive commands.

### 2. Four Required Passes

Every audit must include: (1) Spec Compliance, (2) Test Quality, (3) SOLID / Architecture Quality, (4) Clean Code / Maintainability.

### 3. Evidence-Based Severity

Findings must cite file/symbol/command evidence, impact, and recommended fix. Classify: P0 (must fix: correctness, data loss, security, broken build, architecture break), P1 (should fix: high-risk maintainability, incomplete tests, risky coupling), P2 (backlog: cleanup, naming, minor gaps), Note (observation only).

### 4. Check Architecture and Tests

Check for boundary violations (UI→persistence, UI→providers, Domain→infrastructure, Application→concrete adapters, Tools owning policy, Voice owning orchestration, Perception owning memory, hosts owning business logic, Shared containing product behavior). Assess tests for behavior coverage, meaningful assertions, positive/negative cases, and correct level. Test existence is not test quality.

### 5. Check Spec/Design/Plan Compliance

If spec/design/plan exist, audit against them: scope, non-goals, acceptance criteria, architecture constraints, contract compatibility, forbidden changes. If verification was not run, the audit must say so.

## Audit Procedure

Follow this sequence unless the user asks for a narrower audit:

1. Inspect repository rules.
2. Inspect spec/design/plan if present.
3. Inspect git status.
4. Inspect git diff summary.
5. Inspect changed source files.
6. Inspect changed tests.
7. Inspect verification evidence or run safe verification if appropriate.
8. Run four audit passes.
9. Classify findings.
10. Decide merge/readiness status.
11. List next actions in priority order.

## Output Format

Use this exact structure unless the user explicitly requests another format.

```md
# Audit Report: <Task Name>

## 1. Summary

### Audit Status

Use one:

- Passed
- Passed with P2 notes
- Blocked by P1 issues
- Blocked by P0 issues
- Partial / Evidence insufficient

### Merge Readiness

Use one:

- Ready
- Ready after P2 backlog
- Not ready
- Cannot determine

### High-Level Result

Briefly summarize the outcome.

## 2. Context Reviewed

List what was reviewed.

- Specification:
- Design:
- Plan:
- Verification:
- Git diff:
- Source files:
- Test files:
- Docs/memory:

If something was unavailable, state that explicitly.

## 3. Pass 1 — Spec Compliance

Check implementation against scope, non-goals, acceptance criteria, contracts, and forbidden changes.

### Findings

#### P0

- None / issues

#### P1

- None / issues

#### P2

- None / issues

#### Notes

- ...

## 4. Pass 2 — Test Quality

Check test relevance, behavior coverage, negative paths, regression protection, and meaningful assertions.

### Findings

#### P0

- None / issues

#### P1

- None / issues

#### P2

- None / issues

#### Notes

- ...

## 5. Pass 3 — SOLID / Architecture Quality

Check dependency direction, layer ownership, abstraction quality, coupling, and extension pressure.

### Findings

#### P0

- None / issues

#### P1

- None / issues

#### P2

- None / issues

#### Notes

- ...

## 6. Pass 4 — Clean Code / Maintainability

Check readability, naming, method/class size, duplication, hidden side effects, operational clarity, and long-term maintainability.

### Findings

#### P0

- None / issues

#### P1

- None / issues

#### P2

- None / issues

#### Notes

- ...

## 7. Additional Risk Checks

Include only relevant sections.

### Security / Privacy

- ...

### Performance

- ...

### Reliability

- ...

### Documentation / Memory

- ...

### Migration / Rollback

- ...

## 8. Verification Evidence

### Commands Reviewed or Run

| Command | Result | Notes |
|---|---|---|
| `...` | Passed / Failed / Skipped / Not Available | ... |

### Verification Gaps

- ...

## 9. Consolidated Findings

### P0 — Must Fix

For each issue:

```md
#### P0-001: <Title>

- Evidence:
- Impact:
- Recommended fix:
````

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

## 10. Suggested Fix Order

List the safest order to address findings.

If no fixes are required, write:

No fixes required.

## 11. Final Decision

Use one:

* Approved
* Approved with P2 backlog
* Changes requested
* Blocked
* Cannot determine from available evidence

Explain briefly.

````

## Finding Format

Use this format for every non-trivial issue:

```md
#### <Severity>-<Number>: <Short Title>

- Evidence:
  - `<path>` / `<symbol>` / `<test>` / `<command output>`
- Impact:
  - ...
- Recommended fix:
  - ...
````

## Quality Checklist

Before finalizing the audit, verify:

* [ ] Repository rules were considered.
* [ ] Spec/design/plan were considered if available.
* [ ] Git status or diff was inspected.
* [ ] Source changes were reviewed.
* [ ] Test changes were reviewed.
* [ ] Verification evidence was reviewed.
* [ ] Four required passes are present.
* [ ] Findings are evidence-based.
* [ ] Severities are justified.
* [ ] Architecture boundaries were checked.
* [ ] Test quality was assessed beyond test existence.
* [ ] Documentation/memory impact was checked.
* [ ] Merge/readiness decision is explicit.
* [ ] Next actions are prioritized.
* [ ] No fixes were made unless explicitly requested.

## Anti-Patterns

Avoid:
- rubber-stamp approval or vague review comments;
- severity inflation or minimization;
- ignoring missing verification;
- treating compile success as correctness or test existence as quality;
- auditing only style while missing architecture;
- proposing broad rewrites as first fix;
- inventing requirements not in the spec;
- hiding uncertainty;
- editing files during audit.

## Final Response Requirements

When using this skill, final response must include:

1. audit status;
2. merge/readiness decision;
3. four audit passes;
4. P0/P1/P2 findings;
5. verification evidence or verification gap;
6. suggested fix order;
7. explicit statement that no fixes were implemented unless the user requested them.

Do not modify files unless the user explicitly asks for fixes.

```

