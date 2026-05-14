using System.ComponentModel;
using ModelContextProtocol.Server;
using GlossaryMcp.Tools.Lexicon;

namespace GlossaryMcp.Tools.Tools;

public sealed record EditTermResponse(
    int? TotalEntries = null,
    ErrorInfo? Error = null);

[McpServerToolType]
public sealed class EditTermTool(LexiconFileStore fileStore) : Tool
{
    [McpServerTool(Name = "edit", Title = "Edit", ReadOnly = false, Idempotent = false)]
    [Description("Replace the full description text for an existing term and rewrite the lexicon file.")]
    public Task<EditTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("The term to edit (exact match after normalization).")] string term,
        [Description("The full new description text.")] string description)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new EditTermResponse(Error: new ErrorInfo("invalid term")));

        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(new EditTermResponse(Error: new ErrorInfo("invalid description")));

        cancellationToken.ThrowIfCancellationRequested();

        var result = fileStore.EditTerm(term, description, cancellationToken);
        return Task.FromResult(new EditTermResponse(
            TotalEntries: result.TotalEntries,
            Error: result.Error));
    }
}
