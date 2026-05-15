using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record DeleteTermResponse(
    int? TotalEntries = null,
    GlossaryTerm? DeletedEntry = null,
    ToolError? Error = null);

[McpServerToolType]
public sealed class DeleteTermTool
{
    private readonly GlossaryStore _glossaryStore;

    public DeleteTermTool(IServiceProvider services)
        : this(services.GetRequiredService<GlossaryStore>())
    {
    }

    internal DeleteTermTool(GlossaryStore glossaryStore)
        => _glossaryStore = glossaryStore;

    [McpServerTool(Name = "delete", Title = "Delete", ReadOnly = false, Idempotent = false)]
    [Description("Delete one existing domain term from the glossary.")]
    public Task<DeleteTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("The term to delete.")] string term)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new DeleteTermResponse(Error: new ToolError("invalid term")));

        cancellationToken.ThrowIfCancellationRequested();

        var result = _glossaryStore.Delete(term, cancellationToken);

        return Task.FromResult(new DeleteTermResponse(
            TotalEntries: result.TotalEntries,
            DeletedEntry: result.DeletedEntry is null ? null : GlossaryTerm.From(result.DeletedEntry),
            Error: result.Error is null ? null : ToolError.From(result.Error)));
    }
}
