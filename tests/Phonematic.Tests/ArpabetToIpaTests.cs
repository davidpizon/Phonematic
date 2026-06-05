using Phonematic.Helpers;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="ArpabetToIpa.Convert"/> symbol mapping, stress-digit stripping, and output formatting.</summary>
public class ArpabetToIpaTests
{
    /// <summary>Verifies that known ARPAbet symbols (including stressed variants) map to the correct slash-delimited IPA string.</summary>
    [Theory]
    [InlineData("AH0", "/ʌ/")]
    [InlineData("AH1", "/ʌ/")]
    [InlineData("AH2", "/ʌ/")]
    [InlineData("IY1", "/i/")]
    [InlineData("R",   "/ɹ/")]
    [InlineData("SH",  "/ʃ/")]
    [InlineData("CH",  "/tʃ/")]
    [InlineData("NG",  "/ŋ/")]
    [InlineData("TH",  "/θ/")]
    [InlineData("DH",  "/ð/")]
    [InlineData("ZH",  "/ʒ/")]
    [InlineData("JH",  "/dʒ/")]
    [InlineData("HH",  "/h/")]
    [InlineData("EY1", "/eɪ/")]
    [InlineData("AY1", "/aɪ/")]
    [InlineData("OW1", "/oʊ/")]
    [InlineData("AW1", "/aʊ/")]
    [InlineData("OY1", "/ɔɪ/")]
    [InlineData("ER0", "/ɝ/")]
    public void Convert_KnownArpabet_ReturnsExpectedIpa(string arpabet, string expectedIpa)
    {
        Assert.Equal(expectedIpa, ArpabetToIpa.Convert(arpabet));
    }

    /// <summary>Verifies that an unknown ARPAbet symbol returns a fallback value that is still wrapped in slashes.</summary>
    [Fact]
    public void Convert_UnknownSymbol_ReturnsFallbackWithSlashes()
    {
        var result = ArpabetToIpa.Convert("XX");
        Assert.StartsWith("/", result);
        Assert.EndsWith("/", result);
    }

    /// <summary>Verifies that all stress variants of the same phoneme (0, 1, 2) return the same base IPA symbol.</summary>
    [Theory]
    [InlineData("AH0")]
    [InlineData("AH1")]
    [InlineData("AH2")]
    public void Convert_StressDigitStripped_AllVariantsReturnSameBase(string arpabet)
    {
        Assert.Equal(ArpabetToIpa.Convert("AH"), ArpabetToIpa.Convert(arpabet));
    }

    /// <summary>Verifies that all common stop and fricative symbols produce output wrapped in slashes.</summary>
    [Fact]
    public void Convert_ResultAlwaysWrappedInSlashes()
    {
        foreach (var sym in new[] { "P", "T", "K", "B", "D", "G", "M", "N", "L", "S", "Z" })
        {
            var result = ArpabetToIpa.Convert(sym);
            Assert.StartsWith("/", result);
            Assert.EndsWith("/", result);
        }
    }
}
