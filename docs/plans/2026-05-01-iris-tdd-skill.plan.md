# Implementation Plan: Skill iris-tdd

## 1. Plan Goal

Create the `iris-tdd` workflow skill and integrate it into Iris as a methodology within `/implement`. The skill enforces the Red-Green-Refactor cycle adapted to .NET/C#/xUnit and Iris Clean/Hexagonal layering. Unlike brainstorming and debug, TDD is **not** a separate workflow stage — it has no stage selection table entry, no `/tdd` command, no iris-engineering workflow stages row. Integration touches exactly 3 files: the new skill file, `AGENTS.md`, and `commands/implement.md`. The skill is loaded by the `/implement` command alongside `iris-engineering` and `implement` — TDD is built into implementation, not a separate command.

This plan follows `docs/specs/2026-05-01-iris-tdd-skill.spec.md`. No design was produced — the change is a pure workflow skill (`.opencode/skills/` Markdown), not architecture-affecting.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-01-iris-tdd-skill.spec.md` (draft)
- **Design:** Not required — pure workflow skill. No DI, project references, adapters, hosts, or Iris source code touched.
- **Relevant rules/docs:** `AGENTS.md`, `.opencode/rules/workflow.md`, `.opencode/skills/iris-engineering/SKILL.md`, `.opencode/skills/implement/SKILL.md`, existing skill files as format reference.

### Assumptions

- YAML frontmatter conventions are uniform across all existing Iris skills.
- TDD is a methodology within `/implement`, not a separate stage — it does not get a stage selection table entry, workflow stages row, or command file.
- The `implement` command (`.opencode/commands/implement.md`) will be updated to load `iris-tdd` — TDD is built into implementation, not a separate command.
- Iris source code (`.csproj`, `.cs`, tests) will not be modified.
- The `.opencode/skills/` directory already exists.
- The `AGENTS.md` is already modified by prior work (brainstorm + debug) — our edit adds one more line to the skills list.

## 3. Scope Control

### In Scope

- Create `.opencode/skills/iris-tdd/SKILL.md` (new file).
- Add `iris-tdd/SKILL.md` to the `AGENTS.md` skills list.
- Update `.opencode/commands/implement.md` to load `iris-tdd` alongside `iris-engineering` and `implement`.

### Out of Scope

- Changing `.opencode/rules/workflow.md`.
- Changing `.opencode/skills/iris-engineering/SKILL.md` — no stage selection entry, no workflow stages row.
- Creating a `/tdd` command (`.opencode/commands/tdd.md`) — TDD is built into `/implement`, not a separate command.
- Changing `.opencode/skills/implement/SKILL.md` — the skill file itself stays as is; only the command is updated.
- Creating test projects, test data, or test infrastructure.
- Iris source code, tests, project references, configuration.
- Memory file updates (`.agent/`) — handled by a separate `/update-memory` step.

### Forbidden Changes

- Do not modify any Iris `.csproj`, `.cs`, `.slnx`, `appsettings.json`, or `DependencyInjection.cs`.
- Do not modify `.opencode/rules/*.md`.
- Do not modify `.opencode/skills/iris-engineering/SKILL.md`.
- Do not create a command file.
- Do not touch dirty `src/Iris.Desktop/*` files (unrelated user changes).

## 4. Implementation Strategy

Pure Markdown — no compilation, no tests, no runtime. The order:

1. **Create the skill file first** — must exist before anything references it.
2. **Add to AGENTS.md skills list** — register as discoverable.
3. **Wire into the implement command** — add `iris-tdd` to the command's skill list so it loads automatically during `/implement`.

This is simpler than brainstorm/debug because TDD is not a stage — it needs no iris-engineering routing and no separate command file. 3 files change: 1 new, 2 modified. Each phase is fully reversible by reverting to prior state.

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Confirm no `iris-tdd/` directory exists. Identify exact insertion point in `AGENTS.md` skills list.

#### Files to Inspect

- `.opencode/skills/` — confirm no `iris-tdd/` directory exists.
- `AGENTS.md` — lines 38-49 (skills list), identify exact insertion point for new entry.
- `.opencode/skills/spec/SKILL.md` — YAML frontmatter reference (lines 1-8).
- `.opencode/skills/iris-architecture/SKILL.md` — second YAML reference (lines 1-9).

#### Files Likely to Edit

- None.

#### Steps

1. Check for pre-existing `iris-tdd/` directory.
2. Read AGENTS.md skills list for current entries (after brainstorm + debug, current order is: brainstorm, debug, spec, design, plan, implement, verify, audit, agent-memory, architecture-boundary-review — 10 entries).
3. Decide placement: `iris-tdd` logically belongs between `iris-debug` and `spec` in the alphabetical-by-stage listing. The implement skill uses TDD, placing it before `implement` makes the connection clear.
4. Read two existing skill frontmatters for YAML format confirmation.

#### Verification

- Confirmed no duplicate `iris-tdd/` exists.
- Confirmed exact insertion point in AGENTS.md.

#### Rollback

No code changes.

---

### Phase 1 — Create iris-tdd/SKILL.md

#### Goal

Write the full TDD skill file with Red-Green-Refactor cycle, Iron Law, .NET-specific commands, test placement table, rationalization defense, and TDD Cycle Report format — all following Iris skill conventions.

#### Files to Inspect

- `.opencode/skills/spec/SKILL.md` — YAML frontmatter reference.
- `.opencode/skills/iris-architecture/SKILL.md` — YAML frontmatter reference (second sample).
- `.opencode/skills/iris-debug/SKILL.md` — Iron Law placement reference (bold, early, prominent).
- `docs/specs/2026-05-01-iris-tdd-skill.spec.md` — FR-001 through FR-014 and AC-001 through AC-007.
- `C:/Users/User/.agents/skills/superpowers/test-driven-development/SKILL.md` — conceptual reference for Red-Green-Refactor structure, rationalization table.

#### Files Likely to Edit

- `.opencode/skills/iris-tdd/SKILL.md` — **create**.

#### Files That Must Not Be Touched

- All existing skill files.
- `AGENTS.md`.
- Any Iris source code.

#### Steps

1. Create directory `.opencode/skills/iris-tdd/`.
2. Write `SKILL.md` with YAML frontmatter: `name: iris-tdd`, `compatibility: opencode`, `metadata` with `project: Iris`, `workflow_stage: implementation`, `output_type: tdd_cycle_report`. Note: `workflow_stage: implementation` reflects that TDD operates inside `/implement`, not as a separate stage.
3. Write the following sections, ordered by Iris skill convention:
   - **Purpose** — TDD as implementation methodology, Iron Law upfront.
   - **When to Use** — always during `/implement` with production code changes. NOT for config files, docs, project references.
   - **Iron Law** — "NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST." Bold, first section after Purpose, P1-001 citation.
   - **The Red-Green-Refactor Cycle** — overview of the 5-step cycle with .NET commands.
   - **RED — Write a Failing Test** — test placement table (FR-009), one test per cycle, `[Fact]`/`[Theory]`, AAA, FluentAssertions, clear naming.
   - **Verify RED — Watch It Fail** — `dotnet test --filter`, confirm failure reason, pass/error detection.
   - **GREEN — Minimal Code** — simplest code, no over-engineering, no unrelated changes.
   - **Verify GREEN — Watch It Pass** — focused test → full suite → build → format.
   - **REFACTOR — Clean Up** — duplication removal, naming, helpers, no new behavior.
   - **Repeat** — next cycle.
   - **Test Placement by Layer** (FR-009 expanded) — detailed mapping table.
   - **Architecture-Aware Testing** (FR-010) — layer-specific constraints.
   - **TDD Cycle Report** (FR-011) — exact template.
   - **Rationalization Defense** (FR-012) — Iris-specific counter-examples.
   - **Integration with implement** (FR-014) — how TDD interacts with the plan.
   - **Stop Conditions** (FR-013) — code-before-test → delete, test passes first run → fix test, etc.
   - **Quality Checklist** — per-cycle verification.
4. Verify every FR from spec is covered by at least one section.

#### Verification

- File exists at `.opencode/skills/iris-tdd/SKILL.md`.
- YAML frontmatter parseable (compare against reference skills).
- Iron Law is first section after Purpose, bold, with P1-001 citation.
- Red-Green-Refactor cycle uses .NET/xUnit-specific commands (`dotnet test --filter "FullyQualifiedName~..."`).
- Test placement table maps all Iris layers to their correct test projects.
- Rationalization defense table uses real Iris counter-examples (P1-001, T-04).
- TDD Cycle Report template includes all required fields.
- No implementation code present in the skill.
- Skill explicitly states it is a methodology, not a stage.

#### Rollback

Delete `.opencode/skills/iris-tdd/SKILL.md` and the `iris-tdd/` directory.

---

### Phase 2 — Update AGENTS.md

#### Goal

Register the TDD skill in the project's central agent instructions.

#### Files to Inspect

- `AGENTS.md` — lines 38-49 (skills list after brainstorm + debug additions).

#### Files Likely to Edit

- `AGENTS.md` — one insertion point.

#### Files That Must Not Be Touched

- All skill files.
- `.opencode/rules/*.md`.
- `.opencode/skills/iris-engineering/SKILL.md`.
- Iris source code.

#### Steps

1. **Skills list**: Add `- .opencode/skills/iris-tdd/SKILL.md`. Place it after `iris-debug/SKILL.md` and before `spec/SKILL.md` in the alphabetical-by-stage listing. This keeps the skills list organized: brainstorm (pre-spec) → debug (diagnostic) → tdd (implement methodology) → spec → design → plan → implement → verify → audit → agent-memory → architecture-boundary-review.

#### Verification

- `AGENTS.md` skills list includes `iris-tdd/SKILL.md`.
- No other sections changed (workflow, rules, agents, architecture drift, reconnaissance, verification, memory, security).
- `Dirty` `src/Iris.Desktop/*` files untouched.

#### Rollback

Revert `AGENTS.md` to Phase 0 baseline.

---

### Phase 3 — Update implement command

#### Goal

Wire `iris-tdd` into the `/implement` command so the TDD methodology loads automatically during implementation.

#### Files to Inspect

- `.opencode/commands/implement.md` — lines 1-12 (YAML frontmatter and skill loading section).

#### Files Likely to Edit

- `.opencode/commands/implement.md` — one insertion point.

#### Files That Must Not Be Touched

- All skill files.
- `.opencode/rules/*.md`.
- `.opencode/skills/iris-engineering/SKILL.md`.
- Iris source code.

#### Steps

1. **Skill list**: The command currently loads `iris-engineering` and `implement`. Add `Use the \`iris-tdd\` skill.` after `Use the \`implement\` skill.` at line 9. This ensures the TDD methodology is active during every `/implement` invocation.

#### Verification

- `commands/implement.md` loads `iris-tdd` after `implement`.
- No other sections of the command changed (hard rules, context, verification, output format).
- `iris-engineering/SKILL.md` untouched.
- Dirty `src/Iris.Desktop/*` files untouched.

#### Rollback

Revert `commands/implement.md` to Phase 0 baseline.

---

### Phase 4 — Final Review and Verification

#### Goal

Confirm all changes are consistent, no regressions, and the skill is ready.

#### Files to Inspect

- `.opencode/skills/iris-tdd/SKILL.md` (new file — final review).
- `AGENTS.md` (modified — final review).
- `.opencode/commands/implement.md` (modified — final review).
- `docs/specs/2026-05-01-iris-tdd-skill.spec.md` (spec — cross-check acceptance criteria).
- `.opencode/skills/iris-engineering/SKILL.md` — confirm UNCHANGED (per AC-003).

#### Files Likely to Edit

- None (read-only review phase).

#### Steps

1. Cross-check every acceptance criterion (16 items) from the spec against the implementation.
2. Verify no file contains YAML syntax errors.
3. Verify all Markdown links and paths are correct.
4. Confirm `iris-engineering/SKILL.md` is **untouched** — `git diff` must NOT show changes to this file.
5. Run `git diff --stat` to confirm only intended files changed.
6. Confirm dirty `src/Iris.Desktop/*` files were not touched.

#### Verification

- `git diff --stat` shows exactly: `AGENTS.md`, `commands/implement.md`, new `iris-tdd/SKILL.md`.
- `iris-engineering/SKILL.md` NOT present in diff.
- No `*.cs`, `*.csproj`, `*.slnx`, or test files changed.
- No command file created.
- All 16 acceptance criteria from the spec are satisfied.

#### Rollback

Revert `AGENTS.md` to Phase 0 baseline. Delete new directory.

---

## 6. Testing Plan

### Unit Tests

None. This is a workflow skill (Markdown), not compilable code. No .NET project is involved.

### Integration Tests

None.

### Architecture Tests

Not applicable — this change does not touch Iris source code, project references, or DI. Running `dotnet test .\Iris.slnx` would be noise (tests pass or fail on unrelated Desktop code). **Skip.**

### Regression Tests

Not applicable.

### Manual Verification

- **M-01:** Load the skill in a conversation with `/implement`. Write production code for a feature. Agent must invoke `iris-tdd`, write a test first, watch it fail, then write production code.
- **M-02:** During `/implement`, agent writes a line of production code before any test. Iron Law enforcement: agent must delete the code and start with RED.
- **M-03:** "Implement a new Domain entity `ConversationTitle` with a max-length invariant." Agent must: (a) write test in `tests/Iris.Domain.Tests/`, (b) run RED verification, (c) write Domain code, (d) run GREEN verification.
- **M-04:** Test passes on first run during RED. Agent must recognize this is wrong and fix the test.
- **M-05:** Agent makes GREEN code that breaks an existing test. Agent must fix the production code, not the existing test.
- **M-06:** Agent completes a TDD cycle and produces a TDD Cycle Report with all required sections.
- **M-07:** Existing skills (brainstorm, debug, spec, design, plan, implement, verify, audit) continue to load without errors.

## 7. Documentation and Memory Plan

### Documentation Updates

- Spec already saved: `docs/specs/2026-05-01-iris-tdd-skill.spec.md`.
- No other documentation changes needed.

### Agent Memory Updates

After implementation (separate step, `/update-memory`):
- Append to `.agent/PROJECT_LOG.md` — completed iteration.
- Update `.agent/overview.md` if TDD becomes current active work.

## 8. Verification Commands

No `dotnet build` / `dotnet test` / `dotnet format` needed. This is pure Markdown.

Commands to run during Phase 3 verification:

```powershell
git diff --stat
git diff -- .opencode/skills/iris-engineering/SKILL.md
```

Expected:
- `git diff --stat` shows only `AGENTS.md`, `commands/implement.md`, and new `iris-tdd/SKILL.md`.
- `git diff -- .opencode/skills/iris-engineering/SKILL.md` shows **no output** (file untouched).

## 9. Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| YAML frontmatter syntax error | Skill unavailable | Phase 1: manual review against two reference skills |
| Iron Law placement too weak (buried) | Agent skips TDD cycle | Phase 1: Iron Law must be in first section after Purpose, bold, unavoidable |
| `workflow_stage: implementation` in metadata is unconventional | Confusion about where TDD fits | Document in skill that it's a methodology, not a stage; explicit note in Purpose |
| Accidental edit of `iris-engineering/SKILL.md` | Adds unwanted stage routing | Phase 0: recon confirms no edit needed; Phase 3: git diff confirms untouched |
| Unrelated dirty file touch | User's Desktop work corrupted | Explicit per-phase forbidden-files lists; Phase 3 git diff check |
| Skill too long (Red-Green-Refactor + rationalization + placement) | Agent skips reading sections | Mirror brainstorm/debug structure; use concise tables; keep under 350 lines |

## 10. Implementation Handoff Notes

**Critical constraints:**
- Pure Markdown — no `dotnet` commands during implementation.
- 3 files: 1 new + 2 modified (NOT 4 — no iris-engineering edit).
- Do NOT create a separate `/tdd` command — TDD is built into `/implement`.
- Do NOT edit `iris-engineering/SKILL.md` — TDD is a methodology, not a stage.
- Do NOT edit `implement/SKILL.md` — only the command file is updated.
- Dirty `src/Iris.Desktop/*` files — do not touch.

**Risky areas:**
- `workflow_stage: implementation` in YAML metadata — must be clearly justified in the skill text.
- Iron Law placement — must be section 2 (after Purpose), bold, with P1-001 citation.
- Test placement table — must use real Iris test project names from spec §4 (5 projects).
- Rationalization defense — must use real Iris failures (P1-001, T-04), not generic examples.

**Expected final state:**
- `.opencode/skills/iris-tdd/SKILL.md` exists with Red-Green-Refactor cycle, Iron Law, test placement table, rationalization defense, TDD Cycle Report template.
- `AGENTS.md` skills list includes `iris-tdd/SKILL.md`.
- `.opencode/commands/implement.md` loads `iris-tdd` alongside `iris-engineering` and `implement`.
- `iris-engineering/SKILL.md` is UNCHANGED.
- No separate `/tdd` command created.
- No Iris source, tests, or infrastructure modified.

**Checks that must not be skipped:**
- `iris-engineering/SKILL.md` untouched — git diff must show zero changes.
- No command file accidentally created — check `.opencode/commands/` directory.
- Iron Law placement — highly visible, first section after metadata.
- Cross-check all 16 acceptance criteria from spec.

## 11. Open Questions

No blocking open questions.
