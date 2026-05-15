namespace GlossaryMcp.Tools.Glossary;

internal static class GlossarySearch
{
    private const int DefaultMaxResults = 10;

    private const int ExactTermQueryScore = 1000;
    private const int TermContainsQueryScore = 300;
    private const int DescriptionContainsQueryScore = 80;

    private const int ExactTermTokenScore = 120;
    private const int TermContainsTokenScore = 40;

    private const int ExactDescriptionTokenScore = 30;
    private const int DescriptionContainsTokenScore = 10;

    public static IReadOnlyList<GlossaryMatch> FindMatches(
        this IReadOnlyList<SearchableGlossaryEntry> entries,
        string query,
        int maxResults = DefaultMaxResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(query);

        cancellationToken.ThrowIfCancellationRequested();

        if (maxResults <= 0)
            return [];

        var normalizedQuery = query.NormalizeGlossary();
        if (normalizedQuery.Length == 0)
            return [];

        var queryTokens = normalizedQuery.TokenizeNormalizedGlossary();
        var matches = new List<GlossaryMatch>();

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rankingScore = 0;

            ApplyFullQueryRules(ref rankingScore, entry.NormalizedTerm, entry.NormalizedDescription, normalizedQuery);
            ApplyTokenRules(ref rankingScore, entry.NormalizedTerm, entry.NormalizedDescription, queryTokens);

            if (rankingScore > 0)
                matches.Add(new GlossaryMatch(entry.Entry, rankingScore));
        }

        return matches
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Term.Length)
            .ThenBy(x => x.Entry.Term, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToArray();
    }

    private static void ApplyFullQueryRules(
        ref int rankingScore,
        string term,
        string description,
        string query)
    {
        if (term == query)
            AddMatch(ref rankingScore, ExactTermQueryScore);
        else if (term.Contains(query, StringComparison.Ordinal))
            AddMatch(ref rankingScore, TermContainsQueryScore);

        if (description.Contains(query, StringComparison.Ordinal))
            AddMatch(ref rankingScore, DescriptionContainsQueryScore);
    }

    private static void ApplyTokenRules(
        ref int rankingScore,
        string term,
        string description,
        IReadOnlyList<string> queryTokens)
    {
        foreach (var token in queryTokens)
        {
            if (term == token)
                AddMatch(ref rankingScore, ExactTermTokenScore);
            else if (term.Contains(token, StringComparison.Ordinal))
                AddMatch(ref rankingScore, TermContainsTokenScore);

            if (description == token)
                AddMatch(ref rankingScore, ExactDescriptionTokenScore);
            else if (description.Contains(token, StringComparison.Ordinal))
                AddMatch(ref rankingScore, DescriptionContainsTokenScore);
        }
    }

    private static void AddMatch(ref int rankingScore, int ruleScore)
    {
        rankingScore += ruleScore;
    }
}
