using System.Text;
using System.Text.Json;
using GlossaryMcp.Tools;

namespace GlossaryMcp.Tools.Lexicon;

public sealed class LexiconFileStore
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _sync = new();
    private LexiconStore _store;

    public LexiconFileStore(string filePath, LexiconStore? store = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must not be empty.", nameof(filePath));

        FilePath = filePath;
        _store = store ?? new LexiconStore();
    }

    public string FilePath { get; }
    public int EntryCount
    {
        get
        {
            lock (_sync)
                return _store.Entries.Count;
        }
    }

    public static LexiconFileStore Load(string filePath)
    {
        var store = new LexiconStore();

        if (!File.Exists(filePath))
            return new LexiconFileStore(filePath, store);

        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(filePath, Utf8NoBom))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            LexiconEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<LexiconEntry>(rawLine, JsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Invalid JSON at line {lineNumber}.", ex);
            }

            if (entry is null)
                throw new InvalidDataException($"Invalid JSON at line {lineNumber}.");

            if (string.IsNullOrWhiteSpace(entry.Term))
                throw new InvalidDataException($"Empty term at line {lineNumber}.");

            if (string.IsNullOrWhiteSpace(entry.Description))
                throw new InvalidDataException($"Empty description at line {lineNumber}.");

            var normalizedKey = entry.Term.NormalizeGlossary();
            if (store.ByNormalizedTerm.ContainsKey(normalizedKey))
                throw new InvalidDataException($"Duplicate term at line {lineNumber}.");

            store.Entries.Add(entry);
            store.ByNormalizedTerm[normalizedKey] = entry;
        }

        return new LexiconFileStore(filePath, store);
    }

    public IReadOnlyList<LexiconEntry> GetEntriesSnapshot(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
            return _store.Entries.ToArray();
    }

    public bool TryGetEntry(string term, out LexiconEntry? entry, CancellationToken cancellationToken = default)
    {
        if (term is null)
            throw new ArgumentNullException(nameof(term));

        cancellationToken.ThrowIfCancellationRequested();

        var key = term.NormalizeGlossary();

        lock (_sync)
            return _store.ByNormalizedTerm.TryGetValue(key, out entry);
    }

    public IReadOnlyList<LexiconMatch> FindMatches(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        cancellationToken.ThrowIfCancellationRequested();

        LexiconEntry[] snapshot;
        lock (_sync)
            snapshot = _store.Entries.ToArray();

        return LexiconSearch.FindMatches(snapshot, query, maxResults, cancellationToken);
    }

    public AddTermResult AddTerm(string term, string description, CancellationToken cancellationToken = default)
    {
        if (term is null)
            throw new ArgumentNullException(nameof(term));
        if (description is null)
            throw new ArgumentNullException(nameof(description));

        term = term.Trim();
        description = description.Trim();

        if (term.Length == 0)
            return AddTermResult.AsError("invalid term");
        if (description.Length == 0)
            return AddTermResult.AsError("invalid description");

        var key = term.NormalizeGlossary();
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_store.ByNormalizedTerm.TryGetValue(key, out var existing))
                return AddTermResult.AsExistsAlready(existing);

            EnsureDirectoryExists();
            cancellationToken.ThrowIfCancellationRequested();

            var entry = new LexiconEntry(term, description);
            var json = JsonSerializer.Serialize(entry, JsonOptions);
            File.AppendAllText(FilePath, json + "\n", Utf8NoBom);

            _store.Entries.Add(entry);
            _store.ByNormalizedTerm[key] = entry;

            return AddTermResult.AsSuccess(_store.Entries.Count);
        }
    }

    public EditTermResult EditTerm(string term, string description, CancellationToken cancellationToken = default)
    {
        if (term is null)
            throw new ArgumentNullException(nameof(term));
        if (description is null)
            throw new ArgumentNullException(nameof(description));

        term = term.Trim();
        description = description.Trim();

        if (term.Length == 0)
            return EditTermResult.AsError("invalid term");
        if (description.Length == 0)
            return EditTermResult.AsError("invalid description");

        var key = term.NormalizeGlossary();
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_store.ByNormalizedTerm.TryGetValue(key, out var existing))
                return EditTermResult.AsError("term not found");

            var updated = new LexiconEntry(existing.Term, description);
            var updatedEntries = _store.Entries.ToArray();

            for (var i = updatedEntries.Length - 1; i >= 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ReferenceEquals(updatedEntries[i], existing))
                    continue;

                updatedEntries[i] = updated;
                break;
            }

            RewriteFile(updatedEntries, cancellationToken);

            var updatedStore = new LexiconStore();
            foreach (var entry in updatedEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                updatedStore.Entries.Add(entry);
                updatedStore.ByNormalizedTerm[entry.Term.NormalizeGlossary()] = entry;
            }

            _store = updatedStore;

            return EditTermResult.AsSuccess(_store.Entries.Count);
        }
    }

    private void RewriteFile(IReadOnlyList<LexiconEntry> entries, CancellationToken cancellationToken)
    {
        EnsureDirectoryExists();

        var tmpPath = $"{FilePath}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var fs = new FileStream(tmpPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs, Utf8NoBom))
            {
                foreach (var entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var json = JsonSerializer.Serialize(entry, JsonOptions);
                    sw.WriteLine(json);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(FilePath))
            {
                File.Replace(tmpPath, FilePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tmpPath, FilePath);
            }
        }
        finally
        {
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
        }
    }

    private void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }
}

public sealed record AddTermResult(
    int? TotalEntries,
    LexiconEntry? ExistingEntry,
    ErrorInfo? Error)
{
    public static AddTermResult AsSuccess(int totalEntries) => new(totalEntries, null, null);
    public static AddTermResult AsExistsAlready(LexiconEntry existingEntry) => new(null, existingEntry, new ErrorInfo("exists already"));
    public static AddTermResult AsError(string message) => new(null, null, new ErrorInfo(message));
}

public sealed record EditTermResult(
    int? TotalEntries,
    ErrorInfo? Error)
{
    public static EditTermResult AsSuccess(int totalEntries) => new(totalEntries, null);
    public static EditTermResult AsError(string message) => new(null, new ErrorInfo(message));
}
