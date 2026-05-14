using GlossaryMcp.Tools.Glossary;
using GlossaryMcp.Tools.Storage;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Glossary;

public sealed class GlossaryStoreTests
{
    [Fact]
    public void Load_missing_file_starts_empty()
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));

            store.EntryCount.Is(0);
            store.GetEntriesSnapshot().IsEmpty();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Theory]
    [InlineData("", "x", "invalid term")]
    [InlineData("x", "", "invalid description")]
    public void Add_validates_input(string term, string description, string expectedMessage)
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));

            var result = store.Add(term, description);

            result.Error.IsNotNull();
            result.Error!.Message.Is(expectedMessage);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Add_existing_term_returns_exists_already()
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));
            _ = store.Add("Chargenfreigabe", "first");

            var second = store.Add("CHARGENFREIGABE", "second");

            second.TotalEntries.IsNull();
            second.Error.IsNotNull();
            second.Error!.Message.Is("exists already");
            second.ExistingEntry.IsNotNull();
            second.ExistingEntry!.Description.Is("first");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Edit_existing_term_rewrites_state()
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));
            _ = store.Add("Chargenfreigabe", "old");

            var result = store.Edit("Chargenfreigabe", "new");

            result.Error.IsNull();
            result.TotalEntries.Is(1);
            store.TryGetEntry("Chargenfreigabe", out var updated).IsTrue();
            updated.IsNotNull();
            updated!.Description.Is("new");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Edit_missing_term_returns_error()
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));

            var result = store.Edit("missing", "x");

            result.TotalEntries.IsNull();
            result.Error.IsNotNull();
            result.Error!.Message.Is("term not found");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Find_prefers_exact_term_over_description_match()
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));
            _ = store.Add("foo", "something");
            _ = store.Add("bar", "foo");

            var results = store.Find("foo");

            results[0].Entry.Term.Is("foo");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Find_respects_max_results()
    {
        var path = CreateTempPath();

        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));
            _ = store.Add("alpha", "alpha");
            _ = store.Add("bravo", "bravo");
            _ = store.Add("charlie", "charlie");

            var results = store.Find("alpha bravo charlie", maxResults: 2);

            results.Count.Is(2);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Duplicate_term_in_file_throws_on_load()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path,
                "{\"term\":\"Chargenfreigabe\",\"description\":\"first\"}\n" +
                "{\"term\":\"Chargenfreigabe\",\"description\":\"second\"}\n");

            var ex = ((Action)(() => new GlossaryStore(new JsonlFile<GlossaryEntry>(path)))).IsThrowing<InvalidDataException>();
            ex.IsNotNull();
            ex!.Message.Is("Duplicate term 'Chargenfreigabe'.");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    private static string CreateTempPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "GlossaryMcpTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "glossary.jsonl");
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
