using Iris.Application.Chat.SendMessage;

namespace Iris.Application.Tests.Chat.SendMessage;

public sealed class SendMessageValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithBlankMessage_ReturnsValidationError(string message)
    {
        var validator = new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 10));

        var result = validator.Validate(new SendMessageCommand(null, message));

        Assert.True(result.IsFailure);
        Assert.Equal("chat.message_empty", result.Error.Code);
    }

    [Fact]
    public void Validate_WithTooLongMessage_ReturnsValidationError()
    {
        var validator = new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 3));

        var result = validator.Validate(new SendMessageCommand(null, "abcd"));

        Assert.True(result.IsFailure);
        Assert.Equal("chat.message_too_long", result.Error.Code);
    }

    [Fact]
    public void Validate_WithValidMessage_ReturnsSuccess()
    {
        var validator = new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 10));

        var result = validator.Validate(new SendMessageCommand(null, "hello"));

        Assert.True(result.IsSuccess);
    }
}
