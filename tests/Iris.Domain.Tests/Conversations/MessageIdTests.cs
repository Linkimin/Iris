using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class MessageIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = MessageId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => MessageId.From(Guid.Empty));

        Assert.Equal("message.empty_id", exception.Code);
    }

    [Fact]
    public void Type_DoesNotExposePublicGuidConstructor()
    {
        var constructors = typeof(MessageId).GetConstructors();

        Assert.DoesNotContain(constructors, constructor =>
        {
            var parameters = constructor.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(Guid);
        });
    }
}
