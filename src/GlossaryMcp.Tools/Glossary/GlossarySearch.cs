namespace GlossaryMcp.Tools.Glossary;

internal static class GlossarySearch
{
    private const int TermWeight = 10;
    private const int FullTextWeight = 10;
    private const int ExactMatchWeight = 3;
    private const int MatchLengthWeight = 2;

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

        var queryTokens = normalizedQuery.TokenizeGlossary();

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
            entry.NormalizedTerm.Scores(query, queryTokens).Sum() * TermWeight +
            entry.NormalizedDescription.Scores(query, queryTokens).Sum();
    }

    extension(string text)
    {
        private IEnumerable<int> Scores(string query, IReadOnlyList<string> queryTokens)
        {
            yield return text.Score(query) * FullTextWeight;

            foreach (var token in queryTokens)
                yield return text.Score(token);

            foreach (var token in text.TokenizeGlossary())
                yield return token.Score(query);
        }

        private int Score(string query)
        {
            var baseScore = query.Length * MatchLengthWeight;

            if (text == query)
                return baseScore * ExactMatchWeight;

            if (text.Contains(query, StringComparison.Ordinal))
                return baseScore;

            return 0;
        }
    }
}