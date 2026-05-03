using System.Reflection;

using Iris.Domain.Common;
using Iris.Domain.Memories;

namespace Iris.Domain.Tests.Memories;

public sealed class MemoryIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = MemoryId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => MemoryId.From(Guid.Empty));

        Assert.Equal("memory.empty_id", exception.Code);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsMatchingValue()
    {
        var guid = Guid.NewGuid();

        var id = MemoryId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Equality_TwoNewIds_AreNotEqual()
    {
        var id1 = MemoryId.New();
        var id2 = MemoryId.New();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        var id1 = MemoryId.From(guid);
        var id2 = MemoryId.From(guid);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Type_DoesNotExposePublicGuidConstructor()
    {
        ConstructorInfo[] constructors = typeof(MemoryId).GetConstructors();

        Assert.DoesNotContain(constructors, constructor =>
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(Guid);
        });
    }
}
