using System;
using System.IO;

namespace Iris.Desktop.Hosting;

/// <summary>
/// Resolves the absolute Iris per-user application data directory and ensures it exists.
/// Constructed once at composition time inside <c>AddIrisDesktop</c>; never DI-registered.
/// </summary>
internal sealed class DesktopAppDataLocator
{
    /// <summary>
    /// Production constructor. Resolves the path under
    /// <see cref="Environment.SpecialFolder.ApplicationData"/> and joins with <c>"Iris"</c>.
    /// </summary>
    public DesktopAppDataLocator()
    {
        var root = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "Could not resolve user application data directory. " +
                "Environment.SpecialFolder.ApplicationData returned an empty value.");
        }

        AppDataDirectory = Path.Combine(root, "Iris");
    }

    /// <summary>
    /// Test-only seam. Allows constructing the locator with an explicit absolute root override
    /// so tests do not pollute the developer's real <c>%APPDATA%</c>.
    /// Visible to <c>Iris.Integration.Tests</c> via <c>InternalsVisibleTo</c>.
    /// </summary>
    /// <param name="rootOverride">Absolute path to use as the parent of the <c>Iris</c> subdirectory.</param>
    internal DesktopAppDataLocator(string rootOverride)
    {
        if (string.IsNullOrWhiteSpace(rootOverride))
        {
            throw new ArgumentException(
                "Root override must be a non-empty absolute path.",
                nameof(rootOverride));
        }

        AppDataDirectory = Path.Combine(rootOverride, "Iris");
    }

    /// <summary>
    /// Absolute path to the Iris per-user data directory.
    /// </summary>
    public string AppDataDirectory { get; }

    /// <summary>
    /// Ensures the Iris app-data directory exists. Idempotent.
    /// Wraps OS-level failures in <see cref="InvalidOperationException"/> with the path attached.
    /// </summary>
    public void EnsureExists()
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Could not create Iris app data directory at '{AppDataDirectory}'.",
                ex);
        }
    }
}
