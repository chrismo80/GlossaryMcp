using Is.Assertions;
using System.Text;
using GlossaryMcp.Tools.Lexicon;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Lexicon;

public sealed class LexiconFileStoreTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void Load_missing_file_returns_empty_store()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "glossary.jsonl");

        var fileStore = LexiconFileStore.Load(path);

        fileStore.EntryCount.Is(0);
        fileStore.GetEntriesSnapshot().IsEmpty();
    }

    [Fact]
    public void Load_ignores_empty_lines()
    {
        var path = CreateTempPath();
        try
        {
            File.WriteAllText(path, "\n\n{\"term\":\"A\",\"description\":\"B\"}\n\n", Utf8NoBom);

            var fileStore = LexiconFileStore.Load(path);

            fileStore.GetEntriesSnapshot().Count.Is(1);
            fileStore.EntryCount.Is(1);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Theory]
    [InlineData("{not json}\n", "Invalid JSON at line 1.")]
    [InlineData("{\"term\":\"\",\"description\":\"x\"}\n", "Empty term at line 1.")]
    [InlineData("{\"term\":\"x\",\"description\":\"\"}\n", "Empty description at line 1.")]
    public void Load_invalid_content_throws(string content, string expectedMessage)
    {
        var path = CreateTempPath();
        try
        {
            File.WriteAllText(path, content, Utf8NoBom);

            var ex = ((Action)(() => LexiconFileStore.Load(path))).IsThrowing<InvalidDataException>();
            ex.IsNotNull();
            ex!.Message.Is(expectedMessage);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Load_duplicate_terms_throws()
    {
        var path = CreateTempPath();
        try
        {
            File.WriteAllText(path,
                "{\"term\":\"Chargenfreigabe\",\"description\":\"first\"}\n" +
                "{\"term\":\"Chargenfreigabe\",\"description\":\"second\"}\n",
                Utf8NoBom);

            var ex = ((Action)(() => LexiconFileStore.Load(path))).IsThrowing<InvalidDataException>();
            ex.IsNotNull();
            ex!.Message.Contains("Duplicate term").IsTrue();
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
