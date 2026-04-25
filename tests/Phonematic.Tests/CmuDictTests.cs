using Phonematic.Helpers;

namespace Phonematic.Tests;

public class CmuDictTests
{
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

    [Fact]
    public void TryGetPhones_CaseInsensitive()
    {
        Assert.True(CmuDict.TryGetPhones("CAT", out _));
        Assert.True(CmuDict.TryGetPhones("Cat", out _));
        Assert.True(CmuDict.TryGetPhones("cat", out _));
    }

    [Fact]
    public void TryGetPhones_UnknownWord_ReturnsFalse()
    {
        Assert.False(CmuDict.TryGetPhones("zzzyyyxxx", out var phones));
        Assert.Null(phones);
    }

    [Fact]
    public void TryGetPhones_WordWithPunctuation_StripsAndFinds()
    {
        Assert.True(CmuDict.TryGetPhones("hello!", out _));
        Assert.True(CmuDict.TryGetPhones("\"cat\"", out _));
    }

    [Fact]
    public void TryGetPhones_ReturnsNonEmptyPhones()
    {
        Assert.True(CmuDict.TryGetPhones("phonetic", out var phones));
        Assert.NotEmpty(phones!);
    }

    [Fact]
    public void StripPunctuation_RemovesLeadingAndTrailingMarks()
    {
        Assert.Equal("hello", CmuDict.StripPunctuation("hello!"));
        Assert.Equal("hello", CmuDict.StripPunctuation("\"hello\""));
        Assert.Equal("hello", CmuDict.StripPunctuation("(hello)"));
    }
}
