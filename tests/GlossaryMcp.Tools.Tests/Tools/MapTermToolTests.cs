using GlossaryMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class MapTermToolTests
{
    [Fact]
    public async Task Returns_all_terms()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Production Batch", "Batch.");
        glossary.Store.Add("Batch Release", "Approval of a Production Batch.");

        var tool = new MapTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None);

        response.Error.IsNull();
        response.Terms.Select(term => term.Term).Is(["Production Batch", "Batch Release"]);
        response.Terms.Single(term => term.Term == "Production Batch")
            .MentionedIn.Is(["Batch Release"]);
    }

    [Fact]
    public async Task Term_parameter_returns_requested_term()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Production Batch", "Batch.");
        glossary.Store.Add("Batch Release", "Approval of a Production Batch.");

        var tool = new MapTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "production batch");

        response.Error.IsNull();
        response.Terms.Select(term => term.Term).Is(["Production Batch"]);
        response.Terms.Single().MentionedIn.Is(["Batch Release"]);
    }

    [Fact]
    public async Task Invalid_term_returns_error()
    {
        using var glossary = TestGlossary.Create();
        var tool = new MapTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, " ");

        response.Terms.IsEmpty();
        response.Error.IsNotNull();
        response.Error!.Message.Is("invalid term");
    }

    [Fact]
    public async Task Missing_term_returns_error()
    {
        using var glossary = TestGlossary.Create();
        var tool = new MapTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "does not exist");

        response.Terms.IsEmpty();
        response.Error.IsNotNull();
        response.Error!.Message.Is("term not found");
    }

    [Fact]
    public async Task Canceled_token_throws()
    {
        using var glossary = TestGlossary.Create();
        var tool = new MapTermTool(glossary.Store);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> action = async () => await tool.Execute(cts.Token);
        action.IsThrowing<OperationCanceledException>();
    }
}
