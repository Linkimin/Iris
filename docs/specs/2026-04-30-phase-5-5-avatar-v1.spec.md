# Specification: Phase 5.5 — Avatar v1: визуальная реакция Айрис

## 1. Problem Statement

После завершения Phase 5 (Desktop Chat v1) Айрис функционально отвечает на сообщения через UI, но пользователь не видит её «присутствия». Текущее окно `MainWindow` содержит только `ChatView` — текстовый интерфейс без индикации того, что Айрис «думает», «отвечает» или столкнулась с ошибкой. Отсутствие визуальной обратной связи делает взаимодействие безликим: пользователь не различает состояния ожидания ответа, успешного завершения и ошибки на визуальном уровне.

Согласно дорожной карте продукта (`.agent/mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md`), Phase 5.5 Avatar v1 должна появиться **после рабочего Desktop Chat v1, но до сложных Tools/Voice/Memory/Persona**, чтобы рано дать Айрис визуальное присутствие, не блокируя стабилизацию MVP. Phase 5.5 разрешена дорожной картой **сразу после Phase 5** и явно отделена от Phase 6 (End-to-End Stabilization).

Текущее состояние Avatar-кода — **полностью пустые заглушки**: `AvatarPanel` (пустой `<Grid />`), `AvatarViewModel` (пустой класс без базового типа и без DI-регистрации), пустая папка `Assets/Avatars/`. Никакой Avatar-логики не существует. `ChatViewModel` уже предоставляет все необходимые observable-свойства (`IsSending`, `HasError`, `Messages`) для реактивного отслеживания — механизм наблюдения уже доступен без доработки Application-слоя.

## 2. Goal

Добавить в `Iris.Desktop` визуальный аватар Айрис, который:

- отображается в окне приложения вместе с чатом;
- меняет состояние реактивно, отслеживая `ChatViewModel`: Idle → Thinking → Success → Idle (или Idle → Thinking → Error → Idle);
- не влияет на chat pipeline, Application-слой, доменную модель и persistence;
- может быть скрыт/отключён через конфигурацию;
- полностью принадлежит Desktop-слою — ни один другой проект не знает о существовании аватара.

## 3. Scope

### In Scope

1. **AvatarState enum** — перечисление состояний аватара: `Idle`, `Thinking`, `Speaking`, `Success`, `Error`, `Hidden`. Живёт в `Iris.Desktop.Controls.Avatar` или `Iris.Desktop.Models`. Состояние `Speaking` зарезервировано для будущей интеграции с Voice v1; в Avatar v1 аватар никогда не переходит в `Speaking`.
2. **AvatarPanel control** — переработка пустого `UserControl` в Avalonia-контрол, отображающий текущее состояние аватара через статическое изображение-заглушку для каждого не-`Hidden` состояния.
3. **AvatarViewModel** — переработка пустого класса в полноценную ViewModel, наследующую `ViewModelBase`, которая реактивно отслеживает `ChatViewModel` и вычисляет `AvatarState`. Никаких прямых вызовов фасада, адаптеров или инфраструктуры.
4. **Static avatar placeholder assets** — минимальный набор графических заглушек в `Assets/Avatars/` (цветные круги/фигуры с текстовой меткой состояния для Idle, Thinking, Speaking, Success, Error). Fallback при отсутствии файла: отображение запасной геометрической заглушки.
5. **MainWindow layout** — добавление `AvatarPanel` в `MainWindow` вместе с `ChatView`, с привязкой `DataContext="{Binding Avatar}"` через `MainWindowViewModel`.
6. **MainWindowViewModel** — добавление свойства `Avatar` типа `AvatarViewModel`, пробрасываемого через конструкторную инъекцию.
7. **DI registration** — регистрация `AvatarViewModel` (Transient) в `DependencyInjection.cs` (Desktop).
8. **Configuration parameters** — секция `Desktop:Avatar` в `appsettings.json` с параметрами:

   | Параметр | Тип | По умолчанию | Описание |
   |---|---|---|---|
   | `Enabled` | `bool` | `true` | Включение/отключение аватара. При `false` — `State == Hidden`. |
   | `Size` | `"Small"` / `"Medium"` / `"Large"` | `"Medium"` | Размер отображаемой области аватара. |
   | `Position` | `"TopLeft"` / `"TopRight"` / `"BottomLeft"` / `"BottomRight"` | `"BottomRight"` | Позиция аватара в окне. |
   | `SuccessDisplayDurationSeconds` | `double` | `2.0` | Длительность отображения Success перед переходом в Idle. |

   Состояния `Idle`, `Thinking`, `Success` и `Error` вычисляются реактивно на основе `ChatViewModel` и **не зависят от конфигурации**. Конфигурация управляет только параметрами `Enabled`, `Size`, `Position` и временем отображения `Success`.

