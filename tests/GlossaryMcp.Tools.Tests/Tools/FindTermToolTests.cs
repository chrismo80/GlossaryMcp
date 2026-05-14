using Is.Assertions;
using GlossaryMcp.Tools.Lexicon;
using GlossaryMcp.Tools.Tools;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class FindTermToolTests
{
    [Fact]
    public async Task Invalid_query_returns_error()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new FindTermTool(new LexiconFileStore(path));

            var response = await tool.Execute(CancellationToken.None, "  ");

            response.Error.IsNotNull();
            response.Error!.Message.Is("invalid query");
            response.Results.IsEmpty();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task MaxResults_leq_zero_returns_empty_list()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new FindTermTool(new LexiconFileStore(path));

            var response = await tool.Execute(CancellationToken.None, "x", maxResults: 0);

            response.Error.IsNull();
            response.Results.IsEmpty();
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task Returns_matches()
    {
        var path = CreateTempPath();
        try
        {
            var store = new LexiconFileStore(path);
            _ = store.AddTerm("Chargenfreigabe", "Fachliche Freigabe einer Charge.");
            _ = store.AddTerm("Charge", "Losgröße.");

            var tool = new FindTermTool(store);

            var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe");

            response.Error.IsNull();
            response.Results.Count.Is(1);
            response.Results[0].Entry.Term.Is("Chargenfreigabe");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task Canceled_token_throws()
    {
        var path = CreateTempPath();
        try
        {
            var tool = new FindTermTool(new LexiconFileStore(path));

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> action = async () => await tool.Execute(cts.Token, "x");
            action.IsThrowing<OperationCanceledException>();
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
