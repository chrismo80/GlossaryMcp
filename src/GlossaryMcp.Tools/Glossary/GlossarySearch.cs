namespace GlossaryMcp.Tools.Glossary;

public static class GlossarySearch
{
    private const int DefaultMaxResults = 10;

    private const int ExactTermQueryScore = 1000;
    private const int TermContainsQueryScore = 300;
    private const int DescriptionContainsQueryScore = 80;

    private const int ExactTermTokenScore = 120;
    private const int TermContainsTokenScore = 40;

    private const int ExactDescriptionTokenScore = 30;
    private const int DescriptionContainsTokenScore = 10;

    private const int MatchedRuleBonusScore = 1;

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

        var queryText = query.NormalizeGlossary();
        if (queryText.Length == 0)
            return [];

        var queryTokens = query.TokenizeGlossary();
        var matches = new List<GlossaryMatch>();

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var termText = entry.Term.NormalizeGlossary();
            var descriptionText = entry.Description.NormalizeGlossary();

            var rankingScore = 0;
            var matchedRuleBonus = 0;

            ApplyFullQueryRules(ref rankingScore, ref matchedRuleBonus, termText, descriptionText, queryText);
            ApplyTokenRules(ref rankingScore, ref matchedRuleBonus, termText, descriptionText, queryTokens);

            if (rankingScore > 0)
                matches.Add(new GlossaryMatch(entry, rankingScore + matchedRuleBonus));
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
        ref int matchedRuleBonus,
        string term,
        string description,
        string query)
    {
        if (term == query)
            AddMatch(ref rankingScore, ref matchedRuleBonus, ExactTermQueryScore);
        else if (term.Contains(query, StringComparison.Ordinal))
            AddMatch(ref rankingScore, ref matchedRuleBonus, TermContainsQueryScore);

        if (description.Contains(query, StringComparison.Ordinal))
            AddMatch(ref rankingScore, ref matchedRuleBonus, DescriptionContainsQueryScore);
    }

    private static void ApplyTokenRules(
        ref int rankingScore,
        ref int matchedRuleBonus,
        string term,
        string description,
        IReadOnlyList<string> queryTokens)
    {
        foreach (var token in queryTokens)
        {
            if (term == token)
                AddMatch(ref rankingScore, ref matchedRuleBonus, ExactTermTokenScore);
            else if (term.Contains(token, StringComparison.Ordinal))
                AddMatch(ref rankingScore, ref matchedRuleBonus, TermContainsTokenScore);

            if (description == token)
                AddMatch(ref rankingScore, ref matchedRuleBonus, ExactDescriptionTokenScore);
            else if (description.Contains(token, StringComparison.Ordinal))
                AddMatch(ref rankingScore, ref matchedRuleBonus, DescriptionContainsTokenScore);
        }
    }

    private static void AddMatch(ref int rankingScore, ref int matchedRuleBonus, int ruleScore)
    {
        rankingScore += ruleScore;
        matchedRuleBonus += MatchedRuleBonusScore;
    }
}
