# Specification: Fix Phase 8 Memory v1 Audit Findings

## 1. Problem Statement

Финальный аудит Phase 8 Memory v1 (`docs/audits/2026-05-02-phase-8-memory-v1.audit.md`) вернул решение **Changes requested / Not ready** из-за пяти P1-блокеров на HEAD `6955a4f`:

- **P1-001** — `MemoryViewModel.LoadMemoriesAsync` объявлен (line 58), но никем не вызывается. При открытии вкладки «Память» список всегда пуст. FR-021 / FR-023 родительской спецификации Memory v1 нарушены в runtime.
- **P1-002** — кнопка «Забыть» в `MemoryView.axaml` (line 47-49) отрисована без `Command` и без `Click`-обработчика; на `MemoryViewModel` отсутствует `IAsyncRelayCommand` для Forget; биндинг внутри `<DataTemplate DataType="MemoryViewModelItem">` имеет `DataContext` уровня item, что делает прямой `{Binding ForgetCommand}` некорректным даже после добавления команды на VM. FR-022 / FR-023 нарушены.
- **P1-003** — каталог `tests/Iris.Application.Tests/Memory/` не существует. Все T-APP-MEM-01..24 (хендлеры + `MemoryContextBuilder`) отсутствуют.
- **P1-004** — файл `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` не существует. Все T-PERS-MEM-01..05 отсутствуют.
- **P1-005** — файл `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` не существует. T-APP-PROMPT-01..03 отсутствуют. FR-018/019/020 не защищены тестом.

Совокупный эффект: основной пользовательский сценарий v1 «открыть вкладку Память → увидеть факты → кликнуть Забыть» нефункционален; ~31 запланированный автоматический тест отсутствует, что оставляет search-семантику, error-paths и EF-маппинг непокрытыми CI.

Mechanical verification на момент аудита остаётся чистой (build 0/0, tests 175/175, format clean на 4 core projects, 12 + 4 architecture tests зелёные), поэтому проблема локализована: два UI-defects + три test-coverage-gap; никаких Domain/Application/Persistence/гранечных регрессий не выявлено.

## 2. Goal

Закрыть все пять P1-находок Phase 8 Memory v1, не вводя архитектурных изменений и не трогая Domain/Application src, Persistence src, ModelGateway src, Shared, Infrastructure или хосты, отличные от `Iris.Desktop`, чтобы:

1. Основной пользовательский сценарий M-MEM-01..05 был осмысленно проверяем вживую.
2. Тестовое покрытие Application/Persistence/PromptBuilder восстановилось до уровня, заявленного в исходных Phase 8 спецификации/плане (≥ ~13 новых тестов поверх 175 baseline).
3. Re-run `/audit` Phase 8 Memory v1 на пересмотренном HEAD приводил к решению **Approved** (либо **Approved with P2 backlog**), без P0/P1 находок.

Решение P2-находок Phase 8 не входит в эту цель; см. §3.2.

## 3. Scope

### 3.1 In Scope

- Исправление P1-001 (триггер `LoadMemoriesAsync`) в `Iris.Desktop` UI-слое.
- Исправление P1-002 (бэкэнд `ForgetCommand` + XAML-биндинг) в `Iris.Desktop` UI-слое; включает поглощение P2-005 (`ForgetCommand` consistency), потому что P2-005 семантически тождественен upstream-причине P1-002.
- Создание тестового набора T-APP-MEM (минимум 7 тестов, перечислены в §11) под `tests/Iris.Application.Tests/Memory/`.
- Создание тестового набора T-PERS-MEM (минимум 3 тестов, перечислены в §11) в `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs`.
- Создание T-APP-PROMPT-01..03 в `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs`.
- Добавление одного интеграционного теста, валидирующего, что после привязки команды/триггера загрузки `MemoryViewModel.Memories` заполняется (защищает регрессию P1-001/P1-002).
- Минимально необходимое обновление documentation/agent-memory: `.agent/PROJECT_LOG.md`, `.agent/overview.md`, `.agent/log_notes.md` (закрытие P1 записей).

### 3.2 Out of Scope

