using GlossaryMcp.Tools.Glossary;
using GlossaryMcp.Tools.Storage;

namespace GlossaryMcp.Tools.Tests;

internal sealed class TestGlossary : IDisposable
{
    private readonly string _directory;

    private TestGlossary(string directory)
    {
        _directory = directory;
        Path = System.IO.Path.Combine(directory, "glossary.jsonl");
        File = new JsonlFile<GlossaryEntry>(Path);
    }

    public string Path { get; }

    public JsonlFile<GlossaryEntry> File { get; }

    public GlossaryStore Store => field ??= new GlossaryStore(File);

    public static TestGlossary Create()
    {
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "GlossaryMcpTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return new TestGlossary(directory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}