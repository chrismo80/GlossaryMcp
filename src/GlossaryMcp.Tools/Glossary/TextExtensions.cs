using System.Text;

namespace GlossaryMcp.Tools.Glossary;

public static class TextExtensions
{
    public static string NormalizeGlossary(this string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

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

    public static IReadOnlyList<string> TokenizeGlossary(this string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var normalized = value.NormalizeGlossary();
        if (normalized.Length == 0)
            return [];

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            tokens.Add(token);

        return tokens.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }
}
