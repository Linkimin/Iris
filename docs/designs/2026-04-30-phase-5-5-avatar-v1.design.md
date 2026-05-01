# Architecture Design: Phase 5.5 — Avatar v1: визуальная реакция Айрис

## 1. Design Goal

Спроектировать Desktop-only систему аватара, которая реактивно отображает визуальное состояние Айрис, наблюдая за `ChatViewModel` без обратного связывания, без изменения Application-слоя, и с конфигурируемыми размером, позицией и отключением через `appsettings.json`.

## 2. Specification Traceability

Дизайн базируется на утверждённой спецификации `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md`.

| Spec Requirement | Design Coverage |
|---|---|
| FR-001 (AvatarState enum, Speaking reserved) | `AvatarState` enum в `Iris.Desktop.Models` |
| FR-002 (Thinking when IsSending) | `AvatarViewModel` подписывается на `ChatViewModel.PropertyChanged` |
| FR-003 (Success → timer → Idle) | Подписка на `Messages.CollectionChanged` + `Timer` |
| FR-004 (Error when HasError) | `PropertyChanged` → `HasError` |
| FR-005 (Idle as default) | Начальное значение `State` |
| FR-006 (Hidden when Enabled=false) | `AvatarOptions.Enabled` → `State = Hidden` |
| FR-007–009 (Image/Size/Position) | `AvatarPanel` биндинги к `State`, `Size`, `Position` |
| FR-010 (No non-Desktop types) | Все типы в `Iris.Desktop`; ноль изменений в других проектах |
| FR-011 (ChatViewModel unaware) | `ChatViewModel` не изменяется |
| FR-012 (No adapter access) | `AvatarViewModel` не зависит от фасада/адаптеров/DbContext |
| FR-013 (Config without code changes) | `AvatarOptions` — POCO, создаётся в DI из `IConfiguration` |
| FR-015 (Timer cancellation) | Отмена `Timer` при смене состояния |
| AC-001–008 | Все соблюдены (поакомпонентно ниже) |

## 3. Current Architecture Context

### Текущая структура Desktop

```
Iris.Desktop/
├── Models/
│   └── ChatMessageViewModelItem.cs          (реализован)
├── ViewModels/
│   ├── ViewModelBase.cs                     (public abstract, ObservableObject)
│   ├── ChatViewModel.cs                     (public sealed, full send flow)
│   ├── MainWindowViewModel.cs               (Chat + Greeting)
│   └── AvatarViewModel.cs                   (internal class, пустой)
├── Views/
│   ├── MainWindow.axaml                      (Window, один ChatView)
│   └── MainWindow.axaml.cs
├── Controls/Avatar/
│   ├── AvatarPanel.axaml                     (пустой Grid)
│   └── AvatarPanel.axaml.cs                  (internal partial, пустой)
├── Assets/Avatars/                           (пустая папка)
├── Services/                                 (фасад, error mapper)
├── DependencyInjection.cs                    (ChatViewModel, MainWindowViewModel)
└── appsettings.json                          (без секции Desktop:Avatar)
```

### Ключевые свойства ChatViewModel для наблюдения

| Член | Тип | Сигнал |
|---|---|---|
| `IsSending` | `bool`, private set → PropertyChanged | `Thinking` при true |
| `HasError` | `bool`, вычисляется из ErrorMessage → PropertyChanged | `Error` при true, IsSending==false |
| `Messages` | `ObservableCollection<ChatMessageViewModelItem>` → CollectionChanged | `Success` при Add assistant |
| `ChatMessageViewModelItem.Role` | `MessageRole` enum | Фильтр: `MessageRole.Assistant` |

### Ограничения

- `AvatarViewModel` — internal, без ViewModelBase, без DI.
- `AvatarPanel` — пустой Grid, internal.
- `MainWindow` — ровно один ребёнок (`ChatView`). Нет overlay-контейнера.
- `IApplicationEventBus` — пустая заглушка, не используется.

## 4. Proposed Design Summary

Дизайн — четыре уровня внутри `Iris.Desktop`:

1. **Модели данных**: `AvatarState`, `AvatarSize`, `AvatarPosition` enums + `AvatarOptions` record в `Iris.Desktop.Models`.
2. **ViewModel**: `AvatarViewModel` (наследует `ViewModelBase`). Получает `ChatViewModel` + `AvatarOptions` через конструктор. Реактивно подписывается на `PropertyChanged` и `CollectionChanged`. Управляет таймером Success→Idle. Observable-свойства: `State`, `Size`, `Position`.
3. **Контрол**: `AvatarPanel` (Avalonia `UserControl`). Биндится к `AvatarViewModel.State` → отображает соответствующее изображение или fallback (цветной круг + текст). Размер/позиция через биндинги.
4. **Интеграция в MainWindow**: overlay-компоновка (`Grid` или `Canvas`), `AvatarPanel` поверх `ChatView`.

DI регистрация читает `Desktop:Avatar` из конфигурации, создаёт `AvatarOptions` с defaults, регистрирует `AvatarViewModel` (Transient).

**Нет изменений** в `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure`.

## 5. Responsibility Ownership

| Responsibility | Owner | Notes |
|---|---|---|
| `AvatarState`, `AvatarSize`, `AvatarPosition` enums | `Iris.Desktop.Models` | Новые файлы, public |
| `AvatarOptions` record | `Iris.Desktop.Models` | POCO с конфигурационными значениями |
| Реактивное вычисление `AvatarState` | `Iris.Desktop.ViewModels.AvatarViewModel` | Observer + Timer |
| Визуализация состояния | `Iris.Desktop.Controls.Avatar.AvatarPanel` | Avalonia UserControl |
| Размещение аватара в окне | `Iris.Desktop.Views.MainWindow.axaml` | Overlay-компоновка |
| Предоставление `Avatar` свойством | `Iris.Desktop.ViewModels.MainWindowViewModel` | Новое свойство `Avatar` |
| Чтение конфигурации, создание `AvatarOptions` | `Iris.Desktop.DependencyInjection` | Defaults для отсутствующих ключей |
| Fallback-визуализация | `AvatarPanel` | Ellipse + TextBlock при отсутствии файла |
| Timer Success→Idle | `AvatarViewModel` | `System.Threading.Timer`, отмена при смене |
| Отписка при dispose | `AvatarViewModel` | `IDisposable` |
| Chat pipeline | `ChatViewModel` / `IrisApplicationFacade` | **Не изменяется** |

## 6. Component Design

### 6.1 `AvatarState` enum

- **Owner:** `Iris.Desktop.Models`
- **Values:** `Idle`, `Thinking`, `Speaking`, `Success`, `Error`, `Hidden`
- **Visibility:** `public`
- **Notes:** `Speaking` зарезервирован для Voice v1. В Avatar v1 ни одно переходное правило не ведёт в `Speaking`.

### 6.2 `AvatarSize` enum

- **Owner:** `Iris.Desktop.Models`
- **Values:** `Small` (80×80), `Medium` (120×120), `Large` (180×180)
- **Visibility:** `public`

### 6.3 `AvatarPosition` enum

- **Owner:** `Iris.Desktop.Models`
- **Values:** `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`
- **Visibility:** `public`

### 6.4 `AvatarOptions` record

- **Owner:** `Iris.Desktop.Models`
- **Responsibility:** POCO, содержащий конфигурационные параметры аватара.

```csharp
// Iris.Desktop.Models.AvatarOptions — illustrative contract
public sealed record AvatarOptions(
    bool Enabled,
    AvatarSize Size,
    AvatarPosition Position,
    double SuccessDisplayDurationSeconds);
```

- **Inputs:** DI регистрация читает `IConfiguration`, строит record с defaults для отсутствующих ключей.
- **Collaborators:** Только `AvatarViewModel` (через конструктор).
- **Must not do:** Содержать логику, ссылаться на `IConfiguration`, `IOptions<T>` или инфраструктуру.
- **Stability:** Stable v1. При добавлении новых параметров в v2 — расширение record (backward-compatible через значения по умолчанию в DI).

### 6.5 `AvatarViewModel`

