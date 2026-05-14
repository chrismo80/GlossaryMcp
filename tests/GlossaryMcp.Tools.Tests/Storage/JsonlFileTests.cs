using System.Text;
using System.Text.Json;
using GlossaryMcp.Tools.Glossary;
using GlossaryMcp.Tools.Storage;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Storage;

public sealed class JsonlFileTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void ReadAll_missing_file_returns_empty_list()
    {
        var path = CreateTempPath();

        try
        {
            var file = new JsonlFile<GlossaryEntry>(path);

            file.ReadAll().IsEmpty();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void ReadAll_ignores_empty_lines()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "\n\n{\"term\":\"A\",\"description\":\"B\"}\n\n", Utf8NoBom);

            var file = new JsonlFile<GlossaryEntry>(path);
            var entries = file.ReadAll();

            entries.Count.Is(1);
            entries[0].Term.Is("A");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void ReadAll_invalid_json_throws()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "{not json}\n", Utf8NoBom);

            var file = new JsonlFile<GlossaryEntry>(path);

            var ex = ((Action)(() => file.ReadAll())).IsThrowing<InvalidDataException>();
            ex.IsNotNull();
            ex!.Message.Is("Invalid JSON at line 1.");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void Append_writes_one_json_line()
    {
        var path = CreateTempPath();

        try
        {
            var file = new JsonlFile<GlossaryEntry>(path);

            file.Append(new GlossaryEntry("Chargenfreigabe", "desc"));

            var lines = File.ReadAllLines(path, Utf8NoBom);
            lines.Count().Is(1);

            var entry = JsonSerializer.Deserialize<GlossaryEntry>(lines[0], new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            entry.IsNotNull();
            entry!.Term.Is("Chargenfreigabe");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public void RewriteAll_replaces_file_without_fixed_tmp_file()
    {
        var path = CreateTempPath();

        try
        {
            var file = new JsonlFile<GlossaryEntry>(path);
            file.Append(new GlossaryEntry("A", "old"));

            file.RewriteAll([new GlossaryEntry("A", "new")]);

            var lines = File.ReadAllLines(path, Utf8NoBom);
            lines.Count().Is(1);
            lines[0].Contains("new").IsTrue();
            File.Exists(path + ".tmp").IsFalse();
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