### Out of Scope

- Live2D / Spine / скелетная анимация.
- Сложное перемещение аватара по экрану.
- Tool-specific сценарии аватара (Avatar v2).
- Lip sync (синхронизация губ с голосом).
- Физика (physics-based анимации).
- Плавные анимации перехода между состояниями (v1 — frame-based замена изображения).
- Десятки анимаций.
- Влияние аватара на Application/Domain/Persistence слои.
- Application event bus (пустые заглушки `IApplicationEventBus`/`InMemoryApplicationEventBus` не дорабатываются).
- Memory/Persona/Context/Modes слои.
- ShellView / многооконная компоновка (остаётся `MainWindow` с `ChatView` + `AvatarPanel`).
- Сложный AvatarSettings UI.
- Dedicated `Iris.Desktop.Tests` проект (AvatarViewModel тесты — в `Iris.Integration.Tests`, следуя практике Phase 5; техдолг зафиксирован в `.agent/debt_tech_backlog.md`).
- Runtime-изменение конфигурации аватара без перезапуска приложения.
- Speech bubble (пузырь с последним сообщением возле аватара).

### Non-Goals

- Превратить аватар в «живого персонажа» с характером.
- Интегрировать аватар с системой эмоций (`Iris.Domain.Emotions`).
- Визуализировать внутреннее состояние модели (токены, температура, latency).
- Заменить или дублировать функциональность чата.
- Создать `Iris.Avatar` как отдельный проект.
- Speech bubble (пузырь с последним сообщением возле аватара). Чат уже отображает сообщения в `ChatView`, дублирование их рядом с аватаром избыточно для v1 и создаёт ненужную связность между `AvatarPanel` и содержимым сообщений. Может быть пересмотрено в Avatar v2.
- Интеграция аватара с `Iris.Domain.Persona` (PersonaProfile, PersonaState, PersonaTrait — все заглушки; Avatar v1 не зависит от доменной персоны).

## 4. Current State

