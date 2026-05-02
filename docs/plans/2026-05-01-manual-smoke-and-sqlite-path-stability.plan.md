# Implementation Plan: Phase 5.5 Manual Smoke Closure (M-01–M-07) and Desktop SQLite Path Stability

## 1. Plan Goal

Implement the architecture design in `docs/designs/2026-05-01-manual-smoke-and-sqlite-path-stability.design.md`, following the specification in `docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md`. Outcome:

1. Iris Desktop SQLite database lives at a deterministic `<ApplicationData>/Iris/iris.db` by default, with an optional `appsettings.local.json` override accepting either a full absolute connection string or a bare absolute path. Relative paths fail fast at startup.
2. Manual smoke M-01–M-07 from `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11 has a documented procedure (`docs/manual-smoke/...smoke.md`) and is executed against the post-stabilization build, with results recorded in `.agent/PROJECT_LOG.md`.

No source change in `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure`. No new NuGet packages. No new `csproj` references. The 126 existing tests continue to pass, plus 5–7 new path-resolver tests.

## 2. Inputs and Assumptions

### Inputs

- **Specification:** `docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md`.
- **Design:** `docs/designs/2026-05-01-manual-smoke-and-sqlite-path-stability.design.md`.
- **Reference spec for M-01–M-07 expected outcomes:** `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11.
- **Architecture rules:** `.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`, `.opencode/rules/dotnet.md`, `.opencode/rules/security.md`, `.opencode/rules/verification.md`, `.opencode/rules/memory.md`, `.opencode/rules/workflow.md`.
- **Project memory:** `.agent/overview.md`, `.agent/PROJECT_LOG.md`, `.agent/debt_tech_backlog.md` (lines 138–152 — the SQLite path debt to close), `.agent/log_notes.md`.
- **Source structure verified:**
  - `src/Iris.Desktop/DependencyInjection.cs` (host composition root, `internal static class`, has helpers).
  - `src/Iris.Desktop/App.axaml.cs` (`BuildConfiguration` reads `appsettings.json` + optional `appsettings.local.json`).
  - `src/Iris.Desktop/appsettings.json` (committed; currently has `Database:ConnectionString = "Data Source=iris.db"`).
  - `src/Iris.Desktop/Iris.Desktop.csproj` (`InternalsVisibleTo("Iris.Integration.Tests")`, copies `appsettings.json` and updates `appsettings.local.json` if present).
  - `src/Iris.Persistence/DependencyInjection.cs`, `src/Iris.Persistence/Database/DatabaseOptions.cs`, `src/Iris.Persistence/Database/IrisDatabaseInitializer.cs` — **must not change**.
- **Test projects verified:**
  - `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` already references `Iris.Desktop`. Existing `InternalsVisibleTo` reaches it. New tests go here.
  - `tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj` already references all 13 source projects. Optional new assertion goes here.
- **`.gitignore` line 39:** `src/Iris.Desktop/appsettings.local.json` is already gitignored. Confirmed.
- **Branch / dirty state:** branch `feat/avatar-v1-and-opencode-v2`, only the new spec + design + plan in `docs/` are dirty.

### Assumptions

- Implementer is **`builder`** agent (not `planner`/`reviewer`). `builder` may edit code, run tests, append to `.agent/*` per Iris memory rules.
- Manual smoke M-01–M-07 is executed by the **human operator**, not by `builder`. `builder` produces the smoke document and records that it is awaiting operator execution; the operator closes M-01–M-07 evidence afterward.
- The implementer may rely on `Path.IsPathFullyQualified` (.NET Core 2.1+, present on the project's .NET 10 target) per design Option E.
- `Environment.GetFolderPath(SpecialFolder.ApplicationData, SpecialFolderOption.Create)` is used in production; the test seam uses an `internal` constructor with an explicit root override (design Option C2).
- The `Iris.Architecture.Tests` project references already cover existing dependency-direction assertions; an additional explicit assertion for the new types is **optional** (Phase 4 makes the "add or skip" decision based on inspection).
- Task (1) (path stabilization, code) precedes Task (2) (manual smoke execution). The smoke document is created during code phases; smoke **execution** is a final, operator-driven step.
- No live Ollama is available during `dotnet test`; manual smoke M-01–M-04 require live Ollama and a desktop session and therefore happen outside the automated verification.

## 3. Scope Control

### In Scope

- Two new Desktop-internal types: `DesktopAppDataLocator`, `DesktopDatabasePathResolver`.
- Edit of `src/Iris.Desktop/DependencyInjection.cs` to wire the locator + resolver and to make `Database:ConnectionString` optional.
- Edit of `src/Iris.Desktop/appsettings.json` — remove `Database` section.
- New tracked file: `src/Iris.Desktop/appsettings.local.example.json` (documentation; no `CopyToOutputDirectory`).
- Edit of `src/Iris.Desktop/Iris.Desktop.csproj` to declare `appsettings.local.example.json` as `<None Include …>` with no copy.
- New test file(s) in `tests/Iris.IntegrationTests/Desktop/` covering T-PR-01..06.
- (Conditional, Phase 4) — at most one new assertion in `tests/Iris.Architecture.Tests/` if existing assertions do not already cover the boundary for the new types.
- New tracked file: `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md`.
- After all code phases pass, append a `## 2026-05-01 — Desktop SQLite path stabilization` entry to `.agent/PROJECT_LOG.md` and update `.agent/overview.md` (working status, known blockers).
- After operator runs smoke (separate step), append a `## 2026-05-01 — Phase 5.5 Manual Smoke M-01–M-07` entry to `.agent/PROJECT_LOG.md`, mark debt closed in `.agent/debt_tech_backlog.md`, write to `.agent/log_notes.md` only on anomaly, update `.agent/overview.md`.

### Out of Scope

- Any change to `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure`.
- Any change to `IrisDbContext`, repositories, mappers, EF configurations, `DatabaseOptions`, `IrisDatabaseInitializer`, `AddIrisPersistence`.
- Any change to Avatar v1 source (`AvatarViewModel`, `AvatarPanel`, `AvatarOptions`, `AvatarSize`, `AvatarPosition`, `AvatarState`, converters, assets, `MainWindowViewModel`, `MainWindow.axaml`).
- Any change to `ChatViewModel`, `IrisApplicationFacade`, `DesktopErrorMessageMapper`.
- New NuGet packages.
- New `csproj` project references on production projects.
- Production EF Core migrations.
- Migrating existing developer `iris.db` files (orphaned; see spec DS-002).
- Live config reload, packaging, MSIX, encryption.
- Linux/macOS-specific code paths beyond what the BCL primitives already give us.
- Creating `Iris.Desktop.Tests` or `Iris.Shared.Tests` (separate Medium debt).
- Architecture-review or audit work — those are subsequent gates (E and F).

### Forbidden Changes

- Editing the Phase 5.5 Avatar v1 spec or design (frozen).
- Editing `AGENTS.md`, `.opencode/rules/*`, `.opencode/skills/*`, `.opencode/agents/*`, `.opencode/commands/*`.
- Editing `Iris.slnx`, `Directory.Build.props`, `Directory.Packages.props`.
- Adding `InternalsVisibleTo` to any project beyond what already exists.
- Renaming or moving any existing source file.
- Editing `.github/workflows/*`.
- Running `git push`, `git clean`, `git reset --hard`, or any destructive command.
- Running mutating formatters during `/verify` (per `.opencode/rules/verification.md`).
- Updating memory files outside the explicitly authorized phases (Phase 6 for code-completion entry, Phase 8 for smoke entry).

## 4. Implementation Strategy

The work decomposes into **eight phases**, ordered so that each phase ends with green verification and minimal blast radius if rolled back:

- **Phase 0 — Reconnaissance.** Confirm spec/design/memory/source state. Capture exact starting test count and architecture-test references. No edits.
- **Phase 1 — Add `DesktopAppDataLocator`.** Single new file. Compiles, no behavior change yet (not wired). Build green.
- **Phase 2 — Add `DesktopDatabasePathResolver`.** Single new file. Compiles, no behavior change yet (not wired). Build green.
- **Phase 3 — Wire locator + resolver in `AddIrisDesktop`; remove `Database` from `appsettings.json`; add `appsettings.local.example.json`.** **Behavior change point.** Default launch now writes to `%APPDATA%\Iris\iris.db`. Build green; `dotnet test` green for existing 126; **no new tests yet** so the new behavior is tested only by Phase 4.
- **Phase 4 — Add automated tests for resolver + locator + (optional) architecture assertion.** Tests run under `tests/Iris.IntegrationTests/Desktop/` using the existing `InternalsVisibleTo` seam. New tests pass; existing 126 still pass.
- **Phase 5 — Author the manual smoke procedure document `docs/manual-smoke/...smoke.md`.** Pure documentation. No code or test impact. Build/test untouched.
- **Phase 6 — Code-side memory updates.** Append entry to `.agent/PROJECT_LOG.md` describing path-stabilization implementation, update `.agent/overview.md` (working status, blockers), append `appsettings.local.example.json` mention if relevant. Do **not** close the SQLite path debt yet — manual smoke must confirm first. Do **not** record M-01–M-07 yet.
- **Phase 7 — Manual smoke execution (operator).** Human operator runs M-01–M-07 per the smoke document. `builder` does not execute this phase autonomously.
- **Phase 8 — Smoke memory updates.** After operator confirms smoke results, `builder` (or `/update-memory`) records per-scenario outcomes in `.agent/PROJECT_LOG.md`, marks `Desktop SQLite path uses relative working directory` debt resolved in `.agent/debt_tech_backlog.md`, removes M-01–M-07 from `.agent/overview.md` blockers, appends `.agent/log_notes.md` only on anomaly.

## 5. Phase Plan

### Phase 0 — Reconnaissance

#### Goal

Confirm spec, design, memory, source, and test state are unchanged from the design's assumptions. Capture exact starting test count. No file edits.

#### Files to Inspect

- `docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md`
- `docs/designs/2026-05-01-manual-smoke-and-sqlite-path-stability.design.md`
- `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11 (M-01–M-07 expected outcomes — needed verbatim for Phase 5)
- `src/Iris.Desktop/DependencyInjection.cs`
- `src/Iris.Desktop/App.axaml.cs`
- `src/Iris.Desktop/appsettings.json`
- `src/Iris.Desktop/Iris.Desktop.csproj`
- `src/Iris.Persistence/DependencyInjection.cs` (read-only — confirm contract)
- `src/Iris.Persistence/Database/DatabaseOptions.cs` (read-only — confirm contract)
- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj`
- `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` (use as test-style reference)
- `tests/Iris.Architecture.Tests/*.cs` (confirm what is already asserted)
- `.agent/overview.md`
- `.agent/PROJECT_LOG.md` (last entry)
- `.agent/debt_tech_backlog.md` (lines 138–152 — confirm wording for closure entry in Phase 8)
- `.agent/log_notes.md` (most recent entries — to keep tone consistent in Phase 8)
- `.gitignore` (confirm `appsettings.local.json` line 39 still gitignored)

#### Files Likely to Edit

- None.

#### Steps

1. Run `git status --short --branch`. Confirm only `docs/specs/2026-05-01-...spec.md`, `docs/designs/2026-05-01-...design.md`, and the plan are dirty. If anything else is dirty, **stop** and report.
2. Run `dotnet build .\Iris.slnx`. Record: build status, warnings, errors.
3. Run `dotnet test .\Iris.slnx --no-restore --no-build`. Record: total/passed/failed/skipped count. Expected starting count: **126 passed, 0 failed**.
4. Run `dotnet format .\Iris.slnx --verify-no-changes`. Record: pass/fail.
5. Confirm `Path.IsPathFullyQualified` is the validator chosen by design §14 Option E.
6. Confirm `tests/Iris.IntegrationTests/Desktop/` is the target folder for new tests.
7. Inspect `.agent/PROJECT_LOG.md` last 3 entries and confirm the standard entry shape.

#### Verification

- `git status --short` shows only the spec + design dirty.
- Baseline build: 0 errors, 0 warnings.
- Baseline test count: 126 passed, 0 failed.
- Baseline format: clean.

#### Rollback

No code changes; nothing to roll back.

#### Acceptance Checkpoint

Phase 0 is complete when the baseline is recorded and matches the design's stated assumptions.

---

### Phase 1 — Add `DesktopAppDataLocator`

#### Goal

Introduce the `DesktopAppDataLocator` type per design §6. Single new file. Not wired. Compiles.

#### Files to Inspect

- `src/Iris.Desktop/Services/` (existing host services — confirm no conflicting type name).
- `src/Iris.Desktop/DependencyInjection.cs` (note: helpers are `internal` — same accessibility convention applies).
- `src/Iris.Desktop/Iris.Desktop.csproj` (confirm `InternalsVisibleTo("Iris.Integration.Tests")` is on this project).

#### Files Likely to Edit

- **New file:** `src/Iris.Desktop/Hosting/DesktopAppDataLocator.cs`.

#### Files That Must Not Be Touched

- Anything outside `src/Iris.Desktop/Hosting/` in this phase.
- All of `src/Iris.Application/`, `src/Iris.Domain/`, `src/Iris.Shared/`, `src/Iris.Persistence/`, `src/Iris.ModelGateway/`, `src/Iris.Infrastructure/`, `src/Iris.Api/`, `src/Iris.Worker/`.
- All test projects.
- `.agent/*`, `docs/*` (except inspection).

#### Steps

1. Decide on namespace: `Iris.Desktop.Hosting` (per design §6).
2. Create folder `src/Iris.Desktop/Hosting/` if it does not exist.
3. Create `src/Iris.Desktop/Hosting/DesktopAppDataLocator.cs` containing the `internal sealed class` with:
   - public-internal property `string AppDataDirectory { get; }`;
   - parameterless constructor that calls `Environment.GetFolderPath(SpecialFolder.ApplicationData, SpecialFolderOption.Create)`, validates non-empty, and joins with `"Iris"`;
   - `internal` constructor accepting `string rootOverride` (for tests, design Option C2);
   - `EnsureExists()` method that calls `Directory.CreateDirectory(AppDataDirectory)` and wraps OS exceptions per design §10;
   - argument-null and empty-string guards as per Iris style.
4. Match file-scoped namespace and using-directive style to existing `Iris.Desktop` files.

#### Verification

- `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx --no-restore --no-build` → still 126 passed, 0 failed.
- `dotnet format .\Iris.slnx --verify-no-changes` → clean.
- `git diff --stat` shows exactly one new file under `src/Iris.Desktop/Hosting/`.

#### Rollback

Delete `src/Iris.Desktop/Hosting/DesktopAppDataLocator.cs` and remove the empty folder.

#### Acceptance Checkpoint

Phase 1 is complete when the new file compiles, all three verification commands are green, and `git diff` is bounded to that file.

---

### Phase 2 — Add `DesktopDatabasePathResolver`

#### Goal

Introduce the `DesktopDatabasePathResolver` type per design §6. Single new file. Not wired. Compiles.

#### Files to Inspect

- `src/Iris.Desktop/Hosting/DesktopAppDataLocator.cs` (just created).
- `src/Iris.Desktop/DependencyInjection.cs` (note the `ParseEnumOrDefault` / `ParseDoubleOrDefault` static-helper convention).

#### Files Likely to Edit

- **New file:** `src/Iris.Desktop/Hosting/DesktopDatabasePathResolver.cs`.

#### Files That Must Not Be Touched

- Same forbidden list as Phase 1.

#### Steps

1. Create `src/Iris.Desktop/Hosting/DesktopDatabasePathResolver.cs` containing `internal static class DesktopDatabasePathResolver`.
2. Implement `internal static string Resolve(string? configuredOverride, DesktopAppDataLocator locator)` per the design §6 behavior matrix:
   - null/whitespace → `"Data Source=" + Path.Combine(locator.AppDataDirectory, "iris.db")`;
   - prefix `Data Source=` (case-insensitive, trimmed) → extract the data-source token, validate `Path.IsPathFullyQualified`, return verbatim if rooted, else throw;
   - else → if `Path.IsPathFullyQualified(value)` → return `"Data Source=" + value`, else throw.
3. Throw `InvalidOperationException` with message containing `"Database:ConnectionString"` and the rejected value.
4. Use `ArgumentNullException.ThrowIfNull(locator)`.

#### Verification

- `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx --no-restore --no-build` → still 126 passed, 0 failed.
- `dotnet format .\Iris.slnx --verify-no-changes` → clean.
- `git diff --stat` shows two new files under `src/Iris.Desktop/Hosting/`.

#### Rollback

Delete `src/Iris.Desktop/Hosting/DesktopDatabasePathResolver.cs`. Phase 1's locator remains; build still green.

#### Acceptance Checkpoint

Phase 2 is complete when both new files compile, three verification commands are green, and `git diff` is bounded to `src/Iris.Desktop/Hosting/`.

---

### Phase 3 — Wire locator + resolver in `AddIrisDesktop`; update `appsettings.json`; add example file

#### Goal

Make the resolver actually drive the connection string passed to `AddIrisPersistence`. This is the single behavior-change phase. Default launches now write to `%APPDATA%\Iris\iris.db`.

#### Files to Inspect

- `src/Iris.Desktop/DependencyInjection.cs` (read in full before editing).
- `src/Iris.Desktop/appsettings.json` (read in full before editing).
- `src/Iris.Desktop/Iris.Desktop.csproj` (confirm `<None Update="appsettings.local.json">` already present — yes; new example file goes alongside).
- `src/Iris.Persistence/DependencyInjection.cs` (read-only — confirm the call signature has not drifted).

#### Files Likely to Edit

- `src/Iris.Desktop/DependencyInjection.cs` (modify)
- `src/Iris.Desktop/appsettings.json` (modify — remove `"Database"` section)
- `src/Iris.Desktop/Iris.Desktop.csproj` (modify — add `<None Include="appsettings.local.example.json">` with no `CopyToOutputDirectory`)
- **New file:** `src/Iris.Desktop/appsettings.local.example.json`

#### Files That Must Not Be Touched

- All adapter and Application/Domain/Shared/Infrastructure source.
- All test projects.
- `.agent/*`, `docs/*`.
- `src/Iris.Desktop/App.axaml.cs` (`BuildConfiguration` already correct).
- Any other Desktop file (`Views/`, `ViewModels/`, `Models/`, `Controls/`, `Converters/`, `Services/`, `DesignTime/`, `Themes/`, `Assets/`).

#### Steps

1. **Edit `DependencyInjection.cs`:**
   - Remove the `databaseConnectionString = GetRequiredString(configuration, "Database:ConnectionString", ...)` line.
   - Add (just before the `AddIrisApplication` / `AddIrisPersistence` block): construct `var locator = new DesktopAppDataLocator(); locator.EnsureExists();`.
   - Read optional override: `var configuredDb = configuration.GetValue<string?>("Database:ConnectionString");`.
   - Resolve: `var resolvedDb = DesktopDatabasePathResolver.Resolve(configuredDb, locator);`.
   - Change the `AddIrisPersistence` call's argument to pass `resolvedDb`.
   - Add `using Iris.Desktop.Hosting;` if not implicit via global using.
2. **Edit `appsettings.json`:** remove the `"Database": { "ConnectionString": "Data Source=iris.db" },` block. Keep `Application`, `ModelGateway`, `Desktop:Avatar` sections untouched. Confirm trailing-comma JSON validity.
3. **Create `src/Iris.Desktop/appsettings.local.example.json`:** content per design §11 (sample showing both override forms). Use a `"_comment"` key or clear explanatory text since JSON does not support `//` comments natively.
4. **Edit `Iris.Desktop.csproj`:** inside the existing `<ItemGroup>` that has `<Content Include="appsettings.json">`, add `<None Include="appsettings.local.example.json" />` with no `CopyToOutputDirectory` directive. The `<None Update="appsettings.local.json" CopyToOutputDirectory="PreserveNewest" />` line already present is unchanged.
5. (Sanity) Confirm no other code path reads `Database:ConnectionString`.

#### Verification

- `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx --no-restore --no-build` → still 126 passed, 0 failed. **Important:** if `dotnet test` fails because some unknown test depends on the old default `iris.db` path, **stop** and report.
- `dotnet format .\Iris.slnx --verify-no-changes` → clean.
- Quick code-smoke (optional but recommended): `dotnet run --project .\src\Iris.Desktop\Iris.Desktop.csproj --no-build` — wait 5–10 seconds, confirm a fresh `iris.db` appears in `%APPDATA%\Iris\`.
- `git diff --stat` shows: `DependencyInjection.cs` (modified), `appsettings.json` (modified), `Iris.Desktop.csproj` (modified), `appsettings.local.example.json` (new), `DesktopAppDataLocator.cs` (from P1), `DesktopDatabasePathResolver.cs` (from P2).

#### Rollback

`git restore src/Iris.Desktop/DependencyInjection.cs src/Iris.Desktop/appsettings.json src/Iris.Desktop/Iris.Desktop.csproj` and delete `src/Iris.Desktop/appsettings.local.example.json`.

#### Acceptance Checkpoint

Phase 3 is complete when default Desktop launch creates `%APPDATA%\Iris\iris.db`, all three verification commands pass, and `git diff` is bounded to the files listed.

---

### Phase 4 — Add automated tests for resolver, locator, and (conditional) architecture assertion

#### Goal

Cover the design's testing requirements (T-PR-01 through T-PR-06, with T-PR-07 conditional). Existing 126 + 5–7 new tests all pass.

#### Files to Inspect

- `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` (style reference — xUnit `[Fact]` + `[Theory]` patterns).
- `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs` (additional style reference).
- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` (verify packages — `xunit`, `Microsoft.NET.Test.Sdk` already present).
- `tests/Iris.Architecture.Tests/DependencyDirectionTests.cs`, `ForbiddenNamespaceTests.cs`, `ProjectReferenceTests.cs` (decide on T-PR-07).

#### Files Likely to Edit

- **New file:** `tests/Iris.IntegrationTests/Desktop/DesktopDatabasePathResolverTests.cs`.
- **New file:** `tests/Iris.IntegrationTests/Desktop/DesktopAppDataLocatorTests.cs`.
- **Optional new file:** `tests/Iris.Architecture.Tests/DesktopHostingIsolationTests.cs` — only if Phase 4 inspection concludes that the existing trio does not assert "no non-Desktop project references `Iris.Desktop.Hosting`-scoped behavior." **Default: skip.**

#### Files That Must Not Be Touched

- All production source (Phases 1–3 are frozen).
- `Iris.Desktop.csproj` (no new `InternalsVisibleTo` needed).
- `Iris.Integration.Tests.csproj` (already references `Iris.Desktop`; no edit).

#### Steps

1. **Create `DesktopDatabasePathResolverTests.cs`** with:
   - **T-PR-01** `Resolve_WithoutOverride_ReturnsAbsoluteDataSourceUnderAppDataIris` — use locator with `rootOverride`, assert absolute path with correct suffix.
   - **T-PR-02** `Resolve_WithFullConnectionStringAndAbsoluteDataSource_ReturnsVerbatim` — verbatim equality.
   - **T-PR-03** `Resolve_WithBareAbsolutePath_NormalizesToDataSource` — exact equality with `"Data Source=" + path`.
   - **T-PR-04** `Resolve_WithBareRelativePath_Throws` — `[Theory]` with `"iris.db"`, `"./data/iris.db"`, `"..\\foo.db"`. Assert `InvalidOperationException`; message contains `"Database:ConnectionString"`.
   - **T-PR-05** `Resolve_WithConnectionStringWhoseDataSourceIsRelative_Throws` — `[Theory]` with `"Data Source=iris.db"`, `"Data Source=./foo.db;Cache=Shared"`. Same assertion shape.
2. **Create `DesktopAppDataLocatorTests.cs`** with:
   - **T-PR-06** `EnsureExists_WhenDirectoryMissing_CreatesIt` — `rootOverride` to temp path, create, assert, cleanup.
   - `EnsureExists_IsIdempotent` — call twice, assert no exception.
   - `Constructor_WithoutOverride_ResolvesUnderApplicationData` — assert `Path.IsPathFullyQualified` and ends with `"Iris"`.
3. (Conditional) **Skip** `tests/Iris.Architecture.Tests/DesktopHostingIsolationTests.cs` — existing dependency-direction tests already prevent forbidden projects from referencing `Iris.Desktop`.

#### Verification

- `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- `dotnet test .\Iris.slnx --no-restore --no-build` → 131–133 passed, 0 failed.
- `dotnet format .\Iris.slnx --verify-no-changes` → clean.
- Focused run: `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --filter "FullyQualifiedName~DesktopDatabasePathResolverTests|FullyQualifiedName~DesktopAppDataLocatorTests"` → all new tests pass.

#### Rollback

Delete the new test files. Existing 126 tests still pass.

#### Acceptance Checkpoint

Phase 4 is complete when total test count is 131–133, all green, and the new tests are in `tests/Iris.IntegrationTests/Desktop/`.

---

### Phase 5 — Author manual smoke procedure document

#### Goal

Create the operator-facing smoke procedure document that turns "M-01–M-07" into an unambiguous, executable script with a results-recording template. Pure documentation phase.

#### Files to Inspect

- `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11 (lines ~308–319 — M-01–M-07 table; lift verbatim).
- `docs/` folder structure (confirm `docs/manual-smoke/` does not yet exist).
- `.agent/PROJECT_LOG.md` recent entries (style reference for recording template).

#### Files Likely to Edit

- **New file:** `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md`.

#### Files That Must Not Be Touched

- All source, tests, config.
- Existing spec/design/audit/plan documents.
- `.agent/*`.

#### Steps

1. Create folder `docs/manual-smoke/`.
2. Author the smoke document with:
   - **Header:** title, date, references to spec + design + Phase 5.5 spec §11.
   - **§1 Preconditions** (global): build SHA placeholder, `dotnet build`/`dotnet test` green, Ollama state, cleanup instructions.
   - **§2 Per-scenario sections M-01 through M-07**, each with: preconditions delta, numbered steps, expected outcome (verbatim from spec), observed placeholder, Pass/Fail/Anomaly classification.
   - **§3 Recording instructions:** template for `.agent/PROJECT_LOG.md` entry, template for `.agent/log_notes.md` anomaly entries, instructions for closing debt and updating overview.
   - **§4 Failure-mode notes.**
3. Use existing doc style (no YAML frontmatter).

#### Verification

- File exists at `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md`.
- `git diff --stat` shows only the new doc file.
- `dotnet build` unaffected.

#### Rollback

Delete the new file (and empty `docs/manual-smoke/` folder).

#### Acceptance Checkpoint

Phase 5 is complete when the document exists, contains all seven scenarios with verbatim expected outcomes, and includes a clear recording template.

---

### Phase 6 — Code-side memory updates

#### Goal

Record the path-stabilization implementation in `.agent/PROJECT_LOG.md` and update `.agent/overview.md`. **Do not** close the SQLite path debt yet. **Do not** record M-01–M-07 outcomes yet.

#### Files to Inspect

- `.agent/PROJECT_LOG.md` (last 2–3 entries — match formatting).
- `.agent/overview.md` (note current Phase / Working Status / Known Blockers / Next Step).
- `.agent/log_notes.md` (most recent entries).
- `git log --oneline -10` (capture commit SHAs if commits were made).

#### Files Likely to Edit

- `.agent/PROJECT_LOG.md` (append new dated entry at the **top**).
- `.agent/overview.md` (update Current Working Status, Next Immediate Step; add note that manual smoke remains outstanding).

#### Files That Must Not Be Touched

- `.agent/debt_tech_backlog.md` (close in Phase 8 only).
- `.agent/log_notes.md` (touch in Phase 8 only, and only on anomaly).
- `.agent/architecture.md`, `.agent/first-vertical-slice.md`, `.agent/mem_library/**`, `.agent/README.md` — no relevant change.
- Any source/test/config file.

#### Steps

1. Append to `.agent/PROJECT_LOG.md` (top of file) a new entry:
   - Title: `## 2026-05-01 — Desktop SQLite path stabilization implemented`.
   - `### Changed` — list new types, DI wiring, `appsettings.json` removal, example file, new tests.
   - `### Files` — the modified + new source, test, doc files.
   - `### Validation` — exact `dotnet build`, `dotnet test`, `dotnet format` results from Phase 4.
   - `### Notes` — explicit note: manual smoke M-01–M-07 not yet executed; SQLite path debt not yet closed pending smoke evidence.
   - `### Next` — operator runs M-01–M-07 per smoke document.
2. Edit `.agent/overview.md`:
   - Update **Current Working Status** to "path stabilization implemented and verified; manual smoke pending."
   - Update **Next Immediate Step** to "operator: run M-01–M-07 per smoke document."
   - Keep manual smoke M-01–M-07 in **Known Blockers**.

#### Verification

- `git diff` of `.agent/PROJECT_LOG.md` shows only an appended entry at the top.
- `git diff` of `.agent/overview.md` shows targeted line updates.
- No source/test/config diff.
- `dotnet build .\Iris.slnx` still green.

#### Rollback

`git restore .agent/PROJECT_LOG.md .agent/overview.md`.

#### Acceptance Checkpoint

Phase 6 is complete when the PROJECT_LOG entry is present, overview.md reflects the new working state, and no other memory file was touched. **At this point, code phases 1–6 may be merged independently of operator availability for Phase 7.**

---

### Phase 7 — Manual smoke execution (operator)

#### Goal

Human operator executes M-01 through M-07 per the smoke document and records observations.

#### Files to Inspect

- `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md` (the script).
- `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11.

#### Files Likely to Edit

- None **during** execution. Recording happens in Phase 8.

#### Files That Must Not Be Touched

- `builder` agent must **not** execute this phase autonomously.

#### Steps

1. Operator pulls latest `feat/avatar-v1-and-opencode-v2`, runs `dotnet build .\Iris.slnx` and `dotnet test .\Iris.slnx`.
2. Operator confirms `appsettings.local.json` is absent (or `Database:ConnectionString` is unset). Deletes stale `iris.db` files.
3. Operator runs each scenario in order, recording observations.
4. Operator confirms the database file lives at `<ApplicationData>\Iris\iris.db`.
5. Operator records final results: per-scenario Pass / Fail / Anomaly + free-form notes + build SHA.

#### Verification

- Per-scenario operator-judged Pass/Fail/Anomaly recorded.
- Database file confirmed at the new path.
- Anomalies (if any) described with reproduction steps.

#### Rollback

Phase 7 has no software effect to roll back. If a scenario fails due to a regression in the new code, roll back Phase 3. If the regression is pre-existing, record as anomaly and file a new `/spec`.

#### Acceptance Checkpoint

Phase 7 is complete when all seven scenarios have a recorded outcome and the operator decides whether to proceed to Phase 8.

---

### Phase 8 — Smoke memory updates

#### Goal

Record the smoke evidence in `.agent/*` and close the SQLite path debt entry.

#### Files to Inspect

- `.agent/PROJECT_LOG.md` (Phase 6 entry already exists).
- `.agent/overview.md`.
- `.agent/debt_tech_backlog.md` lines 138–152.
- `.agent/log_notes.md` (only if anomalies were observed).
- The operator's recorded smoke results from Phase 7.

#### Files Likely to Edit

- `.agent/PROJECT_LOG.md` — append smoke entry at the top.
- `.agent/overview.md` — remove M-01–M-07 from Known Blockers; update Next Step to `/audit`.
- `.agent/debt_tech_backlog.md` — append `### Resolution` block to SQLite-path entry.
- (Conditional) `.agent/log_notes.md` — append entries only if anomalies were observed.

#### Files That Must Not Be Touched

- `.agent/architecture.md`, `.agent/first-vertical-slice.md`, `.agent/mem_library/**`, `.agent/README.md`.
- Any source/test/config file.

#### Steps

1. Compose the smoke PROJECT_LOG entry from Phase 7 results.
2. Append to `.agent/PROJECT_LOG.md` at the top.
3. If all seven scenarios passed (or passed with non-blocking anomalies):
   - Edit `.agent/overview.md` Known Blockers to remove M-01–M-07.
   - Edit `.agent/overview.md` Next Immediate Step.
   - Append `### Resolution` block to `.agent/debt_tech_backlog.md` SQLite-path entry.
4. If anomalies were observed:
   - Append per-anomaly entries to `.agent/log_notes.md`.
   - If any anomaly is a P0/P1 regression, **do not** mark the debt resolved.

#### Verification

- `git diff` of `.agent/*` is bounded to the four memory files.
- No source/test/config diff.
- `dotnet build .\Iris.slnx` still green.

#### Rollback

`git restore .agent/PROJECT_LOG.md .agent/overview.md .agent/debt_tech_backlog.md .agent/log_notes.md` (only the files that were actually edited).

#### Acceptance Checkpoint

Phase 8 is complete when smoke evidence is in PROJECT_LOG, overview.md reflects "M-01–M-07 closed", SQLite path debt is marked resolved, and log_notes.md is updated only as needed. The iteration is then ready for `/architecture-review` (Gate E) followed by `/audit` (Gate F).

## 6. Testing Plan

### Unit / Integration Tests

Project `tests/Iris.IntegrationTests/` (integration-level by repository convention):

- `DesktopDatabasePathResolverTests` (Phase 4) — covers T-PR-01..05 (5 facts/theories total).
- `DesktopAppDataLocatorTests` (Phase 4) — covers T-PR-06 plus idempotence and a no-override sanity test (3 facts).
- Total new tests: **5–7**.

### Architecture Tests

- Existing `DependencyDirectionTests`, `ForbiddenNamespaceTests`, `ProjectReferenceTests` continue to pass unchanged.
- Optional `DesktopHostingIsolationTests` — **default skip** (existing assertions already sufficient).

### Regression Tests

- All 126 existing tests pass before and after Phases 1–4.
- The 20 `AvatarViewModelTests` (T-PR-09) must remain green throughout.
- Persistence tests in `tests/Iris.IntegrationTests/Persistence/` are unaffected.

### Negative-Path Tests

- T-PR-04 (bare relative path) — three theory rows.
- T-PR-05 (connection string with relative Data Source) — two theory rows.

### Manual Verification

- Phase 3 quick code-smoke: `dotnet run` once, confirm `iris.db` lands in `<ApplicationData>/Iris/`.
- Phase 7 formal smoke: M-01–M-07 per `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md`.

## 7. Documentation and Memory Plan

### Documentation Updates

- **New** `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md` (Phase 5).
- **No update** to frozen artifacts (`docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md`, `docs/designs/2026-04-30-phase-5-5-avatar-v1.design.md`).
- **No update** to `AGENTS.md`, `.opencode/rules/*`, `.opencode/skills/*`, `.opencode/agents/*`, `.opencode/commands/*`.

### Agent Memory Updates

- **Phase 6** (post-code, pre-smoke):
  - Append entry to `.agent/PROJECT_LOG.md`.
  - Update `.agent/overview.md` Working Status / Next Step (keep M-01–M-07 in blockers).
- **Phase 8** (post-smoke):
  - Append second entry to `.agent/PROJECT_LOG.md` (smoke results).
  - Update `.agent/overview.md` Known Blockers and Next Step.
  - Append `### Resolution` block to `.agent/debt_tech_backlog.md` SQLite-path entry.
  - Append entries to `.agent/log_notes.md` only on anomaly.

## 8. Verification Commands

```bash
# Per Phase 0, after each of Phases 1, 2, 3, 4, and as final iteration check:
dotnet build .\Iris.slnx
dotnet test  .\Iris.slnx --no-restore --no-build
dotnet format .\Iris.slnx --verify-no-changes

# Focused new-test run (Phase 4):
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --filter "FullyQualifiedName~DesktopDatabasePathResolverTests|FullyQualifiedName~DesktopAppDataLocatorTests"

# Scope-confirmation diff (every phase):
git status --short --branch
git diff --stat
```

Forbidden during `/verify` (per `.opencode/rules/verification.md`): mutating formatters, snapshot updates, migrations, source edits.

## 9. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| Existing dev workflow that depended on `iris.db` in CWD silently breaks. | Medium (developer ergonomics). | Smoke document and Phase 6 PROJECT_LOG entry document the new default path. Fail-fast on relative override means misconfiguration produces a clear error. |
| `dotnet test` discovers a hidden test that depended on Desktop's old `Database:ConnectionString` value. | Low (Phase 0 search confirms no other consumer). | Phase 0 explicitly searches for consumers; Phase 3 verifies test count remains 126. |
| `Path.IsPathFullyQualified` semantics on non-Windows CI differ from Windows expectations. | Low (Iris is Windows-first). | Tests use `Path.GetTempPath()`-derived absolute paths which are fully qualified on every platform. |
| `DesktopAppDataLocator` test pollutes real `%APPDATA%\Iris\` on a developer machine. | Low. | Tests use the `internal` ctor with `rootOverride`; production-default constructor is never invoked from tests. |
| Smoke document drifts from spec §11 over time. | Low. | Document includes verbatim copy of M-01–M-07 expected outcomes plus citation to spec. |
| Operator runs M-01–M-07 against a build with uncommitted changes. | Medium. | Smoke document precondition: record build SHA; `git status --short` must be clean. |
| Operator forgets to delete stale `bin/.../iris.db`. | Medium. | Smoke document precondition step is explicit about cleanup. |
| Phase 7 reveals an Avatar timer regression similar to historical P1-001. | Medium. | Per FR-014 / spec §10, operator records anomaly; does **not** authorize a code fix. Separate `/spec` filed. Phases 1–6 still merge. |
| Manual smoke cannot be executed within the iteration window. | Low (per FR-017 the implementation may merge anyway). | Phase 6 leaves M-01–M-07 in Known Blockers. |
| New types accidentally referenced from Application/Domain/Shared in a future PR. | Low. | Existing `DependencyDirectionTests` and `ForbiddenNamespaceTests` would fail before merge. |
| `appsettings.local.example.json` mistakenly copied to output. | Low. | csproj item is `<None Include …>` with no `CopyToOutputDirectory`. |
| Connection-string parsing is too narrow and rejects a legitimate developer override. | Low. | Design §6 explicitly lists supported forms; `.example.json` shows both. |

## 10. Implementation Handoff Notes

For the implementing agent (`builder`):

- **Critical constraints:**
  - No source change in `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure`, `Iris.Api`, `Iris.Worker`. Verify with `git diff --stat src/` after every phase.
  - No new NuGet packages. No new `csproj` references. Verify by inspecting `.csproj` diffs.
  - The `Database:ConnectionString` config key is now **optional**. The existing `GetRequiredString(...)` helper must **not** be used for it.
  - Use `Path.IsPathFullyQualified`, **not** `Path.IsPathRooted`. This is a deliberate design choice (Option E).
  - Locator's test-seam constructor must be `internal`, not `public`. The existing `InternalsVisibleTo("Iris.Integration.Tests")` is the only access path.
- **Risky areas:**
  - Phase 3 is the single behavior-change phase. Any failure of `dotnet test` after Phase 3 must be diagnosed before Phase 4 begins.
  - The connection-string parsing is intentionally narrow. Do not add support for `DataSource` (no space), `Filename`, or other aliases without a fresh design decision.
- **Expected final state (Phases 1–6):**
  - ~5 changed/created files in `src/Iris.Desktop/`.
  - 2 new test files in `tests/Iris.IntegrationTests/Desktop/`.
  - 1 new doc in `docs/manual-smoke/`.
  - 2 memory files appended/edited (`PROJECT_LOG.md`, `overview.md`).
  - Test count: 131–133 passed, 0 failed.
- **Checks that must not be skipped:**
  - `dotnet format --verify-no-changes` after Phase 3 and Phase 4.
  - Verify all 20 `AvatarViewModelTests` still pass after Phase 3.
  - `git status --short` after every phase to confirm scope discipline.
- **Forbidden during implementation:**
  - Editing `AGENTS.md`, `.opencode/`, `Iris.slnx`, `Directory.*.props`.
  - Running `git push`, `git clean`, `git reset --hard`.
  - Updating `.agent/log_notes.md` outside Phase 8 (and only on anomaly).
  - Closing `.agent/debt_tech_backlog.md` SQLite entry before Phase 8.

## 11. Open Questions

No blocking open questions. The four spec-deferred design decisions are already resolved by the design (§14, §17). Phases 1–8 implement those decisions without further discretion.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

- The implementer is the `builder` agent (or human), not `planner`/`reviewer`.
- Manual smoke M-01–M-07 (Phase 7) is operator-driven; `builder` cannot self-certify it.
- `Path.IsPathFullyQualified` is the correct rooted-path predicate for .NET 10.
- Working tree at the start of implementation is clean except for the just-saved spec, design, and plan.
- No NuGet package or `csproj` reference change is needed beyond declaring `appsettings.local.example.json` as `<None Include …>`.
- All 126 existing tests continue to pass after Phase 3.
- Phases 1–6 may merge independently of operator availability; Phase 7 and 8 are gated on the human operator (per spec FR-017).

## Blocking Questions

No blocking questions.

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md` |
| B — Design | ✅ Satisfied | `docs/designs/2026-05-01-manual-smoke-and-sqlite-path-stability.design.md` |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |
