using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record AddTermResponse(
    int? TotalEntries = null,
    GlossaryTerm? ExistingEntry = null,
    ToolError? Error = null);

[McpServerToolType]
public sealed class AddTermTool
{
    private readonly GlossaryStore _glossaryStore;

    public AddTermTool(IServiceProvider services)
        : this(services.GetRequiredService<GlossaryStore>())
    {
    }

    internal AddTermTool(GlossaryStore glossaryStore)
        => _glossaryStore = glossaryStore;

    [McpServerTool(Name = "add", Title = "Add", ReadOnly = false, Idempotent = false)]
    [Description("Append one domain term with its description to the glossary.")]
    public Task<AddTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("The domain term to add.")] string term,
        [Description("Free-text description/meaning for the term.")] string description)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new AddTermResponse(Error: new ToolError("invalid term")));

        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(new AddTermResponse(Error: new ToolError("invalid description")));

        cancellationToken.ThrowIfCancellationRequested();

        var result = _glossaryStore.Add(term, description, cancellationToken);

        return Task.FromResult(new AddTermResponse(
            TotalEntries: result.TotalEntries,
            ExistingEntry: result.ExistingEntry is null ? null : GlossaryTerm.From(result.ExistingEntry),
            Error: result.Error is null ? null : ToolError.From(result.Error)));
    }
}
