using GlossaryMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class DeleteTermToolTests
{
    [Fact]
    public async Task Validates_input()
    {
        using var glossary = TestGlossary.Create();
        var tool = new DeleteTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, " ");

        response.TotalEntries.IsNull();
        response.DeletedEntry.IsNull();
        response.Error.IsNotNull();
        response.Error!.Message.Is("invalid term");
    }

    [Fact]
    public async Task Missing_term_returns_error()
    {
        using var glossary = TestGlossary.Create();
        var tool = new DeleteTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "does not exist");

        response.TotalEntries.IsNull();
        response.DeletedEntry.IsNull();
        response.Error.IsNotNull();
        response.Error!.Message.Is("term not found");
    }

    [Fact]
    public async Task Success_returns_deletedEntry_and_totalEntries()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "desc");

        var tool = new DeleteTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe");

        response.Error.IsNull();
        response.TotalEntries.Is(0);
        response.DeletedEntry.IsNotNull();
        response.DeletedEntry!.Term.Is("Chargenfreigabe");
        response.DeletedEntry.Description.Is("desc");
    }
}
