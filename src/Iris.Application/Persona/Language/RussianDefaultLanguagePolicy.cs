namespace Iris.Application.Persona.Language;

public sealed class RussianDefaultLanguagePolicy : ILanguagePolicy
{
    private readonly LanguageOptions _options;
    private readonly LanguageInstructionBuilder _builder;

    public RussianDefaultLanguagePolicy(LanguageOptions options, LanguageInstructionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(builder);

        _options = options;
        _builder = builder;
    }

    public string GetSystemPrompt()
    {
        return _builder.BuildForRussian();
    }
}
