# Formal Audit Report: Iris Default Response Language is Russian

## 1. Summary

### Audit Status

**Blocked by P0 issues**

### Final Decision

**Changes requested**

### High-Level Result

The implementation matches the approved spec/design/plan structurally — four new `Iris.Application.Persona.Language` types, `PromptBuilder` injection, `AddIrisApplication` signature update, Desktop host wiring, and 14 new unit tests. Build (0/0), tests (155/155), and format (EXIT 0) verification all pass.

**However, the Russian baseline string in `LanguageInstructionBuilder._russianBaseline` is double-encoded mojibake on disk.** What was intended to be `"Ты — Айрис..."` is stored as the literal characters `"РўС‹ вЂ" Айрис..."` (i.e., the UTF-8 encoding of the mojibake). This is the single byte sequence that becomes the system prompt sent to Ollama at runtime — meaning Iris will instruct the model with garbled text, not Russian.

The bug is invisible to current tests because `LanguageInstructionBuilderTests` was written with the **same encoding error**, so its `text.Contains("РўС‹")` checks the mojibake against itself and "passes". `BuildForRussian_ReturnsNonEmptyTextContainingCyrillic` also passes because mojibake characters `Р`, `ў`, `С` happen to be in the Cyrillic Unicode block — they are *technically* Cyrillic codepoints, but they aren't real Russian.

This is a P0 correctness defect: it breaks the spec's primary functional requirement (FR-001..FR-005). Manual smoke (Phase 7) would surface it operationally but the audit must reject readiness now — automated tests give a false green.

A second P0 secondary defect is dead code: `RussianDefaultLanguagePolicy.GetSystemPrompt()` has an unreachable second branch (lines 21–24 vs 26) that returns the same value either way, with `normalized` computed but never used.

## 2. Context Reviewed

- **Specification:** `docs/specs/2026-05-01-iris-default-language-russian.spec.md` ✓
- **Design:** `docs/designs/2026-05-01-iris-default-language-russian.design.md` ✓
- **Implementation plan:** `docs/plans/2026-05-01-iris-default-language-russian.plan.md` ✓
- **Git status:** inspected — 14 modified tracked files (6 in scope), 6 new untracked in scope (`src/Iris.Application/Persona/Language/`, `tests/Iris.Application.Tests/Persona/`)
- **Git diff:** inspected — `--stat` shows +115 / -20 lines across 6 in-scope tracked files
- **Source files:** all 4 new `Iris.Application/Persona/Language/*.cs` files inspected; `PromptBuilder.cs`, `Iris.Application/DependencyInjection.cs`, `Iris.Desktop/DependencyInjection.cs` inspected
- **Test files:** all 5 modified/new test files inspected
- **Documentation/memory:** persona memory (`.agent/mem_library/03_iris_persona.md`) inspected as encoding reference
- **Verification evidence:** implementation result reports build 0/0, tests 155/155, format EXIT 0 (re-validated against current state below)

## 3. Pass 1 — Spec Compliance

### Result

**Failed**

### Findings

#### P0

- **P0-001: Russian baseline text is corrupted on disk (double-encoded mojibake)** — see Consolidated Findings (§9).

#### P1

- None.

#### P2

- **P2-001: Spec said `internal sealed`; design correctly overrode to `public sealed`** — implementation followed design. Recorded as Note for traceability; not an actual deviation.

#### Notes

- Structural FRs are satisfied: FR-006 (`LanguageOptions` POCO), FR-007 (defaults work without config), FR-002 (`PromptBuilder` no longer hardcodes English baseline).
- FR-001..FR-005 are **structurally** wired (the system prompt is fetched from `ILanguagePolicy`) but **functionally violated** because the actual prompt text is mojibake — Iris will not respond in Russian, will not preserve technical tokens, will not follow code-language rules. The prompt sent to Ollama is gibberish and effectively a no-op as a language instruction.

## 4. Pass 2 — Test Quality

### Result

**Failed**

### Findings

#### P0

- **P0-002: Tests cannot detect the encoding bug — they were written with the same broken encoding** — see Consolidated Findings (§9).

#### P1

- **P1-001: T-LANG-04 (`BuildForRussian_ReturnsNonEmptyTextContainingCyrillic`) is a weak guard against language correctness** — the Cyrillic block check passes for encoding garbage. See Consolidated Findings (§9).

#### P2

