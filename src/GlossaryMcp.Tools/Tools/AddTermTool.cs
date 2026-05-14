using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record AddTermResponse(
    int? TotalEntries = null,
    GlossaryEntry? ExistingEntry = null,
    ErrorInfo? Error = null);

[McpServerToolType]
public sealed class AddTermTool(GlossaryStore glossaryStore)
{
    [McpServerTool(Name = "add", Title = "Add", ReadOnly = false, Idempotent = false)]
    [Description("Append one domain term with its description to the glossary.")]
    public Task<AddTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("The domain term to add.")] string term,
        [Description("Free-text description/meaning for the term.")] string description)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new AddTermResponse(Error: new ErrorInfo("invalid term")));

        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(new AddTermResponse(Error: new ErrorInfo("invalid description")));

        cancellationToken.ThrowIfCancellationRequested();

        var result = glossaryStore.Add(term, description, cancellationToken);

        return Task.FromResult(new AddTermResponse(
            TotalEntries: result.TotalEntries,
            ExistingEntry: result.ExistingEntry,
            Error: result.Error));
    }
}