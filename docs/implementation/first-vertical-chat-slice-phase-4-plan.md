# First Vertical Chat Slice Phase 4 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `Iris.ModelGateway` Ollama chat adapter behind `IChatModelClient` so the Phase 1-3 Application chat flow can call a local model through the intended architecture.

**Architecture:** `Iris.ModelGateway` is an adapter. It depends inward on `Iris.Application` and `Iris.Shared`, maps Application chat contracts to Ollama `/api/chat`, performs the HTTP call, maps provider responses back to `ChatModelResponse`, and returns controlled `Result<T>` failures. Application remains free of ModelGateway references, and ModelGateway must not build prompts, persist state, or make persona/memory decisions.

**Tech Stack:** .NET 10, C#, `HttpClientFactory` typed clients, `System.Text.Json`, Ollama `/api/chat`, xUnit, Iris.Application, Iris.Shared, Iris.ModelGateway, Iris.Integration.Tests.

---

## Existing Spec / Design Check

`docs/implementation/first-vertical-chat-slice.md` contains the high-level ModelGateway scope:

- implement `OllamaChatModelClient`;
- implement request mapper;
- implement response mapper;
- implement options;
- register `HttpClient`;
- return `ChatModelResponse`;
- handle unavailable Ollama through a controlled error/result.

That document is directionally correct, but it is incomplete for Phase 4 execution. It does not specify:

- exact Ollama endpoint shape;
- non-streaming request policy;
- options validation policy;
- provider error taxonomy;
- cancellation and timeout behavior;
- JSON failure behavior;
- privacy-safe diagnostics boundaries;
- where adapter tests live while no dedicated `Iris.ModelGateway.Tests` project exists;
- whether `IModelRouter`, LM Studio, OpenAI-compatible, embeddings, or vision are included.

This document closes those gaps and is the executable Phase 4 plan.

## Phase Numbering Correction

`.agent/first-vertical-slice.md` originally listed ModelGateway as Phase 3 and Application SendMessage as Phase 4. The actual implemented order is now:

1. Phase 1-2: Domain minimum and Application send-message use case.
2. Phase 3: Persistence SQLite adapter.
3. Phase 4: ModelGateway Ollama adapter.
4. Phase 5: Desktop wiring.

Future work must follow the implemented sequence above.

## Phase 4 Scope

In scope:

- `Iris.ModelGateway` project references to Application and Shared.
- Ollama chat request/response DTOs for `/api/chat`.
- `OllamaModelClientOptions` with required base URL, model name, and timeout.
- `OllamaRequestMapper`.
- `OllamaResponseMapper`.
- `ModelGatewayHttpErrorHandler`.
- `OllamaChatModelClient` implementing `IChatModelClient`.
- `DependencyInjection` registration for the Ollama typed client.
- Integration tests with fake `HttpMessageHandler`; no real Ollama dependency in automated tests.
- Optional manual smoke command against local Ollama after tests pass.
- Metadata updates after implementation.

Out of scope:

- Streaming responses.
- Tool calls.
- Vision/image messages.
- Embeddings.
- `IModelRouter` implementation.
- LM Studio implementation.
- OpenAI-compatible implementation.
- Desktop wiring.
- Settings UI.
- API/Worker composition.
- Memory recall/extraction.
- Prompt building changes.
- Provider retry/circuit-breaker policy.
- Production telemetry pipeline.
- Remote/cloud providers.

## Design Closure For First Vertical Slice

The first vertical slice now needs these clarified decisions.

### ModelGateway Adapter Boundary

`Iris.ModelGateway` owns provider protocol details only.

It may:

- choose the Ollama endpoint path;
- serialize the provider request;
- deserialize the provider response;
- map HTTP/provider failures to `Error`;
- expose DI registration for hosts;
- use `HttpClient`, `System.Text.Json`, and options.

It must not:

- build prompts;
- decide persona text;
- load conversation history;
- call repositories;
- touch SQLite;
- log full prompts or raw responses by default;
- call tools, voice, perception, API, Worker, or SI runtime;
- expose Ollama-specific DTOs to Application.

