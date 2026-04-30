using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class MessageContentTests
{
    [Fact]
    public void Create_WithText_PreservesOriginalContent()
    {
        var content = MessageContent.Create(" hello\n");

        Assert.Equal(" hello\n", content.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithBlankText_ThrowsDomainException(string value)
    {
        DomainException exception = Assert.Throws<DomainException>(() => MessageContent.Create(value));

        Assert.Equal("message.empty_content", exception.Code);
    }
}
