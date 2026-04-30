---
name: iris-engineering
description: Use when doing non-trivial Iris work where workflow stage, repository state, architecture boundaries, verification, or memory-update responsibility could be confused.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: cross_cutting
  output_type: engineering_guidance
---

# Iris Engineering Skill

## Purpose

Use this skill as the central operating method for Iris work in OpenCode.

It coordinates the project workflow, but it does not replace focused skills:

- use `iris-architecture` for boundary-sensitive work;
- use `iris-memory` for memory reads or updates;
- use `iris-verification` for factual verification;
- use `iris-review` for review, architecture-review, and audit readiness.

This skill should make the agent behave consistently without copying all project rules into every command.

It is the starting point for command behavior; focused skills and rule files provide the deeper checks.

## Iris Identity

Iris / Айрис is:

- a local personal AI companion built on .NET 10;
- desktop-first, with a future API and Worker host;
- Clean / Hexagonal by design;
- centered on Domain and Application;
- adapter-driven for persistence, model gateway, tools, voice, perception, and SI runtime;
- local-first and privacy-sensitive;
- developed through small vertical slices, not speculative platform expansion.

Core direction:

```text
Domain + Application define the system.
Adapters implement external technology.
Hosts compose and run the system.
```

## Workflow Stages

Use these stages deliberately:

| Stage | Role | May edit code? | May update memory? |
|---|---|---:|---:|
| `/status` | Report current factual state | No | No |
| `/spec` | Define problem, scope, non-goals, acceptance | No | No |
| `/design` | Define boundaries, contracts, flow, failures | No | No |
| `/plan` | Define executable steps and verification | No | No |
| `/implement` | Execute approved plan | Yes | Only if required after meaningful work |
| `/verify` | Run and report checks | No by default | No |
| `/review` | Focused read-only engineering review | No | No |
| `/architecture-review` | Read-only boundary review | No | No |
| `/audit` | Formal readiness decision | No | No |
| `/update-memory` | Update `.agent` facts after work | Only memory files | Yes |
| `/save-*` | Save a requested artifact | Only target docs/artifact | Only if explicitly allowed |

Do not blur stages. If a request asks for implementation while design is unclear, produce or request the missing design first.

## Stage Selection

When the user request is ambiguous, choose the safest stage from evidence:

| User asks | Use stage | Why |
|---|---|---|
| "What is current state?" | `/status` | factual reporting only |
| "Let's think / decide / scope" | `/spec` or `/design` | no edits yet |
| "Write a plan" | `/plan` | implementation is not authorized |
| "Start / implement / continue" with approved plan | `/implement` | code/config/docs may change |
| "Check / run tests / is it passing?" | `/verify` | evidence only |
| "Review this" | `/review` | findings, no fixes |
| "Does this break architecture?" | `/architecture-review` | boundary-focused findings |
| "Ready to merge?" | `/audit` | final readiness decision |
| "Update memory" | `/update-memory` | memory-only write |
| "Save this spec/design/plan/audit" | `/save-*` | artifact write, not implementation |

If a request combines stages, split them. Example: "review and fix" starts with review findings, then moves into implementation only if fixing is clearly authorized and a safe plan exists.

## Handoff Rules

Each stage must leave the next stage with enough evidence:

- Spec hands Design a fixed scope, non-goals, invariants, acceptance criteria, and open questions.
- Design hands Plan exact boundaries, contracts, files, data flow, failure behavior, and forbidden shortcuts.
- Plan hands Implementation ordered steps, file scope, tests, validation, metadata updates, and rollback expectations.
- Implementation hands Verification changed files, behavior implemented, tests added, deviations, and known risks.
- Verification hands Review/Audit exact commands, pass/fail/skipped status, and manual gaps.
- Audit hands Memory factual outcomes only after work is complete or explicitly requested.

Never use "probably", "should be fine", or "looks safe" as a handoff artifact.

## Required Context

For non-trivial work, inspect:

- `AGENTS.md`;
- relevant `.opencode/rules/*.md`;
- relevant `.opencode/skills/*/SKILL.md`;
- `.agent/overview.md` or `.agents/overview.md`;
- `.agent/architecture.md` for architecture-sensitive work;
- `.agent/first-vertical-slice.md` for first-slice work;
- current git status and changed files.

Use Context7 or official documentation when external library/API behavior matters.

## Context Budgeting

Read enough context to decide safely, but do not flood the prompt:

- For `.opencode` workflow work, read current commands/skills/rules/config and relevant AGENTS guidance.
- For architecture work, read architecture docs and project references before source details.
- For feature work, read the approved spec/design/plan before implementation files.
- For memory updates, read the target memory files before appending.

If context is large, summarize the part used and name the evidence gap.

## Dirty Working Tree Policy

Before editing:

- inspect `git status --short --branch`;
- identify unrelated user changes;
- do not overwrite or revert user changes;
- do not silently work over dirty files outside the requested scope;
- stop if the dirty state makes safe implementation ambiguous.

For IrisEngineering v2 work, `.opencode` and `opencode.jsonc` are in scope. Existing `AGENTS.md` user edits are not in scope unless explicitly requested.

## Dirty State Decision Matrix

| Dirty state | Agent action |
|---|---|
| Only files in requested scope changed | Proceed carefully |
| Unrelated tracked file modified | Avoid touching it; mention it in final |
| Target file has user edits | Read it and work with it, never revert |
| Generated/build output dirty | Identify whether command caused it |
| Conflict between plan and dirty state | Stop and report blocker |

Do not use broad restore/reset/checkout commands.

## File Creation Policy

Before creating a file:

1. Inspect the target folder.
2. Check whether a suitable file or placeholder already exists.
3. Confirm the responsibility belongs to that layer.
4. Confirm the file is required for the current phase.
5. Avoid parallel duplicate abstractions.

Create files only when the plan requires them or the user explicitly asks.

## File Creation Questions

Before creating a file, answer internally:

- What responsibility does this file own?
- Which layer owns that responsibility?
- Is there an existing file, placeholder, or convention for this?
- Will this duplicate an action skill, rule, command, adapter, or memory file?
- Is this required for the current phase?
- What future phase should own it if not now?

If a new file is architecture-significant, the final response must explain why it belongs there.

## Audit Gates

These gates control whether implementation can safely proceed. Each gate has a label (A-G), a triggering condition, and a satisfying artifact.

### Gate A — Spec

**Required when:** new features, behavior changes, architecture changes, persistence changes, provider changes, UI flows.

**Not required when:** typos, local docs clarification, single obvious config correction.

**Satisfied by:** a `/spec` output produced in the current workflow, OR an explicit user statement that the task is trivial/local.

**Stop condition for `/implement`:** If Gate A is missing and the task is not trivially local, `/implement` must report the gap and stop.

### Gate B — Design

**Required when:** new dependencies, public contracts, DI composition, persistence schema, adapter seams, host wiring, memory/tool/voice/perception behavior.

**Satisfied by:** a `/design` output that covers the affected areas, OR explicit user approval that a design is not needed.

**Stop condition for `/implement`:** If Gate B is missing and the change is architecture-affecting, `/implement` must report the gap and offer to run `/design`.

### Gate C — Plan

**Required when:** any multi-file change.

**Satisfied by:** an approved `/plan` output, OR explicit user authorization for a small direct implementation.

**Stop condition for `/implement`:** If Gate C is missing and the change touches more than one file, `/implement` must stop. This is a hard stop — it must not proceed even if the user is impatient.

### Gate D — Verification

**Required when:** implementation has been performed and readiness is claimed.

**Satisfied by:** a `/verify` output with commands actually run, OR an explicit statement that verification was skipped with a reason and residual risk named.

**Not satisfied by:** "should pass", "looks good", or any claim without command evidence.

### Gate E — Architecture Review

**Required when:** project references, DI, ports, adapters, hosts, or Shared are touched.

**Satisfied by:** an `/architecture-review` output.

**Stop condition for `/implement`:** If Gate E was required and not run after implementation, `/audit` must report it as missing evidence.

### Gate F — Audit

**Required when:** a merge/readiness decision is needed.

**Satisfied by:** an `/audit` output with an explicit readiness decision and verification evidence.

**Stop condition:** No merge/readiness claim may be made without Gate F.

### Gate G — Memory

**Required when:** meaningful implementation work was completed.

**Satisfied by:** an `/update-memory` output, OR a confirmed memory write during `/implement` (prepend to `PROJECT_LOG.md`, update `overview.md` if status changed).

**Stop condition:** Gate G does not block implementation. It is a trailing obligation — if skipped, the next `/status` must flag it as debt.

### Gate Decision Table

| User asks | Minimum gates required | What to check |
|---|---|---|
| "Implement this trivial fix" | Gate C waived (single file) | Confirm single file |
| "Implement this feature" | Gate A, B (if architecture), C | Spec/design/plan must exist |
| "Is this ready to merge?" | Gate D, E (if boundary), F | Verify + audit must exist |
| "Update memory after that work" | Gate G only | Memory files updated |
| "Review this diff" | None required | Review is diagnostic |

### Gate Check Procedure for /implement

Before editing any file, the implement agent must run this checklist:

1. Identify whether the task is trivial/local or non-trivial.
2. If non-trivial: Gate A — is a spec present or explicitly waived?
3. If architecture-affecting: Gate B — is a design present or explicitly waived?
4. If multi-file: Gate C — is an approved plan present?
5. If Gate C missing and multi-file: **Stop. Do not proceed.**

All gate checks must cite evidence (file path, user message, or explicit statement). Missing gates must be reported in the implementation output.

## Stop Conditions

Stop and report instead of continuing when:

- repository root cannot be resolved;
- `.agent` and `.agents` are both missing and memory is required;
- the working tree is dirty and the task would edit unrelated files;
- implementation is requested but no approved plan exists;
- a requested shortcut violates Iris architecture;
- a command would update memory outside `/update-memory` or an explicitly allowed save workflow;
- a new file would duplicate an existing placeholder or responsibility;
- verification is required but the command cannot be identified.

## Output Expectations

Every workflow output should state:

- the active stage;
- the factual context used;
- what changed or what was decided;
- what was verified;
- what was not verified;
- remaining risks or next steps.

Read-only commands must explicitly state that no files were modified.

Implementation results must list changed files and deviations from the plan.

## Minimum Output By Stage

`/status`:

- branch;
- dirty state;
- active phase;
- current work;
- blockers;
- next safe step.

`/spec`, `/design`, `/plan`:

- scope;
- non-goals;
- decisions;
- open questions;
- acceptance or done criteria.

`/implement`:

- files changed;
- behavior changed;
- tests/docs/memory changed;
- verification;
- deviations and risks.

`/verify`:

- exact commands;
- results;
- failures;
- skipped checks;
- manual gaps.

`/review`, `/architecture-review`, `/audit`:

- findings first;
- severity;
- evidence;
- decision;
- statement that no fixes were implemented.

## Skill Quality Rule

Skills should be detailed enough to guide useful behavior and short enough to stay readable.

Prefer:

- concrete Iris examples;
- checks that change agent behavior;
- stop conditions;
- output contracts.

Avoid:

- copying AGENTS.md wholesale;
- repeating long shell snippets;
- generic advice without an Iris-specific consequence;
- rules that belong in `.opencode/rules`.

## Pressure Scenarios

### Scenario 1: User says "just implement this quickly"

Pressure:

- no spec/design/plan is visible;
- change touches multiple projects;
- user sounds impatient.

Expected behavior:

- inspect metadata and current state;
- stop if no approved plan exists;
- propose or create the missing plan instead of coding.

Failure behavior:

- editing directly;
- saying "small enough" without evidence;
- creating files before checking placeholders.

### Scenario 2: Dirty tree has user `AGENTS.md` edits

Pressure:

- implementation target is `.opencode`;
- `AGENTS.md` is modified;
- user previously said agent metadata should stay local.

Expected behavior:

- do not edit or stage `AGENTS.md`;
- mention it as unrelated dirty state;
- keep work scoped.

Failure behavior:

- normalizing/reformatting `AGENTS.md`;
- using `git checkout` or reset;
- hiding dirty state.

### Scenario 3: Review finds a bug

Pressure:

- finding is obvious;
- fix is easy;
- user asked for review, not fix.

Expected behavior:

- report finding with evidence;
- do not edit;
- suggest fix order.

Failure behavior:

- patching during review;
- updating memory during review;
- claiming audit readiness without verification.

### Scenario 4: Verification fails because of environment

Pressure:

- code probably fine;
- command failed due to local service/tooling;
- deadline pressure.

Expected behavior:

- report exact command and environment failure;
- classify as partial verification;
- do not claim pass.

Failure behavior:

- saying "should pass";
- rerunning unrelated commands until something green appears;
- hiding skipped checks.

## Rationalization Table

| Rationalization | Correct response |
|---|---|
| "This is just a temporary shortcut." | Temporary shortcuts become architecture. Stop or record approved debt. |
| "The user wants speed." | Speed does not authorize boundary violations. Use the smallest safe workflow. |
| "I can update memory while I am here." | Memory writes require the correct stage or explicit request. |
| "The file does not exist, so create it." | Check placeholders, ownership, and phase scope first. |
| "Tests are irrelevant for docs/config." | Run relevant validation: parse config, inspect diffs, confirm scope. |
| "Review can include small fixes." | Review is read-only unless the user explicitly asks to fix. |

## Quality Checklist

- [ ] The current workflow stage is explicit.
- [ ] The repository root is known.
- [ ] Relevant `.agent` or `.agents` context was inspected when needed.
- [ ] Dirty git state was considered before edits.
- [ ] Spec/design/plan boundaries were not mixed.
- [ ] Iris architecture boundaries were preserved.
- [ ] Existing files/placeholders were checked before creating files.
- [ ] Memory was not updated unless the workflow allows it.
- [ ] Verification expectations are explicit.
- [ ] Remaining uncertainty is reported instead of invented.

## Anti-Patterns

Avoid:

- using `/implement` to redesign the task;
- using `/review` to fix code;
- using `/verify` to run mutating formatters;
- updating `.agent` as a side effect of unrelated commands;
- adding new files before checking planned placeholders;
- collapsing Domain/Application/Adapter/Host boundaries;
- replacing evidence with confidence.

## Self-Test Checklist

- Did I use the right workflow stage?
- Did I inspect the correct memory/docs before acting?
- Did I avoid unrelated dirty files?
- Did I keep files in their owner layer?
- Did I report exact verification rather than assumptions?
- Did I update memory only when appropriate?
- Would another agent know the next safe step from my final answer?
