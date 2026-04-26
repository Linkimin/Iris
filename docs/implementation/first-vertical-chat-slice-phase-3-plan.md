# First Vertical Chat Slice Phase 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `Iris.Persistence` SQLite adapter for conversations and messages so Application repositories can save and reload the Phase 1-2 chat core.

**Architecture:** `Iris.Persistence` implements Application persistence abstractions and maps Domain objects to EF Core entities. Domain stays free of EF attributes, Application stays free of Persistence references, and integration tests verify the adapter through the public Application repository interfaces.

**Tech Stack:** .NET 10, C#, EF Core 10, EF Core SQLite, xUnit, Iris.Domain, Iris.Application, Iris.Persistence, Iris.Integration.Tests.

---

## Existing Spec / Design Check

`docs/implementation/first-vertical-chat-slice.md` already contains the high-level Phase 3 scope:

- implement EF entities;
- implement EF configurations;
- implement `IrisDbContext`;
- implement `ConversationRepository`;
- implement `MessageRepository`;
- implement `EfUnitOfWork`;
- add SQLite integration tests for save/reload behavior.

That document is sufficient as a slice-level spec, but it is not sufficient as an executable implementation plan. It does not define exact file contents, mapper behavior, rehydration requirements, test setup, commit checkpoints, or adapter failure boundaries.

This document is the executable Phase 3 plan.

---

## Phase 3 Scope

In scope:

- `Iris.Persistence` project references to Application, Domain, and Shared.
- EF Core SQLite entity model for `Conversation` and `Message`.
- EF Core configurations for tables, keys, required fields, indexes, relationships, and date/time storage.
- `IrisDbContext`.
- Domain/entity mappers.
- `ConversationRepository`.
- `MessageRepository`.
- `EfUnitOfWork`.
- Minimal Persistence DI registration.
- SQLite integration tests using a temporary database file.
- A small Domain rehydration factory if required for correct persistence mapping.

Out of scope:

- ModelGateway/Ollama.
- Desktop UI wiring.
- API/Worker wiring.
- Memory, embeddings, persona persistence, tools, voice, perception, SI runtime.
- Production migrations.
- Database encryption.
- Settings UI.
- Logging infrastructure.

Production migrations are intentionally out of scope for Phase 3. Integration tests use `Database.EnsureCreatedAsync()` against a temporary SQLite database. Migrations should be added when host-level database configuration is finalized.

---

## File Responsibility Map

Modify:

- `src/Iris.Persistence/Iris.Persistence.csproj`  
  Adds project references to `Iris.Application`, `Iris.Domain`, and `Iris.Shared`; adds DI/options package references if required by `DependencyInjection.cs`.

- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj`  
  Adds project references for Persistence integration tests.

- `src/Iris.Domain/Conversations/Conversation.cs`  
  Adds a controlled rehydration factory so Persistence can restore status and `UpdatedAt` without reflection or EF attributes.

- `tests/Iris.Domain.Tests/Conversations/ConversationTests.cs`  
  Adds tests for conversation rehydration.

- `src/Iris.Persistence/Entities/ConversationEntity.cs`  
  EF entity for conversations.

- `src/Iris.Persistence/Entities/MessageEntity.cs`  
  EF entity for messages.

- `src/Iris.Persistence/Configurations/ConversationEntityConfiguration.cs`  
  EF table/schema configuration for conversations.

- `src/Iris.Persistence/Configurations/MessageEntityConfiguration.cs`  
  EF table/schema configuration for messages.

- `src/Iris.Persistence/Database/IrisDbContext.cs`  
  EF DbContext and model configuration registration.

- `src/Iris.Persistence/Database/DatabaseOptions.cs`  
  Persistence options with connection string validation.

- `src/Iris.Persistence/Mapping/ConversationMapper.cs`  
  Domain ↔ entity mapping for conversations.

- `src/Iris.Persistence/Mapping/MessageMapper.cs`  
  Domain ↔ entity mapping for messages.

- `src/Iris.Persistence/Repositories/ConversationRepository.cs`  
  Implements `IConversationRepository`.

- `src/Iris.Persistence/Repositories/MessageRepository.cs`  
  Implements `IMessageRepository`.

- `src/Iris.Persistence/UnitOfWork/EfUnitOfWork.cs`  
  Implements `IUnitOfWork`.

- `src/Iris.Persistence/DependencyInjection.cs`  
  Registers DbContext, repositories, and unit of work.

- `tests/Iris.IntegrationTests/UnitTest1.cs`  
  Delete this assertion-free template test after real integration tests are added.

Create:

- `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs`  
  Test helper for temporary SQLite DbContext creation.

- `tests/Iris.IntegrationTests/Persistence/ConversationRepositoryTests.cs`  
  Tests conversation save/reload.

- `tests/Iris.IntegrationTests/Persistence/MessageRepositoryTests.cs`  
  Tests message save/reload/order.

Do not create:

- new production folders beyond existing Persistence folders;
- `Iris.Shared.Tests`;
- migrations;
- Desktop/API/Worker composition files;
- ModelGateway files.

---

## Checkpoint Before Implementation

- [ ] Read `AGENTS.md`.
- [ ] Read `.agent/overview.md`.
- [ ] Read `.agent/architecture.md`.
- [ ] Read `.agent/first-vertical-slice.md`.
- [ ] Read `docs/implementation/first-vertical-chat-slice.md`.
- [ ] Read this plan.
- [ ] Confirm `git status -sb` has no unexpected tracked changes.
- [ ] Leave untracked user files alone, including `docs/.agent.7z` if it is still present.

Command:

```powershell
git status -sb
```

Expected tracked state before edits:

```text
## main...origin/main
```

Untracked user-owned files may appear. Do not add or delete them.

---

## Task 1: Wire Persistence And Integration Project References

**Files:**

- Modify: `src/Iris.Persistence/Iris.Persistence.csproj`
- Modify: `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj`

- [ ] **Step 1: Add project references to Persistence**

Modify `src/Iris.Persistence/Iris.Persistence.csproj` so it contains these references:

```xml
<ItemGroup>
  <ProjectReference Include="..\Iris.Application\Iris.Application.csproj" />
  <ProjectReference Include="..\Iris.Domain\Iris.Domain.csproj" />
  <ProjectReference Include="..\Iris.Shared\Iris.Shared.csproj" />
</ItemGroup>
```

Keep existing EF Core package references.

- [ ] **Step 2: Add DI package references if missing**

In `src/Iris.Persistence/Iris.Persistence.csproj`, add:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.7" />
  <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.7" />
</ItemGroup>
```

If these packages already exist in the file at implementation time, do not duplicate them.

- [ ] **Step 3: Add integration test references**

Modify `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` so it references the projects under test:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Iris.Application\Iris.Application.csproj" />
  <ProjectReference Include="..\..\src\Iris.Domain\Iris.Domain.csproj" />
  <ProjectReference Include="..\..\src\Iris.Persistence\Iris.Persistence.csproj" />
  <ProjectReference Include="..\..\src\Iris.Shared\Iris.Shared.csproj" />
</ItemGroup>
```

Add a direct SQLite package reference for test DbContext setup:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />
</ItemGroup>
```

- [ ] **Step 4: Validate project restore/build**

Run:

```powershell
dotnet build .\src\Iris.Persistence\Iris.Persistence.csproj
dotnet build .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj
```

Expected:

```text
Build succeeded.
```

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/Iris.Persistence/Iris.Persistence.csproj tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj
git commit -m "chore(persistence): wire persistence test references"
```

---

## Task 2: Add Domain Conversation Rehydration

**Files:**

- Modify: `src/Iris.Domain/Conversations/Conversation.cs`
- Modify: `tests/Iris.Domain.Tests/Conversations/ConversationTests.cs`

Reasoning:

Persistence must restore `Conversation.Status` and `Conversation.UpdatedAt`. The current `Conversation.Create(...)` factory always creates an active conversation and sets `UpdatedAt = CreatedAt`. Persistence must not use reflection or EF attributes to bypass Domain.

- [ ] **Step 1: Add failing rehydration tests**

Append these tests to `tests/Iris.Domain.Tests/Conversations/ConversationTests.cs`:

```csharp
[Fact]
public void Rehydrate_WithPersistedState_RestoresConversation()
{
    var id = ConversationId.New();
    var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
    var updatedAt = createdAt.AddMinutes(5);
    var title = ConversationTitle.Create("Saved chat");

    var conversation = Conversation.Rehydrate(
        id,
        title,
        ConversationStatus.Archived,
        ConversationMode.Default,
        createdAt,
        updatedAt);

    Assert.Equal(id, conversation.Id);
    Assert.Equal(title, conversation.Title);
    Assert.Equal(ConversationStatus.Archived, conversation.Status);
    Assert.Equal(ConversationMode.Default, conversation.Mode);
    Assert.Equal(createdAt, conversation.CreatedAt);
    Assert.Equal(updatedAt, conversation.UpdatedAt);
}

