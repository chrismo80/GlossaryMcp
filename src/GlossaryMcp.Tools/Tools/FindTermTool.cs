using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record FindTermMatch(
    GlossaryTerm Entry,
    int Score);

public sealed record FindTermResponse(
    IReadOnlyList<FindTermMatch> Results,
    ToolError? Error = null)
{
    public static FindTermResponse AsError(string message)
        => new([], new ToolError(message));
}

[McpServerToolType]
public sealed class FindTermTool
{
    private readonly GlossaryStore _glossaryStore;

    public FindTermTool(IServiceProvider services)
        : this(services.GetRequiredService<GlossaryStore>())
    {
    }

    internal FindTermTool(GlossaryStore glossaryStore)
        => _glossaryStore = glossaryStore;

    [McpServerTool(Name = "find", Title = "Find", ReadOnly = true, Idempotent = true)]
    [Description("Find glossary entries by matching the full query string and its whitespace-split words against terms and descriptions.")]
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

        var matches = _glossaryStore.Find(query, maxResults, cancellationToken);
        var results = matches
            .Select(m => new FindTermMatch(GlossaryTerm.From(m.Entry), m.Score))
            .ToArray();

        return Task.FromResult(new FindTermResponse(results));
    }
}
