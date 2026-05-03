using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Models.Interfaces;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.SendMessage;
using Iris.Application.Memory.Options;
using Iris.Application.Persona.Language;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;
using Iris.Shared.Results;

using Microsoft.Extensions.DependencyInjection;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddIrisApplication_RegistersSendMessageHandler()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConversationRepository, FakeConversationRepository>();
        services.AddSingleton<IMessageRepository, FakeMessageRepository>();
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IChatModelClient, FakeChatModelClient>();
        services.AddSingleton<IMemoryRepository, FakeMemoryRepository>();
        services.AddIrisApplication(new SendMessageOptions(8000), LanguageOptions.Default, MemoryOptions.Default);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<SendMessageHandler>());
    }

    [Fact]
    public void AddIrisApplication_WithNullOptions_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddIrisApplication(null!, LanguageOptions.Default, MemoryOptions.Default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddIrisApplication_WithInvalidMaxMessageLength_Throws(int maxMessageLength)
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddIrisApplication(new SendMessageOptions(maxMessageLength), LanguageOptions.Default, MemoryOptions.Default));

        Assert.Equal("Chat max message length must be greater than zero.", exception.Message);
    }

    [Fact]
    public void AddIrisApplication_WithNullLanguageOptions_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddIrisApplication(new SendMessageOptions(8000), null!, MemoryOptions.Default));
    }

    [Fact]
    public void AddIrisApplication_RegistersILanguagePolicy()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConversationRepository, FakeConversationRepository>();
        services.AddSingleton<IMessageRepository, FakeMessageRepository>();
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IChatModelClient, FakeChatModelClient>();
        services.AddSingleton<IMemoryRepository, FakeMemoryRepository>();
        services.AddIrisApplication(new SendMessageOptions(8000), LanguageOptions.Default, MemoryOptions.Default);

        using ServiceProvider provider = services.BuildServiceProvider();

        ILanguagePolicy policy = provider.GetRequiredService<ILanguagePolicy>();
        Assert.NotNull(policy);
        Assert.IsType<RussianDefaultLanguagePolicy>(policy);
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        public Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken)
        {
            return Task.FromResult<Conversation?>(null);
        }

        public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessageRepository : IMessageRepository
    {
        public Task<IReadOnlyList<Message>> ListRecentAsync(
            ConversationId conversationId,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Message>>(Array.Empty<Message>());
        }

        public Task AddAsync(Message message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeChatModelClient : IChatModelClient
    {
        public Task<Result<ChatModelResponse>> SendAsync(
            ChatModelRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        }
    }

    private sealed class FakeMemoryRepository : IMemoryRepository
    {
        public Task<DomainMemory?> GetByIdAsync(MemoryId id, CancellationToken ct)
        {
            return Task.FromResult<DomainMemory?>(null);
        }

        public Task AddAsync(DomainMemory memory, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DomainMemory memory, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DomainMemory>> ListActiveAsync(int limit, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<DomainMemory>>(Array.Empty<DomainMemory>());
        }

        public Task<IReadOnlyList<DomainMemory>> SearchActiveAsync(string query, int limit, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<DomainMemory>>(Array.Empty<DomainMemory>());
        }
    }
}
