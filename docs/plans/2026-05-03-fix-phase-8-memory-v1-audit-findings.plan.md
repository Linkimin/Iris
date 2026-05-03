# Implementation Plan: Fix Phase 8 Memory v1 Audit Findings

## 1. Plan Goal

Реализовать утверждённую спецификацию + дизайн «Fix Phase 8 Memory v1 Audit Findings» в восемь фаз: восстановить функциональность Desktop UI памяти (P1-001/P1-002 + P2-005), затем закрыть три тест-coverage gap'а (P1-003/P1-004/P1-005), затем добавить регрессионный Desktop-тест, обновить агентскую память и провести финальную верификацию.

Цель — после реализации запуск `/audit` Phase 8 Memory v1 на новом HEAD должен вернуть **Approved** или **Approved with P2 backlog**, без P0/P1 находок.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `Specification: Fix Phase 8 Memory v1 Audit Findings` (создана в этой сессии; пользователь не запросил `/save-spec`, артефакт остаётся в истории сообщений).
- **Design:** `Architecture Design: Fix Phase 8 Memory v1 Audit Findings` (создан в этой сессии; пользователь не запросил `/save-design`).
- **Source audit:** `docs/audits/2026-05-02-phase-8-memory-v1.audit.md` (409 строк, P1-001..P1-005 + P2-001..P2-008).
- **Source artifacts:** `docs/specs/2026-05-02-phase-8-memory-v1.spec.md`, `docs/designs/2026-05-02-phase-8-memory-v1.design.md`, `docs/plans/2026-05-02-phase-8-memory-v1.plan.md`.
- **Project rules:** `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/dotnet.md`, `.opencode/rules/verification.md`, `.opencode/rules/memory.md`.
- **Architecture doc:** `.agent/architecture.md`.
- **Existing test fakes:** `FakeIrisApplicationFacade` в `tests/Iris.IntegrationTests/Testing/`, `FakeUnitOfWork`/`FakeClock`/`FakeMemoryRepository` локально в `SendMessageHandlerTests.cs` и `DependencyInjectionTests.cs`, `StubMemoryRepository` в `PromptBuilderTests.cs`, `PersistenceTestContextFactory` в `tests/Iris.IntegrationTests/Persistence/`.

### Assumptions

- HEAD остаётся `6955a4f` на ветке `feat/avatar-v1-and-opencode-v2` либо реализация ведётся в отдельном worktree; dirty-tree (29 M + 12 ??) на момент старта реализации идентичен описанному в `/debug` Phase 1 evidence.
- `Iris.Application.Memory.Contracts.MemoryDto` имеет публично-видимые свойства `Id`, `Content`, `Kind`, `Importance`, `Status`, `CreatedAt`, `UpdatedAt` (подтверждено через `FakeIrisApplicationFacade` line 50-72: используется как public ctor с этими параметрами).
- `Iris.Application.Memory.Commands.RememberMemoryResult` и `UpdateMemoryResult` — record-types с public ctor (`MemoryDto`), `ForgetMemoryResult` отсутствует/возвращается `Result` (см. `FakeIrisApplicationFacade.ForgetAsync` возвращает `Task<Result>`).
- Существующая логика `FakeIrisApplicationFacade.ListActiveMemoriesAsync` возвращает `Array.Empty<MemoryDto>()` без поддержки enqueue/calls — её нужно расширить (но контракт `IIrisApplicationFacade` не меняется).
- Архитектурные тесты `MemoryBoundaryTests` (4 теста) существуют и продолжают работать.
- Сборка/тесты на момент старта Phase 0 повторяют картину аудита: build 0/0, tests 175/175, format clean per-project.

### Documentation Discovery

`docs/specs`, `docs/designs`, `docs/plans`, `docs/audits` существуют. На текущий момент `/save-*` не вызывался в этой сессии, поэтому новые spec/design артефакты, описанные здесь, существуют только в conversation history.

## 3. Scope Control

### In Scope

- Изменения src в `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` и `src/Iris.Desktop/Views/MemoryView.axaml` (P1-001 + P1-002 + P2-005).
- Создание тестов:
  - `tests/Iris.Application.Tests/Memory/Commands/*.cs` (≥ 5 файлов или 1 объединённый, ≥ 5 тестов).
  - `tests/Iris.Application.Tests/Memory/Context/MemoryContextBuilderTests.cs` (2 теста).
  - `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` (3 теста).
  - `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` (3 теста).
  - `tests/Iris.IntegrationTests/Desktop/MemoryViewModelTests.cs` (2 теста).
- Расширение `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs` (добавление call-recording + enqueue-результатов для memory-методов; внутренний контракт, не меняет `IIrisApplicationFacade`).
- Обновление агентской памяти: `.agent/PROJECT_LOG.md`, `.agent/overview.md`, `.agent/log_notes.md`.

### Out of Scope

- P2-002 (memory-degradation regression test для `SendMessageHandler`).
- P2-003 (удаление неиспользуемого `MemoryOptions` из `RememberExplicitFactHandler`).
- P2-004 (LIKE-wildcard escaping в `MemoryRepository.SearchActiveAsync`).
- P2-006 (`MemoryPromptFormatter` → `sealed`).
- P2-007 (verbose type qualifier в `MemoryViewModel`).
- P2-008 (`OperationCanceledException` exclusion в `SendMessageHandler` memory-block catch).
- P2-001 (NL «запомни/забудь» pipeline) — отложено spec'ом v1 на Phase 9+.
- Manual smoke M-MEM-01..05 — выполняется оператором живой Desktop-сессией; план только заявляет требование readiness, см. Phase 7.
- Создание нового `Iris.Testing.Shared` проекта (требует Gate E на отдельный architectural review).
- Обновление `mem_library/**`.
- Изменения в `docs/specs/`, `docs/designs/`, `docs/plans/`, `docs/audits/` уже сохранённых артефактов (новый audit-документ создаётся только при отдельном `/audit` запуске).

### Forbidden Changes