- P2-001 (out-of-scope NL «запомни»/«забудь» pipeline). Спецификация Memory v1 §3.2 явно откладывает на Phase 9+.
- P2-002 (memory-degradation regression test for `SendMessageHandler`). Может быть включено как опциональное расширение T-APP-MEM, но не блокирует ready-decision.
- P2-003 (удалить неиспользуемый `MemoryOptions` из `RememberExplicitFactHandler`).
- P2-004 (LIKE-wildcard escaping в `MemoryRepository.SearchActiveAsync`).
- P2-006 (`MemoryPromptFormatter` — добавить `sealed`).
- P2-007 (verbose type qualifier в `MemoryViewModel`).
- P2-008 (`OperationCanceledException` exclusion в `SendMessageHandler` memory-block catch).
- Любые изменения Domain (`Memory`, `MemoryContent`, `MemoryId`, `MemoryStatus`, `MemoryKind`, `MemoryImportance`, `MemorySource`).
- Любые изменения Application src других слоёв (`PromptBuilder`, `SendMessageHandler`, handlers, `MemoryContextBuilder`, `MemoryPromptFormatter`, `MemoryDto`, `IMemoryRepository`).
- Любые изменения Persistence src (`MemoryEntity`, `MemoryEntityConfiguration`, `MemoryMapper`, `MemoryRepository`, `IrisDbContext`, `Persistence/DependencyInjection.cs`).
- Любые изменения hosts other than Desktop (`Iris.Api`, `Iris.Worker`).
- Manual smoke M-MEM-01..05 — выполняется отдельно живой Desktop-сессией с Ollama после фикса; в этой спеке только заявлен как требование readiness, см. §13.
- Расширение out-of-scope placeholders (`Memory/Embeddings/`, `Memory/Consolidation/`, `Memory/Audit/`, `Memory/Extract/`, `Memory/Ranking/`, `Memory/Policies/`, `Memory/Forget/` (старый), `Memory/Recall/`, `Memory/Remember/`).

### 3.3 Non-Goals

- Введение новых Application/Persistence/Domain абстракций.
- Введение `IOptions<>` для `MemoryOptions`.
- Изменение порядка/набора провайдеров DI в `Iris.Application/DependencyInjection.cs` или `Iris.Persistence/DependencyInjection.cs`.
- Замена `EnsureCreatedAsync` на EF migrations (R-001 остаётся принятым риском).
- Замена fire-and-forget паттерна на чистый async-init pattern, требующий новой инфраструктуры (например, `IAsyncInitializable`).
- Исправление визуального наложения аватара на Send-кнопку (P2 backlog — отдельный долг).
- Расширение `IIrisApplicationFacade` новыми методами.
- Изменение `MemoryViewModelItem`-shape.

## 4. Current State

- HEAD: `6955a4f` (`feat(opencode): add brainstorm, debug, TDD skills; update workflow rules`), branch `feat/avatar-v1-and-opencode-v2`. Working tree dirty: 29 M + 12 ??.
- Build: 0 warnings, 0 errors. Tests: 175/175 (App 36, Arch 12, Domain 44, Infra 1, Integration 82). Format clean per-project на Domain/Application/Desktop/Persistence.
- Architecture тесты 12/12 + 4 новых memory boundary теста все зелёные.
- Domain Memory v1 (7 типов), Application Memory v1 (5 хендлеров + `MemoryContextBuilder` + `MemoryPromptFormatter`), Persistence Memory v1 (`MemoryEntity` + config + repo + mapper) реализованы и проходят аудит passes 1, 3, 4 без блокеров.
- Desktop Memory v1: `MemoryViewModel`, `MemoryView.axaml`, `MainWindow.axaml` (TabControl Чат/Память), `MemoryViewModelItem` существуют. Биндинг и триггер загрузки сломаны (см. §1).
- Тестовая инфраструктура: локальные `FakeMemoryRepository`, `FakeUnitOfWork`, `FakeClock` уже определены в `DependencyInjectionTests.cs` и `SendMessageHandlerTests.cs` и могут быть переиспользованы как референс. `PersistenceTestContextFactory` существует в `tests/Iris.IntegrationTests/Persistence/`.
- Phase 8 Memory v1 артефакты: `docs/specs/2026-05-02-phase-8-memory-v1.spec.md`, `docs/designs/2026-05-02-phase-8-memory-v1.design.md`, `docs/plans/2026-05-02-phase-8-memory-v1.plan.md`, `docs/audits/2026-05-02-phase-8-memory-v1.audit.md` (все four присутствуют, аудит блокирует).
- `.agent/log_notes.md` содержит OPEN-запись «Phase 8 audit found 5 P1 issues» и OPEN-запись по M-MEM-01..05.

## 5. Affected Areas

