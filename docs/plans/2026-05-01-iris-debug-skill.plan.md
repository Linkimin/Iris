# Implementation Plan: Skill iris-debug

## 1. Plan Goal

Create the `iris-debug` workflow skill and integrate it into Iris stage separation. The skill enables systematic debugging: 4-phase methodology (root cause → pattern analysis → hypothesis → fix proposal), Iris-specific failure pattern catalog, architecture-aware diagnostic commands, and a Debug Report output. Integration touches exactly 3 files: the new skill file, `AGENTS.md`, and `iris-engineering/SKILL.md`.

This plan follows `docs/specs/2026-05-01-iris-debug-skill.spec.md`. No design was produced — the change is a pure workflow skill (`.opencode/skills/` Markdown), not architecture-affecting. Gate B not triggered: no new dependencies, contracts, DI, persistence, adapters, hosts, or runtime behavior.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-01-iris-debug-skill.spec.md` (draft)
- **Design:** Not required — pure workflow skill. No DI, project references, adapters, hosts, or Iris source code touched.
- **Relevant rules/docs:** `AGENTS.md`, `.opencode/rules/workflow.md`, `.opencode/skills/iris-engineering/SKILL.md`, `.agent/log_notes.md` (for failure pattern catalog).

### Assumptions

- YAML frontmatter conventions are uniform across all existing Iris skills.
- The debug skill is a diagnostic stage — it sits outside the main spec→design→plan→implement chain, similar to `verify` and `review`.
- The spec is sufficient as-is; no design document is needed.
- Iris source code (`.csproj`, `.cs`, tests) will not be modified.
- `rg` fallback commands (`Select-String`, `Get-ChildItem`) are documented in the skill as known workarounds.
- The `AGENTS.md` is not dirty (confirmed: only `src/Iris.Desktop/*` and untracked docs are dirty).

## 3. Scope Control

### In Scope

- Create `.opencode/skills/iris-debug/SKILL.md` (new file).
- Update `AGENTS.md`: add debug to workflow, add to skills list.
- Update `.opencode/skills/iris-engineering/SKILL.md`: add debug row to stage selection table and workflow stages table.

### Out of Scope

- Changing `.opencode/rules/workflow.md`.
- Changing any existing skill other than `iris-engineering`.
- Iris source code, tests, project references, configuration.
- Automated debugging tools, profilers, monitoring infrastructure.

### Forbidden Changes

- Do not modify any Iris `.csproj`, `.cs`, `.slnx`, `appsettings.json`, or `DependencyInjection.cs`.
- Do not modify `.opencode/rules/*.md`.
- Do not touch dirty `src/Iris.Desktop/*` files (unrelated user changes).
- Do not alter existing skill files other than `iris-engineering/SKILL.md`.

## 4. Implementation Strategy

Pure Markdown — no compilation, no tests, no runtime. Order:

1. **Create the skill file first** — must exist before anything references it.
2. **Update AGENTS.md** — register the skill as discoverable.
3. **Update iris-engineering** — route "bug/test/build failure" to debug stage.

This is identical to the `iris-brainstorming` plan structure. Each phase edits one concern. Every phase is fully reversible.

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Confirm exact insertion points in existing files. Verify no `iris-debug/` directory exists.

#### Files to Inspect

- `.opencode/skills/iris-engineering/SKILL.md` — lines 48-65 (workflow stages), 68-85 (stage selection), 286-328 (minimum output).
- `AGENTS.md` — lines 10-18 (workflow), 38-48 (skills list).
- `.opencode/skills/` — confirm no `iris-debug/` directory.
- `.agent/log_notes.md` — review for failure pattern examples to include in skill.

#### Files Likely to Edit

- None.

#### Steps

1. Read iris-engineering tables for exact insertion points.
2. Read AGENTS.md sections for exact insertion points.
3. Check for pre-existing `iris-debug/`.
4. Review log_notes.md for Iris-specific failure patterns to reference.

#### Verification

- Confirmed line positions.
- Confirmed no duplicate exists.

#### Rollback

No code changes.

---

### Phase 1 — Create iris-debug/SKILL.md

#### Goal

Write the full debugging skill file with 4-phase methodology, Iris-specific failure pattern catalog, diagnostic command table, rationalization defense, and Debug Report output format — all following Iris skill conventions.

#### Files to Inspect

- `.opencode/skills/spec/SKILL.md` — YAML frontmatter reference.
- `.opencode/skills/iris-architecture/SKILL.md` — YAML frontmatter reference (second sample).
- `docs/specs/2026-05-01-iris-debug-skill.spec.md` — FR-001 through FR-011 and AC-001 through AC-007.
- `.agent/log_notes.md` — failure pattern examples (P1-001, T-04 flaky, parallel locks, shortcut regressions).

#### Files Likely to Edit

- `.opencode/skills/iris-debug/SKILL.md` — **create**.

#### Files That Must Not Be Touched

- All existing skill files.
- `AGENTS.md`.
- Any Iris source code.

#### Steps

1. Create directory `.opencode/skills/iris-debug/`.
2. Write `SKILL.md` with YAML frontmatter: `name: iris-debug`, `compatibility: opencode`, `metadata` with `project: Iris`, `workflow_stage: debugging`, `output_type: debug_report`.
3. Write the following sections, ordered by Iris convention:
   - **Purpose** — systematic debugging for Iris, Iron Law upfront.
   - **When to Use** — bugs, test/build failures, unexpected behavior. When NOT to use (trivial typos).
   - **Iron Law** — "NO FIX PROPOSAL WITHOUT CONFIRMED ROOT CAUSE." Bold, visible, early.
   - **Phase 1 — Root Cause Investigation** (FR-003).
   - **Phase 2 — Pattern Analysis** (FR-004) with Iris-specific failure pattern catalog table.
   - **Phase 3 — Hypothesis and Verification** (FR-005) with 3+ failures escalation.
   - **Phase 4 — Fix Proposal** (FR-006) with next-stage recommendation.
   - **Debug Report output format** (FR-007).
   - **Diagnostic Command Catalog** (FR-008).
   - **Architecture-Aware Debugging** (FR-009).
   - **Rationalization Defense** (FR-010).
   - **Handoff Rules** (FR-011).
   - **Stop Conditions**.
   - **Quality Checklist**.
4. Verify every FR from spec is covered.
5. Verify YAML is valid.

#### Verification

- File exists at `.opencode/skills/iris-debug/SKILL.md`.
- YAML frontmatter parseable.
- Iron Law is prominently visible (first section after metadata, bold).
- Failure pattern catalog uses real Iris names and real diagnostic commands.
- Debug Report template includes all required sections.
- No implementation code present.

#### Rollback

Delete `.opencode/skills/iris-debug/SKILL.md` and the `iris-debug/` directory.

---

### Phase 2 — Update AGENTS.md

#### Goal

Register the debug skill as a diagnostic stage.

#### Files to Inspect

- `AGENTS.md` — lines 10-18 (workflow), 38-48 (skills list).

#### Files Likely to Edit

- `AGENTS.md` — two insertion points.

#### Files That Must Not Be Touched

- All skill files.
- `.opencode/rules/*.md`.
- Iris source code.

#### Steps

1. **Workflow list**: Add debugging as a diagnostic stage. Since debug is reactive (triggered by failure, not part of the linear chain), add a note after the workflow list:
   ```
   For failures (bugs, test failures, build failures), use Debug before planning a fix.
   ```
2. **Skills list**: Add `- .opencode/skills/iris-debug/SKILL.md`.

#### Verification

- `AGENTS.md` references debugging as a diagnostic stage.
- Skills list includes `iris-debug`.
- No other sections changed.

#### Rollback

Revert `AGENTS.md` to Phase 0 baseline.

---

### Phase 3 — Update iris-engineering/SKILL.md

#### Goal

Route "bug/test/build failure" requests to the debug stage.

#### Files to Inspect

- `.opencode/skills/iris-engineering/SKILL.md` — lines 48-65, 68-85, 286-328.

#### Files Likely to Edit

- `.opencode/skills/iris-engineering/SKILL.md` — three insertion points.

#### Files That Must Not Be Touched

- All other skill files.
- `AGENTS.md` (already edited).
- `.opencode/rules/*.md`.

#### Steps

1. **Workflow stages table**: Insert new row:
   ```
   | /debug | Systematic root cause investigation, no fixes | No | No |
   ```
2. **Stage selection table**: Insert new row:
   ```
   | "Bug / test failure / build failure" | /debug | diagnostic investigation, no fixes |
   ```
3. **Minimum output by stage**: Add debug entry with all Debug Report sections.

#### Verification

- Workflow stages table includes debug row.
- Stage selection routes "bug / test failure" to `/debug`.
- Minimum output includes debug entry.
- Audit gates, stop conditions, pressure scenarios sections untouched.

#### Rollback

Revert `iris-engineering/SKILL.md` to Phase 0 baseline.

---

### Phase 4 — Final Review and Verification

#### Goal

Cross-check against spec, verify no regressions.

#### Files to Inspect

- `.opencode/skills/iris-debug/SKILL.md` (final review).
- `AGENTS.md` (final review).
- `.opencode/skills/iris-engineering/SKILL.md` (final review).
- `docs/specs/2026-05-01-iris-debug-skill.spec.md` (cross-check ACs).

#### Files Likely to Edit

- None.

#### Steps

1. Cross-check all 17 acceptance criteria from the spec.
2. Verify YAML validity.
3. Run `git diff --stat` — confirm only intended files.
4. Confirm dirty `src/Iris.Desktop/*` untouched.

#### Verification

- `git diff --stat` shows exactly: `AGENTS.md`, `iris-engineering/SKILL.md`, new `iris-debug/SKILL.md`.
- No `*.cs`, `*.csproj`, `*.slnx` changed.
- All 17 acceptance criteria satisfied.

#### Rollback

Revert all three files. Delete new directory.

---

## 6. Testing Plan

### Unit Tests

None — pure Markdown skill.

### Integration Tests

None.

### Architecture Tests

Not applicable — skip `dotnet test`.

### Regression Tests

Not applicable.

### Manual Verification

- **M-01:** Load the skill and say "the test StateBecomesSuccessThenIdle is failing." Agent invokes iris-debug, reads error, runs reproduction, produces Debug Report.
- **M-02:** "Desktop is calling IrisDbContext directly." Agent flags architecture violation via forbidden import check.
- **M-03:** During Phase 1, ask "so what's the fix?" Agent enforces Iron Law.
- **M-04:** After 3 failed fix proposals, agent escalates to architecture discussion.
- **M-05:** Debug Report includes all 7 required sections.
- **M-06:** No files created/modified during debugging session.
- **M-07:** "the build is broken" → agent selects debug stage.

## 7. Documentation and Memory Plan

### Documentation Updates

- Spec already saved: `docs/specs/2026-05-01-iris-debug-skill.spec.md`.
- No other docs needed.

### Agent Memory Updates

Post-implementation (separate `/update-memory`):
- Append to `.agent/PROJECT_LOG.md`.
- Update `.agent/overview.md` if debug becomes current active work.

## 8. Verification Commands

```powershell
git diff --stat
```

Expected: only `AGENTS.md`, `iris-engineering/SKILL.md`, and `iris-debug/SKILL.md` show changes.

## 9. Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| YAML frontmatter syntax error | Skill unavailable | Phase 1: manual review against reference skills |
| Failure pattern catalog too generic | Skill doesn't help with real Iris bugs | Use log_notes.md real examples; validate against P1-001 and T-04 |
| Iron Law weak placement (buried in text) | Agent skips debugging | Phase 1: Iron Law must be in first 20 lines, bold, unavoidable |
| Diagnostic command table over/under-inclusive | Wrong commands run | Cross-reference with iris-architecture evidence commands |
| Unrelated dirty file touch | User's Desktop work corrupted | Explicit per-phase forbidden-files lists; Phase 4 git diff check |

## 10. Implementation Handoff Notes

**Critical constraints:**
- Pure Markdown — no `dotnet` commands during implementation.
- Only 3 files: 1 new + 2 modified.
- Do not touch any Iris `.cs`, `.csproj`, `.slnx`, `appsettings.json`, or `DependencyInjection.cs`.
- Dirty `src/Iris.Desktop/*` files — do not touch.

**Risky areas:**
- Iron Law must be prominent (first section after metadata, bold formatting).
- Failure pattern catalog must use real Iris names from log_notes.md — not generic.
- Diagnostic command table must distinguish allowed from forbidden commands.

**Expected final state:**
- `.opencode/skills/iris-debug/SKILL.md` exists with 4-phase methodology, failure pattern catalog, diagnostic command table, Debug Report template.
- `AGENTS.md` references debug as diagnostic stage.
- `iris-engineering/SKILL.md` routes failures to `/debug`.
- No Iris source, tests, or infrastructure modified.

**Checks that must not be skipped:**
- Iron Law placement — first section, highly visible.
- Failure pattern catalog accuracy — real Iris examples.
- git diff to confirm only intended files changed.
- Cross-check all 17 acceptance criteria.

## 11. Open Questions

No blocking open questions.
