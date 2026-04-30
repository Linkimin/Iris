using System.Reflection;

namespace Iris.Architecture.Tests;

public class ProjectReferenceTests
{
    private static readonly Assembly _desktopAssembly = typeof(Iris.Desktop.Services.IIrisApplicationFacade).Assembly;
    private static readonly Assembly _apiAssembly = typeof(Iris.Api.Contracts.Common.ApiResponse).Assembly;
    private static readonly Assembly _workerAssembly = Assembly.Load("Iris.Worker");

    [Fact]
    public void Desktop_does_not_reference_Api_or_Worker()
    {
        var references = _desktopAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris.", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain("Iris.Api", references);
        Assert.DoesNotContain("Iris.Worker", references);
    }

    [Fact]
    public void Api_does_not_reference_Desktop_or_Worker()
    {
        var references = _apiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris.", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain("Iris.Desktop", references);
        Assert.DoesNotContain("Iris.Worker", references);
    }

    [Fact]
    public void Worker_does_not_reference_Desktop_or_Api()
    {
        var references = _workerAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris.", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain("Iris.Desktop", references);
        Assert.DoesNotContain("Iris.Api", references);
    }
}
