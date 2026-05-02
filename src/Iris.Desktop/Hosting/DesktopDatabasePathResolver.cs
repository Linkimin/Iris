using System;
using System.IO;

namespace Iris.Desktop.Hosting;

/// <summary>
/// Resolves the final SQLite connection string passed to <c>AddIrisPersistence</c> from the
/// optional <c>Database:ConnectionString</c> configuration override and the Iris app-data locator.
/// Pure host-internal helper; not DI-registered.
/// </summary>
internal static class DesktopDatabasePathResolver
{
    private const string _configurationKey = "Database:ConnectionString";
    private const string _dataSourcePrefix = "Data Source=";

    /// <summary>
    /// Returns the final EF Core SQLite connection string.
    /// </summary>
    /// <param name="configuredOverride">
    /// The raw value of <c>Database:ConnectionString</c> from configuration. May be <c>null</c>,
    /// whitespace, a full SQLite connection string starting with <c>"Data Source="</c>, or a
    /// bare file path. Relative paths are rejected.
    /// </param>
    /// <param name="locator">
    /// The Desktop app-data locator providing the default per-user directory.
    /// </param>
    /// <returns>
    /// A fully qualified <c>"Data Source=..."</c> connection string suitable for
    /// <c>UseSqlite(...)</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="locator"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="configuredOverride"/> is present but resolves to a relative
    /// path or to a connection string whose <c>Data Source</c> token is relative.
    /// </exception>
    internal static string Resolve(string? configuredOverride, DesktopAppDataLocator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        if (string.IsNullOrWhiteSpace(configuredOverride))
        {
            var defaultPath = Path.Combine(locator.AppDataDirectory, "iris.db");
            return _dataSourcePrefix + defaultPath;
        }

        var trimmed = configuredOverride.Trim();

        if (trimmed.StartsWith(_dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var dataSourceValue = ExtractDataSourceValue(trimmed);

            if (string.IsNullOrWhiteSpace(dataSourceValue) || !Path.IsPathFullyQualified(dataSourceValue))
            {
                throw BuildRelativePathException(configuredOverride);
            }

            return trimmed;
        }

        if (!Path.IsPathFullyQualified(trimmed))
        {
            throw BuildRelativePathException(configuredOverride);
        }

        return _dataSourcePrefix + trimmed;
    }

    /// <summary>
    /// Extracts the <c>Data Source</c> token from a connection string of the form
    /// <c>"Data Source=&lt;path&gt;[;Key=Value;...]"</c>. Returns the path portion, trimmed.
    /// </summary>
    private static string ExtractDataSourceValue(string connectionString)
    {
        // Strip the "Data Source=" prefix (length-only; case already validated by caller).
        ReadOnlySpan<char> afterPrefix = connectionString.AsSpan(_dataSourcePrefix.Length);

        var semicolonIndex = afterPrefix.IndexOf(';');
        ReadOnlySpan<char> token = semicolonIndex >= 0
            ? afterPrefix[..semicolonIndex]
            : afterPrefix;

        return token.Trim().ToString();
    }

    private static InvalidOperationException BuildRelativePathException(string rejectedValue)
    {
        return new InvalidOperationException(
            $"{_configurationKey} must be an absolute path or a connection string with an " +
            $"absolute Data Source. Got: '{rejectedValue}'.");
    }
}