- **Owner:** `Iris.Desktop.ViewModels`
- **Base class:** `ViewModelBase` (ObservableObject)
- **Visibility:** `public sealed partial class`
- **Constructor:**
  ```csharp
  // illustrative signature
  public AvatarViewModel(ChatViewModel chatViewModel, AvatarOptions options)
  ```
- **Inputs:** `ChatViewModel` (для наблюдения), `AvatarOptions` (конфигурация).
- **Outputs (observable properties):**
  - `AvatarState State` — текущее состояние аватара.
  - `AvatarSize Size` — размер (из `AvatarOptions.Size`).
  - `AvatarPosition Position` — позиция (из `AvatarOptions.Position`).
- **Internal state:**
  - `ChatViewModel _chatViewModel` — наблюдаемый объект.
  - `AvatarOptions _options` — настройки.
  - `Timer? _successTimer` — таймер перехода Success→Idle.
  - `bool _disposed` — флаг для предотвращения операций после dispose.
- **Collaborators:** Только `ChatViewModel` (через `PropertyChanged` + `CollectionChanged`). Никакие адаптеры, фасад, DbContext, `IConfiguration`, `IOptions<T>`.
- **Must not do:**
  - Вызывать `IIrisApplicationFacade`, `SendMessageHandler`, `IrisDbContext`, `IChatModelClient`.
  - Ссылаться на `IConfiguration` или `IOptions<T>`.
  - Изменять `ChatViewModel`.
  - Ссылаться на `Iris.Domain.Persona`.
- **Lifecycle:**
  - Конструктор: сохраняет `chatViewModel` и `options`, вычисляет начальный `State` (`Hidden` если `!Enabled`, иначе `Idle`), подписывается на `chatViewModel.PropertyChanged` и `chatViewModel.Messages.CollectionChanged`.
  - `Dispose()`: отписывается от событий, останавливает таймер.
- **Notes:**
  - Таймер создаётся только при входе в `Success`, уничтожается при смене состояния или dispose.
  - `Size` и `Position` — read-only observable properties, не меняются после конструктора (конфигурация не перезагружается в runtime).

### 6.6 `AvatarPanel` control

- **Owner:** `Iris.Desktop.Controls.Avatar`
- **Base class:** `UserControl`
- **DataContext type:** `AvatarViewModel`
- **Visual structure (проект):**

```xml
<!-- AvatarPanel.axaml — illustrative structure -->
<UserControl ... x:DataType="vm:AvatarViewModel">
  <Grid>
    <!-- Fallback: всегда присутствует, видим только при отсутствии изображения -->
    <Panel x:Name="FallbackPanel" IsVisible="False">
      <Ellipse ... />      <!-- цветной круг -->
      <TextBlock ... />     <!-- метка состояния -->
    </Panel>

    <!-- Изображения по одному на состояние, управляются IsVisible -->
    <Image x:Name="IdleImage" Source="/Assets/Avatars/idle.png"
           IsVisible="{Binding State, Converter={StaticResource StateEqualityConverter}, ConverterParameter=Idle}" />
    <Image x:Name="ThinkingImage" Source="/Assets/Avatars/thinking.png" ... />
    <Image x:Name="SpeakingImage" Source="/Assets/Avatars/speaking.png" ... />
    <Image x:Name="SuccessImage" Source="/Assets/Avatars/success.png" ... />
    <Image x:Name="ErrorImage" Source="/Assets/Avatars/error.png" ... />
  </Grid>
</UserControl>
```

- **Ключевые биндинги:**
  - `IsVisible` (на корневом элементе) → `{Binding State, Converter={StaticResource NotHiddenConverter}}` — скрывает весь контрол при `Hidden`.
  - `Width` / `Height` → вычисляется из `Size` через конвертер (`AvatarSizeToPixelConverter`).
  - Позиционирование — через родительский `Grid` alignment или `Canvas.Left`/`Canvas.Top`, в зависимости от выбранной overlay-стратегии (см. секцию 14 «Options Considered»).

