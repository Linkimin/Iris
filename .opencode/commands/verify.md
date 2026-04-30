---
description: Run repository verification and report build, test, format, lint, architecture, and acceptance status
agent: builder
---

# /verify

Use the `iris-engineering` skill.
Use the `iris-verification` skill.
Use the `verify` skill.

Verify the current repository state for:

$ARGUMENTS

If the verification target is empty, verify the current working tree state.

## Hard Rules

Do not implement fixes.  
Do not edit source files.  
Do not edit tests.  
Do not edit documentation.  
Do not update snapshots.  
Do not accept golden outputs.  
Do not change configuration.  
Do not update memory files.  
Do not run destructive commands.  
Do not run migrations unless explicitly requested and scoped to a disposable test database.

Safe verification commands are allowed even if they produce ordinary build/test artifacts such as `bin/`, `obj/`, coverage output, caches, or test result files.

After verification, inspect whether tracked files changed.

Do not hide partial verification.  
If a command was skipped, explain why.  
If repository context is missing or points to the wrong directory, stop and report the evidence gap.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-verification/SKILL.md,.opencode/skills/verify/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Build and Test Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Verification Execution

Run safe verification commands yourself after reading the injected context above.

Use repository-specific commands when they are obvious from scripts, CI, `AGENTS.md`, or `.opencode/skills/verify/SKILL.md`.

For .NET repositories, prefer this order:

1. `dotnet --info`
2. `dotnet build <solution-or-project>`
3. `dotnet test <solution-or-test-project>`
4. `dotnet format <solution-or-project> --verify-no-changes`

Use narrower commands first when the repository is large or when `$ARGUMENTS` names a specific area.

For the Iris repository, prefer the solution-level command when available:

- `dotnet build .\Iris.slnx`
- `dotnet test .\Iris.slnx`
- `dotnet format .\Iris.slnx --verify-no-changes`

If `.slnx` is not supported by the installed SDK or tooling, fall back to `.sln` or project-level verification and state that fallback explicitly.

After running verification, run:

- `git status --short`
- `git diff --name-status`
- `git diff --stat`

Use that final state to report whether verification changed tracked files.

## Forbidden Commands

Do not run:

- `git push`
- `git clean`
- `git reset --hard`
- `rm -rf`
- `docker system prune`
- destructive database commands
- snapshot/golden update commands
- package update commands
- migration apply commands against non-disposable databases

## Verification Scope

Verify:

- build/compile status;
- test status;
- architecture/dependency test status when present;
- formatting/linting status when available in no-write mode;
- acceptance criteria when spec/design/plan are present;
- whether verification modified tracked files;
- whether failures are related to current changes.

## Output Format

# Verification Result

## 1. Summary

Status: Passed / Failed / Partial / Not Run

High-level result:

- ...

## 2. Repository State

### Git Status

- ...

### Changed Files Reviewed

- `<path>` — <reason or change type>

## 3. Commands Run

| Command | Result | Notes |
|---|---|---|
| `...` | Passed / Failed / Skipped | ... |

## 4. Build Verification

Result: Passed / Failed / Skipped

Evidence:

- ...

## 5. Test Verification

Result: Passed / Failed / Skipped

Evidence:

- ...

Failed tests:

- ...

## 6. Architecture / Dependency Verification

Result: Passed / Failed / Skipped / Not Applicable

Evidence:

- ...

## 7. Formatting / Lint Verification

Result: Passed / Failed / Skipped / Not Applicable

Evidence:

- ...

## 8. Acceptance Criteria Verification

| Criterion | Status | Evidence |
|---|---|---|
| ... | Verified / Failed / Not Verified / N/A | ... |

If no acceptance criteria were available, state that explicitly.

## 9. Files Modified During Verification

- None / `<path>` — <why it changed>

## 10. Failure Analysis

If verification failed:

- likely root cause;
- whether the failure appears related to current changes;
- minimum next fix.

If verification passed:

No verification failures found.

## 11. Suggested Fix Order

If failures exist, list the safest order to address them.

If no failures exist:

No fixes required.

## 12. Verification Limits

- ...

## Gate Status

After verification completes, append:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ⬜ Not checked | This command does not check Gate A |
| B — Design | ⬜ Not checked | This command does not check Gate B |
| C — Plan | ⬜ Not checked | This command does not check Gate C |
| D — Verify | ✅ Satisfied / ⚠️ Skipped / ❌ Failed | See verification results above |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |

## Execution Note

No fixes were implemented.
No source files were modified intentionally.

## If Verification Cannot Run

# Verification Not Run

## Reason

<reason>

## What Was Checked

- ...

## Evidence Gap

- ...

## Safe Next Step

- ...
