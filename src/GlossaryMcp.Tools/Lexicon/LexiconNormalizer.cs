using System.Text;

namespace GlossaryMcp.Tools.Lexicon;

public static class LexiconNormalizer
{
    public static string NormalizeGlossary(this string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var lowered = trimmed.ToLowerInvariant();

        var sb = new StringBuilder(lowered.Length);
        var previousWasWhitespace = false;

        foreach (var ch in lowered)
        {
            var normalized = ch switch
            {
                'ä' => "ae",
                'ö' => "oe",
                'ü' => "ue",
                'ß' => "ss",
                _ => null
            };

            if (normalized is not null)
            {
                foreach (var n in normalized)
                    AppendChar(n);

                continue;
            }

            AppendChar(ch);
        }

        return sb.ToString();

        void AppendChar(char ch)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (sb.Length == 0)
                    return;

                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }

                return;
            }

            sb.Append(ch);
            previousWasWhitespace = false;
        }
    }

    public static IReadOnlyList<string> TokenizeGlossary(this string query)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        var normalized = query.NormalizeGlossary();
        if (normalized.Length == 0)
            return [];

        var tokens = new HashSet<string>(StringComparer.Ordinal);

        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            tokens.Add(token);

        return tokens.OrderBy(t => t, StringComparer.Ordinal).ToList();
    }
}
