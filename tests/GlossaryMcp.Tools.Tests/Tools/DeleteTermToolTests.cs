using GlossaryMcp.Tools.Glossary;
using GlossaryMcp.Tools.Storage;
using GlossaryMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class DeleteTermToolTests
{
    [Fact]
    public async Task Validates_input()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new DeleteTermTool(new GlossaryStore(new JsonlFile<GlossaryEntry>(path)));

            var response = await tool.Execute(CancellationToken.None, " ");

            response.TotalEntries.IsNull();
            response.DeletedEntry.IsNull();
            response.Error.IsNotNull();
            response.Error!.Message.Is("invalid term");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task Missing_term_returns_error()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new DeleteTermTool(new GlossaryStore(new JsonlFile<GlossaryEntry>(path)));

            var response = await tool.Execute(CancellationToken.None, "does not exist");

            response.TotalEntries.IsNull();
            response.DeletedEntry.IsNull();
            response.Error.IsNotNull();
            response.Error!.Message.Is("term not found");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task Success_returns_deletedEntry_and_totalEntries()
    {
        var path = CreateTempPath();
        try
        {
            var store = new GlossaryStore(new JsonlFile<GlossaryEntry>(path));
            _ = store.Add("Chargenfreigabe", "desc");

            var tool = new DeleteTermTool(store);

            var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe");

            response.Error.IsNull();
            response.TotalEntries.Is(0);
            response.DeletedEntry.IsNotNull();
            response.DeletedEntry!.Term.Is("Chargenfreigabe");
            response.DeletedEntry.Description.Is("desc");
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
