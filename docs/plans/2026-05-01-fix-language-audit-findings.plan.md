# Implementation Plan: Fix Language Audit Findings P0-001, P0-002, P0-003, P1-001

## 1. Plan Goal

Resolve four audit findings against the "Iris default response language: Russian" implementation so that:

- the system prompt sent to Ollama is real Russian text (not mojibake) — fixes **P0-001**;
- the test suite genuinely validates Russian content (not mirror-mojibake tautology) — fixes **P0-002**;
- `RussianDefaultLanguagePolicy.GetSystemPrompt()` is free of dead code — fixes **P0-003**;
- `BuildForRussian_ReturnsNonEmptyTextContainingCyrillic` is strengthened to fail loudly on encoding regression — fixes **P1-001**;
- (Optional) T-LANG-06 boolean expression has explicit operator precedence — addresses **P2-002**.

This plan implements no new design and changes no architecture. It is a content-correction pass against three already-existing files inside `Iris.Application.Persona.Language` and its unit tests.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-01-iris-default-language-russian.spec.md` — FR-001..FR-008 unchanged; the plan honors all five language rules embedded in the Russian baseline text.
- **Design:** `docs/designs/2026-05-01-iris-default-language-russian.design.md` — boundaries, contracts, DI lifetimes, and visibility decisions all stay as-is.
- **Original implementation plan:** `docs/plans/2026-05-01-iris-default-language-russian.plan.md` — Phases 0–6 implemented; Phase 7 (manual smoke) and Phase 8 (memory) deferred. This plan inserts Phases 6.1–6.5 as remediation between completed Phase 6 and pending Phase 7.
- **Audit report:** `docs/audits/2026-05-01-iris-default-language-russian.audit.md` — defines P0/P1/P2 findings, severity, and recommended fix order (§10).
- **Debug report:** prior `/debug` output in the active workflow — confirms root causes at byte level, codepoint mismatches, and mirror-mojibake mechanism. Establishes the byte-level canary requirement.
- **Rules:** `.opencode/rules/workflow.md`, `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/dotnet.md`, `.opencode/rules/verification.md`, `.opencode/rules/memory.md`.
- **EditorConfig:** `.editorconfig` — `charset = utf-8`, `end_of_line = crlf`, `csharp_style_var_for_built_in_types = true:warning`, `dotnet_naming_rule.private_internal_fields_underscore` (private fields/consts must be `_camelCase`).
- **Reference encoding source:** `.agent/mem_library/03_iris_persona.md` — known-good UTF-8 Russian content; section "Айрис" at codepoints U+0410 U+0439 U+0440 U+0438 U+0441 confirmed.

### Assumptions

- Russian baseline text content (semantic meaning) does not change in this plan — only its byte representation. The five language rules embedded in the original mojibake string (Russian persona; respond in Russian; preserve technical tokens in English; code & code-comments in English with Russian prose around code; sticky Russian even when user writes English) remain the spec'd content per FR-001..FR-005.
- The Edit tool, when given correctly-encoded Russian source/target strings, will write UTF-8 bytes matching its input — no console-codepage corruption is introduced at the tool boundary. The Read tool's display of mojibake glyphs reflects only console rendering, not file content semantics.
- `dotnet format` will not silently rewrite the Russian literal back to mojibake. EditorConfig declares UTF-8; the formatter respects file encoding.
- The agent executing this plan can paste real Russian text into Edit/Write tool calls. If the operator's environment cannot, escalation is required (see Phase 1.1 escalation gate).
- `Iris.Architecture.Tests` (8/8 currently green) will remain unaffected — no project references, no namespace moves, no DI changes.
- The 14 modified tracked files and 21 untracked files in the working tree are the same set described in the audit; no new dirty changes introduced between audit and this plan.

## 3. Scope Control

### In Scope

- `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` — rewrite `_russianBaseline` literal with correct UTF-8 Russian.
- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` — simplify `GetSystemPrompt()` body.
- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` — strengthen T-LANG-04, replace mojibake substrings in T-LANG-05/06 with real Russian, add explicit parentheses to T-LANG-06.
- Verification triad: `dotnet build .\Iris.slnx`, `dotnet test .\Iris.slnx`, `dotnet format .\Iris.slnx --verify-no-changes`.
- Byte-level canary check on `LanguageInstructionBuilder.cs` after rewrite (PowerShell `[System.IO.File]::ReadAllBytes`).

### Out of Scope

- `src/Iris.Application/Persona/Language/LanguageOptions.cs` — already correct (ASCII only, no Russian).
- `src/Iris.Application/Persona/Language/ILanguagePolicy.cs` — already correct (no string content).
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` — DI injection of `ILanguagePolicy` is correct; not touched.
- `src/Iris.Application/DependencyInjection.cs` — registrations are correct; not touched.
- `src/Iris.Desktop/DependencyInjection.cs` — host wiring is correct; not touched.
- All test files outside `LanguageInstructionBuilderTests.cs`. `RussianDefaultLanguagePolicyTests.cs`, `PromptBuilderTests.cs`, `SendMessageHandlerTests.cs`, `DependencyInjectionTests.cs` already use real text or non-Russian assertions — unchanged.
- Phase 7 manual smoke (M-LANG-01..03) — separate phase in the original plan; runs only after this remediation passes verification.
- Phase 8 memory update (P1-002) — separate `/update-memory` invocation; not part of this plan's edits.
- P2-001 (spec visibility amendment), P2-003 (`static class` shape), P2-004 (parameterless `BuildForRussian` shape) — backlog only.
- All other dirty-tree files (`AGENTS.md`, `.opencode/skills/iris-engineering/SKILL.md`, `.opencode/commands/implement.md`, Phase 6 Desktop UI/DI files, untracked skill drafts and other audit/spec/design/plan documents).

### Forbidden Changes