- Любое изменение `src/Iris.Domain/**`, `src/Iris.Application/**` (кроме нулевого нет — все Application изменения — out of scope), `src/Iris.Persistence/**`, `src/Iris.ModelGateway/**`, `src/Iris.Infrastructure/**`, `src/Iris.Shared/**`, `src/Iris.Api/**`, `src/Iris.Worker/**`, `src/Iris.SiRuntimeGateway/**`, `src/Iris.Perception/**`, `src/Iris.Tools/**`, `src/Iris.Voice/**`.
- Изменение `IIrisApplicationFacade` или `IrisApplicationFacade`.
- Изменение `MemoryViewModelItem` shape.
- Изменение `MainWindow.axaml` или `MainWindowViewModel`.
- Изменения `Iris.Desktop/DependencyInjection.cs` (Singleton остаётся как есть).
- Любые новые project references.
- Любые новые NuGet packages или central package version updates.
- Любые `InternalsVisibleTo` атрибуты.
- Любые EF migrations.
- `git push`, `git reset --hard`, `git clean`, удаление существующих файлов.
- Изменение существующих 175 тестов (за исключением вынужденного расширения `FakeIrisApplicationFacade.cs`, что инфраструктурно для интеграционных тестов).

## 4. Implementation Strategy

Стратегия следует §10 Suggested Fix Order аудита, скорректированному под scope spec'a:

1. **UI первой**, поскольку без работоспособного UI manual smoke бессмыслен и regression-тест Desktop невозможно адекватно написать (P1-002 + P2-005 → P1-001).
2. **Тесты Application/Persistence** добавляются после UI, потому что их разработка не блокирует UI и они сами по себе изолированные.
3. **Desktop integration regression тест** добавляется последним из тестов, потому что для него нужен расширенный `FakeIrisApplicationFacade`.
4. **Финальная верификация** + memory update — отдельная фаза по правилам `iris-verification` и `iris-memory`.

Все 8 фаз — узкие и обратимые. В каждой фазе верификация — `dotnet build` + `dotnet test` (фокусированные), полная solution-верификация — только в Phase 7. Это снижает feedback loop.

Между фазами нет тяжёлых зависимостей: Phase 1 → Phase 2 (XAML биндит то, что Phase 1 добавил); Phase 3..6 могут идти в любом порядке после Phase 2 (но порядок ниже даёт самую быструю верификацию).

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Подтвердить factual state перед редактированием: HEAD, dirty-tree, baseline build/test, наличие fakes/factories, форму существующих типов.

#### Files to Inspect

- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` (185 lines).
- `src/Iris.Desktop/Views/MemoryView.axaml` (64 lines).
- `src/Iris.Desktop/Views/MemoryView.axaml.cs` (12 lines).
- `src/Iris.Desktop/Models/MemoryViewModelItem.cs`.
- `src/Iris.Desktop/DependencyInjection.cs` (line 79: `AddSingleton<MemoryViewModel>`).
- `src/Iris.Application/Memory/Contracts/MemoryDto.cs`.
- `src/Iris.Application/Memory/Commands/RememberMemoryResult.cs`, `ForgetMemoryHandler.cs`, `UpdateMemoryHandler.cs`, `UpdateMemoryResult.cs`, `RememberExplicitFactHandler.cs`.
- `src/Iris.Application/Memory/Queries/ListActiveMemoriesHandler.cs`.
- `src/Iris.Application/Memory/Context/MemoryContextBuilder.cs`, `MemoryPromptFormatter.cs`.
- `src/Iris.Application/Memory/Options/MemoryOptions.cs`.
- `src/Iris.Application/Abstractions/Persistence/IMemoryRepository.cs`.
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs`.
- `src/Iris.Persistence/Repositories/MemoryRepository.cs`.
- `src/Iris.Persistence/Configurations/MemoryEntityConfiguration.cs`.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (для `StubMemoryRepository` pattern).
- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` (для `FakeMemoryRepository`/`FakeUnitOfWork`/`FakeClock` patterns).
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` (для альтернативного `FakeMemoryRepository`).
- `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs`.
- `tests/Iris.IntegrationTests/Persistence/ConversationRepositoryTests.cs` (паттерн round-trip).
- `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs`.
- `tests/Iris.Architecture.Tests/MemoryBoundaryTests.cs`.
- `.agent/overview.md`, `.agent/PROJECT_LOG.md`, `.agent/log_notes.md`.

#### Files Likely to Edit

None.

#### Steps

1. `git status --short --branch` — подтвердить ветку и dirty-tree.
2. `git log --oneline -3` — подтвердить HEAD `6955a4f`.
3. `dotnet build .\Iris.slnx --nologo --verbosity minimal` — baseline 0/0.
4. `dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo` — baseline 175/175.
5. Прочитать перечисленные файлы; зафиксировать публичные contract-shapes (MemoryDto, RememberMemoryResult, ForgetMemoryHandler, IMemoryRepository).
6. Подтвердить, что `FakeIrisApplicationFacade.ListActiveMemoriesAsync` сейчас возвращает `Array.Empty<MemoryDto>()` без enqueue/calls (нужно расширить в Phase 6).

#### Verification

- Команды 1-4 выполняются без ошибок.
- Все ожидаемые типы и методы найдены; spec/design assumptions подтверждены.

#### Rollback

No code changes.

#### Acceptance Checkpoint

- Phase 0 пройден, если все 6 шагов завершены и `dotnet test` показал 175/175.

---

### Phase 1 — Add `ForgetCommand` to `MemoryViewModel`

#### Goal

Закрыть P1-002 (бэкэнд) и P2-005: добавить `IAsyncRelayCommand<MemoryId> ForgetCommand` на `MemoryViewModel`. Соответствует FR-004 / FR-005 / FR-008 + Component Design §6.

#### Files to Inspect

- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` (для текущей структуры RememberCommand-инициализации в ctor; чтобы воспроизвести стиль).

#### Files Likely to Edit

- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` — добавить:
  - public свойство `IAsyncRelayCommand<MemoryId> ForgetCommand { get; }`.
  - инициализацию в ctor: `ForgetCommand = new AsyncRelayCommand<MemoryId>(id => ForgetAsync(id, default));`.

#### Files That Must Not Be Touched

- `src/Iris.Desktop/Views/MemoryView.axaml` (Phase 2).
- `src/Iris.Desktop/Models/MemoryViewModelItem.cs`.
- `src/Iris.Desktop/Services/IIrisApplicationFacade.cs` / `IrisApplicationFacade.cs`.
- Любые `src/Iris.Application/**`, `src/Iris.Persistence/**`.

#### Steps

1. Прочитать текущий `MemoryViewModel.cs`.
2. Добавить `using` если требуется (`AsyncRelayCommand<T>` — уже есть `using CommunityToolkit.Mvvm.Input;`).
3. Объявить публичное свойство `ForgetCommand` рядом с существующим `RememberCommand`.
4. В конструкторе инициализировать `ForgetCommand` лямбдой `id => ForgetAsync(id, default)`.
5. Не трогать `ForgetAsync` метод (остаётся `public async Task` для прямого вызова из тестов).

#### Verification

- `dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj --nologo --verbosity minimal` — 0 errors.
- `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-build --filter "FullyQualifiedName~Desktop" --nologo` — все Desktop-тесты остаются зелёными.

#### Rollback

- `git diff src/Iris.Desktop/ViewModels/MemoryViewModel.cs` — ревёртить добавленные строки. Никаких других файлов не затронуто.

#### Acceptance Checkpoint

- `MemoryViewModel.ForgetCommand` присутствует как `IAsyncRelayCommand<MemoryId>`.
- Build clean. Все 175 тестов остаются зелёными.

---

### Phase 2 — Bind Forget Button in `MemoryView.axaml`

#### Goal

Закрыть P1-002 (XAML-сторона). Соответствует FR-004 / FR-005 + Contract Design §7.

#### Files to Inspect

- `src/Iris.Desktop/Views/MemoryView.axaml` (lines 22-54: `ItemsControl`+`DataTemplate`).
- Подтвердить, что `Grid.Column="1"` `<Button Content="Забыть">` находится внутри `<DataTemplate DataType="models:MemoryViewModelItem">`.

#### Files Likely to Edit

- `src/Iris.Desktop/Views/MemoryView.axaml` — на кнопке «Забыть» (line 47-49):
  - добавить `Command="{Binding $parent[ItemsControl].DataContext.ForgetCommand}"`.
  - добавить `CommandParameter="{Binding Id}"`.

#### Files That Must Not Be Touched

- `src/Iris.Desktop/Views/MemoryView.axaml.cs` (этот вариант дизайна не использует view-attach).
- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` (уже зафиксирован Phase 1).
- `src/Iris.Desktop/Models/MemoryViewModelItem.cs`.
- `src/Iris.Desktop/Views/MainWindow.axaml`.

#### Steps

1. Открыть `MemoryView.axaml`.
2. Локализовать кнопку «Забыть» в `<DataTemplate>` (line 47-49).
3. Добавить два атрибута: `Command` через `RelativeSource` к ItemsControl.DataContext + `CommandParameter` к локальному `Id`.
4. Сохранить файл.

#### Verification

- `dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj --nologo --verbosity minimal` — 0 errors. (Avalonia валидирует XAML на этапе билда.)
- `dotnet format .\src\Iris.Desktop\Iris.Desktop.csproj --verify-no-changes` — pass.

#### Rollback

- Удалить добавленные атрибуты `Command=` и `CommandParameter=` с кнопки.

#### Acceptance Checkpoint

- AXAML парсится без AVLN-ошибок.
- `Iris.Desktop.csproj` build clean.

---

### Phase 3 — Trigger `LoadMemoriesAsync` from Constructor

#### Goal

Закрыть P1-001: автоматическая загрузка списка при создании `MemoryViewModel`. Соответствует FR-001 / FR-002 + Component Design §6 (Load Trigger = ctor fire-and-forget) + concurrent load guard (FR-008).

#### Files to Inspect

- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` (после Phase 1).

#### Files Likely to Edit

- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs`:
  - Добавить concurrent guard в `LoadMemoriesAsync`: `if (IsLoading) return;` в начало метода.
  - В конец конструктора добавить fire-and-forget вызов: `_ = LoadMemoriesAsync(CancellationToken.None);`.

#### Files That Must Not Be Touched

- `src/Iris.Desktop/Views/MemoryView.axaml.cs` (остаётся 12-line stub; view-attach вариант отвергнут дизайном).
- `src/Iris.Desktop/DependencyInjection.cs` (lifetime остаётся Singleton).
- Любые другие src.

#### Steps

1. Прочитать текущий `LoadMemoriesAsync`.
2. В первой строке метода добавить guard: `if (IsLoading) return;`.
3. В конструкторе после инициализации `RememberCommand` (и `ForgetCommand` из Phase 1) добавить `_ = LoadMemoriesAsync(CancellationToken.None);`.
4. Подтвердить, что `try/catch/finally` в `LoadMemoriesAsync` уже корректно ловит `OperationCanceledException` и обрабатывает любые исключения через `ErrorMessage` (никакого исключения не должно покинуть fire-and-forget Task).

#### Verification

- `dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj --nologo --verbosity minimal` — 0 errors.
- `dotnet test .\Iris.slnx --no-build --filter "FullyQualifiedName~Memory" --nologo` — существующие memory-related тесты зелёные.

#### Rollback

- Удалить guard и fire-and-forget вызов.

#### Acceptance Checkpoint

- Conctructor вызывает `LoadMemoriesAsync` (fire-and-forget).
- `LoadMemoriesAsync` имеет concurrent guard `if (IsLoading) return;`.
- Все остальные тесты остаются зелёными.

---

### Phase 4 — Add Application Memory Handler Tests (`tests/Iris.Application.Tests/Memory/`)

#### Goal

Закрыть P1-003. Создать минимум 7 тестов из spec §11.1 (T-APP-MEM-01, -06, -07, -08, -12, -21, -22). Соответствует FR-009..FR-015.

#### Files to Inspect

- `src/Iris.Application/Memory/Commands/RememberExplicitFactHandler.cs`, `ForgetMemoryHandler.cs`, `UpdateMemoryHandler.cs` (для контракта input/output и error-кодов).
- `src/Iris.Application/Memory/Context/MemoryContextBuilder.cs` (для логики top-N + empty handling).
- `src/Iris.Application/Memory/Options/MemoryOptions.cs` (для `MaxMemoriesInPrompt` или эквивалентного свойства).
- `src/Iris.Application/Abstractions/Persistence/IMemoryRepository.cs` (5 методов).
- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` (lines 460-540: `FakeUnitOfWork`, `FakeMemoryRepository`, `FakeClock` — образец).
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` (lines 120-180: альтернативный `FakeMemoryRepository`).
- `tests/Iris.Application.Tests/Iris.Application.Tests.csproj` (подтвердить `<EnableDefaultCompileItems>true</EnableDefaultCompileItems>` или эквивалент — глоб-инклюд `*.cs`).

#### Files Likely to Edit / Create

- **Create new:** `tests/Iris.Application.Tests/Memory/Commands/RememberExplicitFactHandlerTests.cs` (T-APP-MEM-01).
- **Create new:** `tests/Iris.Application.Tests/Memory/Commands/ForgetMemoryHandlerTests.cs` (T-APP-MEM-06, -07, -08 — три факта в одном классе).
- **Create new:** `tests/Iris.Application.Tests/Memory/Commands/UpdateMemoryHandlerTests.cs` (T-APP-MEM-12).
- **Create new:** `tests/Iris.Application.Tests/Memory/Context/MemoryContextBuilderTests.cs` (T-APP-MEM-21, -22).
- **Create new:** `tests/Iris.Application.Tests/Memory/FakeInfrastructure.cs` — локальные `FakeUnitOfWork`, `FakeClock`, `FakeMemoryRepository` (Dictionary-backed) для переиспользования внутри Memory-тестов.

#### Files That Must Not Be Touched

- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` (существующие fakes остаются на месте; не извлекаем в shared lib — см. spec §3.3, design §15).
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs`.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (Phase 5).
- Любые `src/Iris.Application/**`.

#### Steps

1. Создать каталог `tests/Iris.Application.Tests/Memory/Commands/` и `tests/Iris.Application.Tests/Memory/Context/`.
2. Создать `FakeInfrastructure.cs` с тремя `internal sealed class`: `FakeUnitOfWork : IUnitOfWork`, `FakeClock : IClock`, `FakeMemoryRepository : IMemoryRepository` (Dictionary<MemoryId, Memory> backing store + call counters).
3. Создать `RememberExplicitFactHandlerTests.cs`:
   - **T-APP-MEM-01**: Happy path — handler принимает валидный content; `FakeMemoryRepository.AddAsync` вызван 1 раз; `FakeUnitOfWork.CommitAsync` вызван 1 раз; `Result.IsSuccess`; result содержит `MemoryDto` с активным статусом.
4. Создать `ForgetMemoryHandlerTests.cs`:
   - **T-APP-MEM-06**: Active memory → forgotten happy path (UpdateAsync + CommitAsync вызваны).
   - **T-APP-MEM-07**: Missing id → not_found error code; UpdateAsync НЕ вызван.
   - **T-APP-MEM-08**: Already-forgotten → idempotent Success; UpdateAsync и CommitAsync НЕ вызваны (R-004 mitigation).
5. Создать `UpdateMemoryHandlerTests.cs`:
   - **T-APP-MEM-12**: Update on Forgotten → conflict error code; UpdateAsync НЕ вызван.
6. Создать `MemoryContextBuilderTests.cs`:
   - **T-APP-MEM-21**: empty active → `IReadOnlyList.Count == 0` без exception.
   - **T-APP-MEM-22**: > top-N memories → result limited to top-N (взять `MemoryOptions.MaxMemoriesInPrompt` или соответствующее property; если property называется иначе — использовать ту, что есть в `MemoryOptions.Default`).

#### Verification

- `dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --nologo --verbosity minimal` — 0 errors.
- `dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Memory" --nologo` — 7 новых тестов pass.
- `dotnet format .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --verify-no-changes` — pass.

#### Rollback

- Удалить новый каталог `tests/Iris.Application.Tests/Memory/`. Существующие тесты остаются нетронутыми.

#### Acceptance Checkpoint

- ≥ 7 новых memory unit-тестов добавлены и зелёные.
- Test count: 175 → ≥ 182.

---

### Phase 5 — Add PromptBuilder Memory Injection Tests

#### Goal

Закрыть P1-005. Соответствует FR-016..FR-018.

#### Files to Inspect

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` (для логики `Build` + memory injection).
- `src/Iris.Application/Memory/Context/MemoryPromptFormatter.cs` (для формата `Известные факты:`).
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (для `StubMemoryRepository`, `_stubPrompt`, `MemoryOptions`, `MemoryPromptFormatter` использование).

#### Files Likely to Edit / Create

- **Create new:** `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs`.

#### Files That Must Not Be Touched

- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs` (существующий тест остаётся).
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs`.
- `src/Iris.Application/Memory/Context/MemoryPromptFormatter.cs`.

#### Steps

1. Создать `PromptBuilderMemoryTests.cs`. Можно переиспользовать `StubMemoryRepository` через `internal` (если в одном assembly), либо создать локальный `StubMemoryRepositoryWithMemories` принимающий список `Memory` доменных и возвращающий их из `ListActiveAsync`.
2. **T-APP-PROMPT-01**: Empty memory list → `PromptBuilder.Build(...)` возвращает `PromptBuildResult` с `Messages.Length == старый_baseline`. Использовать `Assert.Equal` на длину; убедиться, что нет ChatModelMessage с substring `"Известные факты:"` ни в одной роли.
3. **T-APP-PROMPT-02**: Memory list = [memory1, memory2] → `Messages` содержит ровно одно сообщение с `Role == ChatModelRole.System` после baseline-system, чьё `Content` содержит и `"Известные факты:"`, и контент memory1, и контент memory2.
4. **T-APP-PROMPT-03**: Memory list = [memory1] → ни одно `Message` с `Role == ChatModelRole.User` не содержит substring `"Известные факты:"` или контент memory1.

#### Verification

- `dotnet build .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --nologo --verbosity minimal` — 0 errors.
- `dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --filter "FullyQualifiedName~PromptBuilderMemory" --nologo` — 3 новых теста pass.
- Существующий `Build_IncludesSystemMessageHistoryAndCurrentUserMessage` остаётся зелёным.

#### Rollback

- Удалить `PromptBuilderMemoryTests.cs`.

#### Acceptance Checkpoint

- 3 новых prompt-тестов зелёные.
- Test count: ≥ 182 → ≥ 185.

---

### Phase 6 — Add Persistence Integration Tests for `MemoryRepository`

#### Goal

Закрыть P1-004. Соответствует FR-019..FR-021.

#### Files to Inspect

- `src/Iris.Persistence/Repositories/MemoryRepository.cs` (для шейпов `AddAsync`, `GetByIdAsync`, `ListActiveAsync`, `SearchActiveAsync`, `UpdateAsync`).
- `src/Iris.Persistence/Configurations/MemoryEntityConfiguration.cs` (для `COLLATE NOCASE`).
- `src/Iris.Persistence/Mapping/MemoryMapper.cs` (для enum→int, DateTimeOffset?↔ticks).
- `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs` (для `CreateInitializedContextAsync`, `CreateContext` методов).
- `tests/Iris.IntegrationTests/Persistence/ConversationRepositoryTests.cs` (round-trip pattern; `SqliteConnection.ClearAllPools()` если присутствует).

#### Files Likely to Edit / Create

- **Create new:** `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs`.

#### Files That Must Not Be Touched

- Существующие тесты в `tests/Iris.IntegrationTests/Persistence/`.
- `src/Iris.Persistence/**`.

#### Steps

1. Создать `MemoryRepositoryTests.cs`, повторяя стиль `ConversationRepositoryTests.cs` (separated write+read контексты).
2. **T-PERS-MEM-01**: Round-trip — создать `Memory` через `Memory.Create(...)` (или `Memory.Rehydrate` если public per audit §5 Notes) с mixed-case Cyrillic content (например, `"Айрис любит Котиков"`); добавить через `MemoryRepository.AddAsync` + `EfUnitOfWork.CommitAsync`; в read-context — `GetByIdAsync` возвращает идентичный объект (Id, Content, Kind, Importance, Status, CreatedAt, UpdatedAt).
3. **T-PERS-MEM-03**: Создать 2 active memories + 1 forgotten; `ListActiveAsync` возвращает только 2 active.
4. **T-PERS-MEM-04**: Создать memory с content `"Айрис помнит файлы"`; вызвать `SearchActiveAsync("айрис")` (lowercase); результат содержит созданную memory (case-insensitive Cyrillic match через `COLLATE NOCASE`).

#### Verification

- `dotnet build .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --nologo --verbosity minimal` — 0 errors.
- `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-build --filter "FullyQualifiedName~MemoryRepository" --nologo` — 3 новых теста pass.
- `dotnet format .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --verify-no-changes` — pass.

#### Rollback

- Удалить `MemoryRepositoryTests.cs`.

#### Acceptance Checkpoint

- 3 новых persistence integration теста зелёные.
- SQLite-pool корректно очищается (нет file-lock на cleanup).
- Test count: ≥ 185 → ≥ 188.

---

### Phase 7 — Add Desktop Regression Tests + Extend `FakeIrisApplicationFacade`

#### Goal

Закрыть FR-022/FR-023 (regression тесты для P1-001/P1-002). Расширить `FakeIrisApplicationFacade` для записи memory-вызовов и enqueue-результатов.

#### Files to Inspect

- `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs` (текущее состояние: возвращает stub-success, без call-recording для memory-методов).
- `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` (паттерн ViewModel test с DI).
- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` (после Phase 1-3).

#### Files Likely to Edit / Create

- **Modify:** `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs`:
  - Добавить `List<MemoryId> ForgetCalls` (или `List<ForgetCall>`).
  - Добавить `Queue<Result<IReadOnlyList<MemoryDto>>> _listResults` + метод `EnqueueListSuccess(params MemoryDto[])`.
  - В `ForgetAsync` записывать вызовы в `ForgetCalls` перед возвратом результата.
  - В `ListActiveMemoriesAsync` возвращать enqueued result (если есть) или fallback на `Array.Empty`.
- **Create new:** `tests/Iris.IntegrationTests/Desktop/MemoryViewModelTests.cs`.

#### Files That Must Not Be Touched

- `src/Iris.Desktop/Services/IIrisApplicationFacade.cs` (контракт неизменен).
- `src/Iris.Desktop/Services/IrisApplicationFacade.cs`.
- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` (зафиксирован Phase 1-3).
- Существующие методы `SendMessageAsync` в FakeIrisApplicationFacade — не менять (только добавлять).

#### Steps

1. Открыть `FakeIrisApplicationFacade.cs`. Не удалять и не менять существующие public свойства/методы (это сломает существующие 82 интеграционных теста).
2. Дополнить:
   - `public List<MemoryId> ForgetCalls { get; } = new();`
   - `private readonly Queue<Result<IReadOnlyList<MemoryDto>>> _listMemoryResults = new();`
   - `public void EnqueueListMemoriesSuccess(params MemoryDto[] memories) { _listMemoryResults.Enqueue(Result<IReadOnlyList<MemoryDto>>.Success(memories)); }`
   - В `ForgetAsync` — `ForgetCalls.Add(id);` перед `return`.
   - В `ListActiveMemoriesAsync` — `if (_listMemoryResults.Count > 0) return Task.FromResult(_listMemoryResults.Dequeue()); else return Task.FromResult(Result<IReadOnlyList<MemoryDto>>.Success(Array.Empty<MemoryDto>()));`.
3. Создать `MemoryViewModelTests.cs`. Use direct ctor instantiation (без DI) — паттерн уже используется в `AvatarViewModelTests`.
4. **T-DESK-MEM-01**: Setup `FakeIrisApplicationFacade.EnqueueListMemoriesSuccess(dto1, dto2)`. Создать `MemoryViewModel(facade)`. Дождаться завершения fire-and-forget Task (poll `IsLoading == false` или `Memories.Count > 0`, with timeout). Assert `Memories.Count == 2`, `ErrorMessage == ""`.
5. **T-DESK-MEM-02**: Setup `FakeIrisApplicationFacade.EnqueueListMemoriesSuccess(dto1, dto2)` (для initial load) + дополнительный `EnqueueListMemoriesSuccess(dto2)` (для reload after forget). Создать VM. Дождаться initial load. Вызвать `viewModel.ForgetCommand.ExecuteAsync(dto1.Id)`. Assert `facade.ForgetCalls.Count == 1`, `facade.ForgetCalls[0] == dto1.Id`, и (после reload) `Memories.Count == 1`.

#### Verification

- `dotnet build .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --nologo --verbosity minimal` — 0 errors. (Если меняем `FakeIrisApplicationFacade`, существующие 82 теста должны продолжать компилироваться и проходить.)
- `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-build --filter "FullyQualifiedName~MemoryViewModel" --nologo` — 2 новых теста pass.
- `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-build --nologo` — все 82 + 3 (Phase 6) + 2 = 87 интеграционных теста pass.

#### Rollback

- Удалить `MemoryViewModelTests.cs`. Откатить добавления в `FakeIrisApplicationFacade.cs` (удалить `ForgetCalls`, `_listMemoryResults`, `EnqueueListMemoriesSuccess`; вернуть `ListActiveMemoriesAsync` к исходной форме).

#### Acceptance Checkpoint

- 2 новых Desktop integration теста зелёные.
- Существующие 82 + 3 теста зелёные.
- `FakeIrisApplicationFacade` backwards-compatible.
- Test count: ≥ 188 → ≥ 190.

---

### Phase 8 — Verification, Memory Update, Smoke Handoff

#### Goal

Финальная solution-wide верификация (Gate D), agent memory update (Gate G), и handoff для manual smoke.

#### Files to Inspect

- `.agent/overview.md`, `.agent/PROJECT_LOG.md`, `.agent/log_notes.md` (текущее состояние).

#### Files Likely to Edit

- `.agent/PROJECT_LOG.md` — добавить датированную запись (формат как в существующих записях, например, 2026-05-02 entry pattern):
  - **Changed**: список Phase 1-7 изменений (UI fixes + 5 наборов тестов).
  - **Files**: список новых/изменённых файлов.
  - **Validation**: build/test/format результаты.
  - **Notes**: закрытие P1-001..P1-005, ссылка на этот план, P2-002..P2-008 остаются в backlog.
  - **Next**: запуск `/audit`; проведение manual smoke M-MEM-01..05.
- `.agent/overview.md` — обновить:
  - `Current Phase` — отражает завершение Phase 8 Memory v1 P1-фиксов.
  - `Current Working Status` — `clean working tree (если closed) или fix branch ready for PR`.
  - `Next Immediate Step` — manual smoke M-MEM-01..05 + `/audit` re-run.
  - `Current Blockers` — секция P1 пуста; секция P2 сохраняется (P2-002..P2-008 не решены).
- `.agent/log_notes.md` — пометить запись «2026-05-02 — Phase 8 audit found 5 P1 issues» как RESOLVED с краткой ссылкой на этот fix; обновить запись «Phase 8 Memory v1 manual smoke not performed» (статус "now meaningful — pending operator").

#### Files That Must Not Be Touched

- `.agent/architecture.md`, `.agent/first-vertical-slice.md`, `.agent/README.md`, `.agent/mem_library/**` — никаких изменений (см. spec §12 DOC-004).
- `docs/audits/2026-05-02-phase-8-memory-v1.audit.md` — НЕ модифицировать (новый audit doc — отдельный артефакт при будущем `/audit`).
- `docs/specs/`, `docs/designs/`, `docs/plans/` — оригинальные Phase 8 артефакты unchanged. Новые spec/design/plan этой сессии сохраняются только если пользователь явно вызовет `/save-spec`, `/save-design`, `/save-plan`.

#### Steps

1. Запустить полную верификацию:
   - `dotnet build .\Iris.slnx --nologo --verbosity minimal` — ожидание 0/0.
   - `dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo` — ожидание ≥ 190/190.
   - `dotnet format .\Iris.slnx --verify-no-changes --verbosity minimal` — ожидание EXIT_CODE=0. Если timeout (как в audit §8) — fallback per-project format на 4 затронутых проекта (`Iris.Desktop`, `Iris.Application.Tests`, `Iris.IntegrationTests`, опционально `Iris.Persistence` если задет).
2. Обновить `.agent/PROJECT_LOG.md` — добавить новую запись (append-only).
3. Обновить `.agent/overview.md` — заменить только изменившиеся поля.
4. Обновить `.agent/log_notes.md` — пометить P1 запись RESOLVED.
5. `git status --short` — финальная проверка scope: ожидаемые изменения только в `src/Iris.Desktop/{ViewModels,Views}/Memory*`, новых тест-файлах в `tests/Iris.Application.Tests/Memory/`, `tests/Iris.IntegrationTests/{Persistence,Desktop}/`, `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs`, `.agent/{PROJECT_LOG,overview,log_notes}.md`.
6. **Manual smoke handoff**: документировать в финальном response, что M-MEM-01..05 теперь реализуемы и должны быть выполнены оператором живой Desktop-сессией с Ollama и чистой `iris.db` (per audit §10 step 12). Результаты будут записаны оператором отдельным `/update-memory` или внутри Phase 8 PROJECT_LOG записи (если оператор сам выполняет smoke).

#### Verification

- Все 3 solution-команды pass.
- `git status` показывает scope только в разрешённых разделах.
- `.agent/*.md` обновлены без потери существующих записей (append-only).

#### Rollback

- Если solution-verification fails: откатить последнюю фазу до Phase 7 включительно; не обновлять memory.
- Если `.agent` updates непоследовательны: использовать `git diff .agent/` для коррекции; удалять не нужно.

#### Acceptance Checkpoint

- AC-V-001..AC-V-014 spec'a выполнены (за исключением AC-V-013 manual smoke, который требует оператора).
- Готово для re-run `/audit`.

---

## 6. Testing Plan

### Unit Tests (`Iris.Application.Tests`)

- **T-APP-MEM-01** — Phase 4. `RememberExplicitFactHandler` happy path.
- **T-APP-MEM-06** — Phase 4. `ForgetMemoryHandler` active → forgotten.
- **T-APP-MEM-07** — Phase 4. `ForgetMemoryHandler` not_found.
- **T-APP-MEM-08** — Phase 4. `ForgetMemoryHandler` already-forgotten idempotent.
- **T-APP-MEM-12** — Phase 4. `UpdateMemoryHandler` on forgotten → conflict.
- **T-APP-MEM-21** — Phase 4. `MemoryContextBuilder` empty.
- **T-APP-MEM-22** — Phase 4. `MemoryContextBuilder` top-N respected.
- **T-APP-PROMPT-01** — Phase 5. `PromptBuilder` empty memory → byte-equivalent baseline.
- **T-APP-PROMPT-02** — Phase 5. `PromptBuilder` non-empty → second System message с `Известные факты:`.
- **T-APP-PROMPT-03** — Phase 5. `PromptBuilder` non-empty → no User-role с memory content.

### Integration Tests (`Iris.IntegrationTests`)

- **T-PERS-MEM-01** — Phase 6. `MemoryRepository` round-trip Cyrillic.
- **T-PERS-MEM-03** — Phase 6. `ListActiveAsync` excludes Forgotten.
- **T-PERS-MEM-04** — Phase 6. `SearchActiveAsync` case-insensitive Cyrillic.
- **T-DESK-MEM-01** — Phase 7. `MemoryViewModel` auto-loads on construction.
- **T-DESK-MEM-02** — Phase 7. `ForgetCommand` invokes facade with correct id and reloads.

### Architecture Tests

- Существующие `MemoryBoundaryTests` (4) и общие architecture tests (12) — должны остаться зелёными без модификации.

### Regression Tests

- Все 175 baseline-тестов остаются зелёными после каждой фазы.
- Существующие 82 интеграционных теста (включая зависимые от `FakeIrisApplicationFacade`) остаются зелёными после Phase 7 modifications.

### Manual Verification

- M-MEM-01..M-MEM-05 — оператор живой Desktop-сессией с Ollama и чистой `iris.db` (Phase 8 handoff). См. spec §11.8.

## 7. Documentation and Memory Plan

### Documentation Updates

- `docs/audits/2026-05-02-phase-8-memory-v1.audit.md` — НЕ модифицируется. План создаёт основу для нового audit-документа отдельным `/audit` запуском после реализации.
- Этот план, спецификация и дизайн остаются в conversation history, если пользователь не запросит `/save-spec`, `/save-design`, `/save-plan`.

### Agent Memory Updates

- Phase 8 (этого плана):
  - **Append** к `.agent/PROJECT_LOG.md` — датированная запись.
  - **Update** `.agent/overview.md` — поля Current Phase / Current Working Status / Next Immediate Step / Current Blockers.
  - **Update** `.agent/log_notes.md` — пометить запись «Phase 8 audit found 5 P1 issues» как RESOLVED; обновить статус M-MEM-01..05.
- Никаких изменений в `mem_library/**`, `architecture.md`, `first-vertical-slice.md`, `debt_tech_backlog.md` (P2 в backlog не нужно дублировать — уже зафиксировано в audit-документе).

## 8. Verification Commands

Минимально обязательная последовательность Phase 8:

```powershell
dotnet build .\Iris.slnx --nologo --verbosity minimal
dotnet test .\Iris.slnx --no-build --verbosity minimal --nologo
dotnet format .\Iris.slnx --verify-no-changes --verbosity minimal
```

Per-фазная (быстрая) верификация:

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj --nologo --verbosity minimal
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Memory" --nologo
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-build --filter "FullyQualifiedName~Memory" --nologo
```

Format fallback (если solution-format таймаутит, как в audit §8):

```powershell
dotnet format .\src\Iris.Desktop\Iris.Desktop.csproj --verify-no-changes
dotnet format .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj --verify-no-changes
dotnet format .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --verify-no-changes
```

Diagnostic (read-only):

```powershell
git status --short --branch
git log --oneline -5
git diff --stat
```

**Forbidden** в Phase 8: `dotnet format` без `--verify-no-changes`, `git push`, `git reset --hard`, `git clean`, `dotnet add/remove package`, `dotnet ef migrations add`.

## 9. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| Fire-and-forget Task в ctor бросает unobserved exception | UI silent failure, `ErrorMessage` не обновляется | Существующий `try/catch/finally` в `LoadMemoriesAsync` уже ловит всё через `ErrorMessage`. Подтвердить in Phase 3. |
| `RelativeSource $parent[ItemsControl]` биндинг не находит DataContext в test-time / design-time | Test-runtime NRE, Avalonia design-time exception | Avalonia tolerantно обрабатывает unresolved binding (no-op). Test environment не использует AXAML. Регрессия покрыта T-DESK-MEM-02. |
| `FakeIrisApplicationFacade` modifications breaking 82 existing integration tests | Phase 7 ломает baseline | Только additive изменения; не менять существующие методы; не удалять/переименовывать ни одно публичное свойство. |
| `dotnet test` на T-DESK-MEM-01 flaky из-за fire-and-forget timing | False fail в CI | Использовать polling с timeout (5s) на `IsLoading == false` или `Memories.Count > 0`. Шаблон уже устоявшийся в `AvatarViewModelTests` (см. log_notes 2026-05-01 T-04 fix). |
| SQLite file lock на cleanup в T-PERS-MEM tests | Test sporadically fails on Windows | `SqliteConnection.ClearAllPools()` перед `Dispose`/cleanup; `await using` для context (паттерн из `IrisDatabaseInitializerTests`, см. log_notes 2026-04-27). |
| `MemoryOptions.Default` не имеет `MaxMemoriesInPrompt` (имя свойства может отличаться) | T-APP-MEM-22 не компилируется | Phase 0/4 reconnaissance: прочитать `MemoryOptions.cs` и узнать точное имя property. |
| `dotnet format --verify-no-changes` solution-wide timeout (>60s) | Phase 8 verification incomplete | Per-project fallback (4 проекта) — задокументировано в spec §11.7 и audit §8. |
| Конкуррентный `LoadMemoriesAsync` через `RememberCommand`/`ForgetCommand` цепочки | Дублированные list-вызовы, неконсистентное состояние | Phase 3 guard `if (IsLoading) return;` защищает. T-DESK-MEM-02 проверяет happy path; concurrent-edge не покрывается v1 (acceptable per spec §10). |
| Audit re-run обнаруживает ранее не выявленный P1 после фикса | Cycle continues | Не митигировать заранее — если новый P1 находится, выходит за scope этого плана; запустить новый `/debug` → `/spec` → `/plan`. |
| Manual smoke M-MEM-03 (chat использует факт) проваливается из-за prompt-injection регрессии | Видно только в smoke | T-APP-PROMPT-02/03 защищают prompt-injection контракт. Если smoke провалится несмотря на тест — `/debug` для Ollama config. |

## 10. Implementation Handoff Notes

### Critical Constraints

1. **Не трогать `IIrisApplicationFacade`**. Все 4 memory-метода имеют корректную сигнатуру; нужно только грамотно использовать их с UI.
2. **Не трогать `Iris.Application/**` и `Iris.Persistence/**` src.** Все изменения src — только `Iris.Desktop/ViewModels/MemoryViewModel.cs` (Phase 1, 3) и `Iris.Desktop/Views/MemoryView.axaml` (Phase 2).
3. **Не извлекать fakes в shared lib.** Локальное дублирование — explicit design choice (spec §3.3, design §15). Создание `Iris.Testing.Shared` потребует Gate E.
4. **Не нормализовать unrelated dirty files.** Working tree содержит 29 M + 12 ?? на старте. Изменять только то, что прописано в плане.
5. **`MemoryViewModel` остаётся Singleton.** `Iris.Desktop/DependencyInjection.cs:79` не трогаем — fire-and-forget ctor-trigger срабатывает один раз при первом резолве.
6. **Build XAML на каждом этапе.** Avalonia валидирует биндинги при сборке, поэтому Phase 2 build catches typos в `RelativeSource` синтаксисе.

### Risky Areas

- **Phase 2 XAML биндинг**: синтаксис `$parent[ItemsControl].DataContext.ForgetCommand` чувствителен к namespace и whitespace; ошибка приведёт к runtime "binding not resolved" silent failure — не build error. Покрыто T-DESK-MEM-02.
- **Phase 7 polling timing**: использовать достаточно долгий timeout (5+ секунд) с малым intervals (10-50ms), как в `AvatarViewModelTests`. Fire-and-forget Task на slow CI агенте не обязан завершиться мгновенно.
- **Phase 6 Cyrillic encoding**: UTF-8 encoding файла `MemoryRepositoryTests.cs` критичен (см. log_notes 2026-05-02 mojibake regression). Использовать `Assert.Contains("Айрис", ...)` где `й` outside Windows-1252 mojibake glyph set, чтобы поймать byte-level encoding ошибки.
- **Phase 8 `.agent` updates**: append, не overwrite. Использовать `>>` или проверить через `git diff .agent/` после редактирования, что только новые строки добавлены.

### Expected Final State

После Phase 8:
- `git status` показывает modifications in `src/Iris.Desktop/{ViewModels/MemoryViewModel.cs,Views/MemoryView.axaml}`, `tests/Iris.IntegrationTests/Testing/FakeIrisApplicationFacade.cs`, `.agent/{PROJECT_LOG,overview,log_notes}.md`.
- `git status` показывает new files in `tests/Iris.Application.Tests/Memory/...`, `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs`, `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs`, `tests/Iris.IntegrationTests/Desktop/MemoryViewModelTests.cs`.
- `dotnet build .\Iris.slnx` 0/0.
- `dotnet test .\Iris.slnx` ≥ 190/190.
- `dotnet format .\Iris.slnx --verify-no-changes` EXIT_CODE=0 (или per-project fallback EXIT_CODE=0).
- `.agent/log_notes.md` запись «Phase 8 audit found 5 P1 issues» помечена RESOLVED.
- `.agent/PROJECT_LOG.md` содержит новую датированную запись.
- `.agent/overview.md` отражает завершение Memory v1 P1-фиксов.
- Готово к `/audit` re-run.

### Checks That Must Not Be Skipped

- **Phase 0 baseline**: подтвердить 175/175 ДО фикса — иначе невозможно отследить регрессии.
- **Каждая Phase verification**: build перед переходом к следующей фазе.
- **Phase 2 build после XAML edit**: ошибки AVLN — silent in editor, видны только в `dotnet build`.
- **Phase 7 после расширения `FakeIrisApplicationFacade`**: запустить полный `dotnet test` integration-проекта (не только filter), чтобы убедиться, что ни один из 82 существующих тестов не сломан.
- **Phase 8 final solution-wide test**: `dotnet test .\Iris.slnx` без filter; не доверять только per-project результатам.
- **Phase 8 git status**: финальный scope check — ни один файл вне разрешённого списка.

## 11. Open Questions

No blocking open questions.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

- HEAD на момент старта — `6955a4f` либо tip ветки `feat/avatar-v1-and-opencode-v2` без слияния новых коммитов после snapshot аудита.
- `Iris.Application.Memory.Options.MemoryOptions` имеет `Default` static и property, ограничивающий top-N (точное имя — выяснить в Phase 0; spec §11.1 ссылается на «top-N»). Имя property — деталь Phase 4 reconnaissance, не блокирует план.
- `Memory.Rehydrate` остаётся public (per audit §5 Notes) и пригоден для построения test-fixtures в Phase 6 (если конструктор `Memory.Create` слишком ограничен для воспроизведения forgotten-state в одном шаге).
- `PersistenceTestContextFactory.CreateInitializedContextAsync()` создаёт `memories` таблицу через `EnsureCreatedAsync` — подтверждено существующим Memory v1 work (паттерн из `ConversationRepositoryTests`).
- `tests/Iris.Application.Tests/Iris.Application.Tests.csproj` использует SDK-style глоб-инклюд `*.cs`, поэтому новые файлы в `Memory/Commands/` и `Memory/Context/` будут автоматически включены без изменения csproj.
- Аналогично `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` автоматически включает новые тестовые файлы.
- Manual smoke M-MEM-01..05 будет выполнен оператором в отдельной сессии (не в этой реализации); план только обеспечивает их feasibility.

## Blocking Questions

No blocking questions.

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | Specification: Fix Phase 8 Memory v1 Audit Findings (in conversation history; not saved to disk — user did not invoke `/save-spec`) |
| B — Design | ✅ Satisfied | Architecture Design: Fix Phase 8 Memory v1 Audit Findings (in conversation history; not saved to disk — user did not invoke `/save-design`) |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation (Phase 8 step 1) |
| E — Architecture Review | ⬜ Not yet run | Not required: changes localized to `Iris.Desktop` UI + tests; no project references, DI lifetimes, or boundary contracts modified. Architecture tests `MemoryBoundaryTests` (4) provide automated boundary protection. |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim (after Phase 8) |
| G — Memory | ⬜ Not yet run | Memory update is part of Phase 8 (FR-024..FR-026 of spec); will be executed during implementation, not via separate `/update-memory` |