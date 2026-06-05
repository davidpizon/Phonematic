using Phonematic.Helpers;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="CmuDict"/> word lookup, case-insensitivity, punctuation stripping, and missing-word handling.</summary>
public class CmuDictTests
{
    /// <summary>Verifies that known words return their expected ARPAbet phone arrays.</summary>
    [Theory]
    [InlineData("really",  new[] { "R", "IH1", "L", "IY0" })]
    [InlineData("hello",   new[] { "HH", "AH0", "L", "OW1" })]
    [InlineData("the",     new[] { "DH", "AH0" })]
    [InlineData("cat",     new[] { "K", "AE1", "T" })]
    [InlineData("dog",     new[] { "D", "AO1", "G" })]
    public void TryGetPhones_KnownWord_ReturnsTrueAndCorrectPhones(string word, string[] expected)
    {
        var found = CmuDict.TryGetPhones(word, out var phones);
        Assert.True(found);
        Assert.Equal(expected, phones);
    }

    /// <summary>Verifies that lookup is case-insensitive for the same headword.</summary>
    [Fact]
    public void TryGetPhones_CaseInsensitive()
    {
        Assert.True(CmuDict.TryGetPhones("CAT", out _));
        Assert.True(CmuDict.TryGetPhones("Cat", out _));
        Assert.True(CmuDict.TryGetPhones("cat", out _));
    }

    /// <summary>Verifies that an unknown word returns <see langword="false"/> and a null phones array.</summary>
    [Fact]
    public void TryGetPhones_UnknownWord_ReturnsFalse()
    {
        Assert.False(CmuDict.TryGetPhones("zzzyyyxxx", out var phones));
        Assert.Null(phones);
    }

    /// <summary>Verifies that trailing punctuation is stripped before lookup, allowing common words to be found.</summary>
    [Fact]
    public void TryGetPhones_WordWithPunctuation_StripsAndFinds()
    {
        Assert.True(CmuDict.TryGetPhones("hello!", out _));
        Assert.True(CmuDict.TryGetPhones("\"cat\"", out _));
    }

    /// <summary>Verifies that a found word's phone array is non-empty.</summary>
    [Fact]
    public void TryGetPhones_ReturnsNonEmptyPhones()
    {
        Assert.True(CmuDict.TryGetPhones("phonetic", out var phones));
        Assert.NotEmpty(phones!);
    }

    /// <summary>Verifies that <see cref="CmuDict.StripPunctuation"/> removes common surrounding punctuation marks.</summary>
    [Fact]
    public void StripPunctuation_RemovesLeadingAndTrailingMarks()
    {
        Assert.Equal("hello", CmuDict.StripPunctuation("hello!"));
        Assert.Equal("hello", CmuDict.StripPunctuation("\"hello\""));
        Assert.Equal("hello", CmuDict.StripPunctuation("(hello)"));
    }
}
