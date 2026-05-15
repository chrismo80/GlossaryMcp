namespace GlossaryMcp.Tools.Glossary;

internal static class GlossarySearch
{
    private const int TermScore = 10;

    private const int FullScore = 10;
    private const int TokenScore = 5;

    private const int ExactScore = 10;
    private const int ContainsScore = 3;

    public static IReadOnlyList<GlossaryMatch> FindMatches(
        this IReadOnlyList<SearchableGlossaryEntry> entries,
        string query,
        int maxResults = 10,
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

        return entries
            .Select(entry =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new GlossaryMatch(entry.Entry, entry.Score(normalizedQuery, queryTokens));
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Term.Length)
            .ThenBy(x => x.Entry.Term, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToArray();
    }

    private static int Score(this SearchableGlossaryEntry entry, string query, IReadOnlyList<string> queryTokens)
    {
        return
            entry.NormalizedTerm.Scores(query, queryTokens).Sum() * TermScore +
            entry.NormalizedDescription.Scores(query, queryTokens).Sum();
    }

    private static IEnumerable<int> Scores(this string text, string query, IReadOnlyList<string> queryTokens)
    {
        yield return text.Score(query) * FullScore;

        foreach (var token in queryTokens)
            yield return text.Score(token) * TokenScore;

        foreach (var token in text.TokenizeNormalizedGlossary())
            yield return token.Score(query) * TokenScore;
    }

    private static int Score(this string text, string query)
    {
        if (text == query)
            return query.Length * ExactScore;

        if (text.Contains(query, StringComparison.Ordinal))
            return query.Length * ContainsScore;

        return 0;
    }
}
