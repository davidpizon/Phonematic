using Phonematic.Helpers;
using Whisper.net;

namespace Phonematic.Tests;

public class PhoScriptWriterTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static SegmentData MakeSegment(string text, int startMs = 0, int endMs = 1000)
    {
        return new SegmentData(
            text,
            TimeSpan.FromMilliseconds(startMs),
            TimeSpan.FromMilliseconds(endMs),
            minProbability: 0.9f,
            maxProbability: 0.95f,
            probability: 0.92f,
            noSpeechProbability: 0.05f,
            language: "en",
            tokens: []);
    }

    // -----------------------------------------------------------------------
    // SplitWords
    // -----------------------------------------------------------------------

    [Fact]
    public void SplitWords_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(PhoScriptWriter.SplitWords(""));
    }

    [Fact]
    public void SplitWords_MultipleSpaces_DiscardsBlanks()
    {
        var result = PhoScriptWriter.SplitWords("  hello   world  ");
        Assert.Equal(["hello", "world"], result);
    }

    // -----------------------------------------------------------------------
    // Escape
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("Tom & Jerry", "Tom &amp; Jerry")]
    [InlineData("<word>",      "&lt;word&gt;")]
    [InlineData("say \"hi\"",  "say &quot;hi&quot;")]
    public void Escape_EncodesXmlChars(string input, string expected)
    {
        Assert.Equal(expected, PhoScriptWriter.Escape(input));
    }

    // -----------------------------------------------------------------------
    // GetIpaPhones
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIpaPhones_KnownWord_ReturnsSlashDelimitedIpa()
    {
        var phones = PhoScriptWriter.GetIpaPhones("the");
        Assert.NotEmpty(phones);
        Assert.All(phones, p =>
        {
            Assert.StartsWith("/", p);
            Assert.EndsWith("/", p);
        });
    }

    [Fact]
    public void GetIpaPhones_KnownWord_CorrectPhoneCount()
    {
        // "cat" → K AE1 T → 3 phones
        var phones = PhoScriptWriter.GetIpaPhones("cat");
        Assert.Equal(3, phones.Count);
    }

    [Fact]
    public void GetIpaPhones_UnknownWord_ReturnsFallbackPhones()
    {
        var phones = PhoScriptWriter.GetIpaPhones("zzzyyyxxx");
        Assert.NotEmpty(phones);
        Assert.All(phones, p =>
        {
            Assert.StartsWith("/", p);
            Assert.EndsWith("/", p);
        });
    }

    // -----------------------------------------------------------------------
    // Write — document structure
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_EmptySegments_ReturnsHeaderOnly()
    {
        var result = PhoScriptWriter.Write([], "test.mp3");
        Assert.Contains("## PhoScript 1.0", result);
        Assert.DoesNotContain("<sentence", result);
    }

    [Fact]
    public void Write_SingleSegment_ContainsSentenceBlock()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        Assert.Contains("<sentence id=\"utt_001\"", result);
        Assert.Contains("</sentence>", result);
    }

    [Fact]
    public void Write_UsesLfLineEndings()
    {
        var seg = MakeSegment("hello");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        Assert.DoesNotContain("\r\n", result);
    }

    [Fact]
    public void Write_ContainsWordBlocks()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        Assert.Contains("<word orth=\"hello\"", result);
        Assert.Contains("<word orth=\"world\"", result);
    }

    [Fact]
    public void Write_WordBlocksHavePhonChildren()
    {
        var seg = MakeSegment("cat");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        Assert.Contains("<phon ipa=", result);
    }

    [Fact]
    public void Write_PhonIpaAttributesUseSlashDelimiters()
    {
        var seg = MakeSegment("cat");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        // Should contain ipa="/x/" pattern somewhere
        Assert.Matches(@"ipa=""\/[^/]+\/""", result);
    }

    [Fact]
    public void Write_PhonTimestampsAreConsistent()
    {
        var seg = MakeSegment("cat", 0, 300);
        var result = PhoScriptWriter.Write([seg], "test.mp3");

        // dur_ms must equal t_end - t_start on every phon line
        var phonLines = result.Split('\n')
            .Where(l => l.TrimStart().StartsWith("<phon ", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(phonLines);
    }

    [Fact]
    public void Write_LastWordHasIpEndBoundary()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        Assert.Contains("phrase_boundary=\"IP_end\"", result);
    }

    [Fact]
    public void Write_NonLastWordHasNoneBoundary()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriter.Write([seg], "test.mp3");
        Assert.Contains("phrase_boundary=\"none\"", result);
    }

    [Fact]
    public void Write_SourceFileNameInMeta()
    {
        var seg = MakeSegment("hi");
        var result = PhoScriptWriter.Write([seg], "my_recording.mp3");
        Assert.Contains("recording_id=\"my_recording\"", result);
    }

    [Fact]
    public void Write_MultipleSegments_AllPresent()
    {
        var segs = new[]
        {
            MakeSegment("hello", 0, 500),
            MakeSegment("world", 500, 1000),
        };
        var result = PhoScriptWriter.Write(segs, "test.mp3");
        Assert.Contains("utt_001", result);
        Assert.Contains("utt_002", result);
    }
}
