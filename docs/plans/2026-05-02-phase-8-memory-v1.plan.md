# Implementation Plan: Phase 8 — Memory v1 (Explicit Memory)

## 1. Plan Goal

Implement Memory v1 as specified in `docs/specs/2026-05-02-phase-8-memory-v1.spec.md` and designed in `docs/designs/2026-05-02-phase-8-memory-v1.design.md`.

The plan delivers:
- `Memory` aggregate + value types in `Iris.Domain`,
- `IMemoryRepository` port + 5 handlers + `MemoryContextBuilder` + `MemoryPromptFormatter` in `Iris.Application`,
- prompt injection through extended `PromptBuilder` / `SendMessageHandler`,
- `MemoryEntity` + configuration + mapper + repository + `IrisDbContext` extension in `Iris.Persistence`,
- `MemoryView` + `MemoryViewModel` + facade extension in `Iris.Desktop`,
- new tests across Domain, Application, Persistence, and Architecture test projects.

All existing 155 tests must remain green. No new projects, packages, or `InternalsVisibleTo`.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-02-phase-8-memory-v1.spec.md`
- **Design:** `docs/designs/2026-05-02-phase-8-memory-v1.design.md`
- **Rules:** `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/workflow.md`, `.opencode/rules/dotnet.md`
- **Existing live code confirmed during reconnaissance:**
  - `Iris.Application.DependencyInjection.AddIrisApplication(SendMessageOptions, LanguageOptions)` — 2-param signature; must be extended to 3-param (add `MemoryOptions`).
  - `PromptBuilder` is currently a **Singleton** constructed as `new PromptBuilder(ILanguagePolicy)` in tests; must become **Scoped** with 3 constructor args.
  - `IrisDbContext` uses `EnsureCreatedAsync` (no migrations); `OnModelCreating` uses assembly scan for `IEntityTypeConfiguration<T>`.
  - `PersistenceTestContextFactory` creates a temp-file SQLite DB per test; can be reused for memory tests.
  - `FakeIrisApplicationFacade` in integration tests implements `IIrisApplicationFacade`; it must be extended when `IIrisApplicationFacade` gains new methods.
  - All Memory files in `Iris.Domain.Memories.*`, `Iris.Application.Memory.*`, `Iris.Persistence.Entities.MemoryEntity`, etc. are empty 10-line placeholders.
  - `Iris.Domain.Common.DomainException(code, message)` is the existing domain exception type.
  - `ConversationMapper` uses `Conversation.Rehydrate(...)` — confirms the Rehydrate-factory pattern to apply for `Memory`.
  - Architecture tests use `Assembly.GetReferencedAssemblies()` — no special test framework beyond `xUnit`.

### Assumptions

1. Working tree is clean at phase-start (confirmed by `git status` earlier; builder must verify before each phase).
2. `EnsureCreatedAsync` schema bootstrapping is the v1 migration strategy; developers delete `%APPDATA%\Iris\iris.db` to upgrade existing databases.
3. `PromptBuilder.Build` signature becomes `Build(PromptBuildRequest request, IReadOnlyList<Memory> memories)` — the memory list is resolved by `SendMessageHandler` before calling `Build`, keeping `Build` sync.
4. `FakeIrisApplicationFacade` must be extended to implement any new `IIrisApplicationFacade` methods; new memory methods return no-ops / empty success results by default.
5. `DependencyInjectionTests` calling `AddIrisApplication(SendMessageOptions, LanguageOptions)` must be updated to add the new 3rd `MemoryOptions` argument.
6. `PromptBuilderTests` directly instantiate `PromptBuilder(StubLanguagePolicy)` — they must be updated to provide the new 2 added constructor dependencies.
7. No `.axaml` navigation changes are required to `MainWindow` until the Desktop phase; earlier phases stay isolated.
8. The `Iris.IntegrationTests` project (folder `tests/Iris.IntegrationTests`) is the correct home for new persistence integration tests.

## 3. Scope Control

### In Scope

- Domain: `Memory`, `MemoryId`, `MemoryContent`, `MemoryStatus`, `MemoryKind`, `MemoryImportance`, `MemorySource` (replace empty placeholders).
- Application: `IMemoryRepository`, 5 handlers, `MemoryContextBuilder`, `MemoryPromptFormatter`, `MemoryOptions`, `MemoryDto`, command/query records.
- Application DI: 3rd `MemoryOptions` parameter + new service registrations + `PromptBuilder` Singleton→Scoped.
- Application `PromptBuilder` + `SendMessageHandler`: extended to accept and inject memory list.
- Persistence: `MemoryEntity`, `MemoryEntityConfiguration`, `MemoryMapper`, `MemoryRepository`, `IrisDbContext.Memories` DbSet, `DependencyInjection`.
- Desktop: `IIrisApplicationFacade`, `IrisApplicationFacade`, `MemoryViewModel`, `MemoryViewModelItem`, `MemoryView`, `MemoryCard`, `MainWindow` navigation, Desktop `DependencyInjection`.
- Tests: Domain unit, Application unit, Persistence integration, Architecture tests, `FakeIrisApplicationFacade` update, `DependencyInjectionTests` update, `PromptBuilderTests` update.
- Agent memory: `.agent/PROJECT_LOG.md` append, `.agent/overview.md` update — after implementation complete.

### Out of Scope

- Empty placeholder files for: `MemoryEmbedding*`, `MemoryConsolidation*`, `MemoryRanking*`, `MemoryAudit*`, `MemoryExtraction*`, `MemoryEndpoints*`, `MemoryApiMapper`, `MemoryEmbeddingTask`, `MemoryConsolidationTask`. These stay empty.
- `Iris.Api` memory endpoints.
- EF Core migrations.
- `ILogger<T>` injection into Application memory components.
- Semantic/embedding search.
- `MemorySensitivityPolicy` implementation.

### Forbidden Changes

- Must not add any new project, NuGet package, or `InternalsVisibleTo`.
- Must not change `Iris.Domain`'s project references (currently: none).
- Must not change `Iris.Application`'s project references (currently: `Iris.Domain`, `Iris.Shared`).
- Must not let `IrisDbContext` be referenced by `Iris.Application` or `Iris.Domain`.
- Must not let `MemoryViewModel` depend on `IMemoryRepository` or `IrisDbContext`.
- Must not change existing EF Core migrations (none exist; do not create them).
- Must not touch out-of-scope placeholder files.
- Must not modify `Iris.Shared`.
- Must not modify `Iris.ModelGateway`, `Iris.Tools`, `Iris.Voice`, `Iris.Perception`, `Iris.Worker`, `Iris.Api`, `Iris.SiRuntimeGateway`.

## 4. Implementation Strategy

Work progresses **inward-to-outward** — Domain first, then Application, then Persistence, then Desktop — matching the dependency direction. Each phase compiles and ideally passes tests before the next begins.

**Critical cross-phase constraints:**
- `PromptBuilder` and `DependencyInjectionTests` are touched in **Phase 3** (Application) because they depend on the new `MemoryContextBuilder` and `MemoryOptions` — but the signature breaks existing tests. Phase 3 therefore updates affected tests in the same phase, not later, to keep the build green at all times.
- `FakeIrisApplicationFacade` must be updated in **Phase 5** (Desktop) when `IIrisApplicationFacade` gains new methods, or all integration tests that use it will fail to compile.
- Persistence integration tests depend on `PersistenceTestContextFactory` — no changes needed there; just add a new test class for memory.

**Phase sequence:**

| # | Name | Primary area |
|---|---|---|
| 0 | Reconnaissance | Read-only verification |
| 1 | Domain — Memory aggregate | `Iris.Domain` |
| 2 | Domain — Tests | `Iris.Domain.Tests` |
| 3 | Application — Port, handlers, context builder, DI | `Iris.Application` + updated tests |
| 4 | Persistence — Entity, configuration, mapper, repository, DbContext | `Iris.Persistence` + integration tests |
| 5 | Desktop — Facade, ViewModel, View, navigation | `Iris.Desktop` + fake update |
| 6 | Architecture tests | `Iris.Architecture.Tests` |
| 7 | Final verification + memory files | All projects |

---

## 5. Phase Plan

---

### Phase 0 — Reconnaissance

#### Goal

Confirm clean working tree, build baseline, and verify no hidden dirty state before any edits.

#### Files to Inspect

- `git status --short` output.
- `src/Iris.Domain/Memories/` — all placeholder files.
- `src/Iris.Application/Memory/` — all placeholder files and folders.
- `src/Iris.Persistence/Entities/MemoryEntity.cs` — confirm it is empty.
- `src/Iris.Persistence/Configurations/MemoryEntityConfiguration.cs` — confirm it is empty.
- `src/Iris.Desktop/Views/MemoryView.axaml` and `.axaml.cs` — confirm structure.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml` — confirm it is empty/placeholder.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` — understand current direct-construction calls.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` — understand current `AddIrisApplication(2 args)` calls.
- `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs` — understand current `IIrisApplicationFacade` implementation.

