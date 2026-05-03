using Iris.Domain.Common;
using Iris.Domain.Memories;

namespace Iris.Domain.Tests.Memories;

public sealed class MemoryContentTests
{
    [Fact]
    public void Create_WithText_PreservesOriginalContent()
    {
        var content = MemoryContent.Create(" Мой любимый язык — C#. ");

        Assert.Equal(" Мой любимый язык — C#. ", content.Value);
    }

    [Fact]
    public void Create_WithMaxLengthContent_Succeeds()
    {
        string value = new('a', MemoryContent.MaxLength);

        var content = MemoryContent.Create(value);

        Assert.Equal(value, content.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithBlankText_ThrowsDomainException(string value)
    {
        DomainException exception = Assert.Throws<DomainException>(() => MemoryContent.Create(value));

        Assert.Equal("memory.empty_content", exception.Code);
    }

    [Fact]
    public void Create_WithContentExceedingMaxLength_ThrowsDomainException()
    {
        string value = new('a', MemoryContent.MaxLength + 1);

        DomainException exception = Assert.Throws<DomainException>(() => MemoryContent.Create(value));

        Assert.Equal("memory.content_too_long", exception.Code);
    }
}
