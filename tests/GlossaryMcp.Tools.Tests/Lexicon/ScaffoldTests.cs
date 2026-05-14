using Is.Assertions;
using GlossaryMcp.Tools.Lexicon;
using Xunit;

namespace GlossaryMcp.Tools.Tests.Lexicon;

public sealed class ScaffoldTests
{
    [Fact]
    public void Store_starts_empty()
    {
        var store = new LexiconStore();

        store.Entries.IsEmpty();
        store.ByNormalizedTerm.IsEmpty();
    }
}
