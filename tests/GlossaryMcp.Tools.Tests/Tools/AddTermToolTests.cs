using Is.Assertions;
using System.Text;
using GlossaryMcp.Tools.Glossary;
using GlossaryMcp.Tools.Storage;
using GlossaryMcp.Tools.Tools;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class AddTermToolTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public async Task Validates_input()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new AddTermTool(new GlossaryStore(new JsonlFile<GlossaryEntry>(path)));

            var response = await tool.Execute(CancellationToken.None, " ", "x");

            response.Error.IsNotNull();
            response.Error!.Message.Is("invalid term");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task Success_returns_totalEntries_only()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new AddTermTool(new GlossaryStore(new JsonlFile<GlossaryEntry>(path)));

            var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "desc");

            response.TotalEntries.Is(1);
            response.Error.IsNull();
            response.ExistingEntry.IsNull();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task Existing_term_returns_existingEntry_and_error()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new AddTermTool(new GlossaryStore(new JsonlFile<GlossaryEntry>(path)));

            _ = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "first");
            var second = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "second");

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
