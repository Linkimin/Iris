---
name: iris-review
description: Use when Iris work needs focused review, architecture review, formal audit, severity classification, readiness decisions, or evidence-based findings.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: review
  output_type: review_guidance
---

# Iris Review Skill

## Purpose

Use this skill to review Iris work without silently changing files.

It covers focused review, architecture-review readiness, and formal audit readiness. For deep boundary checks, combine with `iris-architecture`.

## Review Types

### `/review`

Focused read-only engineering review.

Use for:

- current diff feedback;
- implementation correctness;
- test quality;
- maintainability;
- finding issues before audit.

### `/architecture-review`

Focused read-only boundary review.

Use for:

- project reference changes;
- DI/composition changes;
- new abstractions;
- adapter/host/application boundary changes.

### `/audit`

Formal readiness decision.

Use for:

- final review before merge;
- release/readiness judgment;
- checking spec/design/plan/implementation/verification evidence.

## Which Review To Run

| Situation | Use | Output decision |
|---|---|---|
| "Review this diff" | `/review` | findings and fix order |
| "Check architecture" | `/architecture-review` | boundary readiness |
| "Is this ready to merge?" | `/audit` | merge/readiness decision |
| "Find issues before I continue" | `/review` | directional issues |
| "Did we follow the plan?" | `/audit` if formal, `/review` if quick | compliance result |
| "Are project references okay?" | `/architecture-review` | dependency decision |

If the user says "review and fix", review first, then implement only if fixing is clearly authorized and a plan/scope exists.

## Required Context

Inspect:

- request or task target;
- current git status and diff;
- relevant spec/design/plan;
- changed source and tests;
- verification evidence;
- `.opencode/rules/iris-architecture.md`;
- `.opencode/rules/no-shortcuts.md`;
- `.opencode/rules/verification.md`;
- `.agent` memory when project state matters.

Do not ask for context that can be discovered locally.

## Read-Only Rule

Review and audit do not:

- edit code;
- edit tests;
- run mutating formatters;
- update snapshots;
- update memory;
- create commits;
- fix findings.

They may run read-only inspection and verification commands when appropriate.

## Evidence Hierarchy

Use stronger evidence first:

1. Exact changed lines or symbols.
2. Failing/passing command output.
3. Project reference or dependency evidence.
4. Spec/design/plan acceptance criteria.
5. Architecture/memory/project rules.

Avoid findings based only on taste. If evidence is weak, write a Note or open question.

## Severity

Use:

- P0: must fix before merge; correctness/security/data loss/build-breaking/architecture-breaking.
- P1: should fix before merge; likely defect, significant test gap, real coupling risk.
- P2: acceptable backlog; minor maintainability/doc/test organization improvement.
- Note: observation with no required action.

Do not inflate severity without evidence.

## Severity Decision Table

| Finding type | Typical severity |
|---|---|
| Build broken | P0 |
| Tests failing due to current diff | P0/P1 |
| Domain depends on infrastructure | P0 |
| Application references concrete adapter | P0 |
| UI bypasses Application for core workflow | P0/P1 |
| Data loss or privacy leak risk | P0 |
| Missing test for changed risky behavior | P1 |
| Missing architecture test after boundary change | P1/P2 |
| Minor naming/folder inconsistency | P2 |
| Optional cleanup | Note/P2 |

Severity is about merge risk, not how annoying the issue is.

## Finding Requirements

Every non-trivial finding must include:

- title;
- severity;
- evidence with file, symbol, command, or diff reference;
- impact;
- recommended minimal fix.

Avoid generic advice. If evidence is missing, mark the review partial.

## Finding Template

```markdown
#### P1-001: <Title>

- Evidence:
  - `<path>` / `<symbol>` / `<command>`
- Impact:
  - ...
- Recommended fix:
  - ...
```

Good finding:

```markdown
#### P1-001: Verification omits integration tests for persistence change

- Evidence:
  - `src/Iris.Persistence/...` changed, but only `Iris.Domain.Tests` was run.
- Impact:
  - Repository/schema regressions may merge undetected.
- Recommended fix:
  - Run persistence integration tests or `dotnet test .\Iris.slnx --no-restore`.
```

Bad finding:

```markdown
Tests seem light.
```

Why bad: no evidence, no impact, no fix.

## Audit Passes

Formal audit uses four passes:

1. Spec compliance.
2. Test quality.
3. SOLID / architecture quality.
4. Clean code / maintainability.

Audit also checks:

- verification evidence;
- memory/doc impact;
- unresolved P0/P1 findings;
- merge readiness.

