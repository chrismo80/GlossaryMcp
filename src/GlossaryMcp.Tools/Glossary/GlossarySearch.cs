namespace GlossaryMcp.Tools.Glossary;

public static class GlossarySearch
{
    public static IReadOnlyList<GlossaryMatch> FindMatches(
        this IReadOnlyList<GlossaryEntry> entries,
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

        var tokens = query.TokenizeGlossary();
        var matches = new List<GlossaryMatch>();

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedTerm = entry.Term.NormalizeGlossary();
            var normalizedDescription = entry.Description.NormalizeGlossary();

            var score = 0;
            var matchCount = 0;

            ApplyFullQueryRules(ref score, ref matchCount, normalizedTerm, normalizedDescription, normalizedQuery);
            ApplyTokenRules(ref score, ref matchCount, normalizedTerm, normalizedDescription, tokens);

            if (score > 0)
                matches.Add(new GlossaryMatch(entry, score + matchCount));
        }

        return matches
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Term.Length)
            .ThenBy(x => x.Entry.Term, StringComparer.OrdinalIgnoreCase)
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

    private static void AddMatch(ref int score, ref int matchCount, int delta)
    {
        score += delta;
        matchCount += 1;
    }
}