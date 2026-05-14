using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record FindTermMatch(
    GlossaryEntry Entry,
    int Score);

public sealed record FindTermResponse(
    IReadOnlyList<FindTermMatch> Results,
    ErrorInfo? Error = null)
{
    public static FindTermResponse AsError(string message)
        => new([], new ErrorInfo(message));
}

[McpServerToolType]
public sealed class FindTermTool(GlossaryStore glossaryStore)
{
    [McpServerTool(Name = "find", Title = "Find", ReadOnly = true, Idempotent = true)]
    [Description("Find lexicon entries by matching the full query string and its whitespace-split words against terms and descriptions.")]
    public Task<FindTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("Query string to search for.")] string query,
        [Description("Maximum number of results to return (default 10).")] int maxResults = 10)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(FindTermResponse.AsError("invalid query"));

        if (maxResults <= 0)
            return Task.FromResult(new FindTermResponse([]));

        cancellationToken.ThrowIfCancellationRequested();

        var matches = glossaryStore.Find(query, maxResults, cancellationToken);
        var results = matches
            .Select(m => new FindTermMatch(m.Entry, m.Score))
            .ToArray();

        return Task.FromResult(new FindTermResponse(results));
    }
}