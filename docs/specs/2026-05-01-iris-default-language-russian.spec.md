# Specification: Iris Default Response Language is Russian

## 1. Problem Statement

Iris currently responds to the operator in English. The operator's primary language is Russian. Iris is a personal local AI companion built for a single operator, therefore its default response language must match the operator's primary language.

The English default originates from a single string constant — `Iris.Application/Chat/Prompting/PromptBuilder._baselineSystemPrompt` (`"You are Iris, a local personal AI companion. Be helpful, clear, and respectful."`) — which is the only system prompt sent to the model. The model (currently `llama3:latest` via Ollama) mirrors the language of the system prompt when no explicit language instruction is given, so all responses come back in English.

The intent that "Iris is built for the operator personally" is documented in `mem_library/03_iris_persona.md` (persona, tone, boundaries), but that file does not declare a default response language. Consequently no agent or future implementation step can be held accountable to a Russian-language requirement, and the requirement can be accidentally reverted.

## 2. Goal

Iris responds in Russian by default in all chat interactions, regardless of the operator's input language, while preserving English form for technical tokens, code, and engineering artifacts. The requirement is encoded both as enforceable behavior in `Iris.Application` and as durable product intent in `mem_library`.

## 3. Scope

### In Scope

- Replace the single English baseline system prompt with a Russian-language system prompt produced by an Application-layer language policy.
- Introduce `ILanguagePolicy` + a default implementation in `Iris.Application/Persona/Language` that owns the language instruction inserted into every chat system prompt.
- Introduce `LanguageOptions` configuration bound to a `Persona:Language` configuration section, with sensible defaults so that hosts work without any new configuration.
- Wire the new policy through `Iris.Application/DependencyInjection.cs` so all hosts (Desktop, API, Worker) receive it automatically.
- Update `PromptBuilder` to obtain the system-prompt language preamble via the new policy instead of a `private const`.
- Update existing `PromptBuilder` and `SendMessageHandler` tests to reflect the new contract, and add tests that lock the Russian-default behavior and the policy-options flow.
- Update `mem_library/03_iris_persona.md` with a new section "Default Language" capturing the product intent (Russian-by-default, English-mirrored technical tokens) so future agents/sessions cannot lose the requirement.

### Out of Scope

- Localization of `Iris.Desktop` UI strings (button labels, statuses such as `Thinking`/`Error`, placeholders, tooltips). UI shell remains English; this is a separate spec if/when needed.
- Localization of engineering artifacts: logs, telemetry, `ApplicationEventKind`, `DecisionReasonCode`, error codes/identifiers, `PROJECT_LOG.md`, `overview.md`, `log_notes.md`, `debt_tech_backlog.md`, spec/design/plan documents under `docs/`, and git commit messages. They remain English.
- Wiring the broader persona slice (`PersonaContextBuilder`, `SpeechStylePolicy`, `MoodSelectionService`, `RelationshipPolicy`) into prompt assembly. That is a separate roadmap phase.
- Mirroring the operator's input language. Russian is a hard default per Q6.
- Dynamic/runtime language switching during a session.
- Multi-language support beyond Russian as default.
- Translation tooling, translation memory, or i18n infrastructure.
- Changes to `Iris.ModelGateway`, `Iris.Persistence`, `Iris.Domain`, `Iris.Shared`, hosts other than the DI registration line, or any other adapter.

### Non-Goals

- Changing the model itself or model-selection behavior.
- Rewriting the persona document beyond appending a new section about language.
- Introducing a translation layer that converts English model output to Russian.
- Detecting the operator's language from input.
- Changing how `mem_library` is read or composed.

## 4. Current State

Inspected facts:

- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` is the single composer of the system prompt:
  ```csharp
  private const string _baselineSystemPrompt =
      "You are Iris, a local personal AI companion. Be helpful, clear, and respectful.";
  ...
  var messages = new List<ChatModelMessage>
  {
      new(ChatModelRole.System, _baselineSystemPrompt)
  };
  ```
- `src/Iris.Application/Chat/Prompting/PromptTemplateProvider.cs` is an empty stub (`internal class { }`, namespace `Iris.Application.Chat.BuildPrompt`) and is not referenced.
- `Iris.Application/Persona/{Style, Mood, Context, Policies, Relationship, State}` exists as a slice but is not currently consumed by `PromptBuilder`.
- `Iris.Application/DependencyInjection.cs` is the registration root for Application services.
- `mem_library/03_iris_persona.md` (§17 "Implementation Guidance", §18 "First Slice Persona") explicitly states that final prompt composition belongs in `Iris.Application/Chat/Prompting`, and provides an English baseline. No section currently defines default response language.
- The Desktop host loads configuration via `Microsoft.Extensions.Configuration` with `appsettings.json` and `appsettings.local.json` (Phase 6 / `appsettings.local.example.json`).
- Operator runs `llama3:latest` via Ollama; model is known to respect language instructions when the system prompt is itself written in the target language (Q8).
- Existing test suites: 22 Application unit tests, 8 Architecture tests, 82 Integration tests. Architecture tests currently pass and forbid Application → adapter references.

## 5. Affected Areas

Production code (writes):

- `src/Iris.Application/Persona/Language/ILanguagePolicy.cs` — new.
- `src/Iris.Application/Persona/Language/LanguageOptions.cs` — new.
- `src/Iris.Application/Persona/Language/RussianDefaultLanguagePolicy.cs` — new.
- `src/Iris.Application/Persona/Language/LanguageInstructionBuilder.cs` — new.
- `src/Iris.Application/Chat/Prompting/PromptBuilder.cs` — modified (remove `_baselineSystemPrompt` const, accept `ILanguagePolicy` via ctor, use it for the system message).
- `src/Iris.Application/DependencyInjection.cs` — modified (register `LanguageOptions` binding + `ILanguagePolicy` → `RussianDefaultLanguagePolicy`).

Tests (writes):

- `tests/Iris.Application.UnitTests/Chat/Prompting/PromptBuilderTests.cs` — modified to construct `PromptBuilder` with a fake `ILanguagePolicy`; assertions updated to check that the system message is whatever the policy returned.
- `tests/Iris.Application.UnitTests/Persona/Language/RussianDefaultLanguagePolicyTests.cs` — new.
- `tests/Iris.Application.UnitTests/Persona/Language/LanguageInstructionBuilderTests.cs` — new.
- `tests/Iris.Application.UnitTests/Chat/SendMessage/SendMessageHandlerTests.cs` — modified only if it currently constructs `PromptBuilder` directly; otherwise unchanged.

Memory (write, gated by `/update-memory`):

- `.agent/mem_library/03_iris_persona.md` — append §21 "Default Language".

Configuration (optional writes):

- `src/Iris.Desktop/appsettings.json` — optionally add an empty `Persona:Language` section as documentation example. Not strictly required: defaults are baked into `LanguageOptions`.
- `src/Iris.Desktop/appsettings.local.example.json` — optionally extend with a `Persona:Language` example block. Not strictly required.

Not affected (must not be modified by this work):

- `Iris.Domain`, `Iris.Shared`, `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Perception`, `Iris.Tools`, `Iris.Voice`, `Iris.Infrastructure`, `Iris.SiRuntimeGateway`, `Iris.Api`, `Iris.Worker`, `Iris.Desktop` (beyond the optional appsettings doc note above).
- Any persona file outside `03_iris_persona.md`.
- Any EF migration.
- `.editorconfig`, project references, NuGet packages.

## 6. Functional Requirements

- **FR-001** Iris must respond in Russian by default to any chat message sent through `SendMessageHandler` → `PromptBuilder`, regardless of the language of the operator's input.
- **FR-002** The system message composed by `PromptBuilder` must be a Russian-language baseline produced by `ILanguagePolicy`. The string `"You are Iris, a local personal AI companion. Be helpful, clear, and respectful."` must no longer appear as the system prompt for the default configuration.
- **FR-003** The Russian baseline system prompt must explicitly instruct the model to keep technical tokens (file names, paths, namespaces, identifiers, commands, code) in their original English form.
- **FR-004** The Russian baseline system prompt must explicitly instruct the model that, when answering with code, code and in-code comments stay in English while prose around the code stays in Russian.
- **FR-005** When the operator's input is in English (e.g., a quoted log line, an English error message, or an English question), Iris must still respond in Russian. The policy must not switch languages based on input.
- **FR-006** `LanguageOptions` must expose at least: a `DefaultLanguage` value (default `"ru"`), and the policy must accept this value via standard `IOptions<LanguageOptions>` binding from a `Persona:Language` configuration section.
- **FR-007** When no `Persona:Language` configuration is present in any host's `appsettings*.json`, the system must still produce a Russian baseline (defaults baked into `LanguageOptions`).
- **FR-008** `mem_library/03_iris_persona.md` must contain a new section that durably records: Russian as the default response language, the technical-token rule, the code/comments-in-English rule, and the no-mirror rule. The new section must be added through `/update-memory` (not as part of code-only commits) and must not delete or rewrite existing persona content.

## 7. Architecture Constraints

- **AC-001** All new code lives inside `Iris.Application`. No new project, no new project reference, no new NuGet package, no new `InternalsVisibleTo`.
- **AC-002** `Iris.Domain` is not modified and gains no new dependency.
- **AC-003** `Iris.Application` continues not to reference any adapter (`Iris.Persistence`, `Iris.ModelGateway`, `Iris.Perception`, `Iris.Tools`, `Iris.Voice`, `Iris.Infrastructure`, `Iris.SiRuntimeGateway`).
- **AC-004** Hosts (`Iris.Desktop`, `Iris.Api`, `Iris.Worker`) do not own the language instruction. They may only read `Persona:Language` configuration via standard `IOptions<>` binding through the existing Application registration extension.
- **AC-005** `ILanguagePolicy`, `LanguageOptions`, `RussianDefaultLanguagePolicy`, and `LanguageInstructionBuilder` live under namespace `Iris.Application.Persona.Language`. They follow existing Application code conventions: `internal sealed` types with explicit ctor injection, no static state, no service locator.
- **AC-006** `PromptBuilder` receives `ILanguagePolicy` via constructor injection. Existing `PromptBuilder.Build(...)` public signature is unchanged.
- **AC-007** Existing Architecture tests must continue to pass without modification (no new forbidden references, no namespace drift).
- **AC-008** No persona logic beyond language is added in this spec. `PersonaContextBuilder`, `SpeechStylePolicy`, `MoodSelectionService`, `RelationshipPolicy` are not consumed by `PromptBuilder` in this work.

## 8. Contract Requirements

- **`PromptBuilder.Build(PromptBuildRequest)` (public method)**: signature unchanged; return type `Result<PromptBuildResult>` unchanged. Behavior changed: the produced `ChatModelRequest.Messages[0]` is now derived from `ILanguagePolicy` instead of a hardcoded constant. Compatibility: callers do not need to change.
- **`PromptBuilder` constructor (public)**: changed. New required parameter `ILanguagePolicy languagePolicy`. Compatibility: only direct callers are tests and DI registration; both are updated in this spec's scope.
- **`ILanguagePolicy` (new public interface)**: introduces a stable seam for future extension. Initial contract: returns the system-prompt preamble string.
- **`LanguageOptions` (new public options class)**: bound to `Persona:Language` config section. Defaults: `DefaultLanguage = "ru"`. Adding an empty `Persona:Language` section to host configuration is optional and backward-compatible.
- **`Iris.Application.DependencyInjection.AddIrisApplication(...)` (public extension)**: signature unchanged. Internal registrations gain `LanguageOptions` binding and `ILanguagePolicy` registration. Hosts calling this method need no source changes.
- **No persistence / database / migration contract changes.**
- **No HTTP/API contract changes.**
- **No UI contract changes.**

## 9. Data and State Requirements

No changes to persisted data, schema, identifiers, or session state. No migrations. The language instruction is recomputed on every prompt build from `IOptions<LanguageOptions>` and is not cached at runtime beyond what the DI container already provides for the singleton `RussianDefaultLanguagePolicy`.

## 10. Error Handling and Failure Modes

- If `LanguageOptions.DefaultLanguage` is null/empty/whitespace at runtime, the policy must fall back to `"ru"` and continue. The system must never produce an empty system prompt.
- If `LanguageOptions.DefaultLanguage` is a non-`"ru"` value (e.g., a future operator sets `"en"`), the policy must still produce a deterministic, non-empty system prompt. For this spec, only `"ru"` produces the documented Russian baseline. Other values fall back to `"ru"` (so accidental misconfiguration cannot revert to English silently). This is intentional: per Q1, Russian is a hard default.
- If `ILanguagePolicy` is missing from the DI container, `PromptBuilder` resolution fails fast at startup (standard DI behavior). No silent fallback in `PromptBuilder` itself.
- Model output that ignores the language instruction (model-side failure) is **not** a failure of this specification. It is a known model-quality risk recorded under "Open Questions". The system is correct as long as the system prompt instructs Russian and the policy was configured.

## 11. Testing Requirements

Unit tests (xUnit, `Iris.Application.UnitTests`):

- **T-LANG-01** `RussianDefaultLanguagePolicyTests`: returns a non-empty Russian system-prompt string when `LanguageOptions.DefaultLanguage = "ru"`.
- **T-LANG-02** `RussianDefaultLanguagePolicyTests`: returns a Russian fallback when `DefaultLanguage` is null/empty/whitespace.
- **T-LANG-03** `RussianDefaultLanguagePolicyTests`: returns a Russian fallback when `DefaultLanguage` is an unknown value (e.g., `"jp"`).
- **T-LANG-04** `LanguageInstructionBuilderTests`: produced text contains an explicit Russian instruction to respond in Russian.
- **T-LANG-05** `LanguageInstructionBuilderTests`: produced text contains an explicit instruction (in Russian) that technical tokens (file names, paths, identifiers, commands, code) are preserved in English.
- **T-LANG-06** `LanguageInstructionBuilderTests`: produced text contains an explicit instruction that code and in-code comments stay in English while prose stays in Russian.
- **T-LANG-07** `PromptBuilderTests`: `Build(...)` returns a `ChatModelRequest` whose first message has `ChatModelRole.System` and whose content equals the string produced by the injected `ILanguagePolicy` (verified with a fake/stub policy).
- **T-LANG-08** `PromptBuilderTests`: the legacy hardcoded English baseline `"You are Iris, a local personal AI companion. Be helpful, clear, and respectful."` no longer appears as a system message under default configuration. (Negative assertion locks the regression.)
- **T-LANG-09** `PromptBuilderTests`: history mapping behavior (User/Assistant/System role mapping) is unchanged.

Architecture tests (`Iris.Architecture.Tests`):

- No new tests required. Existing dependency-direction and forbidden-namespace tests must continue to pass without modification.

Integration tests (`Iris.IntegrationTests`):

- No new tests required for this spec. Composition is exercised indirectly through existing handler tests if any.

Manual verification:

- **M-LANG-01** With `appsettings.local.json` configured for `llama3:latest` and Ollama running, sending a Russian message via Iris.Desktop produces a Russian response.
- **M-LANG-02** With the same configuration, sending an English message ("hello, what is the current time?") produces a Russian response (per FR-005).
- **M-LANG-03** Sending a question that forces a code answer ("покажи hello world на C#") produces Russian prose with an English code block and English code comments.

Required commands to pass before readiness claim:

- `dotnet build .\Iris.slnx`
- `dotnet test .\Iris.slnx`
- `dotnet format .\Iris.slnx --verify-no-changes`

## 12. Documentation and Memory Requirements

- `mem_library/03_iris_persona.md` — append §21 "Default Language" through `/update-memory`. Section must record: Russian default; English-preserved technical tokens; code/comments in English with Russian prose; no language mirroring; `Iris.Application/Persona/Language` is the implementation home; this requirement supersedes §18's English baseline example for actual prompt content.
- `.agent/PROJECT_LOG.md` — appended by `/update-memory` after implementation: new entry describing the Phase-7 (or appropriate phase number) language policy work, files changed, verification results.
- `.agent/overview.md` — updated by `/update-memory` to reflect new current phase / next step.
- `.agent/debt_tech_backlog.md` — append a P2 backlog entry: "Wire `PersonaContextBuilder` into `PromptBuilder`" so the dormant persona slice is not forgotten.
- No `docs/` documents are required by this spec. The spec, design, and plan files for this work follow the standard `docs/{specs,designs,plans}/<date>-iris-default-language-russian.*.md` convention and are produced by their respective workflows.
- No README or AGENTS.md updates are required.

## 13. Acceptance Criteria

- [ ] `Iris.Application/Persona/Language/{ILanguagePolicy,LanguageOptions,RussianDefaultLanguagePolicy,LanguageInstructionBuilder}.cs` exist and follow Application conventions (`internal sealed` where applicable, no static state, no service locator).
- [ ] `PromptBuilder` no longer contains `_baselineSystemPrompt`. Its constructor accepts `ILanguagePolicy`. Its `Build(...)` produces a `ChatModelRequest` whose first message uses the policy-provided text.
- [ ] `Iris.Application.DependencyInjection.AddIrisApplication(...)` binds `LanguageOptions` from `Persona:Language` and registers `ILanguagePolicy` → `RussianDefaultLanguagePolicy` (singleton).
- [ ] All unit tests T-LANG-01..T-LANG-09 are written and pass.
- [ ] Existing `PromptBuilder`/`SendMessageHandler` tests are updated to compile against the new ctor and continue to pass.
- [ ] Existing Architecture tests pass without modification.
- [ ] `dotnet build .\Iris.slnx` returns 0 errors, 0 warnings.
- [ ] `dotnet test .\Iris.slnx` is green, with at least the previous count of tests **plus** the new T-LANG-* tests.
- [ ] `dotnet format .\Iris.slnx --verify-no-changes` exits 0.
- [ ] Manual M-LANG-01..M-LANG-03 produce Russian responses on `llama3:latest` via Ollama.
- [ ] `mem_library/03_iris_persona.md` contains a new "Default Language" section recording the requirement; old §1–§20 are unchanged.
- [ ] `Iris.Domain`, `Iris.Shared`, all adapters, and all hosts other than DI registration-line wiring are not modified.
- [ ] No new NuGet packages, no new project references, no new `InternalsVisibleTo` directives.
- [ ] No git commits, push, or destructive operations are performed by the implementation phase.

## 14. Open Questions

No blocking open questions.

Non-blocking notes (recorded for the design phase, not gating this spec):

- N1: `llama3:latest` quality on Russian instruction-following is empirical. If, after manual M-LANG-01..M-LANG-03, the model drifts to English under certain prompts, the design phase may add a few-shot Russian example in the system prompt. This is a tactical adjustment, not a spec change.
- N2: Future migration to a stronger model (e.g., `qwen2.5:9b`) is out of scope but compatible with `LanguageOptions` (no API surface changes).
