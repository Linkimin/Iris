# First Vertical Chat Slice Phase 1-2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the minimum Domain conversation/message model and the Application `SendMessage` use case with fake dependencies. Phase 1-2 must stop before real Persistence, real ModelGateway, Desktop wiring, API, Worker, tools, voice, perception, memory recall, SI runtime, and companion modes.

**Architecture:** Domain owns conversation/message invariants. Application orchestrates `SendMessage` through Domain objects and Application-owned abstractions only. Tests drive the behavior with fakes. No adapter project is referenced from Domain or Application.

**Tech Stack:** .NET 10, C#, xUnit, Iris.Shared result/time primitives, Iris.Domain, Iris.Application.

---

## Scope Guard

Phase 1 includes:

- shared primitives required by Domain/Application tests: `Error`, `Result`, `Result<T>`, `IClock`, `SystemClock`;
- Domain primitives for chat: IDs, roles, content, metadata, conversation status/mode;
- Domain entities: `Conversation`, `Message`;
- Domain tests for invariants.

Phase 2 includes:

- Application chat contracts: commands, results, DTOs;
- Application abstractions: model client, repositories, unit of work;
- `PromptBuilder`;
- `SendMessageValidator`;
- `SendMessageHandler`;
- Application tests using fake dependencies.

Phase 1-2 excludes:

- EF Core entities, mappings, repositories, SQLite config, migrations;
- Ollama or any real HTTP model provider;
- Avalonia chat UI wiring;
- streaming;
- memory recall;
- tools;
- perception;
- voice;
- API endpoints;
- Worker runtime;
- Python SI runtime;
- architecture tests.

Hard boundary rules for this plan:

- `Iris.Domain` may reference only `Iris.Shared`.
- `Iris.Application` may reference only `Iris.Domain` and `Iris.Shared`.
- `Iris.Application.Tests` may use fakes, not `Iris.Persistence` or `Iris.ModelGateway`.
- Do not add provider names, SQLite paths, or UI concepts to Domain/Application.

---

## Phase 1 Checkpoint

Before editing Phase 1 files, verify:

- `AGENTS.md`, `.agent/architecture.md`, `.agent/first-vertical-slice.md`, and `docs/implementation/first-vertical-chat-slice.md` have been read.
- The target files below already exist as empty or template files.
- No new adapter dependency is added to `Iris.Domain`.
- Domain tests fail before implementation and pass after implementation.

---

## Task 1: Wire Unit Test Projects To Source Projects

- [ ] Modify `tests/Iris.Domain.Tests/Iris.Domain.Tests.csproj`.

Add project references:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Iris.Domain\Iris.Domain.csproj" />
  <ProjectReference Include="..\..\src\Iris.Shared\Iris.Shared.csproj" />
</ItemGroup>
```

- [ ] Modify `tests/Iris.Application.Tests/Iris.Application.Tests.csproj`.

Add project references:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Iris.Application\Iris.Application.csproj" />
  <ProjectReference Include="..\..\src\Iris.Domain\Iris.Domain.csproj" />
  <ProjectReference Include="..\..\src\Iris.Shared\Iris.Shared.csproj" />
</ItemGroup>
```

- [ ] Validate the test projects still restore and run.

Command:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Commit after validation.

Command:

```powershell
git add tests/Iris.Domain.Tests/Iris.Domain.Tests.csproj tests/Iris.Application.Tests/Iris.Application.Tests.csproj
git commit -m "test: reference Iris projects from unit tests"
```

---

## Task 2: Implement Shared Result And Time Primitives

Reasoning:

Application needs controlled errors and time injection before `SendMessageHandler` exists. These primitives belong to `Iris.Shared` because they are cross-cutting, product-neutral, and do not depend on Domain/Application.

- [ ] Replace `src/Iris.Shared/Results/Error.cs`.

```csharp
namespace Iris.Shared.Results;

public sealed record Error(string Code, string Message)
{
    public static Error None { get; } = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message) => new(code, message);

    public static Error Failure(string code, string message) => new(code, message);

    public bool IsNone => string.IsNullOrWhiteSpace(Code);
}
```

- [ ] Replace `src/Iris.Shared/Results/Result.cs`.

```csharp
namespace Iris.Shared.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && !error.IsNone)
        {
            throw new InvalidOperationException("A successful result cannot have an error.");
        }

        if (!isSuccess && error.IsNone)
        {
            throw new InvalidOperationException("A failed result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}
```

- [ ] Replace `src/Iris.Shared/Results/ResultOfT.cs`.

```csharp
namespace Iris.Shared.Results;

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
        _value = default;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);
}
```

- [ ] Replace `src/Iris.Shared/Time/Interfaces/IClock.cs`.

```csharp
namespace Iris.Shared.Time.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

- [ ] Replace `src/Iris.Shared/Time/SystemClock.cs`.

```csharp
using Iris.Shared.Time.Interfaces;

namespace Iris.Shared.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

- [ ] Validate build.

Command:

```powershell
dotnet build .\Iris.slnx
```

Expected result:

```text
Build succeeded.
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Shared/Results src/Iris.Shared/Time
git commit -m "feat(shared): add result and clock primitives"
```

---

## Task 3: Add Domain Primitive Tests First

- [ ] Create `tests/Iris.Domain.Tests/Conversations/MessageContentTests.cs`.

```csharp
using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class MessageContentTests
{
    [Fact]
    public void Create_WithText_ReturnsTrimmedContent()
    {
        var content = MessageContent.Create(" hello ");

        Assert.Equal("hello", content.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithBlankText_ThrowsDomainException(string value)
    {
        var exception = Assert.Throws<DomainException>(() => MessageContent.Create(value));

        Assert.Equal("message.empty_content", exception.Code);
    }
}
```

- [ ] Create `tests/Iris.Domain.Tests/Conversations/ConversationIdTests.cs`.

```csharp
using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class ConversationIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = ConversationId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => ConversationId.From(Guid.Empty));

        Assert.Equal("conversation.empty_id", exception.Code);
    }
}
```

- [ ] Create `tests/Iris.Domain.Tests/Conversations/MessageIdTests.cs`.

```csharp
using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class MessageIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = MessageId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => MessageId.From(Guid.Empty));

        Assert.Equal("message.empty_id", exception.Code);
    }
}
```

- [ ] Run Domain tests and confirm they fail because the production types are not implemented.

Command:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj
```

Expected result:

```text
Failed!
```

Expected failure cause:

```text
The type or namespace name 'MessageContent' could not be found
```

---

## Task 4: Implement Domain Common Types And Value Objects

- [ ] Replace `src/Iris.Domain/Common/DomainException.cs`.

```csharp
namespace Iris.Domain.Common;

public sealed class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
```

- [ ] Replace `src/Iris.Domain/Common/DomainError.cs`.

```csharp
namespace Iris.Domain.Common;

public sealed record DomainError(string Code, string Message);
```

- [ ] Replace `src/Iris.Domain/Conversations/ConversationId.cs`.

```csharp
using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public readonly record struct ConversationId(Guid Value)
{
    public static ConversationId New() => new(Guid.NewGuid());

    public static ConversationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("conversation.empty_id", "Conversation id cannot be empty.");
        }

        return new ConversationId(value);
    }

    public override string ToString() => Value.ToString();
}
```

- [ ] Replace `src/Iris.Domain/Conversations/MessageId.cs`.

```csharp
using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public readonly record struct MessageId(Guid Value)
{
    public static MessageId New() => new(Guid.NewGuid());

    public static MessageId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("message.empty_id", "Message id cannot be empty.");
        }

        return new MessageId(value);
    }

    public override string ToString() => Value.ToString();
}
```

- [ ] Replace `src/Iris.Domain/Conversations/MessageContent.cs`.

```csharp
using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed record MessageContent
{
    private MessageContent(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MessageContent Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("message.empty_content", "Message content cannot be empty.");
        }

        return new MessageContent(value.Trim());
    }

    public override string ToString() => Value;
}
```

- [ ] Replace `src/Iris.Domain/Conversations/MessageRole.cs`.

```csharp
namespace Iris.Domain.Conversations;

public enum MessageRole
{
    User = 1,
    Assistant = 2,
    System = 3
}
```

- [ ] Replace `src/Iris.Domain/Conversations/MessageMetadata.cs`.

```csharp
namespace Iris.Domain.Conversations;

public sealed record MessageMetadata
{
    public static MessageMetadata Empty { get; } = new();
}
```

- [ ] Replace `src/Iris.Domain/Conversations/ConversationStatus.cs`.

```csharp
namespace Iris.Domain.Conversations;

public enum ConversationStatus
{
    Active = 1,
    Archived = 2,
    Closed = 3
}
```

- [ ] Replace `src/Iris.Domain/Conversations/ConversationMode.cs`.

```csharp
namespace Iris.Domain.Conversations;

public enum ConversationMode
{
    Default = 1
}
```

- [ ] Replace `src/Iris.Domain/Conversations/ConversationTitle.cs`.

```csharp
using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed record ConversationTitle
{
    private const int MaxLength = 120;

    private ConversationTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ConversationTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("conversation.empty_title", "Conversation title cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException("conversation.title_too_long", "Conversation title is too long.");
        }

        return new ConversationTitle(trimmed);
    }

    public override string ToString() => Value;
}
```

- [ ] Run Domain tests.

Command:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Domain/Common src/Iris.Domain/Conversations tests/Iris.Domain.Tests/Conversations
git commit -m "feat(domain): add conversation value objects"
```

---

## Task 5: Add Domain Entity Tests First

- [ ] Create `tests/Iris.Domain.Tests/Conversations/ConversationTests.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class ConversationTests
{
    [Fact]
    public void Create_ReturnsActiveDefaultConversation()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        var conversation = Conversation.Create(
            ConversationId.New(),
            title: null,
            ConversationMode.Default,
            createdAt);

        Assert.Equal(ConversationStatus.Active, conversation.Status);
        Assert.Equal(ConversationMode.Default, conversation.Mode);
        Assert.Equal(createdAt, conversation.CreatedAt);
        Assert.Equal(createdAt, conversation.UpdatedAt);
    }

    [Fact]
    public void UpdateTitle_ChangesTitleAndUpdatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(5);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        conversation.UpdateTitle(ConversationTitle.Create("New title"), updatedAt);

        Assert.Equal("New title", conversation.Title!.Value);
        Assert.Equal(updatedAt, conversation.UpdatedAt);
    }

    [Fact]
    public void Touch_UpdatesUpdatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var touchedAt = createdAt.AddMinutes(1);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        conversation.Touch(touchedAt);

        Assert.Equal(touchedAt, conversation.UpdatedAt);
    }
}
```

- [ ] Create `tests/Iris.Domain.Tests/Conversations/MessageTests.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class MessageTests
{
    [Fact]
    public void Create_ReturnsMessageBelongingToConversation()
    {
        var conversationId = ConversationId.New();
        var messageId = MessageId.New();
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var content = MessageContent.Create("Hello");

        var message = Message.Create(
            messageId,
            conversationId,
            MessageRole.User,
            content,
            MessageMetadata.Empty,
            createdAt);

        Assert.Equal(messageId, message.Id);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal(content, message.Content);
        Assert.Equal(createdAt, message.CreatedAt);
    }
}
```

- [ ] Run Domain tests and confirm they fail because the entities are not implemented.

Command:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj
```

Expected result:

```text
Failed!
```

Expected failure cause:

```text
The type or namespace name 'Conversation' could not be found
```

---

## Task 6: Implement Domain Conversation And Message Entities

- [ ] Replace `src/Iris.Domain/Conversations/Conversation.cs`.

```csharp
namespace Iris.Domain.Conversations;

public sealed class Conversation
{
    private Conversation(
        ConversationId id,
        ConversationTitle? title,
        ConversationStatus status,
        ConversationMode mode,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Title = title;
        Status = status;
        Mode = mode;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ConversationId Id { get; }

    public ConversationTitle? Title { get; private set; }

    public ConversationStatus Status { get; private set; }

    public ConversationMode Mode { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Conversation Create(
        ConversationId id,
        ConversationTitle? title,
        ConversationMode mode,
        DateTimeOffset createdAt)
    {
        return new Conversation(
            id,
            title,
            ConversationStatus.Active,
            mode,
            createdAt,
            createdAt);
    }

    public void UpdateTitle(ConversationTitle title, DateTimeOffset updatedAt)
    {
        Title = title;
        Touch(updatedAt);
    }

    public void Archive(DateTimeOffset updatedAt)
    {
        Status = ConversationStatus.Archived;
        Touch(updatedAt);
    }

    public void Close(DateTimeOffset updatedAt)
    {
        Status = ConversationStatus.Closed;
        Touch(updatedAt);
    }

    public void Touch(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt;
    }
}
```

- [ ] Replace `src/Iris.Domain/Conversations/Message.cs`.

```csharp
namespace Iris.Domain.Conversations;

public sealed class Message
{
    private Message(
        MessageId id,
        ConversationId conversationId,
        MessageRole role,
        MessageContent content,
        MessageMetadata metadata,
        DateTimeOffset createdAt)
    {
        Id = id;
        ConversationId = conversationId;
        Role = role;
        Content = content;
        Metadata = metadata;
        CreatedAt = createdAt;
    }

    public MessageId Id { get; }

    public ConversationId ConversationId { get; }

    public MessageRole Role { get; }

    public MessageContent Content { get; }

    public MessageMetadata Metadata { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Message Create(
        MessageId id,
        ConversationId conversationId,
        MessageRole role,
        MessageContent content,
        MessageMetadata metadata,
        DateTimeOffset createdAt)
    {
        return new Message(id, conversationId, role, content, metadata, createdAt);
    }
}
```

- [ ] Run Domain tests.

Command:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Domain/Conversations tests/Iris.Domain.Tests/Conversations
git commit -m "feat(domain): add conversation and message entities"
```

---

## Phase 2 Checkpoint

Before editing Phase 2 files, verify:

- Domain tests pass.
- `Iris.Application` has references only to `Iris.Domain` and `Iris.Shared`.
- The Application abstractions live in `src/Iris.Application/Abstractions`.
- `SendMessageHandler` will depend only on Application abstractions, Domain, Shared, and `IClock`.
- Real Persistence, real ModelGateway, and Desktop wiring are not touched.

---

## Task 7: Add Application Chat Contracts And Abstractions

- [ ] Replace `src/Iris.Application/Abstractions/Models/Contracts/Chat/ChatModelRole.cs`.

```csharp
namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public enum ChatModelRole
{
    System = 1,
    User = 2,
    Assistant = 3
}
```

- [ ] Replace `src/Iris.Application/Abstractions/Models/Contracts/Chat/ChatModelMessage.cs`.

```csharp
namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public sealed record ChatModelMessage(ChatModelRole Role, string Content);
```

- [ ] Replace `src/Iris.Application/Abstractions/Models/Contracts/Chat/ChatModelOptions.cs`.

```csharp
namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public sealed record ChatModelOptions(string? Model = null, double? Temperature = null);
```

- [ ] Replace `src/Iris.Application/Abstractions/Models/Contracts/Chat/ChatModelRequest.cs`.

```csharp
namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public sealed record ChatModelRequest(
    IReadOnlyList<ChatModelMessage> Messages,
    ChatModelOptions Options);
```

- [ ] Replace `src/Iris.Application/Abstractions/Models/Contracts/Chat/ChatModelResponse.cs`.

```csharp
namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public sealed record ChatModelResponse(string Content);
```

- [ ] Replace `src/Iris.Application/Abstractions/Models/IChatModelClient.cs`.

```csharp
using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Shared.Results;

namespace Iris.Application.Abstractions.Models;

public interface IChatModelClient
{
    Task<Result<ChatModelResponse>> SendAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken);
}
```

- [ ] Replace `src/Iris.Application/Abstractions/Persistence/IConversationRepository.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Application.Abstractions.Persistence;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);
}
```

- [ ] Replace `src/Iris.Application/Abstractions/Persistence/IMessageRepository.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Application.Abstractions.Persistence;

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> ListRecentAsync(
        ConversationId conversationId,
        int limit,
        CancellationToken cancellationToken);

    Task AddAsync(Message message, CancellationToken cancellationToken);
}
```

- [ ] Replace `src/Iris.Application/Abstractions/Persistence/IUnitOfWork.cs`.

```csharp
namespace Iris.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}
```

- [ ] Replace `src/Iris.Application/Chat/DTOs/ChatMessageDto.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Application.Chat.DTOs;

public sealed record ChatMessageDto(
    MessageId Id,
    ConversationId ConversationId,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt);
```

- [ ] Validate build.

Command:

```powershell
dotnet build .\Iris.slnx
```

Expected result:

```text
Build succeeded.
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Application/Abstractions src/Iris.Application/Chat/DTOs
git commit -m "feat(application): add chat abstractions"
```

---

## Task 8: Add PromptBuilder Tests First

- [ ] Create `tests/Iris.Application.Tests/Chat/Prompting/PromptBuilderTests.cs`.

```csharp
using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Chat.Prompting;
using Iris.Domain.Conversations;

namespace Iris.Application.Tests.Chat.Prompting;

public sealed class PromptBuilderTests
{
    [Fact]
    public void Build_IncludesSystemMessageHistoryAndCurrentUserMessage()
    {
        var builder = new PromptBuilder();
        var conversationId = ConversationId.New();
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var history = new[]
        {
            Message.Create(MessageId.New(), conversationId, MessageRole.User, MessageContent.Create("Previous user"), MessageMetadata.Empty, createdAt),
            Message.Create(MessageId.New(), conversationId, MessageRole.Assistant, MessageContent.Create("Previous assistant"), MessageMetadata.Empty, createdAt.AddSeconds(1))
        };

        var result = builder.Build(new PromptBuildRequest(
            history,
            MessageContent.Create("Current user")));

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value.ModelRequest.Messages,
            message =>
            {
                Assert.Equal(ChatModelRole.System, message.Role);
                Assert.NotEqual(string.Empty, message.Content);
            },
            message =>
            {
                Assert.Equal(ChatModelRole.User, message.Role);
                Assert.Equal("Previous user", message.Content);
            },
            message =>
            {
                Assert.Equal(ChatModelRole.Assistant, message.Role);
                Assert.Equal("Previous assistant", message.Content);
            },
            message =>
            {
                Assert.Equal(ChatModelRole.User, message.Role);
                Assert.Equal("Current user", message.Content);
            });
    }
}
```

- [ ] Run Application tests and confirm they fail because `PromptBuilder` is not implemented.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Failed!
```

---

## Task 9: Implement PromptBuilder

Reasoning:

`PromptBuilder` belongs in Application because it assembles provider-neutral prompt context. ModelGateway maps provider requests but does not own prompt meaning.

- [ ] Replace `src/Iris.Application/Chat/Prompting/PromptBuildRequest.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Application.Chat.Prompting;

public sealed record PromptBuildRequest(
    IReadOnlyList<Message> RecentMessages,
    MessageContent CurrentUserMessage);
```

- [ ] Replace `src/Iris.Application/Chat/Prompting/PromptBuildResult.cs`.

```csharp
using Iris.Application.Abstractions.Models.Contracts.Chat;

namespace Iris.Application.Chat.Prompting;

public sealed record PromptBuildResult(ChatModelRequest ModelRequest);
```

- [ ] Replace `src/Iris.Application/Chat/Prompting/PromptBuilder.cs`.

```csharp
using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Application.Chat.Prompting;

public sealed class PromptBuilder
{
    private const string BaselineSystemPrompt =
        "You are Iris, a local personal AI companion. Be helpful, clear, and respectful.";

    public Result<PromptBuildResult> Build(PromptBuildRequest request)
    {
        var messages = new List<ChatModelMessage>
        {
            new(ChatModelRole.System, BaselineSystemPrompt)
        };

        messages.AddRange(request.RecentMessages.Select(MapHistoryMessage));
        messages.Add(new ChatModelMessage(ChatModelRole.User, request.CurrentUserMessage.Value));

        var modelRequest = new ChatModelRequest(messages, new ChatModelOptions());

        return Result<PromptBuildResult>.Success(new PromptBuildResult(modelRequest));
    }

    private static ChatModelMessage MapHistoryMessage(Message message)
    {
        var role = message.Role switch
        {
            MessageRole.System => ChatModelRole.System,
            MessageRole.User => ChatModelRole.User,
            MessageRole.Assistant => ChatModelRole.Assistant,
            _ => throw new InvalidOperationException($"Unsupported message role: {message.Role}")
        };

        return new ChatModelMessage(role, message.Content.Value);
    }
}
```

- [ ] Run Application tests.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Application/Chat/Prompting tests/Iris.Application.Tests/Chat/Prompting
git commit -m "feat(application): build provider-neutral chat prompts"
```

---

## Task 10: Add SendMessageValidator Tests First

- [ ] Create `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageValidatorTests.cs`.

```csharp
using Iris.Application.Chat.SendMessage;

namespace Iris.Application.Tests.Chat.SendMessage;

public sealed class SendMessageValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithBlankMessage_ReturnsValidationError(string message)
    {
        var validator = new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 10));

        var result = validator.Validate(new SendMessageCommand(null, message));

        Assert.True(result.IsFailure);
        Assert.Equal("chat.message_empty", result.Error.Code);
    }

    [Fact]
    public void Validate_WithTooLongMessage_ReturnsValidationError()
    {
        var validator = new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 3));

        var result = validator.Validate(new SendMessageCommand(null, "abcd"));

        Assert.True(result.IsFailure);
        Assert.Equal("chat.message_too_long", result.Error.Code);
    }

    [Fact]
    public void Validate_WithValidMessage_ReturnsSuccess()
    {
        var validator = new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 10));

        var result = validator.Validate(new SendMessageCommand(null, "hello"));

        Assert.True(result.IsSuccess);
    }
}
```

- [ ] Run Application tests and confirm they fail because `SendMessageValidator` is not implemented.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Failed!
```

---

## Task 11: Implement SendMessage Command, Result, Options, And Validator

- [ ] Replace `src/Iris.Application/Chat/SendMessage/SendMessageCommand.cs`.

```csharp
using Iris.Domain.Conversations;

namespace Iris.Application.Chat.SendMessage;

public sealed record SendMessageCommand(
    ConversationId? ConversationId,
    string Message);
```

- [ ] Replace `src/Iris.Application/Chat/SendMessage/SendMessageResult.cs`.

```csharp
using Iris.Application.Chat.DTOs;
using Iris.Domain.Conversations;

namespace Iris.Application.Chat.SendMessage;

public sealed record SendMessageResult(
    ConversationId ConversationId,
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage);
```

- [ ] Create `src/Iris.Application/Chat/SendMessage/SendMessageOptions.cs`.

```csharp
namespace Iris.Application.Chat.SendMessage;

public sealed record SendMessageOptions(int MaxMessageLength);
```

- [ ] Replace `src/Iris.Application/Chat/SendMessage/SendMessageValidator.cs`.

```csharp
using Iris.Shared.Results;

namespace Iris.Application.Chat.SendMessage;

public sealed class SendMessageValidator
{
    private readonly SendMessageOptions _options;

    public SendMessageValidator(SendMessageOptions options)
    {
        _options = options;
    }

    public Result Validate(SendMessageCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Message))
        {
            return Result.Failure(Error.Validation(
                "chat.message_empty",
                "Message cannot be empty."));
        }

        if (command.Message.Length > _options.MaxMessageLength)
        {
            return Result.Failure(Error.Validation(
                "chat.message_too_long",
                "Message is too long."));
        }

        return Result.Success();
    }
}
```

- [ ] Run Application tests.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Application/Chat/SendMessage tests/Iris.Application.Tests/Chat/SendMessage
git commit -m "feat(application): validate send message commands"
```

---

## Task 12: Add SendMessageHandler Tests First

- [ ] Create `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs`.

Use fakes inside the test file. Do not use a mock framework.

```csharp
using Iris.Application.Abstractions.Models;
using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.Prompting;
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

namespace Iris.Application.Tests.Chat.SendMessage;

public sealed class SendMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithNewConversation_SavesUserAndAssistantMessagesAndCommits()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello", result.Value.UserMessage.Content);
        Assert.Equal("Assistant reply", result.Value.AssistantMessage.Content);
        Assert.Single(conversations.Added);
        Assert.Equal(2, messages.Added.Count);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(1, model.SendCalls);
    }

    [Fact]
    public async Task HandleAsync_WithExistingConversation_LoadsConversation()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, clock.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Stored[conversation.Id] = conversation;
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(conversation.Id, "Hello"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(conversation.Id, result.Value.ConversationId);
        Assert.Empty(conversations.Added);
        Assert.Equal(1, conversations.GetCalls);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownConversation_ReturnsControlledError()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(ConversationId.New(), "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("chat.conversation_not_found", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(0, model.SendCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenModelFails_ReturnsControlledErrorAndDoesNotSaveMessages()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Failure(Error.Failure(
            "model.unavailable",
            "Local model is unavailable.")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model.unavailable", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Empty(conversations.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    private static SendMessageHandler CreateHandler(
        FakeConversationRepository conversations,
        FakeMessageRepository messages,
        FakeUnitOfWork unitOfWork,
        FakeChatModelClient model,
        FakeClock clock)
    {
        return new SendMessageHandler(
            conversations,
            messages,
            unitOfWork,
            model,
            new PromptBuilder(),
            new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 10_000)),
            clock);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        public Dictionary<ConversationId, Conversation> Stored { get; } = new();

        public List<Conversation> Added { get; } = new();

        public int GetCalls { get; private set; }

        public Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            Stored.TryGetValue(id, out var conversation);
            return Task.FromResult(conversation);
        }

        public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            Added.Add(conversation);
            Stored[conversation.Id] = conversation;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessageRepository : IMessageRepository
    {
        public List<Message> Added { get; } = new();

        public List<Message> History { get; } = new();

        public Task<IReadOnlyList<Message>> ListRecentAsync(
            ConversationId conversationId,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Message> messages = History
                .Where(message => message.ConversationId == conversationId)
                .OrderBy(message => message.CreatedAt)
                .TakeLast(limit)
                .ToList();

            return Task.FromResult(messages);
        }

        public Task AddAsync(Message message, CancellationToken cancellationToken)
        {
            Added.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeChatModelClient : IChatModelClient
    {
        private readonly Result<ChatModelResponse> _response;

        public FakeChatModelClient(Result<ChatModelResponse> response)
        {
            _response = response;
        }

        public int SendCalls { get; private set; }

        public Task<Result<ChatModelResponse>> SendAsync(
            ChatModelRequest request,
            CancellationToken cancellationToken)
        {
            SendCalls++;
            return Task.FromResult(_response);
        }
    }
}
```

- [ ] Run Application tests and confirm they fail because `SendMessageHandler` is not implemented.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Failed!
```

---

## Task 13: Implement SendMessageHandler

Reasoning:

The handler coordinates Application abstractions. It does not know EF, SQLite, Ollama, Avalonia, HTTP, or Desktop state. For Phase 2, model failure saves nothing and returns the model's controlled error.

- [ ] Replace `src/Iris.Application/Chat/SendMessage/SendMessageHandler.cs`.

```csharp
using Iris.Application.Abstractions.Models;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.DTOs;
using Iris.Application.Chat.Prompting;
using Iris.Domain.Conversations;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

namespace Iris.Application.Chat.SendMessage;

public sealed class SendMessageHandler
{
    private const int RecentMessageLimit = 20;

    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatModelClient _chatModelClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly SendMessageValidator _validator;
    private readonly IClock _clock;

    public SendMessageHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IChatModelClient chatModelClient,
        PromptBuilder promptBuilder,
        SendMessageValidator validator,
        IClock clock)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _chatModelClient = chatModelClient;
        _promptBuilder = promptBuilder;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<SendMessageResult>> HandleAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        var validation = _validator.Validate(command);

        if (validation.IsFailure)
        {
            return Result<SendMessageResult>.Failure(validation.Error);
        }

        var now = _clock.UtcNow;
        var conversationResult = await LoadOrCreateConversationAsync(command.ConversationId, now, cancellationToken);

        if (conversationResult.IsFailure)
        {
            return Result<SendMessageResult>.Failure(conversationResult.Error);
        }

        var conversation = conversationResult.Value.Conversation;
        var isNewConversation = conversationResult.Value.IsNewConversation;
        var userContent = MessageContent.Create(command.Message);
        var userMessage = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.User,
            userContent,
            MessageMetadata.Empty,
            now);

        var history = await _messageRepository.ListRecentAsync(
            conversation.Id,
            RecentMessageLimit,
            cancellationToken);

        var promptResult = _promptBuilder.Build(new PromptBuildRequest(history, userContent));

        if (promptResult.IsFailure)
        {
            return Result<SendMessageResult>.Failure(promptResult.Error);
        }

        var modelResponse = await _chatModelClient.SendAsync(
            promptResult.Value.ModelRequest,
            cancellationToken);

        if (modelResponse.IsFailure)
        {
            return Result<SendMessageResult>.Failure(modelResponse.Error);
        }

        var assistantMessage = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.Assistant,
            MessageContent.Create(modelResponse.Value.Content),
            MessageMetadata.Empty,
            _clock.UtcNow);

        conversation.Touch(_clock.UtcNow);

        if (isNewConversation)
        {
            await _conversationRepository.AddAsync(conversation, cancellationToken);
        }

        await _messageRepository.AddAsync(userMessage, cancellationToken);
        await _messageRepository.AddAsync(assistantMessage, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<SendMessageResult>.Success(new SendMessageResult(
            conversation.Id,
            ToDto(userMessage),
            ToDto(assistantMessage)));
    }

    private async Task<Result<ConversationLoadResult>> LoadOrCreateConversationAsync(
        ConversationId? conversationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (conversationId is null)
        {
            var conversation = Conversation.Create(
                ConversationId.New(),
                title: null,
                ConversationMode.Default,
                now);

            return Result<ConversationLoadResult>.Success(new ConversationLoadResult(conversation, true));
        }

        var existing = await _conversationRepository.GetByIdAsync(conversationId.Value, cancellationToken);

        if (existing is null)
        {
            return Result<ConversationLoadResult>.Failure(Error.Failure(
                "chat.conversation_not_found",
                "Conversation was not found."));
        }

        return Result<ConversationLoadResult>.Success(new ConversationLoadResult(existing, false));
    }

    private static ChatMessageDto ToDto(Message message)
    {
        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.Role,
            message.Content.Value,
            message.CreatedAt);
    }

    private sealed record ConversationLoadResult(Conversation Conversation, bool IsNewConversation);
}
```

- [ ] Run Application tests.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Commit after validation.

Command:

```powershell
git add src/Iris.Application/Chat/SendMessage tests/Iris.Application.Tests/Chat/SendMessage
git commit -m "feat(application): orchestrate send message use case"
```

---

## Task 14: Remove Template Tests And Run Full Verification

- [ ] Inspect template test files.

Command:

```powershell
Get-ChildItem -Recurse tests -Filter UnitTest1.cs
```

- [ ] Delete only template `UnitTest1.cs` files that contain no project-specific assertions.

Safe target files:

```text
tests/Iris.Domain.Tests/UnitTest1.cs
tests/Iris.Application.Tests/UnitTest1.cs
```

- [ ] Run Domain tests.

Command:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Run Application tests.

Command:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
```

Expected result:

```text
Passed!
```

- [ ] Run all tests.

Command:

```powershell
dotnet test .\Iris.slnx
```

Expected result:

```text
Passed!
```

- [ ] Run full build.

Command:

```powershell
dotnet build .\Iris.slnx
```

Expected result:

```text
Build succeeded.
```

- [ ] Commit after validation.

Command:

```powershell
git add tests/Iris.Domain.Tests tests/Iris.Application.Tests
git commit -m "test: replace template tests with chat slice coverage"
```

---

## Task 15: Architecture Audit For Phase 1-2

- [ ] Check project references.

Command:

```powershell
dotnet list .\src\Iris.Domain\Iris.Domain.csproj reference
dotnet list .\src\Iris.Application\Iris.Application.csproj reference
```

Expected result:

```text
Iris.Domain references Iris.Shared only.
Iris.Application references Iris.Domain and Iris.Shared only.
```

- [ ] Search for forbidden adapter references in Domain/Application.

Command:

```powershell
rg "Iris\.Persistence|Iris\.ModelGateway|Microsoft\.EntityFrameworkCore|Avalonia|HttpClient|Ollama|SQLite|Sqlite" src/Iris.Domain src/Iris.Application tests/Iris.Domain.Tests tests/Iris.Application.Tests
```

Expected result:

```text
No matches in production code.
```

- [ ] Search for direct time usage in Domain/Application.

Command:

```powershell
rg "DateTime\.Now|DateTimeOffset\.Now|DateTimeOffset\.UtcNow" src/Iris.Domain src/Iris.Application
```

Expected result:

```text
No matches.
```

- [ ] Commit if audit caused cleanup changes.

Command:

```powershell
git add src/Iris.Domain src/Iris.Application tests/Iris.Domain.Tests tests/Iris.Application.Tests
git commit -m "chore: audit phase 1-2 boundaries"
```

If no cleanup changes were needed, do not create an empty commit.

---

## Task 16: Update Local Agent Metadata

These files are local operational memory and are not pushed to GitHub.

- [ ] Append `.agent/PROJECT_LOG.md`.

Use this format:

```md
## 2026-04-26 — Phase 1-2 chat slice implementation

### Changed
- Implemented shared result/time primitives.
- Implemented minimum Domain conversation/message model.
- Implemented Application SendMessage flow with fake-tested dependencies.

### Files
- src/Iris.Shared/Results/*
- src/Iris.Shared/Time/*
- src/Iris.Domain/Common/*
- src/Iris.Domain/Conversations/*
- src/Iris.Application/Abstractions/*
- src/Iris.Application/Chat/*
- tests/Iris.Domain.Tests/*
- tests/Iris.Application.Tests/*

### Validation
- dotnet test .\Iris.slnx: passed.
- dotnet build .\Iris.slnx: passed.

### Next
- Phase 3: implement Persistence adapter for conversations/messages.
```

- [ ] Update `.agent/overview.md`.

Required current state:

```md
Current phase: First vertical chat slice, Phase 1-2 complete.
Current implementation target: Persistence adapter for conversation/message storage.
Current working status: Domain and Application chat core implemented and tested.
Next immediate step: Phase 3 SQLite persistence.
Known blockers: none if build and tests passed.
```

- [ ] Add `.agent/log_notes.md` entries only for actual failures or unexpected behavior encountered during implementation.

- [ ] Add `.agent/debt_tech_backlog.md` entries only for debt introduced or discovered.

---

## Final Verification Checklist

- [ ] `dotnet build .\Iris.slnx` passes.
- [ ] `dotnet test .\Iris.slnx` passes.
- [ ] `Iris.Domain` references only `Iris.Shared`.
- [ ] `Iris.Application` references only `Iris.Domain` and `Iris.Shared`.
- [ ] No Domain/Application references to EF Core, Avalonia, HTTP, Ollama, SQLite, Persistence, or ModelGateway.
- [ ] `SendMessageHandler` returns controlled `Result<SendMessageResult>` errors.
- [ ] Model failure does not save a partial user/assistant pair.
- [ ] Tests use fakes, not adapter projects.
- [ ] `.agent/PROJECT_LOG.md` and `.agent/overview.md` are updated locally.
- [ ] Public implementation commits are pushed only after green validation.

---

## Expected End State

After this plan is complete, the repository has:

- public shared result/time primitives;
- tested Domain chat model;
- tested Application chat use case;
- provider-neutral prompt assembly;
- repository/model/unit-of-work abstractions ready for adapters;
- no Desktop/Persistence/ModelGateway implementation yet.

The next phase is Phase 3: implement `Iris.Persistence` SQLite entities, mappings, repositories, `IrisDbContext`, and integration tests for saving/reloading conversations and messages.
