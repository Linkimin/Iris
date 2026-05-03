using System.Reflection;

using Iris.Application.Abstractions.Persistence;

namespace Iris.Architecture.Tests;

public class MemoryBoundaryTests
{
    private static readonly Assembly _applicationAssembly = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly _persistenceAssembly = typeof(Iris.Persistence.Database.IrisDbContext).Assembly;

    [Fact]
    public void Domain_Memories_types_do_not_reference_EntityFrameworkCore()
    {
        Assembly domainAssembly = typeof(Iris.Domain.Memories.MemoryStatus).Assembly;
        var memoryTypes = domainAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Iris.Domain.Memories", StringComparison.Ordinal))
            .ToList();

        foreach (Type type in memoryTypes)
        {
            var efReferences = type.GetCustomAttributes(inherit: true)
                .Select(a => a.GetType().Assembly)
                .Where(a => a.GetName().Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
                .ToList();

            Assert.Empty(efReferences);
        }

        // Also verify Domain assembly doesn't reference EF Core or Persistence
        var domainReferences = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n != null)
            .ToList();

        Assert.DoesNotContain(domainReferences, r =>
            r!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(domainReferences, r => r == "Iris.Persistence");
    }

    [Fact]
    public void Application_Memory_namespace_types_do_not_reference_MemoryEntity()
    {
        var memoryTypes = _applicationAssembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith("Iris.Application.Memory", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(memoryTypes);

        foreach (Type type in memoryTypes)
        {
            HashSet<Type> referencedTypes = GetAllReferencedTypes(type);

            var referencesMemoryEntity = referencedTypes.Any(t =>
                t.FullName == "Iris.Persistence.Entities.MemoryEntity");

            Assert.False(referencesMemoryEntity,
                $"Type {type.FullName} should not reference Iris.Persistence.Entities.MemoryEntity");
        }
    }

    [Fact]
    public void Desktop_ViewModels_do_not_reference_IMemoryRepository_or_IrisDbContext()
    {
        Assembly desktopAssembly = typeof(Iris.Desktop.Services.IIrisApplicationFacade).Assembly;
        var viewModelTypes = desktopAssembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith("Iris.Desktop.ViewModels", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(viewModelTypes);

        string[] forbiddenTypeNames =
        {
            "Iris.Application.Abstractions.Persistence.IMemoryRepository",
            "Iris.Persistence.Database.IrisDbContext"
        };

        foreach (Type type in viewModelTypes)
        {
            HashSet<Type> referencedTypes = GetAllReferencedTypes(type);
            var referencedTypeFullNames = referencedTypes
                .Select(t => t.FullName)
                .ToList();

            foreach (var forbiddenName in forbiddenTypeNames)
            {
                Assert.DoesNotContain(forbiddenName, referencedTypeFullNames);
            }
        }
    }

    [Fact]
    public void Application_does_not_reference_Iris_Persistence_assembly()
    {
        // T-ARCH-MEM-01: Already covered by DependencyDirectionTests.Application_depends_only_on_Domain_and_Shared
        // This is a focused verification specifically for the memory context.
        var appReferences = _applicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain("Iris.Persistence", appReferences);
    }

    private static HashSet<Type> GetAllReferencedTypes(Type type)
    {
        var types = new HashSet<Type>();

        foreach (ConstructorInfo constructor in type.GetConstructors(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            foreach (ParameterInfo parameter in constructor.GetParameters())
            {
                types.Add(parameter.ParameterType);
                CollectGenericTypeArguments(parameter.ParameterType, types);
            }
        }

        foreach (FieldInfo field in type.GetFields(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            types.Add(field.FieldType);
            CollectGenericTypeArguments(field.FieldType, types);
        }

        foreach (PropertyInfo property in type.GetProperties(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            types.Add(property.PropertyType);
            CollectGenericTypeArguments(property.PropertyType, types);
        }

        return types;
    }

    private static void CollectGenericTypeArguments(Type type, HashSet<Type> types)
    {
        if (type.IsGenericType)
        {
            foreach (Type genericArg in type.GetGenericArguments())
            {
                types.Add(genericArg);
                CollectGenericTypeArguments(genericArg, types);
            }
        }
    }
}
