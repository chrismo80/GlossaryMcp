namespace GlossaryMcp.Tools.Glossary;

internal sealed record GlossaryEntry(
    string Term,
    string Description);

internal sealed class SearchableGlossaryEntry
{
    private SearchableGlossaryEntry(GlossaryEntry entry)
    {
        Entry = entry;
        NormalizedTerm = entry.Term.NormalizeGlossary();
        NormalizedDescription = entry.Description.NormalizeGlossary();
    }

    public GlossaryEntry Entry { get; }

    public string NormalizedTerm { get; }

    public string NormalizedDescription { get; }

    public static SearchableGlossaryEntry From(GlossaryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new SearchableGlossaryEntry(entry);
    }
}
