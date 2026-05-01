---
description: Run a focused read-only engineering review of the current diff or requested area
agent: reviewer
---

# /review

Use the `iris-engineering` skill.
Use the `iris-review` skill.

Run a focused read-only engineering review.

Review target:

$ARGUMENTS

If the review target is empty, review the current working tree diff and staged diff.

## Hard Rules

Do not implement fixes.  
Do not edit files.  
Do not create files.  
Do not update snapshots.  
Do not accept golden outputs.  
Do not change tests.  
Do not modify documentation.  
Do not update memory files.  
Do not run destructive commands.  
Do not run formatting commands.  
Do not run migrations.  
Do not run package restore/update commands unless explicitly requested.

You may only inspect and reason from the factual context below and from additional read-only inspection if absolutely necessary.

If factual context is missing, empty, or clearly points to the wrong directory, report an evidence gap instead of inventing project state.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`

## Review Scope

Review:

- correctness;
- test quality;
- architecture boundaries;
- SOLID;
- clean code;
- maintainability;
- error handling;
- security/safety when relevant;
- documentation/memory impact when relevant;
- verification evidence.

## Difference from `/audit`

`/review` is a focused engineering review.

Use it for:

- quick feedback;
- pre-audit review;
- local diff review;
- checking whether implementation is directionally correct;
- finding issues before formal audit.

Use `/audit` for:

- final readiness decision;
- formal four-pass report;
- merge/blocker classification;
- release-quality evidence review.

## Severity Rules

Use:

- P0 — must fix before merge.
- P1 — should fix before merge.
- P2 — acceptable backlog.
- Note — observation only.

Do not inflate or soften severity without evidence.

## Output Format

# Review Result: <Task Name>

## 1. Summary

### Review Status

Passed / Passed with P2 notes / Blocked by P1 issues / Blocked by P0 issues / Partial / Evidence insufficient

### Readiness

Ready / Ready after P2 backlog / Not ready / Cannot determine

### High-Level Result

<brief outcome>

## 2. Context Reviewed

- Spec:
- Design:
- Plan:
- Git status:
- Git diff:
- Source files:
- Test files:
- Verification evidence:

## 3. Findings

### P0 — Must Fix

No P0 issues.

### P1 — Should Fix

No P1 issues.

### P2 — Backlog

No P2 issues.

### Notes

- ...

## 4. Review Passes

### Correctness

- ...

### Test Quality

- ...

### Architecture / SOLID

- ...

### Clean Code / Maintainability

- ...

### Security / Safety

- ...

### Documentation / Memory

- ...

## 5. Verification Evidence

| Command | Result | Notes |
|---|---|---|
| `...` | Passed / Failed / Skipped / Not Available | ... |

### Verification Gaps

- ...

## 6. Suggested Fix Order

No fixes required.

## 7. Final Decision

Approved / Approved with P2 backlog / Changes requested / Blocked / Cannot determine from available evidence

## Execution Note

No fixes were implemented.
No files were modified.

## Finding Format

For every non-trivial finding, use:

#### <Severity>-<Number>: <Title>

- Evidence:
  - `<path>` / `<symbol>` / `<test>` / `<command>`
- Impact:
  - ...
- Recommended fix:
  - ...

If the review cannot be completed, respond with:

# Review Blocked

## Reason

<reason>

## What Was Checked

- ...

## Evidence Gap

- ...

## Safe Next Step

- ...

