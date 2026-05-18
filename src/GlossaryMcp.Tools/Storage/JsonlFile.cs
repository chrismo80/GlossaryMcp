using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GlossaryMcp.Tools.Storage;

internal sealed class JsonlFile<T>
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly Lock _sync = new();
    private readonly JsonSerializerOptions _json;

    public JsonlFile(string path, JsonSerializerOptions? json = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File path must not be empty.", nameof(path));

        Path = path;
        _json = json ?? new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public string Path { get; }

    public IReadOnlyList<T> ReadAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!File.Exists(Path))
                return [];

            var result = new List<T>();
            var lineNumber = 0;

            foreach (var line in File.ReadLines(Path, Utf8NoBom))
            {
                cancellationToken.ThrowIfCancellationRequested();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var item = JsonSerializer.Deserialize<T>(line, _json)
                        ?? throw new InvalidDataException($"Invalid JSON at line {lineNumber}.");

                    result.Add(item);
                }
                catch (InvalidDataException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Invalid JSON at line {lineNumber}.", ex);
                }
            }

            return result;
        }
    }

    public void Append(T item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            EnsureDirectory();
            cancellationToken.ThrowIfCancellationRequested();

            var json = JsonSerializer.Serialize(item, _json);
            File.AppendAllText(Path, json + Environment.NewLine, Utf8NoBom);
        }
    }

    public void RewriteAll(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            EnsureDirectory();

            var tempPath = $"{Path}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, Utf8NoBom))
                {
                    foreach (var item in items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        writer.WriteLine(JsonSerializer.Serialize(item, _json));
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(tempPath, Path, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }

    private void EnsureDirectory()
    {
        var directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}