- **P2-002: T-LANG-06 keyword check has a precedence ambiguity** — the `&&` / `||` combination lacks explicit parentheses; intent is unclear. See Consolidated Findings (§9).

#### Notes

- Test placement is correct: `tests/Iris.Application.Tests/Persona/Language/` aligns with design §13.
- T-LANG-07/08 in `PromptBuilderTests` are well-formed: stub-policy injection check + negative regression against the legacy English literal.
- T-LANG-10/11 in `DependencyInjectionTests` follow project conventions and resolve `ILanguagePolicy` from a built `ServiceProvider`.
- The `[Theory]` use for null/empty/whitespace and unknown-language fallback is correct, but **all three fallback groups inherit the encoding bug** since they assert equality against the corrupted "ru" baseline.

## 5. Pass 3 — SOLID / Architecture Quality

### Result

**Passed**

### Findings

#### P0

- None.

#### P1

- None.

#### P2

- **P2-003: `LanguageInstructionBuilder` is stateless and could be `static class`** — design §17 N-PLAN-3 explicitly chose `public sealed class` for DI consistency with `SendMessageValidator`. Note only.

#### Notes

- Architecture boundaries fully preserved: 8/8 architecture tests pass without modification.
- `Iris.Application.Persona.Language` namespace correctly placed inside `Iris.Application`. No new project references, no new packages, no `InternalsVisibleTo`.
- DI lifetimes (Singleton for all four new types) match design §11.
- `PromptBuilder` constructor injection is clean. `Iris.Domain` and `Iris.Shared` are untouched.
- Desktop host wiring follows the established `appsettings.json` + null/whitespace fallback pattern (precedent: `Database:ConnectionString`, `ModelGateway:Ollama:*`).

## 6. Pass 4 — Clean Code / Maintainability

### Result

**Failed**

### Findings

#### P0

- **P0-003: `RussianDefaultLanguagePolicy.GetSystemPrompt()` has unreachable branch and dead variable** — both the `if (normalized.Length == 0)` branch and the fall-through return return `_builder.BuildForRussian()` identically. The `normalized` local is computed but unused. See Consolidated Findings (§9).

#### P1

- None.

#### P2

- **P2-004: `LanguageInstructionBuilder.BuildForRussian()` ignores context** — the method is parameterless; design §6 proposed a parameterized form. Functionally equivalent for the current single-language scope. See Consolidated Findings (§9).

#### Notes

- New types follow the project's `public sealed` convention.
- Naming is clear: `ILanguagePolicy`, `LanguageOptions`, `LanguageInstructionBuilder`, `RussianDefaultLanguagePolicy`. The "Russian" prefix on the policy class accurately conveys that Russian is the hard-coded default per spec.
- `_russianBaseline` private const placement is correct (single source of truth for the canonical text).

## 7. Additional Risk Checks

### Documentation / Memory

- **P1-002: `mem_library/03_iris_persona.md` §21 not yet appended** — Phase 8 (`/update-memory`) is still pending per the plan; not a defect, but Gate G is unfilled. Acceptable because the implementation result explicitly defers this to the operator-driven phase.
- `PROJECT_LOG.md`, `overview.md`, `debt_tech_backlog.md` likewise not yet updated. Same status.

### Reliability

- The mojibake means Iris's persona-level language directive is destroyed at the system-prompt boundary. The model receives a string that Ollama will tokenize into mostly nonsense Cyrillic codepoints; behavior is undefined. M-LANG-01..03 manual smoke (Phase 7) would catch this — but waiting until manual smoke means we waste an Ollama session validating a broken artifact.

### Performance

- `LanguageInstructionBuilder._russianBaseline` is a `private const`, evaluated at JIT — no per-call allocation. Acceptable.

### Security / Privacy

- No secrets, no I/O, no permissions surface change. Russian baseline text contains no PII. Design §12 invariants are honored.

### Migration / Rollback

- No migrations, no schema changes, no persisted state changes. Rollback is clean: revert the 6 modified files and delete the new types.

## 8. Verification Evidence

