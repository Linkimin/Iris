namespace Iris.Desktop.Models;

/// <summary>
/// Visual state of the Iris avatar.
/// </summary>
public enum AvatarState
{
    /// <summary>
    /// Default, waiting for user input.
    /// </summary>
    Idle,

    /// <summary>
    /// A message is being sent and the model response is pending.
    /// </summary>
    Thinking,

    /// <summary>
    /// Reserved for Voice v1 integration. Not reachable in Avatar v1.
    /// </summary>
    Speaking,

    /// <summary>
    /// A successful assistant message was received.
    /// </summary>
    Success,

    /// <summary>
    /// An error occurred during the last send operation.
    /// </summary>
    Error,

    /// <summary>
    /// Avatar is disabled. No visual representation.
    /// </summary>
    Hidden
}
