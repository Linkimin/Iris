using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Memories;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Memory.Commands;

public sealed class ForgetMemoryHandler
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ForgetMemoryHandler(
        IMemoryRepository memoryRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _memoryRepository = memoryRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(
        ForgetMemoryCommand command,
        CancellationToken cancellationToken)
    {
        DomainMemory? memory;

        try
        {
            memory = await _memoryRepository.GetByIdAsync(command.Id, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Failure(Error.Failure(
                "memory.persistence_failed",
                "Memory could not be loaded."));
        }

        if (memory is null)
        {
            return Result.Failure(Error.Failure(
                "memory.not_found",
                "Memory was not found."));
        }

        var changed = memory.Forget(_clock.UtcNow);

        if (!changed)
        {
            return Result.Success();
        }

        try
        {
            await _memoryRepository.UpdateAsync(memory, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Failure(Error.Failure(
                "memory.persistence_failed",
                "Memory could not be updated."));
        }

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Failure(Error.Failure(
                "memory.commit_failed",
                "Memory changes could not be committed."));
        }

        return Result.Success();
    }
}
