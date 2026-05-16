using GlossaryMcp.Tools.Storage;

namespace GlossaryMcp.Tools.Glossary;

internal sealed class GlossaryStore
{
    private readonly Lock _sync = new();
    private readonly JsonlFile<GlossaryEntry> _file;

    private List<SearchableGlossaryEntry> _entries = [];
    private Dictionary<string, SearchableGlossaryEntry> _byNormalizedTerm = new(StringComparer.Ordinal);

    public GlossaryStore(JsonlFile<GlossaryEntry> file)
    {
        _file = file ?? throw new ArgumentNullException(nameof(file));
        ReplaceState(_file.ReadAll());
    }

    public static GlossaryStore Load(string filePath)
        => new(new JsonlFile<GlossaryEntry>(filePath));

    public int EntryCount
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    public IReadOnlyList<GlossaryEntry> GetEntriesSnapshot(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
            return _entries.Select(entry => entry.Entry).ToArray();
    }

    public bool TryGetEntry(string term, out GlossaryEntry? entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(term);

        cancellationToken.ThrowIfCancellationRequested();

        var key = term.NormalizeGlossary();

        lock (_sync)
        {
            if (_byNormalizedTerm.TryGetValue(key, out var searchableEntry))
            {
                entry = searchableEntry.Entry;
                return true;
            }

            entry = null;
            return false;
        }
    }

    public AddTermResult Add(string term, string description, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(description);

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
            if (_byNormalizedTerm.TryGetValue(key, out var existing))
                return AddTermResult.AsExistsAlready(existing.Entry);

            var entry = new GlossaryEntry(term, description);
            var searchableEntry = SearchableGlossaryEntry.From(entry);

            _file.Append(entry, cancellationToken);
            _entries.Add(searchableEntry);
            _byNormalizedTerm[key] = searchableEntry;

            return AddTermResult.AsSuccess(_entries.Count);
        }
    }

    public EditTermResult Edit(string term, string description, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(description);

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
            if (!_byNormalizedTerm.TryGetValue(key, out var existing))
                return EditTermResult.AsError("term not found");

            var updatedEntries = _entries.ToArray();
            for (var index = updatedEntries.Length - 1; index >= 0; index--)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ReferenceEquals(updatedEntries[index], existing))
                    continue;

                updatedEntries[index] = SearchableGlossaryEntry.From(new GlossaryEntry(existing.Entry.Term, description));
                _file.RewriteAll(updatedEntries.Select(entry => entry.Entry).ToArray(), cancellationToken);
                ReplaceState(updatedEntries);
                return EditTermResult.AsSuccess(_entries.Count);
            }

            return EditTermResult.AsError("term not found");
        }
    }

    public DeleteTermResult Delete(string term, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(term);

        term = term.Trim();

        if (term.Length == 0)
            return DeleteTermResult.AsError("invalid term");

        var key = term.NormalizeGlossary();
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_byNormalizedTerm.TryGetValue(key, out var existing))
                return DeleteTermResult.AsError("term not found");

            var updatedEntries = _entries.ToList();
            var existingIndex = updatedEntries.FindIndex(entry => ReferenceEquals(entry, existing));
            if (existingIndex < 0)
                return DeleteTermResult.AsError("term not found");

            updatedEntries.RemoveAt(existingIndex);

            _file.RewriteAll(updatedEntries.Select(entry => entry.Entry).ToArray(), cancellationToken);
            ReplaceState(updatedEntries);

            return DeleteTermResult.AsSuccess(_entries.Count, existing.Entry);
        }
    }

    public IReadOnlyList<GlossaryMatch> Find(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        cancellationToken.ThrowIfCancellationRequested();

        SearchableGlossaryEntry[] snapshot;
        lock (_sync)
            snapshot = _entries.ToArray();

        return snapshot.FindMatches(query, maxResults, cancellationToken);
    }

    public IReadOnlyList<GlossaryTermMap> Map(string? term = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SearchableGlossaryEntry[] entries;
        lock (_sync)
            entries = _entries.ToArray();

        var targets = entries;
        if (term is not null)
        {
            var normalizedTerm = term.NormalizeGlossary();
            targets = entries
                .Where(entry => entry.NormalizedTerm == normalizedTerm)
                .ToArray();
        }

        return targets
            .Select(target => MapMentions(target, entries, cancellationToken))
            .ToArray();
    }

    private static GlossaryTermMap MapMentions(
        SearchableGlossaryEntry target,
        IReadOnlyList<SearchableGlossaryEntry> entries,
        CancellationToken cancellationToken)
    {
        var mentionedIn = entries
            .Where(source => Mentions(source, target, cancellationToken))
            .Select(source => source.Entry.Term)
            .ToArray();

        return new GlossaryTermMap(target.Entry.Term, mentionedIn);
    }

    private static bool Mentions(
        SearchableGlossaryEntry source,
        SearchableGlossaryEntry target,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return source.NormalizedTerm != target.NormalizedTerm &&
               source.NormalizedDescription.Contains(target.NormalizedTerm, StringComparison.Ordinal);
    }

    private void ReplaceState(IReadOnlyList<GlossaryEntry> entries)
    {
        var searchableEntries = new SearchableGlossaryEntry[entries.Count];

        for (var index = 0; index < entries.Count; index++)
        {
            Validate(entries[index]);
            searchableEntries[index] = SearchableGlossaryEntry.From(entries[index]);
        }

        ReplaceState(searchableEntries);
    }

    private void ReplaceState(IReadOnlyList<SearchableGlossaryEntry> entries)
    {
        var newEntries = new List<SearchableGlossaryEntry>(entries.Count);
        var newByNormalizedTerm = new Dictionary<string, SearchableGlossaryEntry>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            Validate(entry.Entry);

            if (!newByNormalizedTerm.TryAdd(entry.NormalizedTerm, entry))
                throw new InvalidDataException($"Duplicate term '{entry.Entry.Term}'.");

            newEntries.Add(entry);
        }

        lock (_sync)
        {
            _entries = newEntries;
            _byNormalizedTerm = newByNormalizedTerm;
        }
    }

    private static void Validate(GlossaryEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Term))
            throw new InvalidDataException("Empty term in glossary file.");

        if (string.IsNullOrWhiteSpace(entry.Description))
            throw new InvalidDataException("Empty description in glossary file.");
    }
}

internal sealed record GlossaryMatch(
    GlossaryEntry Entry,
    int Score);

internal sealed record GlossaryTermMap(
    string Term,
    IReadOnlyList<string> MentionedIn);

internal sealed record AddTermResult(
    int? TotalEntries,
    GlossaryEntry? ExistingEntry,
    ErrorInfo? Error)
{
    public static AddTermResult AsSuccess(int totalEntries) => new(totalEntries, null, null);
    public static AddTermResult AsExistsAlready(GlossaryEntry existingEntry) => new(null, existingEntry, new ErrorInfo("exists already"));
    public static AddTermResult AsError(string message) => new(null, null, new ErrorInfo(message));
}

internal sealed record EditTermResult(
    int? TotalEntries,
    ErrorInfo? Error)
{
    public static EditTermResult AsSuccess(int totalEntries) => new(totalEntries, null);
    public static EditTermResult AsError(string message) => new(null, new ErrorInfo(message));
}

internal sealed record DeleteTermResult(
    int? TotalEntries,
    GlossaryEntry? DeletedEntry,
    ErrorInfo? Error)
{
    public static DeleteTermResult AsSuccess(int totalEntries, GlossaryEntry deletedEntry) => new(totalEntries, deletedEntry, null);
    public static DeleteTermResult AsError(string message) => new(null, null, new ErrorInfo(message));
}
