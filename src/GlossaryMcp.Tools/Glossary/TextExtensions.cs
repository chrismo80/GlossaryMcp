using System.Text;

namespace GlossaryMcp.Tools.Glossary;

internal static class TextExtensions
{
    private static readonly Dictionary<char, string> replacements = new()
    {
        { ',', " " },
        { '.', " " },
        { ';', " " },
        { 'ä', "ae" },
        { 'ö', "oe" },
        { 'ü', "ue" },
        { 'ß', "ss" },
    };

    extension(string value)
    {
        public string NormalizeGlossary()
        {
            ArgumentNullException.ThrowIfNull(value);

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
                return string.Empty;

            var lowered = trimmed.ToLowerInvariant();
            var builder = new StringBuilder(lowered.Length);
            var previousWasWhitespace = false;

            foreach (var character in lowered)
            {
                replacements.TryGetValue(character, out var replacement);

                if (replacement is not null)
                {
                    foreach (var replacementCharacter in replacement)
                        Append(replacementCharacter);

                    continue;
                }

                Append(character);
            }

            return builder.ToString().TrimEnd();

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

        internal IReadOnlyList<string> TokenizeGlossary()
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length == 0)
                return [];

            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                tokens.Add(token);

            return tokens
                .Where(token => token.Length > 2)
                .ToArray();
        }
    }
}