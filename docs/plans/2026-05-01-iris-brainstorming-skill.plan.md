# Implementation Plan: Skill iris-brainstorming

## 1. Plan Goal

Create the `iris-brainstorming` workflow skill and integrate it into Iris stage separation. The skill enables a structured pre-spec collaborative dialogue: explore context → one question at a time → propose 2-3 approaches → validate → produce Brainstorm Output. Integration touches exactly 3 files: the new skill file, `AGENTS.md`, and `iris-engineering/SKILL.md`.

This plan follows `docs/specs/2026-05-01-iris-brainstorming-skill.spec.md`. No design was produced, as the change is not architecture-affecting (Gate B not triggered — no new dependencies, contracts, DI, persistence, adapters, hosts, or runtime behavior).

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-01-iris-brainstorming-skill.spec.md` (draft)
- **Design:** Not required — the change is a pure workflow skill (`.opencode/skills/` Markdown), not architecture-affecting. No DI, project references, adapters, hosts, or Iris source code touched.
- **Relevant rules/docs:** `AGENTS.md`, `.opencode/rules/workflow.md`, `.opencode/skills/iris-engineering/SKILL.md`, existing skill files as format reference.

### Assumptions

- YAML frontmatter conventions are uniform across all existing Iris skills (`name`, `description`, `compatibility: opencode`, `metadata` block).
- The brainstorming skill is a pre-Gate-A activity and does not modify the gate system in `.opencode/rules/workflow.md`.
- The spec is sufficient as-is; no design document is needed for a pure workflow skill.
- Iris source code (`.csproj`, `.cs`, tests) will not be modified by this plan.
- No new directories need to be created — `.opencode/skills/` already exists.
- The `AGENTS.md` is not currently dirty (confirmed: dirty files are in `src/Iris.Desktop/*`, not `AGENTS.md`).

## 3. Scope Control

### In Scope

- Create `.opencode/skills/iris-brainstorm/SKILL.md` (new file).
- Update `AGENTS.md`: add `Brainstorm` to workflow list, add `iris-brainstorming` to skills list.
- Update `.opencode/skills/iris-engineering/SKILL.md`: add brainstorm row to stage selection table, add brainstorm row to workflow stages table.

### Out of Scope

- Changing `.opencode/rules/workflow.md` (gates remain A-G).
- Changing `.opencode/skills/spec/SKILL.md`.
- Changing `.opencode/skills/design/SKILL.md`.
- Creating any file outside `.opencode/skills/iris-brainstorm/`.
- Iris source code, tests, project references, configuration.
- Memory file updates (`.agent/`) — handled by a separate `/update-memory` step post-implementation.

### Forbidden Changes

- Do not modify any Iris `.csproj`, `.cs`, `.slnx`, `appsettings.json`, or `DependencyInjection.cs` file.
- Do not modify `.opencode/rules/*.md`.
- Do not create `docs/` spec/design/plan files (the spec is already saved).
- Do not change the gate system (A-G) or gate conditions.
- Do not alter existing skill files other than `iris-engineering/SKILL.md`.
- Do not touch the dirty `src/Iris.Desktop/*` files (unrelated user changes).

## 4. Implementation Strategy

The change is purely Markdown — no compilation, no tests, no runtime. The implementation order is:

1. **Create the skill file first** — the brainstorming skill must exist before anything references it.
2. **Update AGENTS.md** — add the skill to the workflow and skills list, so it becomes discoverable.
3. **Update iris-engineering** — add the stage selection routing so the agent knows when to invoke it.

This order is safe because the skill file is created before any references to it. Each phase edits exactly one file (Phase 2 and 3 edit two but are independent — AGENTS.md and iris-engineering are separate concerns).

Every phase is fully reversible by reverting the file to its prior state (no code changes, no DB migrations, no configuration side effects).

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Confirm exact locations of edits in existing files. Verify no placeholder or duplicate exists.

#### Files to Inspect

- `.opencode/skills/iris-engineering/SKILL.md` — read lines 48-85 for workflow stages table and stage selection table, identify exact insertion points.
- `AGENTS.md` — read lines 10-18 (workflow list) and lines 38-48 (skills list), identify exact insertion points.
- `.opencode/skills/spec/SKILL.md` — read frontmatter (lines 1-8) as YAML format reference.
- `.opencode/skills/iris-architecture/SKILL.md` — read lines 1-9 as second YAML reference.
- `.opencode/skills/` — confirm no `iris-brainstorm/` directory exists.

#### Files Likely to Edit

- None.

#### Steps

1. Read iris-engineering workflow stages table (current: 12 rows, no brainstorm).
2. Read iris-engineering stage selection table (current: 10 rows, "Let's think / decide / scope" routed to `/spec` or `/design`).
3. Read AGENTS.md workflow list (current: Spec → Design → Plan → Implement → Verify → Review → Audit).
4. Read AGENTS.md skills list (current: 8 skills listed).
5. Read two existing skill frontmatters for YAML format confirmation.
6. Check `.opencode/skills/` for pre-existing `iris-brainstorm/`.

#### Verification

- Confirmed exact line positions for Phase 1-3 edits.
- Confirmed no duplicate `iris-brainstorm/` exists.

#### Rollback

No code changes.

---

### Phase 1 — Create iris-brainstorm/SKILL.md

#### Goal

Write the main brainstorming skill file following all Iris conventions.

#### Files to Inspect

- `.opencode/skills/spec/SKILL.md` — YAML frontmatter format reference.
- `.opencode/skills/iris-architecture/SKILL.md` — YAML frontmatter format reference (second sample).
- `docs/specs/2026-05-01-iris-brainstorming-skill.spec.md` — functional requirements (FR-001 through FR-010) and architecture constraints (AC-001 through AC-006).

#### Files Likely to Edit

- `.opencode/skills/iris-brainstorm/SKILL.md` — **create**.

#### Files That Must Not Be Touched

- All existing `.opencode/skills/*/SKILL.md` files.
- `AGENTS.md`.
- Any Iris source code.

#### Steps

1. Create the directory `.opencode/skills/iris-brainstorm/` (parent `.opencode/skills/` exists).
2. Write `SKILL.md` with these sections, ordered by Iris skill convention:
   - YAML frontmatter: `name: iris-brainstorm`, appropriate description, `compatibility: opencode`, `metadata` with `project: Iris`, `workflow_stage: brainstorming`, `output_type: brainstorm_output`.
   - Purpose section — explains what brainstorming does in the Iris workflow.
   - When To Use — activation triggers per FR-001, plus when NOT to use.
   - Required Context — per FR-002: files to inspect before dialogue.
   - Dialogue Rules:
     - One question at a time (FR-003).
     - Approach comparison: 2-3 approaches with trade-offs (FR-004).
     - Incremental validation (FR-005).
     - Scope decomposition detection (FR-006).
   - Brainstorm Output format (FR-007).
   - Handoff Rules — Brainstorm Output → spec/design (FR-008).
   - Read-Only Guarantee (FR-009).
   - Saving Policy (FR-010).
   - Architecture-aware dialogue — every approach must respect Iris boundaries (AC-005) with concrete examples of violations to avoid.
   - Stop conditions — user asks for code, user contradicts constraints, idea violates Iris architecture.
   - Anti-patterns — what the brainstorming skill must never do (create files, skip questions, propose violating approaches).
   - Quality Checklist.
3. Verify the YAML frontmatter is valid Markdown with correct YAML syntax.
4. Verify every FR from the spec is covered by at least one section.

#### Verification

- The file exists at `.opencode/skills/iris-brainstorm/SKILL.md`.
- YAML frontmatter is parseable (no indentation errors, valid keys).
- File uses `# ` headings with the word "Skill" in the title (convention from existing skills: `# Iris Architecture Skill`, `# Spec Skill`).
- No implementation code is present.
- No file creation instructions are present.
- Architecture boundary examples are specifically Iris (references to Domain, Application, Desktop, Persistence, ModelGateway).

#### Rollback

Delete `.opencode/skills/iris-brainstorm/SKILL.md` and the `iris-brainstorm/` directory.

---

### Phase 2 — Update AGENTS.md

#### Goal

Register the brainstorming skill in the project's central agent instructions.

#### Files to Inspect

- `AGENTS.md` — lines 10-18 (workflow) and 38-48 (skills list).

#### Files Likely to Edit

- `AGENTS.md` — two insertion points.

#### Files That Must Not Be Touched

- All `.opencode/skills/*/SKILL.md` (except the new one from Phase 1).
- `.opencode/rules/*.md`.
- Iris source code.

#### Steps

1. **Workflow list** (lines 12-18): Insert `1. Brainstorm` before `1. Spec`, renumber subsequent items (2-8 instead of 1-7).
2. **Skills list** (lines 40-47): Add `- .opencode/skills/iris-brainstorm/SKILL.md` at the top (before spec), consistent with existing alphabetical-by-stage ordering.
3. **Workflow description** (line 10): Update from "For non-trivial work, use:" to reflect the expanded 8-step workflow.

#### Verification

- `AGENTS.md` workflow list starts with `Brainstorm`.
- `AGENTS.md` skills list includes `iris-brainstorm/SKILL.md`.
- No other sections changed (rules list, agents list, architecture drift, reconnaissance, verification, memory, security, external docs, final response).
- Line count increased by ~3-4 lines (not a massive rewrite).

#### Rollback

Revert `AGENTS.md` to its prior state (Phase 0 baseline).

---

### Phase 3 — Update iris-engineering/SKILL.md

#### Goal

Route the brainstorming stage in the central engineering skill so the agent knows when to invoke it.

#### Files to Inspect

- `.opencode/skills/iris-engineering/SKILL.md` — lines 48-65 (workflow stages table), lines 68-85 (stage selection table), lines 86-112 (handoff rules), lines 286-328 (minimum output by stage).

#### Files Likely to Edit

- `.opencode/skills/iris-engineering/SKILL.md` — three insertion points.

#### Files That Must Not Be Touched

- All other `.opencode/skills/*/SKILL.md` files.
- `.opencode/rules/*.md`.
- `AGENTS.md` (already edited in Phase 2).

#### Steps

1. **Workflow stages table** (currently 10 rows + header): Insert a new row at the top:
   ```
   | `/brainstorm` | Pre-spec collaborative scoping dialogue | No | No |
   ```
2. **Stage selection table** (currently 10 rows + header): Replace the row:
   ```
   | "Let's think / decide / scope" | `/spec` or `/design` | no edits yet |
   ```
   with:
   ```
   | "Let's think / brainstorm / explore" | `/brainstorm` | pre-spec dialogue, no edits |
   | "Let's decide / scope" (clear scope ready) | `/spec` or `/design` | no edits yet |
   ```
3. **Minimum output by stage** (currently lists 8 stages): Add brainstorm entry:
   ```
   `/brainstorm`:
   - problem summary;
   - agreed scope boundary;
   - non-goals;
   - selected approach rationale;
   - open questions;
   - recommended next stage.
   ```
4. Verify no other section is accidentally modified.

#### Verification

- Workflow stages table has 11 rows + header (was 10).
- Stage selection table has 11 rows + header (was 10), or 12 if split into two rows as above.
- "Let's think / brainstorm / explore" routes to `/brainstorm`.
- Minimum output section includes brainstorm entry.
- Audit gates section (lines 175-255) is untouched.
- Stop conditions section is untouched.
- Pressure scenarios are untouched.

#### Rollback

Revert `iris-engineering/SKILL.md` to its prior state (Phase 0 baseline).

---

### Phase 4 — Final Review and Verification

#### Goal

Confirm all changes are consistent, no regressions, and the skill is ready.

#### Files to Inspect

- `.opencode/skills/iris-brainstorm/SKILL.md` (new file — final review).
- `AGENTS.md` (modified — final review).
- `.opencode/skills/iris-engineering/SKILL.md` (modified — final review).
- `docs/specs/2026-05-01-iris-brainstorming-skill.spec.md` (spec — cross-check acceptance criteria).

#### Files Likely to Edit

- None (read-only review phase).

#### Steps

1. Cross-check every acceptance criterion (13 items) from the spec against the implementation.
2. Verify no file contains YAML syntax errors.
3. Verify all Markdown links and paths are correct.
4. Run `git diff` to confirm only intended files changed.
5. Confirm dirty `src/Iris.Desktop/*` files were not touched.

#### Verification

- `git diff --stat` shows exactly: `AGENTS.md`, `iris-engineering/SKILL.md`, new `iris-brainstorm/SKILL.md`.
- No `*.cs`, `*.csproj`, `*.slnx`, or test files changed.
- All 13 acceptance criteria from the spec are satisfied.

#### Rollback

Revert all three files to Phase 0 baseline. Delete new directory.

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

- **M-01:** Load the skill in a new conversation and verify the agent invokes it when the user says "let's brainstorm a new feature".
- **M-02:** During brainstorming dialogue, verify the agent asks one question at a time.
- **M-03:** During brainstorming dialogue, verify the agent proposes 2-3 approaches and flags architecture violations.
- **M-04:** Verify the agent produces a Brainstorm Output when the user confirms direction.
- **M-05:** Verify the Brainstorm Output can be used as input to `spec`.
- **M-06:** Verify no files are created during brainstorming (check `.opencode/skills/`, `docs/`).
- **M-07:** Verify existing skills (spec, design, plan, implement, verify, audit) still load without errors.

## 7. Documentation and Memory Plan

### Documentation Updates

- Spec already saved: `docs/specs/2026-05-01-iris-brainstorming-skill.spec.md`.
- No other documentation changes needed.

### Agent Memory Updates

After implementation (separate step, `/update-memory`):
- Append to `.agent/PROJECT_LOG.md` — completed iteration.
- Update `.agent/overview.md` if brainstorming becomes current active work.

## 8. Verification Commands

No `dotnet build` / `dotnet test` / `dotnet format` needed. This is pure Markdown.

Commands to run during Phase 4 verification:

```powershell
git diff --stat
```

Expected: only `AGENTS.md`, `iris-engineering/SKILL.md`, and `iris-brainstorm/SKILL.md` show changes.

## 9. Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| YAML frontmatter syntax error prevents skill loading | Skill unavailable, broken Iris workflow | Phase 1 verification: manual YAML review against two reference skills |
| Wrong table row position breaks iris-engineering routing | Wrong stage selected for ambiguous requests | Phase 0 reconnaissance: identify exact line numbers before editing |
| AGENTS.md renumbering breaks phase | Confusion about workflow order | Phase 2 verification: manual review of numbered list |
| Accidental edit of dirty `src/Iris.Desktop/*` files | User's work corrupted | Explicit forbidden-files list per phase; git diff check in Phase 4 |
| Brainstorm Output format too long | Clutters conversation | Spec FR-007 defines concise format; skill enforces brevity |

## 10. Implementation Handoff Notes

**Critical constraints:**
- This is pure Markdown — no `dotnet` commands needed for build/test.
- Only 3 files change: 1 new, 2 modified.
- Do not touch any Iris `.cs`, `.csproj`, `.slnx`, `appsettings.json`, or `DependencyInjection.cs`.
- The working tree has unrelated dirty files: `src/Iris.Desktop/DependencyInjection.cs`, `src/Iris.Desktop/Iris.Desktop.csproj`, `src/Iris.Desktop/appsettings.json`. **Do not touch these.**

**Risky areas:**
- Table row insertion in `iris-engineering/SKILL.md` — must preserve exact pipe-aligned formatting of existing Markdown tables.
- AGENTS.md renumbering — must renumber ALL subsequent items (2 through 8), not just insert.

**Expected final state:**
- `.opencode/skills/iris-brainstorm/SKILL.md` exists with valid YAML and full content.
- `AGENTS.md` workflow: Brainstorm → Spec → Design → Plan → Implement → Verify → Review → Audit.
- `AGENTS.md` skills list includes `iris-brainstorm/SKILL.md`.
- `iris-engineering/SKILL.md` routes "brainstorm" requests to `/brainstorm` stage.
- No Iris source code, tests, or infrastructure modified.
- Nothing saved to `docs/` beyond what's already there.

**Checks that must not be skipped:**
- YAML frontmatter validity (compare against two reference skills).
- git diff to confirm no unintended files changed.
- Cross-check all 13 acceptance criteria from spec.

## 11. Open Questions

No blocking open questions.
