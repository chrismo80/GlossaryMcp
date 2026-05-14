using Is.Assertions;
using GlossaryMcp.Tools.Lexicon;
using GlossaryMcp.Tools.Tools;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class EditTermToolTests
{
    [Fact]
    public async Task Missing_term_returns_error()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new EditTermTool(new LexiconFileStore(path));

            var response = await tool.Execute(CancellationToken.None, "does not exist", "x");

            response.Error.IsNotNull();
            response.Error!.Message.Is("term not found");
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
            var store = new LexiconFileStore(path);
            _ = store.AddTerm("Chargenfreigabe", "old");

            var tool = new EditTermTool(store);

            var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "new");

            response.TotalEntries.Is(1);
            response.Error.IsNull();
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
