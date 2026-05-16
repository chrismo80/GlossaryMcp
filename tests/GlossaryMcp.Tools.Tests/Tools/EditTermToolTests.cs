using Is.Assertions;
using GlossaryMcp.Tools.Tools;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class EditTermToolTests
{
    [Fact]
    public async Task Missing_term_returns_error()
    {
        using var glossary = TestGlossary.Create();
        var tool = new EditTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "does not exist", "x");

        response.Error.IsNotNull();
        response.Error!.Message.Is("term not found");
    }

    [Fact]
    public async Task Success_returns_totalEntries_only()
    {
        using var glossary = TestGlossary.Create();
        glossary.Store.Add("Chargenfreigabe", "old");

        var tool = new EditTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "new");

        response.TotalEntries.Is(1);
        response.Error.IsNull();
    }
}