- **Fallback behaviour:**
  - При старте проверяется, существуют ли файлы изображений для каждого состояния (через `AvaloniaResource`). Если ресурс не найден — активируется `FallbackPanel`.
  - FallbackPanel: `Ellipse` с цветом, зависящим от состояния (серый для Idle, жёлтый для Thinking, зелёный для Success, красный для Error) + `TextBlock` с именем состояния.

- **Must not do:** Обращаться к `IrisDbContext`, Ollama, фасаду, файловой системе напрямую.

- **Notes:** `internal partial class`. Использует compiled bindings (`x:DataType`).

### 6.7 `MainWindow` и `MainWindowViewModel`

#### MainWindow.axaml (проект изменений)

Текущее состояние — один ребёнок `<views:ChatView DataContext="{Binding Chat}" />`. Предлагаемое изменение — обернуть в overlay-компоновку:

```xml
<!-- MainWindow.axaml — proposed structure -->
<Window ... x:DataType="vm:MainWindowViewModel" Title="Iris">
  <Grid>
    <!-- Основной контент: занимает всё окно -->
    <views:ChatView DataContext="{Binding Chat}" />

    <!-- Аватар: overlay поверх чата, позиция управляется биндингом -->
    <controls:AvatarPanel DataContext="{Binding Avatar}"
                          HorizontalAlignment="Right"
                          VerticalAlignment="Bottom"
                          Margin="16" />
  </Grid>
</Window>
```

- **Позиционирование:** `HorizontalAlignment`/`VerticalAlignment` биндятся к `AvatarViewModel.Position` через конвертер.
- **Margin:** 16px отступ от краёв окна (константа v1).
- **Visibility:** Управляется самим `AvatarPanel` через биндинг `State != Hidden`.

#### MainWindowViewModel

Добавляется свойство `Avatar`:

```csharp
// illustrative addition
public MainWindowViewModel(ChatViewModel chat, AvatarViewModel avatar)
{
    Chat = chat;
    Avatar = avatar;
}

public AvatarViewModel Avatar { get; }
```

- Существующее свойство `Chat` и заглушка `Greeting` сохраняются без изменений.
- Конструктор расширяется вторым параметром — backward-compatible (DI резолвит оба).

### 6.8 `DependencyInjection.cs` (Desktop)

**Добавляемые регистрации:**

```csharp
// Внутри AddIrisDesktop, после существующих регистраций:

// Чтение конфигурации аватара с defaults
var avatarEnabled = configuration.GetValue<bool?>("Desktop:Avatar:Enabled") ?? true;
var avatarSize = ParseEnumOrDefault(configuration["Desktop:Avatar:Size"], AvatarSize.Medium);
var avatarPosition = ParseEnumOrDefault(configuration["Desktop:Avatar:Position"], AvatarPosition.BottomRight);
var successDuration = ParseDoubleOrDefault(configuration["Desktop:Avatar:SuccessDisplayDurationSeconds"], 2.0);

var avatarOptions = new AvatarOptions(avatarEnabled, avatarSize, avatarPosition, successDuration);
services.AddSingleton(avatarOptions);       // одна копия на всё приложение
services.AddTransient<AvatarViewModel>();    // новый экземпляр на каждый запрос
```

- `ParseEnumOrDefault<T>` — хелпер, парсящий строку в enum, с fallback на default при невалидном значении.
- `ParseDoubleOrDefault` — хелпер с проверкой `> 0`, fallback на `2.0`.
- `AvatarOptions` регистрируется как singleton (неизменяемый record).
- `AvatarViewModel` — Transient, как `ChatViewModel` и `MainWindowViewModel`.
- `MainWindowViewModel` регистрация **не меняется** — DI автоматически разрешит оба параметра конструктора.

## 7. Contract Design

### 7.1 Новые контракты (Desktop-only)

| Контракт | Владелец | Потребители | Форма | Стабильность |
|---|---|---|---|---|
| `AvatarState` enum | `Iris.Desktop.Models` | `AvatarViewModel`, `AvatarPanel` | 6 значений (включая `Speaking`) | Stable; расширение backward-compatible |
| `AvatarSize` enum | `Iris.Desktop.Models` | `AvatarViewModel`, `AvatarPanel` | 3 значения | Stable |
| `AvatarPosition` enum | `Iris.Desktop.Models` | `AvatarViewModel`, `AvatarPanel` | 4 значения | Stable |
| `AvatarOptions` record | `Iris.Desktop.Models` | `AvatarViewModel`, DI | `(bool Enabled, AvatarSize Size, AvatarPosition Position, double SuccessDisplayDurationSeconds)` | Stable; новые поля — backward-compatible |
| `AvatarViewModel.State` | `Iris.Desktop.ViewModels` | `AvatarPanel` (binding) | `AvatarState` observable | Stable |
| `AvatarViewModel.Size` | `Iris.Desktop.ViewModels` | `AvatarPanel` (binding) | `AvatarSize` observable | Stable |
| `AvatarViewModel.Position` | `Iris.Desktop.ViewModels` | `AvatarPanel` (binding) | `AvatarPosition` observable | Stable |
| `MainWindowViewModel.Avatar` | `Iris.Desktop.ViewModels` | `MainWindow.axaml` (binding) | `AvatarViewModel` property | Stable |

### 7.2 Изменяемые контракты

| Контракт | Изменение | Совместимость |
|---|---|---|
| `MainWindowViewModel` constructor | Добавлен параметр `AvatarViewModel` | Backward-compatible (DI разрешает новый параметр) |
| `appsettings.json` | Новая секция `Desktop:Avatar` | Backward-compatible (отсутствие секции → defaults) |

### 7.3 Неизменяемые контракты

Все контракты `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure` — **без изменений**. `ChatViewModel`, `IIrisApplicationFacade`, `SendMessageHandler`, `SendMessageResult` — без изменений.

## 8. Data Flow

### Primary Flow: Успешная отправка сообщения

1. Пользователь нажимает Send → `ChatViewModel.IsSending = true`.
2. `ChatViewModel.PropertyChanged("IsSending")` срабатывает.
3. `AvatarViewModel` в обработчике `OnChatPropertyChanged`: видит `IsSending == true`, `HasError == false` → устанавливает `State = Thinking`.
4. `AvatarPanel` через биндинг обновляет изображение на `thinking.png` (или fallback).
5. `ChatViewModel.SendMessageAsync` вызывает фасад → Application → Ollama.
6. Ответ получен. `ChatViewModel`:
   - `Messages.Add(userMessageDto)`
   - `Messages.Add(assistantMessageDto)`
   - `IsSending = false`
7. `ChatViewModel.Messages.CollectionChanged` срабатывает дважды (Add user, Add assistant).
8. `AvatarViewModel` в обработчике `OnMessagesChanged`:
   - Фильтрует: `e.NewItems` содержит `ChatMessageViewModelItem` с `Role == Assistant`.
   - `IsSending == false` и `HasError == false`.
   - Устанавливает `State = Success`.
   - Запускает `_successTimer = new Timer(OnSuccessTimerElapsed, null, _options.SuccessDisplayDurationSeconds * 1000, Timeout.Infinite)`.
9. `AvatarPanel` отображает `success.png`.
10. Таймер срабатывает → `State = Idle`.
11. `AvatarPanel` отображает `idle.png`.

### Error Flow

1–3: Как в Primary Flow.
4. Вызов фасада возвращает `Result.Failure`.
5. `ChatViewModel.ErrorMessage = ...` → `HasError = true` → `PropertyChanged("HasError")`.
6. `ChatViewModel.IsSending = false` → `PropertyChanged("IsSending")`.
7. `AvatarViewModel` (порядок событий не гарантирован, логика идемпотентна):
   - При `HasError == true` И `IsSending == false` → `State = Error`.
   - Отменяет активный таймер (если был — не будет в этом flow).
8. `AvatarPanel` отображает `error.png`.

### Disable/Enable Flow

1. Приложение стартует с `Enabled = false` → `AvatarOptions.Enabled = false`.
2. `AvatarViewModel` constructor: `State = Hidden`.
3. `AvatarPanel`: `IsVisible = false` (через конвертер `NotHidden`).
4. Пользователь меняет `appsettings.json` → требуется перезапуск (runtime reload не поддерживается в v1).

## 9. Data and State Design

### State Machine

```
         ┌─────────────────────────────────────────┐
         │                                         │
         ▼                                         │
      ┌──────┐    IsSending=true     ┌──────────┐  │
      │ Idle │──────────────────────►│ Thinking │  │
      └──────┘                       └──────────┘  │
         ▲                               │    │     │
         │                               │    │     │
         │ timer elapsed          success │    │ error
         │                               │    │     │
         │    ┌─────────┐ ◄──────────────┘    │     │
         │    │ Success │                     │     │
         │    └─────────┘                     │     │
         │                                    ▼     │
         │                               ┌───────┐  │
         └───────────────────────────────│ Error │──┘
                 new send (clears error) └───────┘

         Enabled=false → Hidden (из любого состояния, отменяет таймер)
```

### Transition Priority

При одновременном срабатывании нескольких наблюдаемых изменений:

1. `Enabled == false` → `Hidden` (немедленно, наивысший приоритет).
2. `IsSending == true` → `Thinking` (сбрасывает `Error`, отменяет таймер).
3. `IsSending == false && HasError == true` → `Error`.
4. `IsSending == false && HasError == false && new assistant message` → `Success` (запускает таймер).
5. Таймер → `Idle`.

### Persistence

Нет persistence. Все состояния — runtime-only.

### Timer Lifecycle

- Создаётся при входе в `Success` (с таймаутом `SuccessDisplayDurationSeconds * 1000` мс, однократный).
- Уничтожается (`Dispose()`) при:
  - Срабатывании (переход в `Idle`).
  - Входе в `Thinking` (новая отправка).
  - Входе в `Error`.
  - Входе в `Hidden`.
  - Вызове `AvatarViewModel.Dispose()`.

## 10. Error Handling Design

| Failure | Design Response |
|---|---|
| `ChatViewModel` is null в конструкторе | `throw new ArgumentNullException(nameof(chatViewModel))` |
| `AvatarOptions` is null в конструкторе | `throw new ArgumentNullException(nameof(options))` |
| Файл изображения не найден | `AvatarPanel` перехватывает при задании `Source`, активирует `FallbackPanel` |
| Невалидное значение `State` (будущее расширение enum) | Fallback: показывать `Idle`-изображение + log warning |
| `PropertyChanged` выбрасывает исключение | `AvatarViewModel` не ловит — Avalonia изолирует; сохраняется последнее валидное состояние |
| `CollectionChanged` срабатывает во время dispose | Проверка `_disposed` в обработчике — пропустить |
| Таймер срабатывает после dispose | `Timer.Dispose()` в `AvatarViewModel.Dispose()` + проверка `_disposed` в колбэке |
| `SuccessDisplayDurationSeconds` ≤ 0 | DI использует default `2.0` |
| Двойное срабатывание `PropertyChanged` | `SetProperty` в `AvatarViewModel.State` — идемпотентно |
| Утечка подписок | `AvatarViewModel` реализует `IDisposable`, отписывается от всех событий |

## 11. Configuration and Dependency Injection Impact

### Configuration (`appsettings.json`)

Новая секция (добавляется в существующий файл):

```json
{
  "Desktop": {
    "Avatar": {
      "Enabled": true,
      "Size": "Medium",
      "Position": "BottomRight",
      "SuccessDisplayDurationSeconds": 2.0
    }
  }
}
```

- Все ключи опциональны — отсутствие любого → default.
- Существующие секции (`Application`, `Database`, `ModelGateway`) не затрагиваются.

### DI изменения (только `Iris.Desktop/DependencyInjection.cs`)

- `AddSingleton(AvatarOptions)` — singleton, создаётся из конфигурации.
- `AddTransient<AvatarViewModel>()` — Transient, новый экземпляр на каждый `MainWindowViewModel`.
- `MainWindowViewModel` регистрация остаётся `AddTransient<MainWindowViewModel>()` — DI автоматически разрешает новый параметр `AvatarViewModel`.
- Никакие другие регистрации не меняются.

### Проектные ссылки

`Iris.Desktop.csproj` — без изменений. Все нужные зависимости (Avalonia, CommunityToolkit.Mvvm, Microsoft.Extensions.*) уже присутствуют.

## 12. Security and Permission Considerations

Аватар **не обрабатывает** и **не передаёт** пользовательские данные, содержимое сообщений, токены или файлы за пределы Desktop-процесса.

| Concern | Assessment |
|---|---|
| Доступ к данным чата | Аватар **не читает** содержимое сообщений. Наблюдает только `CollectionChanged` для детекции новых assistant-сообщений (не читает `Content`). |
| Доступ к файловой системе | `AvatarPanel` читает только `AvaloniaResource` (встроенные в сборку). Fallback не использует файловую систему. |
| Утечка данных через изображения | Изображения — статические ресурсы, не содержат пользовательских данных. |
| Конфигурационные секреты | Параметры аватара (`Enabled`, `Size`, `Position`, `SuccessDisplayDurationSeconds`) не являются секретами. |
| Permissions | Аватар не выполняет действий, не вызывает Tools, не затрагивает permission-систему. |
| Thread safety | ViewModel обновляется из UI-потока (Avalonia гарантирует). Timer callback — через `Avalonia.Threading.Dispatcher.UIThread.Post`, если требуется обновление `State` из timer-потока. |

## 13. Testing Design

### Test Project: `tests/Iris.IntegrationTests/Desktop/`

| # | Тест | Уровень | Проверка |
|---|---|---|---|
| T-01 | `InitialStateIsIdle` | Integration | `new AvatarViewModel(chat, options)` → `State == Idle` |
| T-02 | `InitialStateIsHiddenWhenDisabled` | Integration | `options.Enabled == false` → `State == Hidden` |
| T-03 | `StateBecomesThinkingOnSend` | Integration | Raise `PropertyChanged("IsSending", true)` → `State == Thinking` |
| T-04 | `StateBecomesSuccessThenIdle` | Integration + Timer | Raise `CollectionChanged(Add, assistant)` → `State == Success` → timer wait → `State == Idle` |
| T-05 | `StateBecomesErrorOnFailure` | Integration | Raise `PropertyChanged("HasError", true)` + `IsSending == false` → `State == Error` |
| T-06 | `ErrorClearsOnNewSend` | Integration | Set `State = Error` → Raise `IsSending = true` → `State == Thinking` |
| T-07 | `SuccessTimerCancelledOnNewSend` | Integration | Set `State = Success` (timer active) → Raise `IsSending = true` → `State == Thinking`, timer не срабатывает |
| T-08 | `SuccessTimerCancelledOnDisable` | Integration | Timer active → change to `Hidden` → timer cancelled |
| T-09 | `EnabledFromOptions` | Integration | `options.Enabled` → `State == Hidden` when false |
| T-10 | `SizeFromOptions` | Integration | `options.Size` → `AvatarViewModel.Size` |
| T-11 | `PositionFromOptions` | Integration | `options.Position` → `AvatarViewModel.Position` |
| T-12 | `DefaultsWhenConfigMissing` | DI | `IConfiguration` без секции `Desktop:Avatar` → `AvatarOptions` с defaults |
| T-13 | `DefaultsWhenInvalidEnum` | DI | `"Gigantic"` для Size → `Medium` |
| T-14 | `DisposeUnsubscribesFromChatViewModel` | Lifecycle | `Dispose()` → `PropertyChanged`/`CollectionChanged` больше не обрабатываются |
| T-15 | `NoProhibitedLayerReferences` | Architecture | `typeof(AvatarViewModel).Assembly` не ссылается на Application/Domain/Persistence/ModelGateway |

### Manual Smoke (M-01–M-07 из спецификации)

Все семь сценариев ручного smoke из спецификации остаются валидными.

## 14. Options Considered

### Option A: Overlay на Canvas с абсолютным позиционированием

- **Summary:** `MainWindow` содержит `Canvas`, `ChatView` занимает весь `Canvas`, `AvatarPanel` позиционируется через `Canvas.Left`/`Canvas.Top`, биндящиеся к `Position`.
- **Benefits:** Полный контроль над положением аватара (пиксельная точность).
- **Drawbacks:** `ChatView` должен быть растянут на весь `Canvas` (нестандартно для `Canvas`). Margin/отступ нужно считать в коде.
- **Verdict:** Отклонено для v1. Избыточно для четырёх углов.

### Option B: Overlay на Grid с Alignment-биндингом (ВЫБРАНО)

- **Summary:** `MainWindow` содержит `Grid` с одним рядом/колонкой. `ChatView` и `AvatarPanel` в одной ячейке (overlay). Позиция управляется `HorizontalAlignment`/`VerticalAlignment` через конвертер.
- **Benefits:** Стандартный Avalonia Grid overlay-паттерн. `ChatView` не требует специальных размеров. Margin константный (16px).
- **Drawbacks:** Только четыре угла — достаточно для v1.
- **Verdict:** Выбрано для v1.

### Option C: Использовать `IApplicationEventBus` для оповещения аватара

- **Summary:** Доработать `IApplicationEventBus`, публиковать события из `SendMessageHandler`, подписывать `AvatarViewModel`.
- **Benefits:** Более «чистый» pub/sub.
- **Drawbacks:** Требует доработки заглушек в Application и Infrastructure — scope creep. `ChatViewModel` — единственный UI-потребитель `SendMessageHandler`; event bus ради одного подписчика — overengineering. Spec AC-003 явно запрещает.
- **Verdict:** Отклонено.

## 15. Risks and Trade-offs

| Risk / Trade-off | Severity | Mitigation |
|---|---|---|
| **Tight coupling: AvatarViewModel → ChatViewModel** | Low | Coupling односторонний. `ChatViewModel` не знает об `AvatarViewModel`. Тесты аватара обнаружат регрессию observable-свойств. |
| **Timer в ViewModel** | Low | Стандартный BCL. Тесты T-04 и T-07 покрывают. Dispose отменяет таймер. |
| **Fallback-изображения могут не покрыть новый AvatarState в будущем** | Low | Добавление нового enum-значения → нужно добавить `Image` + fallback. Решается код-ревью. |
| **Отсутствие runtime reload конфигурации** | Low | Документировано как non-goal v1. |
| **Аватар не блокирует UI** | Low-Medium | `AvatarPanel` — чисто визуальный. Fallback при ошибке загрузки изображения. Avalonia изолирует binding-исключения. |
| **MainWindow layout меняется (overlay Grid)** | Low | `ChatView` не затрагивается. Grid-ячейка overlay — стандартный паттерн. |
| **DI: AvatarOptions singleton** | Low | Record неизменяем. При необходимости перезагрузки в v2 → `IOptionsSnapshot<T>`. |

## 16. Acceptance Mapping

| Acceptance Criterion (из спецификации) | Design Coverage |
|---|---|
| `dotnet build` passes | Нет новых проектов/пакетов |
| `dotnet test` passes (existing + ~15 new) | T-01–T-15 |
| `AvatarState` enum с 6 значениями | Section 6.1 |
| `AvatarViewModel` наследует ViewModelBase | Section 6.5 |
| `MainWindowViewModel.Avatar` property | Section 6.7 |
| `MainWindow.axaml` — AvatarPanel | Section 6.7 |
| State = Thinking при отправке | Data Flow: Primary step 3 |
| State = Success → Idle | Data Flow: Primary steps 8–10 |
| State = Error при ошибке | Data Flow: Error step 7 |
| State = Hidden при Enabled=false | Section 6.5 |
| Timer cancellation | Sections 6.5, 9, 10 |
| `Desktop:Avatar` секция в `appsettings.json` | Section 11 |
| Defaults при отсутствии конфигурации | Section 6.8 |
| Fallback при отсутствии изображения | Section 6.6 |
| Size / Position из конфигурации | Sections 6.6, 6.8 |
| Нет изменений вне Desktop | Section 4, Contract Design 7.3 |
| Dependency audit | Тест T-15 |
| Manual smoke M-01–M-07 | Section 13 |

## 17. Blocking Questions

No blocking open questions.
