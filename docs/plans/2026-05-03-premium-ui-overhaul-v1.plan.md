## 1. Plan Goal

Реализовать визуальный фундамент `Iris.Desktop` ("Deep Obsidian / Visual Silence") строго в соответствии с утвержденными:
- Спецификацией: `docs/specs/2026-05-03-premium-ui-overhaul-v1.spec.md`
- Дизайном: `docs/designs/2026-05-03-premium-ui-overhaul-v1.design.md`

Цель — внедрить единую палитру, типографику, безрамочное окно с Mica/Acrylic, новый layout (Memory слева | Chat справа | Avatar в правом нижнем углу), визуальное разделение сообщений User vs Iris, заглушку Thought Log и кольцо-индикатор аватара, **без изменений** в Application/Domain/Persistence/ModelGateway/Shared слоях и **без модификаций** существующих ViewModels (за исключением, возможно, удаления свойства `AvatarPosition`-биндинга в `MainWindow.axaml`).

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-03-premium-ui-overhaul-v1.spec.md` (саrд в репозитории).
- **Design:** `docs/designs/2026-05-03-premium-ui-overhaul-v1.design.md` (сохранен в репозитории).
- **Project rules:**
  - `.opencode/rules/iris-architecture.md`
  - `.opencode/rules/no-shortcuts.md`
  - `.opencode/rules/dotnet.md`
  - `.opencode/rules/verification.md`
  - `.opencode/rules/memory.md`
  - `.opencode/rules/workflow.md`
- **Architecture doc:** `.agent/architecture.md`.
- **Memory:** `.agent/mem_library/02_user_experience.md` (§17 Visual Direction), `.agent/mem_library/03_iris_persona.md` (§3 Core Personality).
- **Source baseline:**
  - `src/Iris.Desktop/Iris.Desktop.csproj` (Avalonia 12.0.1 + Avalonia.Fonts.Inter уже подключены).
  - `src/Iris.Desktop/App.axaml` (только `<FluentTheme />`).
  - `src/Iris.Desktop/Themes/IrisTheme.axaml` и `Themes/DarkTheme.axaml` — пустые `ResourceDictionary`.
  - `src/Iris.Desktop/Views/MainWindow.axaml` — `TabControl` с двумя вкладками.
  - `src/Iris.Desktop/Views/ChatView.axaml`, `Views/MemoryView.axaml` — рабочие, но с хардкод-цветами / системными Fluent кистями.
  - `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml` — единый шаблон для User и Iris.
  - `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml` — изображения по `AvatarState`, без кольца.
  - `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs` — уже имеет `bool IsUser` и `bool IsAssistant` (готово для DataTemplate selection).
- **Verification baseline:** Перед началом работы — `dotnet build .\Iris.slnx` 0/0, `dotnet test .\Iris.slnx --no-build` 190/190, `dotnet format .\Iris.slnx --verify-no-changes` EXIT_CODE=0 (зафиксировано в `.agent/overview.md`).

### Assumptions

- **A-001 — Состояние ветки:** HEAD `feat/avatar-v1-and-opencode-v2` остается активной веткой. Working tree сейчас грязный — содержит незакоммиченные изменения Phase 8 Memory v1 (`src/Iris.Application/**`, `src/Iris.Persistence/**`, `src/Iris.Domain/Memories/**`, `tests/**`). **Эти изменения не должны быть затронуты** этим планом ни прямо, ни косвенно. Все правки локализуются строго в файлах визуального слоя `Iris.Desktop` (см. §3 Forbidden Changes). Если Phase 8 Memory v1 будет смержен в master до старта реализации, план остается валидным.
- **A-002 — JetBrains Mono:** Используется системный fallback (`"JetBrains Mono, Cascadia Mono, Consolas, monospace"`), без добавления `.ttf` файлов и без новых NuGet пакетов (см. design §14 Option 2).
- **A-003 — DarkTheme.axaml:** Файл-плейсхолдер. В рамках v1 наполняется как алиас Iris-палитры (для совместимости с FluentTheme variant'ами) или оставляется пустым. Удаления не требуется. Решение принимается на этапе реализации Phase 1.
- **A-004 — `IsUser` свойство:** Существует в `ChatMessageViewModelItem` (подтверждено инспекцией). Никаких добавлений в этот класс не требуется. DataTemplate селектор будет использовать его напрямую.
- **A-005 — `MainWindowViewModel.Settings` / `Permissions`:** Эти ViewModel'и не используются в текущем `MainWindow.axaml` (там только `Chat`, `Avatar`, `Memory`). После плана они тоже не используются. Соответствующие View-файлы остаются в репозитории как зарезервированные, но не интегрированы в `MainWindow`.
- **A-006 — Avalonia `ExtendClientAreaToDecorationsHint`:** Поведение в Avalonia 12.0.1 на Windows 10/11 принято как "работает с грациозной деградацией"; на Linux/macOS — best-effort. Спецификация уже принимает этот риск (FM-002, FM-003).
- **A-007 — `ChatMessageBubble` судьба:** Существующий `Controls/Chat/ChatMessageBubble.axaml` (+ `.axaml.cs`) либо переписывается под "общий контейнер", либо удаляется (если оба DataTemplate определяются inline в `ChatView.axaml`). Решение принимается на этапе Phase 4. Удаление файла, если применимо, безопасно: его единственный потребитель — `ChatView.axaml`.
- **A-008 — `ChatMessageTemplateSelector`:** Если в Avalonia 12 простого `DataTemplate` с условием через `Style.Triggers`/binding достаточно — селектор-класс не создается (упрощенный путь). Если нет — создается C# класс `ChatMessageTemplateSelector : IDataTemplate` в `src/Iris.Desktop/Controls/Chat/`. Финальное решение принимается на Phase 4 после фактической попытки чисто-XAML решения. Это **локальное UI-решение** и не влияет на архитектурные границы.
- **A-009 — Localization:** Все существующие RU/EN строки сохраняются как есть.

### Documentation Discovery

`docs/specs/2026-05-03-premium-ui-overhaul-v1.spec.md` и `docs/designs/2026-05-03-premium-ui-overhaul-v1.design.md` уже сохранены в репозитории. Других артефактов для этой задачи в `docs/` нет.

## 3. Scope Control

### In Scope

Только следующие файлы в `src/Iris.Desktop/`:

- `App.axaml` — регистрация ресурсов / стилей `IrisTheme`.
- `Themes/IrisTheme.axaml` — наполнение палитры, шрифтов, базовых стилей.
- `Themes/DarkTheme.axaml` — либо синхронизация с `IrisTheme`, либо оставление пустым (Phase 1 решение).
- `Views/MainWindow.axaml` — кастомный chrome, новый Grid layout (Memory | Chat + Avatar overlay), отказ от TabControl.
- `Views/MainWindow.axaml.cs` — допустимы только тривиальные изменения, не нарушающие "no logic in code-behind". В идеале — без изменений.
- `Views/ChatView.axaml` — переоформление: ресурсные кисти, Thought Log placeholder, новый input/send styling, выбор шаблона сообщения по `IsUser`.
- `Views/ChatView.axaml.cs` — без изменений (тривиальный existing code-behind).
- `Views/MemoryView.axaml` — переоформление под левую панель, ресурсные кисти, типографика.
- `Views/MemoryView.axaml.cs` — без изменений.
- `Controls/Chat/ChatMessageBubble.axaml` — либо переоформляется как "neutral container" под текущую (User) ветку, либо удаляется и заменяется inline-DataTemplate'ами в `ChatView.axaml`.
- `Controls/Chat/ChatMessageBubble.axaml.cs` — синхронно с XAML (если файл удаляется, удалить и code-behind).
- `Controls/Chat/ChatMessageTemplateSelector.cs` — **новый** файл, **только** если чисто-XAML решение для DataTemplate selection окажется недостаточным (A-008).
- `Controls/Avatar/AvatarPanel.axaml` — добавление Ellipse-кольца + XAML Animations / Style Selectors по `AvatarState`.
- `Controls/Avatar/AvatarPanel.axaml.cs` — без изменений.
- Возможно: `Controls/Memory/MemoryCard.axaml` — синхронизация стиля с новой палитрой, **только если** `MemoryView` использует этот контрол (см. Phase 0 Reconnaissance).

Тестовая работа:

- **Опционально** добавить smoke-тест `MainWindow` инстанцируется в headless Avalonia без падения, **только** если для Desktop UI существует тестовый проект, в котором это легко добавить (см. Phase 0 Reconnaissance). По спецификации §11.2 это явно опционально и допустимо отказаться с пометкой "no test project for Desktop UI".

Memory работа (трейлинг-обязательство, не блокер):

- Append в `.agent/PROJECT_LOG.md` — запись о фазе "UI v1 Visual Foundation".
- Update `.agent/overview.md` — current phase / status / next step.
- Опционально: пункт в `.agent/mem_library/02_user_experience.md` §17 о фиксации палитры "Deep Obsidian v1" как durable product memory.
- При закрытии P2 backlog "Avatar visually overlaps Send button" — отметить в `.agent/debt_tech_backlog.md`.

### Out of Scope

- Все, что отнесено в §3 "Out of Scope" спецификации:
  - Сложные покадровые анимации появления сообщений.
  - Реальный Thought Log из Application слоя.
  - Подсветка используемых memory-фрагментов в реальном времени.
  - Распознавание паттернов (User Fatigue и т.п.).
  - Разные цветовые темы под режимы.
  - Компактный widget-режим.
  - Светлая тема и runtime-переключение тем.
  - Локализация / новые строки.
  - Перенос `SettingsView`/`PermissionsView` в новый layout.
  - Изменение `AvatarPosition` enum или логики (хотя `MainWindow` явно использует Right/Bottom — это override на уровне Window, не изменение enum'а).
- Любая работа над Phase 8 Memory v1 issues (P2 backlog: P2-003, P2-004, P2-006, P2-007, P2-008).
- Manual smoke M-MEM-01..05 — это другая задача.
- Любая работа в `python/`, `tools/`, `.opencode/`.

### Forbidden Changes

- **Запрещены любые правки** в:
  - `src/Iris.Application/**`
  - `src/Iris.Domain/**`
  - `src/Iris.Persistence/**`
  - `src/Iris.ModelGateway/**`
  - `src/Iris.Shared/**`
  - `tests/Iris.Application.Tests/**`
  - `tests/Iris.Domain.Tests/**`
  - `tests/Iris.Architecture.Tests/**` — в т.ч. **запрещено** менять архитектурные тесты под этот план.
  - `tests/Iris.IntegrationTests/**` за пределами возможного нового Desktop UI smoke-теста (§3 In Scope).
- **Запрещено** добавлять новые `ProjectReference` в `Iris.Desktop.csproj`.
- **Запрещено** добавлять новые NuGet пакеты, в т.ч. шрифтовые. (По AC-003 спецификации добавление **одного** пакета JetBrains Mono условно допустимо, но дизайн §14 явно выбирает Fallback — поэтому план фиксирует "никаких новых пакетов".)
- **Запрещено** добавлять новые ViewModel'и или менять публичный API существующих VM (`ChatViewModel`, `MemoryViewModel`, `AvatarViewModel`, `MainWindowViewModel`).
- **Запрещено** добавлять бизнес-логику в `*.axaml.cs` файлы (включая `Iris.Application` calls, `Iris.Domain` calls, обращения к `IrisDbContext`, любые провайдерские типы).
- **Запрещено** трогать `appsettings.json`, `appsettings.local.json`, `Hosting/*`, `DependencyInjection.cs` Desktop'а, `Program.cs`.
- **Запрещено** трогать незакоммиченные Phase 8 Memory v1 файлы (см. dirty state в Inputs). Если случайно затронут — откатить через `git restore <path>` (не `git checkout .`, не `git reset --hard`).
- **Запрещено** менять `Iris.slnx` или `Directory.Packages.props` / `Directory.Build.props`.
- **Запрещено** запускать `git push`, `git clean`, `git reset --hard`, `Remove-Item -Recurse` вне `bin/`/`obj/`.

## 4. Implementation Strategy

Работа разбита на **6 коротких фаз** + Phase 0 (recon) + финальная верификация. Каждая фаза изолирована и заканчивается зеленой сборкой и тестами 190+/190+.

Стратегия:

1. **Сначала Foundation, затем Layout, затем Components.** Сначала наполняем `IrisTheme.axaml` (не ломает ничего, потому что его никто не использует). Затем подключаем ресурсы в `App.axaml`. Затем перекраиваем `MainWindow`. Затем — `ChatView`/`MessageBubble`. Затем — `MemoryView`. Затем — Avatar.
2. **Каждая фаза держит UI работоспособным.** После каждой фазы приложение должно собираться, запускаться и проходить тесты. Это снижает риск длинного broken-window периода.
3. **Минимизируем code-behind.** Все новые визуальные эффекты реализуются через XAML Styles/Animations Avalonia 12 без C# кода (исключение — возможный `ChatMessageTemplateSelector`, если чисто-XAML путь окажется недостаточным).
4. **Никаких изменений во ViewModels.** Все биндинги используют существующие свойства.
5. **Защита dirty state.** Phase 0 фиксирует факт, что Phase 8 Memory v1 файлы не должны быть затронуты. Все последующие фазы перед редактированием обязаны проверять `git status`, чтобы не задеть unrelated файлы.
6. **Trailing memory update.** Memory обновляется только в Phase 7 (после успешной верификации). Не во время реализации.

## 5. Phase Plan

### Phase 0 — Reconnaissance (No Code Changes)

#### Goal

Подтвердить текущее состояние UI слоя, зафиксировать baseline, выявить риски, не указанные в спецификации/дизайне.

#### Files to Inspect

- `src/Iris.Desktop/Iris.Desktop.csproj` — список пакетов / project references.
- `src/Iris.Desktop/App.axaml`, `App.axaml.cs` — точка входа стилей.
- `src/Iris.Desktop/Themes/IrisTheme.axaml`, `Themes/DarkTheme.axaml` — текущее наполнение.
- `src/Iris.Desktop/Views/MainWindow.axaml`, `MainWindow.axaml.cs`.
- `src/Iris.Desktop/Views/ChatView.axaml`, `ChatView.axaml.cs`.
- `src/Iris.Desktop/Views/MemoryView.axaml`, `MemoryView.axaml.cs`.
- `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml`, `ChatMessageBubble.axaml.cs`.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml`, `MemoryCard.axaml.cs` — определить, использует ли его `MemoryView`.
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml`, `AvatarPanel.axaml.cs`.
- `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs` — подтвердить наличие `IsUser`.
- `src/Iris.Desktop/Models/AvatarState.cs` — подтвердить enum значения.
- `src/Iris.Desktop/ViewModels/ChatViewModel.cs`, `MemoryViewModel.cs`, `AvatarViewModel.cs`, `MainWindowViewModel.cs` — подтвердить публичные свойства, на которые будет биндинг.
- `src/Iris.Desktop/Converters/*.cs` — определить, какие конвертеры останутся актуальны.
- `tests/` — определить, есть ли проект `Iris.Desktop.Tests` или эквивалент для опционального smoke (A-008 спецификации). По текущему `.agent/overview.md` тесты Desktop живут в `Iris.IntegrationTests`.

#### Files Likely to Edit

- None (Phase 0 — read-only).

#### Forbidden Edits

- Все.

#### Steps

1. Прочитать перечисленные файлы.
2. Зафиксировать baseline-факты в отчете о фазе:
   - точные имена пакетов и их версии (Avalonia 12.0.1, Avalonia.Fonts.Inter 12.0.1);
   - точные публичные свойства VM, на которые планируется биндинг;
   - использует ли `MemoryView.axaml` контрол `MemoryCard` (если да — он попадает в scope; если нет — `MemoryCard` остается нетронутым).
3. Запустить baseline verification:
   ```powershell
   git status --short --branch
   dotnet build .\Iris.slnx
   dotnet test .\Iris.slnx --no-build
   dotnet format .\Iris.slnx --verify-no-changes
   ```
4. Зафиксировать в отчете: build 0/0, tests N/N (где N ≥ 190), format EXIT_CODE=0. Если baseline красный — **остановиться и эскалировать** до фиксации фазой 8 Memory v1 (это блокер).

#### Expected Outcome

- Все assumptions из §2 подтверждены или явно скорректированы.
- Baseline verification зеленый.
- Список файлов для каждой следующей фазы окончательно зафиксирован.

#### Verification

- `git status` — состояние совпадает с зафиксированным в Inputs (dirty Phase 8 Memory v1 + новые docs/specs+designs+plans).
- Build/test/format baseline — зеленые.

#### Rollback

Не применимо (read-only фаза).

#### Acceptance Checkpoint

- [ ] Все файлы из "Files to Inspect" прочитаны.
- [ ] Baseline build/test/format зеленые.
- [ ] Скорректированы assumptions A-007 (`ChatMessageBubble` судьба) и A-008 (`ChatMessageTemplateSelector`) на основе фактического чтения XAML.
- [ ] Подтверждено наличие `IsUser`/`IsAssistant` в `ChatMessageViewModelItem`.
- [ ] Подтверждено: `MemoryView` использует / не использует `MemoryCard`.

---

### Phase 1 — IrisTheme Resource Dictionary (Foundation)

#### Goal

Наполнить `Themes/IrisTheme.axaml` всеми ресурсами палитры и типографики (FR-001, FR-002, FR-003 из спецификации). Подключить `IrisTheme.axaml` в `App.axaml`.

#### Files to Inspect

- `src/Iris.Desktop/App.axaml` (текущая регистрация стилей).
- `src/Iris.Desktop/Themes/IrisTheme.axaml` (пустой плейсхолдер).
- `src/Iris.Desktop/Themes/DarkTheme.axaml` (пустой плейсхолдер).

#### Files Likely to Edit

- `src/Iris.Desktop/Themes/IrisTheme.axaml` — наполнение.
- `src/Iris.Desktop/App.axaml` — `<Application.Resources>` + `<Application.Styles>` подключение `IrisTheme.axaml`. `<FluentTheme />` остается базой (AC-008).
- Опционально: `src/Iris.Desktop/Themes/DarkTheme.axaml` — содержит alias-ресурсы или остается пустым (A-003).

#### Files That Must Not Be Touched

- Все остальные файлы Iris.Desktop.
- Все файлы вне Iris.Desktop.

#### Steps

1. В `IrisTheme.axaml` объявить `Color` ресурсы:
   - `Iris.Background.Color` = `#FF0D0D12`
   - `Iris.Surface.Color` = `#B31E1E23` (Alpha ≈ 0.7)
   - `Iris.AccentPrimary.Color` = `#FF7C4DFF`
   - `Iris.AccentSecondary.Color` = `#FF00E5FF`
   - `Iris.TextPrimary.Color` = `#FFE0E0E0`
   - `Iris.TextMuted.Color` = `#FFB0BEC5`
   - `Iris.TextLog.Color` = `#66B0BEC5` (Alpha ≈ 0.4)
   - `Iris.Border.Color` = `#FF2A3140` (или эквивалент приглушенный на обсидиане).
   - `Iris.Error.Color` = `#FFF08A8A` (для error-сообщений, чтобы не использовать `#F08A8A` хардкодом).
2. Объявить `SolidColorBrush` ресурсы — `Iris.Background`, `Iris.Surface`, `Iris.AccentPrimary`, `Iris.AccentSecondary`, `Iris.TextPrimary`, `Iris.TextMuted`, `Iris.TextLog`, `Iris.Border`, `Iris.Error` — каждый ссылается на соответствующий `Color`.
3. Объявить `FontFamily` ресурсы:
   - `Iris.FontFamily.Ui` = `Inter` (через ресурс из подключенного `Avalonia.Fonts.Inter`; синтаксис согласно документации Avalonia 12 — `avares://...` либо имя, как принято в проекте).
   - `Iris.FontFamily.Mono` = `JetBrains Mono, Cascadia Mono, Consolas, monospace`.
4. Объявить базовые typography-ресурсы (опционально, для повторного использования в Styles):
   - `Iris.FontSize.Body` = `13`
   - `Iris.FontSize.Caption` = `10`
   - `Iris.FontSize.Mono` = `12`
   - `Iris.FontSize.Title` = `24`.
5. Подключить `IrisTheme.axaml` в `App.axaml`:
   ```xml
   <Application.Resources>
     <ResourceInclude Source="/Themes/IrisTheme.axaml" />
   </Application.Resources>
   <Application.Styles>
     <FluentTheme />
   </Application.Styles>
   ```
   (точный синтаксис подбирается по документации Avalonia 12 — `ResourceInclude` или `MergedDictionaries`).
6. Решение по `DarkTheme.axaml`: оставить пустым (минимально-инвазивный путь). Если в будущей фазе обнаружится конфликт с FluentTheme variants — синхронизировать.

#### Expected Outcome

- Все ресурсы доступны как `{StaticResource Iris.*}` или `{DynamicResource Iris.*}`.
- Сборка зеленая, тесты не сломаны (никто пока не использует новые ресурсы — UI выглядит как и раньше).

#### Verification

```powershell
git status --short
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

- Build: 0 errors, 0 warnings.
- Tests: ≥190/190 passed.
- Format: EXIT_CODE=0.
- `git status` показывает изменения только в `IrisTheme.axaml`, `App.axaml` (и опционально `DarkTheme.axaml`); никаких изменений в других файлах Iris.Desktop, никаких изменений в Phase 8 Memory v1 файлах.

#### Rollback

```powershell
git restore src\Iris.Desktop\App.axaml
git restore src\Iris.Desktop\Themes\IrisTheme.axaml
git restore src\Iris.Desktop\Themes\DarkTheme.axaml
```
(используется `git restore`, не `git checkout .`, не `git reset`).

#### Acceptance Checkpoint

- [ ] Все ресурсы из FR-002 определены.
- [ ] `Iris.FontFamily.Ui` и `Iris.FontFamily.Mono` определены.
- [ ] `App.axaml` подключает `IrisTheme.axaml`.
- [ ] Build/test/format зеленые.
- [ ] Никаких изменений в файлах вне scope.

---

### Phase 2 — MainWindow Custom Chrome and Layout

#### Goal

Переписать `MainWindow.axaml` под безрамочное окно с Mica/Acrylic, отказаться от `TabControl`, ввести Grid layout `200, *` с overlay'ом для аватара (FR-004, FR-005, FR-006, FR-007, FR-008, FR-016).

#### Files to Inspect

- `src/Iris.Desktop/Views/MainWindow.axaml` (текущий TabControl + ChatView/MemoryView).
- `src/Iris.Desktop/Views/MainWindow.axaml.cs` (тривиальный — InitializeComponent).
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs` — подтвердить публичные `Chat`, `Avatar`, `Memory`.
- `src/Iris.Desktop/Converters/AvatarPositionToAlignmentConverter.cs` — будет ли использоваться (по дизайну — нет, аватар фиксированно Right/Bottom).

#### Files Likely to Edit

- `src/Iris.Desktop/Views/MainWindow.axaml`.

#### Files That Must Not Be Touched

- `MainWindow.axaml.cs` — без изменений (биндинги объявляются в XAML).
- `MainWindowViewModel.cs` — без изменений (AC-004 спецификации).
- `AvatarPositionToAlignmentConverter.cs` — без удаления (может использоваться в других местах либо понадобиться будущим режимам).
- `ChatView.axaml`, `MemoryView.axaml`, `AvatarPanel.axaml` — пока не трогаем.

#### Steps

1. На `Window` установить:
   - `Width="1200"`, `Height="800"`, `MinWidth="900"`, `MinHeight="600"`.
   - `ExtendClientAreaToDecorationsHint="True"`.
   - `ExtendClientAreaTitleBarHeightHint="-1"` (или эквивалент Avalonia 12).
   - `TransparencyLevelHint="Mica, AcrylicBlur, Transparent, None"` (массив).
   - `Background="Transparent"` (если используется acrylic) **или** `Background="{StaticResource Iris.Background}"` (если sole color).
   - `Title="Iris"` сохранить.
   - `Icon` сохранить.
2. Корневой `Grid`:
   ```
   <Grid>
     <Grid.ColumnDefinitions>
       <ColumnDefinition Width="200" />
       <ColumnDefinition Width="*" />
     </Grid.ColumnDefinitions>
     <views:MemoryView Grid.Column="0" DataContext="{Binding Memory}" />
     <Grid Grid.Column="1">
       <views:ChatView DataContext="{Binding Chat}" />
       <controls:AvatarPanel DataContext="{Binding Avatar}"
                             HorizontalAlignment="Right"
                             VerticalAlignment="Bottom"
                             Margin="16,16,24,24"
                             IsHitTestVisible="False" />
     </Grid>
   </Grid>
   ```
3. Удалить `<TabControl>` и обертку `Window.Resources` с `AvatarPositionTo*Converter` (если они больше не используются — но **не удалять файлы конвертеров**).
4. Убедиться, что используется `xmlns:views="using:Iris.Desktop.Views"` и `xmlns:controls="using:Iris.Desktop.Controls.Avatar"`.
5. Использовать ресурсы `IrisTheme` (`Iris.Background`) для фона, без хардкод HEX.
6. Обновить корневой обвес для Mica/Acrylic эффекта согласно документации Avalonia 12 (например, поверх `Grid` положить `Border` с `Background={StaticResource Iris.Background}` если acrylic недоступен — fallback из FM-002).

#### Expected Outcome

- При запуске: окно 1200×800, без системной рамки, обсидиановый фон с попыткой Mica/Acrylic (на Windows 11 — видим эффект; на Windows 10 — сплошной фон).
- Memory отображается слева (200px), ChatView — справа, аватар overlay'ится поверх ChatView в правом нижнем углу.
- Никаких вкладок.

#### Verification

```powershell
git status --short
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

- Build/test/format зеленые.
- `git status` показывает изменение только `MainWindow.axaml` (и cumulative — Phase 1 файлы).
- Manual quick check (опционально): запуск `dotnet run --project src/Iris.Desktop` — окно открывается, layout соответствует ожиданиям, нет крэша. Если запустить нельзя — отметить как "deferred to manual smoke".

#### Rollback

```powershell
git restore src\Iris.Desktop\Views\MainWindow.axaml
```

#### Acceptance Checkpoint

- [ ] FR-004..FR-008, FR-016 реализованы в XAML.
- [ ] `TabControl` удален.
- [ ] Аватар явно в Right/Bottom угле.
- [ ] Build/test/format зеленые.
- [ ] `MainWindow.axaml.cs`, `MainWindowViewModel.cs`, ViewModels — нетронуты.

---

### Phase 3 — ChatView Restyle and Thought Log Placeholder

#### Goal

Переоформить `ChatView.axaml` под новую палитру и типографику, добавить Thought Log placeholder, новый input/send styling, без изменения биндингов и логики (FR-001, FR-012, FR-013, FR-014, FR-019).

#### Files to Inspect

- `src/Iris.Desktop/Views/ChatView.axaml` (текущий с хардкод-цветами).
- `src/Iris.Desktop/Views/ChatView.axaml.cs` (минимальный).
- `src/Iris.Desktop/ViewModels/ChatViewModel.cs` — подтвердить `Messages`, `InputText`, `IsSending`, `ErrorMessage`, `HasError`, `CanEditInput`, `SendMessageCommand` остаются как есть.

#### Files Likely to Edit

- `src/Iris.Desktop/Views/ChatView.axaml`.

#### Files That Must Not Be Touched

- `ChatView.axaml.cs`.
- `ChatViewModel.cs`.
- `ChatMessageBubble.axaml` (это отдельная Phase 4).

#### Steps

1. Удалить все хардкод HEX (`#101216`, `#151922`, `#2A3140`, `#3B82F6`, `#F2F4F8`, `#AAB2C0`, `#F08A8A`). Заменить на `{StaticResource Iris.*}`.
2. Корневой `Grid` установить `Background="{StaticResource Iris.Background}"` (или Transparent, если задан фон в `MainWindow`).
3. Заголовок "Iris" / "Local chat" (если оставляем):
   - либо переоформить как минималистичный `TextBlock` со шрифтом `Iris.FontFamily.Ui`, размер 24px, `Iris.TextPrimary`.
   - либо удалить, если новый layout это диктует.
4. Добавить вверху чата (над `ItemsControl`) `Border` или `TextBlock` с **Thought Log placeholder**:
   - `FontFamily="{StaticResource Iris.FontFamily.Mono}"`.
   - `Foreground="{StaticResource Iris.TextLog}"`.
   - `FontSize="{StaticResource Iris.FontSize.Mono}"`.
   - `Text="[core] thinking…"`.
   - `IsVisible="{Binding IsSending}"`.
   - При `IsSending=false` — невидим (FR-012).
5. Удалить старый `TextBlock Text="Thinking..."` (он теперь заменен полноценным Thought Log placeholder).
6. Сохранить структуру: header (опционально) | messages (центр) | error/log/input (низ).
7. `ItemsControl ItemsSource="{Binding Messages}"` — без изменений на этой фазе. ItemTemplate остается с `ChatMessageBubble`. (Phase 4 заменит шаблон.)
8. `TextBox` Input:
   - `Background="Transparent"` (или `Iris.Surface` с прозрачностью).
   - Убрать стандартную рамку через `BorderThickness="0,0,0,1"`, `BorderBrush="{StaticResource Iris.AccentPrimary}"`.
   - `FontFamily="{StaticResource Iris.FontFamily.Ui}"`, `FontSize="{StaticResource Iris.FontSize.Body}"`.
   - `Foreground="{StaticResource Iris.TextPrimary}"`.
   - Поведение Enter/Shift+Enter, плейсхолдер, биндинги — без изменений.
9. `Button Send`:
   - Стилизация: либо акцентный фон `Iris.AccentPrimary` с `Foreground="White"` или `Iris.TextPrimary`, либо текстовая кнопка с `Background="Transparent"` + `Foreground="{StaticResource Iris.AccentPrimary}"`.
   - Hover/Pressed/Disabled — определить через Avalonia Style Selectors.
   - `Command="{Binding SendMessageCommand}"` без изменений.
10. ErrorMessage TextBlock — `Foreground="{StaticResource Iris.Error}"`.

#### Expected Outcome

- ChatView рендерится в обсидиановой палитре.
- Во время `IsSending=true` сверху появляется Mono-строка "[core] thinking…".
- Input визуально — без рамки, с акцентной нижней линией.
- Все биндинги и команды работают как раньше.

#### Verification

```powershell
git status --short
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

- Build/test/format зеленые.
- В файле `ChatView.axaml` нет hex-литералов цветов (`Select-String -Path src\Iris.Desktop\Views\ChatView.axaml -Pattern '#[0-9A-Fa-f]{3,8}'` → пусто).
- Тесты `Iris.IntegrationTests/Desktop/*` (если есть тесты ChatView ViewModel) проходят без изменений.

#### Rollback

```powershell
git restore src\Iris.Desktop\Views\ChatView.axaml
```

#### Acceptance Checkpoint

- [ ] FR-001 в `ChatView.axaml` соблюден (нет хардкод HEX).
- [ ] FR-012 (Thought Log placeholder) реализован и привязан к `IsSending`.
- [ ] FR-013, FR-014 — input/send переоформлены.
- [ ] `ChatView.axaml.cs` и `ChatViewModel.cs` нетронуты.
- [ ] Build/test/format зеленые.

---

### Phase 4 — Chat Message Templates (User vs Iris)

#### Goal

Реализовать визуальное разграничение сообщений: User — полупрозрачный блок справа, Iris — текст с акцентной линией слева (FR-009, FR-010, FR-011). Использовать существующее свойство `ChatMessageViewModelItem.IsUser`.

#### Files to Inspect

- `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs` — подтвердить `IsUser`.
- `src/Iris.Desktop/Views/ChatView.axaml` (после Phase 3).
- `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml` и `.axaml.cs`.

#### Files Likely to Edit

- `src/Iris.Desktop/Views/ChatView.axaml` — `ItemsControl.ItemTemplate` заменяется на DataTemplate selector или на два DataTemplate'а.
- `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml` — либо переписывается под "user-only" вариант + новый IrisMessageBubble, либо удаляется (если решено inline в `ChatView`).
#### Files Likely to Edit (продолжение)

- `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml.cs` — синхронизация с XAML (если файл удаляется — удалить и code-behind).
- **Опционально (только если чисто-XAML недостаточно — A-008):** `src/Iris.Desktop/Controls/Chat/ChatMessageTemplateSelector.cs` — новый файл с `IDataTemplate`.

#### Files That Must Not Be Touched

- `ChatMessageViewModelItem.cs` (свойство `IsUser` уже есть, никаких добавлений).
- `ChatViewModel.cs`.
- Все остальное.

#### Steps

1. Решение по подходу (попробовать в следующем порядке, остановиться на первом рабочем):
   - **Подход A (предпочтительный):** Использовать `Style.Selectors` + `DataTrigger` или `Classes`-binding на `IsUser` внутри одного `DataTemplate`. Если это даёт нужное визуальное разделение — выбрать его.
   - **Подход B:** Inline двух `DataTemplate`'ов в `ChatView.axaml` через `ItemsControl.DataTemplates` или используя `IDataTemplate` через `ItemsControl.ItemTemplate` с условной логикой.
   - **Подход C (если A и B не подходят):** Создать `ChatMessageTemplateSelector : IDataTemplate` в `Controls/Chat/`, объявить два `DataTemplate` свойства (`UserTemplate`, `IrisTemplate`), реализовать `Match` и `Build`. Зарегистрировать в `ChatView.axaml`.
2. Шаблон **User** (`IsUser=true`):
   - `Border` с `Background="{StaticResource Iris.Surface}"` (имеет Alpha 0.7 — полупрозрачный).
   - `CornerRadius="8"`, `Padding="12"`.
   - `HorizontalAlignment="Right"`.
   - `MaxWidth` ≤ 60% — реализовать через привязку к ширине родителя или фиксированно (`MaxWidth="600"` как разумный default; точная привязка — реализуемо через Avalonia binding к `ActualWidth` родителя).
   - Внутри — `TextBlock` с `Text="{Binding Content}"`, `FontFamily="{StaticResource Iris.FontFamily.Ui}"`, `FontSize="{StaticResource Iris.FontSize.Body}"`, `Foreground="{StaticResource Iris.TextPrimary}"`, `TextWrapping="Wrap"`, `LineHeight="1.5"` (если поддерживается).
3. Шаблон **Iris** (`IsUser=false`):
   - **Без `Background`** (текст на фоне сцены).
   - `Border BorderBrush="{StaticResource Iris.AccentPrimary}" BorderThickness="2,0,0,0"` (только левая линия).
   - `Padding="12,4,12,4"` (отступ влево от линии).
   - `HorizontalAlignment="Stretch"` (или `Left`).
   - Внутри — `TextBlock` с теми же типографическими свойствами, что и User-шаблон.
4. Удалить (или заменить) `ChatMessageBubble.axaml`/`.axaml.cs`:
   - Если выбран Подход A/B и шаблоны inline — удалить файлы (они больше не используются).
   - Если выбран Подход C и `ChatMessageBubble` остается как один из под-контролов — переоформить под User-вариант, добавить новый `IrisMessageBubble.axaml` (нежелательно — раздувает файлы; предпочесть inline).
5. Обновить `ItemsControl` в `ChatView.axaml` для использования нового шаблона/селектора.
6. Никаких изменений в `ChatViewModel` или `ChatMessageViewModelItem` (FR-011, AC-004).

#### Expected Outcome

- Сообщения пользователя визуально отделены: справа, полупрозрачный фон, скругленные углы.
- Сообщения Iris: чистый текст с фиолетовой левой линией.
- `IsUser` свойство используется без модификации.
- Биндинги `Messages` работают, новые сообщения корректно отображаются с правильным шаблоном.

#### Verification

```powershell
git status --short
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

- Build/test/format зеленые.
- Если `ChatMessageBubble.axaml` удален — `git status` показывает удаление; не должно быть orphan ссылок (компилятор это обнаружит).
- Существующие тесты `ChatViewModel` (если есть) проходят без изменений.
- Если был создан `ChatMessageTemplateSelector` — он работает (визуальная проверка) и не содержит логики, обращающейся к Application/Domain.

#### Rollback

```powershell
git restore src\Iris.Desktop\Views\ChatView.axaml
git restore src\Iris.Desktop\Controls\Chat\ChatMessageBubble.axaml
git restore src\Iris.Desktop\Controls\Chat\ChatMessageBubble.axaml.cs
```
Если был создан `ChatMessageTemplateSelector.cs`:
```powershell
Remove-Item src\Iris.Desktop\Controls\Chat\ChatMessageTemplateSelector.cs
```

#### Acceptance Checkpoint

- [ ] FR-009 (User: полупрозрачный блок справа) реализован.
- [ ] FR-010 (Iris: левая акцентная линия, без фона) реализован.
- [ ] FR-011 (выбор шаблона по `IsUser`) реализован без изменения публичного API VM.
- [ ] `ChatMessageViewModelItem.cs` нетронут.
- [ ] Code-behind (`*.axaml.cs`) не содержит обращений к `Iris.Application`, `Iris.Domain`, `Iris.Persistence`.
- [ ] Build/test/format зеленые.

---

### Phase 5 — MemoryView Restyle (Left Panel)

#### Goal

Переоформить `MemoryView.axaml` под левую боковую панель: единая палитра, типографика, разделители; **без** изменения биндингов и команд (FR-001, FR-017, FR-018).

#### Files to Inspect

- `src/Iris.Desktop/Views/MemoryView.axaml` (текущий с `DynamicResource SystemControl*Brush`).
- `src/Iris.Desktop/Views/MemoryView.axaml.cs` (минимальный).
- `src/Iris.Desktop/ViewModels/MemoryViewModel.cs` — подтвердить `Memories`, `NewMemoryContent`, `RememberCommand`, `ForgetCommand`, `ErrorMessage`, `IsLoading` остаются как есть.
- `src/Iris.Desktop/Models/MemoryViewModelItem.cs`.
- `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml` — **только** если `MemoryView` его использует (определено в Phase 0).

#### Files Likely to Edit

- `src/Iris.Desktop/Views/MemoryView.axaml`.
- Возможно: `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml` — синхронизация стиля под новую палитру **только если** `MemoryView` его использует.

#### Files That Must Not Be Touched

- `MemoryView.axaml.cs`.
- `MemoryViewModel.cs`.
- `MemoryViewModelItem.cs`.
- `MemoryRepository`, `MemoryDto`, любые Application/Domain/Persistence файлы.

#### Steps

1. Удалить все ссылки на `DynamicResource SystemControl*Brush` (Fluent defaults). Заменить на `{StaticResource Iris.*}`.
2. Корневой контейнер:
   - `Background="Transparent"` (фон даёт `MainWindow`) или `Background="{StaticResource Iris.Background}"`.
   - Если нужно визуальное разделение от Chat-зоны — добавить `Border` справа: `BorderBrush="{StaticResource Iris.Border}" BorderThickness="0,0,1,0"`.
3. Каждый item в `ItemsControl` Memories:
   - `Border` с `BorderBrush="{StaticResource Iris.Border}"`, `BorderThickness="0,0,0,1"` (нижняя разделительная линия), `Padding="8"`, `Margin="0,0,0,4"`.
   - `TextBlock` Content: `FontFamily="{StaticResource Iris.FontFamily.Ui}"`, `FontSize="{StaticResource Iris.FontSize.Body}"`, `Foreground="{StaticResource Iris.TextPrimary}"`, `TextWrapping="Wrap"`.
   - KindLabel / ImportanceLabel / CreatedAt: `FontSize="{StaticResource Iris.FontSize.Caption}"`, `Foreground="{StaticResource Iris.TextMuted}"`.
4. Текстовое поле "Что запомнить?": такие же стили, как input в `ChatView` (Phase 3 — переиспользовать через будущий Style, но в этой фазе допустимо inline).
5. Кнопки `Запомнить` / `Забыть`: согласовать с акцентным/нейтральным стилем, использовать `Iris.AccentPrimary` для активной и `Iris.TextMuted` для нейтральной.
6. Error TextBlock: `Foreground="{StaticResource Iris.Error}"`.
7. **FR-018 — декоративные плейсхолдеры:** Если решено добавить визуальные группы "Active Project" / "Recognized Patterns" — они **должны** быть `IsVisible="False"` и помечены комментарием `<!-- placeholder: requires backend, FR-018 -->`. По дизайну (§14) проще их **не вводить** в v1 — этот вариант предпочтителен.
8. Сохранить все существующие биндинги: `Memories`, `NewMemoryContent`, `RememberCommand`, `ForgetCommand` (включая `RelativeSource`/`$parent` для button-binding в DataTemplate), `ErrorMessage`.

#### Expected Outcome

- `MemoryView` визуально согласован с обсидиановой палитрой.
- Ширина 200px (фиксируется на уровне `MainWindow.axaml` через `ColumnDefinition Width="200"`, см. Phase 2).
- Все функции работают: добавление, удаление, отображение списка, ошибки.

#### Verification

```powershell
git status --short
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

- Build/test/format зеленые.
- Существующие тесты `MemoryViewModel` (включая Phase 8 Memory v1 тесты в dirty state) проходят без изменений.
- В `MemoryView.axaml` нет `SystemControl*Brush` ссылок и hex-литералов.

#### Rollback

```powershell
git restore src\Iris.Desktop\Views\MemoryView.axaml
```
Если правился `MemoryCard.axaml`:
```powershell
git restore src\Iris.Desktop\Controls\Memory\MemoryCard.axaml
```

#### Acceptance Checkpoint

- [ ] FR-001 (нет hex-литералов и Fluent system brushes) — соблюден.
- [ ] FR-017 (стиль панели) реализован.
- [ ] FR-018 — декоративные плейсхолдеры либо отсутствуют, либо невидимы и помечены.
- [ ] `MemoryView.axaml.cs`, `MemoryViewModel.cs`, `MemoryViewModelItem.cs` нетронуты.
- [ ] Build/test/format зеленые.

---

### Phase 6 — Avatar Ring Indicator and State Animations

#### Goal

Добавить кольцо-индикатор вокруг аватара с реакцией на `AvatarState` через XAML Animations / Style Selectors (FR-015).

#### Files to Inspect

- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml`.
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml.cs` (тривиальный).
- `src/Iris.Desktop/ViewModels/AvatarViewModel.cs` — подтвердить `State`, `Size`, `Position`.
- `src/Iris.Desktop/Models/AvatarState.cs` — enum: Idle, Thinking, Speaking, Success, Error, Hidden.
- `src/Iris.Desktop/Converters/StateEqualityConverter.cs`, `AvatarSizeToPixelConverter.cs`, `NotHiddenConverter.cs` — переиспользовать.

#### Files Likely to Edit

- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml`.

#### Files That Must Not Be Touched

- `AvatarPanel.axaml.cs`.
- `AvatarViewModel.cs` (AC-004).
- `AvatarState.cs`.
- Конвертеры.

#### Steps

1. Обернуть текущий `Grid` (с пятью `Image`) в `Panel` или Grid, чтобы поверх изображения можно было разместить `Ellipse`.
2. Добавить `Ellipse` (кольцо):
   - `Stroke` управляется через стилевые селекторы.
   - `StrokeThickness="2"`.
   - `Fill="Transparent"`.
   - Радиус — равен размеру родителя (Width/Height = тот же, что у `Image`, — биндить или fix согласно `Size`).
3. Объявить `Style` селекторы для кольца на основе текущего `State`. Один из работающих подходов:
   - использовать существующий `StateEqualityConverter` для biner'а на `Classes`-property `Ellipse` (через `Classes.thinking="{Binding State, Converter=..., ConverterParameter=ThinkingState}"` и подобные);
   - **либо** определить `<Style.Selectors>` с `:has` / `Classes` логикой согласно документации Avalonia 12.
4. Стили:
   - **Thinking**: `Stroke="{StaticResource Iris.AccentPrimary}"`, `<Style.Animations>` — пульсация `Opacity` от 0.4 до 1.0 и обратно, период 1.5 сек, `IterationCount="Infinite"`.
   - **Idle**: `Stroke="{StaticResource Iris.AccentPrimary}"`, `Opacity="0.3"` (статично).
   - **Success**: `Stroke="{StaticResource Iris.AccentSecondary}"`, короткая вспышка (Animation 1.5–2.5 сек). Точная синхронизация с `AvatarViewModel.SuccessDisplayDurationSeconds` — best-effort через `Duration`. Тонкая синхронизация не требуется в v1.
   - **Error**: `Stroke="{StaticResource Iris.Error}"`, `Opacity="0.6"` (статично).
   - **Hidden**: `Ellipse.IsVisible="False"`.
5. Не трогать существующий `<Panel x:Name="FallbackPanel">` (визуал на случай отсутствия PNG-ассетов) — оставить как есть.
6. Не вводить C#-таймеры для анимации кольца — всё через Avalonia Animation engine.

#### Expected Outcome

- Аватар окружен тонким кольцом.
- При `IsSending=true` (`AvatarState.Thinking`) кольцо пульсирует фиолетовым.
- При `Success` — кратковременная синяя вспышка.
- При `Idle` — приглушенный фиолетовый статичный.
- При `Error` — приглушенный красный.
- При `Hidden` — кольца нет.

#### Verification

```powershell
git status --short
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

- Build/test/format зеленые.
- Существующие тесты `AvatarViewModel` (включая `AvatarViewModelTests.cs`, `HasActiveSuccessTimer`-проверка) проходят без изменений.
- Architecture tests 12/12 — без изменений.

#### Rollback

```powershell
git restore src\Iris.Desktop\Controls\Avatar\AvatarPanel.axaml
```

#### Acceptance Checkpoint

- [ ] FR-015 (кольцо + анимации по `AvatarState`) реализован.
- [ ] Никаких изменений в `AvatarPanel.axaml.cs` и `AvatarViewModel.cs`.
- [ ] Никаких C#-таймеров для анимации кольца.
- [ ] Build/test/format зеленые.

---

### Phase 7 — Final Verification, Manual Smoke, and Memory Update

#### Goal

Завершить эпик: полная решение-уровень верификация, ручной smoke (M-UI-01..06), trailing memory update.

#### Files to Inspect

- `git status` — убедиться, что все изменения только в файлах из In Scope.
- Все файлы, которые редактировались в Phase 1–6.

#### Files Likely to Edit

- `.agent/PROJECT_LOG.md` — append-only.
- `.agent/overview.md` — update current phase / status / next step.
- Опционально: `.agent/mem_library/02_user_experience.md` (§17) — добавить пункт "Deep Obsidian v1 palette" как durable product memory. Решение зависит от того, считается ли визуальная палитра стабильным product meaning.
- Опционально: `.agent/debt_tech_backlog.md` — отметить закрытие "Avatar visually overlaps Send button" (P2 backlog).

#### Files That Must Not Be Touched

- Любые исходники, документация спеков/дизайнов, тесты.

#### Steps

1. Запустить полную верификацию:
   ```powershell
   dotnet build .\Iris.slnx
   dotnet test .\Iris.slnx --no-build
   dotnet format .\Iris.slnx --verify-no-changes
   ```
   Все три должны быть зеленые. Зафиксировать exact output для `/verify` и `/audit`.
2. Запустить targeted проверки на нарушения:
   ```powershell
   Select-String -Path src\Iris.Desktop\Views\*.axaml -Pattern '#[0-9A-Fa-f]{6,8}'
   Select-String -Path src\Iris.Desktop\Controls\**\*.axaml -Pattern '#[0-9A-Fa-f]{6,8}'
   ```
   Ожидаем: только `IrisTheme.axaml` содержит hex-литералы. Все остальные файлы — пусты на эту регулярку (или допустимы только специальные значения вроде `#00000000` для transparent).
3. Запустить targeted проверку на boundary-нарушения в code-behind:
   ```powershell
   Select-String -Path src\Iris.Desktop\**\*.axaml.cs -Pattern 'Iris\.Application|Iris\.Domain|Iris\.Persistence|IrisDbContext|OllamaChatModelClient'
   ```
   Ожидаем: пустой результат (никаких новых boundary violations).
4. **Manual smoke (оператор):**
   - **M-UI-01:** Запустить Desktop. Окно открывается размером 1200×800, без системной рамки, фон обсидиановый. На Windows 11 — Mica/Acrylic эффект; на других ОС — fallback на solid color.
   - **M-UI-02:** Левая панель Memory занимает 200px, отображает существующие записи памяти, кнопки `Запомнить`/`Забыть` работают.
   - **M-UI-03:** Сообщение пользователя — справа, полупрозрачным блоком; ответ Iris — слева, чистым текстом с фиолетовой акцентной линией.
   - **M-UI-04:** Во время `IsSending=true` сверху появляется строка "[core] thinking…" Mono-шрифтом, приглушенно.
   - **M-UI-05:** Аватар в правом нижнем углу не перекрывает кнопку Send. При отправке сообщения кольцо вокруг аватара пульсирует фиолетовым. После ответа — кратковременная синяя вспышка, затем возврат к Idle.
   - **M-UI-06:** При закрытии окна нет крэшей. При повторном открытии состояние памяти и история чата согласованы.
5. Если все smoke-тесты проходят — обновить агент-память:
   - В `.agent/PROJECT_LOG.md` (prepend, не overwrite): новый блок "Phase 9: UI v1 Visual Foundation — Implemented", с суммарным списком изменённых файлов, exact verification commands и результатами, ссылками на spec/design/plan.
   - В `.agent/overview.md`: обновить `Current Phase` → "Phase 9: UI v1 Visual Foundation"; `Current Working Status` → branch + dirty/clean state; `Last Verification` → текущая дата + commands; `Next Immediate Step` → "Run /audit" или "Manual smoke complete, awaiting audit".
   - В `.agent/debt_tech_backlog.md`: отметить закрытие "Avatar visually overlaps Send button" (если был открыт).
   - В `.agent/mem_library/02_user_experience.md` §17 — **только** если решение продуктовое (палитра должна стать стабильной): добавить пункт о Deep Obsidian v1. Если не уверены — оставить только в `PROJECT_LOG`.
6. Финальная проверка `git status` — никаких файлов вне in-scope не модифицировано.

#### Expected Outcome

- `dotnet build`/`test`/`format` — зеленые.
- Все 6 manual smoke checks — Pass.
- Memory обновлена.
- `git diff --stat` показывает изменения только в:
  - `src/Iris.Desktop/App.axaml`
  - `src/Iris.Desktop/Themes/IrisTheme.axaml`
  - `src/Iris.Desktop/Themes/DarkTheme.axaml` (опционально)
  - `src/Iris.Desktop/Views/MainWindow.axaml`
  - `src/Iris.Desktop/Views/ChatView.axaml`
  - `src/Iris.Desktop/Views/MemoryView.axaml`
  - `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml(.cs)` (или удалён)
  - `src/Iris.Desktop/Controls/Chat/ChatMessageTemplateSelector.cs` (опционально, новый)
  - `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml`
  - `src/Iris.Desktop/Controls/Memory/MemoryCard.axaml` (опционально)
  - `.agent/PROJECT_LOG.md`, `.agent/overview.md`, `.agent/debt_tech_backlog.md` (memory update)
  - Возможно: `.agent/mem_library/02_user_experience.md`
- Phase 8 Memory v1 файлы (dirty) **не затронуты**.

#### Verification

См. шаги выше. Финальный отчет должен содержать exact commands и их результаты.

#### Rollback

В этой фазе не требуется code rollback (только memory edits). Если что-то пошло не так в smoke — откат на конкретную предыдущую фазу через `git restore <files>`.

#### Acceptance Checkpoint

- [ ] AC-V-001..AC-V-014 (из спецификации §13) — все Pass.
- [ ] AC-V-015 (manual smokes M-UI-01..06) — все Pass.
- [ ] Memory обновлена согласно §12 спецификации.
- [ ] `git diff --stat` показывает только in-scope файлы.

---

## 6. Testing Plan

### Unit Tests

- **Не добавляются.** Спецификация (§11.2) явно делает это опциональным. UI-стайлинг плохо тестируется через unit. Все существующие unit тесты в `Iris.Application.Tests`, `Iris.Domain.Tests` остаются нетронутыми и должны продолжать проходить.

### Integration Tests

- **Не добавляются.** Существующие тесты в `Iris.IntegrationTests/Desktop/*` (включая `AvatarViewModelTests.cs`, `MemoryViewModelTests.cs` если он есть после Phase 8 dirty merge) должны продолжать проходить без изменений.
- **Опциональный smoke:** Если в `Iris.IntegrationTests` уже есть headless Avalonia setup, можно добавить тривиальный тест `MainWindow_CanInstantiate_WithoutThrowing`. Решение принимается на Phase 0 после инспекции тестового проекта. Если нет инфраструктуры — отказаться без сожаления.

### Architecture Tests

- **Не добавляются и не модифицируются.** `Iris.Architecture.Tests` (12/12) должны продолжать проходить. Если они начнут падать — это сигнал, что был нарушен слой (например, добавлен using в `*.axaml.cs` на запрещённый namespace) — немедленно остановиться, откатить, диагностировать.

### Regression Tests

- Все существующие 190+ тестов продолжают проходить. Это и есть regression check.

### Manual Verification

- M-UI-01..M-UI-06 (см. Phase 7 Step 4 и спецификация §11.3).
- Проводится оператором при наличии живого Desktop запуска и (опционально) Ollama для проверки `IsSending` flow.

---

## 7. Documentation and Memory Plan

### Documentation Updates

- **Не требуется создавать новые документы.** Спецификация и дизайн уже сохранены в `docs/specs/` и `docs/designs/`.
- После реализации `/save-plan` сохранит этот план в `docs/plans/2026-05-03-premium-ui-overhaul-v1.plan.md` (если запросит пользователь).
- После `/audit` сохранится audit-отчет в `docs/audits/`.

### Agent Memory Updates

См. Phase 7 Step 5. Краткий список:

- `.agent/PROJECT_LOG.md` — обязательно (append).
- `.agent/overview.md` — обязательно (update).
- `.agent/debt_tech_backlog.md` — если закрывается "Avatar overlaps Send button".
- `.agent/mem_library/02_user_experience.md` — опционально, по решению.
- `.agent/log_notes.md` — только при появлении новых failures/гочч во время реализации.

---

## 8. Verification Commands

Канонический набор для каждой фазы:

```powershell
git status --short --branch
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-build
dotnet format .\Iris.slnx --verify-no-changes
```

Targeted проверки на нарушения (Phase 7):

```powershell
# Никаких hex-литералов вне IrisTheme.axaml
Select-String -Path src\Iris.Desktop\Views\*.axaml -Pattern '#[0-9A-Fa-f]{6,8}'
Select-String -Path src\Iris.Desktop\Controls\**\*.axaml -Pattern '#[0-9A-Fa-f]{6,8}'

# Никаких boundary-нарушений в code-behind
Select-String -Path src\Iris.Desktop\**\*.axaml.cs -Pattern 'Iris\.Application|Iris\.Domain|Iris\.Persistence|IrisDbContext|OllamaChatModelClient'

# Никаких новых ProjectReference
Select-String -Path src\Iris.Desktop\Iris.Desktop.csproj -Pattern 'ProjectReference|PackageReference'

# Подтверждение, что файлы вне scope не затронуты
git diff --stat src\Iris.Application\
git diff --stat src\Iris.Domain\
git diff --stat src\Iris.Persistence\
git diff --stat src\Iris.ModelGateway\
git diff --stat src\Iris.Shared\
```

Ожидаемые результаты для последних: пусто (или совпадает с Phase 8 Memory v1 dirty baseline, без новых строк).

---

## 9. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| **R-001** Случайно затронуты Phase 8 Memory v1 dirty файлы | Высокий — ломает другую активную работу | Перед каждой фазой выполнять `git status --short`. Если в diff появляется неожиданный файл вне scope — `git restore <file>` (не reset/checkout). |
| **R-002** Mica/Acrylic не работает на целевой ОС | Средний — визуальная деградация, но не функциональная | TransparencyLevelHint массив с fallback `None`. `Iris.Background` solid color гарантирует читаемость. Принимаемый риск (FM-002). |
| **R-003** `ExtendClientAreaToDecorationsHint` ведёт себя странно на Linux/Wayland | Низкий-Средний | Best-effort. Если на Linux окно не открывается — это P1 для отдельного фикса, но не блокер v1. Принимаемый риск (FM-003). |
| **R-004** Удаление `ChatMessageBubble.axaml` ломает что-то невидимое | Низкий | Phase 0 рекогносцировка проверяет всех потребителей. Phase 4 verification ловит compile errors. |
| **R-005** XAML Animation для пульсации кольца не работает корректно из-за тонкостей Avalonia 12 | Средний | Если чисто-XAML анимация даёт сбой, допустимо использовать существующий `StateEqualityConverter` для статичной смены кисти без пульсации. Это деградация FR-015 — должно быть зафиксировано как открытый P2 long-term debt, **но не блокер v1**. Альтернатива — Behaviors/AttachedProperty. |
| **R-006** `IsUser` в `ChatMessageViewModelItem` оказывается недостаточным для DataTemplate selection в Avalonia 12 | Низкий | Phase 4 предусматривает три подхода (A/B/C). Фолбек на `ChatMessageTemplateSelector : IDataTemplate`. |
| **R-007** Ширина 200px для Memory недостаточна на длинных контентах | Низкий | Спецификация фиксирует 200px (A-007). TextWrapping="Wrap" гарантирует читаемость. Resize/collapse — out of scope v1. |
| **R-008** JetBrains Mono недоступен в системе пользователя | Низкий | Fallback на Cascadia Mono / Consolas / monospace. Универсальная читаемость. |
| **R-009** Тестовый baseline (190/190) изменится из-за параллельных правок другого агента | Низкий | Phase 0 фиксирует baseline. Если в midflight количество тестов меняется — переподтвердить и продолжить. |
| **R-010** `dotnet format` находит несоответствия в новом XAML | Низкий | Перед claim "ready" запустить `dotnet format`. Если форматтер вносит изменения — принять их и переверифицировать. |

---

## 10. Implementation Handoff Notes

**Критические ограничения:**

- **Только UI слой.** Любая попытка модифицировать `Iris.Application/**`, `Iris.Domain/**`, `Iris.Persistence/**`, `Iris.ModelGateway/**`, `Iris.Shared/**` — это сигнал отмены. Тоже самое — для тестовых проектов кроме (опционально) Iris.IntegrationTests Desktop UI smoke.
- **Никаких новых ViewModels.** Никаких изменений публичного API существующих VM.
- **Никаких новых пакетов и project references.**
- **Никаких code-behind вызовов на Application/Domain/Persistence.**

**Рискованные зоны:**

- **Custom chrome (Phase 2).** Avalonia 12 API отличается от 11. Сверяться с актуальной документацией. На Linux могут быть глюки.
- **DataTemplate selection (Phase 4).** Перед написанием `ChatMessageTemplateSelector` попробовать чисто-XAML подход. Это сэкономит C# код.
- **Avatar Animations (Phase 6).** XAML Animation в Avalonia 12 — стабильна, но period/easing требуют точной настройки. Если пульсация выглядит "дёргано" — корректировать easing/duration.

**Ожидаемое финальное состояние:**

- 10–12 файлов в `Iris.Desktop` изменены (см. Phase 7 Expected Outcome).
- 0 файлов вне `Iris.Desktop` изменены этим планом.
- Build 0/0, tests 190+/190+, format EXIT_CODE=0.
- 6/6 manual smokes Pass.
- Memory обновлена.

**Чеки, которые нельзя пропускать:**

- `git status --short` после каждой фазы.
- `dotnet build`/`test`/`format` после каждой фазы.
- `Iris.Architecture.Tests` зеленые после **каждой** фазы (не только в финале).
- Targeted boundary searches в Phase 7.
- Manual smoke перед claim'ом "ready".

**Защита dirty state:**

- НЕ запускать `git reset --hard`, `git clean -fd`, `git checkout .`. 
- Для отката отдельных файлов — `git restore <path>`.
- Если случайно изменён файл вне scope — `git restore <path>` и переверифицировать.

---

## 11. Open Questions

No blocking open questions.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

См. §2 — все 9 допущений (A-001..A-009) зафиксированы в плане.

## Blocking Questions

No blocking questions.

***

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `docs/specs/2026-05-03-premium-ui-overhaul-v1.spec.md` |
| B — Design | ✅ Satisfied | `docs/designs/2026-05-03-premium-ui-overhaul-v1.design.md` |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |