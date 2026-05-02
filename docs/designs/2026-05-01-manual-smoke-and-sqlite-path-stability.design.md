# Architecture Design: Phase 5.5 Manual Smoke Closure (M-01–M-07) and Desktop SQLite Path Stability

## 1. Design Goal

Stabilize the Iris Desktop SQLite database location at a deterministic per-user path resolved entirely inside the Desktop host, with a documented `appsettings.local.json` override mechanism, and define an executable manual-smoke procedure for M-01–M-07 that runs against that stabilized Desktop. Do all of this without changing any Application/Domain/Shared/Persistence/ModelGateway/Infrastructure source, without adding NuGet packages, and without introducing new abstractions outside the Desktop host.

## 2. Specification Traceability

Source spec: `docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md`.

| Spec item | Design coverage |
|---|---|
| FR-001 (default stable per-user path) | §6 `DesktopAppDataLocator` + §6 `DesktopDatabasePathResolver` |
| FR-002 (`Environment.SpecialFolder.ApplicationData` + `Iris/iris.db`) | §6 `DesktopAppDataLocator` |
| FR-003 (create directory, fail-fast on creation error) | §6 `DesktopAppDataLocator.EnsureExists`, §10 |
| FR-004 (override via existing config chain) | §11 (no change to `BuildConfiguration`) |
| FR-005 (override accepts full conn string OR absolute path) | §6 `DesktopDatabasePathResolver.Resolve`, §7 contract |
| FR-006 (override lives in `appsettings.local.json` by convention) | §11 + §17 (sample) |
| FR-007 (committed `appsettings.json` must not produce relative CWD db) | §11 (key removed from committed file) |
| FR-008 (relative override → fail fast) | §6 `DesktopDatabasePathResolver`, §10 |
| FR-009 (resolved string passed to existing `AddIrisPersistence`) | §11 wiring |
| FR-010 (Desktop-host-owned, not exposed to Application/Domain) | §5 ownership table; AC-001/AC-002/AC-007 mapping |
| FR-011 – FR-017 (manual smoke procedure & recording) | §6 "Smoke procedure document" + §13 manual section |
| AC-001 / AC-007 / AC-009 (boundary, references, no-shortcuts) | §3, §5, §14, §16 |
| AC-002 (`DatabaseOptions` contract unchanged) | §7, §11 |
| AC-003 (provider not used by Avatar/UI/ViewModels) | §5 |
| AC-004 (no new packages) | §11 (BCL only) |
| AC-005 (no I/O beyond directory create) | §6 component constraints |
| AC-008 (Api/Worker not affected) | §5, §15 |

The four **Open Questions** the spec deliberately deferred to design are resolved in §14 (Options Considered) and §17.

## 3. Current Architecture Context

Inspected via Read/Grep/dotnet:

- `dotnet list .\src\Iris.Desktop\Iris.Desktop.csproj reference` → references `Iris.Application`, `Iris.Domain`, `Iris.ModelGateway`, `Iris.Persistence`, `Iris.Shared`. **No change required.**
- `Iris.Architecture.Tests` enforces:
  - `Domain_depends_only_on_Shared` (assembly references);
  - `Application_depends_only_on_Domain_and_Shared`;
  - `Application_does_not_reference_Persistence` / `ModelGateway`;
  - `Desktop_does_not_reference_Api_or_Worker` (and reverse).
  These assertions are unaffected by this design.
- `Iris.Desktop` already owns host-only concerns: `App.axaml.cs` (`BuildConfiguration` with `appsettings.json` + optional `appsettings.local.json` from `AppContext.BaseDirectory`), `DependencyInjection.cs` (`internal static class` with helpers `ParseEnumOrDefault`/`ParseDoubleOrDefault`), `Services/*` (`IIrisApplicationFacade`, dispatcher, dialog, error mapper). The new resolver belongs alongside these host-only services.
- `Iris.Persistence` exposes a single contract for the path: `DatabaseOptions.ConnectionString` (string). `AddIrisPersistence(Action<DatabaseOptions>)` validates non-empty and feeds `UseSqlite(options.ConnectionString)`. **Persistence does no path resolution and must remain that way.**
- `Iris.Desktop.csproj` has `InternalsVisibleTo("Iris.Integration.Tests")`. `tests/Iris.IntegrationTests/Desktop/AvatarViewModelTests.cs` already exercises Desktop-internal types via this attribute. This is the test seam for the new resolver.
- `appsettings.json` currently contains `"Database": { "ConnectionString": "Data Source=iris.db" }`. No `appsettings.local.json` is committed; csproj has `None Update="appsettings.local.json" CopyToOutputDirectory="PreserveNewest"` (a no-op when the file is absent).
- Working tree: branch `feat/avatar-v1-and-opencode-v2`, clean except for the spec just saved (`docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md`).

No existing AppData/path abstraction lives in `Iris.Shared`, `Iris.Infrastructure`, or anywhere else. No competing seam exists.

## 4. Proposed Design Summary

The design has **two coordinated tracks**, both of which fit inside the existing Desktop-host boundary.

### Track 1 — Database path stabilization (code)

Introduce two **Desktop-internal, host-only** types:

