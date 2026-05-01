# Implementation Plan: Phase 5.5 — Avatar v1: визуальная реакция Айрис

## 1. Plan Goal

Implement a reactive Desktop-only avatar system in `Iris.Desktop` that visually represents Iris's state (Idle, Thinking, Success, Error, Hidden) by observing `ChatViewModel` through `PropertyChanged` and `CollectionChanged`, with zero changes to Application/Domain/Persistence/ModelGateway layers.

This plan implements the approved specification `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` and design `docs/designs/2026-04-30-phase-5-5-avatar-v1.design.md`.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` (approved, 14 sections)
- **Design:** `docs/designs/2026-04-30-phase-5-5-avatar-v1.design.md` (approved, 17 sections, Option B: Grid overlay)
- **Rules:** `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/dotnet.md`, `.opencode/rules/verification.md`
- **Relevant memory:** `.agent/overview.md`, `.agent/PROJECT_LOG.md`, `.agent/mem_library/13_IRIS_PRODUCT_EVOLUTION_ROADMAP.md`

### Assumptions

1. `ChatViewModel` and `ChatMessageViewModelItem` are stable and will not be modified during this implementation (confirmed by spec FR-011).
2. `CommunityToolkit.Mvvm` 8.4.2 `ObservableObject.SetProperty` is safe to call from `System.Threading.Timer` thread-pool thread callbacks without Avalonia dispatcher affinity.
3. `Iris.Desktop.csproj` already includes `<AvaloniaResource Include="Assets\**" />`, so PNG files placed in `Assets/Avatars/` are automatically embedded.
4. The project has no existing `IValueConverter` implementations; all converters will be new.
5. `AvaloniaUseCompiledBindingsByDefault` is enabled — all bindings use compiled binding mode by default.
6. No `Iris.Architecture.Tests` project exists yet; architecture test T-15 is an integration test using assembly inspection (as defined in the design section 13).

## 3. Scope Control

### In Scope

1. **Data models** — `AvatarState`, `AvatarSize`, `AvatarPosition` enums + `AvatarOptions` record in `Iris.Desktop.Models`.
2. **AvatarViewModel** — Full rewrite inheriting `ViewModelBase`, with reactive state machine, `System.Threading.Timer`, `IDisposable`.
3. **Converters** — `StateEqualityConverter`, `NotHiddenConverter`, `AvatarSizeToPixelConverter`, `AvatarPositionToAlignmentConverter`.
4. **AvatarPanel** — Full rewrite of `AvatarPanel.axaml` and `.axaml.cs` with per-state images, fallback, size/position bindings.
5. **Static assets** — 5 PNG placeholder images in `Assets/Avatars/`.
6. **MainWindow** — Grid overlay compositing `ChatView` + `AvatarPanel`, `MainWindowViewModel.Avatar` property.
7. **DI and configuration** — `AvatarOptions` singleton from `IConfiguration`, `AvatarViewModel` transient, `Desktop:Avatar` section in `appsettings.json`.
8. **Integration tests** — 15 tests in `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs`.
9. **Documentation/memory** — Update `.agent/PROJECT_LOG.md` and `.agent/overview.md`.

### Out of Scope

- Live2D / Spine / skeletal animation.
- Smooth state transition animations (v1 is frame-based replacement).
- Speech bubble, voice integration, persona integration, tool scenarios.
- `IApplicationEventBus` changes (stub remains untouched).
- Runtime config reload.
- Dedicated `Iris.Desktop.Tests` project.
- Any changes to `Iris.Application`, `Iris.Domain`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Shared`, `Iris.Infrastructure`.

### Forbidden Changes

- **Do not modify `ChatViewModel`**, its observable properties, or its send flow.
- **Do not modify `IApplicationEventBus`** or `InMemoryApplicationEventBus`.
- **Do not add** project references to `Iris.Desktop.csproj` (all needed packages already exist).
- **Do not modify** `IIrisApplicationFacade` or `IrisApplicationFacade`.
- **Do not touch** `Iris.Domain.Persona` stubs.
- **Do not use** `Canvas` overlay (Option A rejected) — use Grid overlay (Option B).
- **Do not reference** `IrisDbContext`, `OllamaChatModelClient`, or any adapter directly from `AvatarViewModel` or `AvatarPanel`.
- **Do not bind** `AvatarPanel.DataContext` after it is already set via `MainWindow.axaml` — avoid double DataContext assignment.

## 4. Implementation Strategy

The implementation follows a strict bottom-up dependency order within `Iris.Desktop`, consistent with the approved design component tree:

1. **Data models first** — types with zero dependencies, consumed by all other components.
2. **ViewModel second** — depends only on models + existing `ChatViewModel`/`ViewModelBase`.
3. **Converters + Panel** — depends on models and `AvatarViewModel` type.
4. **MainWindow integration** — depends on `AvatarPanel` and `AvatarViewModel`.
5. **DI + config** — depends on `AvatarOptions`, `AvatarViewModel`, and `MainWindowViewModel` constructor.
6. **Tests** — depend on all types but can be written incrementally; best placed after DI to ensure full compilability.
7. **Final verification** — build + test + format + agent memory update.

Each phase is independently compilable (except Phase 5 depends on Phase 3 for `AvatarPanel` type, and Phase 6 depends on Phase 2 and Phase 5 for DI). No phase requires manual UI testing to validate code correctness.

**Architecture invariants preserved throughout:**
- All new types live in `Iris.Desktop` only.
- `AvatarViewModel -> ChatViewModel` (observer), never reverse.
- Zero changes to `Iris.Application`/`Iris.Domain`/`Iris.Shared`/`Iris.Persistence`/`Iris.ModelGateway`.
- Grid overlay pattern (Option B from design).

## 5. Phase Plan

---

### Phase 0 — Reconnaissance

#### Goal

Confirm all stub files exist, verify `ChatViewModel` observable properties match design assumptions, verify test patterns.

#### Files to Inspect

- `src/Iris.Desktop/ViewModels/AvatarViewModel.cs`
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml`
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml.cs`
- `src/Iris.Desktop/ViewModels/ChatViewModel.cs`
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs`
- `src/Iris.Desktop/ViewModels/ViewModelBase.cs`
- `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs`
- `src/Iris.Desktop/Views/MainWindow.axaml`
- `src/Iris.Desktop/DependencyInjection.cs`
- `src/Iris.Desktop/appsettings.json`
- `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs`

#### Files Likely to Edit

- None.

#### Steps

1. Read all stub files to confirm they match the expected placeholder state from the spec.
2. Verify `ChatViewModel.IsSending` raises `PropertyChanged`.
3. Verify `ChatViewModel.HasError` is computed from `ErrorMessage` and raises `PropertyChanged`.
4. Verify `ChatViewModel.Messages` is `ObservableCollection<ChatMessageViewModelItem>`.
5. Verify `ChatMessageViewModelItem` has `Role`, `IsAssistant` properties.
6. Verify `ViewModelBase` inherits `ObservableObject`.
7. Review `ChatViewModelTests` pattern (inline FakeIrisApplicationFacade, manual setup).

#### Verification

- Read-only inspection.
- No build or test commands needed.

#### Rollback

No code changes.

---

### Phase 1 — Data Models

#### Goal

Create `AvatarState`, `AvatarSize`, `AvatarPosition` enums and `AvatarOptions` record in `Iris.Desktop.Models`.

#### Files to Inspect

- `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs` (namespace pattern: `Iris.Desktop.Models;`)

#### Files Likely to Edit

- `src/Iris.Desktop/Models/AvatarState.cs` (new)
- `src/Iris.Desktop/Models/AvatarSize.cs` (new)
- `src/Iris.Desktop/Models/AvatarPosition.cs` (new)
- `src/Iris.Desktop/Models/AvatarOptions.cs` (new)

#### Files That Must Not Be Touched

- All files outside `src/Iris.Desktop/Models/`.

#### Steps

1. Create `AvatarState.cs` — `public enum AvatarState { Idle, Thinking, Speaking, Success, Error, Hidden }` with XML doc on `Speaking`: "Reserved for Voice v1 integration. Not reachable in Avatar v1."
2. Create `AvatarSize.cs` — `public enum AvatarSize { Small, Medium, Large }`.
3. Create `AvatarPosition.cs` — `public enum AvatarPosition { TopLeft, TopRight, BottomLeft, BottomRight }`.
4. Create `AvatarOptions.cs` — `public sealed record AvatarOptions(bool Enabled, AvatarSize Size, AvatarPosition Position, double SuccessDisplayDurationSeconds)`.

#### Verification

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj
```

#### Rollback

Delete the four new files. No other files affected.

---

### Phase 2 — AvatarViewModel

#### Goal

Rewrite `AvatarViewModel` from an empty `internal class` into a `public sealed partial class` inheriting `ViewModelBase`, implementing the reactive state machine with `ChatViewModel` observation, `System.Threading.Timer` for Success->Idle, and `IDisposable`.

#### Files to Inspect

- `src/Iris.Desktop/ViewModels/ViewModelBase.cs`
- `src/Iris.Desktop/ViewModels/ChatViewModel.cs`
- `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs`

#### Files Likely to Edit

- `src/Iris.Desktop/ViewModels/AvatarViewModel.cs` (complete rewrite)

#### Files That Must Not Be Touched

