using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Shared.Results;

namespace Iris.Application.Abstractions.Models.Interfaces;

public interface IChatModelClient
{
    Task<Result<ChatModelResponse>> SendAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken);
}
