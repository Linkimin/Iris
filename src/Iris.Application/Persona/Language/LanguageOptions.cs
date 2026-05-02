namespace Iris.Application.Persona.Language;

public sealed record LanguageOptions(string DefaultLanguage)
{
    public static LanguageOptions Default { get; } = new("ru");
}
