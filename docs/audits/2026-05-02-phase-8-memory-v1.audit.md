# Formal Audit Report: Phase 8 — Memory v1 Implementation

## 1. Summary

### Audit Status

**Blocked by P1 issues**

### Final Decision

**Changes requested**

### High-Level Result

Phase 8 Memory v1 has solid Domain, Application, Persistence, and DI implementation that is architecturally clean and passes 175/175 mechanical tests with zero build/format violations. The plan was followed closely for layers Domain → Application → Persistence and the boundary architecture is preserved (12 architecture tests green, 4 new memory boundary tests added).

However, the **Desktop UI is partially non-functional**:
- The memory list never loads automatically (`LoadMemoriesAsync` is wired but never invoked).
- The "Забыть" button on each memory card has no `Command` binding and does nothing when clicked.

These two defects make FR-021/FR-022/FR-023 (the user-visible "view memories / delete a memory" workflow) **non-functional in the current build**, which is the primary user-facing acceptance scenario for v1.

In addition, two large categories of planned automated tests are absent:
- **~31 Application-layer unit tests** (T-APP-MEM-01..24 + T-APP-PROMPT-01..03) — the entire `tests/Iris.Application.Tests/Memory/` folder does not exist.
- **5 Persistence integration tests** (T-PERS-MEM-01..05) — the entire `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` is missing.

The implementation that exists is high quality, but the absence of these tests means the search semantics, handler error paths, and EF Core round-trip behaviour are all unverified in CI. Combined with the broken UI, readiness cannot be approved.

## 2. Context Reviewed

- **Specification:** `docs/specs/2026-05-02-phase-8-memory-v1.spec.md` (339 lines, fully reviewed)
- **Design:** `docs/designs/2026-05-02-phase-8-memory-v1.design.md` (770 lines, fully reviewed)
- **Implementation plan:** `docs/plans/2026-05-02-phase-8-memory-v1.plan.md` (817 lines, fully reviewed)
- **Git status:** branch `feat/avatar-v1-and-opencode-v2` @ `6955a4f`; 29 modified tracked + 15 untracked
- **Git diff:** 1043 insertions, 103 deletions across 29 tracked files
- **Source files reviewed:**
  - Domain: `Memory.cs`, `MemoryContent.cs`, `MemoryId.cs`, `MemoryStatus.cs`, `MemoryKind.cs`, `MemoryImportance.cs`, `MemorySource.cs`
  - Application: `IMemoryRepository.cs`, `PromptBuilder.cs`, `SendMessageHandler.cs`, `DependencyInjection.cs`, `MemoryContextBuilder.cs`, `MemoryPromptFormatter.cs`, all 5 handlers, `MemoryOptions.cs`, `MemoryDto.cs`
  - Persistence: `MemoryEntity.cs`, `MemoryEntityConfiguration.cs`, `MemoryMapper.cs`, `MemoryRepository.cs`, `IrisDbContext.cs`, `DependencyInjection.cs`
  - Desktop: `IIrisApplicationFacade.cs`, `IrisApplicationFacade.cs`, `MemoryViewModel.cs`, `MemoryViewModelItem.cs`, `MemoryView.axaml`, `MainWindow.axaml`, `MainWindowViewModel.cs`, `DependencyInjection.cs`
- **Test files reviewed:**
  - New: `MemoryTests.cs`, `MemoryContentTests.cs`, `MemoryIdTests.cs`, `MemoryBoundaryTests.cs`
  - Updated: `PromptBuilderTests.cs`, `SendMessageHandlerTests.cs`, `DependencyInjectionTests.cs`, `FakeIrisApplicationFacade.cs`, `AvatarViewModelTests.cs`
- **Documentation/memory:** `.agent/log_notes.md` (Phase 8 manual smoke gap entry confirmed)
- **Verification evidence:** Build 0/0, Tests 175/175, format pass on Domain/Application/Desktop/Persistence projects (test projects timed out but ran clean per build)

## 3. Pass 1 — Spec Compliance

### Result

**Partial**

### Findings

#### P0

- None.

#### P1

- **P1-001 Desktop memory list never loads** (FR-021/FR-023 broken at runtime). See §9.
- **P1-002 Forget button has no Command binding** (FR-022/FR-023 broken at UI level). See §9.

#### P2

- **P2-001 Out-of-scope Russian "remember/forget" voice/natural-language pipeline** is not reachable from chat — but spec §3.2 explicitly defers this to Phase 9+, so this is consistent with scope. Note only.

#### Notes

- FR-001..FR-016 (Domain + Application handlers + repository methods): implemented correctly. Code paths and error translations match spec §10 error table.
- FR-017..FR-020 (Prompt injection): correctly implemented. `PromptBuilder` adds a second System message labelled `Известные факты:` only when memories exist; the empty-memory path is byte-equivalent to baseline (verified by `PromptBuilderTests.Build_IncludesSystemMessageHistoryAndCurrentUserMessage` collection assertion). FR-019 byte-equivalence is preserved.
- FR-024 (navigation): `MainWindow.axaml` adds a TabControl with `Чат` and `Память` tabs — matches design §6.
- FR-025 (no direct adapter access from Desktop): verified in code (`MemoryViewModel` only uses `IIrisApplicationFacade`); also enforced by `MemoryBoundaryTests.Desktop_ViewModels_do_not_reference_IMemoryRepository_or_IrisDbContext`.
- FR-026..FR-028 (cross-cutting): Russian default unchanged; existing chat works (82 integration tests pass); no new background tasks.
- AC-001..AC-014 (architecture constraints): all preserved. Verified via 12 architecture tests + 4 new memory boundary tests, all green.
- AC-013 (out-of-scope placeholders untouched): verified — `Memory/Embeddings/`, `Memory/Consolidation/`, `Memory/Audit/`, `Memory/Extract/`, `Memory/Ranking/`, `Memory/Policies/`, `Memory/Forget/` (old placeholder), `Memory/Recall/`, `Memory/Remember/` all still contain 10-line `internal class` shells. The new code lives in parallel `Memory/Commands/`, `Memory/Queries/`, `Memory/Context/`, `Memory/Contracts/`, `Memory/Options/` folders per design Open Q6 resolution.

## 4. Pass 2 — Test Quality

### Result

**Partial**

### Findings

#### P0

- None.

#### P1

- **P1-003 Application memory unit tests entirely missing** — see §9.
- **P1-004 Persistence memory integration tests entirely missing** — see §9.
- **P1-005 PromptBuilder memory injection tests (T-APP-PROMPT-01..03) missing** — see §9.

#### P2

- **P2-002 `SendMessageHandler` memory-degradation path is implicitly tested only by happy-path assertions** — see §9.

#### Notes

- **Domain tests (5 in `MemoryTests`, 5 in `MemoryContentTests`, 6 in `MemoryIdTests` = 16 total).** Cover happy path, idempotent forget, update-on-forgotten rejection, content empty/whitespace/length validation. Assertions check behaviour (status transitions, timestamps, exception codes), not implementation noise. Quality is good.
- **Architecture tests (4 new in `MemoryBoundaryTests`).** Cover Domain→EF, Application.Memory→MemoryEntity, Desktop ViewModels→IMemoryRepository/IrisDbContext, Application→Persistence assembly. Each test uses constructors/fields/properties reflection (matches existing pattern). Mutation-safe.
- **Updated `PromptBuilderTests`** correctly use the new constructor with stub repository + formatter and confirm baseline byte equivalence in the "no memories" path. Not the same as a dedicated `PromptBuilderMemoryTests` for FR-018/FR-020 — see P1-005.
- **Updated `DependencyInjectionTests`** correctly add the 3rd `MemoryOptions.Default` argument and a `FakeMemoryRepository`.
- **Updated `FakeIrisApplicationFacade`** correctly implements 4 new methods returning success/empty results — keeps integration tests compiling.
- **Test count reality:** plan §10 Implementation Handoff Notes predicted ~204 tests (155 baseline + 16 Domain + 28 App memory + 3 prompt-with-memory + 5 Persistence + 4 Architecture). Current is **175** = 155 baseline + 16 Domain + 4 Architecture only. The gap of ~31 tests is exactly the missing Application + Persistence test files.

## 5. Pass 3 — SOLID / Architecture Quality

### Result

**Passed**

### Findings

#### P0

- None.

#### P1

- None.

#### P2

- **P2-003 `RememberExplicitFactHandler` injects `MemoryOptions` but does not use it.** See §9.

#### Notes

- **Dependency direction preserved.** `Iris.Application` references only `Iris.Domain` and `Iris.Shared`. `Iris.Persistence` references `Iris.Application`, `Iris.Domain`, `Iris.Shared`. `Iris.Desktop` composes via `IIrisApplicationFacade` — no `IMemoryRepository` or `IrisDbContext` reference (verified by `MemoryBoundaryTests.Desktop_ViewModels_do_not_reference_IMemoryRepository_or_IrisDbContext`).
- **No god service.** Five separate handlers + one focused `MemoryContextBuilder` + one `MemoryPromptFormatter`. AC-010 satisfied.
- **No service locator.** All Application services use constructor injection. AC-009 satisfied.
- **Port shape correct.** `IMemoryRepository` has 5 specific methods with clear contracts, no generic `IRepository<T>`. Roadmap §6 Phase 3 constraint satisfied.
- **`PromptBuilder` lifetime correctly upgraded** Singleton→Scoped because it depends on scoped `MemoryContextBuilder`. R-003 mitigated. No service-locator workaround.
- **`Memory.Rehydrate` made public** (per plan §10 #7) with XML doc explaining it's mapper-only. Compliant alternative to `InternalsVisibleTo`. Acceptable trade-off.
- **`Memory.Forget` correctly returns `bool changed`** and `ForgetMemoryHandler` skips `UpdateAsync`+`CommitAsync` when not changed. R-004 mitigated; FR-007 idempotency preserved without spurious DB writes.
- **Type alias `DomainMemory = Iris.Domain.Memories.Memory`** used consistently across Application and Persistence to avoid namespace/type name collision with `Iris.Application.Memory.*` namespace. Pragmatic and readable.
- **Out-of-scope placeholders untouched** — `Memory/Forget/` (old), `Memory/Recall/`, `Memory/Remember/` still contain empty `internal class` placeholders alongside the new `Memory/Commands/`, `Memory/Queries/` work. AC-013 satisfied.

## 6. Pass 4 — Clean Code / Maintainability

### Result

**Passed with P2 notes**

### Findings

#### P0

- None.

#### P1

- None.

#### P2

- **P2-004 `MemoryRepository.SearchActiveAsync` does not sanitize query for `LIKE` wildcards.** A user query containing `%` or `_` will match unintended content. See §9.
- **P2-005 `MemoryViewModel` constructs `RememberCommand` in ctor but `ForgetCommand` is missing entirely.** See §9.
- **P2-006 `MemoryPromptFormatter` is a non-sealed plain class** while peers (`PromptBuilder`, `MemoryContextBuilder`, every handler) are `sealed`. Minor consistency issue.
- **P2-007 `MemoryViewModel` unused constructor parameter mismatch in awaited result type qualifier:** `Result<Application.Memory.Commands.RememberMemoryResult>` is verbose; `using` already imports `Iris.Application.Memory.Commands`. Note only.

#### Notes

- Naming is clear and Russian/English split is sensible (UI labels Russian, code English).
- Method sizes are reasonable. Largest handler `UpdateMemoryHandler.HandleAsync` is 94 lines including 4 try/catch blocks — slightly verbose but matches the chat slice idiom.
- Error codes are stable strings (`memory.empty_content`, `memory.not_active`, `memory.persistence_failed`, etc.) — good for downstream UI mapping in v2.
- `MemoryViewModel.MapKindLabel` and `MapImportanceLabel` are pure switch expressions — clean.
- `MemoryEntityConfiguration` follows the existing `_utcTicksConverter` pattern from `ConversationEntityConfiguration` — consistent with project conventions.
- No hidden side effects. No reflection. No dynamic dispatch. Cancellation tokens forwarded throughout.
- `MemoryView.axaml` is straightforward and testable visually.

## 7. Additional Risk Checks

### Reliability

- **R-001 (existing DB schema mismatch) accepted by spec/design.** `EnsureCreatedAsync` does not add the `memories` table to existing dev databases. Documented in `.agent/log_notes.md`. Acceptable for current dev workflow.
- **R-002 (silent swallow on `MemoryContextBuilder` failure) accepted by spec/design.** `SendMessageHandler` line 105–112 catches everything (no `OperationCanceledException` exclusion) and falls back to `Array.Empty<DomainMemory>()`. This means even cancellation requests during memory selection are converted to empty memory — see P2-008 below in Notes.

### Documentation / Memory

- **Memory files updated correctly:**
  - `.agent/log_notes.md` records the Memory v1 manual smoke gap with M-MEM-01..05 list.
  - `.agent/PROJECT_LOG.md` and `.agent/overview.md` were stated as updated in the previous turn's progress summary (not re-verified in this audit; assumed per the recorded log_notes entry style).
- **Spec/design/plan saved correctly** under `docs/specs/`, `docs/designs/`, `docs/plans/` with kebab-case dated filenames. `.opencode` workflow conventions preserved.
- DOC-004 (architecture.md note about `IrisDbContext.Memories`) — not verified in this audit; the spec says "if appropriate". Note only.

### Migration / Rollback

- No EF migrations introduced (per design Option C rejection). Schema bootstrap via `EnsureCreatedAsync`. Acceptable v1.
- Forward path for users with existing `iris.db`: delete the file. Recorded in log_notes R-001.
- Rollback per phase as defined in plan is intact (each phase's revert path stands alone).

### Notes

- **P2-008 `SendMessageHandler` swallows `OperationCanceledException` from memory selection.** Lines 105–112 catch `Exception` without the standard `when (exception is not OperationCanceledException)` guard used elsewhere in the same file. A user-cancelled chat send during memory selection would be converted to an empty memory list and the chat would continue. Subtle inconsistency with the rest of the handler. Severity: P2 because cancellation right after memory selection still propagates downstream.

## 8. Verification Evidence

| Command | Result | Notes |
|---|---|---|
| `dotnet --version` | Passed | SDK 10.0.201 |
| `dotnet build .\Iris.slnx` | Passed | 0 warnings, 0 errors, 44.58 s |
| `dotnet test .\Iris.slnx --no-build` | Passed | 175/175 tests pass (App 36, Arch 12, Domain 44, Infra 1, Integration 82) |
| `dotnet format .\src\Iris.Domain\*.csproj --verify-no-changes` | Passed | 0 violations |
| `dotnet format .\src\Iris.Application\*.csproj --verify-no-changes` | Passed | 0 violations |
| `dotnet format .\src\Iris.Desktop\*.csproj --verify-no-changes` | Passed | 0 violations |
| `dotnet format .\src\Iris.Persistence\*.csproj --verify-no-changes` | Passed | 0 violations |
| `dotnet format .\Iris.slnx --verify-no-changes` (whole solution) | Skipped | Tool exceeded 60 s timeout; aggregate result not captured. Per-project results above are clean. |
| `git status` / `git diff --name-status` / `git diff --stat` | Passed | 29 M + 15 ?? as expected; verification did not modify tracked files |

### Verification Gaps

- **Whole-solution `dotnet format --verify-no-changes` did not complete in one shot.** Per-project format on the four touched source projects is clean. Test-project format was attempted but timed out at 30 s each (no errors emitted, just slow).
- **Manual smoke M-MEM-01..05 not performed.** Recorded as open in `.agent/log_notes.md`. This is the deciding evidence for FR-021..FR-025 (UI behaviour) and was already named as a gap.
- **No automated test exercises the loaded UI flow.** Even with smoke pending, no integration test loads `MemoryViewModel` and asserts that opening the view populates `Memories` — combined with P1-001/P1-002, no automated coverage detects the broken UI behaviour.

## 9. Consolidated Findings

### P0 — Must Fix

No P0 issues.

### P1 — Should Fix

#### P1-001: Desktop memory list never auto-loads

- **Evidence:**
  - `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` defines `LoadMemoriesAsync(CancellationToken)` (line 58) but no caller invokes it. No call from constructor (line 24-28), no `OnAttachedToVisualTree` override in `MemoryView.axaml.cs`, no DI-side initialization in `Iris.Desktop/DependencyInjection.cs`.
  - `src/Iris.Desktop/Views/MemoryView.axaml.cs` is a 10-line code-behind with only `InitializeComponent()`.
- **Impact:**
  - **FR-021 ("`MemoryView` must list all active memories") is not satisfied at runtime.** Opening the "Память" tab shows an empty list even when memories exist in the database. The list only populates after the user successfully calls `RememberAsync` or `ForgetAsync`.
  - **FR-023 ("After Forget, the list must refresh") is partially satisfied** because Forget reloads, but the user has no way to call Forget on a list they cannot see.
  - The full primary user-facing v1 acceptance scenario "open Memory tab → see remembered facts → click Forget" is broken.
  - Manual smoke M-MEM-02 ("open Память tab, verify the memory is listed") will fail.
- **Recommended fix:**
  - Either invoke `LoadMemoriesAsync` from `MemoryViewModel` constructor (fire-and-forget pattern matching `ChatViewModel` initial load if any) — simplest fix.
  - Or override `MemoryView.OnAttachedToVisualTree` in `MemoryView.axaml.cs` and call `((MemoryViewModel)DataContext).LoadMemoriesAsync(default)`.
  - Or expose `LoadMemoriesCommand : IAsyncRelayCommand` and bind it to a `Loaded` interaction trigger / explicit "Обновить" button.
  - Add an integration test that resolves `MemoryViewModel` from a real `ServiceProvider` with a fake facade returning two memories, awaits the load (or invokes the chosen trigger), and asserts `Memories.Count == 2`.

#### P1-002: "Забыть" button has no Command binding

- **Evidence:**
  - `src/Iris.Desktop/Views/MemoryView.axaml` line 47-49:
    ```xml
    <Button Grid.Column="1"
            Content="Забыть"
            VerticalAlignment="Center" />
    ```
  - No `Command="..."` attribute. No `Click` event handler in `MemoryView.axaml.cs`.
  - Even if a Command existed, the button's DataContext inside `<DataTemplate DataType="models:MemoryViewModelItem">` (line 24) is `MemoryViewModelItem`, not `MemoryViewModel`. To reach `MemoryViewModel.ForgetCommand` from inside the template, a `RelativeSource` or `ElementName` binding is required.
  - `MemoryViewModel.ForgetAsync(MemoryId, CancellationToken)` exists as a public async method but is not exposed as `IAsyncRelayCommand`.
- **Impact:**
  - **FR-022 ("Each memory card must offer a 'Forget' / 'Delete' action that invokes `ForgetMemoryHandler`") is not satisfied.** The button is rendered but does nothing when clicked.
  - **FR-023 ("After Forget, the list must refresh") is unreachable** because Forget cannot be triggered.
  - Manual smoke M-MEM-04 ("click Забыть on the memory; verify it disappears from the list") will fail.
- **Recommended fix:**
  - Add `IAsyncRelayCommand<MemoryId> ForgetCommand` to `MemoryViewModel` that calls `ForgetAsync(id, default)`.
  - In `MemoryView.axaml`, bind the button to that command via `RelativeSource`:
    ```xml
    <Button Grid.Column="1"
            Content="Забыть"
            Command="{Binding $parent[ItemsControl].DataContext.ForgetCommand}"
            CommandParameter="{Binding Id}"
            VerticalAlignment="Center" />
    ```
  - Add an integration test that resolves the VM, simulates `ForgetCommand.ExecuteAsync(memoryId)`, and asserts the facade's `ForgetAsync` was called with the right id.

#### P1-003: Application-layer memory unit tests entirely missing

- **Evidence:**
  - `tests/Iris.Application.Tests/Memory/` directory does not exist (verified via `Get-ChildItem`).
  - Spec §11.2 specifies T-APP-MEM-01..24 (handlers and `MemoryContextBuilder`) — none present.
  - Plan §6 Phase 3 step 23 explicitly lists 6 new test files under `tests/Iris.Application.Tests/Memory/Commands/`, `Queries/`, `Context/`, plus `Chat/Prompting/PromptBuilderMemoryTests.cs`. None were created.
- **Impact:**
  - `RememberExplicitFactHandler`, `ForgetMemoryHandler`, `UpdateMemoryHandler`, `RetrieveRelevantMemoriesHandler`, `ListActiveMemoriesHandler`, and `MemoryContextBuilder` have **zero unit-test coverage**. A regression that breaks (for example) the "update on forgotten returns Conflict" path, or a refactor that loses the silent swallow in `MemoryContextBuilder`, would not be detected by CI.
  - Integration tests cover only the chat orchestration through `SendMessageHandler` — not the memory handlers directly.
  - Acceptance criterion "All five handlers exist with full unit-test coverage" (spec §13) is not met.
- **Recommended fix:**
  - Add at minimum: T-APP-MEM-01 (Remember happy path), T-APP-MEM-06 (Forget happy path), T-APP-MEM-07 (Forget non-existent → not_found), T-APP-MEM-08 (Forget already-forgotten → idempotent success, no UpdateAsync call), T-APP-MEM-12 (Update on Forgotten → conflict), T-APP-MEM-21 (`MemoryContextBuilder` returns empty when none active), T-APP-MEM-22 (top-N respected). These 7 tests cover the critical regression-prone paths.
  - Use a `FakeMemoryRepository` (Dictionary-backed) and the existing `FakeUnitOfWork`/`FakeClock` infrastructure.

#### P1-004: Persistence integration tests for `MemoryRepository` entirely missing

- **Evidence:**
  - `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` does not exist.
  - Spec §11.3 specifies T-PERS-MEM-01..05.
  - Plan §6 Phase 4 step 7 explicitly lists this file with 5 specific tests.
- **Impact:**
  - `MemoryRepository.AddAsync`/`GetByIdAsync` round-trip (including enum→int and `DateTimeOffset?` ↔ tick-long conversions) is unverified.
  - `MemoryEntityConfiguration` index, `COLLATE NOCASE`, and `EnsureCreatedAsync` table creation are unverified.
  - `SearchActiveAsync` Cyrillic case-insensitivity (R-005 in design) is unverified — no test confirms whether `LIKE` + `NOCASE` actually folds Cyrillic case as designed.
  - Manual smoke M-MEM-05 (persistence across restart) is the only validation path; if it fails on first run, debugging is harder without unit-level isolation.
- **Recommended fix:**
  - Add at minimum: T-PERS-MEM-01 (round-trip with mixed-case Cyrillic content + non-null UpdatedAt), T-PERS-MEM-03 (ListActiveAsync excludes Forgotten), T-PERS-MEM-04 (SearchActiveAsync matches lowercase query against capital-case Cyrillic content). These three cover the highest-risk paths.
  - Reuse the existing `PersistenceTestContextFactory`.

#### P1-005: Prompt builder memory injection tests (T-APP-PROMPT-01..03) missing

- **Evidence:**
  - Spec §11.2 specifies T-APP-PROMPT-01 (byte-equivalent baseline), T-APP-PROMPT-02 (memory block as system role), T-APP-PROMPT-03 (no memory in user role). Plan §6 Phase 3 lists `PromptBuilderMemoryTests.cs`.
  - The existing `PromptBuilderTests` only confirms the empty-memory path indirectly (test `Build_IncludesSystemMessageHistoryAndCurrentUserMessage` asserts message count = 4 = system + 2 history + 1 user, which would still pass if the second system message were missing).
  - No test asserts `MemoryPromptFormatter.Format(...)` output is included as a system-role message when memories are non-empty.
- **Impact:**
  - **FR-018 (memory block must be system-role only) is not actively asserted.** A future refactor that accidentally moves the block into a user-role message would not break any test.
  - **FR-019 (byte-equivalent baseline) is partially asserted** by collection size, but the specific "no second system message when memories empty" invariant is fragile.
  - **FR-020 (Russian-labelled block) has no assertion** — the `Известные факты:` prefix could be silently changed.
- **Recommended fix:**
  - Add a `PromptBuilderMemoryTests` class with 3 tests:
    - Given empty memory list → exactly one system-role message that equals `_languagePolicy.GetSystemPrompt()` and no memory-block content.
    - Given two memories → second message has `ChatModelRole.System` and contains both memory contents and the `Известные факты:` prefix.
    - Given one memory → no `User`-role message contains the `Известные факты:` substring.

### P2 — Backlog

#### P2-001: Out-of-scope natural-language remember/forget pipeline not reachable from chat

- **Evidence:** `src/Iris.Application/Chat/SendMessage/SendMessageHandler.cs` does not parse user messages for "запомни" / "забудь" intents.
- **Impact:** Acceptable — spec §3.2 defers natural-language remember/forget to Phase 9+.
- **Recommended fix:** None for v1. Note only.

#### P2-002: `SendMessageHandler` memory-degradation path is implicitly tested only

- **Evidence:** `SendMessageHandlerTests` exercises `MemoryContextBuilder` only through a `FakeMemoryRepository` returning empty results. No test injects a `MemoryContextBuilder` that throws and asserts the chat send still succeeds.
- **Impact:** R-002 silent-swallow design is not regression-protected. A future change that lets the exception propagate would not break any test.
- **Recommended fix:** Add a test: inject a fake `MemoryContextBuilder` (or repository) that throws `InvalidOperationException`, send a chat message, assert `Result.IsSuccess` and that the prompt sent to the model contained no memory block.

#### P2-003: `RememberExplicitFactHandler` injects `MemoryOptions` but never uses it

- **Evidence:** `src/Iris.Application/Memory/Commands/RememberExplicitFactHandler.cs` line 14, 18, 24, 29: `MemoryOptions _memoryOptions` is stored but never read.
- **Impact:** Dead dependency. Adds noise to the constructor and DI registration. No functional bug.
- **Recommended fix:** Remove `MemoryOptions` from `RememberExplicitFactHandler` constructor, or use `MemoryOptions.MaxContentLength` to validate before calling `MemoryContent.Create`.

#### P2-004: `MemoryRepository.SearchActiveAsync` does not escape LIKE wildcards

- **Evidence:** `src/Iris.Persistence/Repositories/MemoryRepository.cs` line 87: `EF.Functions.Like(memory.Content, $"%{query}%")`. If the user types `50%` or `_test`, those characters are treated as SQL LIKE wildcards.
- **Impact:** Substring match returns more rows than the user intended. Severity is low because v1 does not expose `RetrieveRelevantMemoriesHandler` to Desktop UI.
- **Recommended fix:** Either escape `%`, `_`, `\` in the query string before interpolating, or use `EF.Functions.Contains(...)` if EF translates it for SQLite. Document trade-off in handler XML doc.

#### P2-005: `MemoryViewModel` exposes `RememberCommand` but not `ForgetCommand`

- **Evidence:** `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` line 27 creates `RememberCommand`. `ForgetAsync` exists as plain async method only.
- **Impact:** Inconsistency. Combined with P1-002, this is the upstream cause of the broken Forget UI.
- **Recommended fix:** Add `IAsyncRelayCommand<MemoryId> ForgetCommand` initialized in the constructor: `ForgetCommand = new AsyncRelayCommand<MemoryId>(id => ForgetAsync(id, default));`.

#### P2-006: `MemoryPromptFormatter` is non-sealed plain class

- **Evidence:** `src/Iris.Application/Memory/Context/MemoryPromptFormatter.cs` line 7: `public class MemoryPromptFormatter`. Compare to `public sealed class PromptBuilder`, `public sealed class MemoryContextBuilder`, all handlers `public sealed class`.
- **Impact:** Minor consistency. Inheritance is not used anywhere.
- **Recommended fix:** Add `sealed` modifier.

#### P2-007: `MemoryViewModel` verbose result-type qualification

- **Evidence:** `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` line 139: `Result<Application.Memory.Commands.RememberMemoryResult>` even though `using Iris.Application.Memory.Commands;` is not present.
- **Impact:** Readability only.
- **Recommended fix:** Add `using Iris.Application.Memory.Commands;` and use `RememberMemoryResult` directly.

#### P2-008: `SendMessageHandler` swallows OperationCanceledException from memory selection

- **Evidence:** `src/Iris.Application/Chat/SendMessage/SendMessageHandler.cs` lines 105-112: bare `catch` does not exclude `OperationCanceledException`, unlike every other catch in the same file (lines 90, 162, 173).
- **Impact:** Cancellation during memory selection is converted to "no memory" instead of propagating. Inconsistent with file-local convention.
- **Recommended fix:** Change to `catch (Exception exception) when (exception is not OperationCanceledException) { memories = Array.Empty<DomainMemory>(); }` matching file convention.

## 10. Suggested Fix Order

1. **P1-002 + P2-005** (together): Add `ForgetCommand` to `MemoryViewModel` and bind it in `MemoryView.axaml`. Smallest blast radius. Verifies the click flow works end-to-end.
2. **P1-001**: Trigger `LoadMemoriesAsync` on view attach or VM construction. Confirms memories are visible. Combined with #1, the primary user workflow is restored.
3. **P1-003** (subset): Add 7 critical Application-layer tests (Remember/Forget/Update happy paths + edge cases + MemoryContextBuilder top-N). Approximately 1–2 hours of work.
4. **P1-004** (subset): Add 3 critical Persistence integration tests (round-trip + ListActive excludes Forgotten + SearchActive case-insensitive Cyrillic). Approximately 1 hour.
5. **P1-005**: Add 3 PromptBuilder memory tests. Quick (15 min); high regression value.
6. **P2-002**: Add memory-degradation regression test for `SendMessageHandler`.
7. **P2-008**: Fix `OperationCanceledException` exclusion in the bare catch. One-line edit.
8. **P2-003**: Remove unused `MemoryOptions` from `RememberExplicitFactHandler`.
9. **P2-004**: Escape LIKE wildcards in `SearchActiveAsync` (or document acceptance).
10. **P2-006**, **P2-007**: Cosmetic cleanup (sealed modifier, using directive).
11. **Re-run `/verify`** after fixes to confirm test count increase, all green.
12. **Manual smoke M-MEM-01..05** with Ollama running, fresh `iris.db`. Record results in `.agent/log_notes.md`.
13. **Re-run `/audit`** for readiness recommendation.

## 11. Readiness Decision

**Not ready.**

Two P1 defects (P1-001, P1-002) make the primary v1 user-visible workflow ("open Память tab, view memories, click Forget") non-functional. Three P1 test gaps (P1-003, P1-004, P1-005) leave the new Application and Persistence behaviour, including the prompt-injection contract that protects existing chat functionality, unverified by automated tests.

Mechanical verification (build, format, architecture tests) is clean. Domain implementation is high quality. Architecture boundaries are preserved. Once the UI defects are fixed and the missing test files are added, this work should pass a re-audit cleanly.

**Manual smoke M-MEM-01..05 must be deferred until P1-001 and P1-002 are fixed** — running smoke now would simply confirm the broken UI without telling anyone anything new.

## Execution Note

No fixes were implemented.
No files were modified.

## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Reviewed | `docs/specs/2026-05-02-phase-8-memory-v1.spec.md` |
| B — Design | ✅ Reviewed | `docs/designs/2026-05-02-phase-8-memory-v1.design.md` |
| C — Plan | ✅ Reviewed | `docs/plans/2026-05-02-phase-8-memory-v1.plan.md` |
| D — Verify | ✅ Reviewed | Verification evidence section §8 — build 0/0, tests 175/175, format clean on core projects |
| E — Architecture Review | ✅ In audit | Pass 3 above; 12 architecture tests + 4 new memory boundary tests all green; no boundary violations found |
| F — Audit | ✅ Satisfied | This audit |
| G — Memory | ✅ Checked | `.agent/log_notes.md` records Phase 8 manual smoke gap; `.agent/PROJECT_LOG.md` and `.agent/overview.md` reported updated in implementation summary |