#### Files Likely to Edit

- None.

#### Steps

1. Run `git status --short` — must show clean tree (or only the two new doc files from `/save-spec`/`/save-design`).
2. Run `dotnet build .\Iris.slnx --nologo --verbosity minimal` — must succeed with 0 errors.
3. Run `dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo` — must show 155/155 passed.
4. Spot-check placeholder files in Domain, Application, Persistence, and Desktop to confirm they are the empty 10-line shells.

#### Verification

- `dotnet build` → 0 errors, 0 warnings.
- `dotnet test` → 155/155 passed.
- `git status --short` → clean (or doc-only untracked).

#### Rollback

No edits made. Nothing to roll back.

---

### Phase 1 — Domain: Memory Aggregate

#### Goal

Replace the empty Domain memory placeholders with the real `Memory` aggregate and its supporting value types and enums. No tests yet; build must pass.

#### Files to Inspect Before Editing

- `src/Iris.Domain/Memories/Memory.cs` — current empty class.
- `src/Iris.Domain/Memories/MemoryId.cs` — confirm empty.
- `src/Iris.Domain/Memories/MemoryContent.cs` — confirm empty.
- `src/Iris.Domain/Memories/MemoryStatus.cs` — confirm empty.
- `src/Iris.Domain/Memories/MemoryKind.cs` — confirm empty.
- `src/Iris.Domain/Memories/MemoryImportance.cs` — confirm empty.
- `src/Iris.Domain/Memories/MemorySource.cs` — confirm empty.
- `src/Iris.Domain/Conversations/ConversationId.cs` — pattern reference for `MemoryId`.
- `src/Iris.Domain/Conversations/MessageContent.cs` — pattern reference for `MemoryContent`.
- `src/Iris.Domain/Common/DomainException.cs` — used for invariant enforcement.
- `src/Iris.Domain/Conversations/Conversation.cs` — check `Rehydrate` pattern to confirm `Memory.Rehydrate` design.

#### Files Likely to Edit

- `src/Iris.Domain/Memories/Memory.cs`
- `src/Iris.Domain/Memories/MemoryId.cs`
- `src/Iris.Domain/Memories/MemoryContent.cs`
- `src/Iris.Domain/Memories/MemoryStatus.cs`
- `src/Iris.Domain/Memories/MemoryKind.cs`
- `src/Iris.Domain/Memories/MemoryImportance.cs`
- `src/Iris.Domain/Memories/MemorySource.cs`

Do NOT touch: `MemoryLifecycle.cs`, `MemoryUsageStats.cs`, `MemorySensitivity.cs`, `MemoryConfidence.cs` (out-of-scope placeholders — confirm they are empty shells and leave them alone).

#### Files That Must Not Be Touched

- Anything in `Iris.Shared`, `Iris.Application`, `Iris.Persistence`, `Iris.Desktop`.
- Out-of-scope Domain memory placeholders (`MemoryLifecycle.cs`, `MemoryUsageStats.cs`, `MemorySensitivity.cs`, `MemoryConfidence.cs`).

#### Steps

