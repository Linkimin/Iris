# Specification: Premium UI Overhaul (Visual Foundation) — V1

## 1. Problem Statement

Текущий UI слой `Iris.Desktop` визуально утилитарен и не соответствует продуктовой задаче — ощущению "интеллектуального премиального AI-компаньона", закрепленному в `.agent/mem_library/02_user_experience.md` (§3, §17) и `.agent/mem_library/03_iris_persona.md` (§3 — calm, technically competent).

Конкретные факты текущего состояния (по результатам инспекции, 2026-05-03):

- `App.axaml` использует только `<FluentTheme />` без переопределения цветов и шрифтов;
- `Themes/IrisTheme.axaml` и `Themes/DarkTheme.axaml` существуют как пустые `ResourceDictionary` (плейсхолдеры);
- `Views/MainWindow.axaml` использует системную рамку (chrome) и `TabControl` со вкладками "Чат" / "Память", не отражает концепцию единого пространства;
- `Views/ChatView.axaml` содержит хардкод цветов (`#101216`, `#151922`, `#3B82F6`) и не использует ресурсные кисти;
- `Controls/Chat/ChatMessageBubble.axaml` рисует одинаковые "плашки" для пользователя и Iris, без визуального разграничения автора;
- `Views/MemoryView.axaml` использует `DynamicResource SystemControlForegroundBaseMediumBrush` — то есть наследует FluentTheme defaults и не выровнен с остальной визуальной системой;
- `Controls/Avatar/AvatarPanel.axaml` показывает иконки состояний, но не имеет кольца-индикатора и пульсации;
- `Avalonia.Fonts.Inter` уже добавлен как пакет — основной шрифт доступен из коробки. JetBrains Mono пакета нет.

Без единой визуальной системы (палитра, типографика, ресурсы, layout) дальнейшие итерации UI (Fluid UI, Modes, Memory highlighting) будут продолжать наслаивать локальные хардкоды и ускорять визуальный долг.

## 2. Goal

Внедрить **визуальный фундамент v1** для `Iris.Desktop`, согласованный с концепцией "Deep Obsidian / Visual Silence":

- единая палитра, типографика и общие стили оформлены как централизованные Avalonia-ресурсы;
- `MainWindow` использует кастомный chrome и фон `#0D0D12` с попыткой Mica/Acrylic поверх ОС;
- единый рабочий layout: левая панель Memory (200px) + центральный чат + аватар в правом нижнем углу с кольцом-индикатором, привязанным к существующему `AvatarState`;
- сообщения пользователя визуально отделены от сообщений Iris (правый край, полупрозрачные блоки vs. чистый текст с левой акцентной линией);
- статус "Thinking…" оформлен как стилизованная заглушка Thought Log;
- никаких изменений во ViewModels, Application, Domain, Persistence или контрактах.

Цель верифицируема: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, архитектурные тесты остаются зелеными; ручной smoke подтверждает визуальное соответствие.

## 3. Scope

### In Scope

1. Глобальная цветовая палитра как Avalonia `ResourceDictionary` (`Themes/IrisTheme.axaml` или новый `Themes/IrisColors.axaml`):
   - `Iris.Background` `#0D0D12`,
   - `Iris.Surface` `#1E1E23` с Opacity 0.7 (для acrylic-панелей),
   - `Iris.AccentPrimary` `#7C4DFF` (Deep Iris),
   - `Iris.AccentSecondary` `#00E5FF` (Cold Logic),
   - `Iris.TextPrimary` `#E0E0E0`,
   - `Iris.TextMuted` `#B0BEC5`,
   - `Iris.TextLog` `#B0BEC5` с Opacity 0.4 для Thought Log.
2. Типографические ресурсы:
   - `Iris.FontFamily.Ui` = Inter (через уже подключенный `Avalonia.Fonts.Inter`);
   - `Iris.FontFamily.Mono` = JetBrains Mono **если** удастся подключить через системный fallback или embedded resource без добавления нового пакета; иначе — fallback на `Cascadia Mono`/`Consolas` (см. §15 Assumptions);
   - стандартные размеры: 13px UI, 12px Mono, 10px All-Caps caption (LetterSpacing=1).
3. `MainWindow.axaml`:
   - `ExtendClientAreaToDecorationsHint=True`, `SystemDecorations=BorderOnly` (или эквивалент Avalonia API), `TransparencyLevelHint` массив включающий `Mica`, `AcrylicBlur`, `Transparent`, fallback `None`;
   - размер по умолчанию 1200×800, минимальный 900×600;
   - кастомная заголовочная зона с минимальными контролами окна (close/minimize/maximize) — отрисовка средствами Avalonia, поверх обсидианового фона;
   - layout: `Grid` с колонками Memory(200) | Chat(*); правый-нижний slot для аватара (Z-order поверх чата, `IsHitTestVisible=False`).
4. Отказ от `TabControl` в пользу единого окна "Чат + Память сбоку". Содержимое Settings/Permissions остается недостижимым через UI v1 (см. §4 Out of Scope).
5. `ChatView.axaml`:
   - удалены хардкод цветов, всё переведено на ресурсные кисти;
   - вверху чата — однострочная "лог"-полоса (Thought Log placeholder), биндинг к существующему `IsSending` (`"Thinking…"` → отображается; иначе пусто или статичный заголовок). Шрифт — Mono, Opacity 0.4;
   - область сообщений без рамки-контейнера, единый фон сцены;
   - Input: `TextBox` без видимой рамки, акцентная линия снизу `#7C4DFF`, кнопка Send — текстовая, акцентного цвета, без заливки (или с тонкой рамкой).
