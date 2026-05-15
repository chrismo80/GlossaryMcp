namespace GlossaryMcp.Tools.Glossary;

public static class GlossarySearch
{
    private const int DefaultMaxResults = 10;

    private const int ExactTermQueryScore = 1000;
    private const int PartialTermQueryScore = 300;

    private const int ExactDescriptionQueryScore = 150;
    private const int PartialDescriptionQueryScore = 80;

    private const int ExactTermTokenScore = 120;
    private const int PartialTermTokenScore = 40;

    private const int ExactDescriptionTokenScore = 30;
    private const int PartialDescriptionTokenScore = 10;

    private const int MatchCountTieBreakerScore = 1;

    public static IReadOnlyList<GlossaryMatch> FindMatches(
        this IReadOnlyList<GlossaryEntry> entries,
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
            AddMatch(ref score, ref matchCount, ExactTermQueryScore);
        else if (term.Contains(normalizedQuery, StringComparison.Ordinal))
            AddMatch(ref score, ref matchCount, PartialTermQueryScore);

        if (description == normalizedQuery)
            AddMatch(ref score, ref matchCount, ExactDescriptionQueryScore);
        else if (description.Contains(normalizedQuery, StringComparison.Ordinal))
            AddMatch(ref score, ref matchCount, PartialDescriptionQueryScore);
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
                AddMatch(ref score, ref matchCount, ExactTermTokenScore);
            else if (term.Contains(token, StringComparison.Ordinal))
                AddMatch(ref score, ref matchCount, PartialTermTokenScore);

            if (description == token)
                AddMatch(ref score, ref matchCount, ExactDescriptionTokenScore);
            else if (description.Contains(token, StringComparison.Ordinal))
                AddMatch(ref score, ref matchCount, PartialDescriptionTokenScore);
        }
    }

    private static void AddMatch(ref int score, ref int matchCount, int delta)
    {
        score += delta;
        matchCount += MatchCountTieBreakerScore;
    }
}