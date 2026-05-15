using GlossaryMcp.Tools.Glossary;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Glossary;

public sealed class GlossarySearchTests
{
    [Fact]
    public void Exact_key_match_beats_description_match()
    {
        var entries = Searchable(
            new GlossaryEntry("foo", "something"),
            new GlossaryEntry("bar", "foo"));

        var results = entries.FindMatches("foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Exact_match_beats_contains_match()
    {
        var entries = Searchable(
            new GlossaryEntry("foobar", "x"),
            new GlossaryEntry("foo", "y"));

        var results = entries.FindMatches("foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Full_query_affects_score()
    {
        var entries = Searchable(
            new GlossaryEntry("batch release", "x"),
            new GlossaryEntry("something", "batch release"));

        var results = entries.FindMatches("batch release");

        results[0].Entry.Term.Is("batch release");
    }

    [Fact]
    public void Tokens_affect_score()
    {
        var entries = Searchable(
            new GlossaryEntry("batch", "x"),
            new GlossaryEntry("release", "y"));

        var results = entries.FindMatches("batch release");

        results.Count.Is(2);
        results.All(r => r.Score > 0).IsTrue();
    }

    [Fact]
    public void Multiple_tokens_increase_score()
    {
        var entries = Searchable(new GlossaryEntry("batch release", "x"));

        var one = entries.FindMatches("batch").Single().Score;
        var two = entries.FindMatches("batch release").Single().Score;

        (two > one).IsTrue();
    }

    [Fact]
    public void Score_contains_only_ranking_rules()
    {
        var entries = Searchable(new GlossaryEntry("batch release", "x"));

        var score = entries.FindMatches("batch release").Single().Score;

        score.Is(1080);
    }

    [Fact]
    public void MaxResults_limits_results()
    {
        var entries = Searchable(
            new GlossaryEntry("alpha", "alpha"),
            new GlossaryEntry("bravo", "bravo"),
            new GlossaryEntry("charlie", "charlie"));

        var results = entries.FindMatches("alpha bravo charlie", maxResults: 2);
        results.Count.Is(2);
    }

    [Fact]
    public void No_match_returns_empty_list()
    {
        var entries = Searchable(new GlossaryEntry("foo", "bar"));

        var results = entries.FindMatches("does-not-exist");
        results.IsEmpty();
    }

    [Fact]
    public void Tie_breaker_prefers_shorter_term_then_alphabetical()
    {
        var entries = Searchable(
            new GlossaryEntry("ab", "-"),
            new GlossaryEntry("aa", "-"));

        var results = entries.FindMatches("a");

        results[0].Entry.Term.Is("aa");
    }

    private static IReadOnlyList<SearchableGlossaryEntry> Searchable(params GlossaryEntry[] entries)
        => entries.Select(SearchableGlossaryEntry.From).ToArray();
}