6. `ChatMessageBubble.axaml` (или замена на два DataTemplate'а):
   - сообщения автора `User`: полупрозрачный блок `RGBA(30,30,35,0.7)` с CornerRadius=8, прижатый вправо, MaxWidth ≤ 60% доступной ширины;
   - сообщения автора `Iris`: без фона; вертикальная акцентная линия (`#7C4DFF`, толщина 2px) слева; текст `#E0E0E0`; ширина — естественная;
   - выбор шаблона по существующему свойству автора `ChatMessageViewModelItem` (через `DataTemplateSelector` или `IsUser`-флаг — без изменения публичного API VM, разрешено добавление вычисляемого read-only свойства, если оно тривиально выводится из существующих данных, см. §15).
7. `MemoryView.axaml`:
   - переоформление под левую панель: ширина 200px, прозрачный фон, типографика 11–13px, разделители `#2A3140` или равноценный приглушенный цвет на фоне обсидиана;
   - сохранение всей текущей функциональности (`Remember`, `Forget`, список, ошибки) без изменения биндингов;
   - визуальные группы-заголовки All-Caps caption (`Active Project`, `Recognized Patterns`) допускаются только как **статичный визуальный декор**, помеченный комментарием `<!-- placeholder: requires backend -->`. При отсутствии данных эти группы скрыты.
8. `AvatarPanel.axaml`:
   - вокруг текущего изображения добавляется кольцо `Ellipse` толщиной 1–2px;
   - цвет/анимация кольца:
     - `Thinking` → пульсация цветом `#7C4DFF` (Avalonia `Animation` на `Opacity` или `StrokeThickness`);
     - `Idle` → статичный приглушенный `#7C4DFF` Opacity 0.3 или `#00E5FF` Opacity 0.2 (см. §15);
     - `Success` → короткая вспышка `#00E5FF`;
     - `Error` → статичный приглушенный красный (используется существующий error-asset);
     - `Hidden` → не отображается.
9. Регистрация всех ресурсов через `App.axaml` (`Application.Resources` + `Application.Styles`) c сохранением `<FluentTheme />` как базы.

### Out of Scope

- Сложные покадровые анимации появления/исчезновения сообщений (`Fluid UI` эпик).
- Реальный Thought Log из `Application` (события "thinking", "memory recall", "tool plan") — пока только биндинг к `IsSending`.
- Подсветка используемых memory-фрагментов в реальном времени.
- Распознавание "User Fatigue" и других паттернов.
- Разные цветовые темы под режимы (Coding / Brainstorming / Casual).
- Компактный widget-режим, всегда-сверху, mini-overlay.
- Светлая тема (Light theme), переключение тем рантайм.
- Локализация (надписи остаются как сейчас — RU/EN смешанные, см. §15).
- Перенос содержимого `SettingsView`/`PermissionsView` в новый layout (эти View остаются нетронутыми; они и сейчас не доступны через UI согласно `MainWindow.axaml`).
- Изменение `AvatarPosition` enum или логики выбора угла (новая визуальная позиция фиксированная — нижний правый).

### Non-Goals

- Изменение публичного API `ChatViewModel`, `MemoryViewModel`, `AvatarViewModel`, `MainWindowViewModel`, кроме тривиального добавления read-only computed-свойств для UI (если потребуется для DataTemplate selection — см. §15).
- Любые изменения в `Iris.Application`, `Iris.Domain`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Shared`.
- Любые изменения схемы БД, миграций, `IrisDbContext`.
- Добавление новых NuGet пакетов, кроме случая, описанного в §15 Assumptions (JetBrains Mono fallback).
- Замена `FluentTheme` на собственную тему "с нуля" — `FluentTheme` остается базой, поверх неё накладываются Iris-ресурсы.
- Любая работа с code-behind, нарушающая правило "никакой бизнес-логики в `.axaml.cs`".

## 4. Current State

Установленные факты на момент написания спецификации (HEAD: `feat/avatar-v1-and-opencode-v2`, dirty tree, фаза 8 Memory v1):

- Окно: системный chrome, `Title="Iris"`, размеры по умолчанию не заданы, `TabControl` с двумя вкладками.
- Ресурсы тем: `IrisTheme.axaml` и `DarkTheme.axaml` пусты; `App.axaml` подключает только `<FluentTheme />`.
- Цвета: хардкод в `ChatView.axaml` (`#101216`, `#151922`, `#1B2130`, `#2A3140`, `#3B82F6`, `#F2F4F8`, `#AAB2C0`, `#F08A8A`); в `MemoryView.axaml` — динамические системные кисти Fluent.
- Шрифты: `Avalonia.Fonts.Inter` подключен как пакет; явное использование Inter в XAML отсутствует. JetBrains Mono нет.
- Аватар: `AvatarPanel` показывает 5 PNG по `AvatarState`; кольца/анимации нет; позиционируется в `MainWindow` через `AvatarPositionToAlignmentConverter` + Margin=16.
- ViewModels: `ChatViewModel` имеет публичные `Messages`, `InputText`, `IsSending`, `HasError`, `ErrorMessage`, `CanEditInput`, `SendMessageCommand`. `ChatMessageViewModelItem` (по результатам биндинга) содержит `Author`, `Content`. `MemoryViewModel` имеет `Memories`, `NewMemoryContent`, `RememberCommand`, `ForgetCommand`, `ErrorMessage`, `IsLoading`. `AvatarViewModel` экспонирует `State`, `Size`, `Position`.
- Тесты: 190/190 проходят. Architecture tests 12/12 проходят. `dotnet format --verify-no-changes` EXIT_CODE=0.
- CI: `.github/workflows/ci.yml` запускает build/test/format на push/PR.

## 5. Affected Areas

Только `Iris.Desktop`:

- `App.axaml`, `App.axaml.cs` — регистрация ресурсов и стилей.
- `Themes/IrisTheme.axaml` — наполнение единым `ResourceDictionary` (палитра, шрифты, типографические ресурсы, общие Style'ы).
- `Themes/DarkTheme.axaml` — оставить как зарезервированный плейсхолдер либо удалить (см. §15).
- `Views/MainWindow.axaml`, `Views/MainWindow.axaml.cs` — кастомный chrome, новый layout, отказ от `TabControl`. `axaml.cs` остается тонким; никакой бизнес-логики.
- `Views/ChatView.axaml` — переоформление, удаление хардкода.
- `Views/MemoryView.axaml` — переоформление под левую панель.
- `Controls/Chat/ChatMessageBubble.axaml` — два визуальных стиля по автору **или** замена на DataTemplate selection в `ChatView`.
- `Controls/Avatar/AvatarPanel.axaml` — добавление кольца + анимации состояний.
- Возможно: `Converters/` — добавление BoolToVisibilityConverter / IsUserConverter, если необходимо для биндинга шаблонов сообщений (без изменения существующих конвертеров).
- Возможно: `Models/ChatMessageViewModelItem.cs` — добавление вычисляемого read-only `IsUser` (см. §15). Не считается изменением публичного контракта Application слоя — это UI-локальная модель.

Не затрагиваются (must remain unchanged):

- `Iris.Application/**`
- `Iris.Domain/**`
- `Iris.Persistence/**`
- `Iris.ModelGateway/**`
- `Iris.Shared/**`
- `Views/SettingsView.axaml`, `Views/PermissionsView.axaml`, `Views/ShellView.axaml` — остаются как есть (вне UI v1).
- `appsettings.json`, `Hosting/*`, `DependencyInjection.cs` — без изменений.

## 6. Functional Requirements

- **FR-001 Палитра:** Все цвета фона/текста/акцентов в обновленных XAML-файлах должны браться через `StaticResource` или `DynamicResource`, ссылающийся на ресурсы `Themes/IrisTheme.axaml`. Хардкод HEX в `ChatView`, `MemoryView`, `MainWindow`, `ChatMessageBubble`, `AvatarPanel` запрещен (исключения допустимы только в самом `IrisTheme.axaml`).
- **FR-002 Палитра — обязательные ключи:** В `IrisTheme.axaml` определены минимум: `Iris.Background`, `Iris.Surface`, `Iris.AccentPrimary`, `Iris.AccentSecondary`, `Iris.TextPrimary`, `Iris.TextMuted`, `Iris.Border` (или `Iris.Divider`). Значения соответствуют §3.1.
- **FR-003 Шрифты:** Основной UI-текст использует `Iris.FontFamily.Ui` (Inter). Логи / Thought Log / технические подписи используют `Iris.FontFamily.Mono`. Ресурсы шрифтов определены в `IrisTheme.axaml`.
- **FR-004 Главное окно — chrome:** `MainWindow` использует кастомный chrome (`ExtendClientAreaToDecorationsHint=True` + соответствующие `ChromeHints`). Visual chrome содержит закрытие/сворачивание/разворачивание окна, реализованные стандартными командами Avalonia без code-behind бизнес-логики.
- **FR-005 Главное окно — прозрачность:** `MainWindow.TransparencyLevelHint` включает массив `Mica, AcrylicBlur, Transparent, None`. Если ОС не поддерживает Mica/Acrylic, окно корректно деградирует до сплошного `Iris.Background` без визуальных артефактов.
- **FR-006 Главное окно — размер:** `Width=1200`, `Height=800`, `MinWidth=900`, `MinHeight=600`.
- **FR-007 Layout:** Корневой контейнер — `Grid` с колонками `200,*`. Левая колонка содержит `MemoryView`. Правая — `ChatView`. `AvatarPanel` лежит в правой колонке как overlay (`Grid` с Z-order или `Panel`), `IsHitTestVisible=False`, `HorizontalAlignment=Right`, `VerticalAlignment=Bottom`, `Margin` ~16–24.
- **FR-008 Отказ от TabControl:** `TabControl` со вкладками "Чат"/"Память" удалён. Memory интегрирована как постоянная левая панель.
- **FR-009 Сообщения пользователя:** Шаблон сообщения автора "User" — `Border` с `Background` `Iris.Surface` (Opacity 0.7), `CornerRadius=8`, `HorizontalAlignment=Right`, `MaxWidth` ≤ 60% ширины контейнера сообщений. Шрифт — `Iris.FontFamily.Ui` 13px, цвет `Iris.TextPrimary`.
- **FR-010 Сообщения Iris:** Шаблон сообщения автора "Iris" — без `Border.Background`; слева вертикальная линия (`Rectangle`/`Border.BorderThickness="2,0,0,0"`) цвета `Iris.AccentPrimary`. Текст `Iris.TextPrimary`, шрифт `Iris.FontFamily.Ui` 13px, line-height 1.5.
- **FR-011 Выбор шаблона по автору:** Выбор между двумя шаблонами происходит на основе данных, уже доступных в `ChatMessageViewModelItem`. Если необходимо, добавляется тривиальное read-only свойство `IsUser` (вычисляется из существующих полей). Никакой логики по обращению к `Application`/`Domain` в UI не появляется.
- **FR-012 Thought Log placeholder:** В верхней части `ChatView` присутствует однострочный `TextBlock` со шрифтом `Iris.FontFamily.Mono`, цвет `Iris.TextLog` (Opacity ≈ 0.4). Текст: при `IsSending=true` отображается `"[core] thinking…"` (или эквивалент); при `IsSending=false` строка скрыта (`IsVisible=False`) или содержит пустую строку. Никаких других источников данных для Thought Log в v1 нет.
- **FR-013 Input:** Поле ввода имеет прозрачный/обсидиановый фон, нижнюю акцентную линию `Iris.AccentPrimary`, без видимой стандартной рамки. Размер шрифта 13px Inter. Поведение клавиш (Enter, Shift+Enter), команда `SendMessageCommand`, плейсхолдер — без изменений.
- **FR-014 Кнопка Send:** Стилизована как акцентная (текст `Iris.AccentPrimary` или фон `Iris.AccentPrimary` с белым текстом — выбирается дизайнерским решением реализации в рамках этих ограничений). Hover/Pressed/Disabled состояния явно определены через Avalonia Selectors.
- **FR-015 Аватар — кольцо:** Поверх изображения аватара рисуется `Ellipse` с `Stroke` шириной 1–2px. Цвет и поведение зависят от `AvatarState`:
  - `Thinking`: пульсация цвета `Iris.AccentPrimary`, период 1.2–2.0 сек, `Opacity` циклически меняется;
  - `Idle`: статичный `Iris.AccentPrimary` Opacity ≤ 0.4;
  - `Success`: короткая вспышка `Iris.AccentSecondary` (1.5–2.5 сек) — синхронизирована с уже существующим Success-таймером в `AvatarViewModel`;
  - `Error`: статичный приглушенный красный (используется существующий error-визуал);
  - `Hidden`: кольцо не отображается.
- **FR-016 Аватар — позиционирование:** В рамках UI v1 аватар **всегда** в правом нижнем углу окна с фиксированными отступами. Существующий механизм `AvatarPosition` сохраняется в коде (не удалять и не менять), но `MainWindow` явно использует `Right`/`Bottom` (то есть переопределяет Position через XAML-биндинги). Это решает вопрос визуального наложения на Send-кнопку (см. P2 backlog R "Avatar visually overlaps Send button" — должно быть устранено по acceptance criteria).
- **FR-017 MemoryView — стиль:** В новой левой панели применяются ресурсные кисти, шрифт Inter 13px (контент) и 10px All-Caps (caption), разделители `Iris.Border`. Все существующие биндинги (`Memories`, `NewMemoryContent`, `RememberCommand`, `ForgetCommand`, `ErrorMessage`) сохраняются в неизменном виде.
- **FR-018 Декоративные плейсхолдеры в MemoryView:** Если в XAML присутствуют визуальные группы вроде "Active Project" или "Recognized Patterns", они помечены комментарием `<!-- placeholder: requires backend, FR-018 -->` и **не отображаются** (`IsVisible=False`) до появления соответствующих ViewModel свойств. Допустимо вообще их не вводить в v1 — решение принимается на этапе `/design`.
- **FR-019 Code-behind чистота:** В `*.axaml.cs` файлах не появляется ни новой бизнес-логики, ни вызовов `Application`/`Domain`/`Persistence`. Любая логика остается в существующих ViewModels.

## 7. Architecture Constraints

- **AC-001 Слой:** Все изменения локализованы в проекте `Iris.Desktop`. Запрещены любые правки в `Iris.Application`, `Iris.Domain`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Shared`.
- **AC-002 Project references:** Не добавляются новые `ProjectReference` в `Iris.Desktop.csproj`.
- **AC-003 Packages:** По умолчанию новые NuGet-пакеты не добавляются. Допускается добавление **не более одного** пакета только для шрифта JetBrains Mono (например `Avalonia.Fonts.JetBrainsMono` или эквивалент), и **только** если решение из §15 Assumptions выбрано в пользу пакета. Любой новый пакет регистрируется в `Directory.Packages.props` через CPM.
- **AC-004 ViewModel API:** Запрещены изменения публичного API существующих ViewModels, **кроме** добавления тривиальных read-only computed-свойств в UI-локальных моделях (`Iris.Desktop.Models.*`). Такие добавления должны быть чисто вычисляемыми и не делать I/O.
- **AC-005 Никаких новых VM:** Новые ViewModels не вводятся.
- **AC-006 Code-behind:** `*.axaml.cs` остаются тонкими (`InitializeComponent` + при необходимости тривиальные UI-helper'ы без обращения к нижним слоям).
- **AC-007 Архитектурные тесты:** `Iris.Architecture.Tests` остаются зелеными без модификаций (12/12). Если изменение требует правки архитектурного теста — это сигнал стоп для `/design`/`/plan`.
- **AC-008 Theme baseline:** `<FluentTheme />` остается подключенным как базовый стиль. Iris-ресурсы накладываются поверх и не заменяют FluentTheme целиком.
- **AC-009 Hosts isolation:** Изменения не влияют на `Iris.Api`, `Iris.Worker`. Они не подключают `Iris.Desktop.Themes` и не должны.
- **AC-010 Globalization neutrality:** UI v1 не вводит новых строк, кроме существующих. Никаких ресурсных файлов локализации не добавляется (вне scope).

## 8. Contract Requirements

| Контракт | Текущее поведение | Требуемое поведение | Совместимость |
|---|---|---|---|
| `IIrisApplicationFacade` | Не меняется | Не меняется | Без изменений |
| `ChatViewModel` public surface (`Messages`, `InputText`, `IsSending`, `HasError`, `ErrorMessage`, `CanEditInput`, `SendMessageCommand`) | Используется UI | Используется UI без изменений | Без изменений |
| `MemoryViewModel` public surface | Используется UI | Используется UI без изменений | Без изменений |
| `AvatarViewModel` public surface (`State`, `Size`, `Position`) | Используется UI | Используется UI без изменений | Без изменений |
| `ChatMessageViewModelItem` (UI-модель) | Содержит `Author`, `Content` | Возможно добавление read-only computed `IsUser` (или эквивалента), если требуется DataTemplate selector | Backward-compatible расширение |
| `AvatarState` enum | Idle/Thinking/Speaking/Success/Error/Hidden | Используется без изменений | Без изменений |
| Публичные API `Iris.Application`/`Iris.Domain` | Не меняются | Не меняются | Без изменений |

Никаких изменений API, REST, БД, конфигурации не вносится.

## 9. Data and State Requirements

- В БД, миграциях, схемах, файлах конфигурации (`appsettings.json`, `appsettings.local.json`) изменений нет.
- В рантайме UI хранит только то, что уже хранит сегодня (через существующие ViewModels). Никаких новых in-memory state-полей в Application/Domain не появляется.
- Жизненный цикл анимаций аватара (пульсация, success-flash) реализуется средствами Avalonia `Animation`/`Transitions` и должен корректно останавливаться при `Hidden` и при `Dispose` `AvatarViewModel` (существующий механизм `CancelSuccessTimer` сохраняется).

## 10. Error Handling and Failure Modes

- **FM-001 Шрифты не найдены:** Если `Avalonia.Fonts.Inter` по какой-то причине не подгрузил Inter, а JetBrains Mono недоступен, UI должен использовать системный sans-serif/monospace fallback без падения. Проверяется визуально.
- **FM-002 Нет поддержки Mica/Acrylic:** На Windows 10/Linux X11/неподдерживаемых WM окно деградирует до сплошного `Iris.Background` (`TransparencyLevelHint` массив гарантирует fallback). Никаких исключений в логи.
- **FM-003 Кастомный chrome недоступен:** Если кастомный chrome не работает (редкая Linux-конфигурация), окно отображается со штатным chrome. Не блокирует функциональность чата.
- **FM-004 Сжатый layout:** При уменьшении окна до `MinWidth=900`/`MinHeight=600` left memory panel остается видимой; чат не "схлопывается" до нечитаемого размера.
- **FM-005 Аватар-анимация при отсутствии Application.Current:** В юнит-/интеграционных тестах без живого Avalonia App анимации не должны падать (см. существующий fallback в `AvatarViewModel.StartSuccessTimer`).
- **FM-006 Ошибка в чате:** Существующее отображение `ErrorMessage` должно сохраняться (новый стиль, но видимое и читаемое).

## 11. Testing Requirements

### 11.1 Автоматические проверки (must pass)

- `dotnet build .\Iris.slnx` — 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx --no-build` — все существующие 190+ тестов зелёные.
- `dotnet format .\Iris.slnx --verify-no-changes` — EXIT_CODE=0.
- `Iris.Architecture.Tests` — 12/12 зелёные без модификаций.

### 11.2 Новые автоматические тесты

UI-стайлинг трудно покрывать unit-тестами, поэтому достаточен **минимально необходимый** набор:

- **T-UI-001 (smoke unit, опционально):** Проверка, что `MainWindow` инстанцируется в headless Avalonia без исключений (может быть существующим тестом — расширять только если оно уже есть).
- **T-UI-002:** Если введено новое read-only computed-свойство `IsUser` в `ChatMessageViewModelItem`, к нему добавляется маленький unit-тест на корректное определение `User`/`Iris` (Iris.Desktop.Tests, если проект существует; иначе — отказ от теста с пометкой "no test project for Desktop UI").
- Никаких изменений в `Iris.Application.Tests`, `Iris.Domain.Tests`, `Iris.Persistence.Tests`, `Iris.Architecture.Tests`, кроме автоматического прохождения.

### 11.3 Ручной smoke (manual M-UI-01..06)

Должно быть выполнено оператором перед признанием готовности:

- **M-UI-01:** Запуск Desktop. Окно открывается размером 1200×800, без системной рамки, фон обсидиановый. Если ОС поддерживает Mica/Acrylic — наблюдается размытие; иначе — сплошной фон.
- **M-UI-02:** Левая панель Memory занимает 200px, отображает существующие записи памяти, кнопки `Запомнить`/`Забыть` работают.
- **M-UI-03:** Сообщение пользователя отображается справа полупрозрачным блоком; ответ Iris — слева чистым текстом с фиолетовой акцентной линией.
- **M-UI-04:** Во время `IsSending=true` сверху появляется строка "[core] thinking…" Mono шрифтом, приглушенно.
- **M-UI-05:** Аватар в правом нижнем углу не перекрывает кнопку Send. При отправке сообщения вокруг аватара пульсирует фиолетовое кольцо. После ответа — кратковременная синяя вспышка, затем возврат к Idle.
- **M-UI-06:** При закрытии окна нет крэшей. При повторном открытии состояние памяти и история чата (после интеграции — fresh DB) согласованы.

### 11.4 Регрессии

- Нажатие Enter отправляет сообщение, Shift+Enter — переводит строку.
- При отсутствии Ollama сообщение об ошибке отображается читаемо.
- Существующие интеграционные тесты Desktop ViewModel'ов остаются зелеными.

## 12. Documentation and Memory Requirements

После реализации (через `/update-memory`):

- Append в `.agent/PROJECT_LOG.md` — запись о Phase "UI v1 Visual Foundation".
- Update `.agent/overview.md` — current phase / status / next step.
- Возможно: добавить пункт в `.agent/mem_library/02_user_experience.md` §17 ("Visual Direction") о том, что палитра/типографика теперь зафиксированы как "Deep Obsidian v1" (только если решение должно стать стабильным product memory; в противном случае — оставить только в `PROJECT_LOG`).
- При закрытии P2 backlog item "Avatar visually overlaps Send button" — обновить `.agent/debt_tech_backlog.md`.

Спецификация этой работы (этот документ) **не сохраняется автоматически**. Для сохранения требуется явный `/save-spec`.

## 13. Acceptance Criteria

- [ ] AC-V-001: `App.axaml` ссылается на `Themes/IrisTheme.axaml`; `IrisTheme.axaml` содержит все ресурсы из FR-002 и FR-003.
- [ ] AC-V-002: В `Views/ChatView.axaml`, `Views/MemoryView.axaml`, `Views/MainWindow.axaml`, `Controls/Chat/ChatMessageBubble.axaml`, `Controls/Avatar/AvatarPanel.axaml` отсутствуют hex-литералы цветов, кроме случаев, явно разрешенных в `IrisTheme.axaml`.
- [ ] AC-V-003: `MainWindow` запускается с фоном `#0D0D12`, без системной рамки, размером 1200×800.
- [ ] AC-V-004: `TabControl` удалён из `MainWindow.axaml`; layout — Memory (200) | Chat (\*) с аватаром в правом нижнем углу.
- [ ] AC-V-005: Сообщения пользователя визуально отличаются от сообщений Iris в соответствии с FR-009/FR-010.
- [ ] AC-V-006: Thought Log placeholder видим и стилизован Mono-шрифтом во время `IsSending=true` (FR-012).
- [ ] AC-V-007: Аватар имеет кольцо-индикатор, реагирующее на `AvatarState` согласно FR-015.
- [ ] AC-V-008: Аватар не перекрывает кнопку Send при размерах окна ≥ MinWidth/MinHeight.
- [ ] AC-V-009: `dotnet build .\Iris.slnx` — 0 errors, 0 warnings.
- [ ] AC-V-010: `dotnet test .\Iris.slnx` — все тесты, существовавшие до изменений, проходят (190/190 + любые новые из 11.2).
- [ ] AC-V-011: `dotnet format .\Iris.slnx --verify-no-changes` — EXIT_CODE=0.
- [ ] AC-V-012: `Iris.Architecture.Tests` — 12/12 без правок.
- [ ] AC-V-013: Файлы вне `Iris.Desktop` не модифицированы (verifiable через `git diff --stat`).
- [ ] AC-V-014: В `*.axaml.cs` файлах нет новых обращений к `Iris.Application.*`, `Iris.Domain.*`, `Iris.Persistence.*`.
- [ ] AC-V-015: Manual smokes M-UI-01..06 — все Pass.

## 14. Open Questions

No blocking open questions.

(Вопросы, которые не блокируют design, перенесены в §15 Assumptions.)

## 15. Assumptions

- **A-001 JetBrains Mono:** Шрифт JetBrains Mono предполагается через ОС-fallback (на Windows часто отсутствует). Если на этапе `/design` будет решено добавить пакет (`Avalonia.Fonts.JetBrainsMono` или эквивалент), это допустимо ровно одним пакетом и должно быть зафиксировано в design. По умолчанию используется fallback `JetBrains Mono, Cascadia Mono, Consolas, monospace` — UI деградирует на доступный mono без потерь функциональности.
- **A-002 Положение аватара:** В UI v1 аватар фиксированно в правом нижнем углу. Существующий enum `AvatarPosition` и связанные конвертеры остаются в коде (могут пригодиться для будущих режимов), но `MainWindow` их не использует для активного выбора позиции.
- **A-003 IsUser computed-флаг:** Если `ChatMessageViewModelItem` уже содержит достаточно информации (например, `MessageRole` enum или `Author` строка), `IsUser` строится как `=> Role == User` или `=> Author == "User"`. Точное поле определяется на этапе `/design` после чтения `ChatMessageViewModelItem.cs`.
- **A-004 Цвет Idle-кольца:** В концепции есть "статичный синий — холодная концентрация" и "пульсирующий фиолетовый — думает/чувствует". Спецификация фиксирует пульсацию `AccentPrimary` для `Thinking` и оставляет выбор Idle-цвета (приглушенный AccentPrimary или AccentSecondary) дизайну.
- **A-005 DarkTheme.axaml:** Файл-плейсхолдер. В рамках v1 либо наполняется как алиас Iris палитры, либо удаляется. Решение — `/design`.
- **A-006 SettingsView/PermissionsView:** Эти View вне scope; они уже сегодня недоступны через `MainWindow`. UI v1 не возвращает их в навигацию и не удаляет файлы.
- **A-007 Ширина левой панели 200px:** Фиксированная. Resize/collapse вне scope v1.
- **A-008 RU/EN тексты:** Существующие строки ("Чат", "Память", "Запомнить", "Забыть", placeholder'ы, "Thinking…") сохраняются в текущем виде. UI v1 — только визуальная переработка, не ревизия копирайта.

## 16. Blocking Questions

No blocking questions.

***

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

См. §15 — все допущения зафиксированы в спецификации.

## Blocking Questions

No blocking questions.

***

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