# Architecture Design: Iris Default Response Language is Russian

## 1. Design Goal

Spec `docs/specs/2026-05-01-iris-default-language-russian.spec.md` requires Iris to respond in Russian by default, with technical tokens preserved in English, and to record this requirement durably in `mem_library`.

This design defines **how** that requirement is implemented inside `Iris.Application` without breaking existing contracts, without adding new dependencies, and without inventing infrastructure that the project does not yet need. The design picks the smallest seam that satisfies FR-001..FR-008 and AC-001..AC-008, follows the established Application-layer conventions (plain options POCO, `public sealed`, no `IOptions<>` binding), and leaves room for the future persona slice without forcing it now.

## 2. Specification Traceability

Spec source: `docs/specs/2026-05-01-iris-default-language-russian.spec.md`.

| Spec item | Addressed by design section |
|---|---|
| FR-001 (Russian by default) | §6 `RussianDefaultLanguagePolicy`, §8 Primary Flow |
| FR-002 (no English baseline in default config) | §6 `PromptBuilder`, §7 `ILanguagePolicy` contract |
| FR-003 (preserve technical tokens) | §6 `LanguageInstructionBuilder` text rules |
| FR-004 (code/comments in English, prose in Russian) | §6 `LanguageInstructionBuilder` text rules |
| FR-005 (no language mirroring) | §10 invariant, §7 contract, §8 flow |
| FR-006 (`LanguageOptions` + Persona:Language section) | §11 Configuration |
| FR-007 (works with no config) | §11 default values, §10 fallback |
| FR-008 (mem_library §21) | §13 Testing & §16 Acceptance Mapping (memory deferred to `/update-memory`) |
| AC-001..AC-004 (Application-only, no new refs/packages) | §3, §11 |
| AC-005 visibility (`internal sealed`) | **Adjusted** — see §15 R2; existing Chat/* convention is `public sealed`, design follows project convention |
| AC-006 (`PromptBuilder` keeps `Build(...)` shape, accepts `ILanguagePolicy` via ctor) | §7 |
| AC-007 (Architecture tests unchanged) | §6 namespace placement, §11 references |
| AC-008 (no broader persona wiring) | §3 explicit out-of-scope |

## 3. Current Architecture Context

Verified by reconnaissance against `e:\Work\Iris`:

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs:7` — `public sealed class PromptBuilder` with **parameterless** ctor, hardcoded `private const string _baselineSystemPrompt`. This is the only system-prompt source in the system.
- `src/Iris.Application/DependencyInjection.cs` — single public extension `AddIrisApplication(this IServiceCollection, SendMessageOptions)`. **No `IConfiguration`, no `IOptions<>`, no `Configure<>`**. Options are plain POCOs passed by hosts. `ArgumentNullException.ThrowIfNull` + invariant validation in the extension itself.
- `src/Iris.Application/Chat/SendMessage/SendMessageOptions.cs` — `public sealed record SendMessageOptions(int MaxMessageLength);` — establishes the project's options idiom.
- All `Iris.Application/Chat/*` types are `public sealed class` or `public sealed record`. No `InternalsVisibleTo` is configured for `Iris.Application`.
- `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs:13` — constructs `new PromptBuilder()` directly. Will break on compile when ctor signature changes.
- `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs:320` — same direct ctor in `CreateHandler` helper (used by 14 tests). Will break on compile.
- `tests/Iris.Application.Tests/DependencyInjectionTests.cs` — three tests pinned to `AddIrisApplication(SendMessageOptions)`. Will break if signature changes.
- `src/Iris.Desktop/DependencyInjection.cs:54` — single host call site `services.AddIrisApplication(new SendMessageOptions(maxMessageLength));`. Reads `Database`, `ModelGateway:Ollama:*`, `Desktop:Avatar:*` from `IConfiguration`; precedent for adding `Persona:Language:*` with the same idiom.
- `Iris.Application/Persona/{Style, Mood, Context, Policies, Relationship, State}` exists as a slice but is **not consumed by `PromptBuilder`** — no namespace `Iris.Application.Persona.Language` exists today.
- `src/Iris.Application/Chat/Prompting/PromptTemplateProvider.cs` — empty stub `internal class { }`, dead code in namespace `Iris.Application.Chat.BuildPrompt` (typo). Not referenced. Out of scope to modify, but flagged in §15 risks.

Architecture invariants currently honored that this design must not break:

- `Iris.Application` has no reference to any adapter, host, or `Microsoft.EntityFrameworkCore`.
- No `Iris.Application` type uses `HttpClient` or provider SDKs.
- Architecture tests in `tests/Iris.Architecture.Tests/*` enforce these directions.

## 4. Proposed Design Summary

A new namespace `Iris.Application.Persona.Language` hosts four `public sealed` types:

1. **`LanguageOptions`** — POCO record holding `DefaultLanguage` (string, default `"ru"`). Same idiom as `SendMessageOptions`.
2. **`ILanguagePolicy`** — interface with one method that returns the system-prompt language preamble for the current options. Single seam, deliberately minimal.
3. **`RussianDefaultLanguagePolicy`** — implementation. Reads `LanguageOptions`, delegates text composition to `LanguageInstructionBuilder`, applies fallback rules.
4. **`LanguageInstructionBuilder`** — pure text composer that holds the canonical Russian baseline text as `private const`. No state, no I/O, no DI dependencies.

`PromptBuilder` gains one constructor parameter `ILanguagePolicy`. Its `Build(...)` calls `_languagePolicy.GetSystemPrompt()` instead of using the deleted `_baselineSystemPrompt` constant. Public method shape unchanged.

`AddIrisApplication` extension takes one new positional parameter `LanguageOptions languageOptions` (matching the existing `SendMessageOptions` parameter idiom) and registers `LanguageOptions` + `ILanguagePolicy → RussianDefaultLanguagePolicy` as singletons. Hosts pass `new LanguageOptions(...)` constructed from configuration, mirroring how Desktop already constructs `SendMessageOptions`.

`mem_library/03_iris_persona.md` gets a new §21 "Default Language" appended through `/update-memory` after implementation. This is **not** a code change — design records the intent, the implementation phase merely keeps the spec accountable.

`PromptTemplateProvider.cs` (the empty stub in the wrong namespace) is left untouched in this design to avoid scope creep; design records it as observed dead code (§15) for a separate cleanup ticket.

## 5. Responsibility Ownership

| Responsibility | Owner | Notes |
|---|---|---|
| Composing the system-prompt language preamble text | `LanguageInstructionBuilder` (Application) | Pure function over `LanguageOptions`. No I/O. |
| Applying language fallback rules ("ru" hardcoded as final fallback) | `RussianDefaultLanguagePolicy` (Application) | Resolves null/empty/unknown to `"ru"`. |
| Exposing the language preamble as a stable seam | `ILanguagePolicy` (Application) | Single method contract. Stable for future persona wiring. |
| Holding language configuration values | `LanguageOptions` (Application) | POCO, no validation logic — defaults are baked in. |
| Constructing `LanguageOptions` from `appsettings*.json` | `Iris.Desktop/DependencyInjection` (host) | Same pattern as existing `SendMessageOptions(maxMessageLength)`. API/Worker hosts will follow when they exist. |
| Inserting the system prompt into `ChatModelRequest` | `PromptBuilder` (Application) | Keeps existing role: composes the message list. Now obtains the system text via injected port. |
| Persona-level intent of "Russian default" | `mem_library/03_iris_persona.md` §21 | Memory write deferred to `/update-memory`. Code references this intent in XML doc comments. |

No responsibility crosses a layer boundary. No host owns prompt logic. No adapter is involved.

## 6. Component Design

### `LanguageOptions`

- Owner layer: `Iris.Application` — namespace `Iris.Application.Persona.Language`.
- Responsibility: carry language configuration values to `RussianDefaultLanguagePolicy`.
- Inputs: constructed by host with a `DefaultLanguage` string.
- Outputs: read-only properties.
- Collaborators: `RussianDefaultLanguagePolicy`, `AddIrisApplication`.
- Must not do: validate values (validation lives in policy fallback), perform I/O, depend on `IConfiguration`.
- Notes: matches `SendMessageOptions` idiom (`public sealed record` with primary ctor and a `static` Default factory). The default factory removes the need for hosts to know the canonical string `"ru"`.

Illustrative shape:

```csharp
public sealed record LanguageOptions(string DefaultLanguage)
{
    public static LanguageOptions Default { get; } = new("ru");
}
```

### `ILanguagePolicy`

- Owner layer: `Iris.Application` — namespace `Iris.Application.Persona.Language`.
- Responsibility: produce the system-prompt language preamble text.
- Inputs: none at call time (policy is constructed with `LanguageOptions`).
- Outputs: a non-empty string.
- Collaborators: `PromptBuilder` (consumer), `RussianDefaultLanguagePolicy` (default implementation).
- Must not do: take `CancellationToken` (synchronous, in-process, no I/O), accept request-time language overrides (FR-005 forbids per-request switching), throw exceptions for fallback-eligible inputs.
- Notes: deliberately the smallest contract. A future `PersonaContextBuilder` integration may extend with a richer `BuildSystemPrompt(PersonaContext)` overload **without** breaking this seam, by adding a new method rather than changing the existing one.

```csharp
public interface ILanguagePolicy
{
    string GetSystemPrompt();
}
```

### `RussianDefaultLanguagePolicy`

- Owner layer: `Iris.Application` — namespace `Iris.Application.Persona.Language`.
- Responsibility: implement `ILanguagePolicy` by selecting the right preamble for the configured language with a Russian fallback.
- Inputs: `LanguageOptions` injected via ctor; `LanguageInstructionBuilder` injected via ctor.
- Outputs: a non-empty string from `GetSystemPrompt()`.
- Collaborators: `LanguageInstructionBuilder`.
- Must not do: read configuration directly, cache state across instances (DI lifetime is singleton; no per-call mutability), short-circuit non-Russian languages by returning English.
- Notes: per spec §10, only `"ru"` (case-insensitive) maps to the Russian baseline; everything else (null/empty/whitespace/`"en"`/`"jp"`) also maps to the Russian baseline. This is intentional: Russian is a hard default.

### `LanguageInstructionBuilder`

- Owner layer: `Iris.Application` — namespace `Iris.Application.Persona.Language`.
- Responsibility: hold and return the canonical Russian baseline text.
- Inputs: a normalized language code passed by `RussianDefaultLanguagePolicy`.
- Outputs: a non-empty string.
- Collaborators: `RussianDefaultLanguagePolicy`.
- Must not do: read files/resources/config, do string concatenation per call (text is a `private const`), localize at runtime.
- Notes: Russian text lives here as `private const` per Q9. Single source of truth for the canonical baseline. Existing `mem_library/03_iris_persona.md` §18 baseline text is the **product intent**, not the implementation; this builder is the implementation.

The exact Russian text is a `private const` chosen during implementation, not in this design. The design constrains its required content (per FR-001..FR-005):

1. Identify Iris (calm, local, personal companion — preserves §18 persona baseline meaning).
2. Instruct the model to respond in Russian.
3. Instruct that file names, paths, namespaces, type names, identifiers, CLI commands, and code blocks remain in their original English form.
4. Instruct that code blocks and code comments stay in English while prose around them stays in Russian.
5. Instruct that the answer language must remain Russian even if the operator's input is in another language.

### `PromptBuilder` (modified)

- Owner layer: `Iris.Application` — namespace `Iris.Application.Chat.Prompting`. Unchanged.
- Responsibility: assemble `ChatModelRequest` from history + current user message + system preamble. Unchanged.
- Inputs: existing `PromptBuildRequest`. Constructor now takes `ILanguagePolicy languagePolicy`.
- Outputs: existing `Result<PromptBuildResult>`.
- Collaborators: `ILanguagePolicy` (new), existing `Message` mapping.
- Must not do: hardcode the system prompt, query options directly, branch on language at call time.
- Notes: the deleted `private const string _baselineSystemPrompt` is the only behavior change. The `messages.Add` for User/Assistant history is unchanged.

### `AddIrisApplication` extension (modified)

- Owner layer: `Iris.Application`. Unchanged location.
- Responsibility: register Application services for hosts.
- Inputs: now `(IServiceCollection services, SendMessageOptions sendMessageOptions, LanguageOptions languageOptions)`.
- Outputs: `IServiceCollection` for chaining. Unchanged.
- Collaborators: hosts (`Iris.Desktop/DependencyInjection.cs`, future Api/Worker).
- Must not do: read `IConfiguration` directly (responsibility of hosts), bind via `IOptions<>` (project does not use that pattern).
- Notes: signature change is mechanical and traceable. Default convenience overload `AddIrisApplication(services, sendMessageOptions)` is **not** added — explicit options force hosts to make a deliberate choice and keep the invariant that Application options are always supplied by the host. Hosts that want defaults pass `LanguageOptions.Default`.

## 7. Contract Design

### `ILanguagePolicy` (new)

- Owner: `Iris.Application.Persona.Language`.
- Consumers: `PromptBuilder` (only initial consumer; future `PersonaContextBuilder` may consume).
- Shape: single method `string GetSystemPrompt()`. Returns non-empty string.
- Compatibility: new contract. No previous version.
- Error behavior: throws no exceptions for valid `LanguageOptions`. Fallback handled internally (see §10).
- Stability: stable. Future extensions add new methods, do not change this one.

### `LanguageOptions` (new)

- Owner: `Iris.Application.Persona.Language`.
- Consumers: `RussianDefaultLanguagePolicy`, `AddIrisApplication`, hosts.
- Shape: `public sealed record LanguageOptions(string DefaultLanguage)` with `static LanguageOptions Default`.
- Compatibility: new type. Hosts that did not exist (Api/Worker) will adopt without burden; existing Desktop host adopts with one new line.
- Error behavior: no validation in record. `RussianDefaultLanguagePolicy` handles invalid values via fallback.
- Stability: properties may be added later (e.g., `bool MirrorOperatorLanguage`); existing callers unaffected because of record copy semantics.

### `PromptBuilder.Build(PromptBuildRequest)` (modified behavior)

- Owner: `Iris.Application.Chat.Prompting`.
- Consumers: `SendMessageHandler`.
- Shape: signature unchanged — `Result<PromptBuildResult> Build(PromptBuildRequest request)`.
- Compatibility: behavior change. The first message's `Content` is now policy-derived. Tests must be updated to reflect this; production callers (only `SendMessageHandler`) need no change.
- Error behavior: unchanged — wraps in `Result<>` per existing convention.
- Stability: stable.

### `PromptBuilder` constructor (changed)

- Owner: `Iris.Application.Chat.Prompting`.
- Shape: `public PromptBuilder(ILanguagePolicy languagePolicy)`.
- Compatibility: **breaking** for direct callers. Direct callers in this repo: `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs:13` and `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs:320`. Both are in scope for update.
- Error behavior: throws `ArgumentNullException` on null per project convention.
- Stability: stable after this change.

### `AddIrisApplication` extension (changed)

- Owner: `Iris.Application`.
- Shape: `public static IServiceCollection AddIrisApplication(this IServiceCollection services, SendMessageOptions sendMessageOptions, LanguageOptions languageOptions)`.
- Compatibility: **breaking** for the host call site `src/Iris.Desktop/DependencyInjection.cs:54` and three tests in `DependencyInjectionTests.cs`. All in scope.
- Error behavior: extends existing `ArgumentNullException.ThrowIfNull` block. No validation on `LanguageOptions.DefaultLanguage` — fallback is the policy's job.
- Stability: stable.

### Out of contract scope

- `Iris.Application.DependencyInjection.AddIrisApplication` does **not** start binding from `IConfiguration`. Hosts continue to construct options. Future API/Worker hosts will follow.
- `IConfiguration` keys for hosts: `Persona:Language:DefaultLanguage` (string). Adding the section is optional in `appsettings.json`; absence yields `LanguageOptions.Default`.

## 8. Data Flow

### Primary Flow

1. **Application startup (host).**
   - `Iris.Desktop` loads `IConfiguration` (`appsettings.json` + `appsettings.local.json`).
   - Host reads `Persona:Language:DefaultLanguage` (string, may be null).
   - Host constructs `LanguageOptions`: if the string is `null`, host passes `LanguageOptions.Default`; otherwise `new LanguageOptions(value)`.
   - Host calls `services.AddIrisApplication(new SendMessageOptions(maxMessageLength), languageOptions)`.
2. **DI registration (`AddIrisApplication`).**
   - Validates non-null on both options records.
   - Registers `LanguageOptions` as singleton.
   - Registers `LanguageInstructionBuilder` as singleton.
   - Registers `RussianDefaultLanguagePolicy` as `ILanguagePolicy` singleton.
   - Registers `PromptBuilder` as singleton (already singleton in current code).
3. **Request time (per chat send).**
   - `SendMessageHandler` resolves `PromptBuilder` (existing).
   - `SendMessageHandler` calls `PromptBuilder.Build(...)` with history and current user message (existing).
   - `PromptBuilder` calls `_languagePolicy.GetSystemPrompt()` once.
   - `RussianDefaultLanguagePolicy` reads its injected `LanguageOptions`, normalizes the language code (trim + invariant lower), and asks `LanguageInstructionBuilder` for the canonical text. If the normalized code is anything other than `"ru"`, the builder returns the Russian baseline anyway (hard default).
   - `PromptBuilder` constructs `messages = [System(prompt), ...history, User(current)]` and returns `Result<PromptBuildResult>.Success(...)`.
4. **Downstream (unchanged).**
   - `SendMessageHandler` passes the `ChatModelRequest` to `IChatModelClient` (ModelGateway adapter).
   - Ollama receives a Russian-language system message and a possibly-mixed-language user message.
   - Ollama responds — the system message's language is the dominant signal (per Q8 design rationale and `llama3:latest` behavior).

### Error / Alternative Flows

- **`LanguageOptions.DefaultLanguage` is null/empty/whitespace.** `RussianDefaultLanguagePolicy` normalizes to `"ru"` and proceeds. No exception, no log entry, no degraded mode.
- **`LanguageOptions.DefaultLanguage` is an unknown value (`"jp"`, `"en"`, `"xyz"`).** Same behavior as null: hard fallback to Russian baseline. This is intentional per spec §10 — accidental misconfiguration must not silently revert to English.
- **`ILanguagePolicy` not registered in DI.** `PromptBuilder` constructor cannot resolve and DI throws at root-resolve time. Standard fail-fast — no silent fallback. This surfaces the misconfiguration immediately at host startup.
- **Operator sends an English-language message.** Flow is identical. The user message is forwarded as-is (technical tokens, English quotes, etc. are preserved). The system message remains Russian. The model is expected to respond in Russian per its instruction.
- **Model returns English anyway.** Outside the design's correctness boundary. Recorded as N1 risk in §15 — manual verification M-LANG-01..M-LANG-03 is the gate.

## 9. Data and State Design

No persisted data, no schema, no migrations, no identifiers, no session state.

In-memory state:

- `LanguageOptions` is registered as singleton — one instance for the application lifetime.
- `LanguageInstructionBuilder` is stateless — singleton is safe.
- `RussianDefaultLanguagePolicy` is stateless given its singleton dependencies — singleton is safe.
- `PromptBuilder` is already singleton in current code — unchanged.

There is no caching beyond DI singletons. The Russian baseline text is a `private const` resolved at JIT time.

## 10. Error Handling and Failure Modes

| Failure mode | Detection | Behavior |
|---|---|---|
| `LanguageOptions == null` in DI registration | `ArgumentNullException.ThrowIfNull` in `AddIrisApplication` | Host startup fails fast with clear stack. |
| `LanguageOptions.DefaultLanguage` null/empty/whitespace | `RussianDefaultLanguagePolicy` normalize step | Fallback to `"ru"`, baseline returned. No log. |
| `LanguageOptions.DefaultLanguage` unknown value | `RussianDefaultLanguagePolicy` normalize step | Fallback to `"ru"`, baseline returned. No log. |
| `ILanguagePolicy` missing from DI | DI resolve at startup | Standard `InvalidOperationException` from container. Surfaces at host startup. |
| `LanguageInstructionBuilder` returns empty string | Cannot happen — `private const` | N/A; defensive assert in tests T-LANG-04. |
| Model ignores Russian instruction | Manual smoke M-LANG-01..M-LANG-03 | Out of scope for code; design risk N1. |

Cancellation: `GetSystemPrompt()` is synchronous and returns instantly. No `CancellationToken` is needed and adding one would complicate a pure-string operation. `PromptBuilder.Build` is also synchronous (current state) — unchanged.

Logging: no log calls are added in this design. Application currently does not log inside `PromptBuilder`. Consistency is preserved.

## 11. Configuration and Dependency Injection

### Application-side registration (modified `AddIrisApplication`)

```csharp
public static IServiceCollection AddIrisApplication(
    this IServiceCollection services,
    SendMessageOptions sendMessageOptions,
    LanguageOptions languageOptions)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(sendMessageOptions);
    ArgumentNullException.ThrowIfNull(languageOptions);

    if (sendMessageOptions.MaxMessageLength <= 0) { /* existing */ }

    services.AddSingleton(sendMessageOptions);
    services.AddSingleton(languageOptions);
    services.AddSingleton<IClock, SystemClock>();
    services.AddSingleton<SendMessageValidator>();
    services.AddSingleton<LanguageInstructionBuilder>();
    services.AddSingleton<ILanguagePolicy, RussianDefaultLanguagePolicy>();
    services.AddSingleton<PromptBuilder>();
    services.AddScoped<SendMessageHandler>();

    return services;
}
```

### Host-side (Desktop)

```csharp
// in Iris.Desktop/DependencyInjection.cs, replacing line 54:
var configuredLanguage = configuration.GetValue<string?>("Persona:Language:DefaultLanguage");
var languageOptions = string.IsNullOrWhiteSpace(configuredLanguage)
    ? LanguageOptions.Default
    : new LanguageOptions(configuredLanguage);

services.AddIrisApplication(new SendMessageOptions(maxMessageLength), languageOptions);
```

This follows the exact precedent set by `Database:ConnectionString` and `Desktop:Avatar:*` reads earlier in the same file.

### Configuration files

- `src/Iris.Desktop/appsettings.json`: **not modified** by this design. Defaults are sufficient; no need to advertise an empty `Persona:Language` block.
- `src/Iris.Desktop/appsettings.local.example.json`: design **recommends** adding a commented example, but does not require it. The plan phase decides.
- `src/Iris.Api`, `src/Iris.Worker`: not touched in this design. Future hosts will adopt the same idiom when they exist.

### DI lifetime rationale

| Type | Lifetime | Reason |
|---|---|---|
| `LanguageOptions` | Singleton | Plain options POCO, matches `SendMessageOptions`. |
| `LanguageInstructionBuilder` | Singleton | Stateless, no allocations beyond `const`. |
| `ILanguagePolicy` → `RussianDefaultLanguagePolicy` | Singleton | Stateless given singleton dependencies. |
| `PromptBuilder` | Singleton | Existing; no per-request state. |

## 12. Security and Permission Considerations

- No secrets are introduced. `LanguageOptions.DefaultLanguage` is a non-sensitive language code.
- No new I/O, no new HTTP/file/process access, no new permission decisions, no new user data flow.
- The Russian baseline text is a static, audited string — it does not embed user data, secrets, or PII.
- Ollama receives the same body shape as today, only with a different system message string. No exfiltration surface change.
- Memory write to `mem_library/03_iris_persona.md` (§21) does not record secrets — it records persona/language intent.
- Logging surface is unchanged; no new log fields can leak data.

## 13. Testing Design

All tests live in `tests/Iris.Application.Tests/` (existing project, namespace `Iris.Application.Tests.*`). The spec's reference to `Iris.Application.UnitTests` was a naming slip — the **actual** project is `Iris.Application.Tests`. Plan must use the real name.

### Unit tests (xUnit)

New test classes (each in its own file under `tests/Iris.Application.Tests/Persona/Language/`):

- **`RussianDefaultLanguagePolicyTests`**:
  - **T-LANG-01** When `LanguageOptions("ru")`, `GetSystemPrompt()` returns a non-empty string.
  - **T-LANG-02** When `LanguageOptions(null!)` / empty / whitespace, `GetSystemPrompt()` returns the same Russian baseline as T-LANG-01.
  - **T-LANG-03** When `LanguageOptions("jp")` / `"en"` / `"xyz"`, returns the same Russian baseline as T-LANG-01.

- **`LanguageInstructionBuilderTests`**:
  - **T-LANG-04** Returned text is non-empty and contains a Cyrillic character (smoke check that text is in Russian, language-detection-free).
  - **T-LANG-05** Returned text contains literal phrase markers proving the technical-token rule (e.g., contains the words `файл`, `путь`, `идентификатор`, or `команд` — exact tokens decided in implementation; design fixes the requirement, not the wording).
  - **T-LANG-06** Returned text contains literal phrase markers proving the code-stays-in-English rule.

  Test strategy note: assertions check **content categories** (presence of meaningful keywords), not the verbatim string. This isolates the test from cosmetic wording changes during implementation.

Modified test classes:

- **`PromptBuilderTests`** (existing):
  - **T-LANG-07** New test: `Build(...)` first message content equals the string returned by an injected stub `ILanguagePolicy`, proving that `PromptBuilder` no longer hardcodes the prompt.
  - **T-LANG-08** New test: with the **default** `RussianDefaultLanguagePolicy(LanguageOptions.Default)`, the first message's `Content` does NOT equal the legacy English baseline literal `"You are Iris, a local personal AI companion. Be helpful, clear, and respectful."` — locks the regression.
  - **T-LANG-09** Existing test `Build_IncludesSystemMessageHistoryAndCurrentUserMessage` is updated: still asserts the role/content shape (System/User/Assistant/User), but the system content assertion is relaxed from "non-empty" to "equals stub policy output". History mapping remains validated.

- **`SendMessageHandlerTests`** (existing): `CreateHandler` helper is updated to pass a stub `ILanguagePolicy` to `new PromptBuilder(...)`. **No assertion changes** — these tests do not assert the system prompt text, they assert handler orchestration. 14 tests recompile and pass.

- **`DependencyInjectionTests`** (existing):
  - The two existing tests gain a `LanguageOptions.Default` argument in their `AddIrisApplication` call. No new assertions in those tests.
  - **T-LANG-10** (new) `AddIrisApplication_WithNullLanguageOptions_Throws` — mirrors existing null-options pattern.
  - **T-LANG-11** (new) `AddIrisApplication_RegistersILanguagePolicy` — resolves `ILanguagePolicy` from the provider.

### Architecture tests

No new tests. Existing `tests/Iris.Architecture.Tests/*` must continue to pass without modification:

- Application has no new project reference.
- No new namespace touches forbidden adapters.
- New namespace `Iris.Application.Persona.Language` falls under the existing `Iris.Application.*` rules.

### Integration tests

No new integration tests. The chat pipeline is exercised by existing handler tests; the change is in prompt content, which is unit-testable.

### Manual verification

Per spec §11: M-LANG-01 (Russian input → Russian response), M-LANG-02 (English input → Russian response), M-LANG-03 (code question → Russian prose + English code). Operator-driven against `llama3:latest` via Ollama.

### Verification commands before readiness

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

Test count delta: existing tests recompile, +6 new (T-LANG-01..T-LANG-04..T-LANG-06, T-LANG-07, T-LANG-08, T-LANG-10, T-LANG-11). Spec count target "previous +T-LANG-*" remains satisfied.

## 14. Options Considered

### Option A — `LanguagePolicy` as proposed (chosen)

Plain interface + concrete + builder + options POCO. Singleton DI. Construction-injected. Russian text as `private const`.

- **Pros:** matches existing Application idiom; smallest reasonable footprint that still tests well; future-extensible by adding methods to `ILanguagePolicy`; no new packages; no new patterns.
- **Cons:** four files for what is effectively one configurable string. Slight ceremony.
- **Recommendation:** proceed.

### Option B — Hardcode Russian text in `PromptBuilder._baselineSystemPrompt`

Simplest: just change the string from English to Russian.

- **Pros:** one-line diff; minimal blast radius; trivial test update.
- **Cons:** rejected by user in Q7 explicitly. Loses the configurable seam, makes future persona wiring re-introduce the same change. Couples persona/language concerns to `PromptBuilder` permanently. No way to override without rebuild.
- **Recommendation:** rejected.

### Option C — Use `IOptions<LanguageOptions>` binding from `IConfiguration`

Standard ASP.NET-style configuration.

- **Pros:** more idiomatic in some .NET projects; live-reload possible.
- **Cons:** **`Iris.Application` does not currently use this pattern** — adopting it would set a precedent affecting all future Application options and force a parallel rework of `SendMessageOptions`. Live-reload is a non-goal. Adds `Microsoft.Extensions.Options.ConfigurationExtensions` dependency to `Iris.Application` (currently unreferenced).
- **Recommendation:** rejected for this scope; revisit only if the project decides to migrate options conventions globally.

### Option D — Wire the full persona slice (`PersonaContextBuilder` → `PromptBuilder`)

Per spec §3 out-of-scope, but considered for completeness.

- **Pros:** would also satisfy the language requirement as a side effect.
- **Cons:** explicitly out of scope; would balloon implementation, require persona policy work that has its own pending design phase, and risk landing language as part of a bigger change that is harder to revert.
- **Recommendation:** rejected for this work; recorded in `debt_tech_backlog.md` per spec §12.

### Option E — Few-shot Russian example in system prompt

Add a synthetic user/assistant pair in Russian to the message list before the real history.

- **Pros:** stronger language signal for weaker models.
- **Cons:** spec N1 explicitly defers this to a tactical adjustment if M-LANG-* fails; adds ~2 message slots eating context budget; complicates `PromptBuilder` flow.
- **Recommendation:** not in design v1. Reserve as a fallback if manual smoke fails.

## 15. Risks and Trade-offs

- **R1 — `llama3:latest` Russian instruction-following quality.** The model is quantized and not Russian-tuned; it may occasionally drift to English. **Mitigation:** manual smoke M-LANG-01..M-LANG-03 is gating; if it fails, design pivots to Option E (few-shot) or model swap is escalated separately. Severity: medium, addressable, not an architecture risk.
- **R2 — Spec said `internal sealed`, design says `public sealed`.** The spec's AC-005 conflicts with the existing project convention (`Iris.Application/Chat/*` is uniformly `public sealed` and there is no `InternalsVisibleTo` for `Iris.Application.Tests`). Following AC-005 verbatim would require either making test classes use reflection or adding `InternalsVisibleTo`, both of which are larger architectural changes than the spec authorizes. **Mitigation:** design follows project convention (`public sealed`) and records this divergence here. The plan/implement phases must honor this design choice; if the user wants strict AC-005 adherence, the spec needs an amendment first.
- **R3 — Breaking signature change to `AddIrisApplication`.** All call sites are in this repo (1 host + 3 tests). **Mitigation:** all call sites are inventoried in §6 and §13; the implement phase touches them in one pass. No external consumers.
- **R4 — Dead code `PromptTemplateProvider.cs` in wrong namespace.** Observed in §3 reconnaissance. **Mitigation:** out of scope for this design. Recorded for a separate cleanup ticket, not added to this work to avoid scope creep.
- **R5 — Russian baseline text must convey persona meaning equivalent to §18 of `mem_library/03_iris_persona.md`.** Translation is not a mechanical task. **Mitigation:** plan phase reviews the chosen Russian text against §18 semantically; T-LANG-04..T-LANG-06 enforce structural content but cannot judge tone. Operator manual smoke is the qualitative gate.
- **R6 — Future `MirrorOperatorLanguage` switch could break FR-005.** A property added to `LanguageOptions` later could enable mirroring, contradicting the no-mirror rule. **Mitigation:** if mirroring is later wanted, it must be a new spec, not a flag flip. Design records that the current contract treats Russian as a hard default, intentionally, in §10.
- **R7 — `LanguageOptions.Default` static factory creates a single shared instance reference.** Hosts mutating it via reflection would affect all consumers. **Mitigation:** record is immutable by C# semantics; reflection mutation is out of scope for any reasonable threat model.

Trade-offs accepted:

- Four files for one configurable string — accepted to preserve the seam for future persona work.
- Breaking ctor change to `PromptBuilder` — accepted because all call sites are in this repo.
- No `IOptions<>` binding — accepted to preserve the existing Application idiom; revisit only as a project-wide convention change.

## 16. Acceptance Mapping

Mapping each spec acceptance criterion to this design's target component(s):

| Spec acceptance criterion | Design coverage |
|---|---|
| Files in `Iris.Application/Persona/Language/` exist | §6 component design |
| `PromptBuilder` no longer contains `_baselineSystemPrompt`; ctor takes `ILanguagePolicy` | §6 `PromptBuilder` (modified), §7 ctor contract |
| `AddIrisApplication` registers `LanguageOptions` and `ILanguagePolicy` | §11 registration block |
| T-LANG-01..T-LANG-09 pass | §13 testing design (also adds T-LANG-10/11) |
| Existing `PromptBuilder`/`SendMessageHandler` tests still pass | §13 update strategy |
| Existing Architecture tests pass without modification | §3, §11 — no new project refs/forbidden namespaces |
| `dotnet build .\Iris.slnx` 0/0 | covered by plan phase |
| `dotnet test .\Iris.slnx` green, +T-LANG-* | §13 |
| `dotnet format .\Iris.slnx --verify-no-changes` exits 0 | covered by plan phase |
| Manual M-LANG-01..M-LANG-03 produce Russian | §15 R1 mitigation |
| `mem_library/03_iris_persona.md` §21 added via `/update-memory` | §5 ownership; not a code change |
| `Iris.Domain`, `Iris.Shared`, adapters, hosts other than DI line untouched | §3, §6 component scope |
| No new packages/refs/`InternalsVisibleTo` | §3, §11 |
| No commits/push during implementation | governed by `/implement` phase |

Note on AC-005 (visibility): see §15 R2 — design follows project's `public sealed` convention rather than the spec's `internal sealed` request, on the grounds that the existing convention is uniformly `public sealed` in `Iris.Application/Chat/*` and changing it would require introducing `InternalsVisibleTo` (out of scope per the spec itself). If the user disagrees, this is the only point that needs spec amendment before plan.

## 17. Blocking Questions

No blocking open questions.

Non-blocking design notes for the plan phase:

- **N-DESIGN-1** Test project is `Iris.Application.Tests`, not `Iris.Application.UnitTests` (spec slip). Plan must use the real name.
- **N-DESIGN-2** Decide in plan whether to add a documentation example to `appsettings.local.example.json`. Recommendation: yes, one-line commented example, since `Persona:Language` is a discoverable knob.
- **N-DESIGN-3** Decide in plan whether `SendMessageHandlerTests` need any new assertion exercising the language path. Recommendation: no — they assert orchestration, not prompt content.
- **N-DESIGN-4** Visibility divergence from spec AC-005 is intentional (R2). Plan should not silently re-raise to `internal sealed`.

---

## Execution Note

No implementation was performed.
No files were modified.

## Assumptions

1. The existing Application convention (`public sealed` types, plain-POCO options, no `IOptions<>`) is the project standard and must be preserved. Reconnaissance confirmed: 9/9 inspected `Iris.Application/Chat/*` types are `public sealed`, `AddIrisApplication` already takes a POCO options parameter, no `IOptions<>`/`Configure<>`/`Bind(`/`GetSection(` calls exist anywhere in `Iris.Application`.
2. The single host using `AddIrisApplication` is `Iris.Desktop`; future hosts will adopt the same idiom when they exist.
3. `llama3:latest` honors a Russian-language system prompt strongly enough for M-LANG-01..M-LANG-03 to pass without few-shot. If empirically false, design Option E remains available without any contract change.
4. The Russian baseline text chosen during implementation will be reviewed by the operator. Design fixes the **requirements** of the text (§6 `LanguageInstructionBuilder`), not the verbatim wording.
5. Memory update to `mem_library/03_iris_persona.md` §21 is performed by `/update-memory` after implementation, not by this design.

## Blocking Questions

No blocking questions.

---

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `docs/specs/2026-05-01-iris-default-language-russian.spec.md` |
| B — Design | ✅ Satisfied | This design |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes — none expected from this design (Application-only, no new refs/packages, namespace stays under `Iris.Application.*`); reviewer may still want to confirm |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work — covers `mem_library/03_iris_persona.md` §21 + `PROJECT_LOG.md` |
