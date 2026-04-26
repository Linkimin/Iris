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