- `src/Iris.Desktop/ViewModels/ChatViewModel.cs`
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs`
- `src/Iris.Desktop/ViewModels/ViewModelBase.cs`
- All files outside `src/Iris.Desktop/`.

#### Steps

1. **Constructor:** Accept `ChatViewModel chatViewModel` and `AvatarOptions options`. Throw `ArgumentNullException` for null inputs. Store both in readonly fields.
2. **Initial state:** `State = options.Enabled ? AvatarState.Idle : AvatarState.Hidden`. Set `Size` and `Position` from `options`.
3. **Subscribe:** In constructor, subscribe to `chatViewModel.PropertyChanged` and `chatViewModel.Messages.CollectionChanged`.
4. **PropertyChanged handler (`OnChatPropertyChanged`):**
   - Call `ComputeState()` to re-evaluate and set `State`.
   - `ComputeState()` implements the priority-based state machine:
     1. `!_options.Enabled` -> `Hidden` (cancel timer, return).
     2. `chatViewModel.IsSending` -> `Thinking` (cancel timer).
     3. `!chatViewModel.IsSending && chatViewModel.HasError` -> `Error` (cancel timer).
     4. Otherwise -> current state unchanged (Idle or Success during timer).
   - Use `SetProperty(ref _state, value)` for `State`.
5. **CollectionChanged handler (`OnMessagesChanged`):**
   - Filter for `NotifyCollectionChangedAction.Add` and check `e.NewItems`.
   - If any new item has `Role == MessageRole.Assistant`, `!chatViewModel.IsSending`, `!chatViewModel.HasError`:
     - Set `State = AvatarState.Success`.
     - Create `_successTimer = new Timer(OnSuccessTimerElapsed, null, (long)(_options.SuccessDisplayDurationSeconds * 1000), Timeout.Infinite)`.
6. **Timer callback (`OnSuccessTimerElapsed`):**
   - Dispose timer, set `_successTimer = null`.
   - Only set `State = Idle` if current state is still `Success` (defensive check).
7. **Cancel timer helper (`CancelSuccessTimer`):**
   - If `_successTimer != null`, dispose, set null.
8. **Dispose:**
   - Unsubscribe from `chatViewModel.PropertyChanged` and `chatViewModel.Messages.CollectionChanged`.
   - Cancel and dispose timer.
   - Set `_disposed = true`. Check `_disposed` at top of event handlers.
9. **Observable properties:**
   - `AvatarState State` — with `SetProperty`, initialised in constructor.
   - `AvatarSize Size` — get-only from `_options.Size`.
   - `AvatarPosition Position` — get-only from `_options.Position`.

#### Verification

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj
```

#### Rollback

Revert `AvatarViewModel.cs` to its stub state, or `git checkout -- src/Iris.Desktop/ViewModels/AvatarViewModel.cs`.

---

### Phase 3 — Converters and AvatarPanel Control

#### Goal

Create four `IValueConverter` implementations and rewrite `AvatarPanel` from an empty `UserControl` into a fully bound control with per-state image rendering and fallback visual.

#### Files to Inspect

- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml` (current stub)
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml.cs` (current stub)

#### Files Likely to Edit

- `src/Iris.Desktop/Converters/StateEqualityConverter.cs` (new)
- `src/Iris.Desktop/Converters/NotHiddenConverter.cs` (new)
- `src/Iris.Desktop/Converters/AvatarSizeToPixelConverter.cs` (new)
- `src/Iris.Desktop/Converters/AvatarPositionToAlignmentConverter.cs` (new)
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml` (rewrite)
- `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml.cs` (rewrite)

#### Files That Must Not Be Touched

- `src/Iris.Desktop/Views/MainWindow.axaml` (updated in Phase 5)
- `src/Iris.Desktop/ViewModels/AvatarViewModel.cs` (already complete from Phase 2)

#### Steps

**3a. Converters:**

1. **`StateEqualityConverter`** — `IValueConverter`:
   - `Convert`: `(AvatarState)value == (AvatarState)parameter` -> `true`/`false`.
   - `ConvertBack`: `throw new NotSupportedException()`.
2. **`NotHiddenConverter`** — `IValueConverter`:
   - `Convert`: `(AvatarState)value != AvatarState.Hidden`.
   - `ConvertBack`: `throw new NotSupportedException()`.
3. **`AvatarSizeToPixelConverter`** — `IValueConverter`:
   - `Convert`: `AvatarSize.Small -> 80`, `Medium -> 120`, `Large -> 180`.
   - `ConvertBack`: `throw new NotSupportedException()`.
4. **`AvatarPositionToAlignmentConverter`** — `IValueConverter`:
   - `Convert`: parameter `"Horizontal"` -> `TopLeft/BottomLeft -> Left`, `TopRight/BottomRight -> Right`. Parameter `"Vertical"` -> `TopLeft/TopRight -> Top`, `BottomLeft/BottomRight -> Bottom`.
   - `ConvertBack`: `throw new NotSupportedException()`.

**3b. AvatarPanel.axaml.cs:**

1. Change `internal partial class` to `public partial class`.
2. Keep `InitializeComponent()` in constructor.
3. Remove any future business logic from code-behind (binding-only control).

**3c. AvatarPanel.axaml:**

Compose a Grid with:

- `IsVisible` on root Grid bound to `State` via `NotHiddenConverter`.
- `Width`/`Height` bound to `Size` via `SizeConverter`.
- 5 `Image` elements, each with `Source` to `/Assets/Avatars/{state}.png`, `IsVisible` bound to `State` via `StateEqualityConverter` with parameter.
- Fallback `Panel` (Ellipse + TextBlock) visible when image resources are not available.
- `x:DataType="vm:AvatarViewModel"` for compiled bindings.
- Converter resources declared in `UserControl.Resources`.

#### Verification

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj
```

