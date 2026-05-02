# Specification: Phase 5.5 Manual Smoke Closure (M-01–M-07) and Desktop SQLite Path Stability

## 1. Problem Statement

Two outstanding items block confident readiness for the Phase 5.5 Avatar v1 + Phase 5 Desktop chat slice:

**Problem A — Manual smoke M-01–M-07 not executed.**
`docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11 defines seven manual smoke scenarios for Avatar v1: live launch with Ollama running (M-01), Thinking on send (M-02), Success then Idle on response (M-03), Error path with stopped Ollama (M-04), `Enabled=false` hides avatar (M-05), `Size=Large` enlarges avatar (M-06), `Position=TopLeft` repositions avatar (M-07). `.agent/overview.md:27` records that *"Manual UI smoke (M-01–M-07) for Desktop still requires a human-visible desktop session with running Ollama (not blocked by P1 fixes)"*. No M-01–M-07 evidence has been recorded in `.agent/PROJECT_LOG.md`. Avatar v1 cannot be claimed user-verified without this evidence, and the same live session is the only place where Phase 5 Desktop chat behavior under live Ollama is also confirmed end-to-end.

**Problem B — Desktop SQLite path uses relative working directory.**
`.agent/debt_tech_backlog.md` (lines 138–152) records this as Medium-priority debt. `src/Iris.Desktop/appsettings.json` line 8 sets `"ConnectionString": "Data Source=iris.db"`. `src/Iris.Persistence/DependencyInjection.cs` line 25 passes that string directly to `UseSqlite`. EF Core resolves relative SQLite paths against the process current working directory, which differs across launch modes (CLI from project folder, IDE run, packaged executable, shortcut). `.agent/PROJECT_LOG.md` line 837 documents this in the Phase 5 startup smoke: *"`dotnet run … --no-build` launched for 10 seconds without immediate exit and created `iris.db`"* — the file landed in the project folder rather than a stable user data location. As a result, persisted conversation/message data appears to vanish or split when the app is launched differently, and M-01–M-07 manual smoke (which exercises end-to-end persistence implicitly through chat) would inherit this instability.

Solving Problem B before Problem A removes a known confound from manual smoke and gives M-01–M-07 a stable database location to verify against.

## 2. Goal

Deliver, in a single iteration, two coordinated outcomes:

1. **Stable Desktop database location.** Iris Desktop persists its SQLite database at a documented, predictable per-user location by default, configurable through `appsettings.local.json`, irrespective of launch mode. Existing Application/Domain/Persistence boundaries are preserved.

2. **Recorded manual smoke pass for M-01–M-07.** A human operator runs M-01–M-07 against a built `Iris.Desktop` with the new database location active, records observed behavior per scenario, and the result is captured as durable evidence in agent memory. Any defect found is converted into a recorded log_notes entry rather than silently fixed.

After this iteration:

- `.agent/overview.md` no longer lists M-01–M-07 as outstanding;
- `.agent/debt_tech_backlog.md` no longer lists "Desktop SQLite path uses relative working directory" as open;
- `.agent/PROJECT_LOG.md` contains a dated entry describing the smoke results and the path stabilization;
- the Phase 5.5 Avatar v1 + Phase 5 Desktop chat slice is ready for `/audit` and merge readiness review.

## 3. Scope

### In Scope

1. Introduction of an Iris Desktop **app data directory provider** that resolves a stable per-user directory for Iris Desktop runtime data (database, future logs/cache). The provider is host-owned (Desktop) and is not exposed to Application/Domain.
2. Desktop host wiring that:
   - resolves the SQLite database file path at startup using the provider when no explicit absolute path is configured;
   - accepts an explicit override from `appsettings.local.json` (developer / advanced user);
   - composes the final EF Core SQLite connection string and passes it to `AddIrisPersistence` exactly as today.
3. Update of `src/Iris.Desktop/appsettings.json` so the default behavior produces a stable per-user location, not a relative `iris.db`.
4. Documented developer override mechanism through `appsettings.local.json` (a sample / example contents shown in spec — not committed if it would shadow user-local config).
5. Ensuring the chosen directory is created on first launch if missing, with permissions appropriate for the current OS user.
6. Adjustment of database initialization (`IrisDatabaseInitializer` already exists) so it operates on the resolved absolute path with no additional behavior change.
7. Execution of manual smoke **M-01 through M-07** on a human-visible Desktop session, with:
   - a defined operator script (preconditions, steps, expected outcome, recorded outcome per scenario);
   - structured artifact captured in `.agent/PROJECT_LOG.md` and, if any anomaly is observed, in `.agent/log_notes.md`.
8. Memory updates after the work: `.agent/PROJECT_LOG.md`, `.agent/overview.md` (next step / blockers), `.agent/debt_tech_backlog.md` (close the SQLite path debt entry), `.agent/log_notes.md` (only if anomalies observed).
9. Tests covering the new path-resolution behavior at the host level (where it lives) and config-driven override behavior, without regressing the existing 126 tests.

### Out of Scope

- Production EF Core migrations (separate Medium debt; not unblocked by this iteration).
- Migrating an existing project-folder `iris.db` to the new location (treated as one-time developer concern; documented but not coded).
- Cross-platform packaging, installer, MSIX, or auto-update.
- Encryption-at-rest of the SQLite file or any user-data classification beyond standard per-user storage.
- Changing `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.ModelGateway`, `Iris.Infrastructure` source.
- Changing the EF Core schema, repositories, or Domain entities.
- Changing the `DatabaseOptions` contract shape or its current `Validate()` behavior.
- Changing `AvatarViewModel`, `AvatarPanel`, `AvatarOptions`, `MainWindowViewModel`, or any Avatar v1 production code beyond what M-01–M-07 evidence requires.
- Adding logging infrastructure, telemetry, or diagnostics framework beyond what is strictly necessary to pass M-01–M-07.
- Adding `Iris.Api` or `Iris.Worker` paths for the new provider.
- Live-config reload (the override is read at startup only).
- Creating a `Iris.Desktop.Tests` project (already tracked as Medium debt).

### Non-Goals

- Treat the SQLite path provider as a generic "application data" service usable from non-host code. It must remain Desktop-host-owned.
- Use the Iris-Desktop database path provider to influence model gateway, persistence schema, or Application contracts.
- Block this iteration on perfecting smoke automation. Manual smoke remains manual; automation is a separate future task.
- Re-validate Phase 1–4 work that is already covered by `dotnet test`.

## 4. Current State

Inspected files (read-only):

- `src/Iris.Desktop/appsettings.json` — `"Database": { "ConnectionString": "Data Source=iris.db" }`.
- `src/Iris.Desktop/Iris.Desktop.csproj` — copies `appsettings.json` (Content) and updates `appsettings.local.json` if present (`None Update … CopyToOutputDirectory="PreserveNewest"`). No `appsettings.local.json` file currently exists in `src/Iris.Desktop/`.
- `src/Iris.Desktop/App.axaml.cs` — `BuildConfiguration()` uses `AppContext.BaseDirectory` and layers `appsettings.json` (required) then `appsettings.local.json` (optional). `OnFrameworkInitializationCompleted` calls `BuildServiceProvider(configuration)` then `InitializeDatabase(serviceProvider)`.
- `src/Iris.Desktop/DependencyInjection.cs` — reads `Database:ConnectionString` via `GetRequiredString` and passes the literal value to `AddIrisPersistence(o => o.ConnectionString = …)`. No path resolution, no AppData logic.
- `src/Iris.Persistence/DependencyInjection.cs` — `AddIrisPersistence(Action<DatabaseOptions>)` validates only that the connection string is non-empty, registers `DatabaseOptions` as singleton, configures `AddDbContext<IrisDbContext>(o => o.UseSqlite(options.ConnectionString))`, and registers `IIrisDatabaseInitializer`.
- `src/Iris.Persistence/Database/DatabaseOptions.cs` — single `string ConnectionString` with `Validate()` that throws on empty.
- `src/Iris.Persistence/Database/IrisDatabaseInitializer.cs` — calls `_dbContext.Database.EnsureCreatedAsync(cancellationToken)`.
- `src/Iris.Shared/**` — neutral primitives (`IClock`, `IGuidProvider`, results, guards, pagination). No existing path/AppData abstraction.
- `tests/Iris.IntegrationTests/Persistence/IrisDatabaseInitializerTests.cs` — already constructs an absolute test path via `Path.Combine(Path.GetTempPath(), …)` and feeds it as `Data Source={databasePath}`; this is unaffected by the host-side resolution and shows the connection-string contract is path-agnostic at the adapter boundary.
- `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs` — same pattern.
- `.agent/overview.md:27` — manual smoke M-01–M-07 outstanding.
- `.agent/debt_tech_backlog.md:138–152` — Desktop SQLite path uses relative working directory.
- `.agent/PROJECT_LOG.md:115–189` — Phase 5.5 Avatar v1 implementation entry; "Next: Run live interactive Desktop smoke with Ollama running (M-01–M-07 from spec)."
- `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md:308–319` — M-01–M-07 scenario table, source of truth for expected behavior.
- `.opencode/rules/no-shortcuts.md` — Desktop must not use `IrisDbContext` or repositories directly for product workflow; Desktop may still own host concerns such as configuration and path resolution.
- `.opencode/rules/security.md` — never read/print/store secrets; the database path itself is not a secret, but `appsettings.local.json` is the standard place for local-only overrides.
- Branch: `feat/avatar-v1-and-opencode-v2`. Working tree clean.

The contract `DatabaseOptions.ConnectionString` already accepts any valid SQLite connection string. **No persistence-adapter contract change is required** to make the path stable — the change is purely host-side composition.

## 5. Affected Areas

| Area | Impact |
|---|---|
| `Iris.Desktop` host (path provider, DI composition, App startup) | Extended (new Desktop-internal provider + wiring) |
| `Iris.Desktop/appsettings.json` | Default `Database:ConnectionString` removed or changed to a sentinel; new optional keys for path strategy |
| `Iris.Desktop/appsettings.local.json` (sample) | New documented override format (file optional, gitignored) |
| `Iris.Desktop/Iris.Desktop.csproj` | Possibly extended to include a documented `appsettings.local.example.json` (optional) |
| `Iris.Persistence` | **Not touched** (unchanged contract honored) |
| `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.ModelGateway`, `Iris.Infrastructure` | Not touched |
| `tests/Iris.IntegrationTests/Desktop/*` | Extended with host-level path resolution tests |
| `tests/Iris.IntegrationTests/Persistence/*` | Not touched |
| `tests/Iris.Architecture.Tests/*` | Not touched (no new boundary) |
| `.agent/PROJECT_LOG.md` | New entry (smoke + path stabilization) |
| `.agent/overview.md` | Status / next step updated |
| `.agent/debt_tech_backlog.md` | Close "Desktop SQLite path uses relative working directory" |
| `.agent/log_notes.md` | New entry only if smoke uncovered an anomaly |

## 6. Functional Requirements

### 6.1 Database Path Stabilization

- **FR-001.** Iris Desktop shall, by default and without any user-supplied configuration override, persist its SQLite database file at a stable per-user location that does not depend on the process current working directory.

- **FR-002.** The default per-user location shall be a dedicated `Iris` directory under the OS-appropriate per-user roaming application data folder, computed via `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` on all supported platforms (Windows: `%APPDATA%\Iris\`; Linux/macOS: the platform's `ApplicationData` resolution). The relative subpath `Iris/iris.db` shall be the default database file name. *(Implementation choice is constrained because the goal is platform-stability without packaging; `ApplicationData` is the smallest standard primitive available cross-platform.)*

- **FR-003.** If the resolved directory does not exist on first launch, Iris Desktop shall create it before initializing the database. Directory creation failure (permissions, IO) shall surface as a startup error with a clear, user-facing message and shall not silently fall back to the working directory.

- **FR-004.** Iris Desktop shall accept an override from configuration, read by the existing `BuildConfiguration()` chain (`appsettings.json` then `appsettings.local.json`). When the override is present, the override fully replaces the default-resolved value.

- **FR-005.** The override shall support two forms:
  - a **full SQLite connection string** (e.g. `Data Source=...;Cache=Shared`), used verbatim;
  - a **bare absolute file path** to a SQLite database file (e.g. `C:\Dev\iris-dev.db` or `/home/user/iris-dev.db`), which Iris Desktop normalizes to `Data Source=<absolute-path>`.
  Bare relative paths shall be rejected (see FR-008) — relative paths are the very defect being fixed.

- **FR-006.** The override shall live in `appsettings.local.json` by convention. Iris Desktop shall not require `appsettings.local.json` to exist; absence means default behavior per FR-001 / FR-002.

- **FR-007.** `appsettings.json` (committed) shall not contain a relative `Data Source=iris.db` value. It shall either omit the `Database:ConnectionString` key entirely (default flow) or contain a sentinel/placeholder that explicitly signals "use default per-user location." The chosen form is a Design decision; the spec only requires that committed config never produces a relative-CWD database file.

- **FR-008.** If `appsettings.local.json` (or any other layered config) provides a `Database:ConnectionString` whose "Data Source" component, after parsing, resolves to a relative path, Iris Desktop shall fail startup with a clear error identifying the offending key and the rejected value. No silent fallback.

- **FR-009.** The resolved absolute connection string shall be the value passed to `AddIrisPersistence(o => o.ConnectionString = ...)`. The contract of `DatabaseOptions` and `AddIrisPersistence` shall not change.

- **FR-010.** The path-resolution component shall be Desktop-host-owned. It shall not be referenced from `Iris.Application`, `Iris.Domain`, `Iris.Shared`, or any adapter project.

### 6.2 Manual Smoke M-01–M-07

- **FR-011.** A documented manual smoke procedure shall be executed against `Iris.Desktop` built from the current branch after FR-001–FR-010 are implemented. The procedure shall cover all seven scenarios M-01–M-07 as defined in `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11 "Manual Smoke," with no scenarios skipped.

- **FR-012.** Each scenario shall record:
  - preconditions (Ollama state, config used, database file location used, build configuration);
  - steps performed by the operator;
  - expected outcome (per the original spec table);
  - observed outcome (free-form short text);
  - pass/fail/anomaly classification.

- **FR-013.** Pass requires the observed outcome to match the expected outcome in spec §11 for that scenario.

- **FR-014.** Any anomaly (unexpected error, hang, visual glitch, missing transition, permission failure on the new database directory, etc.) shall be recorded in `.agent/log_notes.md` with reproduction steps, current branch SHA, and operator-assessed severity. Recording an anomaly shall not by itself fail this iteration; instead it converts the smoke into a partial pass that explicitly identifies which scenarios were clean vs anomalous.

- **FR-015.** After M-01–M-07 are executed, `.agent/PROJECT_LOG.md` shall be appended with a dated entry listing each scenario, its outcome, and a reference to the build SHA used. `.agent/overview.md` shall be updated to remove M-01–M-07 from the "Known Blockers" list when all seven are pass or pass-with-recorded-anomaly.

- **FR-016.** Manual smoke shall be executed by a human operator with a desktop session and a running Ollama instance for the live scenarios (M-01–M-04). M-05–M-07 (configuration-driven scenarios) require Ollama only insofar as the original spec rows specify. The operator may be the user; this spec does not authorize an automated agent to fabricate manual smoke evidence.

- **FR-017.** If M-01–M-07 cannot be executed by a human operator within the iteration (e.g., operator unavailable), the implementation work for FR-001–FR-010 may still complete and merge, but `.agent/overview.md` shall continue to list M-01–M-07 as outstanding, and the Phase 5.5 readiness claim shall not be made.

## 7. Architecture Constraints

- **AC-001.** The app-data path provider is owned by `Iris.Desktop` (host). It must not appear in `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure`, or any other host (`Iris.Api`, `Iris.Worker`).

- **AC-002.** `Iris.Persistence` shall continue to consume its database path exclusively through `DatabaseOptions.ConnectionString`. No new abstraction shall be introduced into `Iris.Application` or `Iris.Persistence` for path resolution.

- **AC-003.** The provider shall not be used by Avatar code, ChatViewModel, IrisApplicationFacade, or any UI/viewmodel; it is consumed only by `AddIrisDesktop` (DI composition) at startup.

- **AC-004.** No new runtime dependencies (NuGet packages) shall be added. The implementation shall use only the BCL primitives already available (`System.Environment`, `System.IO`, `Microsoft.Extensions.Configuration.*`).

- **AC-005.** The provider shall not perform I/O beyond directory existence check and creation. It shall not enumerate files, read existing databases, or copy data.

- **AC-006.** No file system operations against locations outside the resolved Iris app-data directory shall be performed by this code path.

- **AC-007.** Existing project references shall not change. `Iris.Desktop.csproj` references already cover `Iris.Application`, `Iris.Domain`, `Iris.ModelGateway`, `Iris.Persistence`, `Iris.Shared` and remain sufficient.

- **AC-008.** Host isolation: `Iris.Api` and `Iris.Worker` are not affected. If they later need their own database location, a separate spec/design will define how (out of scope here).

- **AC-009.** Desktop must continue not to access `IrisDbContext`, repositories, or model providers directly for product workflow (`.opencode/rules/no-shortcuts.md`). The path provider sits at composition root only; it does not read from the database.

## 8. Contract Requirements

| Contract | Current behavior | Required behavior | Compatibility |
|---|---|---|---|
| `DatabaseOptions.ConnectionString` | non-empty string passed to `UseSqlite` | unchanged: receives the resolved absolute connection string | **Unchanged** |
| `IIrisDatabaseInitializer.InitializeAsync` | calls `EnsureCreatedAsync` | unchanged | **Unchanged** |
| `AddIrisPersistence(Action<DatabaseOptions>)` | accepts options configurator | unchanged; receives resolved connection string from Desktop | **Unchanged** |
| `AddIrisDesktop(IServiceCollection, IConfiguration)` | reads `Database:ConnectionString` (required) | reads optional `Database:ConnectionString` override; falls back to provider-resolved default | **Extended backward-compatibly**; an absolute connection string in config still works |
| `appsettings.json` `Database:ConnectionString` | required value `Data Source=iris.db` | optional or sentinel; default behavior produces stable per-user path | **Behavior change** for default config consumers; documented |
| `appsettings.local.json` `Database:ConnectionString` | not currently used | optional; full connection string OR absolute path | **New documented contract** |
| Desktop `IIrisApplicationFacade`, `MainWindowViewModel`, `ChatViewModel`, `AvatarViewModel`, `AvatarOptions` | per current Phase 5/5.5 implementation | **unchanged** | **Unchanged** |
| `IrisDbContext`, repositories, `EfUnitOfWork` | per Phase 3 | **unchanged** | **Unchanged** |

No public Application/Domain/Shared contract is added, modified, or removed.

## 9. Data and State Requirements

- **DS-001.** A new on-disk artifact: a per-user Iris app-data directory containing the SQLite database file. Default path layout: `<ApplicationData>/Iris/iris.db`. The directory MAY in future contain additional Iris Desktop runtime artifacts; this iteration only creates and uses the database file.

- **DS-002.** Existing developer/project-folder `iris.db` files (created by past launches) are not migrated. They become orphaned. This is acceptable because the project is pre-production and no real user data exists. A short note in the smoke procedure shall instruct the operator to ensure no stale `src/Iris.Desktop/bin/.../iris.db` is being used by mistake (delete or ignore).

- **DS-003.** The database schema is unchanged. `EnsureCreatedAsync` continues to apply on first launch when the file does not exist.

- **DS-004.** The resolved connection string is not persisted anywhere except in DI as part of the `DatabaseOptions` singleton, exactly as today.

- **DS-005.** The override value in `appsettings.local.json` is read once at startup and not reloaded.

- **DS-006.** No migrations are introduced. The existing tracked debt around production EF migrations remains separate.

## 10. Error Handling and Failure Modes

| Failure mode | Required behavior |
|---|---|
| `Environment.GetFolderPath(SpecialFolder.ApplicationData)` returns empty string (rare on Linux when `XDG_DATA_HOME` and `HOME` are unset) | Startup fails with a clear, host-level error message identifying the resolution failure. No silent fallback to CWD. |
| Resolved Iris app-data directory cannot be created (permissions, read-only filesystem) | Startup fails with a clear error including the path and the OS error. |
| `appsettings.local.json` exists but is malformed JSON | Existing `Microsoft.Extensions.Configuration.Json` behavior surfaces a JSON parse error at startup. No new handling required; the error must not be swallowed. |
| `Database:ConnectionString` override is a non-empty string with a relative `Data Source=...` (e.g. `Data Source=mydb.db`) | Startup fails with a clear error per FR-008. |
| `Database:ConnectionString` override is a non-empty string that is neither a valid connection string nor an absolute path (e.g. random text) | Startup fails with a clear error identifying the key and the value type ambiguity. |
| `Database:ConnectionString` override is an absolute path to a directory rather than a file, or to a non-writable location | Startup fails when EF Core attempts `EnsureCreatedAsync`. The host may, at design discretion, preflight the parent directory. |
| `appsettings.json` accidentally re-introduces a relative `Data Source=...` value | Same fail-fast behavior as FR-008; no special-casing of `appsettings.json`. |
| Manual smoke M-01 cannot start (Ollama not available where M-01 expects it) | Operator records as anomaly (FR-014); not a code failure. |
| Manual smoke M-04 succeeds visually but Ollama produces a non-error response (e.g. cached) | Operator records observed behavior; classification is operator's judgment per FR-013. |
| Manual smoke uncovers Avatar timer regression similar to historical P1-001 | Operator records anomaly; spec does not authorize a code fix in this iteration. A separate `/spec` follow-up is required. |
| Database file at the new location is locked (another Iris instance running) | Existing EF Core / SQLite locking behavior surfaces. No new handling required. |

## 11. Testing Requirements

### 11.1 Automated Tests (must pass; no regression of existing 126)

- **T-PR-01.** Host path-resolution unit/integration test: given no `Database:ConnectionString` configured, the path provider returns a connection string whose `Data Source` is an absolute path under `ApplicationData/Iris/iris.db` (assertion may use `Path.IsPathRooted` and ends-with `iris.db`; absolute equality not required to keep tests portable).

- **T-PR-02.** Given `Database:ConnectionString` set to a full connection string with an **absolute** `Data Source`, the resolved value is exactly that connection string verbatim.

- **T-PR-03.** Given `Database:ConnectionString` set to a bare absolute file path, the resolved value normalizes to `Data Source=<that-path>`.

- **T-PR-04.** Given `Database:ConnectionString` set to a bare relative path (`iris.db`, `./data/iris.db`, `..\foo.db`), the resolution throws (or `AddIrisDesktop` throws) with an error message that identifies `Database:ConnectionString` as the offending key. (FR-008.)

- **T-PR-05.** Given `Database:ConnectionString` set to a connection string whose `Data Source` component is relative, the resolution throws with the same kind of error. (FR-008.)

- **T-PR-06.** The default-resolved directory is created if missing (test against a temp `ApplicationData`-equivalent or via injectable directory base — implementation latitude allowed; spec only requires that the test verifies creation behavior on a writable temp location, not the real user `%APPDATA%`).

- **T-PR-07.** Architecture/host-isolation: no new type added by this iteration is referenced from `Iris.Application`, `Iris.Domain`, `Iris.Shared`, or any adapter project. (Existing `Iris.Architecture.Tests` patterns may be reused or extended.)

- **T-PR-08.** Existing `IrisDatabaseInitializerTests` and `PersistenceTestContextFactory`-based tests continue to pass unchanged. (`Iris.Persistence` contract is unmodified.)

- **T-PR-09.** Existing 20 `AvatarViewModelTests` (including T-16 cancellation token test) continue to pass.

### 11.2 Manual Smoke (must be executed and recorded)

The seven scenarios from `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11, executed against the build produced after path stabilization is implemented. Restated for completeness:

- **M-01.** Launch Desktop with Ollama running. **Expected:** Avatar visible, `Idle`. Window opens, ChatView visible, AvatarPanel overlay visible per `Position`/`Size` defaults. Database file is at the new per-user location (operator verifies path).
- **M-02.** Send a message. **Expected:** Avatar transitions to `Thinking` for the duration of the send.
- **M-03.** Wait for response. **Expected:** Avatar briefly shows `Success`, then returns to `Idle` after `SuccessDisplayDurationSeconds` (default 2.0 s).
- **M-04.** Stop Ollama, send a message. **Expected:** Avatar shows `Thinking`, then `Error`. Error visible in chat UI per Phase 5 error mapping.
- **M-05.** Set `Desktop:Avatar:Enabled = false` in `appsettings.local.json`, restart. **Expected:** Avatar not displayed; chat continues to function.
- **M-06.** Set `Desktop:Avatar:Size = "Large"`, restart. **Expected:** Avatar visible area increases to Large (180×180).
- **M-07.** Set `Desktop:Avatar:Position = "TopLeft"`, restart. **Expected:** Avatar moves to top-left corner.

Recording requirements per FR-011 through FR-015 apply.

### 11.3 Required Checks

The following must pass before the smoke run is started:

```
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx --no-restore
dotnet format .\Iris.slnx --verify-no-changes
```

Existing 126 tests + new path-provider tests (T-PR-01 … T-PR-07) must all pass. No deletions or weakening of existing tests.

## 12. Documentation and Memory Requirements

After implementation and after manual smoke:

- **`.agent/PROJECT_LOG.md`** — append a dated entry covering: path-stabilization work (files changed, before/after default behavior, verification commands and results), and manual smoke outcomes (per-scenario pass/fail/anomaly).
- **`.agent/overview.md`** — update Current Phase / Current Working Status / Next Immediate Step / Known Blockers to remove M-01–M-07 once they are recorded as passed.
- **`.agent/debt_tech_backlog.md`** — close the "Desktop SQLite path uses relative working directory" debt entry (mark resolved, dated; do not delete history).
- **`.agent/log_notes.md`** — append entries only if anomalies were observed; one entry per anomaly with reproduction notes and severity.

Documentation updates **not** required:

- `AGENTS.md`, `.opencode/rules/*`, `.opencode/skills/*` — unchanged.
- `docs/architecture.md`, `.agent/architecture.md` — Avatar/path stabilization does not change architecture diagrams.
- `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` — frozen artifact; not modified.
- `docs/designs/2026-04-30-phase-5-5-avatar-v1.design.md` — frozen artifact; not modified.

A new spec/design/plan/audit cycle artifact for **this** iteration belongs in `docs/specs/`, `docs/designs/`, `docs/plans/`, `docs/audits/` only when the user explicitly requests `/save-spec`, `/save-design`, `/save-plan`, `/save-audit`. This document is a `/spec` output and is not auto-saved.

## 13. Acceptance Criteria

- [ ] `dotnet build .\Iris.slnx` passes with 0 errors and 0 warnings.
- [ ] `dotnet test .\Iris.slnx --no-restore` passes; existing 126 tests + new path-provider tests all pass.
- [ ] `dotnet format .\Iris.slnx --verify-no-changes` passes.
- [ ] Launching Iris Desktop without `appsettings.local.json` produces (or uses) a SQLite database at `<ApplicationData>/Iris/iris.db` regardless of the working directory the process was started from. Verified by operator.
- [ ] Launching with `Database:ConnectionString` set in `appsettings.local.json` to an absolute path uses exactly that path. Verified by operator.
- [ ] Launching with a relative `Database:ConnectionString` (in any layered config) fails fast with a clear error message naming the offending key.
- [ ] No source change in `Iris.Application`, `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Infrastructure`. Verified by `git diff` scope check.
- [ ] No new NuGet package added to any project.
- [ ] No new project reference added to `Iris.Desktop.csproj`.
- [ ] Manual smoke M-01–M-07 executed against the built Desktop with the new default path active. Per-scenario pass/fail/anomaly recorded in `.agent/PROJECT_LOG.md` referencing the branch and commit SHA used.
- [ ] If any anomaly observed, an entry exists in `.agent/log_notes.md` with reproduction details and severity assessment.
- [ ] `.agent/overview.md` updated: M-01–M-07 no longer in Known Blockers; SQLite path debt no longer cited as next step; next step is `/audit`.
- [ ] `.agent/debt_tech_backlog.md`: "Desktop SQLite path uses relative working directory" marked resolved with date, link to the PROJECT_LOG entry, and the resolved default location.
- [ ] No memory file is overwritten wholesale; appends/in-place edits only.
- [ ] No commit, tag, push, or branch operation is executed by the implementing agent without an explicit user request.

## 14. Open Questions

No blocking open questions.

The following are deliberately **left as Design decisions**, not specification ambiguities, and must be resolved in `/design`:

- Exact shape of the host-side path provider (free function, static helper, sealed class with interface, etc.). Spec only constrains ownership (Desktop) and absence of new packages (AC-004).
- Whether `appsettings.json` omits `Database:ConnectionString` entirely or carries an explicit sentinel (`""`, `null`, or a documented marker like `"USE_DEFAULT"`). Spec only constrains that the committed default must not produce a relative-CWD database.
- Whether to ship a tracked `appsettings.local.example.json` documenting the override format. Spec only requires that the override mechanism be documented somewhere accessible; the design may choose example-file vs README vs spec section.
- Whether T-PR-06 uses an injectable directory base (e.g. wrapping `Environment.SpecialFolder.ApplicationData` resolution behind a small Desktop-internal seam) or directly tests under a real temp directory by passing an absolute override. Both satisfy FR-001 / T-PR-01.

These are noted here so the `/design` stage knows it owns them.

## 15. Assumptions

- The user wants both tasks (path stabilization and M-01–M-07) handled in one iteration, with path stabilization preceding manual smoke (per `/plan` choice in the prior conversation turn).
- M-01–M-07 will be executed by the human user (not by an automated agent), and the user accepts that this iteration cannot self-certify those scenarios.
- The current branch `feat/avatar-v1-and-opencode-v2` is the correct integration target for this work; no new feature branch is mandated by the spec.
- `Environment.SpecialFolder.ApplicationData` returning a usable path on Windows (the developer's primary platform per repo evidence) is sufficient for v1. Linux/macOS work is not part of this iteration's acceptance, but the implementation must not actively break those platforms (FR-002 requires the same primitive).
- `Iris.Api` and `Iris.Worker` hosts are not actively launched against the same database file in development; if they later are, a separate spec defines their path strategy.
- The current 126 tests include 20 AvatarViewModelTests and the architecture/integration suites listed in `.agent/PROJECT_LOG.md`. The new path-provider tests increase the total but do not replace any of them.
- No existing user-data SQLite database needs migration; the project is pre-production.

## 16. Blocking Questions

No blocking questions.
