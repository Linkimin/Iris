using System.Reflection;

namespace Iris.Architecture.Tests;

public class DependencyDirectionTests
{
    private static readonly Assembly _domainAssembly = typeof(Iris.Domain.Conversations.ConversationMode).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(Iris.Application.Abstractions.Persistence.IUnitOfWork).Assembly;
    private static readonly Assembly _sharedAssembly = typeof(Iris.Shared.Results.Result).Assembly;

    [Fact]
    public void Domain_depends_only_on_Shared()
    {
        var referencedAssemblies = _domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris.", StringComparison.Ordinal))
            .ToList();

        // Domain must NOT reference Application or any adapter/host.
        // Shared is the only acceptable Iris dependency (if present).
        var forbidden = referencedAssemblies
            .Where(r => r != "Iris.Shared")
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Application_depends_only_on_Domain_and_Shared()
    {
        var referencedAssemblies = _applicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris.", StringComparison.Ordinal))
            .ToList();

        var allowed = new[] { "Iris.Domain", "Iris.Shared" };
        var violations = referencedAssemblies
            .Where(r => !allowed.Contains(r))
            .ToList();

        Assert.Empty(violations);
    }
}
