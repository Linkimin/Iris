using Iris.Application.Abstractions.Persistence;
using Iris.Application.Memory.Contracts;
using Iris.Application.Memory.Options;
using Iris.Domain.Common;
using Iris.Domain.Memories;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Memory.Commands;

public sealed class RememberExplicitFactHandler
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly MemoryOptions _memoryOptions;

    public RememberExplicitFactHandler(
        IMemoryRepository memoryRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        MemoryOptions memoryOptions)
    {
        _memoryRepository = memoryRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _memoryOptions = memoryOptions;
    }

    public async Task<Result<RememberMemoryResult>> HandleAsync(
        RememberExplicitFactCommand command,
        CancellationToken cancellationToken)
    {
        MemoryContent content;

        try
        {
            content = MemoryContent.Create(command.Content);
        }
        catch (DomainException exception)
        {
            return Result<RememberMemoryResult>.Failure(Error.Validation(exception.Code, exception.Message));
        }

        var memory = DomainMemory.Create(
            MemoryId.New(),
            content,
            command.Kind ?? MemoryKind.Note,
            command.Importance ?? MemoryImportance.Normal,
            MemorySource.UserExplicit,
            _clock.UtcNow);

        try
        {
            await _memoryRepository.AddAsync(memory, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<RememberMemoryResult>.Failure(Error.Failure(
                "memory.persistence_failed",
                "Memory could not be saved."));
        }

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<RememberMemoryResult>.Failure(Error.Failure(
                "memory.commit_failed",
                "Memory changes could not be committed."));
        }

        return Result<RememberMemoryResult>.Success(new RememberMemoryResult(MapDto(memory)));
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
