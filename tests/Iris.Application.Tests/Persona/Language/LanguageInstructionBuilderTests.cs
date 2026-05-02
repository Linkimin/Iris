using Iris.Application.Persona.Language;

namespace Iris.Application.Tests.Persona.Language;

public sealed class LanguageInstructionBuilderTests
{
    private readonly LanguageInstructionBuilder _builder = new();

    [Fact]
    public void BuildForRussian_ReturnsNonEmptyTextContainingCyrillic()
    {
        var text = _builder.BuildForRussian();

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.True(ContainsCyrillic(text), "Expected at least one Cyrillic character in the Russian baseline.");
        Assert.Contains("Айрис", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildForRussian_ContainsTechnicalTokenRule()
    {
        var text = _builder.BuildForRussian();

        Assert.True(
            text.Contains("файл", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("пути", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("идентификатор", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("команд", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("оригинал", StringComparison.OrdinalIgnoreCase),
            "Expected a rule about preserving technical tokens (file names, paths, identifiers, commands) in English.");
    }

    [Fact]
    public void BuildForRussian_ContainsCodeStaysEnglishRule()
    {
        var text = _builder.BuildForRussian();

        Assert.True(
            (text.Contains("код", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("английск", StringComparison.OrdinalIgnoreCase)) ||
            text.Contains("комментарии в коде", StringComparison.OrdinalIgnoreCase),
            "Expected a rule about keeping code and code comments in English while prose stays in Russian.");
    }

    private static bool ContainsCyrillic(string text)
    {
        foreach (var c in text)
        {
            if (c >= '\u0400' && c <= '\u04FF')
            {
                return true;
            }
        }

        return false;
    }
}
