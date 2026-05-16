using Is.Assertions;
using GlossaryMcp.Tools.Tools;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class FindTermToolTests
{
    [Fact]
    public async Task Invalid_query_returns_error()
    {
        using var glossary = TestGlossary.Create();
        var tool = new FindTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "  ");

        response.Error.IsNotNull();
        response.Error!.Message.Is("invalid query");
        response.Results.IsEmpty();
    }

    [Fact]
    public async Task MaxResults_leq_zero_returns_empty_list()
    {
        using var glossary = TestGlossary.Create();
        var tool = new FindTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "x", maxResults: 0);

        response.Error.IsNull();
        response.Results.IsEmpty();
    }

    [Fact]
    public async Task Returns_matches()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "Fachliche Freigabe einer Charge.");
        glossary.Store.Add("Charge", "Losgröße.");

        var tool = new FindTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe");

        response.Error.IsNull();
        response.Results.Count.Is(1);
        response.Results[0].Entry.Term.Is("Chargenfreigabe");
    }

    [Fact]
    public async Task Canceled_token_throws()
    {
        using var glossary = TestGlossary.Create();
        var tool = new FindTermTool(glossary.Store);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> action = async () => await tool.Execute(cts.Token, "x");
        action.IsThrowing<OperationCanceledException>();
    }
}
