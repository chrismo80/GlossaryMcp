using Is.Assertions;
using System.Text;
using System.Text.Json;
using GlossaryMcp.Tools.Lexicon;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Lexicon;

public sealed class LexiconFileStoreMutationTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void AddTerm_new_term_appends_to_file_and_store()
    {
        var path = CreateTempPath();
        try
        {
            var fileStore = new LexiconFileStore(path);

            var result = fileStore.AddTerm("Chargenfreigabe", "desc");

            result.TotalEntries.Is(1);
            result.Error.IsNull();
            result.ExistingEntry.IsNull();

            File.Exists(path).IsTrue();

            var lines = File.ReadAllLines(path, Utf8NoBom);
            lines.Count().Is(1);

            var entry = JsonSerializer.Deserialize<LexiconEntry>(lines[0], new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            entry.IsNotNull();
            entry!.Term.Is("Chargenfreigabe");
            entry.Description.Is("desc");

            fileStore.GetEntriesSnapshot().Count.Is(1);
            fileStore.EntryCount.Is(1);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Theory]
    [InlineData("Chargenfreigabe")]
    [InlineData("CHARGENFREIGABE")]
    public void AddTerm_existing_term_returns_exists_already_and_does_not_append(string duplicateTerm)
    {
        var path = CreateTempPath();
        try
        {
            var fileStore = new LexiconFileStore(path);

            _ = fileStore.AddTerm("Chargenfreigabe", "first");
            var second = fileStore.AddTerm(duplicateTerm, "second");

            second.TotalEntries.IsNull();
            second.Error.IsNotNull();
            second.Error!.Message.Is("exists already");
            second.ExistingEntry.IsNotNull();
            second.ExistingEntry!.Description.Is("first");

            var lines = File.ReadAllLines(path, Utf8NoBom);
            lines.Count().Is(1);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void EditTerm_existing_term_rewrites_file_and_updates_store()
    {
        var path = CreateTempPath();
        try
        {
            var fileStore = new LexiconFileStore(path);
            _ = fileStore.AddTerm("Chargenfreigabe", "old");

            var edit = fileStore.EditTerm("Chargenfreigabe", "new");

            edit.TotalEntries.Is(1);
            edit.Error.IsNull();

            var lines = File.ReadAllLines(path, Utf8NoBom);
            lines.Count().Is(1);

            var entry = JsonSerializer.Deserialize<LexiconEntry>(lines[0], new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            entry.IsNotNull();
            entry!.Description.Is("new");

            fileStore.TryGetEntry("Chargenfreigabe", out var updated).IsTrue();
            updated.IsNotNull();
            updated!.Description.Is("new");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void EditTerm_does_not_leave_fixed_temp_file_behind()
    {
        var path = CreateTempPath();
        try
        {
            var fileStore = new LexiconFileStore(path);
            _ = fileStore.AddTerm("Chargenfreigabe", "old");

            var edit = fileStore.EditTerm("Chargenfreigabe", "new");

            edit.Error.IsNull();
            File.Exists(path + ".tmp").IsFalse();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void FindMatches_honors_pre_canceled_token()
    {
        var path = CreateTempPath();
        try
        {
            var fileStore = new LexiconFileStore(path);
            _ = fileStore.AddTerm("Chargenfreigabe", "desc");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            ((Action)(() => fileStore.FindMatches("Chargenfreigabe", cancellationToken: cts.Token))).IsThrowing<OperationCanceledException>();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void EditTerm_missing_term_returns_error()
    {
        var path = CreateTempPath();
        try
        {
            var fileStore = new LexiconFileStore(path);

            var edit = fileStore.EditTerm("does not exist", "x");

            edit.TotalEntries.IsNull();
            edit.Error.IsNotNull();
            edit.Error!.Message.Is("term not found");
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
