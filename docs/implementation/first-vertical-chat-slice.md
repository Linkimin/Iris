# First Vertical Chat Slice

This document defines the first real implementation slice for Iris / Айрис.
It combines spec, design, plan, acceptance criteria, test matrix, and forbidden shortcuts in one file because the slice is small but crosses multiple projects.

## 1. Spec

### Problem

Iris currently has a buildable structural skeleton, but the first usable chat loop is not implemented.
The next goal is one end-to-end local chat message through the intended architecture.

### Scope

In scope:

- Domain conversation and message minimum.
- Application send-message use case with fake dependencies in tests.
- SQLite persistence adapter for conversations and messages.
- Ollama chat model adapter through Application abstractions.
- Desktop chat wiring through `IrisApplicationFacade`.
- Focused unit/integration/architecture validation.

Out of scope:

- Streaming responses.
- Memory recall or extraction.
- Tools.
- Voice.
- Desktop perception.
- API chat endpoint.
- Worker runtime behavior.
- SI runtime.
- Companion modes.
- Advanced persona state.
- Conversation summarization.
- Full settings UI.

### Invariants

- `Iris.Domain` depends only on `Iris.Shared`.
- `Iris.Application` depends only on `Iris.Domain` and `Iris.Shared`.
- Adapters implement Application abstractions.
- Hosts compose dependencies.
- Desktop never calls Ollama, EF Core, SQLite, tools, or perception directly.
- Application never references Persistence or ModelGateway concrete types.
- Prompt logic belongs in Application, not Desktop or ModelGateway.
- First slice uses Default Chat behavior only.

## 2. Design

### Target Flow

```text
ChatView
-> ChatViewModel
-> IrisApplicationFacade
-> SendMessageHandler
-> PromptBuilder
-> IChatModelClient
-> OllamaChatModelClient
-> IConversationRepository / IMessageRepository / IUnitOfWork
-> SQLite
-> SendMessageResult
-> ChatViewModel
-> ChatView
```

### Domain Minimum

Implement the domain model before the Application handler so the use case does not become string-driven.

Required concepts:

- `Conversation`
- `ConversationId`
- `ConversationTitle`
- `ConversationStatus`
- `ConversationMode`
- `Message`
- `MessageId`
- `MessageRole`
- `MessageContent`
- `MessageMetadata`

Minimum conversation state:

- `Id`
- optional `Title`
- `Status`
- `Mode`
- `CreatedAt`
- `UpdatedAt`

Minimum message state:

- `Id`
- `ConversationId`
- `Role`
- `Content`
- `Metadata`
- `CreatedAt`

Domain invariants:

- Message content is not empty or whitespace.
- Message role is limited to `User`, `Assistant`, and `System`.
- A message always belongs to a conversation.
- Domain types do not use EF, HTTP, Avalonia, or adapter attributes.

The first slice does not require storing messages inside the `Conversation` aggregate. Repositories may stay separated.

### Application Minimum

Application owns the use case and contracts.

Required files:

- `Chat/SendMessage/SendMessageCommand.cs`
- `Chat/SendMessage/SendMessageResult.cs`
- `Chat/SendMessage/SendMessageValidator.cs`
- `Chat/SendMessage/SendMessageHandler.cs`
- `Chat/Prompting/PromptBuilder.cs`
- `Chat/Prompting/PromptBuildRequest.cs`
- `Chat/Prompting/PromptBuildResult.cs`
- `Abstractions/Models/Interfaces/IChatModelClient.cs`
- `Abstractions/Persistence/IConversationRepository.cs`
- `Abstractions/Persistence/IMessageRepository.cs`
- `Abstractions/Persistence/IUnitOfWork.cs`

Command shape:

```csharp
public sealed record SendMessageCommand(
    ConversationId? ConversationId,
    string Message);
```

Result shape:

```csharp
public sealed record SendMessageResult(
    ConversationId ConversationId,
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage);
```

Use `Result<SendMessageResult>` or the existing Iris result primitive if it is ready and fits the local pattern.

Handler flow:

1. Validate command.
2. Load existing conversation or create a new one.
3. Create user message in memory.
4. Load recent history.
5. Build prompt.
6. Call `IChatModelClient`.
7. Create assistant message in memory.
8. Save conversation and both messages.
9. Commit `IUnitOfWork`.
10. Return `SendMessageResult`.

Failure policy for the first slice:

- Validation failure returns a controlled error.
- Model failure returns a controlled error and does not crash.
- Repository or unit-of-work failure returns a controlled error and does not leak raw private content.
- For MVP, if the model call fails, do not persist the user/assistant pair. Failed-message persistence can come later.

Prompt minimum:

```text
System identity
Persona baseline
Recent conversation history
Current user message
```

Do not include memory, tools, perception, voice, SI runtime, or modes yet.

### Persistence Adapter

Persistence implements Application abstractions and owns EF Core / SQLite details.

Required files:

- `Database/IrisDbContext.cs`
- `Database/DatabaseOptions.cs`
- `Entities/ConversationEntity.cs`
- `Entities/MessageEntity.cs`
- `Configurations/ConversationEntityConfiguration.cs`
- `Configurations/MessageEntityConfiguration.cs`
- `Mapping/ConversationMapper.cs`
- `Mapping/MessageMapper.cs`
- `Repositories/ConversationRepository.cs`
- `Repositories/MessageRepository.cs`
- `UnitOfWork/EfUnitOfWork.cs`
- `DependencyInjection.cs`

Minimum behavior:

- Create conversation.
- Add user message.
- Add assistant message.
- Load conversation by id.
- Load recent messages ordered by creation time.
- Commit changes through unit of work.

SQLite connection string must come from configuration/options, not handler code.

### ModelGateway Adapter

ModelGateway implements `IChatModelClient` for Ollama.

Required files:

- `Ollama/OllamaChatModelClient.cs`
- `Ollama/OllamaRequestMapper.cs`
- `Ollama/OllamaResponseMapper.cs`
- `Ollama/OllamaModelClientOptions.cs`
- `Http/ModelGatewayHttpErrorHandler.cs`
- `DependencyInjection.cs`

Minimum behavior:

- Base URL comes from options.
- Chat model name comes from options.
- Request/response mapping stays inside ModelGateway.
- Ollama unavailable returns a controlled error/result.
- No prompt building, persistence, memory, or persona decisions in ModelGateway.

Do not work on LM Studio or OpenAI-compatible behavior for this slice unless a build dependency forces it.

### Desktop Wiring

Desktop remains a thin host/UI shell.

Required files:

- `Views/ChatView.axaml`
- `ViewModels/ChatViewModel.cs`
- `Models/ChatMessageViewModelItem.cs`
- `Services/IrisApplicationFacade.cs`
- `DependencyInjection.cs`

Minimum behavior:

- User can type a message.
- Send action calls `IrisApplicationFacade.SendMessageAsync`.
- UI blocks duplicate sends while sending.
- Loading state is visible.
- Assistant response appears.
- Readable error appears if validation/model/database fails.
- No raw exception text by default.

Desktop must not build prompts or know about Ollama/EF.

## 3. Plan

### Phase 1 — Domain Minimum

Implement conversation and message value objects/entities with basic invariants.
Add focused Domain tests for content validation, ids, roles, and timestamps.

### Phase 2 — Application First With Fakes

Implement `SendMessageHandler`, validator, prompt builder, and result/command contracts.
Use fake model and fake repositories in `Iris.Application.Tests`.

### Phase 3 — Persistence Adapter

Implement EF entities, configurations, mappers, repositories, `IrisDbContext`, and unit of work.
Add SQLite integration tests for save/reload behavior.

### Phase 4 — ModelGateway Adapter

Implement Ollama chat client and mapping behind `IChatModelClient`.
Add tests for request/response mapping and unavailable-model error handling where practical.

### Phase 5 — Desktop Wiring

Wire `IrisApplicationFacade`, `ChatViewModel`, and `ChatView`.
Keep UI minimal but usable.

### Phase 6 — Integration Check

Run:

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
```

Manual check:

- Start Iris.Desktop.
- Send one message.
- Verify assistant response appears.
- Verify conversation/messages persist.
- Verify readable error if Ollama is unavailable.

### Phase 7 — Architecture Tests / Safeguards

Add or replace template architecture tests to enforce:

- Domain does not depend on Application/adapters/hosts.
- Application does not depend on Persistence/ModelGateway/Desktop/API/Worker.
- Desktop does not depend on API/Worker.
- Host projects do not depend on each other.
- Adapters do not depend on each other unless explicitly approved.

## 4. Acceptance Criteria

- `dotnet build .\Iris.slnx` succeeds.
- `dotnet test .\Iris.slnx` succeeds.
- Domain conversation/message minimum is implemented.
- `SendMessageHandler` is covered by unit tests with fake dependencies.
- Persistence integration test saves and reloads conversations/messages in SQLite.
- Ollama client maps request/response and handles unavailable provider cleanly.
- Desktop chat can send a message through Application and display the response.
- Messages are persisted after successful model response.
- No forbidden dependency shortcut is introduced.
- `.agent` metadata is updated after each meaningful implementation iteration.

## 5. Test Matrix

| Area | Test | Expected Result |
| --- | --- | --- |
| Domain | Empty `MessageContent` | Validation/domain error |
| Domain | Valid user/assistant/system role | Message can be created |
| Application | Empty command message | Controlled validation error |
| Application | New conversation command | Conversation is created |
| Application | Existing conversation id | Conversation is loaded |
| Application | Successful model response | User and assistant messages saved |
| Application | Model failure | Controlled error, no crash |
| Application | Repository failure | Controlled error, no crash |
| Prompting | Current message and history | Prompt contains expected sections |
| Persistence | Save and reload messages | Roles/content/order preserved |
| Persistence | Missing conversation | Controlled not-found result |
| ModelGateway | Ollama response mapping | Assistant content returned |
| ModelGateway | Ollama unavailable | Controlled provider error |
| Desktop | Send while loading | Duplicate send blocked |
| Desktop | Model unavailable | Readable UI error |
| Architecture | Application references adapters | Test fails |
| Architecture | Desktop references ModelGateway directly | Test fails |

## 6. Forbidden Shortcuts

Do not introduce:

- `ChatViewModel -> OllamaChatModelClient`
- `ChatViewModel -> IrisDbContext`
- `IrisApplicationFacade -> OllamaChatModelClient`
- `IrisApplicationFacade -> IrisDbContext`
- `SendMessageHandler -> OllamaChatModelClient`
- `SendMessageHandler -> IrisDbContext`
- `PromptBuilder -> Ollama`
- `ModelGateway -> Repository`
- `Persistence -> ModelGateway`
- `Application -> Persistence`
- `Application -> ModelGateway`
- `Domain -> EF Core`
- `Tools`, `Voice`, `Perception`, `SI Runtime`, `API`, or `Worker` participation in the first chat path.

If a shortcut seems necessary, stop and revise this document before implementing.
