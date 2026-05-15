namespace GlossaryMcp.Tools;

internal sealed record ErrorInfo(
    string Message,
    IReadOnlyDictionary<string, string>? Details = null);