| Area | Project / Path | Change Type |
|---|---|---|
| Desktop ViewModel | `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` | Modify (add `IAsyncRelayCommand<MemoryId> ForgetCommand`; add load trigger or hook for view-attach) |
| Desktop View | `src/Iris.Desktop/Views/MemoryView.axaml` | Modify (`Command`/`CommandParameter` binding for «Забыть») |
| Desktop View code-behind | `src/Iris.Desktop/Views/MemoryView.axaml.cs` | Modify only if «view-attach» trigger variant is chosen by Design |
| App tests (handlers) | `tests/Iris.Application.Tests/Memory/...` | Create new directory + ≥ 7 test files |
| App tests (prompt) | `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` | Create new file |
| Persistence tests | `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` | Create new file |
| Integration tests (Desktop UI flow) | `tests/Iris.IntegrationTests/Desktop/MemoryViewModelTests.cs` (or analogous) | Create new file (single regression test for P1-001/P1-002) |
| Agent memory | `.agent/PROJECT_LOG.md`, `.agent/overview.md`, `.agent/log_notes.md` | Append/update per `iris-memory` skill |

**Untouched (negative scope):** все `Iris.Domain/*`, все `Iris.Application/*` non-test, все `Iris.Persistence/*` non-test, `Iris.Application/DependencyInjection.cs`, `Iris.Persistence/DependencyInjection.cs`, `Iris.ModelGateway/*`, `Iris.Infrastructure/*`, `Iris.Shared/*`, `Iris.Api/*`, `Iris.Worker/*`, `Iris.SiRuntimeGateway/*`, `Iris.Perception/*`, `Iris.Tools/*`, `Iris.Voice/*`, `IIrisApplicationFacade`/`IrisApplicationFacade`, `MainWindow.axaml`, `MemoryViewModelItem`, `MainWindowViewModel`.

## 6. Functional Requirements

### Desktop UI

- **FR-001**: Открытие вкладки «Память» в работающем Desktop-приложении должно автоматически инициировать загрузку активных воспоминаний пользователя без какого-либо явного взаимодействия (без отдельной кнопки «Обновить»).
- **FR-002**: Загрузка активных воспоминаний должна выполняться через единственную точку — `IIrisApplicationFacade.ListActiveMemoriesAsync`. Прямой доступ Desktop к `IMemoryRepository` или `IrisDbContext` запрещён (повтор существующего FR-025 родительской спеки; защищается `MemoryBoundaryTests`).
- **FR-003**: Любая ошибка загрузки списка должна быть отражена в `MemoryViewModel.ErrorMessage` и не должна обрушивать UI-поток.
- **FR-004**: Каждая карточка памяти в `MemoryView` должна отображать кнопку «Забыть», у которой есть `Command`, обращающаяся к команде уровня `MemoryViewModel` (не уровня `MemoryViewModelItem`), с параметром, идентифицирующим целевой `MemoryId`.
- **FR-005**: Успешный клик по «Забыть» должен вызывать `IIrisApplicationFacade.ForgetAsync(MemoryId, CancellationToken)` ровно один раз для соответствующего `MemoryId`.
- **FR-006**: После успешного `ForgetAsync` список `Memories` должен перезагружаться через ту же точку, что используется при автоматической загрузке (FR-001/FR-002), чтобы исчезнувшая запись пропала из UI.
- **FR-007**: Ошибка `ForgetAsync` (включая not_found / persistence_failed) должна быть отражена в `ErrorMessage` и не должна перезагружать список.
- **FR-008**: Команда `ForgetCommand` должна быть отменяемой (поддерживать `CancellationToken` совместимым способом, как минимум — не зависать на отменённом фасадном вызове).

### Test Coverage — Application Memory Handlers

Минимально необходимый подмножество T-APP-MEM (соответствует Phase 8 Memory v1 spec §11.2 и audit §10):

- **FR-009 (T-APP-MEM-01)**: `RememberExplicitFactHandler` — happy path: входной валидный `Content` → возвращён `Result.Success` с `RememberMemoryResult`, `IMemoryRepository.AddAsync` вызван ровно один раз, `IUnitOfWork.CommitAsync` вызван ровно один раз.
- **FR-010 (T-APP-MEM-06)**: `ForgetMemoryHandler` — happy path active → forgotten: `IMemoryRepository.GetByIdAsync` вернул активное воспоминание, `Memory.Forget` пометил forgotten, `UpdateAsync` + `CommitAsync` вызваны.
- **FR-011 (T-APP-MEM-07)**: `ForgetMemoryHandler` — несуществующий id: `GetByIdAsync` вернул null → `Result.Failure` с error code, соответствующим контракту not_found; `UpdateAsync` не вызывался.
- **FR-012 (T-APP-MEM-08)**: `ForgetMemoryHandler` — уже forgotten: `Memory.Forget` вернул `false` → `Result.Success` (idempotent), но `UpdateAsync` и `CommitAsync` НЕ вызываются. (Защищает R-004 mitigation.)
- **FR-013 (T-APP-MEM-12)**: `UpdateMemoryHandler` — попытка обновить forgotten воспоминание возвращает `Result.Failure` с error code, соответствующим конфликту/недопустимому состоянию.
- **FR-014 (T-APP-MEM-21)**: `MemoryContextBuilder` — при пустом наборе active-memories возвращает пустую коллекцию без ошибок.
- **FR-015 (T-APP-MEM-22)**: `MemoryContextBuilder` — при количестве воспоминаний больше top-N (`MemoryOptions.MaxMemoriesInPrompt`) ограничивает результат top-N значениями.