1. **`MemoryId`** — implement as a `sealed record` with `Guid Value`, private constructor, `New()` factory, `From(Guid)` factory (throws `DomainException("memory.empty_id", ...)` on `Guid.Empty`). Mirror `ConversationId`.
2. **`MemoryContent`** — implement as a `sealed record` with `string Value`, private constructor, `static Create(string value)` that throws `DomainException("memory.empty_content", ...)` for null/whitespace and `DomainException("memory.content_too_long", ...)` for length > `MaxLength`. Add `public const int MaxLength = 4000`.
3. **`MemoryStatus`** — enum: `Active = 0`, `Forgotten = 1`.
4. **`MemoryKind`** — enum: `Fact = 0`, `Preference = 1`, `Note = 2`.
5. **`MemoryImportance`** — enum: `Low = 0`, `Normal = 1`, `High = 2`.
6. **`MemorySource`** — enum: `UserExplicit = 0`.
7. **`Memory`** — sealed class (mirrors `Message`):
   - Private constructor.
   - Public read-only properties: `Id`, `Content`, `Kind`, `Importance`, `Status`, `Source`, `CreatedAt`, `UpdatedAt` (nullable `DateTimeOffset?`).
   - `static Memory Create(MemoryId id, MemoryContent content, MemoryKind kind, MemoryImportance importance, MemorySource source, DateTimeOffset createdAt)` — validates enum values via `Enum.IsDefined`.
   - `internal static Memory Rehydrate(MemoryId id, MemoryContent content, MemoryKind kind, MemoryImportance importance, MemoryStatus status, MemorySource source, DateTimeOffset createdAt, DateTimeOffset? updatedAt)` — bypasses validation, for mapper use only.
   - `bool Forget(DateTimeOffset now)` — returns `false` and no-ops if already `Forgotten`; transitions `Active → Forgotten`, sets `UpdatedAt = now`, returns `true`.
   - `void UpdateContent(MemoryContent newContent, DateTimeOffset now)` — throws `DomainException("memory.not_active", "Memory is not active and cannot be updated.")` when status is `Forgotten`; otherwise replaces `Content`, sets `UpdatedAt = now`.

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
```

Expected: 0 errors, 0 warnings. (Tests not run yet — no memory tests exist at this phase.)

#### Rollback

Revert all changes in `src/Iris.Domain/Memories/` to the empty 10-line placeholder shells. No other files changed.

---

### Phase 2 — Domain Tests

#### Goal

Add all Domain memory unit tests. Build and full test suite must pass.

#### Files to Inspect Before Editing

- `tests/Iris.Domain.Tests/` directory — confirm naming convention and test project structure.
- `src/Iris.Domain/Memories/Memory.cs` (Phase 1 output) — confirm API surface.
- `src/Iris.Domain/Memories/MemoryContent.cs` — confirm `MaxLength`.

#### Files Likely to Edit

- New file: `tests/Iris.Domain.Tests/Memories/MemoryTests.cs`
- New file: `tests/Iris.Domain.Tests/Memories/MemoryContentTests.cs`
- New file: `tests/Iris.Domain.Tests/Memories/MemoryIdTests.cs`

#### Files That Must Not Be Touched

- Any source file in `Iris.Domain`, `Iris.Application`, `Iris.Persistence`, `Iris.Desktop`.
- Existing Domain test files.

#### Steps

Write tests per spec §11.1:

- **`MemoryTests`**: T-DOM-MEM-01 (Create succeeds), T-DOM-MEM-05 (`Forget` transitions Active→Forgotten, returns `true`), T-DOM-MEM-06 (`Forget` on already-Forgotten returns `false` and no-ops), T-DOM-MEM-07 (`UpdateContent` replaces content and updates timestamp), T-DOM-MEM-08 (`UpdateContent` on Forgotten throws `DomainException`).
- **`MemoryContentTests`**: T-DOM-MEM-02 (Create rejects empty), T-DOM-MEM-03 (Create rejects whitespace), T-DOM-MEM-04 (Create rejects > 4000 chars), plus happy-path accepting exactly 4000-char content.
- **`MemoryIdTests`**: T-DOM-MEM-09 (equality, `New()` produces non-empty, `From(Guid.Empty)` throws).

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
```

Expected: 0 errors, all prior 155 + new Domain memory tests green.

#### Rollback

Delete the new test files. Nothing else changed.

---

### Phase 3 — Application: Port, Handlers, Context Builder, DI, Updated Tests

#### Goal

Implement all Application memory components and update all existing Application tests that break due to the `AddIrisApplication` signature change and `PromptBuilder` constructor change. By end of this phase, full test suite must be green.

This is the highest-risk phase because it modifies live Application code and existing tests in the same step.

#### Files to Inspect Before Editing

- `src/Iris.Application/Abstractions/Persistence/IMemoryRepository.cs` — empty placeholder to replace.
- `src/Iris.Application/Memory/` — all placeholder folders/files.
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` — current constructor and `Build` method.
- `src/Iris.Application/Chat/Prompting/PromptBuildRequest.cs` — current shape.
- `src/Iris.Application/Chat/SendMessage/SendMessageHandler.cs` — current constructor and `HandleAsync` flow.
- `src/Iris.Application/DependencyInjection.cs` — current 2-param signature.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` — direct `new PromptBuilder(stub)` calls.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` — `AddIrisApplication(2 args)` calls.
- `src/Iris.Application/Chat/Prompting/PromptBuildResult.cs` — confirm shape.
- `src/Iris.Application/Abstractions/Models/Contracts/Chat/ChatModelMessage.cs` — confirm `System` role is available.

#### Files Likely to Edit

**Application source:**
- `src/Iris.Application/Abstractions/Persistence/IMemoryRepository.cs` — replace placeholder with real port.
- New: `src/Iris.Application/Memory/Options/MemoryOptions.cs`
- New: `src/Iris.Application/Memory/Contracts/MemoryDto.cs`
- New: `src/Iris.Application/Memory/Commands/RememberExplicitFactCommand.cs`
- New: `src/Iris.Application/Memory/Commands/RememberExplicitFactHandler.cs`
- New: `src/Iris.Application/Memory/Commands/ForgetMemoryCommand.cs`
- New: `src/Iris.Application/Memory/Commands/ForgetMemoryHandler.cs`
- New: `src/Iris.Application/Memory/Commands/UpdateMemoryCommand.cs`
- New: `src/Iris.Application/Memory/Commands/UpdateMemoryHandler.cs`
- New: `src/Iris.Application/Memory/Commands/RememberMemoryResult.cs`
- New: `src/Iris.Application/Memory/Commands/UpdateMemoryResult.cs`
- New: `src/Iris.Application/Memory/Queries/RetrieveRelevantMemoriesQuery.cs`
- New: `src/Iris.Application/Memory/Queries/RetrieveRelevantMemoriesHandler.cs`
- New: `src/Iris.Application/Memory/Queries/ListActiveMemoriesQuery.cs`
- New: `src/Iris.Application/Memory/Queries/ListActiveMemoriesHandler.cs`
- New: `src/Iris.Application/Memory/Context/MemoryContextBuilder.cs`
- New: `src/Iris.Application/Memory/Context/MemoryPromptFormatter.cs`
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` — extend constructor + `Build` overload.
- `src/Iris.Application/Chat/SendMessage/SendMessageHandler.cs` — inject `MemoryContextBuilder`, call it before `PromptBuilder.Build`.
- `src/Iris.Application/DependencyInjection.cs` — add `MemoryOptions` param, register new services, change `PromptBuilder` to Scoped.

