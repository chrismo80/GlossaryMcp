namespace GlossaryMcp.Tools.Lexicon;

public sealed class LexiconStore
{
    public List<LexiconEntry> Entries { get; } = [];
    public Dictionary<string, LexiconEntry> ByNormalizedTerm { get; } = new(StringComparer.Ordinal);
}
