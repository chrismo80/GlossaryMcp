using GlossaryMcp.Tools.Glossary;

namespace GlossaryMcp.Tools.Tools;

public sealed record GlossaryTerm(
    string Term,
    string Description)
{
    internal static GlossaryTerm From(GlossaryEntry entry)
        => new(entry.Term, entry.Description);
}
