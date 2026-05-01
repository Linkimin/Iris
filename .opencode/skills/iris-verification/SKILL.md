---
name: iris-verification
description: Use when Iris work needs build, test, format, architecture, manual smoke, or documentation/config validation evidence without silently fixing files.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: verification
  output_type: verification_guidance
---

# Iris Verification Skill

## Purpose

Use this skill to verify Iris work with factual evidence.

Verification proves state. It does not silently repair files, rewrite tests, accept snapshots, or update memory.

Hard rules live in `.opencode/rules/verification.md` and `.opencode/rules/dotnet.md`.

## When To Use

Use this skill:

- after implementation;
- before audit;
- when build/test status is unknown;
- after project reference, DI, persistence, model gateway, or UI wiring changes;
- when a user asks whether the repository is ready.

## Required Context

Inspect:

- current git status;
- changed files;
- relevant spec/design/plan when available;
- solution and project files;
- test projects;
- existing CI or build docs when relevant.

Do not assume success from previous sessions.

## Command Selection

For Iris, prefer:

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

Use narrower commands first when the change is local:

- one project build for isolated project changes;
- one test project for isolated test changes;
- solution-level build/test before final readiness claims.

If `.slnx` is unsupported by local tooling, fall back to project-level or `.sln` commands and state the fallback.

## Verification Matrix

| Change type | Minimum useful checks | Broader readiness checks |
|---|---|---|
| `.opencode` docs/skills/rules | parse config, grep invariants, inspect diff | `dotnet build`, `dotnet test` if branch completion |
| C# Domain | domain tests | solution build/test |
| C# Application | application tests with fakes | solution build/test, architecture tests |
| Persistence | repository/integration tests | solution build/test, migration/schema review |
| ModelGateway | mapper/client tests or controlled manual provider check | solution build/test |
| Desktop UI | ViewModel tests, build | manual smoke, solution tests |
| Project references/DI | build, architecture tests/reference checks | solution test |
| Formatting-only | format verify | build/test if source changed |

Do not use the same checklist for every task. Match checks to risk.

## Non-Mutating Rule

Verification may run:

- build;
- tests;
- format checks in verify mode;
- dependency/reference inspection;
- read-only diff inspection.

Verification must not:

- edit files;
- run mutating formatters;
- update snapshots;
- delete tests;
- apply migrations to non-disposable databases;
- push or commit.

## File Change Detection

Before and after verification, inspect:

```powershell
git status --short
git diff --name-status
```

If verification changed files:

- list them;
- explain why;
- do not hide generated or formatted changes;
- do not stage them automatically.

## Reporting Exact Commands

Every report must include:

- exact command;
- result: passed, failed, skipped, partial;
- short evidence summary;
- whether files changed during verification.

Never say “tests pass” if only build ran.

Never say “verified” when the check was skipped.

## Report Vocabulary

Use these words precisely:

- Passed: command ran and exited successfully.
- Failed: command ran and exited unsuccessfully.
- Skipped: command was intentionally not run.
- Partial: some required checks could not be run.
- Not applicable: check does not apply to this change type.

Avoid:

- "looks good" without command evidence;
- "should pass";
- "probably unrelated" without prior evidence;
- "all verified" when manual gaps remain.

## Failure Classification

Classify failures as:

- build failure;
- test failure;
- architecture/dependency failure;
- formatting failure;
- environment/tooling failure;
- missing dependency/service;
- suspected flaky/timeout;
- pre-existing only when evidence proves it.

For each failure, report:

- likely cause;
- whether it appears related to current diff;
- minimum safe next fix.

## Failure Pattern Playbook

| Symptom | Likely class | First response |
|---|---|---|
| compile error in changed file | Build failure | inspect exact error, fix changed code |
| test assertion changed behavior | Test failure | understand behavior, do not weaken test |
| file lock during build/test | Environment/tooling | rerun sequentially once, record if resolved |
| provider unavailable | Missing service | report controlled manual gap |
| format verify fails | Formatting failure | do not run mutating formatter unless asked |
| architecture test fails | Boundary failure | inspect reference/source dependency |
| `.slnx` unsupported | Tooling fallback | use supported project/solution command |

Repeated failure means stop and report, not random command hopping.

## Architecture Verification

For boundary-sensitive work, include reference checks:

```powershell
dotnet list .\src\Iris.Domain\Iris.Domain.csproj reference
dotnet list .\src\Iris.Application\Iris.Application.csproj reference
```

Use architecture tests when available.

For `.opencode`-only work, architecture verification means confirming commands/scripts/plugins outside scope were not modified.

## Documentation And Config Verification

For `.opencode` work, use checks such as:

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"
Get-ChildItem .\.opencode\skills, .\.opencode\rules -Recurse -File -Include *.md |
  Select-String -Pattern @('\.agents/' + 'local_notes','\.agents\\' + 'local_notes')
git diff --name-only -- .opencode/commands
```

Expected:

- config parses;
- forbidden legacy naming does not appear where prohibited;
- out-of-scope command files are unchanged.

## Manual Verification

Some behavior cannot be proven automatically:

- desktop click-through;
- Ollama running/stopped UX;
- visual layout;
- voice/perception hardware behavior.

Mark these as manual gaps instead of pretending they passed.

## Manual Smoke Template

```markdown
## Manual Verification Gap

- Scenario:
- Why automated check is insufficient:
- Required human action:
- Expected result:
- Risk if skipped:
```

## Output Expectations

Verification output should include:

- summary status;
- repository state;
- commands run;
- build result;
- test result;
- architecture/dependency result;
- format/lint result;
- files changed during verification;
- acceptance criteria mapping;
- verification limits.

## Output Template

```markdown
# Verification Result

## Summary
- Passed / Failed / Partial / Not Run

## Commands
| Command | Result | Evidence |
|---|---|---|
| `...` | Passed | ... |

## Files Modified During Verification
- None

## Failures
- None

## Manual Gaps
- ...

## Final Verification Decision
- ...
```

## Stop Conditions

Stop and report when:

- the repository root is wrong;
- build tooling is unavailable;
- a verification command would mutate files;
- repeated failures have the same cause;
- a required external service is unavailable and no fallback is defined.

## Pressure Scenarios

### Scenario 1: Build passed, tests not run

Expected:

- report build passed;
- report tests skipped/not run;
- do not say "verification passed" unless build-only was sufficient.

### Scenario 2: `dotnet format` would fix files

Expected:

- run `--verify-no-changes`;
- if it fails, report;
- do not run mutating format unless user asks.

### Scenario 3: Manual Desktop smoke not performed

Expected:

- name manual gap;
- report automated checks separately;
- do not claim UI behavior verified.

### Scenario 4: File lock failure

Expected:

- classify as environment/tooling;
- rerun sequentially once if safe;
- record original failure if meaningful.

## Quality Checklist

- [ ] Git state was inspected.
- [ ] Changed files were summarized.
- [ ] Commands were selected from repo conventions.
- [ ] Exact commands and results are reported.
- [ ] Skipped checks are explicit.
- [ ] Failures include likely cause and next fix.
- [ ] Files modified by verification are reported.
- [ ] Manual gaps are named.

## Anti-Patterns

Avoid:

- using verification to fix implementation;
- running broad checks before obvious targeted checks;
- hiding environment failures;
- labeling failures pre-existing without evidence;
- reporting success from stale command output.

## Self-Test Checklist

- Did I choose checks based on risk?
- Did I report exact commands?
- Did I separate automatic and manual verification?
- Did I inspect whether verification changed files?
- Did I avoid mutating fix commands?
- Did I make the final decision match the evidence?