1. **`DesktopAppDataLocator`** — translates "I need the Iris per-user data directory" into an absolute path under `Environment.SpecialFolder.ApplicationData`, and ensures the directory exists. Owned by Desktop. No DI registration; constructed in DI composition only. Pure host concern.
2. **`DesktopDatabasePathResolver`** — given the optional `Database:ConnectionString` config value and a `DesktopAppDataLocator`, returns the **final connection string** that will be handed to `AddIrisPersistence`. Accepts three inputs (none / full conn string / bare absolute path), rejects relative paths with a clear, host-level exception. Owned by Desktop.

These types are wired exclusively inside `Iris.Desktop.DependencyInjection.AddIrisDesktop(...)`. Nothing about them leaks into Application, Domain, Shared, or any adapter.

`appsettings.json` (committed) drops the `Database:ConnectionString` key. `appsettings.local.json` becomes the documented developer override (gitignored already by `.gitignore` at repo root — design preserves that). A tracked **`appsettings.local.example.json`** documents the override format and is shipped as documentation only (no `CopyToOutputDirectory`, no behavior).

### Track 2 — Manual smoke (procedure)

Introduce a **smoke procedure document** alongside the spec, designed to be executed against the post-Track-1 build. It contains, per scenario M-01–M-07:

- preconditions (Ollama state, config used, observed db file path, build SHA);
- numbered steps the operator performs;
- expected outcome (lifted from the Phase 5.5 spec §11 table);
- a results template the operator fills in;
- explicit recording instructions for `.agent/PROJECT_LOG.md` and (only on anomaly) `.agent/log_notes.md`.

This document is the artifact the operator works from. It is not code. It is created in `docs/manual-smoke/` (new convention; rationale in §14 Option B).

### What does NOT change

- `DatabaseOptions` (still `{ ConnectionString }`).
- `AddIrisPersistence` (still `Action<DatabaseOptions>`).
- `IrisDatabaseInitializer` (still `EnsureCreatedAsync`).
- All Application/Domain/Shared source.
- All Avatar v1 source (`AvatarViewModel`, `AvatarPanel`, `AvatarOptions`, etc.).
- All ChatViewModel / IrisApplicationFacade source.
- All EF entity configurations, repositories, mappers.
- `Iris.Architecture.Tests` boundary assertions.
- `Iris.Api`, `Iris.Worker` hosts.

## 5. Responsibility Ownership

| Responsibility | Owner | Notes |
|---|---|---|
| Resolve absolute per-user app-data directory | `Iris.Desktop` (new `DesktopAppDataLocator`) | Single host primitive over `Environment.SpecialFolder.ApplicationData`. Not a Shared primitive — Iris-product-named. |
| Ensure app-data directory exists on first launch | `Iris.Desktop` (new `DesktopAppDataLocator`) | Idempotent `Directory.CreateDirectory`. Throws on failure. |
| Decide which connection string the host will pass to Persistence | `Iris.Desktop` (new `DesktopDatabasePathResolver`) | Reads `Database:ConnectionString` (now optional), normalizes, validates, returns final value. |
| Reject relative-path overrides | `Iris.Desktop` (new `DesktopDatabasePathResolver`) | Fail-fast at startup with named-key error per FR-008. |
| Compose final DI graph and pass connection string to `AddIrisPersistence` | `Iris.Desktop.DependencyInjection.AddIrisDesktop` | Existing host composition — extended, not replaced. |
| Read the committed `appsettings.json` and optional `appsettings.local.json` | `Iris.Desktop.App.OnFrameworkInitializationCompleted` → `BuildConfiguration` | **Unchanged** — already correct. |
| Apply `EnsureCreatedAsync` against the resolved file | `Iris.Persistence.IrisDatabaseInitializer` | **Unchanged** — receives the resolved path indirectly via `DatabaseOptions`. |
| Map provider/database errors to user-visible messages | `Iris.Desktop.Services.DesktopErrorMessageMapper` (existing) | **Unchanged** for normal runtime; startup-time path errors surface as fail-fast exceptions before this mapper applies (handled by the Avalonia/.NET startup path itself). |
| Document M-01–M-07 procedure | `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md` (new doc) | Operator-facing artifact. |
| Execute M-01–M-07 and record results | Human operator + `.agent/PROJECT_LOG.md` | Per FR-011 – FR-017. |

This table is **fully consistent** with `.opencode/rules/iris-architecture.md` and `.opencode/rules/no-shortcuts.md`: Desktop owns host concerns (configuration parsing, path resolution, composition root), Persistence still owns the database-adapter contract, no Application/Domain change, no host-to-host references introduced.

## 6. Component Design

### `DesktopAppDataLocator`

- **Owner layer:** `Iris.Desktop` (host).
- **Visibility:** `internal sealed class` in `Iris.Desktop` namespace (e.g. `Iris.Desktop.Hosting`).
- **Responsibility:** translate a logical request ("Iris user data root") into an absolute filesystem directory path; ensure that directory exists.
- **Inputs:** none (constructor has no arguments in the production wiring; testability achieved by a small constructor seam — see §14 Option C).
- **Outputs:** `string AppDataDirectory { get; }` — absolute path to the Iris app-data root.
- **Key operations:**
  - `string AppDataDirectory` — computed once from `Environment.GetFolderPath(SpecialFolder.ApplicationData, SpecialFolderOption.Create)` joined with `"Iris"`. The `Create` option already calls `Directory.CreateDirectory` for the special folder; we additionally ensure the `Iris` subdirectory exists.
  - `EnsureExists()` — calls `Directory.CreateDirectory(AppDataDirectory)`. Idempotent. Throws `InvalidOperationException` (with inner exception preserved) when:
    - `Environment.GetFolderPath(...)` returns null/empty;
    - `Directory.CreateDirectory` throws.
- **Collaborators:** `System.Environment`, `System.IO.Directory`, `System.IO.Path`. Nothing else.
- **Must not do:**
  - read or write any file inside the directory beyond directory creation;
  - touch the SQLite database;
  - perform network I/O;
  - depend on any Iris project other than (no project — BCL only);
  - leak into Avatar, ViewModel, or Application code paths.
- **Test seam:** see §14 Option C — for tests we construct it with an explicit override directory (`new DesktopAppDataLocator(rootOverride: <temp dir>)`) using an `internal` constructor visible to `Iris.Integration.Tests` via the existing `InternalsVisibleTo`. Production composition uses the parameterless path resolution.

### `DesktopDatabasePathResolver`

- **Owner layer:** `Iris.Desktop` (host).
- **Visibility:** `internal static class` (no instance state) — writing it as static keeps test ergonomics maximal and matches the existing `ParseEnumOrDefault`/`ParseDoubleOrDefault` static-helper convention in `Iris.Desktop.DependencyInjection`.
- **Responsibility:** given the optional configured override and a directory locator, produce the final connection string passed to `AddIrisPersistence`.
- **Public API (illustrative, internal only):**

  ```csharp
  // Iris.Desktop.Hosting.DesktopDatabasePathResolver — illustrative
  internal static class DesktopDatabasePathResolver
  {
      // Returns the final EF Core SQLite connection string.
      // Throws InvalidOperationException with a clear message naming
      // "Database:ConnectionString" if the override is invalid.
      internal static string Resolve(
          string? configuredOverride,
          DesktopAppDataLocator locator);
  }
  ```

- **Behavior matrix** (FR-005 / FR-007 / FR-008):

  | `configuredOverride` value | Treated as | Output |
  |---|---|---|
  | `null` or whitespace | absent (default flow) | `"Data Source=" + Path.Combine(locator.AppDataDirectory, "iris.db")` |
  | starts with case-insensitive `"Data Source="` and Data Source value is rooted (`Path.IsPathFullyQualified`) | full connection string | returned **verbatim** |
  | starts with case-insensitive `"Data Source="` and Data Source value is **not** rooted | invalid relative override | **throws** with message identifying `Database:ConnectionString` and the rejected value |
  | does not start with `"Data Source="` and is rooted (`Path.IsPathFullyQualified`) | bare absolute path | `"Data Source=" + value` |
  | does not start with `"Data Source="` and is not rooted | invalid relative override | **throws** with message identifying `Database:ConnectionString` and the rejected value |

- **Parsing notes:**
  - "starts with `Data Source=`" is detected via case-insensitive prefix match on the trimmed value. Other connection-string keywords (`DataSource`, `Filename`) are deliberately **not** supported in this iteration to keep the resolver's contract narrow; if a developer hand-writes a connection string with an exotic form, they get the "neither a connection string nor an absolute path" error.
  - "rooted" is `Path.IsPathFullyQualified(...)`, which on Windows accepts `C:\foo`, UNC paths `\\host\share\...`, and on Unix accepts paths starting with `/`. It rejects `~/...` (correct — `~` is shell expansion, not a path). It rejects `./foo`, `.\foo`, `..\foo`, bare `iris.db`. This precisely matches FR-008.
- **Inputs:** the optional configured string (already extracted by `AddIrisDesktop`), the locator instance.
- **Outputs:** the final connection string, or thrown `InvalidOperationException`.
- **Collaborators:** `System.IO.Path`, `DesktopAppDataLocator`. Nothing else.
- **Must not do:**
  - touch the filesystem (no existence checks, no creation — that is the locator's job for the directory only);
  - parse beyond the minimal `Data Source=` prefix recognition;
  - read configuration directly (the caller passes the string in).

### Smoke procedure document `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md`

- **Owner layer:** documentation (not source code).
- **Responsibility:** define an unambiguous operator script for M-01 through M-07 against the post-stabilization build, plus a results-recording template.
- **Structure (illustrative outline, not the document itself):**
  1. Preconditions section: build SHA, `dotnet build` + `dotnet test` clean, Ollama running for M-01–M-04, expected default db path printed.
  2. One subsection per scenario M-01 through M-07, each with: preconditions delta from §1, numbered steps, expected outcome (lifted verbatim from `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11), results template (Pass / Fail / Anomaly + free-form notes).
  3. Recording section: how to append a `## YYYY-MM-DD — Phase 5.5 Manual Smoke` block to `.agent/PROJECT_LOG.md` and (if needed) `.agent/log_notes.md`.
- **Must not do:**
  - become an automated test harness;
  - replace or contradict the original Phase 5.5 spec;
  - modify any code path automatically.

## 7. Contract Design

### Existing contracts (state)

| Contract | Owner | Change | Compatibility |
|---|---|---|---|
| `Iris.Persistence.Database.DatabaseOptions` | Persistence | **Unchanged.** Still `{ string ConnectionString }` with `Validate()`. | n/a |
| `Iris.Persistence.DependencyInjection.AddIrisPersistence(Action<DatabaseOptions>)` | Persistence | **Unchanged.** Accepts whatever connection string Desktop hands in. | Backward compatible |
| `Iris.Persistence.Database.IIrisDatabaseInitializer` / `IrisDatabaseInitializer` | Persistence | **Unchanged.** | n/a |
| `Iris.Desktop.DependencyInjection.AddIrisDesktop(IServiceCollection, IConfiguration)` | Desktop host | **Behavior changed:** `Database:ConnectionString` is now read as **optional** (`GetValue<string?>`), not via the existing `GetRequiredString` helper. The signature does not change. | Backward compatible: when an absolute connection string is present in config, behavior matches today; when missing, the resolver supplies the per-user default; when present-but-relative, behavior **changes** from "passed through" to "fail-fast" (this is the intended fix). |
| `appsettings.json` schema | Desktop host | `Database:ConnectionString` removed from the committed file (key absent → default per-user path used). The remaining sections (`Application`, `ModelGateway`, `Desktop:Avatar`) are untouched. | New launches without local override use per-user path. |
| `appsettings.local.json` schema | Desktop host (developer-facing) | New documented use: `Database:ConnectionString` may hold a full absolute SQLite connection string OR a bare absolute path. | Optional file; absent → defaults. |

### New contracts (Desktop-internal only)

| Contract | Owner | Consumers | Shape | Compatibility |
|---|---|---|---|---|
| `DesktopAppDataLocator` | `Iris.Desktop` | `AddIrisDesktop` (composition root only) | `internal sealed class` with `string AppDataDirectory` and `EnsureExists()` | Internal — no public surface |
| `DesktopDatabasePathResolver.Resolve(string? configured, DesktopAppDataLocator locator)` | `Iris.Desktop` | `AddIrisDesktop` (composition root only) | `internal static` method, returns `string`, throws `InvalidOperationException` on invalid override | Internal — no public surface |

**No public contract is added, modified, or removed.** This is the central architectural property of the design: the change is entirely confined to the host composition root.

## 8. Data Flow

### Primary Flow — Default first launch (no `appsettings.local.json`, no relative override)

1. `Program.Main` → `AppBuilder.StartWithClassicDesktopLifetime` → `App.OnFrameworkInitializationCompleted`.
2. `App.BuildConfiguration` reads `appsettings.json` (committed, **no** `Database:ConnectionString` key) and optionally `appsettings.local.json` (absent in this flow).
3. `App.BuildServiceProvider` calls `services.AddIrisDesktop(configuration)`.
4. Inside `AddIrisDesktop`:
   - Constructs `var locator = new DesktopAppDataLocator()`; calls `locator.EnsureExists()` → `%APPDATA%\Iris\` is created (no-op if already exists).
   - Reads `configuredOverride = configuration.GetValue<string?>("Database:ConnectionString")` → `null`.
   - `var connectionString = DesktopDatabasePathResolver.Resolve(configuredOverride, locator)` → `"Data Source=C:\Users\<user>\AppData\Roaming\Iris\iris.db"`.
   - Calls `services.AddIrisPersistence(o => o.ConnectionString = connectionString)` (this line is unchanged from today — the value handed in is just resolved differently).
5. `App.InitializeDatabase` → `IIrisDatabaseInitializer.InitializeAsync` → `_dbContext.Database.EnsureCreatedAsync()` creates the SQLite file at the resolved absolute path on first launch.
6. `MainWindow` opens. Phase 5/5.5 chat + avatar behavior is unchanged from today.

### Alternative Flow A — Developer override via `appsettings.local.json` (full connection string, absolute)

1. As steps 1–2.
2. `appsettings.local.json` has `"Database": { "ConnectionString": "Data Source=C:\\Dev\\iris-dev.db;Cache=Shared" }`.
3. Configuration overlay produces `configuredOverride = "Data Source=C:\\Dev\\iris-dev.db;Cache=Shared"`.
4. `Resolve(...)` → starts with `Data Source=`, the data-source token is rooted → returned **verbatim**.
5. `AddIrisPersistence` receives the developer's connection string. EF Core targets `C:\Dev\iris-dev.db`.

### Alternative Flow B — Developer override via bare absolute path

1. `appsettings.local.json` has `"Database": { "ConnectionString": "C:\\Dev\\iris-dev.db" }`.
2. `Resolve(...)` → does not start with `Data Source=`, the value is rooted → returns `"Data Source=C:\Dev\iris-dev.db"`.
3. Same downstream behavior as Flow A.

### Error Flow A — Relative override (the bug being fixed)

1. `appsettings.local.json` has `"Database": { "ConnectionString": "Data Source=iris.db" }` or `"Database": { "ConnectionString": "iris.db" }`.
2. `Resolve(...)` detects the unrooted Data Source → throws `InvalidOperationException("Database:ConnectionString must be an absolute path or a connection string with an absolute Data Source. Got: '<value>'.")`.
3. The exception propagates through `AddIrisDesktop` → `BuildServiceProvider` → `OnFrameworkInitializationCompleted`. Avalonia's classic-desktop lifetime surfaces the exception via the runtime's standard unhandled-exception path. The application fails fast at startup. **No SQLite file is created.**

### Error Flow B — `Environment.SpecialFolder.ApplicationData` returns empty

1. Rare on Windows; possible on misconfigured Linux.
2. `DesktopAppDataLocator` constructor throws `InvalidOperationException("Could not resolve user application data directory. Environment.SpecialFolder.ApplicationData returned an empty value.")`.
3. Same propagation as Error Flow A. No directory created. No silent CWD fallback.

### Error Flow C — Cannot create app-data directory (permissions / read-only fs)

1. `DesktopAppDataLocator.EnsureExists` calls `Directory.CreateDirectory(...)` which throws e.g. `UnauthorizedAccessException` or `IOException`.
2. The locator wraps it: `throw new InvalidOperationException($"Could not create Iris app data directory at '{AppDataDirectory}'.", inner)`.
3. Same propagation. No silent fallback.

### Manual Smoke Flow

The smoke procedure is sequenced **after** the code changes are built green:

1. Operator builds Desktop (`dotnet build .\Iris.slnx`), runs full tests (`dotnet test .\Iris.slnx`).
2. Operator deletes any stale `iris.db` in `src/Iris.Desktop/bin/...` and in the repo root (DS-002), confirming no shadow database remains.
3. Operator runs M-01: launch Desktop with Ollama running. Confirms a fresh `iris.db` was created at `%APPDATA%\Iris\iris.db`. Records observation.
4. Operator runs M-02 → M-04 in sequence in the same launched window where possible (multi-message session).
5. Operator runs M-05, M-06, M-07 with `appsettings.local.json` edits between launches (each requires a restart per FR-006 / DS-005).
6. Operator appends results to `.agent/PROJECT_LOG.md` per the smoke document; appends to `.agent/log_notes.md` only on anomaly.

## 9. Data and State Design

- **On-disk artifacts created by this design:**
  - One directory: `%APPDATA%\Iris\` (or platform-equivalent under `Environment.SpecialFolder.ApplicationData`).
  - One file: `%APPDATA%\Iris\iris.db` on first launch.
  - Optional, tracked: `src/Iris.Desktop/appsettings.local.example.json` (documentation; no behavior; not copied to output).
  - Optional, untracked: `src/Iris.Desktop/appsettings.local.json` (already gitignored convention; design does not commit it).
  - One new tracked doc: `docs/manual-smoke/2026-05-01-phase-5-5-avatar-v1.smoke.md`.
- **In-memory state:** the resolved connection string lives in the `DatabaseOptions` singleton already registered by `AddIrisPersistence`. No new singleton, no new scope.
- **Lifecycle:** the locator and resolver run once, at composition time. They are not registered in DI. After `AddIrisDesktop` returns, neither type is reachable from anywhere except through the resolved connection string already inside `DatabaseOptions`.
- **Identity / ordering:** none. Path resolution is a pure function of `(configuredOverride, environment)`.
- **Migrations:** none introduced. Existing `EnsureCreatedAsync` continues; existing tracked Medium debt (production migrations) remains separate.
- **Existing developer databases:** orphaned per spec DS-002. The smoke document explicitly instructs the operator to delete `bin/.../iris.db` to avoid confusion — this is a manual one-time cleanup, not coded.

## 10. Error Handling and Failure Modes

| Failure mode | Required behavior | Where handled |
|---|---|---|
| `Environment.GetFolderPath(SpecialFolder.ApplicationData)` returns empty | `DesktopAppDataLocator` throws `InvalidOperationException` with the named primitive | Component design §6 |
| `Directory.CreateDirectory` throws | `DesktopAppDataLocator.EnsureExists` rethrows as `InvalidOperationException` with the path and the inner exception | §6 |
| Configured override is `null` / whitespace | Treated as absent → default per-user path | §6 behavior matrix row 1 |
| Configured override is full conn string with absolute `Data Source` | Returned verbatim | §6 row 2 |
| Configured override is full conn string with relative `Data Source` | `InvalidOperationException` naming `Database:ConnectionString` | §6 row 3, FR-008 |
| Configured override is bare absolute path | Wrapped in `Data Source=` and returned | §6 row 4 |
| Configured override is bare relative path | `InvalidOperationException` naming `Database:ConnectionString` | §6 row 5, FR-008 |
| Configured override is malformed (e.g. random text without `=` and not rooted) | Falls into "bare relative path" branch → fail-fast (`Path.IsPathFullyQualified("foo")` is false) | §6 row 5 |
| Configured override is rooted but points to a directory or non-writable file | Resolver does not check this; EF Core will surface the error from `EnsureCreatedAsync` later. Acceptable per spec (no extra preflight) | EF Core / OS |
| `appsettings.local.json` is malformed JSON | `Microsoft.Extensions.Configuration.Json` throws on `Build()`; propagates through `App.BuildConfiguration`. Unchanged behavior. | Existing |
| Concurrent Iris instances try to use the same db file | SQLite/EF locking surfaces; unchanged. Design adds nothing. | EF Core / SQLite |
| Manual smoke discovers a regression (e.g. Avatar timer issue) | Operator records anomaly per FR-014; **does not authorize a code fix in this iteration** (separate `/spec`). | Smoke procedure document |
| Manual smoke cannot be performed (no operator, no Ollama) | Implementation may still merge per FR-017; `.agent/overview.md` keeps M-01–M-07 listed as outstanding. | Spec / smoke doc |

**Cancellation:** none of the new code paths are async. No `CancellationToken` is plumbed through the resolver.

## 11. Configuration and Dependency Injection Impact

### Configuration

`src/Iris.Desktop/appsettings.json` (committed) — proposed shape after this design:

```jsonc
{
  "Application": { "Chat": { "MaxMessageLength": 8000 } },
  // "Database" key removed entirely.
  "ModelGateway": {
    "Ollama": { "BaseUrl": "http://localhost:11434", "ChatModel": "llama3.1", "TimeoutSeconds": 120 }
  },
  "Desktop": {
    "Avatar": { "Enabled": true, "Size": "Medium", "Position": "BottomRight", "SuccessDisplayDurationSeconds": 2.0 }
  }
}
```

`src/Iris.Desktop/appsettings.local.example.json` (new, **tracked documentation file** with no `CopyToOutputDirectory`):

```jsonc
// Copy this file to appsettings.local.json (gitignored) to override Iris Desktop runtime settings locally.
// All keys are optional. Layered over appsettings.json at startup.
{
  "Database": {
    // Either a full SQLite connection string with an absolute Data Source,
    //   "ConnectionString": "Data Source=C:/Dev/iris-dev.db;Cache=Shared",
    // or a bare absolute file path (Iris will wrap it as Data Source=...):
    //   "ConnectionString": "C:/Dev/iris-dev.db"
    // Relative paths (e.g. "iris.db", "./data/iris.db") are rejected at startup.
  }
}
```

The `.example.json` extension is chosen because the existing csproj already has `<None Update="appsettings.local.json" CopyToOutputDirectory="PreserveNewest" />` for the gitignored real file. The example file does **not** get a `CopyToOutputDirectory` directive so it cannot accidentally shadow the developer's real local config in the build output.

### DI Impact

`Iris.Desktop.DependencyInjection.AddIrisDesktop` changes shape internally only. Pseudocode:

```csharp
// illustrative — design only
public static IServiceCollection AddIrisDesktop(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing arg-null guards, MaxMessageLength, Ollama options unchanged ...

    var locator = new DesktopAppDataLocator();
    locator.EnsureExists();

    var configuredDb = configuration.GetValue<string?>("Database:ConnectionString");
    var resolvedDb   = DesktopDatabasePathResolver.Resolve(configuredDb, locator);

    services.AddIrisApplication(new SendMessageOptions(maxMessageLength));
    services.AddIrisPersistence(options => options.ConnectionString = resolvedDb); // only this line's argument changes
    services.AddIrisModelGateway(options => { /* unchanged */ });

    // ... existing avatar registrations unchanged ...
    return services;
}
```

**No new DI registration is added.** The locator and resolver are local to the composition method. They are not service-locator-resolved. They cannot be obtained from `IServiceProvider` at runtime — by design, because they are pure host wiring.

The existing `GetRequiredString(configuration, "Database:ConnectionString", ...)` call is **removed** for that key. Other `GetRequiredString`/`GetRequiredPositiveInt32` usages for `Application:Chat:MaxMessageLength`, `ModelGateway:Ollama:BaseUrl`, `ChatModel`, `TimeoutSeconds` are **unchanged**.

### Project references / packages

- No new NuGet packages.
- No `csproj` reference changes.
- The new `.example.json` is included in the csproj as `<None Include="appsettings.local.example.json" />` (no copy directive) so it appears in Solution Explorer but does not interfere with build output.

## 12. Security and Permission Considerations

| Concern | Assessment |
|---|---|
| Secrets in config | The database path is not a secret. The connection string never contains credentials (SQLite local file, no password). `appsettings.local.json` may legitimately contain developer absolute paths — no secret-handling rule from `.opencode/rules/security.md` is engaged. |
| Logging of paths | The fail-fast exception messages include the rejected configuration value. This is acceptable: the value is supplied by the operator and the message is the diagnostic the operator needs. No log sink writes the resolved path to disk in this iteration (no logger added). |
| File system surface | The locator writes only to `Environment.SpecialFolder.ApplicationData\Iris\`. No traversal outside that root. The resolver does not touch the filesystem. |
| Override accepting any absolute path | Acceptable: the override is opt-in and developer-facing. Iris Desktop is a per-user application; granting the user the ability to point their own app at an arbitrary file they own does not cross a trust boundary. |
| Multiple Iris instances writing to one db | Not introduced or worsened by this design. SQLite/EF behavior unchanged. |
| Permissions / Tools / Voice / Perception | Not engaged. Path resolver lives at composition root and never collaborates with these adapters. |
| Host-isolation rule | Honored. Desktop owns the locator. `Iris.Api` and `Iris.Worker` are not touched. |

The design preserves all rules in `.opencode/rules/security.md` and `.opencode/rules/no-shortcuts.md`.

## 13. Testing Design

### 13.1 Unit / integration tests (new)

Project: `tests/Iris.IntegrationTests/Desktop/` (already where Avatar/Chat ViewModel tests live; uses the Desktop `InternalsVisibleTo` seam). New file scope decided in `/plan`.

Mapped to spec test IDs:

| Spec ID | Test name (illustrative) | Asserts |
|---|---|---|
| T-PR-01 | `Resolve_WithoutOverride_ReturnsAbsoluteDataSourceUnderAppDataIris` | `configuredOverride = null`; result starts with `"Data Source="`; `Path.IsPathFullyQualified` on the data-source value; ends with `iris.db`; the directory portion is the locator's `AppDataDirectory`. |
| T-PR-02 | `Resolve_WithFullConnectionStringAndAbsoluteDataSource_ReturnsVerbatim` | Verbatim equality. |
| T-PR-03 | `Resolve_WithBareAbsolutePath_NormalizesToDataSource` | Result equals `"Data Source=" + input`. |
| T-PR-04 | `Resolve_WithBareRelativePath_Throws` | Three theory rows: `"iris.db"`, `"./data/iris.db"`, `"..\\foo.db"`. Asserts `InvalidOperationException` and that the message contains `Database:ConnectionString` and the offending value. |
| T-PR-05 | `Resolve_WithConnectionStringWhoseDataSourceIsRelative_Throws` | Two theory rows: `"Data Source=iris.db"`, `"Data Source=./foo.db;Cache=Shared"`. Same assertion shape as T-PR-04. |
| T-PR-06 | `AppDataLocator_WhenDirectoryMissing_CreatesIt` | Constructs locator with a temp-directory override (test-only `internal` constructor); calls `EnsureExists()`; asserts directory now exists; cleans up. Also covers idempotence (second call). |
| T-PR-07 | `Resolver_TypesAreNotReferencedFromForbiddenProjects` | The existing `DependencyDirectionTests` and `ForbiddenNamespaceTests` already assert that Application/Domain do not reference adapters or Desktop. The new types live in `Iris.Desktop` which is already referenced by `Iris.Integration.Tests`. Whether to add an explicit new architecture assertion is left to `/plan` (existing tests may already be sufficient). |
| T-PR-08 | (existing) | `IrisDatabaseInitializerTests` and `PersistenceTestContextFactory`-based tests pass unchanged. |
| T-PR-09 | (existing) | All 20 `AvatarViewModelTests` continue to pass. |

### 13.2 Architecture tests (existing, must continue to pass)

- `DependencyDirectionTests.Domain_depends_only_on_Shared` — unaffected.
- `DependencyDirectionTests.Application_depends_only_on_Domain_and_Shared` — unaffected.
- `ForbiddenNamespaceTests.Domain_does_not_reference_EntityFrameworkCore` — unaffected.
- `ForbiddenNamespaceTests.Application_does_not_reference_Persistence` / `ModelGateway` — unaffected.
- `ProjectReferenceTests.Desktop_does_not_reference_Api_or_Worker` and reverse — unaffected.

Reasoning: the new types live entirely inside `Iris.Desktop`. They reference only BCL. The only assembly relationship that exists is `Iris.Desktop → System.IO`, which is not policed by these tests.

### 13.3 Required commands (no regression)

```
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-restore
dotnet format .\Iris.slnx --verify-no-changes
```

Total tests after implementation: existing 126 + new path-provider tests (5–7 added). All must pass.

### 13.4 Manual verification

The seven scenarios M-01 through M-07 from `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11, executed per the new smoke procedure document. Recording per FR-011 – FR-015.

## 14. Options Considered

### Option A — Place the path provider in `Iris.Shared`

- Summary: introduce a generic `IAppDataLocator` in `Iris.Shared` (alongside `IClock`).
- Benefits: future Iris.Api/Iris.Worker hosts could reuse the abstraction.
- Drawbacks: `Iris.Shared` rule (`.opencode/rules/iris-architecture.md` and `.opencode/rules/no-shortcuts.md`): "Shared must stay product-neutral." A locator named "Iris" violates that. A neutral `IAppDataLocator` named without product is fine in principle, but each host is opinionated about *which* subdirectory ("Iris", possibly "Iris.Api", "Iris.Worker") and per the spec only Desktop is in scope. Building a generic abstraction now to serve a hypothetical future need violates YAGNI and adds Shared surface area before evidence of need.
- **Verdict:** rejected. When Api/Worker need their own paths, refactor at that point.

### Option B — Place the smoke procedure inside the Avatar v1 spec or in `.agent/`

- Summary: append a "Manual Smoke Procedure" section directly to `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md`, or into `.agent/log_notes.md`.
- Benefits: fewer files.
- Drawbacks:
  - Editing a frozen spec violates the "Spec is fixed once approved" handoff rule.
  - `.agent/log_notes.md` is for failures and investigation notes, not procedure templates (`.opencode/rules/memory.md`).
- **Verdict:** rejected. A separate `docs/manual-smoke/...smoke.md` doc keeps the procedure discoverable, dated, and editable without touching frozen artifacts. New folder convention `docs/manual-smoke/` is introduced because no smoke folder exists today and shoving procedural docs into `docs/specs/` would muddle the spec corpus.

### Option C — Locator testability seam: parameterless vs. injectable root

- **C1 (parameterless only).** Locator computes path internally; tests cannot avoid touching the real `%APPDATA%`.
- **C2 (single internal ctor with optional `string? rootOverride`).** Production: `new DesktopAppDataLocator()` calls `Environment.GetFolderPath(...)`. Tests: `new DesktopAppDataLocator(Path.GetTempPath() + "/iris-test-" + Guid)` (via `InternalsVisibleTo`).
- **C3 (inject an `Environment.SpecialFolder` resolver func).** Excessive abstraction for a test seam.
- **Verdict (chosen for §6):** **C2.** Smallest seam that still covers T-PR-06 (directory creation) without polluting the real user `%APPDATA%`. The override constructor is `internal`, only callable from `Iris.Integration.Tests` via `InternalsVisibleTo`. Production code path uses the parameterless constructor exclusively.

### Option D — Sentinel value vs. omitted key in committed `appsettings.json`

- D1: keep the key with an empty string (`"ConnectionString": ""`).
- D2: keep the key with a sentinel (`"ConnectionString": "USE_DEFAULT"`).
- D3: omit the key entirely.
- **Verdict (chosen):** **D3 (omit).** `Microsoft.Extensions.Configuration` returns `null` for missing keys, which the resolver already treats as "use default" — symmetric with the `appsettings.local.json` "absent" case. Sentinels add a magic string and a parsing branch. An empty string adds an ambiguity (is `""` "use default" or "developer cleared the value"?). Omission is the cleanest, fewest-surprise option.

### Option E — Validate via `Path.IsPathFullyQualified` vs. `Path.IsPathRooted`

- `IsPathRooted` returns true for `"\foo"` on Windows (rooted to current drive), which is still relative to whatever drive the process happens to run on — exactly the bug class being fixed.
- `IsPathFullyQualified` returns true only for genuinely absolute paths (`C:\foo`, UNC, `/foo` on Unix).
- **Verdict (chosen):** **`IsPathFullyQualified`** (.NET Core 2.1+, available on .NET 10).

## 15. Risks and Trade-offs

| Risk / Trade-off | Severity | Mitigation |
|---|---|---|
| Existing developer launches that relied on `iris.db` in CWD will appear to "lose" their conversations because the new default path is different. | Low (project pre-production; no real user data). | Smoke document explicitly tells the operator to clean stale `bin/.../iris.db` before M-01. `.agent/PROJECT_LOG.md` entry will document the path migration. |
| Linux/macOS `Environment.SpecialFolder.ApplicationData` resolution on edge configurations (no `XDG_DATA_HOME`, no `HOME`) returns empty. | Low. Iris is Windows-first today (`<OutputType>WinExe</OutputType>` in csproj). | Locator throws fail-fast with a clear message; the spec accepts that this is out of acceptance for v1 but must not actively break those platforms. The thrown error makes the failure mode visible. |
| Tests that need real `%APPDATA%` on a CI agent could pollute the runner home directory. | Low. | Test seam (Option C2) ensures tests pass an absolute temp directory; production code path is exercised only by manual smoke. |
| Operator forgets to record an anomaly in `.agent/log_notes.md`. | Medium (procedural). | Smoke document includes the recording template inline so the operator's next action is obvious. |
| Operator runs M-01 with a stale `appsettings.local.json` left over from a previous experiment, contaminating "default behavior" verification. | Medium. | Smoke document precondition step explicitly: "verify `appsettings.local.json` is absent or `Database:ConnectionString` is unset before M-01–M-04". |
| Future need for the same path resolver in `Iris.Api` or `Iris.Worker` will require code duplication or a refactor into Shared. | Low (deferred). | Per Option A verdict, refactor at that future point with evidence. The cost of duplication is one ~30-line type. |
| `Path.IsPathFullyQualified` semantics on Linux for `~/foo` (returns false). | Low. | Documented in Option E. The smoke document and the `.example.json` recommend explicit absolute paths. |
| Connection-string parsing is intentionally narrow (only `Data Source=` recognized). | Low. | A developer with an exotic connection string should pass the path bare; documented in `.example.json`. The narrow parser is auditable. |
| Manual smoke records a Pass for M-01–M-07 that masks a regression visible only under heavy load. | Medium (inherent to manual smoke). | This iteration does not pretend manual smoke replaces stress testing. The acceptance criterion is "all M-01–M-07 recorded with operator-judged outcome," not "no regression possible." |

## 16. Acceptance Mapping

| Spec acceptance criterion | Design coverage |
|---|---|
| `dotnet build .\Iris.slnx` 0/0 | No new packages; csproj structure preserved; new types compile inside Desktop. |
| `dotnet test .\Iris.slnx --no-restore` passes 126 + new | §13.1 maps each spec test ID to a concrete test. |
| `dotnet format --verify-no-changes` passes | New types follow existing Desktop file/style conventions; no formatter regression expected. |
| Default launch uses `<ApplicationData>/Iris/iris.db` | §6 + §8 Primary Flow + §11 (key omitted from `appsettings.json`). |
| Override via `appsettings.local.json` to absolute path is honored | §6 behavior matrix rows 2 & 4; §8 Alt Flows A & B. |
| Relative override fails fast with named key | §6 behavior matrix rows 3 & 5; §10. |
| No source change in Application/Domain/Shared/Persistence/ModelGateway/Infrastructure | §3, §5, §7, §11. The persistence contract `DatabaseOptions.ConnectionString` is unchanged; everything else is host-internal. |
| No new NuGet package | §11. |
| No new project reference in `Iris.Desktop.csproj` | §11. |
| M-01–M-07 executed and recorded | §6 smoke document + §13.4 + §8 Manual Smoke Flow. |
| Anomaly recording path defined | §10 + §6 smoke document. |
| `.agent/overview.md`, `PROJECT_LOG.md`, `debt_tech_backlog.md` updates documented | §6 smoke document instructions; deferred to `/plan` and `/implement` for actual edits. |

## 17. Open Questions

No blocking open questions.

The four spec-deferred design decisions are resolved as follows:

- Resolver shape → `internal static class DesktopDatabasePathResolver` with `Resolve(string?, DesktopAppDataLocator)`. (Option C alignment with existing `DependencyInjection` static helpers.)
- `appsettings.json` form → omit `Database:ConnectionString`. (Option D3.)
- Override documentation → tracked `appsettings.local.example.json`. (Option B-rejection consequence: keep the smoke doc separate; keep the override doc as a sample file the operator copies.)
- Test seam for T-PR-06 → injectable root via `internal` constructor on `DesktopAppDataLocator`. (Option C2.)

These are design decisions, not specification ambiguities. They can be reopened in `/plan` or `/architecture-review` if needed.