#### Rollback

Delete the four converter files. Revert `AvatarPanel.axaml` and `.axaml.cs` to stub state.

---

### Phase 4 — Static Avatar Assets

#### Goal

Create 5 PNG placeholder images in `Assets/Avatars/` so `AvatarPanel` has actual resources to display.

#### Files to Inspect

- `src/Iris.Desktop/Iris.Desktop.csproj` (confirm `<AvaloniaResource Include="Assets\**" />`)
- `src/Iris.Desktop/Assets/Avatars/` (confirm empty folder)

#### Files Likely to Edit

- `src/Iris.Desktop/Assets/Avatars/idle.png` (new)
- `src/Iris.Desktop/Assets/Avatars/thinking.png` (new)
- `src/Iris.Desktop/Assets/Avatars/speaking.png` (new)
- `src/Iris.Desktop/Assets/Avatars/success.png` (new)
- `src/Iris.Desktop/Assets/Avatars/error.png` (new)

#### Files That Must Not Be Touched

- None outside `Assets/Avatars/`.

#### Steps

1. Create 5 minimal 120x120 PNG files with distinct colours + state name text:
   - `idle.png`: grey circle + "Idle" text.
   - `thinking.png`: yellow/amber circle + "Thinking" text.
   - `speaking.png`: blue circle + "Speaking" text (reserved for Voice v1).
   - `success.png`: green circle + "Success" text.
   - `error.png`: red circle + "Error" text.
2. The PNGs auto-embed via the existing `<AvaloniaResource Include="Assets\**" />` glob — no `.csproj` changes needed.
3. Update `AvatarPanel.axaml` fallback logic: the `FallbackPanel` IsVisible should default to `false` (images take priority when available). If image loading fails at runtime, the image silently collapses and the fallback is displayed.

#### Verification

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj
```

Build confirms PNGs are embedded as Avalonia resources.

#### Rollback

Delete the five PNG files.

---

### Phase 5 — MainWindow Integration

#### Goal

Add `AvatarPanel` to `MainWindow` using Grid overlay compositing, and add `Avatar` property to `MainWindowViewModel`.

#### Files to Inspect

- `src/Iris.Desktop/Views/MainWindow.axaml`
- `src/Iris.Desktop/Views/MainWindow.axaml.cs`
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs`

#### Files Likely to Edit

- `src/Iris.Desktop/Views/MainWindow.axaml` (wrap in Grid, add AvatarPanel)
- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs` (add Avatar property, update constructor)

#### Files That Must Not Be Touched

- `src/Iris.Desktop/ViewModels/ChatViewModel.cs`
- `src/Iris.Desktop/Controls/Chat/*`
- `src/Iris.Desktop/Views/ChatView.axaml`

#### Steps

**5a. MainWindowViewModel.cs:**

1. Add `using Iris.Desktop.ViewModels;` if not already present (should be implicit from file-scoped namespace).
2. Add constructor parameter `AvatarViewModel avatar`.
3. Store as `public AvatarViewModel Avatar { get; }` property.
4. Existing `Chat` property and `Greeting` remain unchanged.
5. Constructor becomes: `public MainWindowViewModel(ChatViewModel chat, AvatarViewModel avatar) { Chat = chat; Avatar = avatar; }`.

**5b. MainWindow.axaml:**

1. Add XML namespace: `xmlns:controls="using:Iris.Desktop.Controls.Avatar"`.
2. Add converter resources: register `AvatarPositionToAlignmentConverter` in `Window.Resources` with two keys for horizontal and vertical alignment.
3. Wrap existing content in a single-cell `Grid`:
   - `ChatView` as first child (full-size).
   - `AvatarPanel` as overlay with `DataContext="{Binding Avatar}"`, `HorizontalAlignment` and `VerticalAlignment` bound to `Position` via converters, `Margin="16"`.

#### Verification

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj
```

Confirm: zero warnings, zero errors. Confirm `ChatView` is still the first child (full-size), `AvatarPanel` overlays.

#### Rollback

Revert `MainWindow.axaml` to single `ChatView`. Revert `MainWindowViewModel.cs` to single-parameter constructor. Build confirms revert.

---

### Phase 6 — DI Registration and Configuration

#### Goal

Add `Desktop:Avatar` configuration section to `appsettings.json`, register `AvatarOptions` singleton and `AvatarViewModel` transient in Desktop DI.

#### Files to Inspect

- `src/Iris.Desktop/DependencyInjection.cs`
- `src/Iris.Desktop/appsettings.json`

#### Files Likely to Edit

- `src/Iris.Desktop/appsettings.json` (add `Desktop:Avatar` section)
- `src/Iris.Desktop/DependencyInjection.cs` (add Avatar registrations)

#### Files That Must Not Be Touched

- Existing config sections (`Application`, `Database`, `ModelGateway`)
- Existing DI registrations (Application, Persistence, ModelGateway, facade, ChatViewModel, MainWindowViewModel)

#### Steps

**6a. appsettings.json:**

Add after `ModelGateway` section:

```json
"Desktop": {
  "Avatar": {
    "Enabled": true,
    "Size": "Medium",
    "Position": "BottomRight",
    "SuccessDisplayDurationSeconds": 2.0
  }
}
```

**6b. DependencyInjection.cs:**

Add after existing `AddIrisDesktop` registrations, before `return services;`:

1. Read config with defaults for all four Avatar keys (`Enabled` -> `true`, `Size` -> `Medium`, `Position` -> `BottomRight`, `SuccessDisplayDurationSeconds` -> `2.0`).
2. Create `AvatarOptions` record from parsed config.
3. Register `AvatarOptions` as singleton.
4. Register `AvatarViewModel` as transient.
5. Add two private static helper methods:
   - `ParseEnumOrDefault<T>(string?, T)` — `Enum.TryParse` with ignoreCase, fallback to default.
   - `ParseDoubleOrDefault(string?, double)` — `double.TryParse` with `> 0` check, fallback to default.
6. `MainWindowViewModel` registration unchanged — DI auto-resolves new `AvatarViewModel` parameter.

#### Verification

```powershell
dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj
```

Confirm: zero warnings, zero errors.

#### Rollback

Remove the `Desktop:Avatar` section from `appsettings.json`. Remove the Avatar registration block and two helper methods from `DependencyInjection.cs`. Build confirms revert.

---

### Phase 7 — Integration Tests

#### Goal

Write 15 integration tests in `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` covering the state machine, timer, config, and architecture rules.

#### Files to Inspect

- `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs` (pattern reference)
- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` (project references)
- `src/Iris.Desktop/ViewModels/AvatarViewModel.cs` (to understand state machine for test assertions)

#### Files Likely to Edit

- `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` (new)

#### Files That Must Not Be Touched

- `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs`
- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj`

#### Steps

Create `AvatarViewModelTests.cs` with 15 test methods, following the `ChatViewModelTests` pattern (xUnit, manual `new AvatarViewModel(...)` construction, reuse `FakeIrisApplicationFacade` pattern). Each test method:

| # | Test Method | Category | Approach |
|---|---|---|---|
| T-01 | `InitialStateIsIdle` | Positive | `new AvatarViewModel(chat, new AvatarOptions(true, ...))` -> `State == Idle` |
| T-02 | `InitialStateIsHiddenWhenDisabled` | Negative | `options.Enabled == false` -> `State == Hidden` |
| T-03 | `StateBecomesThinkingOnSend` | Transition | Send via `ChatViewModel` with `FakeIrisApplicationFacade` -> `State == Thinking` |
| T-04 | `StateBecomesSuccessThenIdle` | Transition + Timer | Add assistant message, short timer (0.1s) -> `Success` then `Idle` |
| T-05 | `StateBecomesErrorOnFailure` | Transition | `ErrorMessage` set -> `HasError == true`, `IsSending == false` -> `Error` |
| T-06 | `ErrorClearsOnNewSend` | Transition | Start as Error -> new send -> `Thinking` |
| T-07 | `SuccessTimerCancelledOnNewSend` | Timer cancel | Success with active timer -> new send -> `Thinking`, timer doesn't fire |
| T-08 | `SuccessTimerCancelledOnDisable` | Timer + Config | Success timer active -> Disabled -> timer cancelled |
| T-09 | `EnabledReadsFromOptions` | Config | `options.Enabled` controls initial `State` |
| T-10 | `SizeReadsFromOptions` | Config | `options.Size` -> `ViewModel.Size` |
| T-11 | `PositionReadsFromOptions` | Config | `options.Position` -> `ViewModel.Position` |
| T-12 | `DefaultsUsedWhenConfigMissing` | Config / Negative | Missing `Desktop:Avatar` section -> defaults via helper methods |
| T-13 | `DefaultsUsedWhenInvalidEnum` | Config / Negative | Invalid enum string -> default via `ParseEnumOrDefault` |
| T-14 | `DisposeUnsubscribesFromChatViewModel` | Lifecycle | Post-dispose events don't change `State` |
| T-15 | `NoProhibitedLayerReferences` | Architecture | Assembly doesn't reference Application/Domain/Persistence/ModelGateway |

**Key test patterns:**

- T-03/T-04/T-05 use actual `ChatViewModel` flow with `FakeIrisApplicationFacade` (with `PendingResult` or `EnqueueSuccess`/`EnqueueFailure`)
- T-04/T-07/T-08 use short `SuccessDisplayDurationSeconds` (0.1s) for fast timer tests
- T-12/T-13 test the parse helper methods (made `internal` or tested through `IConfigurationBuilder`)
- T-15 uses `Assembly.GetReferencedAssemblies()` to check for prohibited assembly names

#### Verification

```powershell
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --filter "FullyQualifiedName~AvatarViewModelTests"
```

Expected: 15 tests, 0 failed.

Then run full suite:

```powershell
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj
```

#### Rollback

Delete `AvatarViewModelTests.cs`.

---

### Phase 8 — Final Verification and Documentation

#### Goal

Run full build + test suite, verify no regressions, update agent memory.

#### Files to Inspect

- `.agent/overview.md`
- `.agent/PROJECT_LOG.md`

#### Files Likely to Edit

- `.agent/PROJECT_LOG.md` (append Phase 5.5 completion entry)
- `.agent/overview.md` (update phase status)

#### Files That Must Not Be Touched

- All source files.
- `AGENTS.md`.
- `.agent/architecture.md`.
- `.agent/mem_library/**`.

#### Steps

1. Full build:
   ```powershell
   dotnet build .\Iris.slnx
   ```
2. Full test suite:
   ```powershell
   dotnet test .\Iris.slnx --no-restore
   ```
3. Format check:
   ```powershell
   dotnet format .\Iris.slnx --verify-no-changes
   ```
4. Verify no changes outside `Iris.Desktop`:
   ```powershell
   git diff --name-only -- src/Iris.Application/ src/Iris.Domain/ src/Iris.Shared/ src/Iris.Persistence/ src/Iris.ModelGateway/ src/Iris.Infrastructure/
   ```
   Expected: empty output.
5. Dependency audit:
   ```powershell
   dotnet list .\src\Iris.Desktop\Iris.Desktop.csproj reference
   ```
   Confirm no new project references.
6. Update `.agent/PROJECT_LOG.md`:
   - Phase 5.5 completed entry with changed files, validation results, and reference to spec/design/plan docs.
7. Update `.agent/overview.md`:
   - Change "Current Phase" to "Phase 5.5 Avatar v1 complete".
   - Change "Next Immediate Step" to "Phase 6 live interactive Desktop smoke with Ollama running and stopped".
   - Update "Known Blockers" if any.

#### Verification

- `dotnet build .\Iris.slnx` — 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx --no-restore` — all tests pass (98 existing + 15 new = 113, 0 failed).
- `dotnet format .\Iris.slnx --verify-no-changes` — no format changes.
- Git diff: zero changes outside `src/Iris.Desktop/` and `tests/Iris.IntegrationTests/Desktop/`.

#### Rollback

Revert memory file changes manually or restore from git. Source files are intact at this point.

## 6. Testing Plan

### Unit Tests

No unit tests in this plan. The `Iris.Desktop` project has no dedicated test project per spec section 3 (Out of Scope). All tests are integration tests under `Iris.IntegrationTests`.

### Integration Tests

| # | Test | Category | Key assertion |
|---|---|---|---|
| T-01 | `InitialStateIsIdle` | Positive | `State == Idle` when `Enabled == true` |
| T-02 | `InitialStateIsHiddenWhenDisabled` | Negative | `State == Hidden` when `Enabled == false` |
| T-03 | `StateBecomesThinkingOnSend` | State transition | `IsSending == true` -> `State == Thinking` |
| T-04 | `StateBecomesSuccessThenIdle` | State transition + timer | Assistant message -> `Success` -> timer -> `Idle` |
| T-05 | `StateBecomesErrorOnFailure` | State transition | `HasError == true`, `IsSending == false` -> `Error` |
| T-06 | `ErrorClearsOnNewSend` | State transition | `Error` -> `IsSending == true` -> `Thinking` |
| T-07 | `SuccessTimerCancelledOnNewSend` | Timer cancellation | Timer active -> `IsSending == true` -> `Thinking`, timer doesn't fire |
| T-08 | `SuccessTimerCancelledOnDisable` | Timer + Config | Timer active -> Disabled -> timer cancelled |
| T-09 | `EnabledReadsFromOptions` | Config binding | `options.Enabled` controls initial `State` |
| T-10 | `SizeReadsFromOptions` | Config binding | `options.Size` -> `ViewModel.Size` |
| T-11 | `PositionReadsFromOptions` | Config binding | `options.Position` -> `ViewModel.Position` |
| T-12 | `DefaultsUsedWhenConfigMissing` | Config / Negative | Missing `Desktop:Avatar` section -> defaults |
| T-13 | `DefaultsUsedWhenInvalidEnum` | Config / Negative | Invalid enum string -> default |
| T-14 | `DisposeUnsubscribesFromChatViewModel` | Lifecycle | Post-dispose events don't change `State` |
| T-15 | `NoProhibitedLayerReferences` | Architecture | No assembly references to Application/Domain/Persistence/ModelGateway |

### Manual Smoke (M-01–M-07)

These seven scenarios from the specification remain deferred to post-implementation human testing. They require a running Ollama instance and interactive Desktop window.

### Regression Tests

All 98 existing tests must continue to pass. No existing test modifications are required or allowed.

## 7. Documentation and Memory Plan

### Documentation Updates

- No new documentation files created. Spec and design are already saved as `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` and `docs/designs/2026-04-30-phase-5-5-avatar-v1.design.md`.

### Agent Memory Updates (Phase 8)

- `.agent/PROJECT_LOG.md` — append Phase 5.5 completion entry with changed files and validation.
- `.agent/overview.md` — update current phase and next step.

Not updated:
- `.agent/architecture.md` — Avatar is Desktop-only; no architecture boundary changes.
- `.agent/mem_library/**` — product roadmap already describes Phase 5.5 scope.
- `.agent/log_notes.md` — only if build/test failures occur.
- `.agent/debt_tech_backlog.md` — only if new technical debt is introduced.

## 8. Verification Commands

| Stage | Command |
|---|---|
| Per-phase build | `dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj` |
| Focused tests (Phase 7) | `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --filter "FullyQualifiedName~AvatarViewModelTests"` |
| Full integration tests (Phase 7) | `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj` |
| Full build (Phase 8) | `dotnet build .\Iris.slnx` |
| Full test suite (Phase 8) | `dotnet test .\Iris.slnx --no-restore` |
| Format check (Phase 8) | `dotnet format .\Iris.slnx --verify-no-changes` |
| Dependency audit (Phase 8) | `dotnet list .\src\Iris.Desktop\Iris.Desktop.csproj reference` |
| Forbidden diff audit (Phase 8) | `git diff --name-only -- src/Iris.Application/ src/Iris.Domain/ src/Iris.Shared/ src/Iris.Persistence/ src/Iris.ModelGateway/ src/Iris.Infrastructure/` |

## 9. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| **Tight Timer coupling in tests** — `System.Threading.Timer` callback is async and may race with test assertions. | Medium | Use short timer durations (0.1s) and `Task.Delay` with generous timeout. Use `ManualResetEvent` or `TaskCompletionSource` if needed for T-04, T-07, T-08. |
| **ChatViewModel.IsSending has private setter** — T-03/T-04/T-05 need to trigger state changes through actual ChatViewModel flow. | Low | Use `FakeIrisApplicationFacade` with `EnqueueSuccess`/`PendingResult`, as in existing `ChatViewModelTests`. |
| **ChatViewModel.HasError computed property** — PropertyChanged fires for `ErrorMessage` which triggers `HasError`. AvatarViewModel sees both. | Low | AvatarViewModel state machine is idempotent — `SetProperty` guards against duplicate `State` changes. |
| **Converter compilation** — Avalonia compiled bindings require correct `x:DataType` and converter parameter types. | Low | Use `x:DataType="vm:AvatarViewModel"` on `AvatarPanel`. Converter `Convert` parameters match binding types. |
| **Fallback Panel** — Images missing at startup could cause binding errors. | Low | `FallbackPanel` is always present in visual tree. Images overlay when source is valid. Avalonia silently handles missing resources for `Image.Source`. |
| **Full test suite regression** — Phase 8 full test run may uncover timing-sensitive tests. | Low-Medium | Run focused Avatar tests first (Phase 7), then full suite sequentially (Phase 8). Re-run if transient file lock occurs. |
| **Format check** — New files may need formatting. | Low | Run `dotnet format` after implementation. If format changes exist, the `--verify-no-changes` flag will fail; format and re-verify. |

## 10. Implementation Handoff Notes

### Critical Constraints

1. **Zero changes outside `Iris.Desktop` and `tests/Iris.IntegrationTests/Desktop/`.** If any other file shows in `git diff`, stop and revert.
2. **`ChatViewModel` must not be modified.** The observer pattern is one-way: `AvatarViewModel -> ChatViewModel`.
3. **No event bus usage.** Do not touch `IApplicationEventBus`/`InMemoryApplicationEventBus`.
4. **Grid overlay, not Canvas.** `MainWindow.axaml` wraps content in `<Grid>` with `ChatView` and `AvatarPanel` in the same cell.
5. **`AvatarOptions` is a POCO record, not an `IOptions<T>` or `IConfiguration`-bound type.** Created in DI, consumed by `AvatarViewModel`.
6. **`AvatarViewModel` must implement `IDisposable`** — unsubscribe from events, stop timer.
7. **Speaking state is in the enum but never activated** — no transition rule leads to `Speaking` in Avatar v1.
8. **`Iris.Desktop` has no implicit usings** — add `using System;`, `using System.Threading;`, etc. as needed.
9. **`CommunityToolkit.Mvvm` `SetProperty`** is used for all observable property updates.

### Risky Areas

1. **Timer callback thread safety:** `System.Threading.Timer` callback runs on thread-pool thread. `ObservableObject.SetProperty` raises `PropertyChanged` which Avalonia binding system observes. Consider adding `Avalonia.Threading.Dispatcher.UIThread.Post` in the timer callback for production safety.
2. **`CollectionChanged` handler:** Must filter for `Action == Add` and check for `ChatMessageViewModelItem.Role == MessageRole.Assistant`. Must access `_chatViewModel` properties inside the handler — if the handler runs during dispose, check `_disposed`.
3. **State machine priority:** When both `IsSending` and `HasError` change simultaneously, the code must apply priority: `IsSending == true` -> `Thinking` (clears Error). The `ComputeState()` method enforces this order.

### Expected Final State

- 5 new model/type files in `src/Iris.Desktop/Models/` and `src/Iris.Desktop/Converters/`.
- `AvatarViewModel.cs` rewritten (internal class -> public sealed partial ViewModelBase).
- `AvatarPanel.axaml` + `.axaml.cs` rewritten (empty UserControl -> full control).
- 5 PNG files in `Assets/Avatars/`.
- `MainWindow.axaml` updated (Grid overlay).
- `MainWindowViewModel.cs` updated (Avatar property).
- `DependencyInjection.cs` updated (AvatarOptions singleton, AvatarViewModel transient, parse helpers).
- `appsettings.json` updated (Desktop:Avatar section).
- `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` created (15 tests).
- `.agent/PROJECT_LOG.md` and `.agent/overview.md` updated.

### File Checklist

| File | Action | Phase |
|---|---|---|
| `src/Iris.Desktop/Models/AvatarState.cs` | Create | 1 |
| `src/Iris.Desktop/Models/AvatarSize.cs` | Create | 1 |
| `src/Iris.Desktop/Models/AvatarPosition.cs` | Create | 1 |
| `src/Iris.Desktop/Models/AvatarOptions.cs` | Create | 1 |
| `src/Iris.Desktop/ViewModels/AvatarViewModel.cs` | Rewrite | 2 |
| `src/Iris.Desktop/Converters/StateEqualityConverter.cs` | Create | 3 |
| `src/Iris.Desktop/Converters/NotHiddenConverter.cs` | Create | 3 |
| `src/Iris.Desktop/Converters/AvatarSizeToPixelConverter.cs` | Create | 3 |
| `src/Iris.Desktop/Converters/AvatarPositionToAlignmentConverter.cs` | Create | 3 |
| `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml` | Rewrite | 3 |
| `src/Iris.Desktop/Controls/Avatar/AvatarPanel.axaml.cs` | Rewrite | 3 |
| `src/Iris.Desktop/Assets/Avatars/idle.png` | Create | 4 |
| `src/Iris.Desktop/Assets/Avatars/thinking.png` | Create | 4 |
| `src/Iris.Desktop/Assets/Avatars/speaking.png` | Create | 4 |
| `src/Iris.Desktop/Assets/Avatars/success.png` | Create | 4 |
| `src/Iris.Desktop/Assets/Avatars/error.png` | Create | 4 |
| `src/Iris.Desktop/Views/MainWindow.axaml` | Edit | 5 |
| `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs` | Edit | 5 |
| `src/Iris.Desktop/DependencyInjection.cs` | Edit | 6 |
| `src/Iris.Desktop/appsettings.json` | Edit | 6 |
| `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` | Create | 7 |
| `.agent/PROJECT_LOG.md` | Edit | 8 |
| `.agent/overview.md` | Edit | 8 |

## 11. Open Questions

No blocking open questions.
