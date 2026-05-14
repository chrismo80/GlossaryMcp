using System.Text;
using GlossaryMcp.Tools.Storage;

namespace GlossaryMcp.Tools.Glossary;

public sealed class GlossaryStore
{
    private readonly object _sync = new();
    private readonly JsonlFile<GlossaryEntry> _file;

    private List<GlossaryEntry> _entries = [];
    private Dictionary<string, GlossaryEntry> _byNormalizedTerm = new(StringComparer.Ordinal);

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
            return _entries.ToArray();
    }

    public bool TryGetEntry(string term, out GlossaryEntry? entry, CancellationToken cancellationToken = default)
    {
        if (term is null)
            throw new ArgumentNullException(nameof(term));

        cancellationToken.ThrowIfCancellationRequested();

        var key = Normalize(term);

        lock (_sync)
            return _byNormalizedTerm.TryGetValue(key, out entry);
    }

    public AddTermResult Add(string term, string description, CancellationToken cancellationToken = default)
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

        var key = Normalize(term);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_byNormalizedTerm.TryGetValue(key, out var existing))
                return AddTermResult.AsExistsAlready(existing);

            var entry = new GlossaryEntry(term, description);

            _file.Append(entry, cancellationToken);
            _entries.Add(entry);
            _byNormalizedTerm[key] = entry;

            return AddTermResult.AsSuccess(_entries.Count);
        }
    }

    public EditTermResult Edit(string term, string description, CancellationToken cancellationToken = default)
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

        var key = Normalize(term);
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

                updatedEntries[index] = new GlossaryEntry(existing.Term, description);
                _file.RewriteAll(updatedEntries, cancellationToken);
                ReplaceState(updatedEntries);
                return EditTermResult.AsSuccess(_entries.Count);
            }

            return EditTermResult.AsError("term not found");
        }
    }

    public IReadOnlyList<GlossaryMatch> Find(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        cancellationToken.ThrowIfCancellationRequested();

        if (maxResults <= 0)
            return [];

        var normalizedQuery = Normalize(query);
        if (normalizedQuery.Length == 0)
            return [];

        var tokens = Tokenize(query);

        GlossaryEntry[] snapshot;
        lock (_sync)
            snapshot = _entries.ToArray();

        var matches = new List<GlossaryMatch>();

        foreach (var entry in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedTerm = Normalize(entry.Term);
            var normalizedDescription = Normalize(entry.Description);

            var score = 0;
            var matchCount = 0;

            ApplyFullQueryRules(ref score, ref matchCount, normalizedTerm, normalizedDescription, normalizedQuery);
            ApplyTokenRules(ref score, ref matchCount, normalizedTerm, normalizedDescription, tokens);

            if (score > 0)
                matches.Add(new GlossaryMatch(entry, score + matchCount));
        }

        return matches
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Term.Length)
            .ThenBy(x => x.Entry.Term, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToArray();
    }

    private void ReplaceState(IReadOnlyList<GlossaryEntry> entries)
    {
        var newEntries = new List<GlossaryEntry>(entries.Count);
        var newByNormalizedTerm = new Dictionary<string, GlossaryEntry>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            Validate(entry);

            var key = Normalize(entry.Term);
            if (!newByNormalizedTerm.TryAdd(key, entry))
                throw new InvalidDataException($"Duplicate term '{entry.Term}'.");

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

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var lowered = trimmed.ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var previousWasWhitespace = false;

        foreach (var character in lowered)
        {
            var replacement = character switch
            {
                'ä' => "ae",
                'ö' => "oe",
                'ü' => "ue",
                'ß' => "ss",
                _ => null
            };

            if (replacement is not null)
            {
                foreach (var replacementCharacter in replacement)
                    Append(replacementCharacter);

                continue;
            }

            Append(character);
        }

        return builder.ToString();

        void Append(char character)
        {
            if (char.IsWhiteSpace(character))
            {
                if (builder.Length == 0)
                    return;

                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                return;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }
    }

    private static IReadOnlyList<string> Tokenize(string query)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0)
            return [];

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            tokens.Add(token);

        return tokens.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static void ApplyFullQueryRules(
        ref int score,
        ref int matchCount,
        string term,
        string description,
        string normalizedQuery)
    {
        if (term == normalizedQuery)
            AddMatch(ref score, ref matchCount, 1000);
        else if (term.Contains(normalizedQuery, StringComparison.Ordinal))
            AddMatch(ref score, ref matchCount, 300);

        if (description == normalizedQuery)
            AddMatch(ref score, ref matchCount, 150);
        else if (description.Contains(normalizedQuery, StringComparison.Ordinal))
            AddMatch(ref score, ref matchCount, 80);
    }

    private static void ApplyTokenRules(
        ref int score,
        ref int matchCount,
        string term,
        string description,
        IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (term == token)
                AddMatch(ref score, ref matchCount, 120);
            else if (term.Contains(token, StringComparison.Ordinal))
                AddMatch(ref score, ref matchCount, 40);

            if (description == token)
                AddMatch(ref score, ref matchCount, 30);
            else if (description.Contains(token, StringComparison.Ordinal))
                AddMatch(ref score, ref matchCount, 10);
        }
    }

    private static void AddMatch(ref int score, ref int matchCount, int delta)
    {
        score += delta;
        matchCount += 1;
    }
}

public sealed record GlossaryMatch(
    GlossaryEntry Entry,
    int Score);

public sealed record AddTermResult(
    int? TotalEntries,
    GlossaryEntry? ExistingEntry,
    ErrorInfo? Error)
{
    public static AddTermResult AsSuccess(int totalEntries) => new(totalEntries, null, null);
    public static AddTermResult AsExistsAlready(GlossaryEntry existingEntry) => new(null, existingEntry, new ErrorInfo("exists already"));
    public static AddTermResult AsError(string message) => new(null, null, new ErrorInfo(message));
}

public sealed record EditTermResult(
    int? TotalEntries,
    ErrorInfo? Error)
{
    public static EditTermResult AsSuccess(int totalEntries) => new(totalEntries, null);
    public static EditTermResult AsError(string message) => new(null, new ErrorInfo(message));
}
