using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Memory.Contracts;
using Iris.Desktop.ViewModels;
using Iris.Domain.Memories;
using Iris.IntegrationTests.Testing;

namespace Iris.IntegrationTests.Desktop;

public sealed class MemoryViewModelTests
{
    private static MemoryDto CreateDto(string content)
    {
        return new MemoryDto(
            MemoryId.New(),
            content,
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemoryStatus.Active,
            DateTimeOffset.UtcNow,
            null);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!condition() && !cts.IsCancellationRequested)
        {
            await Task.Delay(20, cts.Token).ConfigureAwait(false);
        }
    }

    // T-DESK-MEM-01: MemoryViewModel auto-loads on construction; Memories populated after fire-and-forget.
    [Fact]
    public async Task Constructor_AutoLoadsMemories_OnCreation()
    {
        var facade = new FakeIrisApplicationFacade();
        MemoryDto dto1 = CreateDto("Айрис любит котиков");
        MemoryDto dto2 = CreateDto("Пользователь программист");
        facade.EnqueueListMemoriesSuccess(dto1, dto2);

        var viewModel = new MemoryViewModel(facade);

        // Wait for fire-and-forget load to complete
        await WaitForConditionAsync(
            () => !viewModel.IsLoading && viewModel.Memories.Count > 0,
            TimeSpan.FromSeconds(5));

        Assert.Equal(2, viewModel.Memories.Count);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }

    // T-DESK-MEM-02: ForgetCommand invokes facade with correct id and reloads memories.
    [Fact]
    public async Task ForgetCommand_InvokesFacadeAndReloadsMemories()
    {
        var facade = new FakeIrisApplicationFacade();
        MemoryDto dto1 = CreateDto("Memory to forget");
        MemoryDto dto2 = CreateDto("Memory to keep");

        // First load: both memories
        facade.EnqueueListMemoriesSuccess(dto1, dto2);
        // Second load (after forget): only dto2 remains
        facade.EnqueueListMemoriesSuccess(dto2);

        var viewModel = new MemoryViewModel(facade);

        // Wait for initial load to complete
        await WaitForConditionAsync(
            () => !viewModel.IsLoading && viewModel.Memories.Count == 2,
            TimeSpan.FromSeconds(5));

        Assert.Equal(2, viewModel.Memories.Count);

        // Execute ForgetCommand with dto1.Id
        await viewModel.ForgetCommand.ExecuteAsync(dto1.Id);

        // Wait for reload to complete
        await WaitForConditionAsync(
            () => !viewModel.IsLoading && viewModel.Memories.Count == 1,
            TimeSpan.FromSeconds(5));

        MemoryId calledId = Assert.Single(facade.ForgetCalls);
        Assert.Equal(dto1.Id, calledId);
        Assert.Single(viewModel.Memories);
    }
}