- No new project references.
- No new NuGet packages.
- No `InternalsVisibleTo` additions.
- No `IOptions<>` / `Configure<>` / `Bind(...)` introduction (POCO-options idiom stays per design §11).
- No movement of types between projects or namespaces.
- No public contract changes (`ILanguagePolicy`, `LanguageInstructionBuilder.BuildForRussian()`, `LanguageOptions` shape stay).
- No semantic rewriting of the Russian baseline text — only re-encoding the same five language rules with correct UTF-8 bytes. The five rules from the original (corrupted) text per spec FR-001..FR-005 are preserved verbatim in meaning.
- No introduction of language-switching logic (FR-005 hard default to Russian).
- No deletion or weakening of any other test in the solution.
- No formatting normalization of unrelated files.
- No mutation of memory files.
- No commits, no `git push`, no `git checkout`, no `git reset`, no `git clean`.
- No `dotnet format` (mutating). Only `dotnet format --verify-no-changes` is allowed.
- No dependency on the legacy mojibake substrings — once Phase 1 lands, mojibake substrings must not appear anywhere in the solution.

## 4. Implementation Strategy

The plan follows a **strengthened-test-first (TDD red→green)** ordering. This is critical because the original implementation was mechanically green while semantically broken, and we must avoid silently re-mirroring the same bug.

**Phase 0** — read-only reconnaissance. Confirm working tree state, byte-level baseline, and that no new dirty changes appeared since audit. No edits.

**Phase 1** — strengthen T-LANG-04 to assert a known real-Russian substring (`Айрис`) that mojibake cannot satisfy. Verify the test now FAILS against the current corrupted production literal — this establishes the canary works (red signal). This is the only phase where a test failure is the desired outcome.

**Phase 2** — rewrite the production `_russianBaseline` literal with correct UTF-8 Russian text encoding the same five language rules. Verify byte-level canary (`D0 A2 D1 8B` at literal start = real `Ты`, not `D0 A0 D1 9E` mojibake). Verify T-LANG-04 from Phase 1 now PASSES (green signal).

**Phase 3** — rewrite the mojibake substrings in T-LANG-05 and T-LANG-06 to real Russian, and add explicit parentheses to T-LANG-06 boolean expression (P2-002 swept in). Verify all T-LANG-* tests pass against the corrected production string.

**Phase 4** — simplify `RussianDefaultLanguagePolicy.GetSystemPrompt()` to single-line body. Verify T-LANG-01..03 (10 rows after `[Theory]`) still pass.

**Phase 5** — full verification triad: `dotnet build`, `dotnet test` (155/155 expected), `dotnet format --verify-no-changes`, plus byte-level canary re-check on `LanguageInstructionBuilder.cs`.

**Phase 6** — handoff. State remaining audit gates (Phase 7 manual smoke, Phase 8 memory update). No edits.

**Why this ordering is safe:**

- TDD red→green forces the production fix to satisfy the strengthened assertion before silently passing on tautology.
- Byte-level canary in Phase 2 makes encoding regression impossible to ship green.
- Production fix and test substring fix are kept in **separate phases** so that an intermediate broken state surfaces clearly (Phase 2 produces real Russian production + still-mojibake T-LANG-05/06 → T-LANG-05/06 fail until Phase 3 lands; this is informative, not a defect).
- The dead-branch removal (Phase 4) is independent of encoding work and ordered last among code edits to keep encoding-related diffs reviewable in isolation.
- All six phases together form a single logical change set; commits (if requested by operator) should land as one atomic unit per `.opencode/rules/dotnet.md` "Project Conventions".

**Critical write-technique note for the implementing agent:** when rewriting the Russian literal in Phase 2 and the substrings in Phase 3, the Edit/Write tool calls must contain the *real* Russian characters in the `newString` argument, not mojibake. After each such write, the agent must read back the file bytes via `[System.IO.File]::ReadAllBytes` and confirm the first quoted Cyrillic byte at the literal is `D0 A2` (UTF-8 of `Т`). If the byte check fails, immediately rollback that file (revert content via Edit tool with the previous content) and escalate — the encoding pipeline is still broken and direct implementation is unsafe.

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Confirm working tree state, audit findings still apply, no new dirty changes since audit, and capture the byte-level baseline as the canary reference point.

#### Files to Inspect

- `git status --short` — confirm 14 modified tracked + 21 untracked, language-scope files present.
- `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` — read full content + bytes.
- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` — read full content.
- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` — read full content.
- `tests/Iris.Application.Tests/Persona/Language/RussianDefaultLanguagePolicyTests.cs` — read for context (no edits, but T-LANG-01..03 test values must remain consistent).
- `.editorconfig` — confirm `charset = utf-8`, `end_of_line = crlf`.
- `.agent/mem_library/03_iris_persona.md` — read as encoding reference; capture a known-good UTF-8 Russian byte sequence.

#### Files Likely to Edit

- None (read-only phase).

#### Files That Must Not Be Touched

- All files.

#### Steps

1. Run `git status --short` and confirm the in-scope working-tree state matches audit §2.
2. Read `LanguageInstructionBuilder.cs` content and capture its bytes. Confirm bytes at offset 152 start with `22 D0 A0 D1 9E` (opening quote + mojibake `Р ў`). Record this as "the bug we're fixing".
3. Read `RussianDefaultLanguagePolicy.cs` and confirm lines 17–27 contain the dead `if` and unused `normalized` local.
4. Read `LanguageInstructionBuilderTests.cs` and confirm lines 24–28 and 38–40 contain mojibake substrings at codepoints in U+0400–U+04FF + U+201E + U+00B0 + U+2116 etc.
5. Read `.agent/mem_library/03_iris_persona.md` bytes around an "Айрис" or "русск" occurrence and confirm `D0 90 D0 B9 D1 80 D0 B8 D1 81` for `Айрис` (or analogous correct UTF-8 for `русск`). Record this as "the encoding norm we're matching".
6. Verify `Iris.slnx` exists and `dotnet build .\Iris.slnx --nologo --verbosity minimal` returns 0 errors, 0 warnings — establishes the green baseline before any edits.

#### Expected Outcome

