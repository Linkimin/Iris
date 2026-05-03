# Architecture Design: Fix Phase 8 Memory v1 Audit Findings

## 1. Design Goal

Обеспечить техническое решение для закрытия 5 P1-находок аудита Phase 8 Memory v1 (P1-001..P1-005) + P2-005. Цель — восстановить основной пользовательский сценарий (FR-001..FR-008) в `Iris.Desktop` и восстановить покрытие Application и Persistence тестами (FR-009..FR-023) без нарушения текущей архитектуры `Iris.Desktop`, `Iris.Application` и `Iris.Persistence`.

## 2. Specification Traceability

Дизайн базируется на спецификации: `Specification: Fix Phase 8 Memory v1 Audit Findings` (создана в этой же сессии).

- **FR-001 / FR-008**: Авто-загрузка и отмена → Component Design `MemoryViewModel`.
- **FR-004 / FR-005**: Биндинг `ForgetCommand` → Contract Design `MemoryView.axaml` + `MemoryViewModel`.
- **FR-002 / AC-001 / AC-004**: No direct adapter access → Data Flow.
- **FR-009..FR-015**: Application handlers test coverage → Testing Design.
- **FR-016..FR-018**: Prompt injection test coverage → Testing Design.
- **FR-019..FR-021**: Persistence integration test coverage → Testing Design.
- **FR-022..FR-023**: Desktop regression testing → Testing Design.
- **Spec Open Questions**: Решены в §§ 6, 7.

## 3. Current Architecture Context

- **UI Layer**: `MemoryView.axaml` (Avalonia) привязан к `MemoryViewModel`. Коллекция `Memories` хранит `MemoryViewModelItem` (содержит типизированный `MemoryId`). Доступ к бизнес-логике осуществляется только через `IIrisApplicationFacade` (содержит 4 метода).
- **Application Layer**: Логика инкапсулирована в handlers (`RememberExplicitFactHandler`, `ForgetMemoryHandler`, `UpdateMemoryHandler`, `RetrieveRelevantMemoriesHandler`, `ListActiveMemoriesHandler`) и `MemoryContextBuilder` + `MemoryPromptFormatter`.
- **Persistence Layer**: `MemoryRepository` реализует `IMemoryRepository` через EF Core + SQLite (`EnsureCreatedAsync`, `COLLATE NOCASE`).
- **Tests**: Существует локальная инфраструктура фейков: `FakeUnitOfWork`, `FakeClock` в `SendMessageHandlerTests.cs` и `FakeMemoryRepository` в `DependencyInjectionTests.cs`. Существует `PersistenceTestContextFactory`.

## 4. Proposed Design Summary

Изменения кода (src) локализованы исключительно в `Iris.Desktop`:
1. `MemoryViewModel` получает `ForgetCommand` (IAsyncRelayCommand) и триггер загрузки в конструкторе (fire-and-forget).
2. `MemoryView.axaml` получает биндинг кнопки «Забыть» через `RelativeSource` к `ForgetCommand` родительской VM.

Изменения тестов охватывают `Iris.Application.Tests` (≥10 тестов) и `Iris.IntegrationTests` (≥5 тестов), используя локальные fake-реализации и существующие фабрики. 

Никаких изменений контрактов Application или Persistence. Никакого нового DI или пакетов.

## 5. Responsibility Ownership

| Responsibility | Owner | Notes |
|---|---|---|
| Инициализация загрузки UI | `Iris.Desktop` (`MemoryViewModel`) | Constructor вызывает `LoadMemoriesAsync` |
| Выполнение "Забыть" UI | `Iris.Desktop` (`MemoryViewModel`) | `ForgetCommand` вызывает `ForgetAsync` |
| Биндинг контекста кнопки | `Iris.Desktop` (`MemoryView.axaml`) | `RelativeSource` связывает DataTemplate с Parent VM |
| Валидация Application логики | `Iris.Application.Tests` | 7 Handler тестов + 3 PromptBuilder теста |
| Валидация Persistence логики | `Iris.IntegrationTests` | 3 MemoryRepository интеграционных теста |

## 6. Component Design

### `MemoryViewModel`

- **Owner layer**: `Iris.Desktop`
- **Responsibility**: MVVM-презентация состояния памяти.
- **Inputs**: `IIrisApplicationFacade`.
- **Outputs**: `Memories` (коллекция), `ErrorMessage`, `IsLoading`.
- **Collaborators**: `MemoryView.axaml` (View).
- **Design Decisions (Spec Open Questions resolution)**:
  - **Load Trigger**: Вызов `LoadMemoriesAsync(CancellationToken.None)` (или с CTS) прямо в конструкторе (fire-and-forget). Это классический Avalonia MVVM паттерн (избегает зависимости от View-lifecycle). 
  - **Concurrent Load Guard**: Добавить `if (IsLoading) return;` в начало `LoadMemoriesAsync` для защиты от двойного вызова (например, при быстром switch'е вкладок, если `LoadMemoriesAsync` будет переиспользован позже).
  - **ForgetCommand Signature**: `IAsyncRelayCommand<MemoryId>`. Поскольку `MemoryViewModelItem` уже имеет свойство `MemoryId Id`, `CommunityToolkit.Mvvm` легко справляется с этим параметром.
- **Must not do**: Не вызывать `IMemoryRepository` или `IrisDbContext`.

## 7. Contract Design

### `MemoryView.axaml` Data Binding (UI Contract)

- **Owner**: `Iris.Desktop`
- **Consumers**: User.
- **Shape**:
  ```xml
  <Button Grid.Column="1"
          Content="Забыть"
          Command="{Binding $parent[ItemsControl].DataContext.ForgetCommand}"
          CommandParameter="{Binding Id}"
          VerticalAlignment="Center" />
  ```
- **Compatibility**: Строго UI-binding; C# контракты не ломает.
- **Error behavior**: Если биндинг проваливается (DataContext null), кнопка будет disabled или no-op (стандарт Avalonia).

### `MemoryViewModel` (Public API)

- **Owner**: `Iris.Desktop`
- **Consumers**: `MemoryView.axaml`, `MainWindowViewModel`, DI Container.
- **Shape**:
  ```csharp
  // Существующее:
  public IAsyncRelayCommand RememberCommand { get; }
  public async Task ForgetAsync(MemoryId id, CancellationToken ct) { ... }
  // Новое:
  public IAsyncRelayCommand<MemoryId> ForgetCommand { get; }
  // В конструкторе:
  ForgetCommand = new AsyncRelayCommand<MemoryId>(id => ForgetAsync(id, default));
  ```
- **Compatibility**: Аддитивное расширение.

Никакие другие контракты (Application, Persistence, Facade) **не меняются**.

## 8. Data Flow

### Primary Flow (Load Memories)

1. `MemoryViewModel` конструируется (DI).
2. Конструктор вызывает `LoadMemoriesAsync` (fire-and-forget).
3. `IsLoading` = true.
4. Вызов `IIrisApplicationFacade.ListActiveMemoriesAsync`.
5. Facade вызывает `ListActiveMemoriesHandler` → `IMemoryRepository` → SQLite.
6. Результат возвращается; коллекция `Memories` очищается и заполняется новыми `MemoryViewModelItem`.
7. `IsLoading` = false.

### Primary Flow (Forget Memory)

1. Пользователь кликает «Забыть» на карточке `MemoryViewModelItem`.
2. Avalonia резолвит `ForgetCommand` через `$parent[ItemsControl]` и передаёт `MemoryViewModelItem.Id` как `CommandParameter`.
3. `ForgetCommand.ExecuteAsync` вызывает `MemoryViewModel.ForgetAsync`.
4. `IIrisApplicationFacade.ForgetAsync` маршрутизирует вызов в `ForgetMemoryHandler`.
5. Handler обновляет SQLite.
6. При `Result.Success`, `MemoryViewModel.ForgetAsync` вызывает `LoadMemoriesAsync` для перезагрузки списка (поведение FR-006).

### Error / Alternative Flows

- **Concurrent Load**: Второй вызов `LoadMemoriesAsync` (когда `IsLoading` == true) возвращает сразу (noop).
- **Forget Failed (not_found/persistence_error)**: `Result.IsFailure` → `ErrorMessage` заполняется, список `Memories` НЕ перезагружается (сохраняется локальное состояние), пользователь видит ошибку.
- **Task Cancellation**: `OperationCanceledException` перехватывается пустым блоком `catch` (существующий паттерн в файле), `IsLoading` сбрасывается в `finally`.

## 9. Data and State Design

Дизайн не меняет Data Design (SQLite-схему, миграции, EF entities, доменные модели). 
`MemoryViewModel` in-memory state (коллекция `Memories`) просто синхронизируется с хранилищем при старте и после мутаций. `MemoryViewModel` остаётся Singleton.

## 10. Error Handling and Failure Modes

Определено в `Specification` §10. `MemoryViewModel` уже имеет блок `try/catch` с обработкой `Result.IsFailure`. Единственное дополнение — concurrent guard (`if (_isLoading) return;`).

## 11. Configuration and Dependency Injection

- `Iris.Desktop/DependencyInjection.cs`: `services.AddSingleton<MemoryViewModel>()` остаётся **unchanged**. Post-init не требуется, так как конструктор сам инициирует загрузку.
- Никаких новых опций или конфигурационных блоков.

## 12. Security and Permission Considerations

Not relevant. Уровень безопасности/песочницы определяется на уровне Tools/Persistence. Desktop UI просто отражает то, к чему фасад разрешил доступ.

## 13. Testing Design

В соответствии с `Specification` §11, архитектура тестов:

### `tests/Iris.Application.Tests/Memory/...` (Unit Tests)

- **Approach**: Создать 7 файлов-тестов (или 1 с вложенными классами) для `RememberExplicitFactHandler`, `ForgetMemoryHandler`, `UpdateMemoryHandler`, `MemoryContextBuilder`.
- **Fakes**: Скопировать или извлечь `FakeMemoryRepository`, `FakeUnitOfWork`, `FakeClock` (как в `SendMessageHandlerTests.cs`). Учитывая запрет на изменения вне scope, безопаснее скопировать fakes локально (в новый `tests/Iris.Application.Tests/Memory/FakeInfrastructure.cs` или напрямую в тестовые файлы), чем рефакторить существующие тесты для шаринга.
- **Assertion pattern**: `Assert.True(result.IsSuccess)`, валидация состояний фейков (`_unitOfWork.CommitCalls`).

### `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderMemoryTests.cs` (Unit Tests)

- **Approach**: 3 теста. Inject `ILanguagePolicy` и `MemoryContextBuilder`. 
- **Assertion pattern**: `Assert.Contains("Известные факты:", message.Content)` (для FR-017) и `Assert.DoesNotContain` для User message (FR-018).

### `tests/Iris.IntegrationTests/Persistence/MemoryRepositoryTests.cs` (Integration Tests)

- **Approach**: 3 теста. Наследовать паттерн от `ConversationRepositoryTests.cs`.
- **Infrastructure**: Использовать `PersistenceTestContextFactory.CreateContextAsync()`. Использовать `SqliteConnection.ClearAllPools()` в dispose.
- **Assertion pattern**: Round-trip assert, `Assert.DoesNotContain(forgottenMemory)` (FR-020), case-insensitive `Assert.Single` (FR-021).

### `tests/Iris.IntegrationTests/Desktop/MemoryViewModelTests.cs` (Integration Tests)

- **Approach**: 2 теста (T-DESK-MEM-01, 02). 
- **Infrastructure**: Использовать существующий `FakeIrisApplicationFacade` из `tests/Iris.IntegrationTests/Testing/`.
- **Mocking**: В фейке добавить простейшую поддержку `ListActiveMemoriesAsync` (возврат двух DTO) и `ForgetAsync` (запись вызова).

## 14. Options Considered

### Load Trigger Option Comparison

| Option | Benefit | Drawback | Recommendation |
|---|---|---|---|
| **Ctor fire-and-forget** | Самый чистый MVVM. Никакого code-behind. Синхронно с DI-resolved. Уже используется в ChatViewModel. | `Task` не awaited; исключения должны быть строго пойманы внутри метода. | **Recommended.** Удовлетворяет всем правилам проекта. |
| **`OnAttachedToVisualTree`** | Жизненный цикл привязан к UI. Перезагрузка при каждом открытии вкладки. | Требует code-behind cast `(MemoryViewModel)DataContext`. Если вкладка "живёт" в памяти, attach может не выстрелить дважды. | Rejected (over-complication). |
| **Explicit `LoadCommand` + XAML Behavior** | Явный Control. | Требует пакета `Avalonia.Xaml.Behaviors` (нарушает AC-002: no new packages). | Rejected. |

## 15. Risks and Trade-Offs

- **Fire-and-forget Task in Ctor**: Если `IIrisApplicationFacade` когда-нибудь начнёт блокировать вызывающий поток, UI зависнет при старте. Текущая реализация (SQLite async + EF) полностью асинхронна, поэтому риск минимален. Опционально можно обернуть в `Task.Run(() => LoadMemoriesAsync(default))` для полной отвязки.
- **Local Fakes vs Shared Test Lib**: Копирование fakes (`FakeUnitOfWork` etc) в `Iris.Application.Tests.Memory` увеличивает дублирование тестового кода. Trade-off: создание `Iris.Testing.Shared` выходит за рамки spec-scope и требует Gate E (Architecture Review). Локальное дублирование безопаснее для v1.
- **Concurrent Load Guard**: `IsLoading` guard решает проблему двойного входа, но может "проглотить" запрос на загрузку, если State Machine зависнет. `try/finally { IsLoading = false; }` гарантирует, что этого не произойдет.

## 16. Acceptance Mapping

- **AC-V-008/009**: Обеспечивается Component Design §6 и Contract Design §7.
- **AC-V-005..007**: Обеспечивается Testing Design §13.
- **AC-V-001..004, 010..014**: Механические проверки и соблюдение scope.

## 17. Blocking Questions

No blocking open questions.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

- Fakes из `SendMessageHandlerTests.cs` (или их упрощенные версии) могут быть дублированы/созданы заново в `tests/Iris.Application.Tests/Memory/FakeInfrastructure.cs` для соблюдения scope "не трогать существующие тесты кроме добавления новых".
- В `ForgetCommand` будет передан `default` (CancellationToken.None), так как IAsyncRelayCommand в CommunityToolkit может сам менеджить токены (если использовать перегрузку), но для простоты v1 и соответствия `RememberCommand` мы передадим `default`.

## Blocking Questions

No blocking questions.

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | Spec: Fix Phase 8 Memory v1 Audit Findings (in conversation history) |
| B — Design | ✅ Satisfied | This design |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |