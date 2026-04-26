namespace Iris.Shared.Results;

public sealed record Error(string Code, string Message)
{
    public static Error None { get; } = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message) => new(code, message);

    public static Error Failure(string code, string message) => new(code, message);

    public bool IsNone => string.IsNullOrWhiteSpace(Code);
}