- Working tree state matches audit §2 with no surprises.
- Byte-level evidence captured for `LanguageInstructionBuilder.cs` (mojibake) and `.agent/mem_library/03_iris_persona.md` (real Russian).
- Build green.

#### Verification

- `git status --short` produces expected file list.
- `[System.IO.File]::ReadAllBytes("...\LanguageInstructionBuilder.cs")[152..160]` matches `22 D0 A0 D1 9E D0 A1 E2 80 B9`.
- `dotnet build .\Iris.slnx` exits 0.

#### Rollback

No code changes; no rollback needed.

#### Acceptance Checkpoint

Operator/agent confirms baseline state captured before proceeding to Phase 1.

---

### Phase 1 — Strengthen T-LANG-04 (canary red signal)

#### Goal

Replace or augment T-LANG-04 (`BuildForRussian_ReturnsNonEmptyTextContainingCyrillic`) so that it asserts a known real-Russian substring (`Айрис`) that mojibake cannot satisfy. Confirm the strengthened test FAILS against the still-corrupted production literal — this proves the canary detects the bug it's meant to detect.

#### Files to Inspect

- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` (full content, especially lines 9–16 and 44–55).
- `docs/specs/2026-05-01-iris-default-language-russian.spec.md` AC-005, AC-006 — confirm the strengthened assertion does not contradict acceptance criteria (it does not; it strengthens FR-004 verification).

#### Files Likely to Edit

- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs`.

#### Files That Must Not Be Touched

- All production files (Phase 1 is test-only).
- All other test files.
- All non-test files.

#### Steps

1. In `LanguageInstructionBuilderTests.cs`, modify `BuildForRussian_ReturnsNonEmptyTextContainingCyrillic` to add a real-Russian substring assertion. The new assertion is `Assert.Contains("Айрис", text, StringComparison.Ordinal)` (the assistant's name; uses Cyrillic letters `А` U+0410, `й` U+0439, `р` U+0440, `и` U+0438, `с` U+0441 — `й`, `и`, `с` are absent from the typical Windows-1252→UTF-8 mojibake glyph set).
2. Keep the existing `ContainsCyrillic` block check as a coarse guard, OR remove it as redundant — implementer's choice; either preserves the FR-004 acceptance criterion. Prefer keeping for defense in depth.
3. The `ContainsCyrillic` private helper may stay (still useful as a quick filter) or be deleted if no longer referenced.
4. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests.BuildForRussian_ReturnsNonEmptyTextContainingCyrillic"` — **expect FAILURE**. The failure message should reference the missing `Айрис` substring.
5. **If the test passes:** STOP. The production file may already have been silently fixed, or the mojibake has a coincidental `Айрис` substring (extremely unlikely given the byte-level inspection — but verify). Re-read production bytes; if mojibake still present and test still passes, escalate: the assertion implementation is wrong.

#### Expected Outcome

- T-LANG-04 fails with a clear message about missing `Айрис` substring.
- All other tests in `LanguageInstructionBuilderTests` and elsewhere remain unaffected (they still pass on mojibake).
- Production code unchanged.

#### Verification

- `dotnet test ... --filter "...BuildForRussian_ReturnsNonEmptyTextContainingCyrillic"` exits non-zero with failure message naming `Айрис` or "Russian content not found" or similar.
- `dotnet test ... --filter "FullyQualifiedName~LanguageInstructionBuilderTests"` shows 1 failed, 2 passed (T-LANG-05 and T-LANG-06 still tautology-pass on mojibake).
- `dotnet test ... --filter "FullyQualifiedName~RussianDefaultLanguagePolicyTests"` still 7/7 pass (untouched).
- Byte check on `LanguageInstructionBuilderTests.cs`: should now contain real Russian `Айрис` codepoints (U+0410 U+0439 U+0440 U+0438 U+0441 → bytes `D0 90 D0 B9 D1 80 D0 B8 D1 81`) somewhere in the file. Run `Select-String -Path <test-file> -Pattern "Айрис" -SimpleMatch` and expect 1+ match.

#### Rollback

- If the strengthened test does not fail as expected (Step 5 stop condition): revert `LanguageInstructionBuilderTests.cs` via Edit tool back to its pre-Phase-1 content. Do not proceed to Phase 2.
- If the agent's environment cannot write the literal Russian word `Айрис` correctly to the test file (byte check shows mojibake there too): revert and escalate. Direct implementation is unsafe in this environment.

#### Acceptance Checkpoint

T-LANG-04 fails with the expected message AND the test file contains real-Russian bytes for `Айрис`. If both true → Phase 1 complete, proceed to Phase 2. Otherwise → rollback and escalate.

---

### Phase 2 — Rewrite production `_russianBaseline` with correct UTF-8 (canary green signal)

#### Goal

Rewrite `LanguageInstructionBuilder._russianBaseline` so that the bytes on disk encode real UTF-8 Russian text containing the five language rules from spec FR-001..FR-005. Verify byte-level canary at file offset matches `D0 A2 D1 8B` (real `Ты`). Verify Phase 1's strengthened T-LANG-04 now passes.

#### Files to Inspect

- `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` (full content, lines 5–15).
- `docs/specs/2026-05-01-iris-default-language-russian.spec.md` FR-001..FR-005 — confirm the five language rules to encode.
- `docs/designs/2026-05-01-iris-default-language-russian.design.md` §6 — confirm `_russianBaseline` private const placement and visibility decisions.
- `.editorconfig` — confirm the file editorconfig settings (CRLF, UTF-8).

#### Files Likely to Edit

- `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs`.

#### Files That Must Not Be Touched

- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` (Phase 4).
- `src/Iris.Application/Persona/Language/LanguageOptions.cs`.
- `src/Iris.Application/Persona/Language/ILanguagePolicy.cs`.
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs`.
- `src/Iris.Application/DependencyInjection.cs`.
- `src/Iris.Desktop/DependencyInjection.cs`.
- All test files (Phase 3).
- All other production files.

#### Steps

1. Use the Edit tool to replace the entire `_russianBaseline` literal block (lines 5–15) with the same five language rules expressed in correct UTF-8 Russian. Content semantics must satisfy FR-001..FR-005:
   - **Rule 1 (FR-001, FR-002):** establishes Iris/Айрис as a local personal AI companion; calm, precise, helpful, privacy-aware; speaks naturally and personally; does not falsely claim biological humanity; helps the user via the local desktop application.
   - **Rule 2 (FR-001):** respond in Russian.
   - **Rule 3 (FR-003):** preserve technical tokens — file names, paths, namespaces, identifiers, commands, environment variables, code blocks — in original English.
   - **Rule 4 (FR-004):** in code-bearing answers — code itself and comments inside code stay in English; explanatory prose around the code is in Russian.
   - **Rule 5 (FR-005):** keep Russian as the response language even if the user writes in another language.
2. Preserve the existing `private const string _russianBaseline =` declaration shape and the multi-line concatenation pattern (each `+` on a new line). The five rules may be distributed across the same number of concatenated lines as the original, or a slightly different number — implementer's discretion. The `private const` modifier and the underscore prefix must remain (private-fields-underscore-prefix EditorConfig rule).
3. Do NOT change any other line in `LanguageInstructionBuilder.cs` — namespace, class declaration, `BuildForRussian()` method body, file structure all stay.
4. After the Edit tool call returns success, **immediately** run the byte-level canary:
   ```powershell
   $bytes = [System.IO.File]::ReadAllBytes("E:\Work\Iris\src\Iris.Application\Persona\Language\LanguageInstructionBuilder.cs")
   # Find offset of first quote ", inspect 4 bytes after it.
   ```
   The first 4 bytes after the opening `"` of the `_russianBaseline` literal must be `D0 A2 D1 8B` (UTF-8 of `Ты`) if the rule starts with `Ты — Айрис...`, OR the corresponding correct UTF-8 sequence for whatever Russian word the rule starts with. Mojibake bytes `D0 A0 D1 9E` (`Р ў`) MUST NOT appear anywhere in the file.
5. **If the byte check shows mojibake at any offset:** revert the file via Edit tool back to its pre-Phase-2 content and STOP. Escalate — encoding pipeline is corrupted at the tool boundary and direct implementation cannot proceed.
6. Run `dotnet build .\Iris.slnx --nologo --verbosity minimal` — expect 0 errors, 0 warnings.
7. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests.BuildForRussian_ReturnsNonEmptyTextContainingCyrillic"` — expect PASS (the canary now finds `Айрис`).
8. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests"` — expect 1 PASS (T-LANG-04), 2 FAIL (T-LANG-05, T-LANG-06 still searching for mojibake substrings; this is the expected intermediate state to be fixed in Phase 3).
9. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~RussianDefaultLanguagePolicyTests"` — expect all 7 PASS (their assertions don't depend on Russian content semantics).
10. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~PromptBuilderTests|FullyQualifiedName~SendMessageHandlerTests|FullyQualifiedName~DependencyInjectionTests"` — expect all PASS (they assert injection plumbing, not Russian content).

#### Expected Outcome

- `LanguageInstructionBuilder.cs` bytes now encode real UTF-8 Russian throughout the literal.
- Byte-level canary check passes.
- T-LANG-04 passes (canary green).
- T-LANG-05 and T-LANG-06 fail (expected intermediate state — fixed in Phase 3).
- All non-`LanguageInstructionBuilderTests` tests pass.
- Build green, no warnings.

#### Verification

- Byte-level canary: first 4 bytes after `_russianBaseline` opening quote are NOT `D0 A0 D1 9E`; they ARE the correct UTF-8 for whatever Russian word the rewritten text starts with (e.g., `D0 A2 D1 8B` for `Ты`).
- `Select-String -Path <file> -Pattern "Р ў" -SimpleMatch` returns 0 matches (no mojibake glyphs).
- `Select-String -Path <file> -Pattern "Айрис" -SimpleMatch` returns 1+ match.
- `dotnet build .\Iris.slnx` → 0/0.
- T-LANG-04 passes; T-LANG-05/06 fail (intermediate); other tests pass.

#### Rollback

- Use Edit tool to restore `_russianBaseline` to its pre-Phase-2 (mojibake) content. Re-run byte check to confirm rollback fully restored the original bytes.
- If byte check after rollback does not match the Phase 0 baseline exactly: escalate — the file may have been silently mutated by another process during the phase.

#### Acceptance Checkpoint

Byte canary green AND T-LANG-04 green AND build clean → Phase 2 complete, proceed to Phase 3. Any byte-canary failure → rollback + escalate.

---

### Phase 3 — Rewrite test substrings to real Russian; add explicit parentheses (T-LANG-05/06 + P2-002)

#### Goal

Bring T-LANG-05 (`BuildForRussian_ContainsTechnicalTokenRule`) and T-LANG-06 (`BuildForRussian_ContainsCodeStaysEnglishRule`) back to green by replacing their mojibake substring arguments with real Russian substrings that the corrected production literal contains. Sweep in P2-002 by adding explicit parentheses to T-LANG-06's boolean expression.

#### Files to Inspect

- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` (lines 18–42).
- The new content of `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` (post-Phase-2) — to confirm which real-Russian substrings actually appear in the rewritten baseline.

#### Files Likely to Edit

- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs`.

#### Files That Must Not Be Touched

- All production files.
- All other test files.

#### Steps

1. In `LanguageInstructionBuilderTests.cs`, replace the mojibake substrings in T-LANG-05's `text.Contains(...)` calls (currently lines 24–28) with real Russian substrings drawn from the spec's FR-003 vocabulary. Suggested set (any subset that matches Phase 2's rewritten baseline):
   - `файл` (file)
   - `путь` (path)
   - `идентификатор` (identifier)
   - `команд` (command — root form)
   - `оригинал` (original — root form)
   - The `||` semantics from the original test (any one match suffices) is preserved. Use `StringComparison.Ordinal` (real-Russian case-sensitivity) instead of `OrdinalIgnoreCase` if the rewritten production text uses consistent casing — implementer's discretion; either is acceptable per FR-004 verification.
2. In T-LANG-06 (currently lines 32–42), replace mojibake substrings (`РєРѕРґ`, `Р°РЅРіР»РёР№СЃРє`, `РєРѕРјРјРµРЅС‚Р°СЂРёРё РІ РєРѕРґРµ`) with real Russian: `код`, `английск`, `комментарии в коде`. Adjust to whatever forms appear in the rewritten production string.
3. Add explicit parentheses to the boolean expression in T-LANG-06 to encode intent. The original expression is `A && B || C` which C# evaluates as `(A && B) || C`. Choose one of:
   - `(A && B) || C` — current de-facto behavior, made explicit.
   - `A && (B || C)` — only if Phase 0 spec re-read indicates this was the intent.
   The implementer should choose based on FR-004 wording: if the rule is "code AND English language is the requirement, OR an explicit comments-in-code phrasing also satisfies", then `(A && B) || C` is the right reading. Default to `(A && B) || C`.
4. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests"` — expect all 3 tests PASS.
5. Verify byte-level: `Select-String -Path <test-file> -Pattern "файл|код|комментарии" -SimpleMatch` returns expected matches. `Select-String -Path <test-file> -Pattern "Р°Р№Р»|РєРѕРґ|РєРѕРјРјРµРЅС‚" -SimpleMatch` returns 0 matches (no remaining mojibake).
6. **Stop condition:** if any T-LANG-* test fails, the production text and the test substrings are out of sync. Re-read production content, choose substring arguments that actually appear in production text, and edit accordingly. Do NOT modify production text to match the tests — production text is the source of truth post-Phase-2.

#### Expected Outcome

- T-LANG-04, T-LANG-05, T-LANG-06 all pass against real-Russian production text.
- T-LANG-06 has unambiguous operator precedence.
- Test file contains zero mojibake bytes.

#### Verification

- `dotnet test ... --filter "FullyQualifiedName~LanguageInstructionBuilderTests"` → 3/3 pass.
- Byte search for known mojibake patterns (`Р°`, `Р№`, `Рё`, `СЃ`, `РєРѕРґ`, etc.) in the test file returns 0 matches.
- `dotnet build .\Iris.slnx` → 0/0 (no warnings introduced by parentheses or substring changes).

#### Rollback

- Use Edit tool to restore `LanguageInstructionBuilderTests.cs` to its pre-Phase-3 content (which is its post-Phase-1 content). T-LANG-04 will still pass (Phase 1 + Phase 2 good); T-LANG-05/06 will fail (mojibake substrings against real Russian production). This is a stable intermediate state — Phase 3 can be re-attempted from there.

#### Acceptance Checkpoint

All 3 T-LANG tests pass AND byte search confirms no mojibake remains in the test file → Phase 3 complete, proceed to Phase 4.

---

### Phase 4 — Simplify `RussianDefaultLanguagePolicy.GetSystemPrompt()` (P0-003)

#### Goal

Remove the dead `if (normalized.Length == 0)` branch and the unused `normalized` local. Replace the body with a single-line return.

#### Files to Inspect

- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` (full content; especially lines 17–27).
- `tests/Iris.Application.Tests/Persona/Language/RussianDefaultLanguagePolicyTests.cs` — confirm T-LANG-01..03 do not depend on the dead `if` branch (they test return values across null/empty/whitespace and unknown-language inputs; both branches return identical values, so removal is safe).

#### Files Likely to Edit

- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs`.

#### Files That Must Not Be Touched

- All test files.
- All other production files.
- The constructor of `RussianDefaultLanguagePolicy` — `_options` field stays (still injected; future language-switching may use it; removing it is out of scope per P2-004 backlog).

#### Steps

1. Edit `RussianDefaultLanguagePolicy.cs` `GetSystemPrompt()` body. New body is a single statement: `return _builder.BuildForRussian();`. Remove:
   - the `var normalized = (_options.DefaultLanguage ?? string.Empty).Trim();` line;
   - the `if (normalized.Length == 0) { return _builder.BuildForRussian(); }` block.
2. Keep:
   - the `_options` private readonly field declaration;
   - the constructor and its `ArgumentNullException.ThrowIfNull(options)` guard (FR-006 guards stay);
   - `_builder` field and the constructor's null-check on it.
3. Run `dotnet build .\Iris.slnx --nologo --verbosity minimal` — expect 0 errors, 0 warnings. Specifically watch for IDE0052 ("unused private field `_options`") — it should NOT fire because `_options` is read in the constructor's `ArgumentNullException.ThrowIfNull` (which counts as a read in C# analyzers). If it does fire, suppress with `// kept for future language-switching per spec FR-005` rationale comment, OR escalate — `_options` removal is out of scope.
4. Run `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~RussianDefaultLanguagePolicyTests"` — expect 7/7 PASS.

#### Expected Outcome

- `GetSystemPrompt()` is a 3-line method (signature + body + closing brace), single return statement.
- `_options` field stays (architectural decision per spec FR-005 / design §6).
- All 7 T-LANG-01..03 tests pass.

#### Verification

- `dotnet build .\Iris.slnx` → 0/0.
- `dotnet test ... --filter "FullyQualifiedName~RussianDefaultLanguagePolicyTests"` → 7/7 pass.
- Source inspection: `GetSystemPrompt()` body has no `if`, no `var`, no `normalized` references.

#### Rollback

- Use Edit tool to restore the multi-line body. The pre-Phase-4 state is functionally identical (both branches returned the same value), so rollback is safe.

#### Acceptance Checkpoint

Build green AND `RussianDefaultLanguagePolicyTests` 7/7 pass AND source inspection confirms single-return body → Phase 4 complete, proceed to Phase 5.

---

### Phase 5 — Full verification triad and final byte canary

#### Goal

Run the full Iris verification triad to confirm the remediation does not break the rest of the solution. Re-confirm byte-level canary on `LanguageInstructionBuilder.cs`. Capture exact verification evidence for the next audit pass.

#### Files to Inspect

- `Iris.slnx` exists.
- `LanguageInstructionBuilder.cs` bytes (final canary read).

#### Files Likely to Edit

- None (verification-only phase).

#### Files That Must Not Be Touched

- All files. This is a non-mutating phase. `dotnet format` (mutating) is forbidden; only `--verify-no-changes` is allowed.

#### Steps

1. `dotnet build .\Iris.slnx` — capture full output. Expect "0 errors, 0 warnings".
2. `dotnet test .\Iris.slnx --no-build` — capture full output. Expect 155/155 passed.
3. `dotnet format .\Iris.slnx --verify-no-changes` — capture exit code. Expect EXIT 0.
4. Final byte-level canary on `LanguageInstructionBuilder.cs`:
   - File size sane (likely ~2200–2800 bytes; was 2544 before; real Russian uses similar UTF-8 byte volume).
   - No BOM.
   - First 4 bytes after `_russianBaseline` opening quote: real UTF-8 Cyrillic, not `D0 A0 D1 9E`.
   - Full-text search for known mojibake patterns (`РўС‹`, `РђР№СЂРёСЃ`, `Р°Р№Р»`, `РєРѕРјР°РЅРґ`, `РєРѕРґ`, `Р°РЅРіР»РёР№СЃРє`, `вЂ"`) returns 0 hits.
   - Full-text search for real Russian markers (`Айрис`, `файл`, `русск`, `Ты`, `англий`) returns expected hits.
5. Final byte-level canary on `LanguageInstructionBuilderTests.cs` — same mojibake-absence and real-Russian-presence checks.
6. Architecture tests sanity: `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~Iris.Architecture.Tests"` — expect 8/8 pass (no architecture changes were made; failure here means an unintended boundary impact).
7. Compile a verification report fragment for handoff:
   - Exact command lines.
   - Pass/fail/skipped counts.
   - Byte-canary evidence.
   - Files changed (3 expected: `LanguageInstructionBuilder.cs`, `RussianDefaultLanguagePolicy.cs`, `LanguageInstructionBuilderTests.cs`).

#### Expected Outcome

- Build clean (0/0).
- All tests pass (155/155).
- Format verify clean (EXIT 0).
- Both Russian-bearing files contain only real-Russian Cyrillic codepoints; no mojibake bytes.
- Architecture tests 8/8 green.

#### Verification

- `dotnet build .\Iris.slnx` exit 0, "0 ошибок" / "0 errors".
- `dotnet test .\Iris.slnx --no-build` reports `всего 155, длительность <X> ms` with `пройдено 155, не пройдено 0`.
- `dotnet format .\Iris.slnx --verify-no-changes` exit 0.
- Byte canaries pass for both files.

#### Rollback

- If any verification step fails, identify which phase introduced the failure and rollback that phase's edits (not all phases). Re-run verification after each rollback to localize the regression.
- If the failure cannot be localized to a single phase, rollback all three modified files to their post-audit state and escalate.

#### Acceptance Checkpoint

All four verification commands green AND both byte canaries green → Phase 5 complete, audit findings P0-001/002/003 + P1-001 closed mechanically.

---

### Phase 6 — Handoff

#### Goal

Document the resolved findings, remaining audit gates, and recommended next steps. Hand off to operator.

#### Files to Inspect

- `docs/audits/2026-05-01-iris-default-language-russian.audit.md` (just for confirmation that the resolved findings map cleanly).

#### Files Likely to Edit

- None.

#### Files That Must Not Be Touched

- All files. No memory updates, no audit re-write, no source edits. Memory update belongs to a separate `/update-memory` invocation.

#### Steps

1. Produce a handoff summary stating:
   - **Closed (mechanically + semantically):** P0-001, P0-002, P0-003, P1-001, P2-002.
   - **Still open:** P1-002 (memory update — Phase 8 in original plan), Phase 7 manual smoke (M-LANG-01..03), P2-001 (spec amendment, optional), P2-003 (`static class` shape, backlog), P2-004 (parameterless `BuildForRussian`, backlog).
   - **Recommended next stages:**
     1. `/audit` (re-run) to confirm readiness against the corrected artifact — optional but recommended given the original audit's severity.
     2. Phase 7 manual smoke against `llama3:latest` via Ollama (M-LANG-01..03) — operator-driven, requires Ollama session.
     3. `/update-memory` per original Phase 8 — closes Gate G.
     4. Operator decision on commits/merge boundary.
2. State the working-tree delta clearly: 3 production/test files modified beyond their post-implementation state, no new files, no deletions.
3. Confirm no architectural drift, no dependency-direction changes, no DI changes, no contract changes.

#### Expected Outcome

- Operator has a clear list of resolved findings, remaining gates, and recommended next stages.
- No files modified in this phase.

#### Verification

- The handoff statement matches the actual verification evidence from Phase 5.
- The list of resolved findings matches `docs/audits/...audit.md` §9.

#### Rollback

- N/A (no edits).

#### Acceptance Checkpoint

Handoff produced, no further phases needed within this plan.

## 6. Testing Plan

### Unit Tests

- **Strengthened:** T-LANG-04 (`BuildForRussian_ReturnsNonEmptyTextContainingCyrillic`) — adds `Assert.Contains("Айрис", text, StringComparison.Ordinal)` as a real-Russian canary; replaces or augments existing block-range check.
- **Repaired (substring updates):** T-LANG-05 (`BuildForRussian_ContainsTechnicalTokenRule`) and T-LANG-06 (`BuildForRussian_ContainsCodeStaysEnglishRule`) — substrings switch from mojibake to real Russian.
- **Improved (precedence):** T-LANG-06 — explicit parentheses around `&& / ||` expression.
- **Unchanged:** T-LANG-01..03 (`RussianDefaultLanguagePolicyTests`), T-LANG-07..11 (`PromptBuilderTests`, `DependencyInjectionTests`), `SendMessageHandlerTests`. They do not assert against Russian content semantics, so they are unaffected by the encoding fix.

### Integration Tests

- No integration test changes. `Iris.IntegrationTests` does not currently assert on Russian content; if it gains a slice that does, that is a separate spec.

### Architecture Tests

- No changes. `Iris.Architecture.Tests` (8/8) covers dependency direction and forbidden namespaces; this fix touches no project references or namespaces. Architecture tests serve as a sanity gate in Phase 5.

### Regression Tests

- The full solution test suite (155 tests, 5 test projects) runs in Phase 5 as the regression gate. Any test outside the language scope that fails indicates a regression and triggers per-phase rollback investigation.

### Manual Verification

- **Byte-level canary** (Phase 2 step 4, Phase 3 step 5, Phase 5 step 4-5) — replaces what would normally be manual smoke for encoding correctness. Mojibake bytes can be detected automatically without an Ollama session.
- **Phase 7 manual smoke (M-LANG-01..03)** — out of scope for this plan but recommended as the next operator action. Validates the runtime impact (Iris actually responds in Russian via `llama3:latest`).

## 7. Documentation and Memory Plan

### Documentation Updates

- **None required by this plan.**
- The audit report `docs/audits/2026-05-01-iris-default-language-russian.audit.md` may optionally be updated by a later `/audit` re-run; this plan does not modify it.
- The original specification, design, and plan documents stay unchanged. P2-001 (spec amendment to align AC-005 visibility with design §15 R2) is backlog and out of scope here.

### Agent Memory Updates

- **None within this plan.** Memory writes during `/implement` are restricted per `.opencode/rules/memory.md` to "after meaningful completed implementation". This remediation is the completion of Phase 6 of the original plan, but the original plan reserves memory updates for Phase 8 / `/update-memory`.
- After this plan's Phase 5 verifies green, the operator should run `/update-memory` to:
   - Append a new entry to `.agent/PROJECT_LOG.md` covering the audit-remediation work.
   - Update `.agent/overview.md` to reflect that the language work is now mechanically + semantically complete.
   - Append `.agent/log_notes.md` with a note that the encoding bug + mirror-mojibake-test trap is a generalizable lesson for future non-ASCII work.
   - Append §21 "Default Language" to `.agent/mem_library/03_iris_persona.md` per original plan §12 / Phase 8.
- This plan must NOT touch any `.agent/**` file directly.

## 8. Verification Commands

```powershell
# Phase 0 baseline
git status --short
dotnet build .\Iris.slnx --nologo --verbosity minimal

# Phase 1 red signal (expected to fail one test)
dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests"

# Phase 2 production fix + canary
$bytes = [System.IO.File]::ReadAllBytes("E:\Work\Iris\src\Iris.Application\Persona\Language\LanguageInstructionBuilder.cs")
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests.BuildForRussian_ReturnsNonEmptyTextContainingCyrillic"

# Phase 3 test substring fix
dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~LanguageInstructionBuilderTests"

# Phase 4 dead-branch removal
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~RussianDefaultLanguagePolicyTests"

# Phase 5 full verification triad
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~Iris.Architecture.Tests"

# Mojibake/real-Russian byte canaries
Select-String -Path "E:\Work\Iris\src\Iris.Application\Persona\Language\LanguageInstructionBuilder.cs" -Pattern "Айрис" -SimpleMatch
Select-String -Path "E:\Work\Iris\src\Iris.Application\Persona\Language\LanguageInstructionBuilder.cs" -Pattern "РўС‹" -SimpleMatch
Select-String -Path "E:\Work\Iris\tests\Iris.Application.Tests\Persona\Language\LanguageInstructionBuilderTests.cs" -Pattern "файл" -SimpleMatch
Select-String -Path "E:\Work\Iris\tests\Iris.Application.Tests\Persona\Language\LanguageInstructionBuilderTests.cs" -Pattern "Р°Р№Р»" -SimpleMatch
```

The `Айрис` / `файл` searches must return at least 1 match each post-fix. The `РўС‹` / `Р°Р№Р»` searches must return 0 matches post-fix.

## 9. Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| **Encoding pipeline at the agent's tool boundary is still broken** — the Edit tool re-introduces mojibake when given real Russian in `newString`. | Critical — fix silently re-creates the original P0-001 bug; verification stays mechanically green (mirror-mojibake again if test pasted in same session). | Medium — was the original failure mode; persistence depends on the runtime environment. | Phase 1 writes Russian to the test file FIRST as a probe. Phase 2 byte-level canary catches mojibake in the production file before claiming completion. Both phases have explicit STOP+escalate clauses if bytes don't match. Phase 5 final canary is a third checkpoint. |
| **Implementer modifies production text to match mojibake test substrings instead of the other way around** — quick fix that reverts P0-001. | Critical — silently undoes the entire fix. | Low — explicit Phase 3 step 6 stop condition forbids this. | Plan ordering forces production-first (Phase 2), test-substring-second (Phase 3). Phase 3 step 6 explicitly states "Do NOT modify production text to match the tests". |
| **Compiler IDE0052 / unused-field warning for `_options` in `RussianDefaultLanguagePolicy`** after Phase 4. | Medium — would block green build under `TreatWarningsAsErrors=true`. | Low — `_options` is read by `ArgumentNullException.ThrowIfNull` in the constructor; analyzers count this as a read. | Phase 4 step 3 watches for the warning and escalates if it appears (instead of silently removing `_options`, which is out of scope). |
| **`dotnet format --verify-no-changes` flags a difference** introduced by the new Russian text (e.g., trailing whitespace, line endings, indentation). | Medium — blocks Phase 5 readiness. | Low — `.editorconfig` is honored by the Edit tool; CRLF and 4-space indentation are standard in this file; rewriting the literal does not change indentation. | Phase 5 includes `dotnet format --verify-no-changes` as the gate; if it fails, identify the specific diff (line-ending, whitespace) and correct it manually with another Edit. Do NOT run mutating `dotnet format`. |
| **T-LANG-05/T-LANG-06 substring choice in Phase 3 doesn't match the rewritten production text in Phase 2** — substring not found, test fails. | Medium — Phase 3 fails. | Medium — depends on Phase 2 word choice. | Phase 3 step 6 stop condition: re-read production content, choose substrings that exist there. Reverse direction (modifying production to match) is forbidden. |
| **Phase 2 implementer rewrites Russian text with different semantics** (loses one of the five language rules). | High — silently violates FR-001..FR-005 even with correct encoding. | Low — Phase 2 step 1 lists the five rules verbatim. | Phase 2 step 1 explicit rule enumeration; cross-reference with spec FR-001..FR-005 in inspect step; manual smoke (Phase 7) catches semantic regressions operationally. |
| **Architecture tests fail unexpectedly** (e.g., due to namespace handling, cross-project import). | High — readiness blocked. | Very low — no namespaces or references touched in this plan. | Phase 5 step 6 runs architecture tests as a sanity gate. Failure triggers immediate rollback investigation. |
| **`dotnet test` reports >155 or <155 tests** — count drift. | Medium — indicates unintended test addition/removal/skipping. | Very low — only modifies existing tests, no add/delete. | Phase 5 expects exactly 155/155. Any drift triggers investigation. |
| **Working tree diverges between phases due to concurrent edits by user** (dirty-tree rule). | Medium — phase outcome unreliable. | Low — single-session execution. | Phase 0 captures git status as baseline; each subsequent phase implicitly assumes single-agent control. If concurrent edits detected, escalate. |
| **Phase 1 strengthened test passes against mojibake production** — would mean the encoding pipeline accidentally produced `Айрис` somewhere, OR the test assertion is wrong. | High — invalidates the canary. | Very low — byte-level inspection in Debug Report confirmed `Айрис` is absent from current production literal. | Phase 1 step 5 stop condition: read production bytes; if `Айрис` IS present in mojibake form, the encoding bug was mis-diagnosed and this whole plan needs revision. |

## 10. Implementation Handoff Notes

### Critical Constraints

- **Byte-level canary is non-negotiable.** Every phase that writes Russian text (1, 2, 3) requires a byte-level read-back via `[System.IO.File]::ReadAllBytes` and confirmation of correct UTF-8 codepoints. Trusting visual rendering of the Read/Edit tool is unsafe — the original P0-001 bug demonstrated that the visible text in the agent's display is not the same as the bytes on disk in this environment.
- **Strengthened-test-first ordering** (Phase 1 before Phase 2) is mandatory. If Phase 2 lands first, the risk of silent re-mojibaking is unmitigated.
- **Production-text-first ordering** within encoding fix (Phase 2 before Phase 3): test substrings must adapt to production text, never the reverse. Phase 3 step 6 forbids the reverse.
- **No mutating `dotnet format`.** Only `--verify-no-changes` is allowed throughout. If formatting drift appears, fix it via Edit tool with explicit edits, not via the formatter.
- **Memory updates strictly forbidden in this plan.** All `.agent/**` files are out of scope. Memory updates flow through `/update-memory` after Phase 5 green.
- **No commits in this plan.** Commit decisions belong to the operator after the working tree is in a defensible state.

### Risky Areas

- The Russian literal in `LanguageInstructionBuilder.cs` is the highest-risk single edit in the plan. The byte-level canary is the only defense.
- T-LANG-06 boolean precedence (P2-002) is a low-risk sweep but must not change semantic intent.
- `_options` field retention in `RussianDefaultLanguagePolicy` is forward-looking; resist analyzer pressure to remove it.

### Expected Final State

- Working tree: 14 modified tracked → still 14 modified tracked (3 of them now have new content beyond original implementation: `LanguageInstructionBuilder.cs`, `RussianDefaultLanguagePolicy.cs`, `LanguageInstructionBuilderTests.cs`); `.agent/**` unchanged; `AGENTS.md` and Phase 6 dirty files untouched.
- `Iris.Application.Persona.Language` namespace and types: same shape, same DI lifetimes, same contracts. Only literal content + dead-branch removal changed.
- Tests: 155/155 pass. T-LANG-04 strengthened to assert real Russian. T-LANG-05/06 use real-Russian substrings. T-LANG-06 has explicit parentheses.
- Build: 0 errors, 0 warnings under `TreatWarningsAsErrors=true`.
- Format: `--verify-no-changes` exit 0.
- Byte canaries: green for both `LanguageInstructionBuilder.cs` and `LanguageInstructionBuilderTests.cs`.
- Audit findings P0-001, P0-002, P0-003, P1-001 closed; P2-002 closed.
- Audit findings P1-002 (memory), Phase 7 manual smoke, P2-001 (spec amendment), P2-003 (`static class`), P2-004 (parameterless builder) — still open, deferred to subsequent operator-driven actions.

### Checks That Must Not Be Skipped

1. Byte-level read of `LanguageInstructionBuilder.cs` after Phase 2.
2. Strengthened-test red signal in Phase 1 (must fail before fix).
3. Strengthened-test green signal in Phase 2 (must pass after fix).
4. Full-text mojibake-absence search in Phase 5 (`Select-String "РўС‹"` returns 0).
5. Full-test-suite run in Phase 5 (155/155 expected).
6. `dotnet format --verify-no-changes` in Phase 5.
7. Architecture test sanity in Phase 5.

## 11. Open Questions

No blocking open questions.

The plan is fully implementable from the existing spec, design, audit, and Debug Report. The implementer's only choices are:

- exact phrasing of the rewritten Russian baseline (constrained by FR-001..FR-005);
- which concrete real-Russian substrings to use in T-LANG-05/06 (constrained by what appears in the rewritten baseline);
- whether to keep or remove the `ContainsCyrillic` private helper in T-LANG-04 (defense in depth — recommend keep);
- whether to use `StringComparison.Ordinal` or `OrdinalIgnoreCase` in T-LANG-04/05/06 (recommend `Ordinal` to enforce specific casing).

None of these choices are blocking; all default-recommended values are stated in the corresponding phase steps.
