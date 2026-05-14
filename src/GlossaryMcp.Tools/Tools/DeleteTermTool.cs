using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record DeleteTermResponse(
    int? TotalEntries = null,
    GlossaryEntry? DeletedEntry = null,
    ErrorInfo? Error = null);

[McpServerToolType]
public sealed class DeleteTermTool(GlossaryStore glossaryStore)
{
    [McpServerTool(Name = "delete", Title = "Delete", ReadOnly = false, Idempotent = false)]
    [Description("Delete one existing domain term from the glossary.")]
    public Task<DeleteTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("The term to delete.")] string term)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new DeleteTermResponse(Error: new ErrorInfo("invalid term")));

        cancellationToken.ThrowIfCancellationRequested();

        var result = glossaryStore.Delete(term, cancellationToken);

        return Task.FromResult(new DeleteTermResponse(
            TotalEntries: result.TotalEntries,
            DeletedEntry: result.DeletedEntry,
            Error: result.Error));
    }
}
