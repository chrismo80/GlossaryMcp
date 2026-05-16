using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Glossary;

public sealed class GlossaryStoreTests
{
    [Fact]
    public void Load_missing_file_starts_empty()
    {
        using var glossary = TestGlossary.Create();

        glossary.Store.EntryCount.Is(0);
        glossary.Store.GetEntriesSnapshot().IsEmpty();
    }

    [Theory]
    [InlineData("", "x", "invalid term")]
    [InlineData("x", "", "invalid description")]
    public void Add_validates_input(string term, string description, string expectedMessage)
    {
        using var glossary = TestGlossary.Create();

        var result = glossary.Store.Add(term, description);

        result.Error.IsNotNull();
        result.Error!.Message.Is(expectedMessage);
    }

    [Fact]
    public void Add_existing_term_returns_exists_already()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "first");

        var second = glossary.Store.Add("CHARGENFREIGABE", "second");

        second.TotalEntries.IsNull();
        second.Error.IsNotNull();
        second.Error!.Message.Is("exists already");
        second.ExistingEntry.IsNotNull();
        second.ExistingEntry!.Description.Is("first");
    }

    [Fact]
    public void Edit_existing_term_rewrites_state()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "old");

        var result = glossary.Store.Edit("Chargenfreigabe", "new");

        result.Error.IsNull();
        result.TotalEntries.Is(1);
        glossary.Store.TryGetEntry("Chargenfreigabe", out var updated).IsTrue();
        updated.IsNotNull();
        updated!.Description.Is("new");
    }

    [Fact]
    public void Edit_existing_term_updates_search_state()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "obsoleteword");

        glossary.Store.Edit("Chargenfreigabe", "freshword");

        glossary.Store.Find("freshword").Single().Entry.Term.Is("Chargenfreigabe");
        glossary.Store.Find("obsoleteword").IsEmpty();
    }

    [Fact]
    public void Edit_missing_term_returns_error()
    {
        using var glossary = TestGlossary.Create();

        var result = glossary.Store.Edit("missing", "x");

        result.TotalEntries.IsNull();
        result.Error.IsNotNull();
        result.Error!.Message.Is("term not found");
    }

    [Fact]
    public void Find_prefers_exact_term_over_description_match()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("foo", "something");
        glossary.Store.Add("bar", "foo");

        var results = glossary.Store.Find("foo");

        results[0].Entry.Term.Is("foo");
    }

    [Fact]
    public void Find_respects_max_results()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("alpha", "alpha");
        glossary.Store.Add("bravo", "bravo");
        glossary.Store.Add("charlie", "charlie");

        var results = glossary.Store.Find("alpha bravo charlie", maxResults: 2);

        results.Count.Is(2);
    }

    [Fact]
    public void Map_lists_every_term()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Production Batch", "Batch.");
        glossary.Store.Add("Batch Release", "Approval.");

        var map = glossary.Store.Map();

        map.Select(term => term.Term).Is(["Production Batch", "Batch Release"]);
    }

    [Fact]
    public void Map_lists_terms_whose_descriptions_mention_target_term()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Production Batch", "Batch.");
        glossary.Store.Add("Batch Release", "Approval of a Production Batch.");
        glossary.Store.Add("Batch Record", "Documentation for a Production Batch.");

        var map = glossary.Store.Map();

        map.Single(term => term.Term == "Production Batch")
            .MentionedIn.Is(["Batch Release", "Batch Record"]);
    }

    [Fact]
    public void Map_ignores_self_matches()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Batch Release", "A Batch Release is an approval.");

        var map = glossary.Store.Map();

        map.Single().MentionedIn.IsEmpty();
    }

    [Fact]
    public void Map_uses_normalized_containment()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "Freigabe.");
        glossary.Store.Add("Audit", "Review of a CHARGENFREIGABE.");

        var map = glossary.Store.Map();

        map.Single(term => term.Term == "Chargenfreigabe")
            .MentionedIn.Is(["Audit"]);
    }

    [Fact]
    public void Map_can_list_only_requested_term()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Production Batch", "Batch.");
        glossary.Store.Add("Batch Release", "Approval of a Production Batch.");

        var map = glossary.Store.Map("production batch");

        map.Select(term => term.Term).Is(["Production Batch"]);
        map.Single().MentionedIn.Is(["Batch Release"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Delete_validates_input(string term)
    {
        using var glossary = TestGlossary.Create();

        var result = glossary.Store.Delete(term);

        result.TotalEntries.IsNull();
        result.DeletedEntry.IsNull();
        result.Error.IsNotNull();
        result.Error!.Message.Is("invalid term");
    }

    [Fact]
    public void Delete_missing_term_returns_error()
    {
        using var glossary = TestGlossary.Create();

        var result = glossary.Store.Delete("missing");

        result.TotalEntries.IsNull();
        result.DeletedEntry.IsNull();
        result.Error.IsNotNull();
        result.Error!.Message.Is("term not found");
    }

    [Fact]
    public void Delete_existing_term_removes_entry_and_rewrites_file()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "deleted");
        glossary.Store.Add("Batch Release", "kept");

        var result = glossary.Store.Delete("  CHARGENFREIGABE  ");

        result.Error.IsNull();
        result.TotalEntries.Is(1);
        result.DeletedEntry.IsNotNull();
        result.DeletedEntry!.Term.Is("Chargenfreigabe");
        glossary.Store.TryGetEntry("Chargenfreigabe", out _).IsFalse();
        glossary.Store.TryGetEntry("Batch Release", out var kept).IsTrue();
        kept.IsNotNull();
        kept!.Description.Is("kept");
        File.ReadAllLines(glossary.Path).Length.Is(1);
    }

    [Fact]
    public void Duplicate_term_in_file_throws_on_load()
    {
        using var glossary = TestGlossary.Create();
        File.WriteAllText(glossary.Path,
            "{\"term\":\"Chargenfreigabe\",\"description\":\"first\"}\n" +
            "{\"term\":\"Chargenfreigabe\",\"description\":\"second\"}\n");

        var ex = ((Action)(() => glossary.Store.GetEntriesSnapshot())).IsThrowing<InvalidDataException>();

        ex.IsNotNull();
        ex!.Message.Is("Duplicate term 'Chargenfreigabe'.");
    }
}
