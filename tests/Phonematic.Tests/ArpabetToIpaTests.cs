using Phonematic.Helpers;

namespace Phonematic.Tests;

public class ArpabetToIpaTests
{
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

    [Fact]
    public void Convert_UnknownSymbol_ReturnsFallbackWithSlashes()
    {
        var result = ArpabetToIpa.Convert("XX");
        Assert.StartsWith("/", result);
        Assert.EndsWith("/", result);
    }

    [Theory]
    [InlineData("AH0")]
    [InlineData("AH1")]
    [InlineData("AH2")]
    public void Convert_StressDigitStripped_AllVariantsReturnSameBase(string arpabet)
    {
        Assert.Equal(ArpabetToIpa.Convert("AH"), ArpabetToIpa.Convert(arpabet));
    }

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
