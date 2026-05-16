using Is.Assertions;
using System.Text;
using GlossaryMcp.Tools.Tools;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Tools;

public sealed class AddTermToolTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public async Task Validates_input()
    {
        using var glossary = TestGlossary.Create();
        var tool = new AddTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, " ", "x");

        response.Error.IsNotNull();
        response.Error!.Message.Is("invalid term");
    }

    [Fact]
    public async Task Success_returns_totalEntries_only()
    {
        using var glossary = TestGlossary.Create();
        var tool = new AddTermTool(glossary.Store);

        var response = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "desc");

        response.TotalEntries.Is(1);
        response.Error.IsNull();
        response.ExistingEntry.IsNull();
    }

    [Fact]
    public async Task Existing_term_returns_existingEntry_and_error()
    {
        using var glossary = TestGlossary.Create();
        var tool = new AddTermTool(glossary.Store);

        await tool.Execute(CancellationToken.None, "Chargenfreigabe", "first");
        var second = await tool.Execute(CancellationToken.None, "Chargenfreigabe", "second");

        second.TotalEntries.IsNull();
        second.Error.IsNotNull();
        second.Error!.Message.Is("exists already");
        second.ExistingEntry.IsNotNull();
        second.ExistingEntry!.Description.Is("first");

        var lines = File.ReadAllLines(glossary.Path, Utf8NoBom);
        lines.Count().Is(1);
    }
}
