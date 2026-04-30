using Iris.Domain.Conversations;
using Iris.Persistence.Database;
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
        DateTimeOffset updatedAt = createdAt.AddMinutes(3);
        var conversation = Conversation.Create(
            ConversationId.New(),
            ConversationTitle.Create("Phase 3 chat"),
            ConversationMode.Default,
            createdAt);
        conversation.Touch(updatedAt);

        await using (IrisDbContext writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new ConversationRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(conversation, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using IrisDbContext readContext = factory.CreateContext();
        var readRepository = new ConversationRepository(readContext);

        Conversation? persisted = await readRepository.GetByIdAsync(conversation.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(conversation.Id, persisted.Id);
        Assert.Equal("Phase 3 chat", persisted.Title!.Value);
        Assert.Equal(ConversationStatus.Active, persisted.Status);
        Assert.Equal(ConversationMode.Default, persisted.Mode);
        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal(updatedAt, persisted.UpdatedAt);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PreservesUtcTicksForNonZeroOffsetTimestamps()
    {
        await using var factory = new PersistenceTestContextFactory();
        DateTimeOffset createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.FromHours(5)).AddTicks(1_234);
        DateTimeOffset updatedAt = createdAt.AddTicks(5_678);
        var conversation = Conversation.Create(
            ConversationId.New(),
            null,
            ConversationMode.Default,
            createdAt);
        conversation.Touch(updatedAt);

        await using (IrisDbContext writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new ConversationRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(conversation, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using IrisDbContext readContext = factory.CreateContext();
        var readRepository = new ConversationRepository(readContext);

        Conversation? persisted = await readRepository.GetByIdAsync(conversation.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(TimeSpan.Zero, persisted.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, persisted.UpdatedAt.Offset);
        Assert.Equal(createdAt.UtcTicks, persisted.CreatedAt.UtcTicks);
        Assert.Equal(updatedAt.UtcTicks, persisted.UpdatedAt.UtcTicks);
    }

    [Fact]
    public async Task UpdateAsync_PersistsExistingConversationChanges()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset updatedAt = createdAt.AddMinutes(4);
        var conversation = Conversation.Create(
            ConversationId.New(),
            ConversationTitle.Create("Original title"),
            ConversationMode.Default,
            createdAt);

        await using (IrisDbContext writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new ConversationRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(conversation, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        conversation.UpdateTitle(ConversationTitle.Create("Updated title"), updatedAt);

        await using (IrisDbContext updateContext = factory.CreateContext())
        {
            var repository = new ConversationRepository(updateContext);
            var unitOfWork = new EfUnitOfWork(updateContext);

            await repository.UpdateAsync(conversation, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using IrisDbContext readContext = factory.CreateContext();
        var readRepository = new ConversationRepository(readContext);

        Conversation? persisted = await readRepository.GetByIdAsync(conversation.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal("Updated title", persisted.Title!.Value);
        Assert.Equal(updatedAt, persisted.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingConversation_ReturnsNull()
    {
        await using var factory = new PersistenceTestContextFactory();

        await using IrisDbContext context = await factory.CreateInitializedContextAsync();
        var repository = new ConversationRepository(context);

        Conversation? result = await repository.GetByIdAsync(ConversationId.New(), CancellationToken.None);

        Assert.Null(result);
    }
}