## Audit Readiness Checklist

Before approving:

- spec or approved scope is known;
- design exists for boundary-sensitive work;
- plan exists for multi-file implementation;
- implementation matches plan or deviations are justified;
- tests are meaningful for changed behavior;
- verification commands are exact and recent;
- P0/P1 findings are resolved or intentionally blocked;
- memory/doc updates are handled if required;
- manual gaps are named.

If any item is missing, audit can still be useful, but final decision should be partial or blocked.

## Architecture Review Checks

For boundary-sensitive changes, check:

- dependency direction;
- project references;
- DI ownership;
- adapter boundaries;
- host boundaries;
- Shared neutrality;
- forbidden shortcuts.

Use `iris-architecture` for detailed method.

## Review Pass Playbooks

### Correctness

Ask:

- Does the change satisfy the requested behavior?
- Are edge cases and failure paths handled?
- Are partial-state and cancellation cases considered?
- Are controlled errors used where expected?

### Test Quality

Ask:

- Do tests fail for the right reason before the fix when applicable?
- Do tests assert behavior, not implementation noise?
- Are fakes used at Application boundaries instead of concrete adapters?
- Are integration tests present for real persistence/provider seams?

### Architecture

Ask:

- Did any layer gain a responsibility it should not own?
- Did any project reference violate dependency direction?
- Did hosts remain composition/presentation?
- Did adapters stay technical?

### Maintainability

Ask:

- Are names precise?
- Is duplication meaningful or accidental?
- Are methods/classes still focused?
- Are comments useful rather than decorative?

## Output Expectations

Review output should include:

- status: passed, passed with notes, blocked, partial;
- context reviewed;
- P0/P1/P2/Note findings;
- verification evidence;
- suggested fix order;
- final decision;
- statement that no fixes were implemented.

Audit output must include a readiness decision:

- approved;
- approved with P2 backlog;
- changes requested;
- blocked;
- cannot determine.

## Output Skeleton

```markdown
# Review Result: <target>

## Summary
- Status:
- Readiness:

## Context Reviewed
- ...

## Findings
### P0
- None

### P1
- ...

### P2
- ...

### Notes
- ...

## Verification Evidence
- ...

## Suggested Fix Order
- ...

## Final Decision
- Approved / Changes requested / Blocked / Cannot determine

## Execution Note
- No fixes were implemented.
```

## Stop Conditions

Stop and report partial review when:

- target is unclear;
- git diff cannot be inspected;
- required plan/spec/design is missing for a formal audit;
- verification evidence is absent and cannot be run safely;
- source context is too incomplete for an evidence-based finding.

## Pressure Scenarios

### Scenario 1: Obvious one-line fix during review

Expected:

- report finding only;
- no edit;
- recommend minimal fix.

### Scenario 2: Missing tests but change is docs-only

Expected:

- do not invent a test requirement;
- verify docs/config-specific checks;
- mark .NET tests as optional or branch-completion evidence.

### Scenario 3: Audit requested but no verification exists

Expected:

- run safe verification if allowed, or mark evidence insufficient;
- do not approve readiness without evidence.

### Scenario 4: Architecture concern but no reference evidence

Expected:

- inspect references/source imports;
- if unable, mark partial;
- do not assert violation from naming alone.

## Rationalization Table

| Rationalization | Correct response |
|---|---|
| "I can fix this faster than writing it up." | Review is read-only; findings first. |
| "No tests changed, so test quality is irrelevant." | Check whether changed behavior needs tests. |
| "Architecture looks okay by inspection." | Use project references/imports when relevant. |
| "One P2 means not ready." | P2 can be backlog; P0/P1 block readiness. |
| "Audit can approve with missing verification." | Missing evidence means partial/cannot determine. |

## Quality Checklist

- [ ] Review type is explicit.
- [ ] Relevant diff/context was inspected.
- [ ] Findings include evidence.
- [ ] Severity is justified.
- [ ] Architecture rules were checked when relevant.
- [ ] Verification evidence is reported.
- [ ] Memory/doc impact is considered.
- [ ] No fixes were implemented.

## Anti-Patterns

Avoid:

- mixing review with implementation;
- approving without checking diff;
- vague “looks good” summaries;
- requiring speculative abstractions;
- hiding missing verification;
- updating memory from review unless explicitly requested.

## Self-Test Checklist

- Did I lead with findings?
- Did every issue include evidence?
- Did severity reflect merge risk?
- Did I separate review from fixing?
- Did I report verification evidence and gaps?
- Did final readiness follow from the findings?
