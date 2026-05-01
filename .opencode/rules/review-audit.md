# Iris Review and Audit Rules

## Read-Only By Default

`/review`, `/architecture-review`, and `/audit` are read-only unless the user explicitly asks for fixes.

Do not edit code, tests, docs, memory, snapshots, or formatting during review/audit.

## Severity

- P0: must fix before merge.
- P1: should fix before merge.
- P2: acceptable backlog.
- Note: observation only.

Severity must be evidence-based.

## Required Evidence

Findings must cite:

- file/symbol/command evidence;
- impact;
- recommended minimal fix.

No generic findings.

## Audit Passes

Formal audit checks:

1. Spec compliance.
2. Test quality.
3. SOLID / architecture quality.
4. Clean code / maintainability.

Audit must include verification evidence and final readiness decision.

## Readiness

Do not approve readiness with unresolved P0/P1 issues or missing required verification.
