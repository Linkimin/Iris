using System;
using System.IO;

using Iris.Desktop.Hosting;

namespace Iris.IntegrationTests.Desktop;

public sealed class DesktopDatabasePathResolverTests
{
    // ── T-PR-01 ──────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WithoutOverride_ReturnsAbsoluteDataSourceUnderAppDataIris()
    {
        var rootOverride = Path.Combine(Path.GetTempPath(), $"iris-resolver-{Guid.NewGuid():N}");
        var locator = new DesktopAppDataLocator(rootOverride);

        var result = DesktopDatabasePathResolver.Resolve(null, locator);

        Assert.StartsWith("Data Source=", result, StringComparison.Ordinal);

        var dataSource = result["Data Source=".Length..];
        Assert.True(Path.IsPathFullyQualified(dataSource));
        Assert.EndsWith("iris.db", dataSource, StringComparison.Ordinal);
        Assert.Equal(Path.Combine(locator.AppDataDirectory, "iris.db"), dataSource);
    }

    [Fact]
    public void Resolve_WithWhitespaceOverride_TreatedAsAbsent()
    {
        var rootOverride = Path.Combine(Path.GetTempPath(), $"iris-resolver-{Guid.NewGuid():N}");
        var locator = new DesktopAppDataLocator(rootOverride);

        var result = DesktopDatabasePathResolver.Resolve("   ", locator);

        Assert.Equal($"Data Source={Path.Combine(locator.AppDataDirectory, "iris.db")}", result);
    }

    // ── T-PR-02 ──────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WithFullConnectionStringAndAbsoluteDataSource_ReturnsVerbatim()
    {
        var locator = new DesktopAppDataLocator(Path.GetTempPath());
        var absolutePath = Path.Combine(Path.GetTempPath(), "iris-dev.db");
        var input = $"Data Source={absolutePath};Cache=Shared";

        var result = DesktopDatabasePathResolver.Resolve(input, locator);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Resolve_WithFullConnectionStringMixedCasePrefix_ReturnsVerbatim()
    {
        var locator = new DesktopAppDataLocator(Path.GetTempPath());
        var absolutePath = Path.Combine(Path.GetTempPath(), "iris-dev.db");
        var input = $"DATA SOURCE={absolutePath}";

        var result = DesktopDatabasePathResolver.Resolve(input, locator);

        Assert.Equal(input, result);
    }

    // ── T-PR-03 ──────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WithBareAbsolutePath_NormalizesToDataSource()
    {
        var locator = new DesktopAppDataLocator(Path.GetTempPath());
        var absolutePath = Path.Combine(Path.GetTempPath(), "iris-dev.db");

        var result = DesktopDatabasePathResolver.Resolve(absolutePath, locator);

        Assert.Equal($"Data Source={absolutePath}", result);
    }

    // ── T-PR-04 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("iris.db")]
    [InlineData("./data/iris.db")]
    [InlineData("..\\foo.db")]
    public void Resolve_WithBareRelativePath_Throws(string relativePath)
    {
        var locator = new DesktopAppDataLocator(Path.GetTempPath());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            DesktopDatabasePathResolver.Resolve(relativePath, locator));

        Assert.Contains("Database:ConnectionString", ex.Message, StringComparison.Ordinal);
        Assert.Contains(relativePath, ex.Message, StringComparison.Ordinal);
    }

    // ── T-PR-05 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Data Source=iris.db")]
    [InlineData("Data Source=./foo.db;Cache=Shared")]
    public void Resolve_WithConnectionStringWhoseDataSourceIsRelative_Throws(string connectionString)
    {
        var locator = new DesktopAppDataLocator(Path.GetTempPath());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            DesktopDatabasePathResolver.Resolve(connectionString, locator));

        Assert.Contains("Database:ConnectionString", ex.Message, StringComparison.Ordinal);
        Assert.Contains(connectionString, ex.Message, StringComparison.Ordinal);
    }

    // ── Locator argument guard ──────────────────────────────────────────

    [Fact]
    public void Resolve_WithNullLocator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DesktopDatabasePathResolver.Resolve(null, locator: null!));
    }
}
