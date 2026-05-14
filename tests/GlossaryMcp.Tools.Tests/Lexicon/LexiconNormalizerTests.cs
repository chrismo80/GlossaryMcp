using Is.Assertions;
using GlossaryMcp.Tools.Lexicon;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Lexicon;

public sealed class LexiconNormalizerTests
{
    [Theory]
    [InlineData("  abc  ", "abc")]
    [InlineData("AbC", "abc")]
    [InlineData("a   b\t\n  c", "a b c")]
    [InlineData("ä ö ü ß", "ae oe ue ss")]
    public void NormalizeGlossary_normalizes_input(string input, string expected)
    {
        input.NormalizeGlossary().Is(expected);
    }

    [Theory]
    [InlineData("Batch Release", new[] { "batch", "release" })]
    [InlineData("  a   b  ", new[] { "a", "b" })]
    [InlineData("a a b b", new[] { "a", "b" })]
    public void TokenizeGlossary_returns_expected_tokens(string input, string[] expected)
    {
        input.TokenizeGlossary().Is(expected);
    }
}