| Command | Result | Notes |
|---|---|---|
| `dotnet build .\Iris.slnx` | Passed (0 errors, 0 warnings) | Reported by `/implement` Phase 6; consistent with current state |
| `dotnet test .\Iris.slnx --no-build` | Passed (155/155, +14 new) | Reported by `/implement` Phase 6 |
| `dotnet format .\Iris.slnx --verify-no-changes` | Passed (EXIT 0) | Reported after `/implement` Phase 6 fixes |
| `git status --short` | Verified by audit | 14 modified, 6 new untracked, in scope |
| Encoding inspection of `LanguageInstructionBuilder.cs` | **Fail evidence** | First Cyrillic-looking byte sequence is `D0 A0 D1 9E ...` (UTF-8 of `Р`, `ў`) — the real intended `Ты` would be `D0 A2 D1 8B`. Compared against `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` which contains correct UTF-8 Cyrillic (`D0 B2 D0 B8 ...` = `ви` from "визуальная") — the project encoding norm is correct UTF-8 Cyrillic, only the new file was written with double-encoded text. |
| Manual smoke M-LANG-01..03 | Not Available | Phase 7 not run; would surface P0-001 operationally, but tests should have caught it first |

### Verification Gaps

- Phase 7 manual smoke against `llama3:latest` is the only place where the mojibake's runtime impact would be observable. Currently un-run.
- No automated test asserts that the system prompt contains a *specific known Russian word* at a specific Unicode codepoint — T-LANG-04 only checks "any character in the U+0400–U+04FF block", which mojibake satisfies.

## 9. Consolidated Findings

### P0 — Must Fix

#### P0-001: `LanguageInstructionBuilder._russianBaseline` is double-encoded mojibake on disk

- Evidence:
  - `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` lines 6–15.
  - Raw byte inspection: first quoted Cyrillic-looking bytes are `D0 A0 D1 9E D0 A1 E2 80 B9` which are the correct UTF-8 encoding of the Cyrillic letters `Р`, `ў`, `С`, `‹` — i.e., the literal mojibake of `Ты`. Real `Ты` would be UTF-8 bytes `D0 A2 D1 8B`.
  - Reference: `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` first Cyrillic bytes `D0 B2 D0 B8` = correct UTF-8 for `ви` (from "визуальная"). The Iris project encoding convention is correct UTF-8 Cyrillic; this single file diverges.
- Impact:
  - Iris's runtime system prompt is gibberish. The string sent to Ollama as `ChatModelRole.System` content is the literal Cyrillic letters `Р ў С‹ ...`, which spells nothing meaningful in any language and provides zero language-instruction signal to the model.
  - **Spec FR-001..FR-005 are violated functionally.** The structural plumbing is correct; the payload it carries is broken.
  - Manual smoke M-LANG-01..03 will fail (or worse — produce confused English/Russian mixed output that *looks* partially correct, masking the bug).
  - Operator-facing UX is broken on the very feature being shipped.
- Recommended fix:
  - Rewrite `LanguageInstructionBuilder.cs` saving with explicit UTF-8 encoding (without BOM is fine — matches project norm). Verify by re-running the byte-inspection command and confirming first quoted Cyrillic byte is `D0 A2` (the letter `Т`) for the opening word "Ты".
  - Add a stronger test that asserts a known real-Russian substring at a specific codepoint (see P1-001 fix).
  - This is a one-file fix — no other code changes required. Architecture, DI, and PromptBuilder integration are all correct.

#### P0-002: Test suite cannot detect the encoding bug — tests were written with the same broken encoding

- Evidence:
  - `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` lines 24–28, 38–40.
  - The keyword tokens used in `text.Contains(...)` checks are themselves mojibake: `"С„Р°Р№Р»"` (mojibake of `файл`), `"РєРѕРјРјРµРЅС‚Р°СЂРёРё РІ РєРѕРґРµ"` (mojibake of `комментарии в коде`), etc.
  - Because both the production `_russianBaseline` and the test substring literals share the same encoding error, the test's `Contains` check finds the corrupted substring inside the corrupted baseline and reports green.
- Impact:
  - The test suite is structurally a tautology for these checks. It enforces "the production string matches the test string", not "the production string is valid Russian".
  - Once P0-001 is fixed, T-LANG-05 and T-LANG-06 will **fail** (the fix produces real Russian but the test still searches for the mojibake) — which is actually the right outcome. The fix must update both the production text and the test keyword strings together.
- Recommended fix:
  - When fixing P0-001, simultaneously rewrite `LanguageInstructionBuilderTests.cs` (and any test expectation depending on Russian text) so that the keyword arguments to `text.Contains(...)` are real Russian (`файл`, `путь`, `идентификатор`, `команд`, `оригинал`, `код`, `английск`, `комментарии в коде`).
  - This is a paired correction with P0-001. Doing one without the other will break tests, which is exactly the signal we need.

