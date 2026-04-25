using Phonematic.Helpers;

namespace Phonematic.Tests;

public class GraphemeToPhonemeTests
{
    [Fact]
    public void Convert_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(GraphemeToPhoneme.Convert(""));
    }

    [Fact]
    public void Convert_PunctuationOnly_ReturnsEmpty()
    {
        Assert.Empty(GraphemeToPhoneme.Convert("..."));
    }

    [Fact]
    public void Convert_SimpleWord_ReturnsNonEmptyPhones()
    {
        var phones = GraphemeToPhoneme.Convert("test");
        Assert.NotEmpty(phones);
    }

    [Fact]
    public void Convert_LongerWord_ReturnsMultiplePhones()
    {
        var phones = GraphemeToPhoneme.Convert("speaking");
        Assert.True(phones.Length > 1, $"Expected multiple phones, got {phones.Length}");
    }

    [Theory]
    [InlineData("sh", "SH")]
    [InlineData("ch", "CH")]
    [InlineData("th", "TH")]
    [InlineData("ng", "NG")]
    public void Convert_CommonDigraph_MapsToCorrectArpabet(string grapheme, string expectedArpabet)
    {
        var phones = GraphemeToPhoneme.Convert(grapheme);
        Assert.Contains(expectedArpabet, phones);
    }

    [Fact]
    public void Convert_IsCaseInsensitive()
    {
        var lower = GraphemeToPhoneme.Convert("test");
        var upper = GraphemeToPhoneme.Convert("TEST");
        Assert.Equal(lower, upper);
    }

    [Fact]
    public void Convert_AllPhonesAreNonEmpty()
    {
        var phones = GraphemeToPhoneme.Convert("something");
        Assert.All(phones, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }
}
