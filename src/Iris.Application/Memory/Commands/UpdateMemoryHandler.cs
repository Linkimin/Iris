using Iris.Application.Abstractions.Persistence;
using Iris.Application.Memory.Contracts;
using Iris.Domain.Common;
using Iris.Domain.Memories;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Memory.Commands;

public sealed class UpdateMemoryHandler
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateMemoryHandler(
        IMemoryRepository memoryRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _memoryRepository = memoryRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<UpdateMemoryResult>> HandleAsync(
        UpdateMemoryCommand command,
        CancellationToken cancellationToken)
    {
        DomainMemory? memory;

        try
        {
            memory = await _memoryRepository.GetByIdAsync(command.Id, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<UpdateMemoryResult>.Failure(Error.Failure(
                "memory.persistence_failed",
                "Memory could not be loaded."));
        }

        if (memory is null)
        {
            return Result<UpdateMemoryResult>.Failure(Error.Failure(
                "memory.not_found",
                "Memory was not found."));
        }

        MemoryContent newContent;

        try
        {
            newContent = MemoryContent.Create(command.NewContent);
        }
        catch (DomainException exception)
        {
            return Result<UpdateMemoryResult>.Failure(Error.Validation(exception.Code, exception.Message));
        }

        try
        {
            memory.UpdateContent(newContent, _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result<UpdateMemoryResult>.Failure(Error.Failure(exception.Code, exception.Message));
        }

        try
        {
            await _memoryRepository.UpdateAsync(memory, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<UpdateMemoryResult>.Failure(Error.Failure(
                "memory.persistence_failed",
                "Memory could not be updated."));
        }

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<UpdateMemoryResult>.Failure(Error.Failure(
                "memory.commit_failed",
                "Memory changes could not be committed."));
        }

        return Result<UpdateMemoryResult>.Success(new UpdateMemoryResult(MapDto(memory)));
    }

    private static MemoryDto MapDto(DomainMemory memory)
    {
        return new MemoryDto(
            memory.Id,
            memory.Content.Value,
            memory.Kind,
            memory.Importance,
            memory.Status,
            memory.CreatedAt,
            memory.UpdatedAt);
    }
}