### Test Coverage — PromptBuilder Memory Injection

- **FR-016 (T-APP-PROMPT-01)**: При пустой коллекции воспоминаний `PromptBuilder.Build(...)` возвращает результат, **byte-equivalent** базовой prompt-структуре до Phase 8 (single system message + history + user; никакого второго system message). Это контракт FR-019 родительской спеки.
- **FR-017 (T-APP-PROMPT-02)**: При непустой коллекции воспоминаний `PromptBuilder` добавляет ровно одно дополнительное `ChatModelRole.System` сообщение, содержащее (а) префикс `Известные факты:` и (б) текстовое представление каждого `MemoryDto.Content` через `MemoryPromptFormatter`. Это контракт FR-018 + FR-020 родительской спеки.
- **FR-018 (T-APP-PROMPT-03)**: При непустой коллекции воспоминаний ни одно `ChatModelRole.User` сообщение не должно содержать substring `Известные факты:` или содержимое любой воспоминания. Memory-блок остаётся system-role only.

### Test Coverage — Persistence Memory Repository

- **FR-019 (T-PERS-MEM-01)**: `MemoryRepository.AddAsync` + `GetByIdAsync` — round-trip активного воспоминания со смешанным регистром Cyrillic content и не-null `UpdatedAt`. Все поля доменного типа сохраняются и восстанавливаются (включая `MemoryKind`/`MemoryImportance` enum→int и `DateTimeOffset?` ↔ tick-long конверсии).
- **FR-020 (T-PERS-MEM-03)**: `MemoryRepository.ListActiveAsync` исключает воспоминания со статусом `Forgotten`.
- **FR-021 (T-PERS-MEM-04)**: `MemoryRepository.SearchActiveAsync` — case-insensitive Cyrillic match: lowercase query (например, `"айрис"`) находит запись, чей `Content` содержит ту же подстроку в смешанном регистре (например, `"Айрис"`). Это валидирует `COLLATE NOCASE` design-decision (R-005).

### Desktop Integration Regression Test

- **FR-022 (T-DESK-MEM-01)**: Интеграционный тест, разрешающий `MemoryViewModel` через `IIrisApplicationFacade`-фейк с двумя предустановленными `MemoryDto`, после симуляции выбранного триггера загрузки (см. Design) утверждает: `Memories.Count == 2`, без ошибок в `ErrorMessage`. Защищает регрессию P1-001.
- **FR-023 (T-DESK-MEM-02)**: Тот же setup; после `ForgetCommand.ExecuteAsync(memoryDtoList[0].Id)` фейковый facade фиксирует ровно один вызов `ForgetAsync` с правильным `MemoryId`, и (при возврате `Result.Success`) последующая перезагрузка списка вызвана. Защищает регрессию P1-002.

### Documentation / Memory

- **FR-024**: После завершения работы `.agent/PROJECT_LOG.md` должен содержать новую датированную запись со списком изменений, тестовыми результатами и явным закрытием P1-001..P1-005.
- **FR-025**: `.agent/log_notes.md` записи «Phase 8 audit found 5 P1 issues» и (если применимо) «Phase 8 Memory v1 manual smoke not performed» должны быть либо помечены как RESOLVED, либо обновлены с актуальным статусом.
- **FR-026**: `.agent/overview.md` должен отражать новую текущую фазу/статус (`Current Phase`, `Current Working Status`, `Next Immediate Step`, `Current Blockers`).

## 7. Architecture Constraints

- **AC-001**: Никаких изменений в `Iris.Domain`, `Iris.Shared`, `Iris.Application` (non-test), `Iris.Persistence` (non-test), `Iris.ModelGateway`, `Iris.Infrastructure`, `Iris.Api`, `Iris.Worker`, `Iris.SiRuntimeGateway`, `Iris.Perception`, `Iris.Tools`, `Iris.Voice`. Любое изменение в этих проектах = блокер на спецификацию.
- **AC-002**: Никаких новых project references, NuGet packages, central package version updates, или `InternalsVisibleTo` атрибутов.
- **AC-003**: Никаких изменений `IIrisApplicationFacade` (неизменный публичный контракт; Desktop UI должен использовать существующие 4 memory-метода).
- **AC-004**: `MemoryViewModel` остаётся в `Iris.Desktop.ViewModels`. Доступ к Application — только через `IIrisApplicationFacade`.
- **AC-005**: Архитектурные тесты `MemoryBoundaryTests` (4 теста) должны продолжать проходить без модификации.
- **AC-006**: Direct UI → `IMemoryRepository` или UI → `IrisDbContext` запрещены.
- **AC-007**: Direct UI → model providers запрещены.
- **AC-008**: Domain MUST NOT impact: `Memory.Rehydrate` остаётся public per current state; никаких новых mapper-only API.
- **AC-009**: Никакого service-locator паттерна в новых тестах или Desktop-коде.
- **AC-010**: Все новые Application-тесты используют только публичные/internal-через-existing-IVT абстракции `Iris.Application` + публичные типы `Iris.Domain` и `Iris.Shared`.
- **AC-011**: Все новые Persistence-тесты используют публичные типы из `Iris.Application.Abstractions.Persistence`, `Iris.Domain.Memories`, `Iris.Shared`, и существующий `PersistenceTestContextFactory`. Никакого прямого `IrisDbContext` use из тестов в обход существующих паттернов.
- **AC-012**: Все out-of-scope placeholders (см. §3.2) остаются untouched.
- **AC-013**: Никаких новых файлов в src вне `Iris.Desktop/ViewModels/MemoryViewModel.cs`, `Iris.Desktop/Views/MemoryView.axaml`, `Iris.Desktop/Views/MemoryView.axaml.cs`. (Создание новых файлов — только в test-проектах.)

## 8. Contract Requirements

| Contract | Current | Required | Compatibility |
|---|---|---|---|
| `IIrisApplicationFacade` (public) | 4 memory methods | Unchanged | No changes |
| `MemoryViewModel` (public surface) | `RememberCommand`, `NewMemoryContent`, `Memories`, `ErrorMessage`, `IsLoading`, `LoadMemoriesAsync`, `ForgetAsync` | + `ForgetCommand : IAsyncRelayCommand<MemoryId>` (or compatible signature determined by Design); existing surface unchanged | Additive only |
| `MemoryViewModelItem` (record/POCO) | Existing fields | Unchanged | No changes |
| `MemoryView.axaml` data binding contract | Button without `Command` | Button with `Command` resolved through `RelativeSource` / `ElementName` to `MemoryViewModel.ForgetCommand`, plus `CommandParameter="{Binding Id}"` | UI-only; no public C# contract change |
| `MemoryView.axaml.cs` (code-behind) | `InitializeComponent()` only | Either unchanged (ctor-trigger variant) OR adds `OnAttachedToVisualTree` override (view-attach variant) — Design decides | Internal class; no public contract change |
| `IMemoryRepository` (Application port) | 5 methods | Unchanged | No changes |
| `MemoryDto`, `MemoryOptions`, all Memory commands/queries/results | Existing | Unchanged | No changes |
| `PromptBuilder.Build(...)` semantics | Adds memory block when memories non-empty | Unchanged | Tests assert existing semantics; no semantic change |

Все остальные публичные/internal контракты Phase 8 Memory v1 — **unchanged**.

## 9. Data and State Requirements

- Никаких изменений в SQLite-схеме. Таблица `memories` остаётся такой, как создаётся `EnsureCreatedAsync` в текущей реализации.
- Никаких миграций.
- Никаких изменений `MemoryStatus` / `MemoryKind` / `MemoryImportance` / `MemorySource` enum-значений.
- Никаких изменений в `IrisDbContext.Memories` DbSet.
- В `MemoryViewModel.Memories` (in-memory state):
  - В момент открытия view — пуст до первого успешного `LoadMemoriesAsync`.
  - После успешного `LoadMemoriesAsync` — содержит все active memories из persistence в порядке, возвращённом `IIrisApplicationFacade.ListActiveMemoriesAsync` (порядок диктуется существующей логикой Application/Persistence; не меняется).
  - После успешного `RememberAsync` или `ForgetAsync` — перезагружается полностью (существующий идiom для v1).
- `MemoryViewModel` остаётся Desktop-DI Singleton (как сейчас в `Iris.Desktop/DependencyInjection.cs:79`); это не меняется.

## 10. Error Handling and Failure Modes

| Failure mode | Required behavior |
|---|---|
| `ListActiveMemoriesAsync` возвращает `Result.Failure` | `MemoryViewModel.ErrorMessage` ← `result.Error.Message`; `Memories` не очищается силой если уже был заполнен; `IsLoading` = false |
| `ListActiveMemoriesAsync` бросает `OperationCanceledException` | Игнорируется (как сейчас в lines 88-90); `IsLoading` корректно сбрасывается в finally |
| `ListActiveMemoriesAsync` бросает любое другое исключение | `ErrorMessage` ← `exception.Message`; `IsLoading` = false |
| `ForgetAsync` возвращает `Result.Failure` (not_found / persistence_failed) | `ErrorMessage` ← `result.Error.Message`; список НЕ перезагружается; команда возвращает control |
| `ForgetAsync` бросает `OperationCanceledException` | Игнорируется (паттерн совпадает с `RememberAsync`) |
| `ForgetAsync` бросает любое другое исключение | `ErrorMessage` ← `exception.Message` |
| Загрузочный триггер вызван дважды быстро подряд (например, view re-attach) | Не должен вызвать неконсистентное состояние `Memories`; реализация может: либо разрешить второй вызов завершиться (overwriting), либо защититься от concurrent loads (Design решает). Минимум: ни один вариант не должен бросать |
| `ForgetCommand` вызван с `MemoryId` который не существует в текущем `Memories` | Контракт `ForgetAsync` обрабатывает not_found — UI отобразит ошибку, не упадёт |
| Цвиграция `MemoryView` без `MemoryViewModel` `DataContext` (design-time / null DataContext) | Ни один триггер не должен бросить NRE (стандартная Avalonia design-time guards) |

Никаких новых стратегий retry, resilience policies, или structured error mapping. Существующая семантика error messages из Application/Persistence (стабильные коды, см. audit §6 Notes) сохраняется и отображается as-is.

## 11. Testing Requirements

### 11.1 Application Unit Tests — Memory (`tests/Iris.Application.Tests/Memory/`)

Минимально необходимо ≥ 7 тестов, покрывающих критические regression-prone paths:

| ID | Subject | Validates |
|---|---|---|
| T-APP-MEM-01 | `RememberExplicitFactHandler` happy path | FR-009 |
| T-APP-MEM-06 | `ForgetMemoryHandler` happy path active → forgotten | FR-010 |
| T-APP-MEM-07 | `ForgetMemoryHandler` missing id → not_found | FR-011 |
| T-APP-MEM-08 | `ForgetMemoryHandler` already-forgotten idempotent (no DB writes) | FR-012 |
| T-APP-MEM-12 | `UpdateMemoryHandler` on forgotten → conflict | FR-013 |
| T-APP-MEM-21 | `MemoryContextBuilder` empty active → empty | FR-014 |
| T-APP-MEM-22 | `MemoryContextBuilder` top-N respected | FR-015 |

Использовать локально определяемые fakes (`FakeMemoryRepository` — Dictionary-backed, `FakeUnitOfWork`, `FakeClock`), стиль которых уже устоявшийся в существующих `DependencyInjectionTests.cs` и `SendMessageHandlerTests.cs`.

### 11.2 Application Unit Tests — PromptBuilder Memory (`tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs`)

| ID | Subject | Validates |
|---|---|---|
| T-APP-PROMPT-01 | Empty memory → byte-equivalent baseline (single system message) | FR-016 |
| T-APP-PROMPT-02 | Non-empty memory → second System message с `Известные факты:` + контент | FR-017 |
| T-APP-PROMPT-03 | Non-empty memory → no User-role message contains `Известные факты:` или memory content | FR-018 |

### 11.3 Persistence Integration Tests (`tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs`)

| ID | Subject | Validates |
|---|---|---|
| T-PERS-MEM-01 | Round-trip Cyrillic mixed-case + non-null UpdatedAt | FR-019 |
| T-PERS-MEM-03 | `ListActiveAsync` excludes Forgotten | FR-020 |
| T-PERS-MEM-04 | `SearchActiveAsync` case-insensitive Cyrillic match | FR-021 |

Использовать существующий `PersistenceTestContextFactory`. Тесты должны корректно очищать SQLite-pool после прогона (паттерн уже выработан в `IrisDatabaseInitializerTests`).

### 11.4 Desktop Integration Regression Tests (`tests/Iris.IntegrationTests/Desktop/MemoryViewModelTests.cs` или эквивалент)

| ID | Subject | Validates |
|---|---|---|
| T-DESK-MEM-01 | View-attach / ctor trigger → `Memories.Count == 2` after fake facade returns 2 DTOs | FR-022 |
| T-DESK-MEM-02 | `ForgetCommand.ExecuteAsync(id)` → fake facade `ForgetAsync(id)` called once + reload triggered | FR-023 |

`FakeIrisApplicationFacade` уже существует в `tests/Iris.IntegrationTests/Testing/` — расширить (если требуется) для записи последовательности memory-вызовов; не нарушает AC-002 (не меняет публичный контракт).

### 11.5 Architecture Tests

`MemoryBoundaryTests` (4 теста) должны остаться зелёными без модификации. Никаких новых архитектурных тестов в этой работе.

### 11.6 Existing Tests

Все 175 существующих тестов должны остаться зелёными.

### 11.7 Verification Commands

Минимально обязательные:

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

Если последняя команда таймаутит на солюшен-уровне (как в audit §8), допустим fallback per-project format на затронутые проекты:

```powershell
dotnet format .\src\Iris.Desktop\*.csproj --verify-no-changes
dotnet format .\tests\Iris.Application.Tests\*.csproj --verify-no-changes
dotnet format .\tests\Iris.IntegrationTests\*.csproj --verify-no-changes
```

Ожидаемый итог: 0 errors, 0 warnings, ≥ 188 tests pass (175 baseline + 7 App + 3 Persistence + 3 PromptBuilder; T-DESK-MEM добавляются как +2). Точное число тестов будет известно после Design (если для xUnit `[Theory]` будет использован, count может быть выше).

### 11.8 Manual Smoke

После прохождения автоматической верификации обязательно выполнение M-MEM-01..M-MEM-05 живой Desktop-сессией с Ollama и чистой `iris.db`:

| ID | Scenario |
|---|---|
| M-MEM-01 | Запомнить новый факт через «Запомнить» |
| M-MEM-02 | Открыть вкладку «Память», увидеть запись |
| M-MEM-03 | Послать чат-запрос; ассистент использует факт |
| M-MEM-04 | Кликнуть «Забыть»; запись исчезает |
| M-MEM-05 | Перезапустить; не-forgotten записи сохраняются |

Результаты документируются в `.agent/log_notes.md` и `.agent/PROJECT_LOG.md` (см. FR-024..FR-026).

## 12. Documentation and Memory Requirements

- **DOC-001**: `.agent/PROJECT_LOG.md` — добавить датированную запись с разделами Changed / Files / Validation / Notes / Next, перечисляющую закрытие P1-001..P1-005, тестовый score, и оставшиеся P2 в backlog.
- **DOC-002**: `.agent/overview.md` — обновить `Current Phase`, `Current Working Status`, `Next Immediate Step`, `Current Blockers` (P1 секция должна стать пустой; P2-секция остаётся).
- **DOC-003**: `.agent/log_notes.md` — пометить запись «Phase 8 audit found 5 P1 issues» как RESOLVED с кратким разрешением; «Phase 8 Memory v1 manual smoke not performed» обновить (или закрыть, если smoke выполнен в той же итерации).
- **DOC-004**: Никакого обновления `mem_library/**`. Phase 8 Memory v1 уже отражён в `mem_library/05_memory_system.md` (если применимо); это не задача рефреша.
- **DOC-005**: `docs/audits/2026-05-02-phase-8-memory-v1.audit.md` НЕ модифицируется. Новый audit-артефакт создаётся отдельным `/audit` запуском после фикса (отдельный документ, новой датой).
- **DOC-006**: `docs/specs/`, `docs/designs/`, `docs/plans/` — оригинальные Phase 8 Memory v1 артефакты остаются неизменными. Текущая работа сохраняется как новая spec/design/plan/audit пара (с более узким scope) только если пользователь явно вызовет `/save-*`.

## 13. Acceptance Criteria

