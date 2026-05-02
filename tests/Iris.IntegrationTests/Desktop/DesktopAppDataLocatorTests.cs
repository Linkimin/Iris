using System;
using System.IO;

using Iris.Desktop.Hosting;

namespace Iris.IntegrationTests.Desktop;

public sealed class DesktopAppDataLocatorTests
{
    // ── T-PR-06 ──────────────────────────────────────────────────────────

    [Fact]
    public void EnsureExists_WhenDirectoryMissing_CreatesIt()
    {
        var rootOverride = Path.Combine(Path.GetTempPath(), $"iris-locator-{Guid.NewGuid():N}");
        var locator = new DesktopAppDataLocator(rootOverride);

        Assert.False(Directory.Exists(locator.AppDataDirectory));

        try
        {
            locator.EnsureExists();

            Assert.True(Directory.Exists(locator.AppDataDirectory));
            Assert.Equal(Path.Combine(rootOverride, "Iris"), locator.AppDataDirectory);
        }
        finally
        {
            if (Directory.Exists(rootOverride))
            {
                Directory.Delete(rootOverride, recursive: true);
            }
        }
    }

    [Fact]
    public void EnsureExists_IsIdempotent()
    {
        var rootOverride = Path.Combine(Path.GetTempPath(), $"iris-locator-{Guid.NewGuid():N}");
        var locator = new DesktopAppDataLocator(rootOverride);

        try
        {
            locator.EnsureExists();
            locator.EnsureExists();
            locator.EnsureExists();

            Assert.True(Directory.Exists(locator.AppDataDirectory));
        }
        finally
        {
            if (Directory.Exists(rootOverride))
            {
                Directory.Delete(rootOverride, recursive: true);
            }
        }
    }

    [Fact]
    public void Constructor_WithoutOverride_ResolvesUnderApplicationData()
    {
        var locator = new DesktopAppDataLocator();

        Assert.True(Path.IsPathFullyQualified(locator.AppDataDirectory));
        Assert.EndsWith("Iris", locator.AppDataDirectory, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WithEmptyOverride_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DesktopAppDataLocator(string.Empty));
        Assert.Throws<ArgumentException>(() => new DesktopAppDataLocator("   "));
    }
}