**Application tests (must update to compile):**
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` — update 3 tests: provide `MemoryContextBuilder` stub + `MemoryPromptFormatter` stub in constructor, use empty memory list in `Build(request, [])` calls.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` — update 5 tests: add `MemoryOptions.Default` as 3rd argument, add `FakeMemoryRepository` inner class.

**New Application tests:**
- New: `tests/Iris.Application.Tests/Memory/Commands/RememberExplicitFactHandlerTests.cs`
- New: `tests/Iris.Application.Tests/Memory/Commands/ForgetMemoryHandlerTests.cs`
- New: `tests/Iris.Application.Tests/Memory/Commands/UpdateMemoryHandlerTests.cs`
- New: `tests/Iris.Application.Tests/Memory/Queries/RetrieveRelevantMemoriesHandlerTests.cs`
- New: `tests/Iris.Application.Tests/Memory/Queries/ListActiveMemoriesHandlerTests.cs`
- New: `tests/Iris.Application.Tests/Memory/Context/MemoryContextBuilderTests.cs`
- New: `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` (T-APP-PROMPT-01..03)

#### Files That Must Not Be Touched

- `src/Iris.Application/Abstractions/Models/*` (model gateway contracts).
- `src/Iris.Application/Persona/*` (language policy — unchanged).
- Out-of-scope placeholders in `Iris.Application.Memory.*` (Embeddings, Consolidation, Ranking, Audit, Extraction, BackgroundTasks).
- Anything in `Iris.Domain`, `Iris.Persistence`, `Iris.Desktop`.

#### Steps

