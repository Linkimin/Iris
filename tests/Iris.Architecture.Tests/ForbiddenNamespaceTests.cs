using System.Reflection;

namespace Iris.Architecture.Tests;

public class ForbiddenNamespaceTests
{
    private static readonly Assembly _domainAssembly = typeof(Iris.Domain.Conversations.ConversationMode).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(Iris.Application.Abstractions.Persistence.IUnitOfWork).Assembly;

    [Fact]
    public void Domain_does_not_reference_EntityFrameworkCore()
    {
        var efCoreTypes = _domainAssembly.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(efCoreTypes);
    }

    [Fact]
    public void Application_does_not_reference_Persistence()
    {
        var persistenceRefs = _applicationAssembly.GetReferencedAssemblies()
            .Where(a => a.Name == "Iris.Persistence")
            .ToList();

        Assert.Empty(persistenceRefs);
    }

    [Fact]
    public void Application_does_not_reference_ModelGateway()
    {
        var gatewayRefs = _applicationAssembly.GetReferencedAssemblies()
            .Where(a => a.Name == "Iris.ModelGateway")
            .ToList();

        Assert.Empty(gatewayRefs);
    }
}
