# Implementation Plan: Iris Default Response Language is Russian

## 1. Plan Goal

Implement the approved design `docs/designs/2026-05-01-iris-default-language-russian.design.md` so that Iris responds in Russian by default while preserving English for technical tokens, code, and engineering artifacts. The plan delivers four new types in `Iris.Application.Persona.Language`, modifies `PromptBuilder` and `AddIrisApplication` to consume them, updates the single Desktop host call site, updates existing tests that compile-broke from the constructor change, adds new unit tests T-LANG-01..T-LANG-11, and records the persona/language intent in `mem_library/03_iris_persona.md` §21 via `/update-memory`. Verification is `dotnet build .\Iris.slnx`, `dotnet test .\Iris.slnx`, `dotnet format .\Iris.slnx --verify-no-changes`, plus operator-driven manual smoke M-LANG-01..M-LANG-03.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-01-iris-default-language-russian.spec.md`
- **Design:** `docs/designs/2026-05-01-iris-default-language-russian.design.md`
- **Relevant rules:** `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/dotnet.md`, `.opencode/rules/verification.md`, `.opencode/rules/memory.md`, `.opencode/rules/workflow.md`
- **Persona memory:** `.agent/mem_library/03_iris_persona.md` (§17, §18 inform the Russian baseline meaning; §21 will be appended)
- **Project memory:** `.agent/PROJECT_LOG.md`, `.agent/overview.md` (read-only during implement; updated via `/update-memory` after)

### Assumptions

1. The existing dirty tree (`AGENTS.md`, `.opencode/skills/iris-engineering/SKILL.md`, `.opencode/commands/implement.md`, Desktop UI/DI files modified by Phase 6 work) is **not** in this plan's scope. The implementation agent must not stage, revert, or normalize these unrelated files. Scope is `Iris.Application/*`, `tests/Iris.Application.Tests/*`, and a single line in `Iris.Desktop/DependencyInjection.cs:54`.
2. The `Iris.Application` test project is `Iris.Application.Tests` (confirmed in design §3 reconnaissance), not `Iris.Application.UnitTests` as spec §5 mistakenly states.
3. `PromptBuilder`, `LanguageOptions`, `ILanguagePolicy`, `RussianDefaultLanguagePolicy`, and `LanguageInstructionBuilder` are `public sealed` (per design §15 R2 — the project has no `InternalsVisibleTo` for `Iris.Application.Tests` and uniform `public sealed` is the established convention).
4. `dotnet build .\Iris.slnx` is the supported build command (confirmed by `.opencode/rules/verification.md`).
5. The Russian baseline text is chosen during Phase 2 by the implementation agent, subject to design §6 content rules. Operator review is in manual smoke (Phase 7).
6. `Persona:Language` configuration section may be absent from `appsettings.json` — defaults are baked into `LanguageOptions.Default`. The Desktop host falls back when the value is null/empty/whitespace.

## 3. Scope Control

### In Scope

- Create `src/Iris.Application/Persona/Language/LanguageOptions.cs`.
- Create `src/Iris.Application/Persona/Language/ILanguagePolicy.cs`.
- Create `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs`.
- Create `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs`.
- Modify `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` (remove `_baselineSystemPrompt`, add ctor parameter, delegate to `ILanguagePolicy`).
- Modify `src/Iris.Application/DependencyInjection.cs` (new `LanguageOptions` parameter, three new singleton registrations).
- Modify `src/Iris.Desktop/DependencyInjection.cs` line 54 (read `Persona:Language:DefaultLanguage`, construct `LanguageOptions`, pass to `AddIrisApplication`).
- Modify `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (update existing test for new ctor; add T-LANG-07/08).
- Modify `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` (update `CreateHandler` helper only; no assertion changes).
- Modify `tests/Iris.Application.Tests/DependencyInjectionTests.cs` (existing tests gain `LanguageOptions.Default` argument; add T-LANG-10/11).
- Create `tests/Iris.Application.Tests/Persona/Language/RussianDefaultLanguagePolicyTests.cs` (T-LANG-01..03).
- Create `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` (T-LANG-04..06).
- After implementation: `/update-memory` appends §21 "Default Language" to `.agent/mem_library/03_iris_persona.md`, prepends a `PROJECT_LOG.md` entry, updates `.agent/overview.md` "Next step", appends a P2 backlog entry to `debt_tech_backlog.md`.
- Manual smoke M-LANG-01..M-LANG-03 against `llama3:latest` via Ollama.

### Out of Scope

- Localization of `Iris.Desktop` UI strings (button labels, statuses, placeholders).
- Localization of logs, telemetry, error codes, identifiers, `PROJECT_LOG.md`, spec/design/plan documents, commit messages.
- Wiring `PersonaContextBuilder`, `SpeechStylePolicy`, `MoodSelectionService`, or `RelationshipPolicy` into `PromptBuilder`.
- Cleaning up `src/Iris.Application/Chat/Prompting/PromptTemplateProvider.cs` (dead stub; design §3, §15 R4 — separate ticket).
- Adding `IOptions<>` / `Configure<>` / `Bind(` patterns to `Iris.Application` (design Option C — rejected).
- Few-shot Russian example in system prompt (design Option E — reserved fallback only if manual smoke fails).
- Adding `Persona:Language` keys to `src/Iris.Desktop/appsettings.json` (defaults sufficient).
- `src/Iris.Api`, `src/Iris.Worker` host changes (no host exists in production yet).
- Migrations, EF schema, persisted data.

### Forbidden Changes

- Modifying `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Perception`, `Iris.Tools`, `Iris.Voice`, `Iris.Infrastructure`, `Iris.SiRuntimeGateway`.
- Modifying `Iris.Api`, `Iris.Worker`, or anything in `Iris.Desktop` other than the single call site at `DependencyInjection.cs:54`.
- Adding new NuGet packages, new `ProjectReference` entries, or `InternalsVisibleTo` directives.
- Making types `internal sealed` (spec AC-005 superseded by design §15 R2 — project convention is `public sealed`).
- Modifying `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `.slnx`, `.csproj` files (other than what `dotnet build` regenerates incidentally — there should be nothing).
- Editing dirty-tree files outside scope: `AGENTS.md`, `.opencode/skills/iris-engineering/SKILL.md`, `.opencode/commands/implement.md`, `src/Iris.Desktop/Iris.Desktop.csproj`, `src/Iris.Desktop/Views/*`, `src/Iris.Desktop/appsettings.json` (Phase 6 in-flight changes).
- Editing `src/Iris.Application/Chat/Prompting/PromptTemplateProvider.cs` (dead stub, separate ticket).
- Mutating snapshots, golden outputs, or generated files.
- Running `git push`, `git clean`, `git reset --hard`, `Remove-Item -Recurse`, or any destructive command.
- Updating `.agent/` memory **during** Phase 0–7. Memory writes happen **only** in Phase 8 via `/update-memory`.

## 4. Implementation Strategy

The work is fundamentally a single architectural seam (a `private const` becomes a DI-injected port) plus its callers. The strategy minimizes the time the build is broken and lets us verify each step in isolation.

**Order of work and why it is safe:**

1. **Phase 0 — Reconnaissance.** Confirm assumptions against the actual repo state. Catches drift between design context and current files. No edits. (Cheap insurance.)
2. **Phase 1 — Create new types in isolation.** All four new types in `Iris.Application/Persona/Language/` are unreferenced by anything until Phase 3. The build stays green throughout. New types ship with their own tests in Phase 2 — TDD-friendly order.
3. **Phase 2 — Add unit tests for the new types.** Tests T-LANG-01..06 exercise the new types directly. Build + run only the new test class to validate before touching `PromptBuilder`.
4. **Phase 3 — Modify `PromptBuilder` and update its existing tests in one atomic edit.** This is the only point where the existing build breaks if not done together. `PromptBuilder` ctor change + `PromptBuilderTests` ctor update + `SendMessageHandlerTests.CreateHandler` ctor update happen in one commit logically. T-LANG-07/08 are added in the same edit.
5. **Phase 4 — Modify `AddIrisApplication` and update `DependencyInjectionTests`.** Atomic for the same reason. Adds T-LANG-10/11.
6. **Phase 5 — Modify Desktop host call site.** One line replacement plus host-side config read. After this phase the `.slnx` builds end-to-end.
7. **Phase 6 — Full verification.** Build + test + format on the whole solution. Architecture tests must remain green automatically.
8. **Phase 7 — Manual smoke M-LANG-01..M-LANG-03.** Operator-driven. Validates real model behavior against the Russian baseline.
9. **Phase 8 — Memory update via `/update-memory`.** Records the durable persona intent and logs the work.

**Why this order:** Phases 1–2 add code that can be deleted with no rollback complexity. Phases 3–5 each make the build temporarily inconsistent; running them as discrete phases means each phase's verification narrows the failure surface. Phase 6 is the gate before the irreversible commitment of manual smoke and memory writes. Phase 7 cannot be automated. Phase 8 is the trailing memory obligation per `iris-engineering` Gate G.

**Branching/git:** No new commits are produced by `/implement` per `iris-engineering` rule. The implementation agent leaves the working tree dirty for operator review.

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Confirm the design's current-state assumptions against the actual repository. Detect any drift since `/design` was authored. No edits.

#### Files to Inspect

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` — confirm `private const string _baselineSystemPrompt` and parameterless ctor still present.
- `src/Iris.Application/DependencyInjection.cs` — confirm `AddIrisApplication(this IServiceCollection, SendMessageOptions)` signature.
- `src/Iris.Application/Chat/SendMessage/SendMessageOptions.cs` — confirm `public sealed record` idiom.
- `src/Iris.Application/Persona/` — confirm no existing `Language/` subfolder.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` — confirm `new PromptBuilder()` at line ~13.
- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` — confirm `new PromptBuilder()` at line ~320 inside `CreateHandler`.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` — confirm `services.AddIrisApplication(new SendMessageOptions(...))` call sites.
- `src/Iris.Desktop/DependencyInjection.cs` — confirm line 54 `services.AddIrisApplication(new SendMessageOptions(maxMessageLength));`.
- `src/Iris.Application/Iris.Application.csproj` — confirm no `InternalsVisibleTo`.
- `tests/Iris.Architecture.Tests/` — confirm test count baseline.
- `git status --short` — confirm dirty-tree files match the assumed list and nothing within `src/Iris.Application/` or `tests/Iris.Application.Tests/` is dirty.

#### Files Likely to Edit

- None.

#### Steps

1. Read each file/folder above.
2. Run `git status --short` to compare with the assumption in §2.1.
3. If any assumption fails, **stop and report the drift**. Do not proceed to Phase 1.
4. If all assumptions hold, log baseline test count: `dotnet test .\Iris.slnx --no-build --list-tests` is too slow; instead just record the existing test counts from `.agent/PROJECT_LOG.md` (currently 141 total).

#### Verification

- All inspected facts match design §3.
- `git status --short` shows the expected dirty files only.
- No file under `src/Iris.Application/Persona/Language/` exists.

#### Rollback

No code changes. Nothing to rollback.

#### Acceptance Checkpoint

Reconnaissance produces a one-paragraph "all assumptions hold" or "drift detected: ..." statement. Implementation does not proceed on drift.

---

### Phase 1 — Create New Application Types

#### Goal

Add the four new `public sealed` types in `Iris.Application.Persona.Language` namespace. They are unreferenced; build stays green.

#### Files to Inspect

- `src/Iris.Application/Persona/State/PersonaStateService.cs` (style reference for new file headers, namespace declaration style, brace style).
- `src/Iris.Application/Chat/SendMessage/SendMessageOptions.cs` (record-with-static-default pattern reference for `LanguageOptions`).
- `.editorconfig` (CRLF, IDE1006 underscore-prefix for private fields/consts, `var` when type is obvious).

#### Files Likely to Edit

- `src/Iris.Application/Persona/Language/LanguageOptions.cs` — **create**. `public sealed record LanguageOptions(string DefaultLanguage)` + `public static LanguageOptions Default { get; } = new("ru");`.
- `src/Iris.Application/Persona/Language/ILanguagePolicy.cs` — **create**. `public interface ILanguagePolicy { string GetSystemPrompt(); }`.
- `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` — **create**. `public sealed class` with single `private const string _russianBaseline = "..."` and one method `public string Build(string normalizedLanguageCode)` that returns the Russian baseline (current spec: any code falls back to Russian; the parameter exists for future extension and is currently ignored or only used to assert a non-null contract). The Russian text must satisfy design §6 content rules 1–5.
- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` — **create**. `public sealed class` implementing `ILanguagePolicy`. Ctor takes `LanguageOptions options, LanguageInstructionBuilder builder` with `ArgumentNullException.ThrowIfNull` per project convention. `GetSystemPrompt()` normalizes `options.DefaultLanguage` (trim + invariant lower; treat null/empty/whitespace as `"ru"`), calls `_builder.Build(normalized)`.

#### Files That Must Not Be Touched

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` (Phase 3 owns it).
- `src/Iris.Application/DependencyInjection.cs` (Phase 4 owns it).
- Any test file (Phase 2 owns new tests; Phase 3/4 own existing-test updates).
- `src/Iris.Application/Chat/Prompting/PromptTemplateProvider.cs` (out of scope).
- All adapter projects, all hosts, all domain files.
- All dirty-tree files outside `src/Iris.Application/Persona/Language/`.

#### Steps

1. Create `src/Iris.Application/Persona/Language/` directory.
2. Create the four files in this order: `LanguageOptions.cs`, `ILanguagePolicy.cs`, `LanguageInstructionBuilder.cs`, `RussianDefaultLanguagePolicy.cs` (dependencies forward only).
3. Each file's namespace is `Iris.Application.Persona.Language`.
4. The Russian baseline text in `LanguageInstructionBuilder._russianBaseline` must satisfy design §6 content rules 1–5. Recommended structural shape (wording is the implementer's choice):
   - Identity sentence (calm, local, personal companion).
   - Language directive ("Отвечай на русском.").
   - Technical-token rule (file names, paths, identifiers, commands stay in English).
   - Code rule (code and in-code comments stay in English; prose in Russian).
   - Hard-default rule ("Сохраняй русский язык, даже если пользователь пишет на другом.").
5. No XML doc comments are required (existing Application types do not have them); add them only if the implementation agent considers a method's contract non-obvious.

#### Verification

- `dotnet build .\src\Iris.Application\Iris.Application.csproj` returns 0 errors, 0 warnings.
- `git status --short src/Iris.Application/Persona/Language/` shows four new untracked files.
- `git status --short` outside that folder shows no new modifications introduced by this phase.

#### Rollback

- `Remove-Item -Recurse src/Iris.Application/Persona/Language/` (the only command of this type in the plan; only safe because the folder did not exist before Phase 1).
- No other rollback needed.

#### Acceptance Checkpoint

Four new files exist; project builds; no other file changed.

---

### Phase 2 — Unit Tests for New Types

#### Goal

Add T-LANG-01..T-LANG-06 covering `RussianDefaultLanguagePolicy` and `LanguageInstructionBuilder`. Tests run green standalone, proving the new types behave per design before any consumer is wired.

#### Files to Inspect

- `tests/Iris.Application.Tests/Iris.Application.Tests.csproj` (to confirm test project name and target framework).
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (xUnit style reference: `public sealed class`, `[Fact]`, `Assert.*`, no FluentAssertions).
- `.editorconfig` (test code follows the same style rules).

#### Files Likely to Edit

- `tests/Iris.Application.Tests/Persona/Language/RussianDefaultLanguagePolicyTests.cs` — **create**. Tests T-LANG-01, T-LANG-02 (`[Theory]` with null/empty/whitespace), T-LANG-03 (`[Theory]` with `"jp"`/`"en"`/`"xyz"`).
- `tests/Iris.Application.Tests/Persona/Language/LanguageInstructionBuilderTests.cs` — **create**. Tests T-LANG-04 (Cyrillic presence via `Regex` or `char.IsLetter` + Unicode block check), T-LANG-05 (technical-token keyword presence — design recommends checking 1–2 keywords like `файл`, `код`, `команд`), T-LANG-06 (code-stays-English keyword presence).

#### Files That Must Not Be Touched

- Any production source (Phase 1 already created what's needed).
- `tests/Iris.Application.Tests/Chat/**` (Phase 3 owns).
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` (Phase 4 owns).
- Any other test project.

#### Steps

1. Create `tests/Iris.Application.Tests/Persona/Language/` directory.
2. Implement `RussianDefaultLanguagePolicyTests` with one `[Fact]` (T-LANG-01) and two `[Theory]` blocks (T-LANG-02 with null/empty/whitespace `InlineData`, T-LANG-03 with non-Russian language codes). All three groups assert that `GetSystemPrompt()` returns the same Russian baseline string (use `Equal` against the value returned for `"ru"` to avoid hardcoding the Russian text in tests — robust to wording changes).
3. Implement `LanguageInstructionBuilderTests` with three `[Fact]` tests (T-LANG-04..06). Use keyword presence checks rather than verbatim string equality (design §13 strategy note).
4. Test class names match design §13 exactly.

#### Verification

- `dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj` returns 0 errors, 0 warnings.
- `dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --filter "FullyQualifiedName~Persona.Language"` shows 6+ tests passing (3 from `RussianDefaultLanguagePolicyTests` may be `[Theory]` that expand to more rows; count by xUnit).
- All other tests in `Iris.Application.Tests` still pass (they should — no production code changed since Phase 1).

#### Rollback

- `Remove-Item -Recurse tests/Iris.Application.Tests/Persona/Language/` (only safe because the folder did not exist before Phase 2).

#### Acceptance Checkpoint

T-LANG-01..06 green; all preexisting `Iris.Application.Tests` still green.

---

### Phase 3 — Modify `PromptBuilder` and Update Affected Tests

#### Goal

Inject `ILanguagePolicy` into `PromptBuilder` and remove the English `_baselineSystemPrompt` constant. Update the two existing test files that constructed `PromptBuilder` directly. Add T-LANG-07/08.

#### Files to Inspect

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` (current state).
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (existing tests).
- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` (specifically `CreateHandler` helper around line 308–324).

#### Files Likely to Edit

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs`:
  - Remove `private const string _baselineSystemPrompt = "..."`.
  - Add private readonly field `private readonly ILanguagePolicy _languagePolicy`.
  - Add ctor `public PromptBuilder(ILanguagePolicy languagePolicy)` with `ArgumentNullException.ThrowIfNull`.
  - In `Build(...)`, replace the inline string with `_languagePolicy.GetSystemPrompt()`.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs`:
  - Update existing `Build_IncludesSystemMessageHistoryAndCurrentUserMessage` to construct `PromptBuilder` with a stub `ILanguagePolicy` (define `private sealed class StubLanguagePolicy : ILanguagePolicy { public string GetSystemPrompt() => "TEST_SYSTEM_PROMPT"; }` inside the test file).
  - Update the system-message assertion: instead of `Assert.NotEqual(string.Empty, message.Content)`, use `Assert.Equal("TEST_SYSTEM_PROMPT", message.Content)` (this is T-LANG-09 per design §13).
  - Add T-LANG-07 (`Build_UsesInjectedLanguagePolicy_ForSystemMessage`) — same stub, asserts the first message's `Content` equals stub's output.
  - Add T-LANG-08 (`Build_WithDefaultRussianPolicy_DoesNotEmitLegacyEnglishBaseline`) — uses real `RussianDefaultLanguagePolicy(LanguageOptions.Default, new LanguageInstructionBuilder())` and asserts the first message `Content` is `NotEqual` to the literal `"You are Iris, a local personal AI companion. Be helpful, clear, and respectful."`.
- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs`:
  - In `CreateHandler` (around line 308), change `new PromptBuilder()` to `new PromptBuilder(new RussianDefaultLanguagePolicy(LanguageOptions.Default, new LanguageInstructionBuilder()))`. **No assertion changes**. All 14 tests must continue to pass without other edits.

#### Files That Must Not Be Touched

- `src/Iris.Application/DependencyInjection.cs` (Phase 4).
- `src/Iris.Desktop/DependencyInjection.cs` (Phase 5).
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` (Phase 4).
- All adapter projects and hosts.
- `src/Iris.Application/Chat/Prompting/PromptTemplateProvider.cs`.
- All preexisting dirty-tree files outside this phase's scope.

#### Steps

1. Edit `PromptBuilder.cs` (one file, atomic).
2. Edit `PromptBuilderTests.cs`: update existing test, add stub class, add T-LANG-07, add T-LANG-08.
3. Edit `SendMessageHandlerTests.cs`: change one line in `CreateHandler`.
4. Build only the Application project + Application.Tests project.

#### Verification

- `dotnet build .\src\Iris.Application\Iris.Application.csproj` returns 0 errors, 0 warnings.
- `dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj` returns 0 errors, 0 warnings.
- `dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --filter "FullyQualifiedName~PromptBuilderTests|FullyQualifiedName~SendMessageHandlerTests|FullyQualifiedName~Persona.Language"` — all targeted tests green.
- `git diff src/Iris.Application/Chat/Prompting/PromptBuilder.cs` shows only the constant deletion and ctor/field/method-body changes — no unrelated drift.

#### Rollback

- `git checkout -- src/Iris.Application/Chat/Prompting/PromptBuilder.cs tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs`. Phase 1/2 artifacts remain (they are independent).

#### Acceptance Checkpoint

`PromptBuilder` no longer contains `_baselineSystemPrompt`. T-LANG-07/08/09 green. All 14 `SendMessageHandlerTests` green. Existing `PromptBuilderTests` test green with new assertion.

---

### Phase 4 — Modify `AddIrisApplication` and Update DI Tests

#### Goal

Extend `AddIrisApplication` signature with a `LanguageOptions` parameter, register the three new singletons, and update the three `DependencyInjectionTests`. Add T-LANG-10/11.

#### Files to Inspect

- `src/Iris.Application/DependencyInjection.cs` (current 32 lines).
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs`.

#### Files Likely to Edit

- `src/Iris.Application/DependencyInjection.cs`:
  - Add `LanguageOptions languageOptions` as third positional parameter.
  - Add `ArgumentNullException.ThrowIfNull(languageOptions);` after the existing null-checks.
  - After `services.AddSingleton(sendMessageOptions);` add:
    - `services.AddSingleton(languageOptions);`
    - `services.AddSingleton<LanguageInstructionBuilder>();`
    - `services.AddSingleton<ILanguagePolicy, RussianDefaultLanguagePolicy>();`
  - Existing `services.AddSingleton<PromptBuilder>();` stays (DI resolves `ILanguagePolicy` via constructor).
  - Add `using Iris.Application.Persona.Language;`.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs`:
  - Update `AddIrisApplication_RegistersSendMessageHandler`: pass `LanguageOptions.Default` as third argument.
  - Update `AddIrisApplication_WithNullOptions_Throws` (currently passes `null!` for `SendMessageOptions`): keep test as-is but pass `LanguageOptions.Default` for the third argument; this test continues to assert that null `SendMessageOptions` throws.
  - Update `AddIrisApplication_WithInvalidMaxMessageLength_Throws`: pass `LanguageOptions.Default`.
  - Add T-LANG-10 `AddIrisApplication_WithNullLanguageOptions_Throws`: passes valid `SendMessageOptions(8000)` and `null!` for `LanguageOptions`, asserts `ArgumentNullException`.
  - Add T-LANG-11 `AddIrisApplication_RegistersILanguagePolicy`: builds `ServiceProvider`, resolves `ILanguagePolicy`, asserts `NotNull` and is `RussianDefaultLanguagePolicy`.

#### Files That Must Not Be Touched

- `src/Iris.Desktop/DependencyInjection.cs` (Phase 5 — modifying it now would temporarily break Desktop build, but this phase keeps it broken intentionally; Phase 5 is the very next phase to close the gap).
- All other production and test code.
- `Iris.Desktop` test projects (none reference `AddIrisApplication`).

#### Steps

1. Edit `Iris.Application/DependencyInjection.cs`.
2. Edit `tests/Iris.Application.Tests/DependencyInjectionTests.cs`: update three existing tests, add T-LANG-10/11.
3. Build the Application project + Application.Tests project (do not yet build full `.slnx` — Desktop is intentionally broken until Phase 5).

#### Verification

- `dotnet build .\src\Iris.Application\Iris.Application.csproj` returns 0 errors, 0 warnings.
- `dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj` returns 0 errors, 0 warnings.
- `dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build` — all `Iris.Application.Tests` green (count rises by 2 from this phase: T-LANG-10/11).
- **Expected partial failure**: building `src/Iris.Desktop/Iris.Desktop.csproj` here will fail because `Iris.Desktop/DependencyInjection.cs:54` still calls the old two-argument signature. This is expected and resolved in Phase 5.

#### Rollback

- `git checkout -- src/Iris.Application/DependencyInjection.cs tests/Iris.Application.Tests/DependencyInjectionTests.cs`. Phase 1/2/3 artifacts remain.

#### Acceptance Checkpoint

`AddIrisApplication` has the new signature; new singletons registered; T-LANG-10/11 plus the three updated existing tests are green. Desktop build is intentionally broken until Phase 5.

---

### Phase 5 — Modify Desktop Host Call Site

#### Goal

Update `Iris.Desktop/DependencyInjection.cs` to read `Persona:Language:DefaultLanguage` and pass `LanguageOptions` to `AddIrisApplication`. Restores full `.slnx` build.

#### Files to Inspect

- `src/Iris.Desktop/DependencyInjection.cs` lines 35–60 (existing config-read patterns for `Database:ConnectionString`, `ModelGateway:Ollama:*`).
- `src/Iris.Desktop/Iris.Desktop.csproj` (confirm it already references `Iris.Application` — it does).

#### Files Likely to Edit

- `src/Iris.Desktop/DependencyInjection.cs`:
  - Add `using Iris.Application.Persona.Language;` near other `using Iris.Application.*` directives.
  - Before line 54 (the existing `services.AddIrisApplication(...)`), add:
    ```csharp
    var configuredLanguage = configuration.GetValue<string?>("Persona:Language:DefaultLanguage");
    var languageOptions = string.IsNullOrWhiteSpace(configuredLanguage)
        ? LanguageOptions.Default
        : new LanguageOptions(configuredLanguage);
    ```
    (using project's `var`-when-obvious convention from `.editorconfig`).
  - Replace line 54 with `services.AddIrisApplication(new SendMessageOptions(maxMessageLength), languageOptions);`.

#### Files That Must Not Be Touched

- `src/Iris.Desktop/Iris.Desktop.csproj` (already dirty from Phase 6 work; do not re-touch).
- `src/Iris.Desktop/appsettings.json` (already dirty from Phase 6 work; do not re-touch — `Persona:Language` section is optional and absent is fine).
- `src/Iris.Desktop/appsettings.local.json` (gitignored, operator-controlled).
- `src/Iris.Desktop/appsettings.local.example.json` (untracked from Phase 6; do not re-touch — design §11 left this as plan's discretion; **decision**: do not modify, keep blast radius minimal).
- `src/Iris.Desktop/Views/*` (Phase 6 in-flight, out of this plan's scope).
- `src/Iris.Desktop/Hosting/*` (Phase 6 in-flight, out of this plan's scope).
- All `Iris.Desktop` test projects.

#### Steps

1. Edit `src/Iris.Desktop/DependencyInjection.cs`: add `using`, add three lines reading config, replace one line at the call site.
2. Verify the diff is minimal (4–5 added lines, 1 modified line).

#### Verification

- `dotnet build .\Iris.slnx` returns 0 errors, 0 warnings (full solution build now succeeds).
- `git diff src/Iris.Desktop/DependencyInjection.cs` shows only the language-related additions, no other changes.

#### Rollback

- `git checkout -- src/Iris.Desktop/DependencyInjection.cs`. Phase 1–4 remain. Application build succeeds; Desktop build breaks because of the Phase 4 signature change. Either also rollback Phase 4 or proceed forward.

#### Acceptance Checkpoint

Full `.slnx` builds green. Desktop reads `Persona:Language:DefaultLanguage` (or falls back to `LanguageOptions.Default`) and passes it through.

---

### Phase 6 — Full Verification Pass

#### Goal

Confirm the full repository state — build, tests, format — is green and the test count rose by exactly the planned amount.

#### Files to Inspect

- None (read-only verification).

#### Files Likely to Edit

- None.

#### Files That Must Not Be Touched

- All files (read-only phase).

#### Steps

1. Run `dotnet build .\Iris.slnx` — expect 0 errors, 0 warnings.
2. Run `dotnet test .\Iris.slnx --no-build` — expect all green. Test count = previous baseline (141 per `.agent/PROJECT_LOG.md`) + 8 new (T-LANG-01, T-LANG-02 expanded by `[Theory]` to ~3, T-LANG-03 expanded to ~3, T-LANG-04, T-LANG-05, T-LANG-06, T-LANG-07, T-LANG-08, T-LANG-10, T-LANG-11). Exact count depends on `[Theory]` row counts; allow range 149–155, expected ~150–152.
3. Run `dotnet format .\Iris.slnx --verify-no-changes` — expect EXIT 0.
4. Run `git status --short` — expect:
   - Within scope: 4 new files in `src/Iris.Application/Persona/Language/`, 2 new test files in `tests/Iris.Application.Tests/Persona/Language/`, modifications to `src/Iris.Application/Chat/Prompting/PromptBuilder.cs`, `src/Iris.Application/DependencyInjection.cs`, `src/Iris.Desktop/DependencyInjection.cs`, `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs`, `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs`, `tests/Iris.Application.Tests/DependencyInjectionTests.cs`.
   - Outside scope: pre-existing dirty files unchanged (`AGENTS.md`, `.opencode/skills/iris-engineering/SKILL.md`, etc.).

#### Verification

- All three commands exit 0.
- `git status --short` matches the expected list.
- No new unintended modifications.

#### Rollback

- Per-file rollback per the relevant earlier phase. Phase 6 itself does not write.

#### Acceptance Checkpoint

Build green, tests green, format green, scope honored.

---

### Phase 7 — Manual Smoke (Operator)

#### Goal

Verify real model behavior against the Russian baseline. Operator-driven; cannot be automated.

#### Files to Inspect

- `src/Iris.Desktop/appsettings.local.json` — confirm `ModelGateway:Ollama:ChatModel = "llama3:latest"` (or the operator's preferred model). Optionally add `Persona:Language: { DefaultLanguage: "ru" }` to confirm explicit-config path; not required.

#### Files Likely to Edit

- None by the implementation agent. The operator may tweak `appsettings.local.json` (gitignored).

#### Files That Must Not Be Touched

- All tracked files.
- `src/Iris.Desktop/appsettings.json` (must not gain a `Persona:Language` block in this plan).

#### Steps

1. Confirm Ollama is running (`curl http://127.0.0.1:11434/api/tags` returns 200).
2. Launch `Iris.Desktop` (`dotnet run --project src\Iris.Desktop\Iris.Desktop.csproj` or the project shortcut).
3. **M-LANG-01:** Send "Привет. Расскажи о себе кратко." → expect a Russian response.
4. **M-LANG-02:** Send "Hello. What is the current time?" → expect a Russian response (per FR-005, no language mirroring).
5. **M-LANG-03:** Send "Покажи hello world на C#." → expect Russian prose around an English code block; comments inside the code block in English; type/method names in English.
6. Record outcomes (Pass/Anomaly + brief note) in `docs/manual-smoke/2026-05-01-iris-default-language-russian.smoke.md` (operator can extend the existing manual-smoke folder or create a new file — this is operator's discretion; the implementation agent does not pre-create this file).

#### Verification

- All three scenarios produce Russian responses with the expected technical-token preservation. Anomaly = model drifted to English or violated rules → see Risk R1 fallback (Option E few-shot, separate ticket).

#### Rollback

- No code state to rollback. If model behavior is insufficient, escalate to a separate ticket implementing design Option E or swapping the model. Do not modify the chosen Russian baseline text without a new spec amendment cycle.

#### Acceptance Checkpoint

M-LANG-01..03 all Pass. If any fails, escalate without modifying scope.

---

### Phase 8 — Memory Update via `/update-memory`

#### Goal

Record the durable persona-language intent in `mem_library/03_iris_persona.md` §21, log the work in `PROJECT_LOG.md`, refresh `overview.md`, and add a P2 backlog entry for the deferred persona wiring.

This phase is executed via the `/update-memory` workflow, not by `/implement`. The implementation agent must not write memory files during Phases 0–7.

#### Files to Inspect

- `.agent/mem_library/03_iris_persona.md` (current §17 baseline reference, §18 first-slice baseline; §21 will be appended).
- `.agent/PROJECT_LOG.md` (existing entry style).
- `.agent/overview.md` (current Phase 6 complete state).
- `.agent/debt_tech_backlog.md` (entry style for new P2 item).

#### Files Likely to Edit

- `.agent/mem_library/03_iris_persona.md` — append `## 21. Default Language` section recording: Russian as default; English-preserved technical tokens; code/comments in English with Russian prose; no language mirroring; `Iris.Application/Persona/Language` is the implementation home; supersedes §18's English baseline example for actual prompt content.
- `.agent/PROJECT_LOG.md` — prepend dated entry summarizing the language-policy work, files changed, verification results from Phase 6, manual smoke results from Phase 7.
- `.agent/overview.md` — update "Current Phase" / "Next Step" / "Known Blockers" to reflect language-policy completion.
- `.agent/debt_tech_backlog.md` — append P2 entry: "Wire `PersonaContextBuilder` and `SpeechStylePolicy` into `PromptBuilder` so the dormant persona slice is consumed by chat prompt assembly. Currently `PromptBuilder` only consumes `ILanguagePolicy`."

#### Files That Must Not Be Touched

- Source code under `src/`.
- Tests under `tests/`.
- Documentation under `docs/`.
- `.opencode/` (already dirty from Phase 6 work, out of this plan's scope).
- Other `mem_library/*` files.
- `.agent/log_notes.md` (no failure notes to add — Phase 7 either passed or escalated to a separate ticket).

#### Steps

1. Run `/update-memory` workflow. The workflow itself reads the file states and proposes the writes.
2. Confirm the four memory files were updated (and only those).

#### Verification

- `git status --short .agent/` shows only those four files modified. (Note: `.agent/` is gitignored, so the test is whether the files exist with the new content; `git status` will not show them.)
- Use direct file reads to confirm: `Read .agent/mem_library/03_iris_persona.md` shows §21; `Read .agent/PROJECT_LOG.md` shows the new top entry; `Read .agent/overview.md` shows updated phase; `Read .agent/debt_tech_backlog.md` shows the new P2 entry.

#### Rollback

- Memory files are not under version control; rollback is manual file edit. Keep original content in a temporary buffer if any uncertainty before the update.

#### Acceptance Checkpoint

Gate G satisfied. `mem_library/03_iris_persona.md` §21 present. `PROJECT_LOG.md` has the dated entry. `overview.md` reflects new state. `debt_tech_backlog.md` has the new P2 item.

---

## 6. Testing Plan

### Unit Tests

Created in Phase 2 and Phase 3/4:

- **`RussianDefaultLanguagePolicyTests`** (Phase 2): T-LANG-01 (Russian baseline for `"ru"`), T-LANG-02 (`[Theory]` null/empty/whitespace fallback), T-LANG-03 (`[Theory]` unknown-language fallback). All assert equality against the canonical `"ru"` baseline output to avoid hardcoding the Russian text.
- **`LanguageInstructionBuilderTests`** (Phase 2): T-LANG-04 (Cyrillic presence), T-LANG-05 (technical-token rule keyword presence), T-LANG-06 (code-stays-English rule keyword presence). Use keyword-presence checks, not verbatim string equality.
- **`PromptBuilderTests`** (Phase 3): existing test updated to use stub `ILanguagePolicy` (T-LANG-09 in design); T-LANG-07 (uses injected stub policy); T-LANG-08 (default Russian policy ≠ legacy English baseline literal).
- **`DependencyInjectionTests`** (Phase 4): three existing tests updated to pass `LanguageOptions.Default`; T-LANG-10 (null `LanguageOptions` throws); T-LANG-11 (`ILanguagePolicy` resolves from provider).

### Integration Tests

None new. Existing `Iris.IntegrationTests` are unaffected; no integration test currently asserts system-prompt content.

### Architecture Tests

No new tests. Existing `Iris.Architecture.Tests` (8 tests per `.agent/PROJECT_LOG.md`) must continue to pass without modification:

- Dependency-direction tests are satisfied (no new project references).
- Forbidden-namespace tests are satisfied (`Iris.Application.Persona.Language` is under the existing `Iris.Application.*` rules).
- Project-reference tests are satisfied (no `.csproj` changes).

### Regression Tests

The 14 `SendMessageHandlerTests` serve as the regression net for the `PromptBuilder` ctor change — they don't assert prompt content but exercise the full handler orchestration with the new ctor. T-LANG-08 specifically locks the regression that the legacy English baseline must not reappear.

### Manual Verification

Phase 7: M-LANG-01..03 against `llama3:latest` via Ollama. Operator-driven.

---

## 7. Documentation and Memory Plan

### Documentation Updates

- **None during Phases 0–7.** The spec (`docs/specs/2026-05-01-iris-default-language-russian.spec.md`) and design (`docs/designs/2026-05-01-iris-default-language-russian.design.md`) already exist; no `docs/` updates are required by this plan beyond optional manual-smoke notes the operator may write in Phase 7.
- **No README, AGENTS.md, or guide updates.**

### Agent Memory Updates

Phase 8, via `/update-memory` only:

- `.agent/mem_library/03_iris_persona.md` §21 "Default Language" — durable persona intent.
- `.agent/PROJECT_LOG.md` — work-completion entry.
- `.agent/overview.md` — phase/state refresh.
- `.agent/debt_tech_backlog.md` — P2 entry for deferred persona wiring.

No memory writes are performed by `/implement`.

---

## 8. Verification Commands

Run from repository root in PowerShell:

```powershell
# Phase 0 reconnaissance
git status --short

# Phase 1 verification
dotnet build .\src\Iris.Application\Iris.Application.csproj

# Phase 2 verification
dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Persona.Language"

# Phase 3 verification
dotnet build .\src\Iris.Application\Iris.Application.csproj
dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Chat.Prompting|FullyQualifiedName~Chat.SendMessage|FullyQualifiedName~Persona.Language"

# Phase 4 verification (Desktop intentionally broken at this point)
dotnet build .\src\Iris.Application\Iris.Application.csproj
dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build

# Phase 5 verification (full solution restored)
dotnet build .\Iris.slnx

# Phase 6 full verification (the gate before Phase 7/8)
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
git status --short

# Phase 7 manual smoke (operator)
# (no command; operator launches Iris.Desktop and exercises M-LANG-01..03)

# Phase 8 memory update via /update-memory workflow
# (no shell command; the workflow inspects and writes .agent/* files)
```

All commands are read-only or build/test commands. **No destructive command appears in this plan.**

---

## 9. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| **R1** — `llama3:latest` ignores Russian system prompt and responds in English. | Manual smoke M-LANG-01..03 fails. Acceptance not satisfied. | Design Option E (few-shot Russian example) reserved as fallback. Escalate via separate spec amendment if triggered. |
| **R2** — Implementation drifts to `internal sealed` per spec AC-005, breaking test compilation. | Test project cannot construct types; build fails. | Plan explicitly fixes `public sealed` per design §15 R2. Phase 1's verification catches regressions immediately. |
| **R3** — Chosen Russian baseline text is awkward or off-tone vs. persona §18. | Manual smoke produces correct language but wrong feel. Operator dissatisfied. | Operator review in Phase 7 is the qualitative gate. Adjustment is an in-place text change inside `LanguageInstructionBuilder._russianBaseline` — small blast radius, no architectural rework. |
| **R4** — Implementation agent stages or modifies dirty-tree files outside scope (e.g., `AGENTS.md`, Phase 6 Desktop UI changes). | Operator's in-flight work corrupted. | Plan §3 forbidden list and Phase 0 reconnaissance both record the dirty state. `/implement` skill enforces dirty-tree discipline. |
| **R5** — `Iris.Application.Tests` project has a target framework or analyzer setting that rejects `[Theory]` row count, breaking Phase 2 tests. | Phase 2 verification fails on syntax/analyzer. | Phase 0 inspects `Iris.Application.Tests.csproj` to confirm xUnit version and analyzer settings before authoring tests. |
| **R6** — Test count delta from Phase 6 doesn't match expectation (because of `[Theory]` row count). | False alarm during verification. | Plan accepts a range (149–155) rather than fixed count. Final count recorded in Phase 8 PROJECT_LOG entry. |
| **R7** — DI lifetime mismatch (e.g., scoped `LanguageOptions`) leaks across requests. | Subtle threading bug if defaults change. | Design §11 explicitly fixes Singleton for all four new types. Plan does not allow deviation. |
| **R8** — `Iris.Desktop/appsettings.local.json` configured with `Persona:Language:DefaultLanguage = "en"` will still produce Russian responses (hard-default fallback). | Operator may expect override-to-English to work. | This is intentional per spec §10 and design §10. If operator wants English override, that is a new spec. |
| **R9** — Architecture-test file paths in `tests/Iris.Architecture.Tests/` reference specific namespaces by string match. | New `Iris.Application.Persona.Language` namespace might trip an existing string-pattern rule. | Phase 0 inspects `Iris.Architecture.Tests` dependency-direction and forbidden-namespace tests. Phase 6 verification catches any architecture-test failure. |
| **R10** — Implementation agent skips Phase 8 `/update-memory`. | Persona-language intent is not durably recorded; future agents may revert the change. | Plan §1 and §2 explicitly require Phase 8. `iris-engineering` Gate G catches the omission in subsequent `/status` reports. |

---

## 10. Implementation Handoff Notes

### Critical Constraints

- **Application-only architectural change.** Do not touch any adapter, Domain, Shared, or host beyond the single line in `Iris.Desktop/DependencyInjection.cs:54`.
- **`public sealed` is non-negotiable.** Spec AC-005 said `internal sealed`; design §15 R2 overrode this. Do not silently re-raise to `internal sealed` — it would require either reflection or `InternalsVisibleTo`, both out of scope.
- **No new packages, references, or `InternalsVisibleTo`.** Confirmed by Phase 0 reconnaissance.
- **Russian baseline text is the subjective deliverable.** Tests assert structural content (Cyrillic presence, keyword presence), not the exact wording. The wording is the implementation agent's craft, reviewed by the operator in Phase 7.
- **Memory writes happen only in Phase 8.** No `.agent/*` writes during `/implement`.
- **Dirty tree is preserved.** Phase 6 in-flight files (`AGENTS.md`, `.opencode/skills/iris-engineering/SKILL.md`, Desktop UI/DI changes, etc.) are not staged, reverted, or normalized. Scope is strictly the in-scope file list.

### Risky Areas

- **Phase 3** is the only point where `PromptBuilder.cs`, `PromptBuilderTests.cs`, and `SendMessageHandlerTests.cs` change together. Implement the three edits in immediate succession before running verification — out-of-order edits leave the build broken.
- **Phase 4** intentionally leaves Desktop unbuildable until Phase 5. Do not attempt full `.slnx` build between Phase 4 and Phase 5.
- **Russian baseline text** must satisfy design §6 content rules 1–5 simultaneously. If any rule is missed, T-LANG-04..06 will fail.

### Expected Final State

- 4 new files in `src/Iris.Application/Persona/Language/`.
- 2 new test files in `tests/Iris.Application.Tests/Persona/Language/`.
- 6 modified files: `PromptBuilder.cs`, `Iris.Application/DependencyInjection.cs`, `Iris.Desktop/DependencyInjection.cs`, `PromptBuilderTests.cs`, `SendMessageHandlerTests.cs`, `Iris.Application.Tests/DependencyInjectionTests.cs`.
- Test count rises by ~8 (range 149–155 depending on `[Theory]` rows).
- `dotnet build .\Iris.slnx` 0/0; `dotnet test .\Iris.slnx` green; `dotnet format .\Iris.slnx --verify-no-changes` exit 0.
- M-LANG-01..03 manual smoke Pass.
- 4 memory files updated.
- No git commits (operator decides commit boundaries).

### Checks That Must Not Be Skipped

- Phase 0 reconnaissance — catches drift cheaply.
- Phase 6 full verification — gates Phase 7/8.
- Phase 7 manual smoke — only place where real model behavior is validated.
- Phase 8 memory update — Gate G obligation.
- `git status --short` after Phase 6 — confirms scope honored, dirty tree preserved.

---

## 11. Open Questions

No blocking open questions.

Non-blocking implementation notes:

- **N-PLAN-1:** Whether to add a documentation example to `src/Iris.Desktop/appsettings.local.example.json` (already untracked from Phase 6 work). Plan decision: **do not**, to keep blast radius minimal. If the operator later wants discoverable override, that's a follow-up touch.
- **N-PLAN-2:** Whether T-LANG-09 (the existing `Build_IncludesSystemMessageHistoryAndCurrentUserMessage` test renamed/refactored) should be additionally split into a separate "history mapping" test. Plan decision: **no split** — the existing test name remains, only the system-message assertion changes; design §13 endorses this.
- **N-PLAN-3:** Whether `LanguageInstructionBuilder` should be `public sealed class` or `public static class` (since it's stateless). Plan decision: **`public sealed class`** with a default ctor and a singleton DI registration, matching `SendMessageValidator` (also stateless, also `public sealed class`, registered as singleton). Consistency over micro-optimization.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

1. The Application test project is named `Iris.Application.Tests` (confirmed by design reconnaissance; spec §5 misnames it `Iris.Application.UnitTests`).
2. All new Application types are `public sealed` per project convention; spec AC-005's `internal sealed` is superseded by design §15 R2.
3. The operator runs `llama3:latest` via Ollama for manual smoke; if a different model is in use, manual smoke procedure is identical but Risk R1 may trigger differently.
4. Pre-existing dirty-tree files (`AGENTS.md`, `.opencode/skills/iris-engineering/SKILL.md`, `.opencode/commands/implement.md`, Phase 6 Desktop UI/DI files) are out of this plan's scope and remain untouched.
5. The Russian baseline text crafted in Phase 1/Phase 2 is the implementation agent's craft within the design §6 content-rule constraints; the wording is reviewed by the operator in Phase 7, not asserted verbatim by tests.
6. `.agent/` is gitignored; `git status` will not show memory writes from Phase 8.
7. Architecture tests do not pattern-match on the specific new namespace; if they do, Phase 0 reconnaissance flags it before Phase 1.

## Blocking Questions

No blocking questions.

---

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `docs/specs/2026-05-01-iris-default-language-russian.spec.md` |
| B — Design | ✅ Satisfied | `docs/designs/2026-05-01-iris-default-language-russian.design.md` |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after Phase 6 |
| E — Architecture Review | ⬜ Not yet run | No boundary changes expected (Application-only); reviewer may still want to confirm Singleton DI lifetime + new namespace placement |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` (Phase 8) after Phase 7 manual smoke |
