namespace Iris.Desktop.Models;

/// <summary>
/// Configuration options for the avatar system.
/// Created from <c>appsettings.json</c> at startup via DI.
/// </summary>
/// <param name="Enabled">Whether the avatar is visible.</param>
/// <param name="Size">Visual size of the avatar.</param>
/// <param name="Position">Screen corner position.</param>
/// <param name="SuccessDisplayDurationSeconds">How long the Success state is shown before returning to Idle.</param>
public sealed record AvatarOptions(
    bool Enabled,
    AvatarSize Size,
    AvatarPosition Position,
    double SuccessDisplayDurationSeconds);
