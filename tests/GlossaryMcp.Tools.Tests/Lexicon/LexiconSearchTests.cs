using Is.Assertions;
using GlossaryMcp.Tools.Lexicon;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Lexicon;

public sealed class LexiconSearchTests
{
    [Fact]
    public void Exact_key_match_beats_description_match()
    {
        var entries = new List<LexiconEntry>
        {
            new("foo", "something"),
            new("bar", "foo")
        };

        var results = LexiconSearch.FindMatches(entries, "foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Exact_match_beats_contains_match()
    {
        var entries = new List<LexiconEntry>
        {
            new("foobar", "x"),
            new("foo", "y")
        };

        var results = LexiconSearch.FindMatches(entries, "foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Full_query_affects_score()
    {
        var entries = new List<LexiconEntry>
        {
            new("batch release", "x"),
            new("something", "batch release")
        };

        var results = LexiconSearch.FindMatches(entries, "batch release");

        results[0].Entry.Term.Is("batch release");
    }

    [Fact]
    public void Tokens_affect_score()
    {
        var entries = new List<LexiconEntry>
        {
            new("batch", "x"),
            new("release", "y"),
        };

        var results = LexiconSearch.FindMatches(entries, "batch release");

        results.Count.Is(2);
        results.All(r => r.Score > 0).IsTrue();
    }

    [Fact]
    public void Multiple_tokens_increase_score()
    {
        var entries = new List<LexiconEntry>
        {
            new("batch release", "x")
        };

        var one = LexiconSearch.FindMatches(entries, "batch").Single().Score;
        var two = LexiconSearch.FindMatches(entries, "batch release").Single().Score;

        (two > one).IsTrue();
    }

    [Fact]
    public void MaxResults_limits_results()
    {
        var entries = new List<LexiconEntry>
        {
            new("a", "a"),
            new("b", "b"),
            new("c", "c"),
        };

        var results = LexiconSearch.FindMatches(entries, "a b c", maxResults: 2);
        results.Count.Is(2);
    }

    [Fact]
    public void No_match_returns_empty_list()
    {
        var entries = new List<LexiconEntry>
        {
            new("foo", "bar")
        };

        var results = LexiconSearch.FindMatches(entries, "does-not-exist");
        results.IsEmpty();
    }

    [Fact]
    public void Tie_breaker_prefers_shorter_term_then_alphabetical()
    {
        // same score for both (contains-match on full query + token)
        var entries = new List<LexiconEntry>
        {
            new("ab", "-"),
            new("aa", "-")
        };

        var results = LexiconSearch.FindMatches(entries, "a");

        // same term length => alphabetical
        results[0].Entry.Term.Is("aa");
    }
}
