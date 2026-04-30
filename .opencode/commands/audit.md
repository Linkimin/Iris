---
description: Run a structured four-pass engineering audit and report merge/readiness status
agent: auditor
---

# /audit

Use the `iris-engineering` skill.
Use the `iris-review` skill.
Use the `audit` skill.

Run a structured four-pass engineering audit for:

$ARGUMENTS

If the audit target is empty, audit the current working tree diff and staged diff.

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
Do not approve if verification evidence is missing or insufficient.

Inspect evidence before making a readiness decision.  
Report missing evidence explicitly.  
Use `Cannot determine from available evidence` when required context is missing.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-review/SKILL.md,.opencode/skills/audit/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`

## Required Context

Before auditing, consider:

- `AGENTS.md`;
- `.opencode/skills/audit/SKILL.md`;
- `.opencode/skills/architecture-boundary-review/SKILL.md`;
- relevant `.opencode/rules/*.md`;
- approved specification if present;
- approved architecture design if present;
- approved implementation plan if present;
- verification report if present;
- current git status;
- current git diff;
- project/solution structure;
- project reference files;
- changed source files;
- changed test files;
- changed documentation files;
- `.agent/overview.md` or `.agents/overview.md` if present;
- `.agent/PROJECT_LOG.md` or `.agents/PROJECT_LOG.md` if present;
- `.agent/local_notes.md`, `.agent/log_notes.md`, `.agents/local_notes.md`, or `.agents/log_notes.md` if present;
- `.agent/mem_library/**` or `.agents/mem_library/**` if relevant.

## Required Audit Passes

Run exactly these four main passes:

1. Spec Compliance
2. Test Quality
3. SOLID / Architecture Quality
4. Clean Code / Maintainability

Additional sections may be included only when relevant:

- Security / Privacy
- Performance
- Reliability
- Documentation / Memory
- Migration / Rollback
- Developer Experience

## Severity Rules

Use:

- P0 — must fix before merge.
- P1 — should fix before merge.
- P2 — acceptable backlog.
- Note — observation only.

Do not inflate or soften severity without evidence.

## Approval Rules

Approve only if:

- no P0 issues exist;
- no P1 issues exist;
- verification evidence is sufficient;
- architecture boundaries are preserved;
- tests are adequate for changed behavior;
- scope matches the spec/request.

Use `Cannot determine from available evidence` if required context or verification is missing.

## Output Format

# Formal Audit Report: <Task Name>

## 1. Summary

### Audit Status

Passed / Passed with P2 notes / Blocked by P1 issues / Blocked by P0 issues / Partial / Evidence insufficient

### Final Decision

Approved / Approved with P2 backlog / Changes requested / Blocked / Cannot determine from available evidence

### High-Level Result

<brief outcome>

## 2. Context Reviewed

- Specification:
- Design:
- Implementation plan:
- Git status:
- Git diff:
- Source files:
- Test files:
- Documentation/memory:
- Verification evidence:

## 3. Pass 1 — Spec Compliance

### Result

Passed / Failed / Partial

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

### Result

Passed / Failed / Partial

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

### Result

Passed / Failed / Partial

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

### Result

Passed / Failed / Partial

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

Include only relevant subsections.

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

### Developer Experience

- ...

## 8. Verification Evidence

| Command | Result | Notes |
|---|---|---|
| `...` | Passed / Failed / Skipped / Not Available | ... |

### Verification Gaps

- ...

## 9. Consolidated Findings

### P0 — Must Fix

No P0 issues.

### P1 — Should Fix

No P1 issues.

### P2 — Backlog

No P2 issues.

## 10. Suggested Fix Order

No fixes required.

## 11. Readiness Decision

Ready / Ready with P2 backlog / Not ready / Blocked / Cannot determine

## Execution Note

No fixes were implemented.
No files were modified.

## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | Reviewed / ⚠️ Missing | `<path>` or "Not available" |
| B — Design | Reviewed / ⚠️ Missing | `<path>` or "Not available" |
| C — Plan | Reviewed / ⚠️ Missing | `<path>` or "Not available" |
| D — Verify | Reviewed / ⚠️ Missing | Verification evidence section above |
| E — Architecture Review | In audit / ⚠️ Not run | Architecture pass findings above |
| F — Audit | ✅ Satisfied | This audit |
| G — Memory | Checked / ⚠️ Not updated | Memory review above |

Each gate that was checked should cite the relevant spec/design/plan/verify/architecture-review path. Missing gates should be flagged as ⚠️.

## Finding Format

For every non-trivial finding, use:

#### <Severity>-<Number>: <Title>

- Evidence:
  - `<path>` / `<symbol>` / `<test>` / `<command>`
- Impact:
  - ...
- Recommended fix:
  - ...

If audit cannot be completed, respond with:

# Audit Blocked

## Reason

<reason>

## What Was Checked

- ...

## Evidence Gap

- ...

## Safe Next Step

- ...
