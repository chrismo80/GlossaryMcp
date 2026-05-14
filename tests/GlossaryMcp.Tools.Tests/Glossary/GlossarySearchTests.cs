using GlossaryMcp.Tools.Glossary;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Glossary;

public sealed class GlossarySearchTests
{
    [Fact]
    public void Exact_key_match_beats_description_match()
    {
        IReadOnlyList<GlossaryEntry> entries =
        [
            new("foo", "something"),
            new("bar", "foo")
        ];

        var results = entries.FindMatches("foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Exact_match_beats_contains_match()
    {
        IReadOnlyList<GlossaryEntry> entries =
        [
            new("foobar", "x"),
            new("foo", "y")
        ];

        var results = entries.FindMatches("foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Full_query_affects_score()
    {
        IReadOnlyList<GlossaryEntry> entries =
        [
            new("batch release", "x"),
            new("something", "batch release")
        ];

        var results = entries.FindMatches("batch release");

        results[0].Entry.Term.Is("batch release");
    }

    [Fact]
    public void Tokens_affect_score()
    {
        IReadOnlyList<GlossaryEntry> entries =
        [
            new("batch", "x"),
            new("release", "y")
        ];

        var results = entries.FindMatches("batch release");

        results.Count.Is(2);
        results.All(r => r.Score > 0).IsTrue();
    }

    [Fact]
    public void Multiple_tokens_increase_score()
    {
        IReadOnlyList<GlossaryEntry> entries = [new("batch release", "x")];

        var one = entries.FindMatches("batch").Single().Score;
        var two = entries.FindMatches("batch release").Single().Score;

        (two > one).IsTrue();
    }

    [Fact]
    public void MaxResults_limits_results()
    {
        IReadOnlyList<GlossaryEntry> entries =
        [
            new("alpha", "alpha"),
            new("bravo", "bravo"),
            new("charlie", "charlie")
        ];

        var results = entries.FindMatches("alpha bravo charlie", maxResults: 2);
        results.Count.Is(2);
    }

    [Fact]
    public void No_match_returns_empty_list()
    {
        IReadOnlyList<GlossaryEntry> entries = [new("foo", "bar")];

        var results = entries.FindMatches("does-not-exist");
        results.IsEmpty();
    }

    [Fact]
    public void Tie_breaker_prefers_shorter_term_then_alphabetical()
    {
        IReadOnlyList<GlossaryEntry> entries =
        [
            new("ab", "-"),
            new("aa", "-")
        ];

        var results = entries.FindMatches("a");

        results[0].Entry.Term.Is("aa");
    }
}
