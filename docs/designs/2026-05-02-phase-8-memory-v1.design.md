# Design: Phase 8 — Memory v1 (Explicit Memory)

**Active stage:** `/design`
**Spec:** `docs/specs/2026-05-02-phase-8-memory-v1.spec.md`

## 1. Design Goal

Deliver Memory v1 as an additive vertical slice that:
- adds the `Memory` aggregate to Domain,
- adds explicit-memory use cases and a `MemoryContextBuilder` to Application,
- adds an EF Core adapter to Persistence,
- exposes Desktop UI through the existing facade pattern,
- preserves all existing chat behavior byte-equivalent when no memories are relevant.

The design is structured so each later v2 capability (embeddings, consolidation, ranking, audit, extraction, API surface) plugs into the same boundaries without rework.

## 2. Specification Traceability

| Spec ref | Design address |
|---|---|
| FR-001..005 (Remember) | `RememberExplicitFactHandler` + `Memory.Create` + `IMemoryRepository.AddAsync` + `IUnitOfWork.CommitAsync`. |
| FR-006..008 (Forget) | `ForgetMemoryHandler` + `Memory.Forget` (idempotent on `Forgotten`) + `IMemoryRepository.GetByIdAsync` + `UpdateAsync`. |
| FR-009..011 (Update) | `UpdateMemoryHandler` + `Memory.UpdateContent` (rejects on `Forgotten`). |
| FR-012..015 (Retrieve) | `RetrieveRelevantMemoriesHandler` + `IMemoryRepository.SearchActiveAsync(query, limit, ct)`. |
| FR-016 (List) | `ListActiveMemoriesHandler` + `IMemoryRepository.ListActiveAsync(limit, ct)`. |
| FR-017..020 (Prompt injection) | `MemoryContextBuilder` + `PromptBuilder` adds an additional `system` role message labelled in Russian when result non-empty; byte-equivalent path when empty. |
| FR-021..025 (Desktop UI) | `MemoryView/ViewModel/Card` + facade extension. |
| FR-026..028 (Cross-cutting) | Russian default unchanged; chat send unchanged when no memories; no background task activated. |
| AC-001..014 | See §11 (DI), §3 (current architecture), §13 (testing), §6 (component design), §10 (failure handling). |
| D-001..009 | §9 Data and State Design. |
| Open Q1 | §7 contract design (separate methods). |
| Open Q2 | §6/§8 (separate `system` message). |
| Open Q3 | §6 (label = `Известные факты:`). |
| Open Q4 | §6 (top-N most-recent). |
| Open Q5 | §6 (explicit Desktop affordance: text-box + button). |
| Open Q6 | §6 (folder layout chosen). |
| Open Q7 | §9 (indexing strategy). |

## 3. Current Architecture Context

```text
Iris.Domain          (no project deps)
Iris.Shared          (no project deps)
Iris.Application     -> Domain, Shared
Iris.Persistence     -> Application, Domain, Shared
Iris.ModelGateway    -> Application, Domain, Shared
Iris.Desktop (host)  -> Application, Domain, ModelGateway, Persistence, Shared
```

Key facts:
- The Application layer owns ports under `Iris.Application.Abstractions.Persistence.*`. Persistence implements them.
- `IrisApplicationFacade` lives in **Desktop**, not Application. It uses an injected `IServiceScopeFactory` to open a scope per call and resolve scoped Application handlers. v1 memory must extend this same facade.
- `IrisDbContext` has 2 DbSets (`Conversations`, `Messages`). Schema is created by `EnsureCreatedAsync`.
- All dirty file scaffolds (`MemoryEntity`, `MemoryRepository`, etc.) are 10-line empty placeholders from `55ed9f8` and will be replaced in-place during `/implement`. Out-of-scope placeholders (Embeddings, Consolidation, Ranking, Audit, Extraction, Api) stay empty per spec AC-013.

## 4. Proposed Design Summary

Memory v1 is an additive, single-layer-per-responsibility slice:

```text
Domain:        Memory aggregate + value objects + enums
Application:   IMemoryRepository port
               5 handlers (Remember, Forget, Update, Retrieve, ListActive)
               MemoryContextBuilder (used by PromptBuilder)
               Memory* DTOs and Commands
               MemoryOptions (POCO, like SendMessageOptions)
Persistence:   MemoryEntity + EntityConfiguration + Mapper + Repository
               IrisDbContext.Memories DbSet
               No new migrations; EnsureCreated handles new table
Desktop:       IIrisApplicationFacade gains memory methods
               IrisApplicationFacade resolves new handlers per scope
               MemoryViewModel (Singleton) loads from facade
               MemoryView lists active memories with Forget action
               Navigation entry from MainWindow (TabControl or button)
Tests:         Domain, Application, Persistence integration, Architecture
```

Russian-default invariant: when `MemoryContextBuilder` returns empty, `PromptBuilder` emits exactly the same single `system` message it does today (FR-019). When non-empty, it emits the same first system message **plus a second `system` message** containing a Russian-labelled list of memories. This is the safest representation because it preserves the existing byte-level prompt unchanged in the empty case and lets us test the memory block independently.

## 5. Responsibility Ownership

| Responsibility | Owner layer | File area |
|---|---|---|
| `Memory` invariants (content limits, lifecycle) | Domain | `Iris.Domain.Memories` |
| `MemoryId`, `MemoryContent`, enums | Domain | `Iris.Domain.Memories` |
| `IMemoryRepository` port | Application | `Iris.Application.Abstractions.Persistence` |
| Use cases (Remember/Forget/Update/Retrieve/ListActive) | Application | `Iris.Application.Memory.*` |
| Memory selection for prompt | Application | `Iris.Application.Memory.Context.MemoryContextBuilder` |
| Memory block formatting in prompt | Application | `Iris.Application.Memory.Context.MemoryPromptFormatter` |
| Prompt assembly | Application | `Iris.Application.Chat.Prompting.PromptBuilder` (extended) |
| Memory persistence schema | Persistence | `Iris.Persistence.Configurations.MemoryEntityConfiguration` |
| Memory repository EF Core | Persistence | `Iris.Persistence.Repositories.MemoryRepository` |
| Domain↔Entity mapping | Persistence | `Iris.Persistence.Mapping.MemoryMapper` |
| DbSet exposure | Persistence | `IrisDbContext` (extended) |
| DI for memory port + handlers | Application + Persistence | respective `DependencyInjection` |
| Facade scope orchestration for memory | Desktop (host) | `Iris.Desktop.Services.IrisApplicationFacade` |
| Memory list UI / Forget action | Desktop | `Iris.Desktop.ViewModels.MemoryViewModel`, `Views/MemoryView` |
| Navigation between Chat and Memory views | Desktop | `Iris.Desktop.Views.MainWindow` |
| Architecture invariants | Architecture tests | `tests/Iris.Architecture.Tests` |

Forbidden ownership:
- `MemoryViewModel` must **not** receive `IMemoryRepository` or `IrisDbContext`.
- `PromptBuilder` must **not** call `IMemoryRepository`. Only `MemoryContextBuilder` does.
- `Iris.Persistence` must **not** call `MemoryContextBuilder` or any handler.
- `MemoryEntity` must **not** leak to Application or Domain.
- `Iris.Shared` must **not** gain memory types.

## 6. Component Design

### `Memory` (Domain aggregate)

- Owner: `Iris.Domain.Memories`.
- Responsibility: enforce content invariants and lifecycle (Active ↔ Forgotten, content updates).
- Inputs: `MemoryContent`, `MemoryKind`, `MemoryImportance`, `MemorySource`, creation timestamp.
- Outputs: immutable read of `Id`, `Content`, `Kind`, `Importance`, `Status`, `Source`, `CreatedAt`, `UpdatedAt`.
- Collaborators: `MemoryContent` (value object), `MemoryId` (value object), `DomainException`, enums.
- Must not do: depend on EF Core, `IClock`, `Result<T>`, or anything outside Domain. Time is passed in as `DateTimeOffset`. `IClock` lives in handlers.
- API shape (illustrative only):
  - `static Memory Create(MemoryId id, MemoryContent content, MemoryKind kind, MemoryImportance importance, MemorySource source, DateTimeOffset createdAt)`
  - `void Forget(DateTimeOffset now)` — idempotent: if already `Forgotten`, no-op.
  - `void UpdateContent(MemoryContent newContent, DateTimeOffset now)` — throws `DomainException("memory.not_active", ...)` when status is `Forgotten`.
- Lifecycle rules:
  - `Forget` on already-`Forgotten` is a successful no-op (FR-007).
  - `UpdateContent` on `Forgotten` throws `DomainException` — handler maps it to `Result.Failure(Error.Conflict("memory.not_active", ...))` (matches existing chat-slice idiom of catch + translate).
  - `UpdatedAt` is set on first `UpdateContent` or `Forget`; `null` until then.

### `MemoryContent` (value object)

- Owner: `Iris.Domain.Memories`.
- Responsibility: enforce non-empty and max-length content rules.
- Constants: `MaxLength = 4000` (Domain const, FR-003).
- API: `static MemoryContent Create(string value)` throws `DomainException("memory.empty_content", ...)` for empty/whitespace and `DomainException("memory.content_too_long", ...)` for >4000 chars.
- Mirrors `MessageContent` pattern.

### `MemoryId`, `MemoryKind`, `MemoryImportance`, `MemoryStatus`, `MemorySource` (Domain)

- `MemoryId` — `sealed record` wrapping `Guid`, factories `New()` / `From(Guid)`. Mirrors `ConversationId`.
- Enums:
  - `MemoryStatus { Active = 0, Forgotten = 1 }` — only two values used by v1 (D-002). v2 may add more later.
  - `MemoryKind { Fact = 0, Preference = 1, Note = 2 }` — minimum from D-003.
  - `MemoryImportance { Low = 0, Normal = 1, High = 2 }` — minimum from D-004. Persisted but unused by v1 ranking.
  - `MemorySource { UserExplicit = 0 }` — minimum from D-005. Future sources are not enumerated yet (avoids leaking Phase 9+ scope into Domain).

### `IMemoryRepository` (Application port)

- Owner: `Iris.Application.Abstractions.Persistence`.
- Consumers: handlers + `MemoryContextBuilder`.
- Implementation: `Iris.Persistence.Repositories.MemoryRepository` (Persistence).
- Async, no `Result<T>` at port level (matches `IConversationRepository`).
- Shape (illustrative):
  ```csharp
  public interface IMemoryRepository
  {
      Task<Memory?> GetByIdAsync(MemoryId id, CancellationToken ct);
      Task AddAsync(Memory memory, CancellationToken ct);
      Task UpdateAsync(Memory memory, CancellationToken ct);
      Task<IReadOnlyList<Memory>> ListActiveAsync(int limit, CancellationToken ct);
      Task<IReadOnlyList<Memory>> SearchActiveAsync(string query, int limit, CancellationToken ct);
  }
  ```
- Compatibility: new contract; only memory handlers and `MemoryContextBuilder` consume it.
- Error behavior: persistence exceptions propagate; handlers catch them and translate to `Result.Failure(...)`.

### Handlers (Application use cases)

All handlers live under `Iris.Application.Memory.*` with the layout:

```text
Iris.Application/Memory/
  Commands/
    RememberExplicitFactCommand.cs
    RememberExplicitFactHandler.cs
    ForgetMemoryCommand.cs
    ForgetMemoryHandler.cs
    UpdateMemoryCommand.cs
    UpdateMemoryHandler.cs
  Queries/
    RetrieveRelevantMemoriesQuery.cs
    RetrieveRelevantMemoriesHandler.cs
    ListActiveMemoriesQuery.cs
    ListActiveMemoriesHandler.cs
  Context/
    MemoryContextBuilder.cs
    MemoryPromptFormatter.cs
  Contracts/
    MemoryDto.cs
  Options/
    MemoryOptions.cs
  Validation/
    MemoryContentValidator.cs    // optional, can also live in Domain via MemoryContent.Create
```

Each handler:
- receives `IMemoryRepository`, `IUnitOfWork`, `IClock`, `MemoryOptions` (constructor injection),
- returns `Task<Result<T>>` with the same idiom as `SendMessageHandler`,
- catches `Exception` (excluding `OperationCanceledException`) on persistence calls and maps to `Result.Failure(Error.Failure("memory.persistence_failed", ...))`.

#### `RememberExplicitFactHandler`

- Validates content via `MemoryContent.Create` (Domain raises `DomainException`; handler catches and converts to `Error.Validation`).
- Generates `MemoryId.New()`.
- Defaults: `Kind = Note`, `Importance = Normal`, `Source = UserExplicit` if not supplied.
- Persists via `IMemoryRepository.AddAsync` then `IUnitOfWork.CommitAsync`.
- Returns `Result<RememberMemoryResult>` with the new `MemoryDto`.

#### `ForgetMemoryHandler`

- Loads via `GetByIdAsync`. If `null` → `Error.NotFound("memory.not_found", ...)`.
- Calls `Memory.Forget(now)` (idempotent on already-Forgotten — successful path even if already forgotten, FR-007).
- `UpdateAsync` then `CommitAsync`.

#### `UpdateMemoryHandler`

- Loads. Not found → `Error.NotFound`.
- Validates new content (Domain rules).
- If status is `Forgotten`, returns `Error.Conflict("memory.not_active", ...)` (matches D-002 / FR-010).
- Otherwise calls `Memory.UpdateContent(newContent, now)` and persists.

#### `RetrieveRelevantMemoriesHandler`

- Inputs: `query` (string), `limit` (int? default 10).
- Calls `IMemoryRepository.SearchActiveAsync(query, effectiveLimit, ct)`.
- Returns ordered list (most-recent first) limited to `effectiveLimit`. Repository is responsible for ordering and case-insensitive substring (D-007 + FR-014).

#### `ListActiveMemoriesHandler`

- Inputs: optional `limit` (default = `MemoryOptions.MaxListPageSize`, default value 200).
- Calls `IMemoryRepository.ListActiveAsync(limit, ct)`.
- Returns DTO list.

#### `MemoryContextBuilder`

- Owner: `Iris.Application.Memory.Context`.
- Responsibility: select memories that should be injected into the next prompt.
- Algorithm v1 (Open Q4 resolved → top-N most-recent):
  1. Read `MemoryOptions.PromptInjectionTopN` (default 5).
  2. Call `IMemoryRepository.ListActiveAsync(topN, ct)`.
  3. Return list (already most-recent-first per repository contract).
- Inputs: `PromptBuildRequest` (passed for future content-aware ranking; v1 ignores its body, but accepting it future-proofs the seam).
- Output: `IReadOnlyList<Memory>`.
- Failure mode: throws — caller (`PromptBuilder`) wraps and degrades gracefully (see §10).

#### `MemoryPromptFormatter`

- Owner: `Iris.Application.Memory.Context`.
- Responsibility: convert `IReadOnlyList<Memory>` into a Russian-labelled system-prompt text block.
- Format (Open Q3 resolved):
  ```text
  Известные факты:
  - <content 1>
  - <content 2>
  ...
  ```
- Pure function; no I/O, no DI dependencies beyond `MemoryOptions` if formatting becomes configurable. Returns `string`.

#### `PromptBuilder` (extended)

- New constructor: `PromptBuilder(ILanguagePolicy languagePolicy, MemoryContextBuilder memoryContextBuilder, MemoryPromptFormatter memoryPromptFormatter)`.
- Build flow:
  1. Compose first `system` message from `_languagePolicy.GetSystemPrompt()` (unchanged).
  2. Call `_memoryContextBuilder.SelectAsync(...)`. **Sync wrapper acceptable** since builder can be sync; design choice: builder is async because repository is async.
  3. If returned list is non-empty, append a second `ChatModelMessage(System, formattedBlock)` after the language system message and **before** history.
  4. Append history. Append current user message. Build `ChatModelRequest`.
- FR-019 byte-equivalence: in the empty-list path, the produced `ChatModelRequest.Messages` is identical to today's. Tests verify by snapshot of message count + role + content.
- Builder must catch exceptions from `MemoryContextBuilder` and **degrade gracefully** (FR-027 + §10): log via `Microsoft.Extensions.Logging.ILogger<PromptBuilder>` (already part of `Microsoft.Extensions.Hosting` package set used in DI) and continue without memory block. **Note**: `PromptBuilder` is currently injected without `ILogger`; to avoid expanding the package surface here, the simpler v1 choice is: catch and swallow silently. Trade-off discussed in §15.

**Sync vs async PromptBuilder.Build**: today `Build` is sync (`Result<PromptBuildResult>`). Async memory selection forces either making `Build` async **or** doing memory selection eagerly in the handler before `Build`. The cleaner option is to keep `Build` sync and let `SendMessageHandler` resolve memories first (one extra await, no signature change for value objects). This is the chosen approach in §8.

### Application DI (`Iris.Application.DependencyInjection`)

Register:
- `MemoryOptions` (singleton, like `SendMessageOptions`/`LanguageOptions`).
- `MemoryContextBuilder` (scoped — depends on scoped `IMemoryRepository`).
- `MemoryPromptFormatter` (singleton — pure).
- 5 handlers (scoped, like `SendMessageHandler`).
- `PromptBuilder` lifetime upgrades from singleton to scoped (because it now consumes scoped `MemoryContextBuilder`). **Trade-off**: minor runtime cost; no behavior risk (it has no per-instance state today). Alternative discussed in §14.

Signature change to `AddIrisApplication`:
- Adds `MemoryOptions memoryOptions` parameter (mirrors `SendMessageOptions`/`LanguageOptions` POCO style — AC-007 forbids new packages, so we stay POCO + non-`IOptions<>`).
- Desktop DI passes a default `MemoryOptions.Default` unless `Application:Memory:*` is configured.

### `MemoryOptions` (Application POCO)

- Properties:
  - `int PromptInjectionTopN` (default 5).
  - `int MaxListPageSize` (default 200).
  - `int RetrieveDefaultLimit` (default 10).
  - `int MaxContentLength` (default 4000) — duplicate of Domain constant for documentation only; Domain remains source of truth.
- No package dependency. POCO.

### Persistence components

#### `MemoryEntity`

- Owner: `Iris.Persistence.Entities`.
- Plain mutable EF entity: `Guid Id`, `string Content`, `int Kind`, `int Importance`, `int Status`, `int Source`, `long CreatedAt`, `long? UpdatedAt`.
- Stored as primitives so SQLite columns stay simple (mirrors existing `ConversationEntity`/`MessageEntity` pattern).

#### `MemoryEntityConfiguration`

- `ToTable("memories")`.
- Primary key: `Id` (no value generation, like conversations).
- `Content` required, max length not enforced at DB level (Domain enforces 4000).
- `Status`, `Kind`, `Importance`, `Source` stored as INTEGER.
- `CreatedAt`, `UpdatedAt` use the same `_utcTicksConverter` pattern (long-tick storage). `UpdatedAt` nullable.
- Indexes (D-007 resolved): `HasIndex(m => m.Status)` and `HasIndex(m => m.UpdatedAt)`. Composite index `(Status, UpdatedAt DESC)` is preferable but EF Core SQLite syntax for descending is supported — keeping it as two separate indexes is safer for v1 because the existing conversations configuration uses simple single-column indexes.

#### `MemoryMapper`

- Static class: `ToDomain(MemoryEntity)` → `Memory`, `ToEntity(Memory)` → `MemoryEntity`.
- Domain `Memory` exposes a private constructor; mapping uses an internal-static factory (`Memory.Rehydrate(...)`) that bypasses validation and is **the only** rehydration path. Domain owns this, not Persistence.

#### `MemoryRepository`

- Owner: `Iris.Persistence.Repositories`.
- Implements all 5 port methods.
- Uses `IrisDbContext.Memories`.
- For `SearchActiveAsync(query, limit, ct)`: SQLite is case-insensitive by default for ASCII (`LIKE` with `COLLATE NOCASE`). For Russian text, `EF.Functions.Like(content, $"%{query}%")` works case-insensitively when paired with `COLLATE NOCASE` **on the column**. Design choice: add `.UseCollation("NOCASE")` to the `Content` column to keep substring matching deterministic across locales. Alternative: use `string.Contains` with `StringComparison.OrdinalIgnoreCase` translated by EF — **but** EF Core SQLite provider does not translate the comparison overload; the `COLLATE NOCASE` route is more reliable.
- Returns ordered most-recent-first (`UpdatedAt ?? CreatedAt`).

#### `IrisDbContext` extension

- Adds `public DbSet<MemoryEntity> Memories => Set<MemoryEntity>();`.
- `OnModelCreating` already scans the assembly for `IEntityTypeConfiguration<>`, so adding `MemoryEntityConfiguration` is automatic.

#### Schema bootstrap

- No new migration. `IrisDatabaseInitializer.InitializeAsync` already calls `EnsureCreatedAsync`. With the new `Memories` DbSet declared, `EnsureCreatedAsync` will create the `memories` table on fresh databases. **For existing databases**, `EnsureCreatedAsync` does **not** add new tables to an existing database — this is a known limitation. v1 mitigation: see §9 and §15.

### Desktop components

#### `IIrisApplicationFacade` (extended)

Adds:
```csharp
Task<Result<RememberMemoryResult>> RememberAsync(string content, MemoryKind? kind, MemoryImportance? importance, CancellationToken ct);
Task<Result<Unit>> ForgetAsync(MemoryId id, CancellationToken ct);
Task<Result<UpdateMemoryResult>> UpdateAsync(MemoryId id, string newContent, CancellationToken ct);
Task<Result<IReadOnlyList<MemoryDto>>> ListActiveMemoriesAsync(int? limit, CancellationToken ct);
```

`RetrieveRelevantMemoriesAsync` is **not** exposed to Desktop in v1: only the prompt pipeline needs it, and the prompt pipeline runs inside `SendMessageHandler` scope.

#### `IrisApplicationFacade` (extended)

Resolves new handlers per scope, calls them, returns `Result<...>`. Same pattern as `SendMessageAsync`.

#### `MemoryViewModel`

- Owner: `Iris.Desktop.ViewModels`.
- Constructor: `(IIrisApplicationFacade facade)`.
- Singleton (matches existing `ChatViewModel`/`MainWindowViewModel` lifetime).
- Properties: `ObservableCollection<MemoryViewModelItem> Memories`, `bool IsLoading`, `string? ErrorMessage`, optional `string NewMemoryContent`.
- Commands: `LoadAsync()`, `RememberAsync()`, `ForgetAsync(MemoryId)`.
- Refresh: after Remember or Forget, reload the list (simplest correct behavior; later optimizations are v2).

#### `MemoryViewModelItem`

- POCO/record: `MemoryId Id`, `string Content`, `string KindLabel`, `string ImportanceLabel`, `DateTimeOffset CreatedAt`, `DateTimeOffset? UpdatedAt`.

#### `MemoryView` and `MemoryCard`

- `MemoryView`: `ItemsControl` over `Memories`, with a header containing a textbox + "Запомнить" button and an empty-state placeholder.
- `MemoryCard`: shows content + meta + a "Забыть" button bound to `ForgetCommand`.

#### MainWindow navigation

- Approach v1: add a `TabControl` to `MainWindow` with two tabs: "Чат" and "Память". The existing chat content goes into the first tab; `MemoryView` goes into the second. This is the smallest possible navigation change and respects FR-024 ("exact placement is a design decision").
- Alternative considered: side rail or slide-in panel. Rejected as more UX work without v1 benefit.

#### Desktop DI

- Register `MemoryViewModel` as Singleton.
- No new packages. No new project references.

## 7. Contract Design

### `IMemoryRepository`

- Owner: Application.
- Consumers: 5 handlers + `MemoryContextBuilder`.
- Shape: 5 async methods (see §6).
- Compatibility: new; no existing consumers.
- Stability: v1 — additive in v2 (e.g. `ListByKindAsync`). v1 must avoid generic `IRepository<T>` — roadmap §6 Phase 3 explicitly rejects that.
- Error behavior: exceptions propagate; handlers translate.

### `IUnitOfWork` — unchanged.

### `PromptBuilder`

- Constructor: extended (3 deps instead of 1). Production caller is `SendMessageHandler` via DI.
- `Build` signature: unchanged shape. **However**, `PromptBuilder.Build` becomes synchronous-but-needs-pre-computed-memory-list: see §8 for the explicit decision.
- New collaborator added; FR-019 byte-equivalent in empty-memory case.

### `IIrisApplicationFacade`

- 4 new methods. Additive. Existing `SendMessageAsync` unchanged.
- Stability: stable for v1; v2 may add `RetrieveRelevantMemoriesAsync` if a "search memories" UI lands.

### Domain `Memory`

- Public constructor private. Public static `Create(...)` and `internal static Rehydrate(...)`.
- Methods: `Forget(now)`, `UpdateContent(content, now)`.
- No public mutators outside these methods.

## 8. Data Flow

### 8.1 Remember (Desktop → Domain)

1. User clicks "Запомнить" in `MemoryView`.
2. `MemoryViewModel.RememberAsync()` → `IIrisApplicationFacade.RememberAsync(content, ...)`.
3. Facade opens scope, resolves `RememberExplicitFactHandler`.
4. Handler:
   1. Calls `MemoryContent.Create(content)` (Domain validation).
   2. Builds `Memory.Create(MemoryId.New(), content, kind ?? Note, importance ?? Normal, MemorySource.UserExplicit, _clock.UtcNow)`.
   3. `_memoryRepository.AddAsync(memory, ct)`.
   4. `_unitOfWork.CommitAsync(ct)`.
   5. Returns `Result.Success(new RememberMemoryResult(MemoryDto))`.
5. ViewModel reloads list and clears the textbox.

### 8.2 Forget

1. User clicks "Забыть" on a card.
2. ViewModel → facade → handler.
3. Handler `GetByIdAsync` → if null, `Error.NotFound`.
4. `memory.Forget(_clock.UtcNow)` (idempotent on Forgotten).
5. `UpdateAsync` + `CommitAsync`.
6. ViewModel reloads list (forgotten memory disappears).

### 8.3 List

Same as 8.1 with `ListActiveMemoriesHandler` and `IMemoryRepository.ListActiveAsync(limit, ct)`.

### 8.4 Chat send (with memory injection)

This is the critical path that must preserve FR-019.

1. ViewModel → `IIrisApplicationFacade.SendMessageAsync(...)`.
2. Facade opens scope, resolves `SendMessageHandler`.
3. Handler validates command.
4. Handler loads/creates conversation (existing flow).
5. Handler loads recent history (existing flow).
6. **New:** Handler resolves `MemoryContextBuilder` (constructor-injected) and calls `await _memoryContextBuilder.SelectAsync(promptBuildRequest, ct)` → `IReadOnlyList<Memory>`.
7. Handler calls `_promptBuilder.Build(promptBuildRequest, memories)` — extended overload that accepts a pre-resolved memory list. `PromptBuilder.Build` stays sync.
8. Inside `PromptBuilder.Build`:
   - Always emit `ChatModelMessage(System, languagePolicySystemPrompt)`.
   - If `memories.Count > 0`: append `ChatModelMessage(System, _memoryPromptFormatter.Format(memories))`.
   - Append history mapped messages.
   - Append `ChatModelMessage(User, currentUserMessage)`.
9. `IChatModelClient.SendAsync(modelRequest, ct)` (unchanged).
10. Persistence (unchanged).

This decision keeps `PromptBuilder.Build` synchronous and concentrates async I/O in the handler.

### 8.5 Memory selection failure during chat

If `MemoryContextBuilder.SelectAsync` throws:
- `SendMessageHandler` catches, treats as empty memory list, proceeds. (Prefer letting chat work without memory rather than failing the whole turn.)
- Future v2: surface a degradation flag in `SendMessageResult` and log via `ILogger`. v1 silent fallback is acceptable per §15 trade-off.

### 8.6 Error / alternative flows

| Flow | Behavior |
|---|---|
| Remember with empty content | `MemoryContent.Create` throws; handler catches and returns `Error.Validation("memory.empty_content", ...)`. |
| Remember with content > 4000 | Same path: `Error.Validation("memory.content_too_long", ...)`. |
| Forget non-existent id | `Error.NotFound("memory.not_found", ...)`. |
| Forget already-forgotten | Domain idempotent; `UpdateAsync`+`CommitAsync` succeed; `Result.Success`. |
| Update on Forgotten | `DomainException("memory.not_active", ...)` → `Error.Conflict`. |
| Repository throws on Add/Update/Get | `Error.Failure("memory.persistence_failed", ...)`. |
| Cancellation | `OperationCanceledException` propagates; handler does not swallow. |
| Memory-context build fails during chat send | Chat send proceeds without memory block. No error surfaced to user. |
| EF schema not yet applied for an existing dev DB | See §9 / §15: app fails on first memory write; migration strategy delegated to evolution beyond v1. |

## 9. Data and State Design

### 9.1 Table

```text
memories(
    Id          BLOB/TEXT PRIMARY KEY,
    Content     TEXT NOT NULL COLLATE NOCASE,
    Kind        INTEGER NOT NULL,
    Importance  INTEGER NOT NULL,
    Status      INTEGER NOT NULL DEFAULT 0,   -- 0 = Active
    Source      INTEGER NOT NULL,
    CreatedAt   INTEGER NOT NULL,             -- UTC ticks
    UpdatedAt   INTEGER NULL                  -- UTC ticks
);
CREATE INDEX IX_memories_Status     ON memories(Status);
CREATE INDEX IX_memories_UpdatedAt  ON memories(UpdatedAt);
```

(Generated by EF Core via `MemoryEntityConfiguration`. Exact column types follow conversations table pattern.)

### 9.2 Identity

- `MemoryId.New()` produces `Guid.NewGuid()` at handler time (matches conversation pattern).

### 9.3 Lifecycle

```text
[create] → Active
Active → Active   (UpdateContent, sets UpdatedAt = now)
Active → Forgotten (Forget, sets UpdatedAt = now)
Forgotten → Forgotten (Forget idempotent, no-op, no DB write either if no actual transition)
Forgotten → Active (forbidden in v1; future restore is Phase 9+)
```

Optimization: when `Forget` is called on already-Forgotten, the handler must still succeed (FR-007), but it should **not** write to the DB if nothing changed. Implementation: `Memory.Forget` returns a `bool changed` (or domain raises a flag), and handler skips `UpdateAsync` when `changed == false`.

### 9.4 Ordering

- Repository `ListActiveAsync` and `SearchActiveAsync`: `OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)`.
- v1 does not depend on importance for ordering.

### 9.5 Schema migration policy

- v1 uses `EnsureCreatedAsync` only (consistent with conversations slice).
- For developers who already have an `iris.db` with the old schema, `EnsureCreatedAsync` does **not** create the new `memories` table on an existing database. They must delete `iris.db` (it lives in `%APPDATA%\Iris`). **This is acceptable v1 behavior** and is consistent with how the project has handled schema evolution to date. Recorded as known limitation in §15 + acceptance debt note.

### 9.6 No relation to conversations or messages

Per D-008, memories have no FK to conversations or messages.

### 9.7 Caching

None for v1. Repository hits the DB on every facade call.

## 10. Error Handling Design

### Error sources and translations

| Source | Raw form | Translation |
|---|---|---|
| Empty/whitespace content | `DomainException("memory.empty_content", ...)` | `Error.Validation("memory.empty_content", "...")` |
| Content > 4000 | `DomainException("memory.content_too_long", ...)` | `Error.Validation` |
| `Memory.UpdateContent` on Forgotten | `DomainException("memory.not_active", ...)` | `Error.Conflict("memory.not_active", ...)` |
| `IMemoryRepository.GetByIdAsync` returns null | n/a | `Error.NotFound("memory.not_found", ...)` |
| Repository throws | `Exception` | `Error.Failure("memory.persistence_failed", ...)` |
| Commit throws | `Exception` | `Error.Failure("memory.commit_failed", ...)` |
| `MemoryContextBuilder` throws during chat | `Exception` | swallowed; chat continues with empty memory list |
| Cancellation | `OperationCanceledException` | propagated, never converted to `Result` |

### Logging

- v1 does not introduce `ILogger<T>` injection into Application memory components. Reasoning: existing chat slice does not log either; introducing logging is a cross-cutting v2 task.
- A plain `try/catch` with silent swallow is acceptable for `MemoryContextBuilder` failures because the alternative (failing the chat turn) is worse UX. Trade-off recorded in §15.

### Cancellation

- Every async path takes `CancellationToken` and forwards it.

## 11. Configuration and Dependency Injection

### Application

`AddIrisApplication` signature change:

```text
AddIrisApplication(
    SendMessageOptions sendMessageOptions,
    LanguageOptions languageOptions,
    MemoryOptions memoryOptions)
```

Registers (in addition to existing):

- `services.AddSingleton(memoryOptions);`
- `services.AddSingleton<MemoryPromptFormatter>();`
- `services.AddScoped<MemoryContextBuilder>();`
- `services.AddScoped<RememberExplicitFactHandler>();`
- `services.AddScoped<ForgetMemoryHandler>();`
- `services.AddScoped<UpdateMemoryHandler>();`
- `services.AddScoped<RetrieveRelevantMemoriesHandler>();`
- `services.AddScoped<ListActiveMemoriesHandler>();`
- `PromptBuilder` lifetime upgrades from Singleton to **Scoped** (because it depends on scoped `MemoryContextBuilder`). Existing tests that resolve `PromptBuilder` directly must be updated to use a scope or to construct it manually. No public contract change beyond the constructor.

### Persistence

`AddIrisPersistence` adds:

- `services.AddScoped<IMemoryRepository, MemoryRepository>();`

### Desktop

- Reads optional `Application:Memory:PromptInjectionTopN`, `Application:Memory:MaxListPageSize`, `Application:Memory:RetrieveDefaultLimit` from `IConfiguration`, falls back to `MemoryOptions.Default`.
- Calls `services.AddIrisApplication(sendMessageOptions, languageOptions, memoryOptions);`.
- Registers `services.AddSingleton<MemoryViewModel>();`.
- No new package. No new project reference.

### Configuration shape (additive)

```jsonc
{
  "Application": {
    "Chat": { "MaxMessageLength": 4000 },
    "Memory": {
      "PromptInjectionTopN": 5,
      "MaxListPageSize": 200,
      "RetrieveDefaultLimit": 10
    }
  },
  "Persona": { "Language": { "DefaultLanguage": "ru" } }
}
```

All keys optional; absence yields defaults.

## 12. Security and Permission Considerations

- v1 trusts the local Desktop user with full memory CRUD.
- No PII detection. No secret detection. The user is responsible for content (D-009).
- Memory content is stored in plain text in SQLite at `%APPDATA%\Iris\iris.db`, i.e. user-private but file-readable. This matches conversation message privacy posture.
- No external network exposure: API endpoints stay empty (spec out-of-scope).
- Memory injection into prompts means content is sent to the local Ollama host. Acceptable because Ollama is local and already receives full conversation history.
- No telemetry. No outbound logging.
- Permission policy expansion (per-memory ACL, encryption, sensitivity classes) is a Phase 9+ concern and lives behind `MemorySensitivityPolicy` (out of scope per spec §3.2).

## 13. Testing Design

### 13.1 Domain unit tests (`Iris.Domain.Tests`)

T-DOM-MEM-01..09 from the spec. xUnit, no fakes needed. `MemoryContent.Create` and `Memory.Create/Forget/UpdateContent` cover invariants and idempotency.

### 13.2 Application unit tests (`Iris.Application.Tests`)

T-APP-MEM-01..24 + T-APP-PROMPT-01..03.

Test fakes:
- `FakeMemoryRepository` implementing `IMemoryRepository` over an in-memory `Dictionary<MemoryId, Memory>`.
- `FakeUnitOfWork` (already exists for chat tests, reuse).
- `FakeClock` (already in `Iris.Shared.Time` test infrastructure).

Critical regression test:
- T-APP-PROMPT-01 (FR-019): given `MemoryContextBuilder` returns empty list, the produced `ChatModelRequest.Messages` must equal the baseline (one system message + history + user message). Use sequence equality on role + content.
- T-APP-PROMPT-02: given two memories, the messages list contains: `[System(language), System(memoryBlock), ...history, User(current)]` in this order, with the memory block matching the `Известные факты:` template.
- T-APP-PROMPT-03: assert no `User`-role message contains the memory block prefix.

### 13.3 Persistence integration tests

Use the existing in-memory or temp-SQLite test fixture pattern (`Iris.Integration.Tests` already runs 82 tests).

T-PERS-MEM-01..05. Round-trip including enum + tick conversion. Search test asserts case-insensitive match on Russian text using `COLLATE NOCASE`.

### 13.4 Architecture tests (`Iris.Architecture.Tests`)

- T-ARCH-MEM-01: `Iris.Application` does not reference `Iris.Persistence` (extends existing pattern).
- T-ARCH-MEM-02: types under `Iris.Domain.Memories` do not reference `Microsoft.EntityFrameworkCore.*` or `Iris.Persistence.*`.
- T-ARCH-MEM-03: `Iris.Desktop.ViewModels.MemoryViewModel` (and `MemoryView`) do not reference `IMemoryRepository`, `IrisDbContext`, or `MemoryEntity`.
- T-ARCH-MEM-04: `Iris.Application.Memory.*` does not reference `Iris.Persistence.Entities.MemoryEntity`.

### 13.5 Manual smoke

M-MEM-01..05 from spec. Recorded in `.agent/log_notes.md` if anomalies are observed.

### 13.6 Verification commands

- `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx` → all green.
- `dotnet format .\Iris.slnx --verify-no-changes` → exit 0.

## 14. Options Considered

### Option A — `PromptBuilder.Build` becomes async (rejected)

Make `PromptBuilder.Build` async so it can call `MemoryContextBuilder` itself.
- ✓ One place owns memory selection.
- ✗ Breaks current sync test pattern.
- ✗ Forces `PromptBuilder` to know about repository concerns and async.
- **Decision:** rejected. Handler does memory selection, builder stays sync but accepts the list.

### Option B — Single combined `MemoryService` (rejected)

One service exposing all 5 operations + context building.
- ✓ Fewer types.
- ✗ Roadmap §7.4 v2 explicitly forbids "god" service.
- ✗ Hard to test in isolation.
- **Decision:** rejected. Use 5 handlers + context builder, matching chat-slice pattern.

### Option C — EF Core migrations introduced now (rejected)

Add `dotnet ef migrations add InitialCreate` + `add MemoryV1`.
- ✓ Cleaner schema evolution for users with existing DBs.
- ✗ New piece of infrastructure not required by v1 spec; project today uses `EnsureCreated`.
- ✗ Adds tooling/process burden (dotnet-ef tool, design-time DbContext factory wiring).
- **Decision:** rejected for v1. Recorded as `TECH-DEBT-EF-MIGRATIONS` in §15.

### Option D — Memory injected into existing language system message (rejected)

Concat the memory block into the same first system message instead of emitting a second one.
- ✓ One system message.
- ✗ Breaks FR-019 byte equivalence in empty case is still preserved, but in non-empty case it changes the boundary. More invasive.
- ✗ Harder to test the memory block independently.
- **Decision:** rejected. Two system messages is cleaner.

### Option E — `PromptBuilder` upgrade to Singleton kept (rejected)

Keep `PromptBuilder` singleton and inject `IServiceScopeFactory` so it can resolve `MemoryContextBuilder` per call.
- ✓ Less DI churn.
- ✗ Service locator anti-pattern (rule `iris-architecture.md`).
- **Decision:** rejected. Use scoped `PromptBuilder`.

### Option F — Desktop calls handlers directly via DI (rejected)

Skip the facade, inject handlers into `MemoryViewModel`.
- ✗ Couples Desktop to Application internals.
- ✗ Violates existing `IIrisApplicationFacade` pattern.
- **Decision:** rejected. Extend facade.

### Option G — `RetrieveRelevantMemoriesHandler` exposed to Desktop (rejected for v1)

UI search box that lists matching memories.
- ✓ Useful for power users.
- ✗ Spec FR-021 only requires listing all active memories. Not in scope.
- **Decision:** deferred to v2.

## 15. Risks and Trade-Offs

### R-001: `EnsureCreatedAsync` does not migrate existing databases

When users with an existing `iris.db` install Phase 8, the `memories` table will not exist and the first memory write will fail. Mitigation: documented in user-facing release note ("delete `%APPDATA%\Iris\iris.db` to upgrade"). Long-term mitigation: introduce EF migrations as a separate phase. **Severity: medium** for current dev workflow, **low** for first-install users.

### R-002: Silent swallow of `MemoryContextBuilder` failures

If memory selection fails, chat continues without injection. The user gets no signal. Trade-off: chat resilience over user transparency. v2 may add a degradation flag in `SendMessageResult`.

### R-003: `PromptBuilder` lifetime change (Singleton → Scoped)

Existing tests that resolve it from a singleton container must be adapted. `PromptBuilder` has no per-instance state today, so behavior is unchanged. **Severity: low**.

### R-004: `Memory.Forget` no-op DB write avoidance

If the handler always calls `UpdateAsync` even on idempotent forget, we incur an unnecessary write. Design says: skip `UpdateAsync` when domain method reports no change. **Severity: low**, but matters for FR-007 semantics.

### R-005: Substring search is naive

Case-insensitive substring on Russian text via `COLLATE NOCASE` covers ASCII case folding only. Cyrillic case folding is generally well-handled by SQLite's NOCASE collation in modern builds, but edge cases (Ё/е, capitalization) may behave unexpectedly. **Severity: low** for v1; semantic search is v2.

### R-006: `MemoryOptions` becomes a third "options POCO" pattern

We now have `SendMessageOptions`, `LanguageOptions`, `MemoryOptions` — none of them use `IOptions<>`. This is consistent within the codebase but diverges from idiomatic .NET. **Severity: note**, acceptable until a broader options design.

### R-007: `IIrisApplicationFacade` lives in Desktop

This was an existing decision (pre-Phase 8), not introduced here. If the future API host needs the same orchestration, it will need its own facade or the facade needs to move into a shared host-neutral place. v1 does not address this; recorded for Phase 14.

### R-008: Domain `Memory` placeholder file conflicts

The spec authorized in-place replacement of placeholder files. The risk is that an unrelated dirty file gets touched. Mitigation: implementation phase verifies a clean working tree before starting (already standard per `iris-engineering`).

## 16. Acceptance Mapping

| Spec acceptance | Design proof |
|---|---|
| `Memory` aggregate with invariants | §6 `Memory`, §6 `MemoryContent` |
| 5 handlers exist | §6 Handlers |
| `MemoryContextBuilder` consumed by `PromptBuilder` | §6 `MemoryContextBuilder` + `PromptBuilder` extension |
| `IMemoryRepository` is a real public port | §6 + §7 |
| Russian default byte-equivalent when empty | §6 `PromptBuilder`, §13 T-APP-PROMPT-01 |
| Memory block in system role only | §6 + §13 T-APP-PROMPT-02/03 |
| Desktop `MemoryView` with Forget | §6 Desktop components |
| Architecture tests for boundaries | §13.4 |
| Build / test / format green | §13.6 |
| No new project / package / IVT | §11 (no new package); no project additions discussed |
| Out-of-scope placeholders untouched | §3 + §6 (named scopes only); §15 R-008 |

## 17. Blocking Questions

None. All spec open questions (§14) have been resolved by design choices recorded in §6 / §9 / §11 / §14.
