using Phonematic.Helpers;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="GraphemeToPhoneme.Convert"/> rule coverage, edge cases, and output format.</summary>
public class GraphemeToPhonemeTests
{
    /// <summary>Verifies that an empty string input returns an empty phone array.</summary>
    [Fact]
    public void Convert_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(GraphemeToPhoneme.Convert(""));
    }

    /// <summary>Verifies that a punctuation-only input returns an empty phone array.</summary>
    [Fact]
    public void Convert_PunctuationOnly_ReturnsEmpty()
    {
        Assert.Empty(GraphemeToPhoneme.Convert("..."));
    }

    /// <summary>Verifies that a simple word returns at least one phone.</summary>
    [Fact]
    public void Convert_SimpleWord_ReturnsNonEmptyPhones()
    {
        var phones = GraphemeToPhoneme.Convert("test");
        Assert.NotEmpty(phones);
    }

    /// <summary>Verifies that a multi-syllable word produces more than one phone.</summary>
    [Fact]
    public void Convert_LongerWord_ReturnsMultiplePhones()
    {
        var phones = GraphemeToPhoneme.Convert("speaking");
        Assert.True(phones.Length > 1, $"Expected multiple phones, got {phones.Length}");
    }

    /// <summary>Verifies that common English digraphs map to their expected ARPAbet symbols.</summary>
    [Theory]
    [InlineData("ch", "CH")]
    [InlineData("th", "TH")]
    [InlineData("ng", "NG")]
    public void Convert_CommonDigraph_MapsToCorrectArpabet(string grapheme, string expectedArpabet)
    {
        var phones = GraphemeToPhoneme.Convert(grapheme);
        Assert.Contains(expectedArpabet, phones);
    }

    /// <summary>Verifies that upper- and lower-case input produce identical phone arrays.</summary>
    [Fact]
    public void Convert_IsCaseInsensitive()
    {
        var lower = GraphemeToPhoneme.Convert("test");
        var upper = GraphemeToPhoneme.Convert("TEST");
        Assert.Equal(lower, upper);
    }

    /// <summary>Verifies that every phone in the output is a non-empty, non-whitespace string.</summary>
    [Fact]
    public void Convert_AllPhonesAreNonEmpty()
    {
        var phones = GraphemeToPhoneme.Convert("something");
        Assert.All(phones, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }
}
