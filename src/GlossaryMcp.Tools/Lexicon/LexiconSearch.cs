namespace GlossaryMcp.Tools.Lexicon;

public static class LexiconSearch
{
    public static IReadOnlyList<LexiconMatch> FindMatches(
        IReadOnlyList<LexiconEntry> entries,
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (entries is null)
            throw new ArgumentNullException(nameof(entries));
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        cancellationToken.ThrowIfCancellationRequested();

        if (maxResults <= 0)
            return [];

        var normalizedQuery = query.NormalizeGlossary();
        if (normalizedQuery.Length == 0)
            return [];

        var tokens = query.TokenizeGlossary();

        var matches = new List<LexiconMatch>();

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var term = entry.Term.NormalizeGlossary();
            var description = entry.Description.NormalizeGlossary();

            var score = 0;
            var matchCount = 0;

            ApplyFullQueryRules(ref score, ref matchCount, term, description, normalizedQuery);
            ApplyTokenRules(ref score, ref matchCount, term, description, tokens);

            if (score > 0)
                matches.Add(new LexiconMatch(entry, score + matchCount));
        }

        return matches
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Entry.Term.Length)
            .ThenBy(m => m.Entry.Term, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToArray();
    }

    private static void ApplyFullQueryRules(
        ref int score,
        ref int matchCount,
        string term,
        string description,
        string normalizedQuery)
    {
        if (term == normalizedQuery)
            AddMatch(ref score, ref matchCount, 1000);
        else if (term.Contains(normalizedQuery, StringComparison.Ordinal))
            AddMatch(ref score, ref matchCount, 300);

        if (description == normalizedQuery)
            AddMatch(ref score, ref matchCount, 150);
        else if (description.Contains(normalizedQuery, StringComparison.Ordinal))
            AddMatch(ref score, ref matchCount, 80);
    }

    private static void ApplyTokenRules(
        ref int score,
        ref int matchCount,
        string term,
        string description,
        IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (term == token)
                AddMatch(ref score, ref matchCount, 120);
            else if (term.Contains(token, StringComparison.Ordinal))
                AddMatch(ref score, ref matchCount, 40);

            if (description == token)
                AddMatch(ref score, ref matchCount, 30);
            else if (description.Contains(token, StringComparison.Ordinal))
                AddMatch(ref score, ref matchCount, 10);
        }
    }

    private static void AddMatch(ref int score, ref int matchCount, int add)
    {
        score += add;
        matchCount += 1;
    }
}

public sealed record LexiconMatch(
    LexiconEntry Entry,
    int Score);
