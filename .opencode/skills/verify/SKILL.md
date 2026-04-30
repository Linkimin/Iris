
---
name: verify
description: Run and report repository verification after implementation. Use to validate build, tests, formatting, architecture rules, documentation checks, and task-specific acceptance criteria.
compatibility: opencode
metadata:
  workflow_stage: verification
  output_type: verification_report
---

# Verify Skill

## Purpose

Use this skill to verify repository state after implementation or before audit.

Verification proves whether the current repository state satisfies the expected technical checks.

This skill may run diagnostic commands, but it must not silently fix issues, rewrite code, or change scope.

## When to Use

Use this skill when:

- implementation has been completed;
- the user asks to verify changes;
- build/test status is unknown;
- an audit needs factual verification evidence;
- a refactor may have broken behavior;
- dependency or project reference changes were made;
- persistence, migrations, configuration, or integration behavior changed.

Do not use this skill as a substitute for implementation or audit.

## Required Context

Before running verification, inspect:

1. `AGENTS.md`
2. Relevant `.opencode/rules/*.md`
3. Approved spec/design/plan if present
4. Current git status
5. Current git diff summary
6. Repository type and build system
7. Existing test projects
8. Existing CI/build scripts if relevant
9. Existing architecture tests if present
10. Existing formatting/linting configuration if present

Do not assume commands. Prefer repository conventions.

## Core Rules

Hard rules live in `.opencode/rules/verification.md` and `.opencode/rules/dotnet.md`.

### 1. Verify, Don't Fix

Verification must not modify files. Run build, tests, format checks in verify mode, dependency inspection, and diff inspection. Do not edit files, apply mutating formatters, update snapshots, or delete tests.

### 2. Use Repository Commands

Prefer commands from repo conventions (README, CI, build scripts, .sln/.slnx). For .NET: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`. Use narrower commands first for large repos.

### 3. Report Honestly

Report exact commands and results. Never say "tests pass" if only build ran. Never say "verified" when checks were skipped. Classify failures: build, test, architecture, formatting, environment, flaky, pre-existing. If verification is partial, say so.

### 4. No Destruction

Never run `git push`, `git clean`, `git reset --hard`, `rm -rf`, `docker system prune`, or destructive database commands.

## Verification Procedure

Follow this sequence unless repository conventions require a different order:

1. Read relevant rules and task artifacts.
2. Run or inspect `git status`.
3. Inspect changed files or diff summary.
4. Identify the repository build/test system.
5. Identify targeted verification commands.
6. Run the narrowest useful checks first.
7. Run broader checks if narrow checks pass or if the user requested full verification.
8. Record exact commands and outcomes.
9. Check whether verification modified files.
10. Summarize results and next fix order.

## Output Format

Use this exact structure unless the user explicitly requests another format.

```md
# Verification Result

## 1. Summary

State whether verification passed, failed, or was partial.

Use one of:

- Passed
- Failed
- Partial
- Not Run

## 2. Repository State

### Git Status

Summarize current git state.

### Changed Files Reviewed

- `<path>` — <reason or change type>

## 3. Commands Run

| Command | Result | Notes |
|---|---|---|
| `...` | Passed / Failed / Skipped | ... |

## 4. Build Verification

### Result

Passed / Failed / Skipped

### Evidence

- ...

## 5. Test Verification

### Result

Passed / Failed / Skipped

### Evidence

- ...

### Failed Tests

If any:

- `<test name>` — <failure summary>

## 6. Architecture / Dependency Verification

### Result

Passed / Failed / Skipped / Not Applicable

### Evidence

- ...

## 7. Formatting / Lint Verification

### Result

Passed / Failed / Skipped / Not Applicable

### Evidence

- ...

## 8. Acceptance Criteria Verification

| Criterion | Status | Evidence |
|---|---|---|
| ... | Verified / Failed / Not Verified / N/A | ... |

If no spec/plan acceptance criteria were available, state that explicitly.

## 9. Files Modified During Verification

- None

If verification changed files, list them and explain why.

## 10. Failure Analysis

If verification failed, explain:

- likely root cause;
- whether failure appears related to current changes;
- minimum next fix.

If verification passed, write:

No verification failures found.

## 11. Suggested Fix Order

If failures exist, list the safest order to address them.

If no failures exist, write:

No fixes required.

## 12. Verification Limits

State anything not verified.

Examples:

- manual UI behavior;
- external provider availability;
- real database migration;
- production configuration;
- OS-specific behavior;
- performance;
- concurrency;
- security hardening.
```

## Quality Checklist

Before finalizing verification, confirm:

* [ ] Repository state was inspected.
* [ ] Changed files were reviewed or summarized.
* [ ] Commands were selected from repository conventions where possible.
* [ ] Exact commands are reported.
* [ ] Build result is reported.
* [ ] Test result is reported.
* [ ] Architecture/dependency checks are reported if present.
* [ ] Formatting/lint checks are reported if relevant.
* [ ] Acceptance criteria are mapped if available.
* [ ] Files modified during verification are reported.
* [ ] Failures are not hidden.
* [ ] Partial verification is clearly labeled.
* [ ] No destructive commands were run.
* [ ] No code was changed unless explicitly requested.

## Anti-Patterns

Avoid:
- saying "looks good" without running checks;
- claiming tests passed when only build ran;
- running mutating formatters without permission;
- updating snapshots during verification;
- deleting failing tests;
- treating environment failures as success;
- calling failures pre-existing without evidence;
- hiding files changed by verification.

## Final Response Requirements

When using this skill, final response must include:

1. verification status;
2. exact commands run;
3. build/test/format results;
4. acceptance criteria status if available;
5. files modified during verification;
6. failures and suggested fix order;
7. verification limits.

Do not modify implementation files unless the user explicitly asks to fix discovered issues.

```