[Theory]
[InlineData(0)]
[InlineData(999)]
public void Rehydrate_WithInvalidStatus_ThrowsDomainException(int status)
{
    var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    var exception = Assert.Throws<DomainException>(() => Conversation.Rehydrate(
        ConversationId.New(),
        null,
        (ConversationStatus)status,
        ConversationMode.Default,
        createdAt,
        createdAt));

    Assert.Equal("conversation.invalid_status", exception.Code);
}

[Fact]
public void Rehydrate_WithUpdatedAtBeforeCreatedAt_ThrowsDomainException()
{
    var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    var exception = Assert.Throws<DomainException>(() => Conversation.Rehydrate(
        ConversationId.New(),
        null,
        ConversationStatus.Active,
        ConversationMode.Default,
        createdAt,
        createdAt.AddTicks(-1)));

    Assert.Equal("conversation.invalid_updated_at", exception.Code);
}
```

Ensure the file has:

```csharp
using Iris.Domain.Common;
using Iris.Domain.Conversations;
```

- [ ] **Step 2: Run Domain tests and verify failure**

Run:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj --no-restore
```

Expected:

```text
Failed!
```

Expected failure cause:

```text
'Conversation' does not contain a definition for 'Rehydrate'
```

- [ ] **Step 3: Implement `Conversation.Rehydrate`**

Modify `src/Iris.Domain/Conversations/Conversation.cs` by adding this factory below `Create(...)`:

```csharp
public static Conversation Rehydrate(
    ConversationId id,
    ConversationTitle? title,
    ConversationStatus status,
    ConversationMode mode,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt)
{
    if (!Enum.IsDefined(status))
    {
        throw new DomainException("conversation.invalid_status", "Conversation status is invalid.");
    }

    if (!Enum.IsDefined(mode))
    {
        throw new DomainException("conversation.invalid_mode", "Conversation mode is invalid.");
    }

    if (updatedAt < createdAt)
    {
        throw new DomainException(
            "conversation.invalid_updated_at",
            "Conversation updated timestamp cannot be earlier than created timestamp.");
    }

    return new Conversation(
        id,
        title,
        status,
        mode,
        createdAt,
        updatedAt);
}
```

- [ ] **Step 4: Run Domain tests**

Run:

```powershell
dotnet test .\tests\Iris.Domain.Tests\Iris.Domain.Tests.csproj --no-restore
```

Expected:

```text
Passed!
```

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/Iris.Domain/Conversations/Conversation.cs tests/Iris.Domain.Tests/Conversations/ConversationTests.cs
git commit -m "feat(domain): support conversation rehydration"
```

---

## Task 3: Add Failing Persistence Integration Tests

**Files:**

- Create: `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs`
- Create: `tests/Iris.IntegrationTests/Persistence/ConversationRepositoryTests.cs`
- Create: `tests/Iris.IntegrationTests/Persistence/MessageRepositoryTests.cs`

- [ ] **Step 1: Create temporary SQLite test factory**

Create `tests/Iris.IntegrationTests/Persistence/PersistenceTestContextFactory.cs`:

```csharp
using Iris.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Iris.Integration.Tests.Persistence;

internal sealed class PersistenceTestContextFactory : IAsyncDisposable
{
    private readonly string _databasePath;

    public PersistenceTestContextFactory()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"iris-persistence-{Guid.NewGuid():N}.db");
    }

    public async Task<IrisDbContext> CreateInitializedContextAsync()
    {
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public IrisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new IrisDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
```

- [ ] **Step 2: Create conversation repository test**

Create `tests/Iris.IntegrationTests/Persistence/ConversationRepositoryTests.cs`:

```csharp
using Iris.Domain.Conversations;
using Iris.Persistence.Repositories;
using Iris.Persistence.UnitOfWork;

namespace Iris.Integration.Tests.Persistence;

public sealed class ConversationRepositoryTests
{
    [Fact]
    public async Task AddAndGetByIdAsync_PersistsConversation()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(3);
        var conversation = Conversation.Create(
            ConversationId.New(),
            ConversationTitle.Create("Phase 3 chat"),
            ConversationMode.Default,
            createdAt);
        conversation.Touch(updatedAt);

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new ConversationRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(conversation, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new ConversationRepository(readContext);

        var persisted = await readRepository.GetByIdAsync(conversation.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(conversation.Id, persisted.Id);
        Assert.Equal("Phase 3 chat", persisted.Title!.Value);
        Assert.Equal(ConversationStatus.Active, persisted.Status);
        Assert.Equal(ConversationMode.Default, persisted.Mode);
        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal(updatedAt, persisted.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingConversation_ReturnsNull()
    {
        await using var factory = new PersistenceTestContextFactory();

        await using var context = await factory.CreateInitializedContextAsync();
        var repository = new ConversationRepository(context);

        var result = await repository.GetByIdAsync(ConversationId.New(), CancellationToken.None);

        Assert.Null(result);
    }
}
```

- [ ] **Step 3: Create message repository test**

Create `tests/Iris.IntegrationTests/Persistence/MessageRepositoryTests.cs`:

```csharp
using Iris.Domain.Conversations;
using Iris.Persistence.Repositories;
using Iris.Persistence.UnitOfWork;

namespace Iris.Integration.Tests.Persistence;

public sealed class MessageRepositoryTests
{
    [Fact]
    public async Task AddAndListRecentAsync_PersistsMessagesInChronologicalOrder()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(
            ConversationId.New(),
            null,
            ConversationMode.Default,
            createdAt);
        var newest = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.Assistant,
            MessageContent.Create("Newest"),
            MessageMetadata.Empty,
            createdAt.AddMinutes(2));
        var oldest = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.User,
            MessageContent.Create("Oldest"),
            MessageMetadata.Empty,
            createdAt.AddMinutes(1));

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);
            await messageRepository.AddAsync(newest, CancellationToken.None);
            await messageRepository.AddAsync(oldest, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(
            conversation.Id,
            limit: 10,
            CancellationToken.None);

        Assert.Collection(
            messages,
            message =>
            {
                Assert.Equal(MessageRole.User, message.Role);
                Assert.Equal("Oldest", message.Content.Value);
            },
            message =>
            {
                Assert.Equal(MessageRole.Assistant, message.Role);
                Assert.Equal("Newest", message.Content.Value);
            });
    }

    [Fact]
    public async Task ListRecentAsync_RespectsLimitAndReturnsOldestToNewestWithinWindow()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);

            for (var index = 0; index < 3; index++)
            {
                var message = Message.Create(
                    MessageId.New(),
                    conversation.Id,
                    MessageRole.User,
                    MessageContent.Create($"Message {index}"),
                    MessageMetadata.Empty,
                    createdAt.AddMinutes(index));

                await messageRepository.AddAsync(message, CancellationToken.None);
            }

            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(
            conversation.Id,
            limit: 2,
            CancellationToken.None);

        Assert.Collection(
            messages,
            message => Assert.Equal("Message 1", message.Content.Value),
            message => Assert.Equal("Message 2", message.Content.Value));
    }
}
```

- [ ] **Step 4: Run integration tests and verify failure**

Run:

```powershell
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-restore
```

Expected:

```text
Failed!
```

Expected failure cause:

```text
'IrisDbContext' is inaccessible due to its protection level
```

or:

```text
'ConversationRepository' does not contain a constructor that takes 1 arguments
```

---

## Task 4: Implement EF Entities And Configurations

**Files:**

- Modify: `src/Iris.Persistence/Entities/ConversationEntity.cs`
- Modify: `src/Iris.Persistence/Entities/MessageEntity.cs`
- Modify: `src/Iris.Persistence/Configurations/ConversationEntityConfiguration.cs`
- Modify: `src/Iris.Persistence/Configurations/MessageEntityConfiguration.cs`

- [ ] **Step 1: Implement `ConversationEntity`**

Replace `src/Iris.Persistence/Entities/ConversationEntity.cs`:

```csharp
namespace Iris.Persistence.Entities;

public sealed class ConversationEntity
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public int Status { get; set; }

    public int Mode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<MessageEntity> Messages { get; } = new();
}
```

- [ ] **Step 2: Implement `MessageEntity`**

Replace `src/Iris.Persistence/Entities/MessageEntity.cs`:

```csharp
namespace Iris.Persistence.Entities;

public sealed class MessageEntity
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public int Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public ConversationEntity? Conversation { get; set; }
}
```

- [ ] **Step 3: Implement `ConversationEntityConfiguration`**

Replace `src/Iris.Persistence/Configurations/ConversationEntityConfiguration.cs`:

```csharp
using Iris.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iris.Persistence.Configurations;

public sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Id)
            .ValueGeneratedNever();

        builder.Property(conversation => conversation.Title)
            .HasMaxLength(120);

        builder.Property(conversation => conversation.Status)
            .IsRequired();

        builder.Property(conversation => conversation.Mode)
            .IsRequired();

        builder.Property(conversation => conversation.CreatedAt)
            .IsRequired();

        builder.Property(conversation => conversation.UpdatedAt)
            .IsRequired();

        builder.HasIndex(conversation => conversation.UpdatedAt);
    }
}
```

- [ ] **Step 4: Implement `MessageEntityConfiguration`**

Replace `src/Iris.Persistence/Configurations/MessageEntityConfiguration.cs`:

```csharp
using Iris.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iris.Persistence.Configurations;

public sealed class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedNever();

        builder.Property(message => message.ConversationId)
            .IsRequired();

        builder.Property(message => message.Role)
            .IsRequired();

        builder.Property(message => message.Content)
            .IsRequired();

        builder.Property(message => message.CreatedAt)
            .IsRequired();

        builder.Property(message => message.MetadataJson)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });
    }
}
```

- [ ] **Step 5: Build Persistence**

Run:

```powershell
dotnet build .\src\Iris.Persistence\Iris.Persistence.csproj --no-restore
```

Expected:

```text
Build succeeded.
```

---

## Task 5: Implement IrisDbContext And DatabaseOptions

**Files:**

- Modify: `src/Iris.Persistence/Database/IrisDbContext.cs`
- Modify: `src/Iris.Persistence/Database/DatabaseOptions.cs`

- [ ] **Step 1: Implement `IrisDbContext`**

Replace `src/Iris.Persistence/Database/IrisDbContext.cs`:

```csharp
using Iris.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Database;

public sealed class IrisDbContext : DbContext
{
    public IrisDbContext(DbContextOptions<IrisDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();

    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IrisDbContext).Assembly);
    }
}
```

- [ ] **Step 2: Implement `DatabaseOptions`**

Replace `src/Iris.Persistence/Database/DatabaseOptions.cs`:

```csharp
namespace Iris.Persistence.Database;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Database connection string is required.");
        }
    }
}
```

- [ ] **Step 3: Build Persistence**

Run:

```powershell
dotnet build .\src\Iris.Persistence\Iris.Persistence.csproj --no-restore
```

Expected:

```text
Build succeeded.
```

---

## Task 6: Implement Domain/Entity Mappers

**Files:**

- Modify: `src/Iris.Persistence/Mapping/ConversationMapper.cs`
- Modify: `src/Iris.Persistence/Mapping/MessageMapper.cs`

- [ ] **Step 1: Implement `ConversationMapper`**

Replace `src/Iris.Persistence/Mapping/ConversationMapper.cs`:

```csharp
using Iris.Domain.Conversations;
using Iris.Persistence.Entities;

namespace Iris.Persistence.Mapping;

public static class ConversationMapper
{
    public static ConversationEntity ToEntity(Conversation conversation)
    {
        return new ConversationEntity
        {
            Id = conversation.Id.Value,
            Title = conversation.Title?.Value,
            Status = (int)conversation.Status,
            Mode = (int)conversation.Mode,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    public static Conversation ToDomain(ConversationEntity entity)
    {
        return Conversation.Rehydrate(
            ConversationId.From(entity.Id),
            entity.Title is null ? null : ConversationTitle.Create(entity.Title),
            (ConversationStatus)entity.Status,
            (ConversationMode)entity.Mode,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
```

- [ ] **Step 2: Implement `MessageMapper`**

Replace `src/Iris.Persistence/Mapping/MessageMapper.cs`:

```csharp
using Iris.Domain.Conversations;
using Iris.Persistence.Entities;

namespace Iris.Persistence.Mapping;

public static class MessageMapper
{
    private const string EmptyMetadataJson = "{}";

    public static MessageEntity ToEntity(Message message)
    {
        return new MessageEntity
        {
            Id = message.Id.Value,
            ConversationId = message.ConversationId.Value,
            Role = (int)message.Role,
            Content = message.Content.Value,
            CreatedAt = message.CreatedAt,
            MetadataJson = EmptyMetadataJson
        };
    }

    public static Message ToDomain(MessageEntity entity)
    {
        return Message.Create(
            MessageId.From(entity.Id),
            ConversationId.From(entity.ConversationId),
            (MessageRole)entity.Role,
            MessageContent.Create(entity.Content),
            MessageMetadata.Empty,
            entity.CreatedAt);
    }
}
```

- [ ] **Step 3: Build Persistence**

Run:

```powershell
dotnet build .\src\Iris.Persistence\Iris.Persistence.csproj --no-restore
```

Expected:

```text
Build succeeded.
```

---

## Task 7: Implement Repositories And Unit Of Work

**Files:**

- Modify: `src/Iris.Persistence/Repositories/ConversationRepository.cs`
- Modify: `src/Iris.Persistence/Repositories/MessageRepository.cs`
- Modify: `src/Iris.Persistence/UnitOfWork/EfUnitOfWork.cs`

- [ ] **Step 1: Implement `ConversationRepository`**

Replace `src/Iris.Persistence/Repositories/ConversationRepository.cs`:

```csharp
using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Conversations;
using Iris.Persistence.Database;
using Iris.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IrisDbContext _dbContext;

    public ConversationRepository(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation?> GetByIdAsync(
        ConversationId id,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                conversation => conversation.Id == id.Value,
                cancellationToken);

        return entity is null ? null : ConversationMapper.ToDomain(entity);
    }

    public async Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        var entity = ConversationMapper.ToEntity(conversation);
        await _dbContext.Conversations.AddAsync(entity, cancellationToken);
    }
}
```

- [ ] **Step 2: Implement `MessageRepository`**

Replace `src/Iris.Persistence/Repositories/MessageRepository.cs`:

```csharp
using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Conversations;
using Iris.Persistence.Database;
using Iris.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly IrisDbContext _dbContext;

    public MessageRepository(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Message>> ListRecentAsync(
        ConversationId conversationId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Message>();
        }

        var entities = await _dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId.Value)
            .OrderByDescending(message => message.CreatedAt)
            .Take(limit)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities
            .Select(MessageMapper.ToDomain)
            .ToList();
    }

    public async Task AddAsync(
        Message message,
        CancellationToken cancellationToken)
    {
        var entity = MessageMapper.ToEntity(message);
        await _dbContext.Messages.AddAsync(entity, cancellationToken);
    }
}
```

- [ ] **Step 3: Implement `EfUnitOfWork`**

Replace `src/Iris.Persistence/UnitOfWork/EfUnitOfWork.cs`:

```csharp
using Iris.Application.Abstractions.Persistence;
using Iris.Persistence.Database;

namespace Iris.Persistence.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly IrisDbContext _dbContext;

    public EfUnitOfWork(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Run integration tests**

Run:

```powershell
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-restore
```

Expected:

```text
Passed!
```

If this fails because SQLite cannot translate the double ordering in `ListRecentAsync`, replace the repository query with this version:

```csharp
var newestEntities = await _dbContext.Messages
    .AsNoTracking()
    .Where(message => message.ConversationId == conversationId.Value)
    .OrderByDescending(message => message.CreatedAt)
    .Take(limit)
    .ToListAsync(cancellationToken);

return newestEntities
    .OrderBy(message => message.CreatedAt)
    .Select(MessageMapper.ToDomain)
    .ToList();
```

Then rerun the same test command.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/Iris.Persistence tests/Iris.IntegrationTests/Persistence
git commit -m "feat(persistence): persist chat conversations and messages"
```

---

## Task 8: Implement Persistence Dependency Injection

**Files:**

- Modify: `src/Iris.Persistence/DependencyInjection.cs`

- [ ] **Step 1: Replace `DependencyInjection.cs`**

Replace `src/Iris.Persistence/DependencyInjection.cs`:

```csharp
using Iris.Application.Abstractions.Persistence;
using Iris.Persistence.Database;
using Iris.Persistence.Repositories;
using Iris.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddIrisPersistence(
        this IServiceCollection services,
        Action<DatabaseOptions> configureOptions)
    {
        var options = new DatabaseOptions();
        configureOptions(options);
        options.Validate();

        services.AddSingleton(options);

        services.AddDbContext<IrisDbContext>(dbContextOptions =>
        {
            dbContextOptions.UseSqlite(options.ConnectionString);
        });

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
```

- [ ] **Step 2: Build Persistence**

Run:

```powershell
dotnet build .\src\Iris.Persistence\Iris.Persistence.csproj --no-restore
```

Expected:

```text
Build succeeded.
```

---

## Task 9: Remove Integration Template Test And Run Full Verification

**Files:**

- Delete: `tests/Iris.IntegrationTests/UnitTest1.cs`

- [ ] **Step 1: Inspect template test**

Run:

```powershell
Get-Content .\tests\Iris.IntegrationTests\UnitTest1.cs
```

Expected if safe to delete:

```text
The file contains only an empty template test with no Iris-specific assertion.
```

- [ ] **Step 2: Delete template test**

Delete `tests/Iris.IntegrationTests/UnitTest1.cs`.

- [ ] **Step 3: Run integration tests**

Run:

```powershell
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --no-restore
```

Expected:

```text
Passed!
```

- [ ] **Step 4: Run all tests**

Run:

```powershell
dotnet test .\Iris.slnx --no-restore
```

Expected:

```text
Passed!
```

- [ ] **Step 5: Run full build**

Run:

```powershell
dotnet build .\Iris.slnx
```

Expected:

```text
Build succeeded.
```

- [ ] **Step 6: Commit**

Run:

```powershell
git add tests/Iris.IntegrationTests
git commit -m "test(persistence): cover sqlite chat persistence"
```

---

## Task 10: Architecture And Boundary Audit

- [ ] **Step 1: Verify project references**

Run:

```powershell
dotnet list .\src\Iris.Domain\Iris.Domain.csproj reference
dotnet list .\src\Iris.Application\Iris.Application.csproj reference
dotnet list .\src\Iris.Persistence\Iris.Persistence.csproj reference
```

Expected:

```text
Iris.Domain has no project references.
Iris.Application references Iris.Domain and Iris.Shared.
Iris.Persistence references Iris.Application, Iris.Domain, and Iris.Shared.
```

- [ ] **Step 2: Verify Application/Domain did not gain adapter references**

Run:

```powershell
Select-String -Path .\src\Iris.Domain\**\*.cs,.\src\Iris.Application\**\*.cs -Pattern 'Iris.Persistence','Microsoft.EntityFrameworkCore','Sqlite','SQLite','Iris.ModelGateway','Avalonia','HttpClient','Ollama'
```

Expected:

```text
No matches in Domain or Application source files.
```

- [ ] **Step 3: Verify Persistence did not gain forbidden adapter references**

Run:

```powershell
Select-String -Path .\src\Iris.Persistence\**\*.cs -Pattern 'Iris.ModelGateway','Iris.Desktop','Iris.Tools','Iris.Voice','Iris.Perception','Iris.Api','Iris.Worker','Ollama','HttpClient'
```

Expected:

```text
No matches in Persistence source files.
```

- [ ] **Step 4: Verify no direct prompt/model logic entered Persistence**

Run:

```powershell
Select-String -Path .\src\Iris.Persistence\**\*.cs -Pattern 'Prompt','ChatModel','IChatModelClient','SystemPrompt'
```

Expected:

```text
No matches in Persistence source files.
```

If the string `ChatModel` appears only in unrelated generated output under `bin` or `obj`, ignore it and restrict the scan to `.cs` files under source folders.

---

## Task 11: Update Local Agent Metadata

These files are local-only and must not be pushed:

- `.agent/PROJECT_LOG.md`
- `.agent/overview.md`
- `.agent/log_notes.md`
- `.agent/debt_tech_backlog.md`

- [ ] **Step 1: Append `.agent/PROJECT_LOG.md`**

Use this entry:

```md
## 2026-04-27 — Phase 3 SQLite persistence adapter

### Changed
- Implemented EF Core SQLite persistence for conversations and messages.
- Added `IrisDbContext`, entities, configurations, mappers, repositories, unit of work, and Persistence DI registration.
- Added SQLite integration tests for saving/reloading conversations and messages.
- Added Domain conversation rehydration required by Persistence mapping.

### Files
- src/Iris.Domain/Conversations/Conversation.cs
- src/Iris.Persistence/Iris.Persistence.csproj
- src/Iris.Persistence/Database/*
- src/Iris.Persistence/Entities/ConversationEntity.cs
- src/Iris.Persistence/Entities/MessageEntity.cs
- src/Iris.Persistence/Configurations/ConversationEntityConfiguration.cs
- src/Iris.Persistence/Configurations/MessageEntityConfiguration.cs
- src/Iris.Persistence/Mapping/ConversationMapper.cs
- src/Iris.Persistence/Mapping/MessageMapper.cs
- src/Iris.Persistence/Repositories/ConversationRepository.cs
- src/Iris.Persistence/Repositories/MessageRepository.cs
- src/Iris.Persistence/UnitOfWork/EfUnitOfWork.cs
- src/Iris.Persistence/DependencyInjection.cs
- tests/Iris.Domain.Tests/Conversations/ConversationTests.cs
- tests/Iris.IntegrationTests/*

### Validation
- `dotnet test .\Iris.slnx --no-restore`: passed.
- `dotnet build .\Iris.slnx`: passed.
- Architecture boundary scans passed.

### Next
- Phase 4: implement `Iris.ModelGateway` Ollama adapter behind `IChatModelClient`.
```

- [ ] **Step 2: Update `.agent/overview.md`**

Set:

```md
Current phase: First vertical chat slice — Phase 3 complete.
Current implementation target: Phase 4 ModelGateway Ollama adapter.
Current working status: Domain/Application/Persistence chat core implemented and tested.
Next immediate step: Implement Ollama `IChatModelClient`, request/response mapping, options, and controlled unavailable-provider error.
Known blockers: none if build/test/audit passed.
```

- [ ] **Step 3: Update `.agent/log_notes.md` only for actual failures**

Record any build/test/SQLite/EF failures encountered during implementation. Use the required `log_notes.md` format.

- [ ] **Step 4: Update `.agent/debt_tech_backlog.md` only for actual debt**

Record deferred migrations if the team wants them tracked:

```md
## Debt: Persistence migrations not created in Phase 3

### Area
Iris.Persistence

### Problem
Phase 3 validates schema through EF Core model configuration and SQLite `EnsureCreatedAsync()` integration tests, but does not create production EF migrations.

### Risk
Future production database upgrades will need explicit migrations before persistent user data exists.

### Proposed fix
Add initial EF Core migration when host-level database configuration is finalized for Desktop wiring.

### Priority
Medium
```

---

## Final Verification Checklist

- [ ] `dotnet build .\Iris.slnx` passes.
- [ ] `dotnet test .\Iris.slnx --no-restore` passes.
- [ ] `Iris.Domain` has no project references.
- [ ] `Iris.Application` references only `Iris.Domain` and `Iris.Shared`.
- [ ] `Iris.Persistence` references only `Iris.Application`, `Iris.Domain`, and `Iris.Shared`.
- [ ] Domain source does not reference EF Core.
- [ ] Application source does not reference Persistence or EF Core.
- [ ] Persistence source does not reference ModelGateway, Desktop, API, Worker, Tools, Voice, Perception, Ollama, or HttpClient.
- [ ] `ConversationRepository.GetByIdAsync` returns `null` for missing conversations.
- [ ] `MessageRepository.ListRecentAsync` returns messages oldest-to-newest.
- [ ] `EfUnitOfWork.CommitAsync` calls `SaveChangesAsync`.
- [ ] Integration tests use temporary SQLite database files and clean them up.
- [ ] `.agent/PROJECT_LOG.md` and `.agent/overview.md` are updated locally.
- [ ] Public commits do not include `.agent` or `docs/.agent.7z`.

---

## Expected End State

After Phase 3 is complete:

- Application can use real `IConversationRepository`, `IMessageRepository`, and `IUnitOfWork` implementations from `Iris.Persistence`.
- Conversations and messages can be saved to SQLite and reloaded.
- Message history is returned chronologically for prompt building.
- No Domain/Application dependency boundary is violated.
- No Desktop or ModelGateway code is involved yet.

The next phase is Phase 4: implement the Ollama `IChatModelClient` in `Iris.ModelGateway`.
