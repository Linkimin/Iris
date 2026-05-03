# Architecture Design: Premium UI Overhaul (Visual Foundation) V1

## 1. Design Goal

Создать единый визуальный и структурный фундамент `Iris.Desktop` (цвета, шрифты, базовый layout с боковой панелью, кастомное безрамочное окно), обеспечивающий концепцию "интеллектуального, премиального AI-компаньона". Дизайн должен строго оставаться в слое UI (Views/Styles), не нарушая работу существующих ViewModels и архитектурных границ Avalonia/MVVM.

## 2. Specification Traceability

Дизайн опирается на спецификацию `Premium UI Overhaul V1` (2026-05-03).
- **FR-001, FR-002 (Палитра):** Решается через централизованный XAML `ResourceDictionary` (`IrisTheme.axaml`), где определены все `SolidColorBrush` и `Color` ресурсы.
- **FR-003 (Шрифты):** Решается через глобальные XAML `FontFamily` ресурсы.
- **FR-004, FR-005, FR-006 (MainWindow chrome/размер):** Решается через свойства `Window` (ExtendClientArea, TransparencyLevelHint).
- **FR-007, FR-008 (Layout / отказ от TabControl):** Решается через корневой `Grid` в `MainWindow.axaml` с колонками `200,*`.
- **FR-009, FR-010, FR-011 (Стили сообщений):** Решается через создание `ChatMessageTemplateSelector` (или двух раздельных `DataTemplate` в `ItemsControl`), который читает уже существующее свойство `ChatMessageViewModelItem.IsUser`.
- **FR-012 (Thought Log):** Решается через `TextBlock` с триггером видимости на `IsSending`.
- **FR-015, FR-016 (Avatar кольцо/анимации):** Решается через добавление `Ellipse` в `AvatarPanel.axaml` и использование `Style` селекторов с `Setter`/`Animation` или `Transitions` для реакции на `State`.
- **AC-001..AC-007 (Ограничения слоев):** Дизайн не вводит новых ViewModels, не трогает Application/Domain, не добавляет code-behind логики.

## 3. Current Architecture Context

- `Iris.Desktop` — хост-проект (Avalonia 12.0.1, MVVM через CommunityToolkit).
- Корневой компонент — `MainWindow` с `TabControl`.
- ViewModels (`ChatViewModel`, `MemoryViewModel`, `AvatarViewModel`) инстанцируются через DI и предоставляют стабильные `ICommand` и `ObservableCollection`.
- `ChatMessageViewModelItem` содержит `bool IsUser` и `bool IsAssistant`.
- Подключен базовый пакет `Avalonia.Fonts.Inter`.
- `IrisTheme.axaml` существует, но пуст. `DarkTheme.axaml` существует, но пуст.

## 4. Proposed Design Summary

Мы превращаем пустой `IrisTheme.axaml` в полноценную "Design System" для Iris, где декларируются все цвета, кисти, шрифты и базовые стили контролов (например, кастомный `TextBox` или кнопки). `MainWindow.axaml` переписывается с использованием `<Window.TransparencyLevelHint>` для достижения эффекта Acrylic/Mica, а `TabControl` заменяется на `Grid` с явным разделением зон. `ChatMessageBubble.axaml` удаляется (или становится базовым контейнером), а в `ChatView` внедряется селектор шаблонов (один для Iris, один для User). Аватар получает обертку с кольцом-индикатором и простыми CSS-подобными `Style` селекторами Avalonia для анимации.

## 5. Responsibility Ownership

| Responsibility | Owner | Notes |
|---|---|---|
| Цветовая палитра и шрифты (Design System) | `Themes/IrisTheme.axaml` | Все глобальные константы UI. |
| Окно (Chrome, Backdrop, Layout) | `Views/MainWindow.axaml` | Владеет прозрачностью и структурой "Память слева, Чат по центру". |
| Выбор визуального шаблона сообщения | `Views/ChatView.axaml` | Определяет, как рисовать `User` и как `Iris` на основе `IsUser`. |
| Анимация состояний (Thinking/Idle) | `Controls/AvatarPanel.axaml` | Управляется исключительно Avalonia Styles, биндинг к `State`. |
| Данные для UI (Текст, Флаги, Ошибки) | Существующие ViewModels | Изменений не требуется. |

## 6. Component Design

