using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Chat.Contracts;
using Iris.Application.Chat.SendMessage;
using Iris.Application.Memory.Commands;
using Iris.Application.Memory.Contracts;
using Iris.Application.Memory.Queries;
using Iris.Desktop;
using Iris.Desktop.Models;
using Iris.Desktop.Services;
using Iris.Desktop.ViewModels;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;
using Iris.IntegrationTests.Testing;
using Iris.Shared.Results;

namespace Iris.IntegrationTests.Desktop;

public sealed class AvatarViewModelTests
{
    // ── T-01 ──────────────────────────────────────────────────────────

    [Fact]
    public void InitialStateIsIdle()
    {
        var chat = new ChatViewModel(new MinimalFakeFacade());
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 2.0);

        using var avatar = new AvatarViewModel(chat, options);

        Assert.Equal(AvatarState.Idle, avatar.State);
        Assert.Equal(AvatarSize.Medium, avatar.Size);
        Assert.Equal(AvatarPosition.BottomRight, avatar.Position);
    }

    // ── T-02 ──────────────────────────────────────────────────────────

    [Fact]
    public void InitialStateIsHiddenWhenDisabled()
    {
        var chat = new ChatViewModel(new MinimalFakeFacade());
        var options = new AvatarOptions(false, AvatarSize.Small, AvatarPosition.TopLeft, 1.0);

        using var avatar = new AvatarViewModel(chat, options);

        Assert.Equal(AvatarState.Hidden, avatar.State);
    }

    // ── T-03 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task StateBecomesThinkingOnSend()
    {
        var pending = new TaskCompletionSource<Result<SendMessageResult>>();
        var fake = new FakeIrisApplicationFacade { PendingResult = pending };
        var chat = new ChatViewModel(fake) { InputText = "hello" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 2.0);
        using var avatar = new AvatarViewModel(chat, options);

        Task sendTask = chat.SendMessageCommand.ExecuteAsync(null);
        await fake.WaitForCallsAsync(1);

        Assert.Equal(AvatarState.Thinking, avatar.State);

        pending.SetResult(FakeIrisApplicationFacade.CreateSuccessfulResult(null, "hello", "response"));
        await sendTask;
    }

    // ── T-04 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task StateBecomesSuccessThenIdle()
    {
        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueSuccess(ConversationId.New(), "hello", "response");
        var chat = new ChatViewModel(fake) { InputText = "hello" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 0.1);
        using var avatar = new AvatarViewModel(chat, options);

        await chat.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal(AvatarState.Success, avatar.State);

        // Wait for timer to transition back to Idle
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (avatar.State == AvatarState.Success && !timeout.IsCancellationRequested)
        {
            await Task.Delay(30, timeout.Token);
        }

        Assert.Equal(AvatarState.Idle, avatar.State);
    }

    // ── T-05 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task StateBecomesErrorOnFailure()
    {
        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueFailure(Error.Failure("model_gateway.provider_unavailable", "raw error"));
        var chat = new ChatViewModel(fake) { InputText = "hello" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 2.0);
        using var avatar = new AvatarViewModel(chat, options);

        await chat.SendMessageCommand.ExecuteAsync(null);

        Assert.True(chat.HasError);
        Assert.False(chat.IsSending);
        Assert.Equal(AvatarState.Error, avatar.State);
    }

    // ── T-06 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ErrorClearsOnNewSend()
    {
        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueFailure(Error.Failure("model_gateway.provider_unavailable", "raw error"));
        var pending = new TaskCompletionSource<Result<SendMessageResult>>();
        var chat = new ChatViewModel(fake) { InputText = "hello" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 2.0);
        using var avatar = new AvatarViewModel(chat, options);

        // First: trigger error
        await chat.SendMessageCommand.ExecuteAsync(null);
        Assert.Equal(AvatarState.Error, avatar.State);

        // Second: new send -> Thinking
        fake.PendingResult = pending;
        chat.InputText = "try again";
        Task sendTask = chat.SendMessageCommand.ExecuteAsync(null);
        await fake.WaitForCallsAsync(2);

        Assert.Equal(AvatarState.Thinking, avatar.State);

        pending.SetResult(FakeIrisApplicationFacade.CreateSuccessfulResult(null, "try again", "ok"));
        await sendTask;
    }

    // ── T-07 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SuccessTimerCancelledOnNewSend()
    {
        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueSuccess(ConversationId.New(), "first", "response");
        var pending = new TaskCompletionSource<Result<SendMessageResult>>();
        var chat = new ChatViewModel(fake) { InputText = "first" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 10.0);
        using var avatar = new AvatarViewModel(chat, options);

        // First send -> Success with long timer
        await chat.SendMessageCommand.ExecuteAsync(null);
        Assert.Equal(AvatarState.Success, avatar.State);

        // Second send before timer fires -> Thinking (timer cancelled)
        fake.PendingResult = pending;
        chat.InputText = "second";
        Task sendTask = chat.SendMessageCommand.ExecuteAsync(null);
        await fake.WaitForCallsAsync(2);

        Assert.Equal(AvatarState.Thinking, avatar.State);

        pending.SetResult(FakeIrisApplicationFacade.CreateSuccessfulResult(null, "second", "ok"));
        await sendTask;
    }

    // ── T-08 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SuccessTimerCancelledOnDisable()
    {
        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueSuccess(ConversationId.New(), "hello", "response");
        var chat = new ChatViewModel(fake) { InputText = "hello" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 10.0);
        using var avatar = new AvatarViewModel(chat, options);

        // Send -> Success with long timer
        await chat.SendMessageCommand.ExecuteAsync(null);
        Assert.Equal(AvatarState.Success, avatar.State);

        // Set ErrorMessage without sending (simulate external error/disable)
        // We can't directly set Enabled on the options since it's immutable.
        // Instead, we test that the _disposed guard prevents PropertyChanged from firing.
        // For the timer cancel test, we dispose the ViewModel.
        avatar.Dispose();

        // Wait a bit to confirm timer doesn't fire and change state
        await Task.Delay(150);

        Assert.Equal(AvatarState.Success, avatar.State);
    }

    // ── T-09 ──────────────────────────────────────────────────────────

    [Fact]
    public void EnabledReadsFromOptions()
    {
        var chat = new ChatViewModel(new MinimalFakeFacade());

        using var enabled = new AvatarViewModel(chat, new AvatarOptions(true, AvatarSize.Small, AvatarPosition.TopLeft, 1.0));
        Assert.Equal(AvatarState.Idle, enabled.State);

        using var disabled = new AvatarViewModel(chat, new AvatarOptions(false, AvatarSize.Small, AvatarPosition.TopLeft, 1.0));
        Assert.Equal(AvatarState.Hidden, disabled.State);
    }

    // ── T-10 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AvatarSize.Small)]
    [InlineData(AvatarSize.Medium)]
    [InlineData(AvatarSize.Large)]
    public void SizeReadsFromOptions(AvatarSize size)
    {
        var chat = new ChatViewModel(new MinimalFakeFacade());
        var options = new AvatarOptions(true, size, AvatarPosition.BottomRight, 2.0);

        using var avatar = new AvatarViewModel(chat, options);

        Assert.Equal(size, avatar.Size);
    }

    // ── T-11 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AvatarPosition.TopLeft)]
    [InlineData(AvatarPosition.TopRight)]
    [InlineData(AvatarPosition.BottomLeft)]
    [InlineData(AvatarPosition.BottomRight)]
    public void PositionReadsFromOptions(AvatarPosition position)
    {
        var chat = new ChatViewModel(new MinimalFakeFacade());
        var options = new AvatarOptions(true, AvatarSize.Medium, position, 2.0);

        using var avatar = new AvatarViewModel(chat, options);

        Assert.Equal(position, avatar.Position);
    }

    // ── T-12 ──────────────────────────────────────────────────────────

    [Fact]
    public void DefaultsUsedWhenConfigMissing()
    {
        AvatarSize defaultSize = DependencyInjection.ParseEnumOrDefault<AvatarSize>(null, AvatarSize.Medium);
        AvatarPosition defaultPosition = DependencyInjection.ParseEnumOrDefault<AvatarPosition>(null, AvatarPosition.BottomRight);
        var defaultDuration = DependencyInjection.ParseDoubleOrDefault(null, 2.0);

        Assert.Equal(AvatarSize.Medium, defaultSize);
        Assert.Equal(AvatarPosition.BottomRight, defaultPosition);
        Assert.Equal(2.0, defaultDuration);

        var defaultEmptyString = DependencyInjection.ParseDoubleOrDefault("", 3.5);
        Assert.Equal(3.5, defaultEmptyString);
    }

    // ── T-13 ──────────────────────────────────────────────────────────

    [Fact]
    public void DefaultsUsedWhenInvalidEnum()
    {
        AvatarSize invalidSize = DependencyInjection.ParseEnumOrDefault<AvatarSize>("InvalidValue", AvatarSize.Medium);
        AvatarPosition invalidPosition = DependencyInjection.ParseEnumOrDefault<AvatarPosition>("NotAPosition", AvatarPosition.TopLeft);

        Assert.Equal(AvatarSize.Medium, invalidSize);
        Assert.Equal(AvatarPosition.TopLeft, invalidPosition);

        var invalidDouble = DependencyInjection.ParseDoubleOrDefault("notanumber", 1.5);
        Assert.Equal(1.5, invalidDouble);

        var negativeDouble = DependencyInjection.ParseDoubleOrDefault("-5", 2.0);
        Assert.Equal(2.0, negativeDouble);

        var zeroDouble = DependencyInjection.ParseDoubleOrDefault("0", 2.0);
        Assert.Equal(2.0, zeroDouble);
    }

    // ── T-14 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeUnsubscribesFromChatViewModel()
    {
        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueSuccess(ConversationId.New(), "hello", "response");
        var chat = new ChatViewModel(fake) { InputText = "hello" };
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 2.0);
        var avatar = new AvatarViewModel(chat, options);

        avatar.Dispose();

        // After dispose, sending should not change state
        AvatarState stateBefore = avatar.State;
        await chat.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal(stateBefore, avatar.State);
        Assert.Equal(AvatarState.Idle, stateBefore);
    }

    // ── T-15 ──────────────────────────────────────────────────────────

    [Fact]
    public void NoProhibitedLayerReferences()
    {
        Assembly assembly = typeof(AvatarViewModel).Assembly;
        var referencedNames = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Desktop host must NOT reference other hosts
        Assert.DoesNotContain("Iris.Api", referencedNames);
        Assert.DoesNotContain("Iris.Worker", referencedNames);
    }

    // ── T-16 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SuccessTimerCancellationTokenIsTriggeredOnNewSend()
    {
        // FR-015: the Success timer must be cancelled when state transitions
        // (new send, error, or disable). This test asserts the cancellation
        // invariant directly via the internal HasActiveSuccessTimer hook so
        // a future regression that leaves _successCts unwired is caught.

        var fake = new FakeIrisApplicationFacade();
        fake.EnqueueSuccess(ConversationId.New(), "first", "response");
        var pending = new TaskCompletionSource<Result<SendMessageResult>>();
        var chat = new ChatViewModel(fake) { InputText = "first" };
        // Long display duration so the timer is still alive when we trigger
        // the next send. If _successCts were unwired (P1-001 regression), the
        // hook would always read false and the first assertion would fail.
        var options = new AvatarOptions(true, AvatarSize.Medium, AvatarPosition.BottomRight, 10.0);
        using var avatar = new AvatarViewModel(chat, options);

        // First send -> Success with active timer.
        await chat.SendMessageCommand.ExecuteAsync(null);
        Assert.Equal(AvatarState.Success, avatar.State);
        Assert.True(
            avatar.HasActiveSuccessTimer,
            "Success timer must be active immediately after assistant response.");

        // Second send -> ComputeState() runs CancelSuccessTimer().
        fake.PendingResult = pending;
        chat.InputText = "second";
        Task sendTask = chat.SendMessageCommand.ExecuteAsync(null);
        await fake.WaitForCallsAsync(2);

        Assert.Equal(AvatarState.Thinking, avatar.State);
        Assert.False(
            avatar.HasActiveSuccessTimer,
            "Success timer must be cancelled (or null) after a new send transitions state to Thinking.");

        pending.SetResult(FakeIrisApplicationFacade.CreateSuccessfulResult(null, "second", "ok"));
        await sendTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private sealed class MinimalFakeFacade : IIrisApplicationFacade
    {
        public Task<Result<SendMessageResult>> SendMessageAsync(
            ConversationId? conversationId,
            string message,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<SendMessageResult>.Failure(
                Error.Failure("test.not_used", "minimal fake")));
        }

        public Task<Result<RememberMemoryResult>> RememberAsync(
            string content,
            MemoryKind? kind,
            MemoryImportance? importance,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<RememberMemoryResult>.Failure(
                Error.Failure("test.not_used", "minimal fake")));
        }

        public Task<Result> ForgetAsync(
            MemoryId id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Failure(
                Error.Failure("test.not_used", "minimal fake")));
        }

        public Task<Result<UpdateMemoryResult>> UpdateAsync(
            MemoryId id,
            string newContent,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<UpdateMemoryResult>.Failure(
                Error.Failure("test.not_used", "minimal fake")));
        }

        public Task<Result<IReadOnlyList<MemoryDto>>> ListActiveMemoriesAsync(
            int? limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<IReadOnlyList<MemoryDto>>.Failure(
                Error.Failure("test.not_used", "minimal fake")));
        }
    }
}