1. **`MemoryOptions`** — POCO with defaults: `PromptInjectionTopN = 5`, `MaxListPageSize = 200`, `RetrieveDefaultLimit = 10`. Add `static MemoryOptions Default => new()`.
2. **`MemoryDto`** — record: `MemoryId Id`, `string Content`, `MemoryKind Kind`, `MemoryImportance Importance`, `MemoryStatus Status`, `DateTimeOffset CreatedAt`, `DateTimeOffset? UpdatedAt`.
3. **`IMemoryRepository`** — replace placeholder with the 5-method public interface.
4. **`RememberExplicitFactCommand`** — record: `string Content`, `MemoryKind? Kind`, `MemoryImportance? Importance`.
5. **`RememberMemoryResult`** — record: `MemoryDto Memory`.
6. **`RememberExplicitFactHandler`** — inject `IMemoryRepository`, `IUnitOfWork`, `IClock`, `MemoryOptions`. Validate, create, persist, commit.
7. **`ForgetMemoryCommand`** — record: `MemoryId Id`.
8. **`ForgetMemoryHandler`** — inject `IMemoryRepository`, `IUnitOfWork`, `IClock`. Get, forget (check `changed` flag to skip `UpdateAsync`), commit.
9. **`UpdateMemoryCommand`** — record: `MemoryId Id`, `string NewContent`.
10. **`UpdateMemoryResult`** — record: `MemoryDto Memory`.
11. **`UpdateMemoryHandler`** — inject `IMemoryRepository`, `IUnitOfWork`, `IClock`. Get, validate content, update, persist, commit.
12. **`RetrieveRelevantMemoriesQuery`** — record: `string Query`, `int? Limit`.
13. **`RetrieveRelevantMemoriesHandler`** — inject `IMemoryRepository`, `MemoryOptions`. Call `SearchActiveAsync` with effective limit.
14. **`ListActiveMemoriesQuery`** — record: `int? Limit`.
15. **`ListActiveMemoriesHandler`** — inject `IMemoryRepository`, `MemoryOptions`. Call `ListActiveAsync`.
16. **`MemoryPromptFormatter`** — pure static-like class; `string Format(IReadOnlyList<Memory> memories)` renders `Известные факты:\n- content1\n- content2\n...`.
17. **`MemoryContextBuilder`** — inject `IMemoryRepository`, `MemoryOptions`. Async `Task<IReadOnlyList<Memory>> SelectAsync(PromptBuildRequest request, CancellationToken ct)` → calls `ListActiveAsync(PromptInjectionTopN, ct)`.
18. **`PromptBuilder`** — extend constructor to `(ILanguagePolicy, MemoryContextBuilder, MemoryPromptFormatter)`. Update `Build` to accept `(PromptBuildRequest request, IReadOnlyList<Memory> memories)`. If `memories.Count > 0`, insert a second `System` message after the language message.
19. **`SendMessageHandler`** — add `MemoryContextBuilder _memoryContextBuilder` constructor parameter. In `HandleAsync`, after loading history, call `await _memoryContextBuilder.SelectAsync(...)` wrapped in try/catch (swallow exception, use empty list). Then call `_promptBuilder.Build(request, memories)`.
20. **`DependencyInjection`** — add `MemoryOptions memoryOptions` param (null-guard), register new services, change `PromptBuilder` from `AddSingleton` to `AddScoped`, add `AddSingleton(memoryOptions)`.
21. **Update `PromptBuilderTests`** — update 3 existing tests to use extended constructor (provide no-op stubs for `MemoryContextBuilder` and `MemoryPromptFormatter`) and call `Build(request, Array.Empty<Memory>())`.
22. **Update `DependencyInjectionTests`** — add `MemoryOptions.Default` as 3rd arg to all `AddIrisApplication` calls; add inner `FakeMemoryRepository` implementing `IMemoryRepository`; register it in DI-setup steps.
23. **Add new Application tests** — implement T-APP-MEM-01..24 and T-APP-PROMPT-01..03 across the new test files listed above.

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
```

Expected: 0 errors; all prior + new Application and Domain tests green. No persistence or Desktop tests yet.

#### Rollback

Revert all new files and all changes to `PromptBuilder.cs`, `SendMessageHandler.cs`, `DependencyInjection.cs`, `PromptBuilderTests.cs`, `DependencyInjectionTests.cs`. Restore `IMemoryRepository.cs` to empty placeholder. The Domain phase 1+2 work is unaffected.

---

### Phase 4 — Persistence: Entity, Configuration, Mapper, Repository, DbContext, Tests

#### Goal

Implement all Persistence memory components and add integration tests. The `memories` table will be created by `EnsureCreatedAsync` on fresh DBs.

#### Files to Inspect Before Editing

- `src/Iris.Persistence/Entities/MemoryEntity.cs` — confirm empty placeholder.
- `src/Iris.Persistence/Entities/MemoryEmbeddingEntity.cs` — confirm empty; do not touch (out-of-scope).
- `src/Iris.Persistence/Configurations/MemoryEntityConfiguration.cs` — confirm empty.
- `src/Iris.Persistence/Configurations/MemoryEmbeddingEntityConfiguration.cs` — confirm empty; do not touch.
- `src/Iris.Persistence/Mapping/MemoryMapper.cs` — confirm empty.
- `src/Iris.Persistence/Mapping/MemoryEmbeddingMapper.cs` — confirm empty; do not touch.
- `src/Iris.Persistence/Repositories/MemoryRepository.cs` — confirm empty.
- `src/Iris.Persistence/Database/IrisDbContext.cs` — current 2 DbSets.
- `src/Iris.Persistence/DependencyInjection.cs` — current registrations.
- `src/Iris.Persistence/Configurations/ConversationEntityConfiguration.cs` — pattern for timestamps/indexes.
- `src/Iris.Persistence/Mapping/ConversationMapper.cs` — pattern for mapper.
- `src/Iris.Persistence/Repositories/ConversationRepository.cs` — pattern for repository.
- `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs` — reuse as-is.
- `tests/Iris.IntegrationTests/Persistence/ConversationRepositoryTests.cs` — naming/fixture pattern.

#### Files Likely to Edit

- `src/Iris.Persistence/Entities/MemoryEntity.cs` — replace placeholder.
- `src/Iris.Persistence/Configurations/MemoryEntityConfiguration.cs` — replace placeholder.
- `src/Iris.Persistence/Mapping/MemoryMapper.cs` — replace placeholder.
- `src/Iris.Persistence/Repositories/MemoryRepository.cs` — replace placeholder.
- `src/Iris.Persistence/Database/IrisDbContext.cs` — add `Memories` DbSet.
- `src/Iris.Persistence/DependencyInjection.cs` — add `IMemoryRepository` → `MemoryRepository`.
- New: `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs`

#### Files That Must Not Be Touched

- `src/Iris.Persistence/Entities/MemoryEmbeddingEntity.cs` (out-of-scope placeholder).
- `src/Iris.Persistence/Configurations/MemoryEmbeddingEntityConfiguration.cs` (out-of-scope placeholder).
- `src/Iris.Persistence/Mapping/MemoryEmbeddingMapper.cs` (out-of-scope placeholder).
- Any file outside `Iris.Persistence` and `Iris.IntegrationTests`.

#### Steps

1. **`MemoryEntity`** — implement mutable EF entity: `Guid Id`, `string Content`, `int Kind`, `int Importance`, `int Status`, `int Source`, `long CreatedAt`, `long? UpdatedAt`.
2. **`MemoryEntityConfiguration`**:
   - `ToTable("memories")`.
   - PK `Id`, no value generation.
   - `Content` required, `.UseCollation("NOCASE")`.
   - `Kind`, `Importance`, `Status`, `Source` stored as `INTEGER`, all required.
   - `CreatedAt` as `long` with `_utcTicksConverter` (copy pattern from `ConversationEntityConfiguration`), column type `INTEGER`, required.
   - `UpdatedAt` as `long?` with `_utcTicksConverter`, column type `INTEGER`, nullable.
   - Two indexes: `HasIndex(m => m.Status)` and `HasIndex(m => m.UpdatedAt)`.
3. **`MemoryMapper`** — static class:
   - `ToEntity(Memory memory) → MemoryEntity` — maps all fields, `UpdatedAt` tick-converts nullable.
   - `ToDomain(MemoryEntity entity) → Memory` — calls `Memory.Rehydrate(...)`.
4. **`MemoryRepository`** — implement all 5 `IMemoryRepository` methods:
   - `GetByIdAsync` — `AsNoTracking().FirstOrDefaultAsync(...)`.
   - `AddAsync` — `_dbContext.Memories.AddAsync(entity, ct)`.
   - `UpdateAsync` — load tracked entity, map all fields from domain object.
   - `ListActiveAsync(limit, ct)` — `Where(m => m.Status == (int)MemoryStatus.Active).OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt).Take(limit).ToListAsync(ct)`.
   - `SearchActiveAsync(query, limit, ct)` — `Where(m => m.Status == (int)MemoryStatus.Active && EF.Functions.Like(m.Content, $"%{query}%")).OrderByDescending(...).Take(limit).ToListAsync(ct)`.
5. **`IrisDbContext`** — add `public DbSet<MemoryEntity> Memories => Set<MemoryEntity>();`.
6. **`DependencyInjection`** — add `services.AddScoped<IMemoryRepository, MemoryRepository>();`.
7. **`MemoryRepositoryTests`** — add integration tests T-PERS-MEM-01..05:
   - Use `PersistenceTestContextFactory` (existing, reuse without changes).
   - T-PERS-MEM-01: `AddAsync` + `GetByIdAsync` round-trip (all fields, enum values, timestamps).
   - T-PERS-MEM-02: `UpdateAsync` changes content/`UpdatedAt`/`Status`.
   - T-PERS-MEM-03: `ListActiveAsync` excludes Forgotten items.
   - T-PERS-MEM-04: `SearchActiveAsync` matches case-insensitively (test with lowercase query against mixed-case content, and Cyrillic characters).
   - T-PERS-MEM-05: confirm `IrisDbContext.Database.EnsureCreatedAsync()` creates the `memories` table (query `sqlite_master`).

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
```

Expected: 0 errors; all prior + new persistence integration tests green.

#### Rollback

Revert: `MemoryEntity.cs`, `MemoryEntityConfiguration.cs`, `MemoryMapper.cs`, `MemoryRepository.cs`, `IrisDbContext.cs`, `Persistence/DependencyInjection.cs`. Delete `MemoryRepositoryTests.cs`. The Domain and Application phases are unaffected.

---

### Phase 5 — Desktop: Facade, ViewModel, View, Navigation

#### Goal

Wire memory into the Desktop layer. Extend `IIrisApplicationFacade`, implement `MemoryViewModel`, populate `MemoryView` and `MemoryCard`, add a "Память" tab to `MainWindow`, register `MemoryViewModel` in DI.

**Important:** `FakeIrisApplicationFacade` in `Iris.IntegrationTests` must be updated to implement the new facade methods or the integration tests will fail to compile.

#### Files to Inspect Before Editing

- `src/Iris.Desktop/Services/IIrisApplicationFacade.cs` — current 1-method interface.
- `src/Iris.Desktop/Services/IrisApplicationFacade.cs` — current implementation.
- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` — confirm empty.
- `src/Iris.Desktop/Models/MemoryViewModelItem.cs` — confirm empty.
- `src/Iris.Desktop/Views/MemoryView.axaml` — confirm `<Grid />` stub.
- `src/Iris.Desktop/Views/MemoryView.axaml.cs` — confirm empty code-behind.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml` — confirm empty.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml.cs` — confirm empty.
- `src/Iris.Desktop/DependencyInjection.cs` — current registrations and pattern.
- `src/Iris.Desktop/Views/MainWindow.axaml` — current layout to understand where to add the tab.
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs` — confirm relationship to nav.
- `src/Iris.Desktop/ViewModels/ChatViewModel.cs` — understand existing ViewModel pattern.
- `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs` — must be updated.
- `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs` — check if it will be affected.

#### Files Likely to Edit

- `src/Iris.Desktop/Services/IIrisApplicationFacade.cs` — add 4 memory methods.
- `src/Iris.Desktop/Services/IrisApplicationFacade.cs` — implement 4 memory methods.
- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` — replace placeholder.
- `src/Iris.Desktop/Models/MemoryViewModelItem.cs` — replace placeholder.
- `src/Iris.Desktop/Views/MemoryView.axaml` — implement memory list UI.
- `src/Iris.Desktop/Views/MemoryView.axaml.cs` — bind DataContext to `MemoryViewModel`.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml` — implement card UI.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml.cs` — code-behind if needed.
- `src/Iris.Desktop/DependencyInjection.cs` — register `MemoryViewModel` as Singleton; pass `MemoryOptions` to `AddIrisApplication`.
- `src/Iris.Desktop/Views/MainWindow.axaml` — add `TabControl` with "Чат" and "Память" tabs.
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs` — expose `MemoryViewModel` if needed by navigation.
- `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs` — add no-op implementations for all 4 new facade methods.

#### Files That Must Not Be Touched

- Anything in `Iris.Domain`, `Iris.Application`, `Iris.Persistence`.
- `src/Iris.Desktop/ViewModels/ChatViewModel.cs` (do not regress existing chat).
- `src/Iris.Desktop/ViewModels/AvatarViewModel.cs`.
- `src/Iris.Desktop/Services/ILanguagePolicy.cs` and language-related files.

#### Steps

1. **`IIrisApplicationFacade`** — add 4 new method signatures per design §7.
2. **`IrisApplicationFacade`** — implement each new method with the same scope-per-call pattern: open scope, resolve handler, call `HandleAsync`, return `Result<...>`.
3. **`FakeIrisApplicationFacade`** (integration tests) — add no-op implementations for all 4 new methods returning empty/success results. This is required to keep integration tests compiling.
4. **`MemoryViewModelItem`** — replace empty class with a record/class exposing `MemoryId Id`, `string Content`, `string KindLabel`, `string ImportanceLabel`, `DateTimeOffset CreatedAt`, `DateTimeOffset? UpdatedAt`.
5. **`MemoryViewModel`** — replace empty class with a proper ViewModel:
   - Constructor: `(IIrisApplicationFacade facade)`.
   - Properties: `ObservableCollection<MemoryViewModelItem> Memories`, `bool IsLoading`, `string? ErrorMessage`, `string NewMemoryContent`.
   - Async commands/methods: `LoadMemoriesAsync(CancellationToken)`, `RememberAsync(CancellationToken)`, `ForgetAsync(MemoryId, CancellationToken)`.
   - After `RememberAsync` or `ForgetAsync`, call `LoadMemoriesAsync` to refresh.
6. **`MemoryCard.axaml`** — simple card: shows `Content`, `KindLabel`, `ImportanceLabel`, `CreatedAt`; "Забыть" button bound to a command passing the `MemoryId`.
7. **`MemoryView.axaml`** — `StackPanel` at top with `TextBox` (bound to `NewMemoryContent`) + "Запомнить" button; below, `ItemsControl` over `Memories` using `MemoryCard` as item template; empty-state `TextBlock`.
8. **`MemoryView.axaml.cs`** — set `DataContext` from DI (resolve `MemoryViewModel`) or bind in XAML through the existing DI-injection pattern used by `ChatView`.
9. **`MainWindow.axaml`** — wrap existing content in a `TabControl`. Tab 1: "Чат" with current `ChatView`. Tab 2: "Память" with `MemoryView`.
10. **`MainWindowViewModel`** — expose `MemoryViewModel` property if `MainWindow` needs to bind it.
11. **Desktop `DependencyInjection`** — read optional `Application:Memory:*` config keys with defaults; construct `MemoryOptions`; pass to `AddIrisApplication`; register `services.AddSingleton<MemoryViewModel>()`.

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
```

Expected: 0 errors; all prior + new memory tests green. Manual smoke deferred to Phase 7.

#### Rollback

Revert all Desktop file changes. Revert `FakeIrisApplicationFacade` to its current state. The Domain, Application, and Persistence phases are unaffected.

---

### Phase 6 — Architecture Tests

#### Goal

Add the 4 new architecture boundary tests for memory (T-ARCH-MEM-01..04).

#### Files to Inspect Before Editing

- `tests/Iris.Architecture.Tests/DependencyDirectionTests.cs` — existing dependency tests pattern.
- `tests/Iris.Architecture.Tests/ForbiddenNamespaceTests.cs` — existing forbidden namespace test pattern.
- `tests/Iris.Architecture.Tests/ProjectReferenceTests.cs` — existing project reference test pattern.

#### Files Likely to Edit

- `tests/Iris.Architecture.Tests/DependencyDirectionTests.cs` — or a new `MemoryBoundaryTests.cs`.

#### Files That Must Not Be Touched

- Any source, persistence, application, or desktop file.

#### Steps

Add (either in existing files or a new `MemoryBoundaryTests.cs`):

- **T-ARCH-MEM-01** — Application assembly does not reference `Iris.Persistence` assembly (already covered by existing test; confirm it still passes; no new code needed if it does).
- **T-ARCH-MEM-02** — Domain assembly does not reference `Microsoft.EntityFrameworkCore` — check `_domainAssembly.GetReferencedAssemblies()` for any EF reference.
- **T-ARCH-MEM-03** — Types in `Iris.Desktop.ViewModels` do not reference `Iris.Application.Abstractions.Persistence.IMemoryRepository` or `Iris.Persistence.Database.IrisDbContext` — verify via `Assembly.GetTypes()` + `GetConstructors()` or use the existing project reference test pattern.
- **T-ARCH-MEM-04** — Types in `Iris.Application.Memory` namespace do not reference `Iris.Persistence.Entities.MemoryEntity` — verify `_applicationAssembly.GetTypes()` from the memory namespace.

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
```

Expected: 0 errors; all tests green including new architecture tests.

#### Rollback

Delete `MemoryBoundaryTests.cs` or revert additions to existing architecture test files.

---

### Phase 7 — Final Verification and Memory Files

#### Goal

Run the full verification suite, confirm all acceptance criteria are mechanically met, record results in agent memory, and note manual smoke tests to run.

#### Files to Inspect Before Editing

- `git status --short` — confirm only intended files are dirty.
- `git diff --stat` — confirm scope of changes.

#### Files Likely to Edit

- `.agent/PROJECT_LOG.md` — append Phase 8 entry.
- `.agent/overview.md` — update Current Phase, Working Status, Next Step.
- `.agent/log_notes.md` — record any anomalies.

#### Files That Must Not Be Touched

- Any source file.
- Test files (no further test changes).
- `mem_library/` (no rewrite of roadmap).

#### Steps

1. Run full verification commands (see §8).
2. Confirm all acceptance criteria from spec §13 are met:
   - [ ] `Memory` aggregate in `Iris.Domain.Memories`.
   - [ ] 5 handlers in `Iris.Application.Memory.*`.
   - [ ] `MemoryContextBuilder` consumed by `PromptBuilder` / `SendMessageHandler`.
   - [ ] `IMemoryRepository` is real, public, non-empty.
   - [ ] `MemoryRepository` in Persistence with migration-less schema bootstrap.
   - [ ] `MemoryView` shows active memories and supports Forget.
   - [ ] Architecture tests T-ARCH-MEM-01..04 pass.
   - [ ] Build 0/0.
   - [ ] All tests green (155 baseline + new).
   - [ ] Format exit 0.
   - [ ] No new project / package / IVT.
   - [ ] Out-of-scope placeholders untouched.
3. Update `.agent/PROJECT_LOG.md` — append Phase 8 entry with files changed, test count, validation status.
4. Update `.agent/overview.md` — Current Phase = "Phase 8 Memory v1 complete", Next Step = "Phase 9 Persona v1 / Context v1 / Modes v1".
5. Record manual smoke M-MEM-01..05 as pending in `.agent/log_notes.md`.

#### Manual Smoke (Required Before Merge Claim)

M-MEM-01: Launch Desktop, create a new memory via "Запомнить" button with text "Мой любимый язык — C#.".
M-MEM-02: Open "Память" tab; verify the memory is listed with correct content and timestamp.
M-MEM-03: Send a chat message "Какой мой любимый язык?"; verify response references C# (non-deterministic, but memory should be injected into prompt — confirm via debug or PromptBuilder log).
M-MEM-04: Click "Забыть" on the memory; verify it disappears from the list; send another message and verify memory no longer injected.
M-MEM-05: Close and relaunch the application; verify non-forgotten memories persist.

#### Verification

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
dotnet format .\Iris.slnx --verify-no-changes --verbosity minimal
```

#### Rollback

Revert memory file updates only. Source changes from prior phases are not rolled back here.

---

## 6. Testing Plan

### Unit Tests

**`Iris.Domain.Tests` — new files:**
- `tests/Iris.Domain.Tests/Memories/MemoryTests.cs` — T-DOM-MEM-01, 05, 06, 07, 08.
- `tests/Iris.Domain.Tests/Memories/MemoryContentTests.cs` — T-DOM-MEM-02, 03, 04.
- `tests/Iris.Domain.Tests/Memories/MemoryIdTests.cs` — T-DOM-MEM-09.

**`Iris.Application.Tests` — new files:**
- `tests/Iris.Application.Tests/Memory/Commands/RememberExplicitFactHandlerTests.cs` — T-APP-MEM-01..05.
- `tests/Iris.Application.Tests/Memory/Commands/ForgetMemoryHandlerTests.cs` — T-APP-MEM-06..09.
- `tests/Iris.Application.Tests/Memory/Commands/UpdateMemoryHandlerTests.cs` — T-APP-MEM-10..13.
- `tests/Iris.Application.Tests/Memory/Queries/RetrieveRelevantMemoriesHandlerTests.cs` — T-APP-MEM-14..17.
- `tests/Iris.Application.Tests/Memory/Queries/ListActiveMemoriesHandlerTests.cs` — T-APP-MEM-18..20.
- `tests/Iris.Application.Tests/Memory/Context/MemoryContextBuilderTests.cs` — T-APP-MEM-21..24.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` — T-APP-PROMPT-01..03.

**`Iris.Application.Tests` — updated files:**
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` — update constructor calls + `Build` signature.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` — add `MemoryOptions.Default` arg, add `FakeMemoryRepository`.

Application test fakes needed (defined inline or in a `Testing/` folder):
- `FakeMemoryRepository` — `Dictionary<MemoryId, Memory>`-backed in-memory implementation of `IMemoryRepository`.
- Reuse existing `FakeUnitOfWork` and `FakeClock`.

### Integration Tests

**`Iris.IntegrationTests` — new file:**
- `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` — T-PERS-MEM-01..05.

**`Iris.IntegrationTests` — updated file:**
- `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs` — add 4 no-op method implementations.

### Architecture Tests

**`Iris.Architecture.Tests` — new or extended:**
- T-ARCH-MEM-01..04 (T-ARCH-MEM-01 may already pass from existing test; verify).

### Regression Tests

- All 155 existing tests must remain green throughout every phase.
- `PromptBuilderTests` 3 existing tests must be adapted (not deleted).
- `DependencyInjectionTests` 5 existing tests must be adapted.
- Integration tests using `FakeIrisApplicationFacade` must compile after facade extension.

### Manual Verification

M-MEM-01..05 — executed in Phase 7 against live Desktop + Ollama. Results recorded in `.agent/log_notes.md`.

---

## 7. Documentation and Memory Plan

### Documentation Updates

- No changes to `docs/specs/` or `docs/designs/` (already saved).
- No changes to `docs/architecture.md` in this plan phase (DOC-004 in spec says "if appropriate" — the builder may add a brief `IrisDbContext.Memories` note during Phase 7).
- Roadmap `mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md` — do not rewrite; a one-line "Phase 8 v1 delivered" note may be appended near §6 Phase 8 in Phase 7.

### Agent Memory Updates

After Phase 7 mechanical verification passes:
- **`.agent/PROJECT_LOG.md`** — append Phase 8 entry (phase name, files changed, test count, validation status, remaining v2 work).
- **`.agent/overview.md`** — update Current Phase, Working Status, Next Step.
- **`.agent/log_notes.md`** — record manual smoke M-MEM-01..05 status and any anomalies (R-001 `EnsureCreated` note, R-002 silent-swallow note).

Memory updates are performed in Phase 7 only, after mechanical verification. They must not be written during any source-code phase.

---

## 8. Verification Commands

```powershell
# Build (run after each phase)
dotnet build .\Iris.slnx --nologo --verbosity minimal

# Full test suite (run after each phase)
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo

# Format check (run in Phase 7 only, read-only)
dotnet format .\Iris.slnx --verify-no-changes --verbosity minimal

# Architecture tests alone (optional narrow check in Phase 6)
dotnet test .\tests\Iris.Architecture.Tests\Iris.Architecture.Tests.csproj --no-build --verbosity minimal --nologo

# Domain tests alone (after Phase 2)
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj --no-build --verbosity minimal --nologo

# Application tests alone (after Phase 3)
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --verbosity minimal --nologo

# Integration tests alone (after Phase 4)
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-build --verbosity minimal --nologo
```

---

## 9. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| R-001: `EnsureCreatedAsync` does not add `memories` table to existing `iris.db` | Medium — first memory write on existing DB fails | Documented; developer deletes `%APPDATA%\Iris\iris.db`; acceptable v1 behavior |
| R-002: `PromptBuilder` Singleton→Scoped breaks test that constructs it from singleton container | Low | `PromptBuilderTests` construct directly (unaffected); `DependencyInjectionTests` must register `MemoryContextBuilder` + `FakeMemoryRepository` or use a scope |
| R-003: `FakeIrisApplicationFacade` fails to compile after interface extension | High if not addressed in Phase 5 | Explicitly scheduled in Phase 5 step 3 |
| R-004: `.UseCollation("NOCASE")` not supported by current EF Core SQLite version | Low | Fallback: use `EF.Functions.Like(...)` without collation (ASCII case-insensitive); Russian case sensitivity is acceptable v1 limitation |
| R-005: `MemoryContextBuilder.SelectAsync` exception silently swallowed masks a real bug | Low | Architecturally acceptable for v1; recorded as design trade-off R-002 in design doc |
| R-006: `MainWindow.axaml` `TabControl` wrapping breaks existing snapshot or layout tests | Low | No snapshot tests for Desktop exist; visual regression is manual smoke only |
| R-007: Out-of-scope empty placeholders accidentally edited | Medium | Explicitly listed in forbidden-edits for each phase; builder verifies `git diff` scope |

---

## 10. Implementation Handoff Notes

**Critical for the implementation agent:**

1. **Phase order is mandatory.** Do not implement Application or Persistence before Domain is complete and building. Do not implement Desktop before Application DI passes tests.

2. **`PromptBuilder` tests use direct construction.** `PromptBuilderTests` constructs `new PromptBuilder(StubLanguagePolicy)`. After Phase 3, this will become `new PromptBuilder(stubLanguagePolicy, stubMemoryContextBuilder, memoryPromptFormatter)`. The builder must supply stubs that return an empty list. These stubs can be anonymous inner classes in the test file.

3. **`AddIrisApplication` signature change breaks `DependencyInjectionTests`.** All 5 tests call `AddIrisApplication(options, languageOptions)` — must add `MemoryOptions.Default` as 3rd arg AND register `FakeMemoryRepository` to satisfy the `IMemoryRepository` dependency.

4. **`FakeIrisApplicationFacade` must be updated in Phase 5 before the build can succeed.** The interface change breaks it immediately. New methods should return `Task.FromResult(Result<X>.Success(...))` with minimal/empty values.

5. **Do not use `EF.Functions.Like` without `COLLATE NOCASE`.** Set `UseCollation("NOCASE")` on the `Content` column in `MemoryEntityConfiguration`. If this fails, fall back to `EF.Functions.Like(m.Content, $"%{query}%")` without collation.

6. **`Memory.Forget` returns `bool`.** The handler must check this return value and skip `UpdateAsync` + `CommitAsync` when the result is `false` (already-Forgotten, idempotent path). This is critical for FR-007 correctness and avoiding a spurious DB write.

7. **Memory.Rehydrate is `internal static`.** The mapper must use it. No public back-door exists. If `internal` visibility between `Iris.Domain` and `Iris.Persistence` is required, use `InternalsVisibleTo` in the Domain project for the Persistence project — BUT this requires `InternalsVisibleTo` which spec says is forbidden between production projects. **Resolution:** make `Memory.Rehydrate` `public static` and document it as "rehydration-only" in an XML doc comment. This is the simpler compliant approach.

8. **No `[Fact]`-less test files.** Every new test file must contain at least one `[Fact]`.

9. **`git status --short` before Phase 1.** If tree is dirty with anything beyond the two doc files, stop and resolve.

10. **Expected final test count:** 155 (existing) + 9 (Domain) + 28 (Application memory) + 3 (Prompt-with-memory) + 5 (Persistence integration) + 4 (Architecture) = approximately **204 tests**.

---

## 11. Open Questions

No blocking open questions. All design decisions are resolved in `docs/designs/2026-05-02-phase-8-memory-v1.design.md`.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

1. Working tree is clean (or only contains untracked doc files) at plan execution time.
2. `Memory.Rehydrate` is made `public static` (not `internal`) to avoid `InternalsVisibleTo` between production projects — acceptable because it is documented as a mapper-only factory.
3. `PromptBuilder.Build` signature extension is a new overload `Build(PromptBuildRequest, IReadOnlyList<Memory>)`, not a replacement. The original `Build(PromptBuildRequest)` may be removed if no tests use it, or kept as a convenience delegating to the new overload with an empty list.
4. `FakeMemoryRepository` for Application tests is an inner class in `DependencyInjectionTests` or a shared test helper — whichever matches the project's existing test helper style.
5. Avalonia XAML `TabControl` is available in the project's current Avalonia version (confirmed via existing `ChatView`/`MainWindow` usage).
6. The `Iris.IntegrationTests` project's namespace is `Iris.Integration.Tests` (confirmed in `PersistenceTestContextFactory.cs`).

## Blocking Questions

No blocking questions.

---

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `docs/specs/2026-05-02-phase-8-memory-v1.spec.md` |
| B — Design | ✅ Satisfied | `docs/designs/2026-05-02-phase-8-memory-v1.design.md` |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |