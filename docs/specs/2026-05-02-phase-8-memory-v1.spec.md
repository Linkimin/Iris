# Specification: Phase 8 — Memory v1 (Explicit Memory)

**Active stage:** `/spec`
**Source authority:** `.agent/mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md` §6 (Phase 8), §7.8 (Memory v1), §9, §10

## 1. Problem Statement

Iris currently has no durable user-facing memory. Each chat call rebuilds prompt context strictly from `IMessageRepository` recent messages plus `ILanguagePolicy.GetSystemPrompt()`. There is no concept of "facts the user explicitly asked Iris to remember", no command surface to add/remove such facts, and no mechanism to inject relevant facts into the prompt.

This makes Iris functionally a stateless chat client across conversations. The roadmap (§6 Phase 8, §7.8 v1, §10) defines the next product step as **explicit, user-driven memory**: Iris remembers only what the user explicitly said to remember, can recall/forget on user command, and injects only relevant facts into the prompt.

The placeholder files in Domain/Application/Persistence/Desktop are scaffolding from initial project setup; none of the v2-grade features they hint at (consolidation, embeddings, ranking, conflict detection, audit log, candidate review) are implemented or required for v1. Phase 8 must not attempt them — that is explicitly Memory v2 (§7.8 v2).

## 2. Goal

Deliver Memory v1 such that:

1. The user can issue an explicit "remember" instruction and Iris persists that fact as a `Memory` aggregate in SQLite.
2. The user can issue an explicit "forget" instruction and Iris marks the corresponding memory as removed.
3. The user can issue an explicit "what do you remember about X" query and Iris returns the matching subset.
4. When Iris assembles a prompt for a chat turn, relevant active memories are injected into the system context — but only when relevance criteria are satisfied.
5. The user can view all active memories in a Desktop list and delete any memory from that list.
6. Architecture boundaries (Domain/Application/Adapters/Hosts), Russian default language, existing chat slice, and existing test/format/CI gates remain intact.

The goal is verifiable when the Acceptance Criteria (§13) are satisfied.

## 3. Scope

### 3.1 In Scope

- **Domain:** `Memory` aggregate, `MemoryId`, `MemoryContent`, `MemoryKind`, `MemorySource`, `MemoryImportance`, `MemoryStatus`, lifecycle methods (create / soft-delete / update content), invariants.
- **Application:**
  - Use cases: `RememberExplicitFactHandler`, `ForgetMemoryHandler`, `UpdateMemoryHandler`, `RetrieveRelevantMemoriesHandler`, `ListActiveMemoriesHandler`.
  - `MemoryContextBuilder` that selects relevant active memories for a given prompt build request.
  - Port: `IMemoryRepository` abstraction in `Iris.Application.Abstractions.Persistence`.
  - Wiring of `MemoryContextBuilder` into the existing `PromptBuilder` so the system prompt receives a memory block alongside the language instruction.
  - DTOs for memory results.
- **Persistence:**
  - `MemoryEntity`, `MemoryEntityConfiguration` (EF Core fluent), `MemoryMapper` (Domain↔Entity), `MemoryRepository` implementing `IMemoryRepository`.
  - `IrisDbContext.Memories` DbSet.
  - EF Core migration that creates the `Memories` table.
  - DI registration in existing `Iris.Persistence.DependencyInjection`.
- **Desktop:**
  - `MemoryView`, `MemoryViewModel`, `MemoryViewModelItem`, `MemoryCard` populated for v1.
  - Navigation entry from existing shell to `MemoryView`.
  - "Delete" / "Forget" action on a memory list item.
  - DI registration through existing facade pattern.
- **Tests:**
  - Domain unit tests for `Memory` invariants.
  - Application unit tests for each handler and `MemoryContextBuilder` (with fake `IMemoryRepository`, fake `IClock`).
  - Persistence integration tests for `MemoryRepository` against SQLite (mirroring existing `MessageRepository` test pattern).
  - Architecture tests extended to forbid Application referencing concrete `MemoryEntity`/EF Core.
  - Existing chat tests must still pass with prompt now possibly including a memory block.
- **Memory files / docs:** updates to `.agent/PROJECT_LOG.md` and `.agent/overview.md` after implementation; `mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md` may add a "Phase 8 status" note but should not be rewritten.

### 3.2 Out of Scope

- Embeddings, vector similarity, semantic search (`MemoryEmbedding*` placeholders stay empty).
- Memory consolidation, conflict detection, merge, decay, importance scoring (`MemoryConsolidation*`, `MemoryConflictDetector`, `MemoryScoringPolicy`, `MemoryMergePolicy`, `MemoryRetentionPolicy` placeholders stay empty).
- Memory candidate extraction from chat (`MemoryExtractionService`, `MemoryCandidate*`, `MemoryReviewQueue` stay empty).
- Memory audit log (`MemoryAccessLogger`, `MemoryAuditRecord`, `MemorySourceExplainer` stay empty).
- Memory ranking ML (`MemoryRankingService` stays empty).
- API endpoints (`MemoryEndpoints`, `MemoryHttpResponse`, `MemoryApiMapper` stay empty).
- Background tasks (`MemoryEmbeddingTask`, `MemoryConsolidationTask` stay empty).
- Persona/Context v1 / Modes v1 (Phase 9, separate spec).
- Tools, Voice, Perception (later phases).
- Sensitivity/privacy policy beyond a simple "no memory containing obvious secrets" guard (`MemorySensitivityPolicy` stays minimal or empty until Phase 9+).
- Voice-driven memory commands.

### 3.3 Non-Goals

- **No automatic memory extraction from conversation.** Roadmap §6 Phase 8 explicitly forbids this for v1.
- **No emotion-as-fact storage.**
- **No automatic secret capture.**
- **No memory derived from desktop perception.**
- **No God-object memory service.** Use the handler/policy/service pattern matching existing chat slice (§7.4 v2 constraint).
- **No new top-level project.** All work lives in existing projects.
- **No new NuGet package.** Phase 8 v1 must be implementable with the package set already in `Directory.Packages.props`.
- **No change to `ILanguagePolicy` semantics.** Russian default remains a hard default.

## 4. Current State

- **Chat slice (Phase 5–7):** `SendMessageHandler` orchestrates conversation load/create, recent-history load, `PromptBuilder.Build`, `IChatModelClient.SendAsync`, message persistence, `IUnitOfWork.CommitAsync`. `PromptBuilder` currently constructs a system message from `ILanguagePolicy.GetSystemPrompt()` plus 20 recent history messages plus the new user message. There is no memory injection.
- **Persistence:** `IrisDbContext` has only `Conversations` and `Messages`. EF Core configuration is loaded by assembly scan. Repositories use `IrisDbContext` directly. SQLite path is now stable under `%APPDATA%\Iris` (per recent commit `f95c161`).
- **Domain:** `Conversations` slice is complete with `Conversation`, `Message`, `MessageRole`, `MessageContent`, etc. `Memories` namespace contains 17 empty placeholder files.
- **Application:** `Memory` namespace contains many empty placeholder files. `IMemoryRepository` is an empty `internal interface` placeholder.
- **Persistence Memory:** `MemoryEntity`, `MemoryEntityConfiguration`, `MemoryMapper`, `MemoryRepository` are empty placeholders. No migration for memory exists.
- **Desktop Memory:** `MemoryView`, `MemoryViewModel`, `MemoryViewModelItem`, `MemoryCard` are empty placeholders.
- **API Memory:** `MemoryEndpoints`, `MemoryHttpResponse`, `MemoryApiMapper` are empty placeholders. The API host itself is not yet a delivered phase.
- **Architecture safeguards:** 8 architecture tests green; `Directory.Packages.props` central versioning; CI workflow runs build/test/format on push/PR.
- **Memory files:** `.agent/overview.md` lists Phase 8 as the next roadmap step. `mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md` §6 Phase 8 and §7.8 are the authoritative references.

## 5. Affected Areas

| Project | Area | Nature of change |
|---|---|---|
| `Iris.Domain` | `Memories/*` | Replace placeholders with real types |
| `Iris.Domain.Tests` | new `Memories/*` | New unit tests |
| `Iris.Application` | `Abstractions/Persistence/IMemoryRepository.cs` | Replace placeholder with real port |
| `Iris.Application` | `Memory/Commands/*` (or equivalent owner folder) | New: `Remember`, `Forget`, `Update`, `Retrieve`, `ListActive` handlers |
| `Iris.Application` | `Memory/Context/MemoryContextBuilder` | New service used by `PromptBuilder` |
| `Iris.Application` | `Chat/Prompting/PromptBuilder` | Modify: accept memory block from builder |
| `Iris.Application.Tests` | `Memory/*` | New unit tests; existing prompt tests adjusted |
| `Iris.Persistence` | `Entities/MemoryEntity`, `Configurations/MemoryEntityConfiguration`, `Mapping/MemoryMapper`, `Repositories/MemoryRepository`, `Database/IrisDbContext`, `DependencyInjection` | Replace placeholders, add DbSet, add migration |
| `Iris.Persistence` | `Migrations/*` | New migration |
| `Iris.Integration.Tests` (or existing equivalent) | new memory persistence integration tests | New tests |
| `Iris.Desktop` | `Views/MemoryView`, `ViewModels/MemoryViewModel`, `Models/MemoryViewModelItem`, `Controls/Memory/MemoryCard`, navigation/shell | Replace placeholders, add nav |
| `Iris.Desktop` | `DependencyInjection` | Register MemoryViewModel |
| `Iris.Architecture.Tests` | new tests | Forbid Application/Domain referencing EF Core memory types |
| `.agent/PROJECT_LOG.md` | append | Phase 8 entry after implementation |
| `.agent/overview.md` | update | Current Phase, Working Status, Next Step |

Untouched (must remain unchanged):

- `Iris.Shared` (no new shared primitives required for v1).
- `Iris.ModelGateway` (memory does not touch model providers).
- `Iris.Tools`, `Iris.Voice`, `Iris.Perception`, `Iris.Worker`, `Iris.Api`, `Iris.SiRuntimeGateway`.
- `LanguageOptions`, `RussianDefaultLanguagePolicy`, `LanguageInstructionBuilder` semantics.

## 6. Functional Requirements

### Remember

- **FR-001:** When the application receives an explicit `RememberExplicitFactCommand` (content, optional kind, optional importance, source), it must persist a new `Memory` aggregate with status `Active` and creation timestamp from `IClock`.
- **FR-002:** Empty/whitespace-only memory content must be rejected with a domain or validation error; no memory is persisted.
- **FR-003:** Maximum content length must be enforced. The limit must be defined as a constant in the Domain layer. The default limit must be at least 4000 characters.
- **FR-004:** Each persisted memory must have a unique `MemoryId` generated at creation time.
- **FR-005:** Persistence write of a memory must commit through `IUnitOfWork`.

### Forget

- **FR-006:** When the application receives `ForgetMemoryCommand(MemoryId)`, the memory must transition to status `Forgotten` (soft-delete). Hard deletion is not required for v1.
- **FR-007:** Forgetting an already-forgotten memory must be a no-op success, not an error.
- **FR-008:** Forgetting a non-existent `MemoryId` must return a controlled "not found" error result.

### Update

- **FR-009:** `UpdateMemoryCommand(MemoryId, newContent)` must replace the content of an active memory and update its `UpdatedAt` timestamp.
- **FR-010:** Updating a forgotten memory must return a controlled "not active" error.
- **FR-011:** Update must enforce the same content rules as remember (FR-002, FR-003).

### Retrieve

- **FR-012:** `RetrieveRelevantMemoriesQuery(query: string, limit: int)` must return active memories whose content matches the query in a deterministic, simple way (case-insensitive substring match is acceptable for v1).
- **FR-013:** Retrieve must never return forgotten memories.
- **FR-014:** Result list must be ordered most-recent-first by `UpdatedAt` (or `CreatedAt` if never updated).
- **FR-015:** Result count must respect the supplied `limit`. A default sane limit (e.g. 10) must be applied when none is supplied.

### List

- **FR-016:** `ListActiveMemoriesQuery` must return all active memories ordered most-recent-first, with pagination parameters or a default cap (e.g. 200) to prevent unbounded reads. v1 may use a hard cap rather than full pagination.

### Prompt Injection

- **FR-017:** `MemoryContextBuilder` must, given a `PromptBuildRequest`, return zero-or-more relevant memories. v1 selection rule: take the top N (e.g. 5, configurable via an Application-layer constant or POCO options object) most-recent active memories. v1 must not depend on embeddings or semantic similarity.
- **FR-018:** `PromptBuilder` must include a memory block in the system message **only if** at least one relevant memory is returned. The block must be appended to the language-policy system prompt as an additional system-role message **or** included as a labeled section in the same system-role message — exact representation is a design decision, but it must be a system-role injection, never a user-role injection.
- **FR-019:** When no relevant memories exist, the prompt produced by `PromptBuilder` must be byte-equivalent to the pre-Phase-8 prompt for the same input. (This protects existing chat tests.)
- **FR-020:** The memory block, when present, must be in Russian or language-policy-neutral text. It must not be auto-translated. The exact wording is a design decision but must be testable.

### Desktop UI

- **FR-021:** `MemoryView` must list all active memories with content, kind, importance, created/updated timestamps.
- **FR-022:** Each memory card must offer a "Forget" / "Delete" action that invokes `ForgetMemoryHandler`.
- **FR-023:** After Forget, the list must refresh and the forgotten memory must disappear from the view.
- **FR-024:** `MemoryView` must be reachable from the existing Desktop shell via a navigation control; exact placement is a design decision.
- **FR-025:** `MemoryView` must not call `IrisDbContext`, `IMemoryRepository`, or any persistence type directly. It must use `IrisApplicationFacade` or an equivalent Desktop-side facade method.

### Cross-cutting

- **FR-026:** Russian default language policy must remain unchanged.
- **FR-027:** Existing chat send/receive flow must continue to work end-to-end after the changes.
- **FR-028:** No new background tasks may be activated for v1 (`MemoryEmbeddingTask` and `MemoryConsolidationTask` stay empty placeholders).

## 7. Architecture Constraints

- **AC-001:** `Iris.Domain` must not reference EF Core, `Iris.Application`, adapters, or hosts. Domain memory types are pure C# with `Iris.Shared` primitives only.
- **AC-002:** `Iris.Application` must not reference `Iris.Persistence`. Memory port is an abstraction inside `Iris.Application.Abstractions.Persistence`.
- **AC-003:** `Iris.Application` must not reference `Iris.ModelGateway`, `Iris.Tools`, `Iris.Voice`, `Iris.Perception`, `Iris.Infrastructure`, or any host.
- **AC-004:** `Iris.Persistence` may reference `Iris.Domain`, `Iris.Application`, and `Iris.Shared`. It must not reference any host or other adapter.
- **AC-005:** `Iris.Desktop` must not call `IMemoryRepository`, EF Core, or `IrisDbContext` directly. All memory operations go through the Application facade.
- **AC-006:** No new project references between existing projects beyond what already exists. No new project added.
- **AC-007:** No new NuGet package added. v1 must use packages already declared in `Directory.Packages.props`.
- **AC-008:** No `InternalsVisibleTo` added between production projects.
- **AC-009:** No service-locator pattern. All Application services receive dependencies via constructor injection.
- **AC-010:** Memory work must not introduce a "god service" combining remember + forget + update + retrieve + context building. Use separate handlers + a focused `MemoryContextBuilder`.
- **AC-011:** `PromptBuilder` may receive `MemoryContextBuilder` (or equivalent) via constructor; it must not call `IMemoryRepository` itself. Read path is `PromptBuilder` → `MemoryContextBuilder` → `IMemoryRepository`.
- **AC-012:** All architecture-test-protected boundary rules must continue to pass. New architecture tests must be added for the memory boundary (Application does not depend on `MemoryEntity`; Domain `Memory` does not depend on EF Core).
- **AC-013:** Memory placeholders not used by v1 (`MemoryEmbedding*`, `MemoryConsolidation*`, `MemoryRanking*`, `MemoryAudit*`, `MemoryExtraction*`, `MemoryEndpoints`, `MemoryApiMapper`, etc.) must remain empty placeholders. They must not be partially implemented or deleted in this phase.
- **AC-014:** Russian-default invariants from the language slice are preserved.

## 8. Contract Requirements

| Contract | Current | Required after Phase 8 v1 | Compatibility |
|---|---|---|---|
| `IMemoryRepository` (Application port) | empty internal interface | new public interface with `AddAsync`, `UpdateAsync`, `GetByIdAsync`, `ListActiveAsync(limit, ct)`, `SearchActiveAsync(query, limit, ct)` (exact shape decided in `/design`) | new contract; no consumer outside this phase |
| `IConversationRepository`, `IMessageRepository`, `IUnitOfWork`, `IClock`, `IChatModelClient`, `ILanguagePolicy` | live | unchanged | backward compatible |
| `PromptBuilder` constructor | `(ILanguagePolicy)` | extended to `(ILanguagePolicy, MemoryContextBuilder)` (or equivalent) | breaking for direct instantiators; existing tests must be updated; only one production caller (`SendMessageHandler` via DI) |
| `PromptBuilder.Build` return shape | `Result<PromptBuildResult>` | unchanged shape; semantics extended to include memory block when present | additive; FR-019 protects byte-equivalence in empty case |
| `IrisDbContext.DbSets` | `Conversations`, `Messages` | adds `Memories` | additive; no schema breaking change to existing tables |
| EF Core migrations | covers conversations + messages | adds `Memories` table migration | new migration; existing migrations unchanged |
| `IrisApplicationFacade` (Desktop side) | not authoritatively referenced here | extended with memory methods (`RememberAsync`, `ForgetAsync`, `ListMemoriesAsync`, etc.) | additive |
| API endpoints | n/a — API host not delivered | unchanged (placeholders stay empty) | n/a |

## 9. Data and State Requirements

- **D-001:** `Memory` is persisted in a new SQLite table (default name `Memories`). Required columns: `Id` (text, PK), `Content` (text, not null), `Kind` (text or int enum), `Importance` (int or text enum), `Status` (int or text enum, default Active), `Source` (text or int enum), `CreatedAt` (text/ticks, not null), `UpdatedAt` (text/ticks, nullable).
- **D-002:** `Memory.Status` allowed values for v1: `Active`, `Forgotten`. Other values from §7.8 v2 may be defined in domain enum but must not be used by v1 handlers.
- **D-003:** `Memory.Kind` enum must cover at least: `Fact`, `Preference`, `Note`. Additional values from §7.8 v2 may be declared but unused.
- **D-004:** `Memory.Importance` enum must cover at least: `Low`, `Normal`, `High`. v1 selection logic (FR-017) must not yet rely on importance (recency is sufficient for v1) — but the column must be persisted and exposed.
- **D-005:** `Memory.Source` enum must cover at least: `UserExplicit`. Other sources from §7.8 v2 may be defined but must not be assigned by v1 handlers.
- **D-006:** All timestamps stored as UTC, generated through `IClock`.
- **D-007:** Index recommendations (design-level, not strict): `Status`, `UpdatedAt DESC` to support FR-014/FR-016/FR-017 efficiently. Final indexing decisions belong in `/design`.
- **D-008:** No cascade rules link memories to conversations or messages in v1. Memories are independent of conversation lifecycle.
- **D-009:** No PII or secret detection is required for v1. The user is responsible for what they ask Iris to remember. (See FR §3.2 sensitivity policy out of scope.)

## 10. Error Handling and Failure Modes

| Failure | Required behavior |
|---|---|
| Empty/whitespace memory content | Domain factory throws `DomainException` or validator returns `Result.Failure(Error.Validation(...))`. No row is written. |
| Content exceeds max length | Same as empty: validation failure. |
| `MemoryId` not found on Forget | Handler returns `Result.Failure(Error.NotFound("memory.not_found", ...))`. No exception propagated to UI. |
| `MemoryId` not found on Update | Same. |
| Updating a `Forgotten` memory | Handler returns `Result.Failure(Error.Conflict("memory.not_active", ...))`. |
| Repository throws on Add/Update/Get | Handler catches non-`OperationCanceledException`, returns `Result.Failure(Error.Failure("memory.persistence_failed", ...))` mirroring chat slice pattern. |
| Cancellation token triggers | `OperationCanceledException` propagates; handler does not swallow. |
| `MemoryContextBuilder` throws | `PromptBuilder` must degrade gracefully: log/wrap and proceed without memory block (better degradation than failing the chat send). Exact wrapping decision in `/design`. |
| Migration apply failure on startup | Existing `IrisDatabaseInitializer` behavior governs; no special handling required for memory specifically beyond existing pattern. |
| Concurrent Forget of same memory | Idempotent (FR-007). Last write wins; no exception bubbles to UI. |

## 11. Testing Requirements

### 11.1 Domain unit tests (`Iris.Domain.Tests`)

- T-DOM-MEM-01: `Memory.Create` succeeds with valid content/kind/importance/source/clock.
- T-DOM-MEM-02: `Memory.Create` rejects empty content.
- T-DOM-MEM-03: `Memory.Create` rejects whitespace-only content.
- T-DOM-MEM-04: `Memory.Create` rejects content longer than max.
- T-DOM-MEM-05: `Memory.Forget` transitions Active→Forgotten and sets `UpdatedAt`.
- T-DOM-MEM-06: `Memory.Forget` on already-Forgotten is idempotent.
- T-DOM-MEM-07: `Memory.UpdateContent` replaces content and updates timestamp.
- T-DOM-MEM-08: `Memory.UpdateContent` on Forgotten is rejected.
- T-DOM-MEM-09: `MemoryId` equality / immutability.

### 11.2 Application unit tests (`Iris.Application.Tests`)

- T-APP-MEM-01..05: `RememberExplicitFactHandler` happy path, validation failure, empty content, max-length, repository exception.
- T-APP-MEM-06..09: `ForgetMemoryHandler` happy path, not-found, idempotent on Forgotten, repository exception.
- T-APP-MEM-10..13: `UpdateMemoryHandler` happy path, not-found, conflict on Forgotten, validation failures.
- T-APP-MEM-14..17: `RetrieveRelevantMemoriesHandler` substring match, default limit applied, ignores Forgotten, ordering most-recent-first.
- T-APP-MEM-18..20: `ListActiveMemoriesHandler` cap respected, ordering, ignores Forgotten.
- T-APP-MEM-21..24: `MemoryContextBuilder` returns empty when none active, returns top-N when many active, ignores Forgotten, deterministic ordering.
- T-APP-PROMPT-01: `PromptBuilder` produces prompt byte-equivalent to baseline when `MemoryContextBuilder` returns empty (FR-019).
- T-APP-PROMPT-02: `PromptBuilder` includes memory block as system-role content when memories exist (FR-018, FR-020).
- T-APP-PROMPT-03: `PromptBuilder` does not put memory in user-role messages.

### 11.3 Persistence integration tests

- T-PERS-MEM-01: `MemoryRepository.AddAsync` persists row; `GetByIdAsync` round-trips all fields including enums and timestamps.
- T-PERS-MEM-02: `UpdateAsync` updates content/`UpdatedAt`/`Status` correctly.
- T-PERS-MEM-03: `ListActiveAsync` ignores Forgotten rows.
- T-PERS-MEM-04: `SearchActiveAsync` (or equivalent) substring-matches case-insensitively.
- T-PERS-MEM-05: Migration creates `Memories` table with required columns.

### 11.4 Architecture tests (`Iris.Architecture.Tests`)

- T-ARCH-MEM-01: `Iris.Application` does not reference `Iris.Persistence`.
- T-ARCH-MEM-02: `Iris.Domain.Memories.*` does not reference EF Core or `Iris.Persistence`.
- T-ARCH-MEM-03: `Iris.Desktop` does not reference `IMemoryRepository` or `IrisDbContext` directly.
- T-ARCH-MEM-04: `Iris.Application.Memory.*` does not reference `MemoryEntity`.

### 11.5 Existing tests

- All 155/155 currently green tests must remain green.
- `PromptBuilder` tests that verify the empty-memory case must continue to pass without modification (FR-019).
- Tests that newly inject memory must use a fake `IMemoryRepository`.

### 11.6 Manual smoke (Phase 7-style)

- M-MEM-01: Launch Desktop, type "Запомни, что мой любимый язык — C#.", confirm chat works (memory remember API may be invoked from a UI affordance or via Desktop facade — exact UX is a design decision).
- M-MEM-02: Open `MemoryView`, see the memory listed.
- M-MEM-03: Send a chat message asking about the remembered fact; observe the assistant response references it (model is non-deterministic; pass criterion is that the memory block is present in the prompt, observable via debug logs or via deterministic prompt-tap test).
- M-MEM-04: Use "Forget" on the memory; confirm it disappears from the list and is not injected on next chat turn.
- M-MEM-05: Restart application; confirm any non-forgotten memory persists across restarts.

Manual smoke is acceptance evidence, not a unit test substitute.

### 11.7 Verification commands

- `dotnet build .\Iris.slnx` must produce 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx` must pass with all 155 + new memory tests.
- `dotnet format .\Iris.slnx --verify-no-changes` must exit 0.
- Architecture tests in CI must pass.

## 12. Documentation and Memory Requirements

- **DOC-001:** After implementation, append a Phase 8 entry to `.agent/PROJECT_LOG.md` with: phase name, files changed, validation status, remaining v2 work.
- **DOC-002:** Update `.agent/overview.md` Current Phase, Working Status, Next Step.
- **DOC-003:** No rewrite of `mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md`. A short status note may be added near §6 Phase 8 confirming v1 delivery, but the roadmap document itself stays the authority for v2 scope.
- **DOC-004:** Add a brief migration note to `docs/architecture.md` (if appropriate) describing that `IrisDbContext` now owns `Memories`. Actual placement decided in `/design`.
- **DOC-005:** Manual smoke results recorded in `.agent/log_notes.md` if any failure or anomaly is observed.
- **DOC-006:** No new files in `mem_library/` for v1.
- **DOC-007:** Updates to memory files are out-of-scope for `/spec`, `/design`, `/plan`, `/audit`. They occur during `/implement` (per `agent-memory.md` write policy) or `/update-memory`.

## 13. Acceptance Criteria

- [ ] `Memory` aggregate exists in `Iris.Domain.Memories` with documented invariants (FR-001..FR-011, T-DOM-MEM-01..09).
- [ ] All five handlers (`Remember`, `Forget`, `Update`, `Retrieve`, `ListActive`) exist in `Iris.Application.Memory.*` with full unit-test coverage.
- [ ] `MemoryContextBuilder` exists and is consumed by `PromptBuilder` (FR-017, FR-018).
- [ ] `IMemoryRepository` is a real, public, non-empty port; concrete `MemoryRepository` lives in `Iris.Persistence` with EF Core configuration and a migration that creates the `Memories` table.
- [ ] Russian default language behavior is byte-identical when no relevant memories exist (FR-019, T-APP-PROMPT-01).
- [ ] When memories exist, the prompt contains a memory block in system-role messages only (FR-018, FR-020, T-APP-PROMPT-02/03).
- [ ] Desktop `MemoryView` shows active memories and supports Forget; navigation reaches it from the shell.
- [ ] Architecture tests prevent Desktop from calling `IMemoryRepository`/`IrisDbContext` directly and prevent Application from referencing `MemoryEntity` (T-ARCH-MEM-01..04).
- [ ] `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- [ ] `dotnet test .\Iris.slnx` → all green (existing 155 + new memory tests).
- [ ] `dotnet format .\Iris.slnx --verify-no-changes` → exit 0.
- [ ] CI workflow runs and passes.
- [ ] No new project, no new NuGet package, no new `InternalsVisibleTo`.
- [ ] Out-of-scope placeholders (`MemoryEmbedding*`, `MemoryConsolidation*`, `MemoryRanking*`, `MemoryAudit*`, `MemoryExtraction*`, `MemoryEndpoints*`, `MemoryApiMapper`, `MemoryEmbeddingTask`, `MemoryConsolidationTask`) remain empty placeholders.
- [ ] Manual smoke M-MEM-01..05 recorded with results.

## 14. Open Questions

The following decisions are deferred to `/design` and do not block this specification:

1. Exact shape of `IMemoryRepository` (separate methods vs. a small query object).
2. Whether the prompt memory block is appended as an additional `system`-role message or merged into the existing language-policy system message.
3. Exact wording of the memory block prefix label (e.g. "Известные факты:" / "What I remember:").
4. Whether `MemoryContextBuilder` selection takes a hard top-N or a (top-N + recency window) hybrid.
5. Whether Desktop "Remember" affordance is an explicit button + dialog, or whether v1 only supports remembering through a future natural-language command pipeline (Phase 9+). For v1, an explicit affordance is acceptable.
6. Folder layout inside `Iris.Application.Memory` (`Commands/`, `Queries/`, `Context/`, `Contracts/` vs. roadmap's flat naming).
7. Index strategy for the `Memories` table.

The following are blocking only if the answer is "no":

- None identified.

If the user wants v1 to **also** wire memory into API endpoints, scope expands and this spec must be revised. Current scope per roadmap §6 Phase 8 is Desktop + Application + Domain + Persistence only.
