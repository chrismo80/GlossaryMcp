using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record EditTermResponse(
    int? TotalEntries = null,
    ToolError? Error = null);

[McpServerToolType]
public sealed class EditTermTool
{
    private readonly GlossaryStore _glossaryStore;

    public EditTermTool(IServiceProvider services)
        : this(services.GetRequiredService<GlossaryStore>())
    {
    }

    internal EditTermTool(GlossaryStore glossaryStore)
        => _glossaryStore = glossaryStore;

    [McpServerTool(Name = "edit", Title = "Edit", ReadOnly = false, Idempotent = false)]
    [Description("Replace the full description text for an existing term.")]
    public Task<EditTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("The term to edit.")] string term,
        [Description("The full new description text.")] string description)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new EditTermResponse(Error: new ToolError("invalid term")));

        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(new EditTermResponse(Error: new ToolError("invalid description")));

        cancellationToken.ThrowIfCancellationRequested();

        var result = _glossaryStore.Edit(term, description, cancellationToken);
        return Task.FromResult(new EditTermResponse(
            TotalEntries: result.TotalEntries,
            Error: result.Error is null ? null : ToolError.From(result.Error)));
    }
}