### `Themes/IrisTheme.axaml`
- **Owner layer:** UI (Desktop).
- **Responsibility:** Хранение палитры (`Iris.Background`, `Iris.AccentPrimary` и т.д.) и шрифтов (`Iris.FontFamily.Ui`, `Iris.FontFamily.Mono`).
- **Inputs:** None.
- **Outputs:** XAML-ресурсы, доступные глобально через `DynamicResource`.
- **Must not do:** Не должен содержать разметку окон (layout).

### `Views/MainWindow.axaml`
- **Owner layer:** UI (Desktop).
- **Responsibility:** Главный контейнер.
- **Особенности:**
  - `ExtendClientAreaToDecorationsHint="True"`
  - `ExtendClientAreaTitleBarHeightHint="-1"`
  - `SystemDecorations="BorderOnly"` (опционально, если нужно).
  - `TransparencyLevelHint="Mica, AcrylicBlur, Transparent, None"`
  - `Background="Transparent"` (фон определяется операционной системой для эффекта, либо резервным цветом). Корневой `Panel` получает `<ExperimentalAcrylicBorder>` или `SolidColorBrush` с Opacity=0.7.
- **Must not do:** Не должен управлять логикой закрытия приложения в code-behind (использовать системные механизмы Avalonia или стандартный Window TitleBar).

### `Controls/Chat/ChatMessageTemplateSelector` (опциональный C#-класс)
Вместо написания логики в `axaml.cs`, если XAML `DataTemplate` не поддерживает условный выбор "из коробки" без дополнительных усилий, мы добавим класс-наследник `IDataTemplate` в `Iris.Desktop/Controls/Chat/ChatMessageTemplateSelector.cs`.
- **Owner layer:** UI (Desktop / Controls).
- **Responsibility:** Возвращать шаблон User или шаблон Iris в зависимости от `item is ChatMessageViewModelItem { IsUser: true }`.
- **Collaborators:** Используется в `ItemsControl.ItemTemplate` в `ChatView.axaml`.

### `Controls/Avatar/AvatarPanel.axaml`
- **Owner layer:** UI (Desktop).
- **Responsibility:** Отрисовка аватара и его индикатора состояния.
- **Изменения:**
  - Обернуть `Image` в `Panel` или `Grid`.
  - Добавить `Ellipse` поверх `Image` (Z-Index выше).
  - Определить `<Style Selector="Ellipse.Thinking">` с `<Style.Animations>`, который манипулирует `Opacity` кисти `Iris.AccentPrimary`.

## 7. Contract Design

Ни один публичный C# контракт Application, Domain, ModelGateway или Shared слоев не изменяется. Изменения происходят исключительно в XAML и UI-локальных хелперах.

### `ChatMessageTemplateSelector : IDataTemplate` (New)
- **Owner:** `Iris.Desktop`.
- **Consumers:** `ChatView.axaml`.
- **Shape:** Реализует `Control? Build(object? param)` и `bool Match(object? data)`. Содержит два публичных свойства `DataTemplate UserTemplate` и `DataTemplate IrisTemplate`.
- **Compatibility:** Чисто UI-внутренний компонент.

## 8. Data Flow

### Primary Flow (Рендеринг нового сообщения)
1. `ChatViewModel.SendMessageAsync` получает ответ от Application.
2. Новые `ChatMessageViewModelItem` добавляются в `ObservableCollection<Messages>`.
3. `ChatView.axaml` (`ItemsControl`) обновляется.
4. `ChatMessageTemplateSelector` вызывается для каждого нового элемента. Для пользователя рендерится шаблон с `Iris.Surface` фоном и выравниванием вправо; для ассистента рендерится шаблон без фона с акцентной линией `#7C4DFF`.
5. Во время ожидания (между отправкой и получением), `ChatViewModel.IsSending` равно `true`.
6. Свойство `IsSending` включает `Thought Log` панель в UI и переводит `AvatarViewModel.State` в `Thinking`.
7. `AvatarPanel.axaml` применяет стиль анимации к кольцу (пульсация).

## 9. Data and State Design

Изменений в БД, кэшировании, персистентности — нет. 
Анимации (пульсация) используют встроенный в Avalonia движок кадров (`KeyFrame` / `Animation`) и живут исключительно в визуальном дереве. Никаких C#-таймеров для анимации кольца не вводится. Существующий C#-таймер для состояния `Success` в `AvatarViewModel` остается нетронутым.

## 10. Error Handling and Failure Modes

- **Срыв загрузки шрифта JetBrains Mono (Fallback):** Если шрифт отсутствует в системе, UI `FontFamily` будет объявлен как `"JetBrains Mono, Cascadia Mono, Consolas, monospace"`. Avalonia автоматически выберет следующий доступный моноширинный шрифт.
- **ОС не поддерживает Mica/Acrylic:** Указанный массив `TransparencyLevelHint` заставит Avalonia откатиться к `None`. В этом случае фоновому слою будет присвоен SolidColorBrush цвета `Iris.Background` (`#0D0D12`). Текст останется читаемым.
- **Avalonia 12 Chrome глюки на Linux:** Возможны артефакты при `ExtendClientAreaToDecorationsHint`. В дизайне не предусмотрена специальная ОС-зависимая логика; это принимаемый риск.

## 11. Configuration and Dependency Injection Impact

Изменений в конфигурации или DI нет. Все ViewModels уже зарегистрированы и корректно пробрасываются в `MainWindowViewModel`.

## 12. Security and Permission Considerations

Визуальное обновление не затрагивает безопасность.

## 13. Testing Design

- **Unit тесты:** UI-тестирование (цвета/шрифты) в Avalonia обычно не проводится через unit-тесты. Новый класс `ChatMessageTemplateSelector` тривиален и не требует отдельного покрытия.
- **Manual Verification:** Основной упор делается на операторский Smoke тест (M-UI-01..06) согласно спецификации, подтверждающий корректность Mica-эффекта, прозрачности и анимаций.

## 14. Options Considered

**Опция 1: Использование `Interaction.Behaviors` для анимаций.**
- *Отвергнуто:* Avalonia 12 предоставляет мощные XAML-стили и XAML-анимации (`<Style.Animations>`). Использование Behaviors создало бы лишнюю C# обертку для того, что должно быть чистым визуальным ресурсом.

**Опция 2: Создание отдельного NuGet-пакета или AvaloniaResource для шрифта JetBrains Mono.**
- *Выбрано (с нюансом):* Мы не добавляем NuGet пакет (AC-003). Мы используем системный fallback (A-001 из спецификации) — `FontFamily="JetBrains Mono, Cascadia Mono, Consolas, monospace"`. Если пользователь хочет идеальный вид — он ставит шрифт в систему. Скачивание TTF файла напрямую и добавление его в репозиторий в рамках этого эпика возможно, но требует ручного действия в плане (Plan). Дизайн предписывает использовать Fallback, чтобы не раздувать репозиторий бинарниками.

## 15. Risks and Trade-Offs

- **Производительность:** `TransparencyLevelHint` с `Mica` или `AcrylicBlur` может увеличить GPU-overhead на старых машинах. Avalonia сама решает, как рендерить, но это риск.
- **Cross-platform UI:** Custom chrome (`ExtendClientAreaToDecorationsHint`) может вести себя по-разному на Windows 11, Windows 10, macOS и Linux (Wayland vs X11). Наш фокус — Windows-совместимость с грациозной деградацией.
- **Удаление TabControl:** Пользователь теряет явное переключение между окнами. Однако, `Memory` теперь всегда видна (боковая панель), что улучшает UX контекста (спецификация FR-008). 

## 16. Acceptance Mapping

- **FR-001..FR-003** → `IrisTheme.axaml`.
- **FR-004..FR-008** → `MainWindow.axaml` (Grid, TransparencyLevelHint).
- **FR-009..FR-011** → `ChatView.axaml` + `ChatMessageTemplateSelector`.
- **FR-012** → `ChatView.axaml` (Thought Log TextBlock).
- **FR-015** → `AvatarPanel.axaml` (Ellipse + XAML Animations).
- **AC-001..AC-010** → Гарантируется отсутствием изменений в C# логике.

## 17. Blocking Questions

No blocking open questions.

***

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

- Avalonia 12.0.1 стабильно поддерживает `ExtendClientAreaToDecorationsHint` и `TransparencyLevelHint`.
- `ChatMessageTemplateSelector` разрешен как UI-internal C# класс, так как не нарушает границ слоев и не является ViewModel'ью.
- Шрифт JetBrains Mono будет использован через системный fallback, без внедрения .ttf-файла в проект, для упрощения сборки (в соответствии с A-001).

## Blocking Questions

No blocking questions.

***

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | Спецификация 2026-05-03 Premium UI Overhaul V1 |
| B — Design | ✅ Satisfied | This design |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |