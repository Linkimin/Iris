using Iris.Shared.Results;

namespace Iris.Domain.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithNullReferenceValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }
}