#### P0-003: `RussianDefaultLanguagePolicy.GetSystemPrompt()` contains unreachable branch and unused local variable

- Evidence:
  - `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` lines 17–27:
    ```csharp
    public string GetSystemPrompt()
    {
        var normalized = (_options.DefaultLanguage ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            return _builder.BuildForRussian();
        }

        return _builder.BuildForRussian();
    }
    ```
  - The `if` branch and the fall-through return return identical values. The `normalized` local is computed but never used to influence the return.
- Impact:
  - **Correctness:** the design and spec require a deterministic Russian fallback; the current code achieves it accidentally — the `if` is dead, but the result happens to be correct because both branches are the same.
  - **Maintainability:** future readers will assume the `if` branch has meaning and may add code that depends on it. A reviewer adding `"en"` → English support might modify only one branch and break the spec's hard-default invariant (FR-005).
  - **Test coverage:** T-LANG-02 (null/empty) and T-LANG-03 (unknown language) pass for the same reason; they don't actually distinguish the branches.
- Recommended fix:
  - Remove the unused `if` block and the `normalized` local. Single-line body:
    ```csharp
    public string GetSystemPrompt()
    {
        return _builder.BuildForRussian();
    }
    ```
  - This honors design §6: per spec §10, only Russian is supported as default, and unknown values fall back to Russian. There is no second branch to write yet. If a future spec adds language switching, that's when the `if` reappears — with real semantics.

### P1 — Should Fix

#### P1-001: T-LANG-04 `BuildForRussian_ReturnsNonEmptyTextContainingCyrillic` is a weak language-correctness guard

- Evidence:
  - `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` lines 10–16, 44–55.
  - `ContainsCyrillic` returns true for any character in U+0400–U+04FF. Mojibake characters `Р` (U+0420), `ў` (U+045E), `С` (U+0421) all live in this block — so the test passes even when the text is encoding garbage.
- Impact:
  - The current test gives a false sense of "Russian content present". Together with P0-002, it means the test layer offered no defense against P0-001.
  - This is the test that should have failed loudly when the encoding bug was introduced.
- Recommended fix:
  - Replace or augment T-LANG-04 with an assertion against a specific known Russian word that uses **distinct Cyrillic letters not in the mojibake set**. Suggestions:
    - `Assert.Contains("Айрис", text)` — name of the assistant; combination of `А` (U+0410), `й` (U+0439), `р` (U+0440), `и` (U+0438), `с` (U+0441) — `й` and `и` and `с` are not in the typical Windows-1252→UTF-8 mojibake glyph set, so they reliably distinguish real Russian from mojibake.
    - Or: `Assert.Contains("русск", text)` — root of "русский".
  - Combined with P0-001 + P0-002, this makes the test layer self-protecting against future re-encoding.

#### P1-002: Phase 8 memory update not yet performed

- Evidence:
  - `.agent/mem_library/03_iris_persona.md` does not yet contain §21 "Default Language".
  - `.agent/PROJECT_LOG.md` has no entry for this work.
  - Plan §5 Phase 8 explicitly defers this to `/update-memory`.
- Impact:
  - Gate G is unfilled. Future agents cannot durably know that Russian is the default response language until memory is updated.
  - Not blocking implementation, but blocks readiness/merge claim.
- Recommended fix:
  - After P0 fixes land, run `/update-memory` per plan Phase 8.

### P2 — Backlog

#### P2-001: Spec/design visibility divergence preserved (record only)

- Evidence:
  - Spec AC-005 said `internal sealed`; design §15 R2 overrode to `public sealed`; implementation followed design.
- Impact:
  - None at runtime. The design's reasoning (no `InternalsVisibleTo` exists for `Iris.Application.Tests`) is correct.
- Recommended fix:
  - Optionally amend the spec post-hoc to align with design. Not blocking.

#### P2-002: T-LANG-06 keyword check has operator precedence ambiguity

- Evidence:
  - `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` lines 37–41:
    ```csharp
    Assert.True(
        text.Contains("...", ...) &&
        text.Contains("...", ...) ||
        text.Contains("...", ...),
        "...");
    ```
  - C# evaluates `&&` before `||`, so the expression is `(A && B) || C`. The intent is unclear — was this meant to be `A && (B || C)`?
- Impact:
  - Currently passes by coincidence. Future edits may rely on the wrong reading.
- Recommended fix:
  - Add explicit parentheses to encode intent. When fixing P0-002 (rewriting keywords to real Russian), also resolve this.

#### P2-003: `LanguageInstructionBuilder` could be `static class`

- Evidence:
  - `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` — type is stateless; the only method `BuildForRussian()` returns a `private const`.
- Impact:
  - Minor. Plan §11 N-PLAN-3 explicitly chose `public sealed class` with DI singleton for consistency with `SendMessageValidator`. Acceptable as-is.
- Recommended fix:
  - Backlog or no-op. Re-evaluate when the persona slice (`PersonaContextBuilder`) is wired in — the builder may gain dependencies then.

#### P2-004: `LanguageInstructionBuilder.BuildForRussian()` ignores its conceptual context

- Evidence:
  - The method takes no parameter and always returns the same constant.
  - Design §6 defined `public string Build(string normalizedLanguageCode)` "with the parameter currently ignored or used to assert non-null contract".
  - Implementation diverged: parameterless `BuildForRussian()`. Functionally equivalent for the current spec.
- Impact:
  - Minor naming/extension shape question. The parameterless form is simpler and arguably better for the current single-language scope. The design's parameter was forward-looking.
- Recommended fix:
  - Backlog. If/when a second language is added, reintroduce the parameterized form.

## 10. Suggested Fix Order

1. **P0-001** — rewrite `LanguageInstructionBuilder.cs` with correct UTF-8 Russian text. Verify with byte-level hex inspection that the first quoted Cyrillic byte is `D0 A2` (letter `Т`), not `D0 A0` (letter `Р`).
2. **P0-002** — rewrite `LanguageInstructionBuilderTests.cs` keywords to real Russian (`файл`, `код`, etc.), plus paired update to any other test that compares against the Russian baseline.
3. **P1-001** — strengthen T-LANG-04 with `Assert.Contains("Айрис", text)` or `Assert.Contains("русск", text)`.
4. **P0-003** — simplify `RussianDefaultLanguagePolicy.GetSystemPrompt()` to one-line body.
5. **P2-002** — add parentheses to T-LANG-06.
6. Re-run `dotnet build .\Iris.slnx`, `dotnet test .\Iris.slnx`, `dotnet format .\Iris.slnx --verify-no-changes`. Tests must remain green; T-LANG-04 should now genuinely prove Russian content.
7. **Phase 7** — manual smoke M-LANG-01..03 against `llama3:latest`.
8. **P1-002** — run `/update-memory` per plan Phase 8.
9. **P2-001** — optional spec amendment to align AC-005 with design §15 R2.

P2-003 and P2-004 are pure backlog; defer until persona slice work.

## 11. Readiness Decision

**Not ready.**

The structural implementation is correct and verification mechanically passes (build, tests, format, architecture). However, the actual product feature being shipped — Iris responding in Russian — is broken at the byte level. The failure is invisible to the test suite due to mirror-mojibake. Approving this would ship a feature that demonstrably does not satisfy its primary FR.

P0-001 alone blocks merge. P0-002 blocks any meaningful test signal. P0-003 is dead code.

Scope of fixes: P0-001/002 live in `LanguageInstructionBuilder.cs` + `LanguageInstructionBuilderTests.cs`. P0-003 is a one-line simplification in `RussianDefaultLanguagePolicy.cs`. After fixing, re-run verification; readiness flips to "Ready with P2 backlog" pending Phase 7 + P1-002.

## Execution Note

No fixes were implemented.
No files were modified.

## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | Reviewed | `docs/specs/2026-05-01-iris-default-language-russian.spec.md` |
| B — Design | Reviewed | `docs/designs/2026-05-01-iris-default-language-russian.design.md` |
| C — Plan | Reviewed | `docs/plans/2026-05-01-iris-default-language-russian.plan.md` |
| D — Verify | Reviewed (mechanically passing, semantically blocked by P0-001) | Verification evidence in §8 above |
| E — Architecture Review | Covered in Pass 3 — Passed | §5 above; no boundary changes detected, 8/8 architecture tests green |
| F — Audit | ✅ Satisfied | This audit |
| G — Memory | ⚠️ Not updated | P1-002 above; Phase 8 (`/update-memory`) pending per plan |