- [ ] **AC-V-001**: `dotnet build .\Iris.slnx` проходит с 0 errors, 0 warnings.
- [ ] **AC-V-002**: `dotnet test .\Iris.slnx` проходит с ≥ 188 тестами, 0 failures.
- [ ] **AC-V-003**: `dotnet format .\Iris.slnx --verify-no-changes` (или per-project fallback на затронутые проекты) — EXIT_CODE=0.
- [ ] **AC-V-004**: Архитектурные тесты — 12 + 4 memory boundary — все зелёные без модификаций.
- [ ] **AC-V-005**: 7 новых тестов T-APP-MEM (минимум) присутствуют в `tests/Iris.Application.Tests/Memory/` и проходят.
- [ ] **AC-V-006**: 3 новых теста T-APP-PROMPT присутствуют в `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` и проходят.
- [ ] **AC-V-007**: 3 новых теста T-PERS-MEM присутствуют в `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` и проходят.
- [ ] **AC-V-008**: ≥ 1 регрессионный Desktop-тест (T-DESK-MEM-01) подтверждает, что `MemoryViewModel.Memories` заполняется автоматически после view-trigger.
- [ ] **AC-V-009**: ≥ 1 регрессионный Desktop-тест (T-DESK-MEM-02) подтверждает, что `ForgetCommand.ExecuteAsync(id)` вызывает `IIrisApplicationFacade.ForgetAsync` ровно один раз.
- [ ] **AC-V-010**: `git status` показывает изменения только в `src/Iris.Desktop/ViewModels/MemoryViewModel.cs`, `src/Iris.Desktop/Views/MemoryView.axaml`, опционально `src/Iris.Desktop/Views/MemoryView.axaml.cs`, новых тест-файлах в `tests/Iris.Application.Tests/Memory/...`, `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs`, `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs`, `tests/Iris.IntegrationTests/Desktop/...`, и `.agent/*.md` файлах. Никаких изменений в src других проектов.
- [ ] **AC-V-011**: Никаких новых project references, NuGet packages, central package version updates, или `InternalsVisibleTo`.
- [ ] **AC-V-012**: Запуск `/audit` после реализации возвращает решение **Approved** или **Approved with P2 backlog** без P0/P1 находок по этой работе.
- [ ] **AC-V-013** (manual): M-MEM-01..M-MEM-05 пройдены живой Desktop-сессией; результаты записаны в `.agent/log_notes.md` + `.agent/PROJECT_LOG.md`.
- [ ] **AC-V-014**: `.agent/PROJECT_LOG.md` содержит новую запись; `.agent/overview.md` обновлён; `.agent/log_notes.md` записи P1-001..P1-005 закрыты как RESOLVED.

## 14. Open Questions

Все ключевые design-decisions имеют не более одного допустимого варианта в рамках архитектурных constraints; те, у которых есть выбор, не блокируют спецификацию и решаются на этапе `/design`. Перечисление их явно (не как блокеры) для `/design`-ввода:

- Триггер `LoadMemoriesAsync`: ctor fire-and-forget vs `OnAttachedToVisualTree` override vs explicit `LoadMemoriesCommand` + `Loaded`-trigger. Все три варианта Desktop-only, удовлетворяют FR-001/FR-022; выбор за `/design`.
- Защита от двойного concurrent load (например, `Interlocked` guard или `bool _isLoading` re-entry check). Принять как design-decision (см. §10 «Загрузочный триггер вызван дважды»).
- Включить ли P2-005 (`ForgetCommand` consistency) формально в этот фикс. Спека рекомендует ДА (см. §3.1), потому что P2-005 — upstream P1-002. `/design` подтверждает.
- Сигнатура `ForgetCommand`: `IAsyncRelayCommand<MemoryId>` против `IAsyncRelayCommand<Guid>` (поскольку XAML биндит `MemoryViewModelItem.Id`, тип которого зависит от MemoryViewModelItem-shape). Решается в `/design`.

**No blocking open questions.** Все вышеперечисленное — design-уровень choices внутри архитектурных рамок, заданных AC-001..AC-013.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

- HEAD остаётся на `6955a4f` с текущим dirty-tree до момента фикса; реализация будет выполнена в той же ветке `feat/avatar-v1-and-opencode-v2` либо в новом worktree, без изменений в неотносящихся к фиксу dirty-файлах.
- Существующая инфраструктура fakes в `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs` и `DependencyInjectionTests.cs` (`FakeMemoryRepository`, `FakeUnitOfWork`, `FakeClock`) принимается как референс-стиль для новых тестов; локальные fakes допускаются (без выноса в shared test infrastructure project).
- `FakeIrisApplicationFacade` в `tests/Iris.IntegrationTests/Testing/` может быть расширен (при необходимости — через сохранение call-history) без изменения публичного контракта `IIrisApplicationFacade`.
- `PersistenceTestContextFactory` пригоден для всех T-PERS-MEM без модификации.
- `Memory.Rehydrate` остаётся public (как зафиксировано в audit §5 Notes); тесты могут им пользоваться для построения test-fixtures, если требуется.
- Manual smoke M-MEM-01..05 будет выполняться оператором отдельно после автоматической верификации; данная спека не предписывает порядок (smoke может пройти в той же итерации или после `/audit` re-run).

## Blocking Questions

No blocking questions.

## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | This specification |
| B — Design | ⬜ Not yet run | Run `/design` when ready |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |