using Iris.Application.Persona.Language;

namespace Iris.Application.Tests.Persona.Language;

public sealed class RussianDefaultLanguagePolicyTests
{
    [Fact]
    public void GetSystemPrompt_WhenDefaultLanguageIsRussian_ReturnsNonEmptyString()
    {
        RussianDefaultLanguagePolicy policy = CreatePolicy(new LanguageOptions("ru"));

        var prompt = policy.GetSystemPrompt();

        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSystemPrompt_WhenDefaultLanguageIsNullOrWhitespace_FallsBackToRussian(string? language)
    {
        RussianDefaultLanguagePolicy policy = CreatePolicy(new LanguageOptions(language!));
        var canonicalPrompt = CreatePolicy(new LanguageOptions("ru")).GetSystemPrompt();

        var prompt = policy.GetSystemPrompt();

        Assert.Equal(canonicalPrompt, prompt);
    }

    [Theory]
    [InlineData("jp")]
    [InlineData("en")]
    [InlineData("xyz")]
    public void GetSystemPrompt_WhenDefaultLanguageIsUnknown_FallsBackToRussian(string language)
    {
        RussianDefaultLanguagePolicy policy = CreatePolicy(new LanguageOptions(language));
        var canonicalPrompt = CreatePolicy(new LanguageOptions("ru")).GetSystemPrompt();

        var prompt = policy.GetSystemPrompt();

        Assert.Equal(canonicalPrompt, prompt);
    }

    private static RussianDefaultLanguagePolicy CreatePolicy(LanguageOptions options)
    {
        return new RussianDefaultLanguagePolicy(options, new LanguageInstructionBuilder());
    }
}
