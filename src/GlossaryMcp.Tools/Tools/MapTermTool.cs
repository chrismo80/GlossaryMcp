using System.ComponentModel;
using GlossaryMcp.Tools.Glossary;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Tools;

public sealed record MappedGlossaryTerm(
    string Term,
    IReadOnlyList<string> MentionedIn);

public sealed record MapTermResponse(
    IReadOnlyList<MappedGlossaryTerm> Terms,
    ToolError? Error = null)
{
    public static MapTermResponse AsError(string message)
        => new([], new ToolError(message));
}

[McpServerToolType]
public sealed class MapTermTool
{
    private readonly GlossaryStore _glossaryStore;

    public MapTermTool(IServiceProvider services)
        : this(services.GetRequiredService<GlossaryStore>())
    {
    }

    internal MapTermTool(GlossaryStore glossaryStore)
        => _glossaryStore = glossaryStore;

    [McpServerTool(Name = "map", Title = "Map", ReadOnly = true, Idempotent = true)]
    [Description("List glossary terms and the other terms whose descriptions mention them.")]
    public Task<MapTermResponse> Execute(
        CancellationToken cancellationToken,
        [Description("Optional glossary term to map. Omit to map every term.")] string? term = null)
    {
        if (term is not null && string.IsNullOrWhiteSpace(term))
            return Task.FromResult(MapTermResponse.AsError("invalid term"));

        cancellationToken.ThrowIfCancellationRequested();

        if (term is not null && !_glossaryStore.TryGetEntry(term, out _, cancellationToken))
            return Task.FromResult(MapTermResponse.AsError("term not found"));

        var terms = _glossaryStore
            .Map(term, cancellationToken)
            .Select(mapped => new MappedGlossaryTerm(mapped.Term, mapped.MentionedIn))
            .ToArray();

        return Task.FromResult(new MapTermResponse(terms));
    }
}