### Ollama API Contract

Context7 verified the Ollama Chat API contract from the official Ollama API documentation.

Phase 4 uses:

```text
POST /api/chat
```

Request body:

```json
{
  "model": "configured-model-name",
  "messages": [
    {
      "role": "system",
      "content": "system prompt"
    },
    {
      "role": "user",
      "content": "message"
    }
  ],
  "stream": false,
  "options": {
    "temperature": 0.7
  }
}
```

Response body:

```json
{
  "model": "configured-model-name",
  "created_at": "2025-10-17T23:14:07.414671Z",
  "message": {
    "role": "assistant",
    "content": "assistant text"
  },
  "done": true
}
```

Phase 4 always sends `stream: false`.

### Application Contract

Phase 4 uses the existing Application abstraction:

```csharp
public interface IChatModelClient
{
    Task<Result<ChatModelResponse>> SendAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken);
}
```

Existing Application chat contracts remain:

```csharp
public sealed record ChatModelRequest(
    IReadOnlyList<ChatModelMessage> Messages,
    ChatModelOptions Options);

public sealed record ChatModelMessage(ChatModelRole Role, string Content);

public sealed record ChatModelOptions(string? Model = null, double? Temperature = null);

public sealed record ChatModelResponse(string Content);
```

Do not modify Application contracts in Phase 4 unless implementation proves a compile blocker that cannot be solved inside ModelGateway.

## Invariants

### Architecture Invariants

- `Iris.ModelGateway` may reference `Iris.Application` and `Iris.Shared`.
- `Iris.ModelGateway` must not reference `Iris.Persistence`, `Iris.Desktop`, `Iris.Api`, `Iris.Worker`, `Iris.Tools`, `Iris.Voice`, `Iris.Perception`, or `Iris.SiRuntimeGateway`.
- `Iris.Application` must not reference `Iris.ModelGateway`.
- `Iris.Domain` must not be referenced by ModelGateway unless a concrete existing Application contract requires it. Current Phase 4 does not require Domain.
- Hosts will compose the adapter later; Phase 4 only provides `AddIrisModelGateway`.

### Provider Invariants

- The HTTP endpoint path is `/api/chat`.
- The request uses non-streaming mode: `stream = false`.
- Roles map exactly:
  - `ChatModelRole.System` -> `"system"`;
  - `ChatModelRole.User` -> `"user"`;
  - `ChatModelRole.Assistant` -> `"assistant"`.
- Message content is preserved as provided by Application.
- Empty request message lists return a controlled adapter failure.
- Empty individual message content returns a controlled adapter failure even if Application should normally prevent it.
- The model name comes from `request.Options.Model` when present; otherwise from configured `OllamaModelClientOptions.ChatModel`.
- If no model can be resolved, return a controlled configuration error.
- Temperature is sent only when `request.Options.Temperature` has a value.
- Temperature must be finite and within `0.0..2.0` for Phase 4.
- The adapter returns one `ChatModelResponse` containing assistant text.
- Empty or whitespace assistant content is a controlled invalid-response failure.

### Privacy Invariants

- Do not log full prompts.
- Do not log full conversation history.
- Do not log raw model responses.
- Do not include user content in error messages.
- HTTP non-success response bodies are not exposed through returned `Error.Message`.
- Diagnostic names and error codes may mention provider name, status code category, and operation.

### Failure Invariants

- Expected provider failures return `Result<ChatModelResponse>.Failure(...)`.
- User-requested cancellation is not converted into a failure result; it propagates as cancellation.
- Timeout/provider connectivity failures are converted into controlled failures when they are not caused by the caller cancellation token.
- Invalid JSON is converted into a controlled invalid-response failure.
- No partial Application state is saved by Phase 4 because Persistence remains outside ModelGateway.

## Failure Pattern Matrix

| Failure | Detection | Result policy | Privacy policy |
| --- | --- | --- | --- |
| Missing base URL | Options validation | `model_gateway.ollama.base_url_required` | No prompt data |
| Invalid base URL | Options validation | `model_gateway.ollama.base_url_invalid` | No prompt data |
| Missing default model and no request model | Before HTTP call | `model_gateway.ollama.model_required` | No prompt data |
| Timeout <= 0 | Options validation | `model_gateway.ollama.timeout_invalid` | No prompt data |
| Empty request | Before mapping | `model_gateway.request.empty_messages` | No prompt data |
| Empty message content | Before mapping | `model_gateway.request.empty_message_content` | No content echo |
| Invalid temperature | Before mapping | `model_gateway.request.temperature_invalid` | No prompt data |
| Ollama unavailable | `HttpRequestException` | `model_gateway.provider_unavailable` | No exception text to UI |
| HTTP timeout | `TaskCanceledException` without caller cancellation | `model_gateway.provider_timeout` | No prompt data |
| Caller cancellation | cancellation token set | Propagate cancellation | No failure conversion |
| HTTP 404 | non-success status | `model_gateway.provider_not_found` | No response body |
| HTTP 408/504 | non-success status | `model_gateway.provider_timeout` | No response body |
| HTTP 429 | non-success status | `model_gateway.provider_rate_limited` | No response body |
| HTTP 5xx | non-success status | `model_gateway.provider_failure` | No response body |
| Other non-success | non-success status | `model_gateway.provider_http_error` | Status code only |
| Invalid JSON | `JsonException` | `model_gateway.provider_invalid_response` | No raw JSON |
| Missing `message` | response mapper | `model_gateway.provider_invalid_response` | No raw JSON |
| Empty assistant content | response mapper | `model_gateway.provider_empty_response` | No raw JSON |
| Provider returns non-assistant role | response mapper | `model_gateway.provider_invalid_role` | No raw JSON |

## File Responsibility Map

Modify:

- `src/Iris.ModelGateway/Iris.ModelGateway.csproj`  
  Add project references to `Iris.Application` and `Iris.Shared`. Add package references required for DI typed clients if the SDK does not provide them transitively.

- `src/Iris.ModelGateway/DependencyInjection.cs`  
  Replace the existing empty file with a public static DI extension class.

- `src/Iris.ModelGateway/Ollama/OllamaApiModels.cs`  
  Replace the existing empty file with internal request/response DTO records used only by the adapter.

- `src/Iris.ModelGateway/Ollama/OllamaChatModelClient.cs`  
  Replace the existing empty file with an internal sealed `IChatModelClient` implementation.

- `src/Iris.ModelGateway/Ollama/OllamaRequestMapper.cs`  
  Replace the existing empty file with Application-to-Ollama mapping.

- `src/Iris.ModelGateway/Ollama/OllamaResponseMapper.cs`  
  Replace the existing empty file with Ollama-to-Application mapping.

- `src/Iris.ModelGateway/Ollama/OllamaModelClientOptions.cs`  
  Replace the existing empty file with validated options.

- `src/Iris.ModelGateway/Http/ModelGatewayHttpErrorHandler.cs`  
  Replace the existing empty file with HTTP and exception-to-error mapping.

- `src/Iris.ModelGateway/Http/ModelGatewayHttpClientNames.cs`  
  Replace the existing empty file with stable internal client names.

- `src/Iris.ModelGateway/Http/ModelGatewayHttpOptions.cs`  
  Replace the existing empty file only if shared HTTP timeout/header settings are needed. If `OllamaModelClientOptions` fully owns Phase 4 configuration, leave this file untouched and record the reason in metadata.

- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj`  
  Add a project reference to `Iris.ModelGateway` and any package reference needed for `HttpClient` test helpers only if required.

Create:

- `tests/Iris.IntegrationTests/ModelGateway/FakeHttpMessageHandler.cs`  
  Test helper for controlled HTTP responses.

- `tests/Iris.IntegrationTests/ModelGateway/OllamaRequestMapperTests.cs`  
  Verifies role/model/temperature/stream mapping.

- `tests/Iris.IntegrationTests/ModelGateway/OllamaResponseMapperTests.cs`  
  Verifies success and invalid response mapping.

- `tests/Iris.IntegrationTests/ModelGateway/OllamaChatModelClientTests.cs`  
  Verifies HTTP path, serialization, success, unavailable provider, non-success status, invalid JSON, timeout, and cancellation behavior.

Do not create a separate `Iris.ModelGateway.Tests` project in Phase 4. The repository currently has five test projects and adapter integration tests already belong in `Iris.Integration.Tests` per `.agent/architecture.md`.

## Expected Production Shape

### Project References

`src/Iris.ModelGateway/Iris.ModelGateway.csproj` should include:

```xml
<ItemGroup>
  <ProjectReference Include="..\Iris.Application\Iris.Application.csproj" />
  <ProjectReference Include="..\Iris.Shared\Iris.Shared.csproj" />
</ItemGroup>
```

Add package references only when required by compile:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.1" />
  <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.1" />
</ItemGroup>
```

If the repository already centralizes package versions later, move versions to that mechanism instead of duplicating them.

### Options

`OllamaModelClientOptions` owns provider configuration:

```csharp
public sealed class OllamaModelClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; }

    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.base_url_required",
                "Ollama base URL is required."));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.base_url_invalid",
                "Ollama base URL must be an absolute URI."));
        }

        if (Timeout <= TimeSpan.Zero)
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.timeout_invalid",
                "Ollama timeout must be greater than zero."));
        }

        return Result.Success();
    }
}
```

The options class intentionally does not provide production defaults for base URL, model, or timeout. Those values are configuration decisions.

### Ollama DTOs

`OllamaApiModels.cs` should define internal DTOs:

```csharp
internal sealed record OllamaChatRequest(
    string Model,
    IReadOnlyList<OllamaChatMessage> Messages,
    bool Stream,
    OllamaChatOptions? Options);

internal sealed record OllamaChatMessage(string Role, string Content);

internal sealed record OllamaChatOptions(double? Temperature);

internal sealed record OllamaChatResponse(
    string? Model,
    DateTimeOffset? CreatedAt,
    OllamaChatMessage? Message,
    bool? Done);
```

Use `[JsonPropertyName]` only where C# names and JSON names differ, for example `created_at`.

### Request Mapper

`OllamaRequestMapper` should:

- validate request object;
- validate message list;
- validate message content;
- resolve model name from request options or adapter options;
- validate temperature range;
- map roles;
- set `Stream` to `false`.

Expected public shape inside the assembly:

```csharp
internal static class OllamaRequestMapper
{
    public static Result<OllamaChatRequest> Map(
        ChatModelRequest request,
        OllamaModelClientOptions options)
    {
        // mapping with controlled Result failures
    }
}
```

### Response Mapper

`OllamaResponseMapper` should:

- reject null response;
- require `Message`;
- require role `assistant`;
- require non-empty content;
- return `new ChatModelResponse(content)`.

Expected public shape inside the assembly:

```csharp
internal static class OllamaResponseMapper
{
    public static Result<ChatModelResponse> Map(OllamaChatResponse? response)
    {
        // mapping with controlled Result failures
    }
}
```

### HTTP Error Handler

`ModelGatewayHttpErrorHandler` should be small and deterministic:

```csharp
internal static class ModelGatewayHttpErrorHandler
{
    public static Error FromStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => Error.Failure(
                "model_gateway.provider_not_found",
                "The configured local model provider endpoint or model was not found."),
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => Error.Failure(
                "model_gateway.provider_timeout",
                "The local model provider did not respond in time."),
            HttpStatusCode.TooManyRequests => Error.Failure(
                "model_gateway.provider_rate_limited",
                "The local model provider is temporarily rate limited."),
            >= HttpStatusCode.InternalServerError => Error.Failure(
                "model_gateway.provider_failure",
                "The local model provider returned an internal error."),
            _ => Error.Failure(
                "model_gateway.provider_http_error",
                "The local model provider returned an unsuccessful response.")
        };
    }
}
```

### Chat Client

`OllamaChatModelClient` should:

- be `internal sealed`;
- implement `IChatModelClient`;
- accept `HttpClient` and `OllamaModelClientOptions`;
- validate options before calling HTTP;
- map request before calling HTTP;
- post JSON to `/api/chat`;
- handle HTTP non-success without reading or exposing raw bodies unless future diagnostics explicitly redacts them;
- deserialize success response;
- map response through `OllamaResponseMapper`;
- rethrow caller cancellation;
- convert provider exceptions to controlled failures.

Expected behavior shape:

```csharp
public async Task<Result<ChatModelResponse>> SendAsync(
    ChatModelRequest request,
    CancellationToken cancellationToken)
{
    var optionsResult = _options.Validate();
    if (optionsResult.IsFailure)
    {
        return Result<ChatModelResponse>.Failure(optionsResult.Error);
    }

    var mappedRequest = OllamaRequestMapper.Map(request, _options);
    if (mappedRequest.IsFailure)
    {
        return Result<ChatModelResponse>.Failure(mappedRequest.Error);
    }

    try
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            mappedRequest.Value,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result<ChatModelResponse>.Failure(
                ModelGatewayHttpErrorHandler.FromStatusCode(response.StatusCode));
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            JsonOptions,
            cancellationToken);

        return OllamaResponseMapper.Map(ollamaResponse);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (TaskCanceledException)
    {
        return Result<ChatModelResponse>.Failure(Error.Failure(
            "model_gateway.provider_timeout",
            "The local model provider did not respond in time."));
    }
    catch (HttpRequestException)
    {
        return Result<ChatModelResponse>.Failure(Error.Failure(
            "model_gateway.provider_unavailable",
            "The local model provider is unavailable."));
    }
    catch (JsonException)
    {
        return Result<ChatModelResponse>.Failure(Error.Failure(
            "model_gateway.provider_invalid_response",
            "The local model provider returned an invalid response."));
    }
}
```

Use the repository's actual `Result<T>` API names when implementing. Do not force this exact snippet if the existing `Result<T>` shape differs.

### Dependency Injection

`DependencyInjection.cs` should expose a host-facing extension:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddIrisModelGateway(
        this IServiceCollection services,
        Action<OllamaModelClientOptions> configureOllama)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOllama);

        var options = new OllamaModelClientOptions();
        configureOllama(options);

        var validation = options.Validate();
        if (validation.IsFailure)
        {
            throw new InvalidOperationException(validation.Error.Message);
        }

        services.AddSingleton(options);
        services.AddHttpClient<IChatModelClient, OllamaChatModelClient>(
            (serviceProvider, httpClient) =>
            {
                var ollamaOptions = serviceProvider.GetRequiredService<OllamaModelClientOptions>();
                httpClient.BaseAddress = new Uri(ollamaOptions.BaseUrl, UriKind.Absolute);
                httpClient.Timeout = ollamaOptions.Timeout;
            });

        return services;
    }
}
```

This is acceptable because hosts own configuration composition. The adapter validates configuration early so Desktop/API/Worker fail fast when misconfigured.

## Test Plan

Phase 4 uses `Iris.Integration.Tests` because there is no dedicated `Iris.ModelGateway.Tests` project and the architecture document assigns ModelGateway stub/local provider checks to integration tests.

### Test Project Update

`tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` should add:

```xml
<ProjectReference Include="..\..\src\Iris.ModelGateway\Iris.ModelGateway.csproj" />
```

### Fake HTTP Handler

Create `tests/Iris.IntegrationTests/ModelGateway/FakeHttpMessageHandler.cs`:

```csharp
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return _handler(request, cancellationToken);
    }
}
```

### Request Mapper Tests

`OllamaRequestMapperTests`:

- maps system/user/assistant roles to `system`, `user`, `assistant`;
- uses request model over default configured model;
- uses configured model when request model is null;
- sets `stream` false;
- includes temperature when provided;
- omits temperature when null;
- fails when no messages are provided;
- fails when message content is empty;
- fails when no model can be resolved;
- fails when temperature is outside `0.0..2.0`.

### Response Mapper Tests

`OllamaResponseMapperTests`:

- maps assistant content into `ChatModelResponse`;
- fails when response is null;
- fails when `message` is null;
- fails when role is not `assistant`;
- fails when content is empty or whitespace.

### Client Tests

`OllamaChatModelClientTests`:

- posts to `/api/chat`;
- sends `Content-Type: application/json`;
- serializes `stream: false`;
- returns assistant response on 200;
- returns `model_gateway.provider_unavailable` when handler throws `HttpRequestException`;
- returns `model_gateway.provider_timeout` when handler throws non-caller `TaskCanceledException`;
- rethrows `OperationCanceledException` when caller cancellation token is cancelled;
- returns `model_gateway.provider_not_found` for 404;
- returns `model_gateway.provider_failure` for 500;
- returns `model_gateway.provider_invalid_response` for invalid JSON;
- returns `model_gateway.provider_empty_response` for empty assistant content.

### DI Tests

Add one test if implementation allows straightforward verification:

- `AddIrisModelGateway` registers `IChatModelClient`;
- invalid options throw at composition time.

If DI testing would require host configuration not yet present, defer DI host composition testing to Phase 5 and record it in `.agent/debt_tech_backlog.md`.

## Implementation Steps

- [ ] Checkpoint: confirm `git status -sb --untracked-files=all` is clean or only contains known user changes.
- [ ] Update `src/Iris.ModelGateway/Iris.ModelGateway.csproj` with inward references and required DI/HTTP package references.
- [ ] Implement `OllamaModelClientOptions`.
- [ ] Implement `OllamaApiModels`.
- [ ] Implement `OllamaRequestMapper`.
- [ ] Implement `OllamaResponseMapper`.
- [ ] Implement `ModelGatewayHttpClientNames`.
- [ ] Implement `ModelGatewayHttpErrorHandler`.
- [ ] Implement `OllamaChatModelClient`.
- [ ] Implement `DependencyInjection.AddIrisModelGateway`.
- [ ] Checkpoint: run `dotnet build .\Iris.slnx` and fix compile failures inside Phase 4 scope only.
- [ ] Update `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` with `Iris.ModelGateway` reference.
- [ ] Add `FakeHttpMessageHandler`.
- [ ] Add request mapper tests.
- [ ] Add response mapper tests.
- [ ] Add chat client tests.
- [ ] Add DI tests if host-independent.
- [ ] Checkpoint: run `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj`.
- [ ] Checkpoint: run `dotnet test .\Iris.slnx`.
- [ ] Optional manual smoke: run local Ollama and call the adapter through a small host/composition path only if Phase 5 wiring is already available. Otherwise skip manual provider call and state why.
- [ ] Update `.agent/PROJECT_LOG.md`.
- [ ] Update `.agent/overview.md`.
- [ ] Update `.agent/log_notes.md` for any build/test/provider issues.
- [ ] Update `.agent/debt_tech_backlog.md` for any deferred DI host test, dedicated ModelGateway test project, or diagnostics/logging debt.
- [ ] Final audit: verify no forbidden project references were added.

## Validation Commands

Run after implementation:

```powershell
dotnet build .\Iris.slnx
```

Expected result:

```text
Build succeeded.
```

Run focused tests:

```powershell
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj
```

Expected result:

```text
Failed: 0
```

Run full tests:

```powershell
dotnet test .\Iris.slnx
```

Expected result:

```text
Failed: 0
```

Check forbidden references:

```powershell
dotnet list .\src\Iris.Application\Iris.Application.csproj reference
dotnet list .\src\Iris.ModelGateway\Iris.ModelGateway.csproj reference
```

Expected dependency direction:

```text
Iris.Application -> Iris.Domain, Iris.Shared
Iris.ModelGateway -> Iris.Application, Iris.Shared
```

## Architecture Checkpoint

Before marking Phase 4 complete, verify:

- [ ] `Iris.Application` has no `Iris.ModelGateway` reference.
- [ ] `Iris.ModelGateway` has no adapter-to-adapter references.
- [ ] `Iris.ModelGateway` has no `Iris.Persistence` reference.
- [ ] `Iris.ModelGateway` has no UI references.
- [ ] `OllamaChatModelClient` implements only `IChatModelClient`.
- [ ] Prompt building remains in Application.
- [ ] Persistence remains in Persistence.
- [ ] No future provider work was implemented accidentally.

## Acceptance Criteria

- `Iris.ModelGateway` compiles.
- `OllamaChatModelClient` implements `IChatModelClient`.
- Ollama request mapping targets `/api/chat` with `stream: false`.
- Roles are mapped exactly.
- Model name is configurable and never hardcoded in Application.
- Temperature is optional and validated.
- Successful response maps to `ChatModelResponse`.
- Ollama unavailable returns controlled error.
- HTTP non-success returns controlled error.
- Invalid JSON returns controlled error.
- Empty assistant content returns controlled error.
- Caller cancellation propagates.
- No full prompt or raw model response is logged by default.
- Integration tests cover mapper/client failure behavior with fake HTTP.
- Full solution build and tests pass, or every failure is recorded in `.agent/log_notes.md`.

## Bug And Regression Risks

- Wrong endpoint path, for example `api/chat` without leading slash or `/v1/chat/completions`.
- Accidentally using streaming default by omitting `stream: false`.
- Mapping `System` role as `user`, which changes prompt semantics.
- Using configured model even when request-specific model is provided.
- Treating caller cancellation as provider timeout.
- Catching all exceptions and hiding programming errors.
- Leaking provider response body into UI error messages.
- Adding `Iris.ModelGateway` reference to Application tests or Application project.
- Registering `HttpClient` without `BaseAddress`, causing runtime invalid URI failures.
- Creating production defaults that silently point to an unwanted provider/model.
- Testing against real local Ollama in automated tests, causing machine-dependent failures.

## Deferred Items

These are intentionally not implemented in Phase 4:

- Dedicated `Iris.ModelGateway.Tests` project.
- Retry/circuit-breaker policy.
- Structured diagnostics events.
- Provider health check.
- Model discovery/listing.
- Token usage mapping.
- Streaming response support.
- Tool call mapping.
- Vision/image message mapping.
- OpenAI-compatible provider.
- LM Studio provider.
- Model router.

Dedicated `Iris.ModelGateway.Tests` may become worthwhile when ModelGateway grows beyond one Ollama chat adapter. Until then, fake-HTTP adapter tests in `Iris.Integration.Tests` are acceptable and consistent with current test structure.

## Metadata Updates Required After Implementation

Append `.agent/PROJECT_LOG.md`:

```md
## 2026-04-27 — Phase 4 ModelGateway Ollama adapter

### Changed
- Implemented Ollama chat adapter behind `IChatModelClient`.
- Added controlled provider error handling.
- Added fake-HTTP integration tests for request/response mapping and failure behavior.

### Files
- src/Iris.ModelGateway/...
- tests/Iris.IntegrationTests/ModelGateway/...

### Validation
- `dotnet build .\Iris.slnx`: ...
- `dotnet test .\Iris.slnx`: ...

### Next
- Phase 5 Desktop wiring through `IrisApplicationFacade`.
```

Update `.agent/overview.md`:

```md
Current phase: Phase 4 complete.
Current implementation target: ModelGateway Ollama adapter behind `IChatModelClient`.
Current working status: ...
Next immediate step: Phase 5 Desktop wiring.
Known blockers: ...
```

Append `.agent/debt_tech_backlog.md` only when a deferred item becomes real debt during implementation, for example if DI tests are skipped or a dedicated test project is clearly needed.

Append `.agent/log_notes.md` for any failed command, broken package version, JSON issue, or provider behavior surprise discovered during work.

## Implementation Mode

Recommended execution mode:

```text
Subagent-driven development
```

Suggested split:

- Worker 1: ModelGateway production files.
- Worker 2: Integration tests with fake HTTP.
- Main agent: dependency audit, metadata, build/test integration, final review.

Workers must not edit the same files concurrently. Production worker owns `src/Iris.ModelGateway/**`; test worker owns `tests/Iris.IntegrationTests/ModelGateway/**` and the integration test project reference.