- **AvatarPanel** (`Controls/Avatar/AvatarPanel.axaml` + `.axaml.cs`): пустой `UserControl` с `<Grid />`, код-бихайнд пустой, `internal`.
- **AvatarViewModel** (`ViewModels/AvatarViewModel.cs`): пустой класс, `internal`, без базового класса, без DI-регистрации.
- **Assets/Avatars/**: пустая папка, объявлена в `.csproj` как `<Folder>` и `<AvaloniaResource>`.
- **MainWindow** (`Views/MainWindow.axaml`): содержит только `<views:ChatView DataContext="{Binding Chat}" />`.
- **MainWindowViewModel**: содержит `Chat` (тип `ChatViewModel`) и неиспользуемый `Greeting`.
- **ChatViewModel**: полная реализация с observable-свойствами:
  - `IsSending` (`bool`, private set) — `true` во время отправки.
  - `HasError` (`bool`, вычисляется из `ErrorMessage`) — `true` при наличии ошибки.
  - `Messages` (`ObservableCollection<ChatMessageViewModelItem>`) — сообщения чата.
  - `ErrorMessage` (`string`, private set) — текст ошибки или пустая строка.
  - `InputText` (`string`) — ввод пользователя.
  - `CanEditInput` (`bool`) — `true`, когда не идёт отправка.
  - Все свойства реализуют `INotifyPropertyChanged` через `ObservableObject`.
- **DependencyInjection (Desktop)**: регистрирует `ChatViewModel` (Transient), `MainWindowViewModel` (Transient), фасад и слои Application/Persistence/ModelGateway. Avatar не регистрируется.
- **IApplicationEventBus** и **InMemoryApplicationEventBus**: пустые заглушки. Инфраструктура событий не реализована и не требуется для Avatar v1.
- **Темы**: пустые `ResourceDictionary` (не требуются для Avatar v1).
- **Iris.Domain.Persona**: 11 пустых заглушек (`PersonaProfile`, `PersonaState`, `PersonaTrait`, `PersonaMode`, `SpeechStyle` и др.) — не реализованы, не используются.

## 5. Affected Areas

| Область | Воздействие |
|---|---|
| `Iris.Desktop.Controls.Avatar.AvatarPanel` | Полная переработка из заглушки |
| `Iris.Desktop.Models` (новый `AvatarState` enum, возможные `AvatarSize`, `AvatarPosition` enums) | Новые типы |
| `Iris.Desktop.ViewModels.AvatarViewModel` | Полная переработка из заглушки |
| `Iris.Desktop.ViewModels.MainWindowViewModel` | Расширение — новое свойство `Avatar` |
| `Iris.Desktop.Views.MainWindow.axaml` | Расширение — добавление `AvatarPanel` |
| `Iris.Desktop.DependencyInjection.cs` | Расширение — регистрация `AvatarViewModel` |
| `Iris.Desktop.appsettings.json` | Расширение — секция `Desktop:Avatar` |
| `Iris.Desktop.Assets.Avatars` | Новые файлы-заглушки |
| `tests/Iris.IntegrationTests/Desktop/` | Новые тесты AvatarViewModel |
| `Iris.Domain` | Не затронут |
| `Iris.Application` | Не затронут |
| `Iris.Persistence` | Не затронут |
| `Iris.ModelGateway` | Не затронут |
| `Iris.Shared` | Не затронут |
| `Iris.Infrastructure` | Не затронут |

## 6. Functional Requirements

- **FR-001**: `AvatarViewModel` должен иметь публичное observable-свойство `State` типа `AvatarState`. Перечисление `AvatarState` включает шесть значений: `Idle`, `Thinking`, `Speaking`, `Success`, `Error`, `Hidden`. Состояние `Speaking` зарезервировано для будущей интеграции с Voice v1; в Avatar v1 аватар никогда не переходит в `Speaking` — ни одно переходное правило не ведёт в это состояние.

- **FR-002**: Когда `ChatViewModel.IsSending == true` и `ChatViewModel.HasError == false`, `AvatarViewModel.State` должен быть `Thinking`. Если `HasError` был `true` в момент начала отправки, ошибка считается сброшенной — приоритет у `Thinking`.

- **FR-003**: Когда `ChatViewModel.IsSending == false`, `ChatViewModel.HasError == false`, и в `ChatViewModel.Messages` только что добавилось сообщение с `Role == Assistant` (событие `CollectionChanged` с `NotifyCollectionChangedAction.Add`), `AvatarViewModel.State` должен стать `Success` на время, заданное параметром `Desktop:Avatar:SuccessDisplayDurationSeconds` (по умолчанию 2.0 секунды), после чего автоматически перейти в `Idle`.

- **FR-004**: Когда `ChatViewModel.IsSending == false` и `ChatViewModel.HasError == true`, `AvatarViewModel.State` должен быть `Error`.

- **FR-005**: В отсутствие активности (нет отправки, нет ошибок, нет недавнего Success-таймера), `AvatarViewModel.State` должен быть `Idle`.

- **FR-006**: Если конфигурационный параметр `Desktop:Avatar:Enabled` равен `false`, `AvatarViewModel.State` должен быть `Hidden`. `AvatarPanel` в этом состоянии не должен занимать видимое место в окне (свёрстан, но скрыт).

- **FR-007**: `AvatarPanel` должен отображать соответствующее изображение-заглушку из `Assets/Avatars/` для каждого состояния: `Idle` → `idle.png`, `Thinking` → `thinking.png`, `Speaking` → `speaking.png`, `Success` → `success.png`, `Error` → `error.png`. Если файл отсутствует, должен отображаться fallback: цветной круг с текстовой меткой состояния.

- **FR-008**: `AvatarPanel` должен поддерживать три размера через `AvatarViewModel.Size` (enum `AvatarSize`: `Small` ≈ 80×80px, `Medium` ≈ 120×120px, `Large` ≈ 180×180px), значение читается из `Desktop:Avatar:Size`.

- **FR-009**: `AvatarPanel` должен поддерживать четыре позиции через `AvatarViewModel.Position` (enum `AvatarPosition`: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`), значение читается из `Desktop:Avatar:Position`.

- **FR-010**: Ни один тип, свойство или метод Avatar v1 не должен находиться в `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure` или любом другом не-Desktop проекте.

- **FR-011**: Chat pipeline (`ChatViewModel` → `IrisApplicationFacade` → `SendMessageHandler`) не должен быть изменён для поддержки аватара. `ChatViewModel` не должна знать о существовании `AvatarViewModel`.

- **FR-012**: `AvatarViewModel` не должен напрямую вызывать `IIrisApplicationFacade`, `SendMessageHandler`, `IrisDbContext`, `IChatModelClient`, или любой другой адаптер/инфраструктурный сервис.

- **FR-013**: Конфигурационные параметры `Enabled`, `Size`, `Position` и `SuccessDisplayDurationSeconds` должны загружаться из `appsettings.json` (секция `Desktop:Avatar`). Изменение этих параметров не должно требовать правок кода. При отсутствии любого параметра в конфигурации должно использоваться значение по умолчанию — исключения при старте не допускаются.

- **FR-014**: `AvatarViewModel` должен корректно обрабатывать случай, когда `ChatViewModel == null` (не должно происходить при правильной DI-композиции, но код должен быть защищён от `NullReferenceException` через `ArgumentNullException` в конструкторе).

- **FR-015**: Success-таймер должен быть отменён при смене состояния (новая отправка сообщения, возникновение ошибки, отключение аватара) — нельзя допустить переход в `Idle` из `Success`, если состояние уже изменилось.

## 7. Architecture Constraints

- **AC-001**: Avatar — исключительно Desktop concern. Ни один слой вне `Iris.Desktop` не должен знать о существовании аватара, его состояниях, изображениях или анимациях.

- **AC-002**: Направление зависимости: `AvatarViewModel → ChatViewModel` (через подписку на `PropertyChanged` и `CollectionChanged`). Обратное направление (`ChatViewModel → AvatarViewModel`) запрещено. `ChatViewModel` не знает, что за ней наблюдают.

- **AC-003**: `AvatarViewModel` не должен зависеть от `IApplicationEventBus`, `InMemoryApplicationEventBus` или любой другой инфраструктуры событий. Текущие event bus заглушки не дорабатываются.

- **AC-004**: Запрещены прямые зависимости аватара от адаптеров и инфраструктуры: `AvatarPanel → IrisDbContext`, `AvatarPanel → Ollama`, `AvatarViewModel → SendMessageHandler`, `AvatarViewModel → IConversationRepository`, `AvatarViewModel → IChatModelClient`, `AvatarViewModel → IConfiguration` (конфигурация пробрасывается через конструктор при создании, а не запрашивается напрямую).

- **AC-005**: Все аватар-типы (`AvatarState`, `AvatarViewModel`, `AvatarPanel`, `AvatarSize`, `AvatarPosition`) объявлены в `Iris.Desktop` и не экспортируются за его пределы.

- **AC-006**: `AvatarViewModel` наследует `ViewModelBase` (как `ChatViewModel`).

- **AC-007**: Desktop DI композиция (`DependencyInjection.cs`) остаётся composition root и имеет право ссылаться на все адаптеры. Регистрация `AvatarViewModel` в Desktop DI допустима и не нарушает архитектуру.

- **AC-008**: `AvatarViewModel` **не должен** ссылаться на `Iris.Domain.Persona` (PersonaProfile, PersonaState, PersonaTrait — пустые заглушки). Аватар не зависит от доменной персоны в v1.

## 8. Contract Requirements

### Public contracts (Application layer)

Все контракты Application-слоя остаются **без изменений**:

| Контракт | Статус |
|---|---|
| `IIrisApplicationFacade.SendMessageAsync` | Без изменений |
| `SendMessageCommand` | Без изменений |
| `SendMessageResult` | Без изменений |
| `ChatMessageDto` | Без изменений |
| `IApplicationEventBus` | Без изменений (не дорабатывается) |
| `IChatModelClient` | Без изменений |
| `IConversationRepository` / `IMessageRepository` | Без изменений |
| `SendMessageOptions` | Без изменений |

### Desktop contracts

| Контракт | Статус |
|---|---|
| `ChatViewModel` (public observable properties: `IsSending`, `HasError`, `Messages`, `ErrorMessage`, `CanEditInput`) | Без изменений |
| `MainWindowViewModel` | **Расширяется** — добавляется свойство `Avatar` (backward-compatible) |
| `AvatarPanel` control | **Перерабатывается** из заглушки |
| `AvatarViewModel` | **Перерабатывается** из заглушки |
| `AvatarState` enum | **Новый** |
| `AvatarSize` enum | **Новый** |
| `AvatarPosition` enum | **Новый** |

### Configuration contract

| Контракт | Статус |
|---|---|
| `appsettings.json` — секция `Desktop:Avatar` | **Новая** |
| `appsettings.json` — существующие секции (`Database`, `ModelGateway`, `Application:Chat`) | Без изменений |

## 9. Data and State Requirements

### AvatarState enum

| Значение | Смысл | Триггер перехода |
|---|---|---|
| `Idle` | Нет активности, аватар ждёт | Начальное состояние; возврат из Success после таймера; возврат из Error при следующей отправке |
| `Thinking` | Идёт отправка сообщения | `ChatViewModel.IsSending == true` |
| `Speaking` | *Зарезервировано для Voice v1* | Недостижимо в Avatar v1 |
| `Success` | Сообщение успешно отправлено и получен ответ | `IsSending == false`, `HasError == false`, новое assistant-сообщение в `Messages` |
| `Error` | Произошла ошибка при отправке | `IsSending == false`, `HasError == true` |
| `Hidden` | Аватар отключён пользователем | `Desktop:Avatar:Enabled == false` |

### AvatarViewModel state

| Observable Property | Тип | Начальное значение |
|---|---|---|
| `State` | `AvatarState` | `Idle` (если `Enabled == true`), иначе `Hidden` |
| `Size` | `AvatarSize` | Из `appsettings.json`: `Small` / `Medium` / `Large` |
| `Position` | `AvatarPosition` | Из `appsettings.json`: `TopLeft` / `TopRight` / `BottomLeft` / `BottomRight` |

### AvatarSize enum

| Значение | Размер области (px) |
|---|---|
| `Small` | 80 × 80 |
| `Medium` | 120 × 120 |
| `Large` | 180 × 180 |

### AvatarPosition enum

| Значение | Позиция в окне |
|---|---|
| `TopLeft` | Левый верхний угол |
| `TopRight` | Правый верхний угол |
| `BottomLeft` | Левый нижний угол |
| `BottomRight` | Правый нижний угол |

### State transition rules (приоритет)

При одновременном изменении нескольких observable-свойств применяется приоритет:

1. `Hidden` (если `Enabled == false`) — немедленно, отменяет все таймеры.
2. `Thinking` (если `IsSending == true`) — сбрасывает `Error`, отменяет Success-таймер.
3. `Error` (если `IsSending == false` и `HasError == true`).
4. `Success` (если `IsSending == false`, `HasError == false`, новое assistant-сообщение) — запускает таймер.
5. `Idle` (по умолчанию, после истечения Success-таймера).

### Persistence

Avatar **не требует persistence**. Состояния аватара — runtime-only, не сохраняются в SQLite.

### Configuration (appsettings.json)

Новая секция:

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

При отсутствии любого ключа используется значение по умолчанию. Невалидные значения (`"Gigantic"` для Size, `"Center"` для Position) заменяются на default (`Medium` / `BottomRight`).

## 10. Error Handling and Failure Modes

| Failure mode | Требуемое поведение |
|---|---|
| `AvatarViewModel` получает `null` вместо `ChatViewModel` | `ArgumentNullException` в конструкторе — не должно происходить при правильной DI-композиции |
| Файл изображения для состояния отсутствует в `Assets/Avatars/` | Отображать fallback: цветной круг (`Ellipse`) с текстовой меткой состояния (например, «Idle», «Thinking») |
| Секция `Desktop:Avatar` отсутствует в `appsettings.json` | Все параметры используют defaults: `Enabled=true`, `Size=Medium`, `Position=BottomRight`, `SuccessDisplayDurationSeconds=2.0` |
| `Desktop:Avatar:Enabled` имеет невалидное значение (не bool) | Использовать `true` |
| `Desktop:Avatar:Size` имеет невалидное значение | Использовать `Medium` |
| `Desktop:Avatar:Position` имеет невалидное значение | Использовать `BottomRight` |
| `Desktop:Avatar:SuccessDisplayDurationSeconds` ≤ 0 или невалидное | Использовать `2.0` |
| Аватар отключён (`Enabled == false`) во время активного Success-таймера | Таймер отменяется, `State` немедленно становится `Hidden` |
| Новая отправка сообщения во время активного Success-таймера | Таймер отменяется, `State` становится `Thinking` |
| Возникновение ошибки во время активного Success-таймера | Таймер отменяется, `State` становится `Error` |
| Сбой загрузки изображения (I/O error) | Отображать fallback-заглушку; аватар не блокирует UI и не выбрасывает исключение |
| `ChatViewModel.PropertyChanged` выбрасывает исключение | `AvatarViewModel` не должен падать — сохранять предыдущее состояние; Avalonia binding infrastructure изолирует исключения |
| `AvatarViewModel` disposed (закрытие окна) | Success-таймер должен быть остановлен; подписки на `ChatViewModel.PropertyChanged` и `Messages.CollectionChanged` должны быть отписаны во избежание утечек |

## 11. Testing Requirements

### Integration Tests (в `tests/Iris.IntegrationTests/Desktop/`, следуя практике Phase 5)

| # | Тест | Категория | Что проверяется |
|---|---|---|---|
| T-01 | `InitialStateIsIdle` | Positive | При `Enabled=true`, начальный `State == Idle` |
| T-02 | `InitialStateIsHiddenWhenDisabled` | Negative | При `Enabled=false`, `State == Hidden` |
| T-03 | `StateTransitionsToThinkingWhenSending` | State transition | Симуляция `IsSending=true` через `PropertyChanged` → `State == Thinking` |
| T-04 | `StateTransitionsToSuccessThenIdle` | State transition + timer | Симуляция assistant-сообщения → `State == Success` → через `SuccessDisplayDurationSeconds` → `State == Idle` |
| T-05 | `StateTransitionsToErrorOnFailure` | State transition | Симуляция `HasError=true` при `IsSending=false` → `State == Error` |
| T-06 | `ErrorClearsOnNewSend` | State transition | `State == Error` → симуляция `IsSending=true` → `State == Thinking` |
| T-07 | `SuccessTimerCancelledOnNewSend` | Timer cancellation | Установить `State == Success` с активным таймером → симуляция `IsSending=true` → `State == Thinking` (таймер не переводит в Idle позже) |
| T-08 | `SuccessTimerCancelledOnDisable` | Timer + Config | `State == Success` → `Enabled = false` → `State == Hidden` (таймер отменён) |
| T-09 | `IsEnabledReadsFromConfiguration` | Config binding | `AvatarViewModel` получает `true`/`false` из `IConfiguration["Desktop:Avatar:Enabled"]` |
| T-10 | `SizeReadsFromConfiguration` | Config binding | `AvatarViewModel.Size` соответствует `IConfiguration["Desktop:Avatar:Size"]` |
| T-11 | `PositionReadsFromConfiguration` | Config binding | `AvatarViewModel.Position` соответствует `IConfiguration["Desktop:Avatar:Position"]` |
| T-12 | `SuccessDurationReadsFromConfiguration` | Config binding | `AvatarViewModel` использует значение из `IConfiguration["Desktop:Avatar:SuccessDisplayDurationSeconds"]` |
| T-13 | `DefaultsUsedWhenConfigMissing` | Config / Negative | При отсутствии секции `Desktop:Avatar` используются defaults |
| T-14 | `DefaultsUsedWhenConfigInvalid` | Config / Negative | При невалидных значениях Size/Position используются defaults |
| T-15 | `AvatarViewModelDoesNotReferenceProhibitedLayers` | Architecture | Проверка отсутствия зависимостей от Application/Domain/Persistence/ModelGateway |

### Manual Smoke

| # | Сценарий | Ожидаемое поведение |
|---|---|---|
| M-01 | Запуск Desktop, Ollama работает | Аватар виден, `Idle`. |
| M-02 | Отправить сообщение | Аватар переходит в `Thinking` на время отправки. |
| M-03 | Дождаться ответа | Аватар кратко показывает `Success`, затем возвращается в `Idle`. |
| M-04 | Остановить Ollama, отправить сообщение | Аватар показывает `Thinking`, затем `Error`. |
| M-05 | `Desktop:Avatar:Enabled = false`, перезапустить | Аватар не отображается. |
| M-06 | Сменить `Size` на `Large` | Область аватара увеличивается. |
| M-07 | Сменить `Position` на `TopLeft` | Аватар перемещается в левый верхний угол. |

### Required Checks

```bash
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-restore
```

Все существующие тесты (98 на момент Phase 5) должны продолжать проходить. Новые Avatar-тесты (~15) должны проходить.

## 12. Documentation and Memory Requirements

После реализации обновить:

- `.agent/PROJECT_LOG.md` — запись о завершении Phase 5.5.
- `.agent/overview.md` — обновить текущую фазу (Phase 5.5 complete → Phase 6 stabilization / Phase 7 architecture safeguards).
- `.agent/log_notes.md` — записать найденные проблемы (если есть).

Не требуется обновлять:

- `.agent/architecture.md` — Avatar не меняет архитектурные границы (Desktop host уже описан).
- `.agent/first-vertical-slice.md` — Avatar явно вне первого вертикального среза (документ завершён после Phase 5).
- `AGENTS.md` — без изменений.
- `docs/implementation/` — отдельный документ не создаётся; данная спецификация является достаточным артефактом для Design/Plan фаз.

## 13. Acceptance Criteria

- [ ] `dotnet build .\Iris.slnx` passes with 0 errors and 0 warnings.
- [ ] `dotnet test .\Iris.slnx --no-restore` passes: все существующие тесты + ~15 новых Avatar-тестов, 0 failed.
- [ ] `AvatarState` enum существует в `Iris.Desktop` с шестью значениями: `Idle`, `Thinking`, `Speaking`, `Success`, `Error`, `Hidden`. Значение `Speaking` документировано как зарезервированное для Voice v1.
- [ ] `AvatarViewModel` наследует `ViewModelBase`, зарегистрирован в Desktop DI (Transient), имеет observable-свойства `State`, `Size`, `Position`.
- [ ] `MainWindowViewModel` имеет свойство `Avatar` типа `AvatarViewModel`.
- [ ] `MainWindow.axaml` отображает `AvatarPanel` с привязкой `DataContext="{Binding Avatar}"`.
- [ ] При отправке сообщения `State == Thinking`.
- [ ] При успешном ответе `State == Success` на `SuccessDisplayDurationSeconds`, затем `Idle`.
- [ ] При ошибке `State == Error`.
- [ ] При `Enabled == false` `State == Hidden`, `AvatarPanel` скрыт.
- [ ] Success-таймер отменяется при новой отправке, возникновении ошибки или отключении аватара.
- [ ] `appsettings.json` содержит секцию `Desktop:Avatar` с `Enabled`, `Size`, `Position`, `SuccessDisplayDurationSeconds`.
- [ ] При отсутствии любого параметра конфигурации используется значение по умолчанию.
- [ ] `AvatarPanel` отображает изображения из `Assets/Avatars/` с fallback-заглушкой при отсутствии файла.
- [ ] Размер аватара соответствует `Desktop:Avatar:Size`.
- [ ] Позиция аватара соответствует `Desktop:Avatar:Position`.
- [ ] Ни один файл в `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure` не изменён.
- [ ] Dependency audit: `Iris.Desktop` не ссылается на Ollama напрямую; Avatar-типы не проникают за пределы Desktop.
- [ ] Manual smoke: аватар визуально реагирует на отправку/ответ/ошибку/отключение при запущенном Desktop (M-01–M-07).

## 14. Open Questions

No blocking open questions.
