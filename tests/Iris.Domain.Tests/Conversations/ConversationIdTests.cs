using System.Reflection;

using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class ConversationIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = ConversationId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => ConversationId.From(Guid.Empty));

        Assert.Equal("conversation.empty_id", exception.Code);
    }

    [Fact]
    public void Type_DoesNotExposePublicGuidConstructor()
    {
        ConstructorInfo[] constructors = typeof(ConversationId).GetConstructors();

        Assert.DoesNotContain(constructors, constructor =>
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(Guid);
        });
    }
}
