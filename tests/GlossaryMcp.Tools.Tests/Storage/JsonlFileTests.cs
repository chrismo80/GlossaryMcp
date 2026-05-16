using System.Text;
using System.Text.Json;
using GlossaryMcp.Tools.Glossary;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Storage;

public sealed class JsonlFileTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void ReadAll_missing_file_returns_empty_list()
    {
        using var glossary = TestGlossary.Create();

        glossary.File.ReadAll().IsEmpty();
    }

    [Fact]
    public void ReadAll_ignores_empty_lines()
    {
        using var glossary = TestGlossary.Create();
        File.WriteAllText(glossary.Path, "\n\n{\"term\":\"A\",\"description\":\"B\"}\n\n", Utf8NoBom);

        var entries = glossary.File.ReadAll();

        entries.Count.Is(1);
        entries[0].Term.Is("A");
    }

    [Fact]
    public void ReadAll_invalid_json_throws()
    {
        using var glossary = TestGlossary.Create();
        File.WriteAllText(glossary.Path, "{not json}\n", Utf8NoBom);

        var ex = ((Action)(() => glossary.File.ReadAll())).IsThrowing<InvalidDataException>();

        ex.IsNotNull();
        ex!.Message.Is("Invalid JSON at line 1.");
    }

    [Fact]
    public void Append_writes_one_json_line()
    {
        using var glossary = TestGlossary.Create();

        glossary.File.Append(new GlossaryEntry("Chargenfreigabe", "desc"));

        var lines = File.ReadAllLines(glossary.Path, Utf8NoBom);
        lines.Count().Is(1);

        var entry = JsonSerializer.Deserialize<GlossaryEntry>(lines[0], new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        entry.IsNotNull();
        entry!.Term.Is("Chargenfreigabe");
    }

    [Fact]
    public void RewriteAll_replaces_file_without_fixed_tmp_file()
    {
        using var glossary = TestGlossary.Create();
        glossary.File.Append(new GlossaryEntry("A", "old"));

        glossary.File.RewriteAll([new GlossaryEntry("A", "new")]);

        var lines = File.ReadAllLines(glossary.Path, Utf8NoBom);
        lines.Count().Is(1);
        lines[0].Contains("new").IsTrue();
        File.Exists(glossary.Path + ".tmp").IsFalse();
    }
}
