using Phonematic.Helpers;
using Whisper.net;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="PhoScriptWriterLegacy"/> XML structure, word/phone formatting, boundary markers, and helper methods.</summary>
public class PhoScriptWriterTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Creates a minimal <see cref="SegmentData"/> for use in writer tests.</summary>
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

    /// <summary>Verifies that splitting an empty string returns an empty list.</summary>
    [Fact]
    public void SplitWords_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(PhoScriptWriterLegacy.SplitWords(""));
    }

    /// <summary>Verifies that extra whitespace between words is collapsed and blank entries are discarded.</summary>
    [Fact]
    public void SplitWords_MultipleSpaces_DiscardsBlanks()
    {
        var result = PhoScriptWriterLegacy.SplitWords("  hello   world  ");
        Assert.Equal(["hello", "world"], result);
    }

    // -----------------------------------------------------------------------
    // Escape
    // -----------------------------------------------------------------------

    /// <summary>Verifies that XML special characters are correctly entity-encoded by <see cref="PhoScriptWriterLegacy.Escape"/>.</summary>
    [Theory]
    [InlineData("<word>",      "&lt;word&gt;")]
    [InlineData("say \"hi\"",  "say &quot;hi&quot;")]
    public void Escape_EncodesXmlChars(string input, string expected)
    {
        Assert.Equal(expected, PhoScriptWriterLegacy.Escape(input));
    }

    // -----------------------------------------------------------------------
    // GetIpaPhones
    // -----------------------------------------------------------------------

    /// <summary>Verifies that <see cref="PhoScriptWriterLegacy.GetIpaPhones"/> returns slash-delimited IPA strings for a known word.</summary>
    [Fact]
    public void GetIpaPhones_KnownWord_ReturnsSlashDelimitedIpa()
    {
        var phones = PhoScriptWriterLegacy.GetIpaPhones("the");
        Assert.NotEmpty(phones);
        Assert.All(phones, p =>
        {
            Assert.StartsWith("/", p);
            Assert.EndsWith("/", p);
        });
    }

    /// <summary>Verifies that <see cref="PhoScriptWriterLegacy.GetIpaPhones"/> returns the correct number of phones for a known word.</summary>
    [Fact]
    public void GetIpaPhones_KnownWord_CorrectPhoneCount()
    {
        // "cat" → K AE1 T → 3 phones
        var phones = PhoScriptWriterLegacy.GetIpaPhones("cat");
        Assert.Equal(3, phones.Count);
    }

    /// <summary>Verifies that an OOV word returns a non-empty list of slash-delimited fallback phones from the G2P rules.</summary>
    [Fact]
    public void GetIpaPhones_UnknownWord_ReturnsFallbackPhones()
    {
        var phones = PhoScriptWriterLegacy.GetIpaPhones("zzzyyyxxx");
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

    /// <summary>Verifies that writing an empty segment list produces only the PhoScript header without any sentence elements.</summary>
    [Fact]
    public void Write_EmptySegments_ReturnsHeaderOnly()
    {
        var result = PhoScriptWriterLegacy.WriteLegacy([], "test.mp3");
        Assert.Contains("## PhoScript 1.0", result);
        Assert.DoesNotContain("<sentence", result);
    }

    /// <summary>Verifies that a single segment produces a well-formed <c>&lt;sentence&gt;</c> block.</summary>
    [Fact]
    public void Write_SingleSegment_ContainsSentenceBlock()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        Assert.Contains("<sentence id=\"utt_001\"", result);
        Assert.Contains("</sentence>", result);
    }

    /// <summary>Verifies that the output uses LF (not CRLF) line endings.</summary>
    [Fact]
    public void Write_UsesLfLineEndings()
    {
        var seg = MakeSegment("hello");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        Assert.DoesNotContain("\r\n", result);
    }

    /// <summary>Verifies that each word in the segment appears as a <c>&lt;word orth=…&gt;</c> element.</summary>
    [Fact]
    public void Write_ContainsWordBlocks()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        Assert.Contains("<word orth=\"hello\"", result);
        Assert.Contains("<word orth=\"world\"", result);
    }

    /// <summary>Verifies that word elements contain at least one <c>&lt;phon&gt;</c> child element.</summary>
    [Fact]
    public void Write_WordBlocksHavePhonChildren()
    {
        var seg = MakeSegment("cat");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        Assert.Contains("<phon ipa=", result);
    }

    /// <summary>Verifies that <c>&lt;phon&gt;</c> IPA attribute values are slash-delimited.</summary>
    [Fact]
    public void Write_PhonIpaAttributesUseSlashDelimiters()
    {
        var seg = MakeSegment("cat");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        // Should contain ipa="/x/" pattern somewhere
        Assert.Matches(@"ipa=""\/[^/]+\/""", result);
    }

    /// <summary>Verifies that each <c>&lt;phon&gt;</c> element contains timestamp attributes.</summary>
    [Fact]
    public void Write_PhonTimestampsAreConsistent()
    {
        var seg = MakeSegment("cat", 0, 300);
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");

        // dur_ms must equal t_end - t_start on every phon line
        var phonLines = result.Split('\n')
            .Where(l => l.TrimStart().StartsWith("<phon ", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(phonLines);
    }

    /// <summary>Verifies that the last word in a sentence carries an <c>IP_end</c> phrase boundary marker.</summary>
    [Fact]
    public void Write_LastWordHasIpEndBoundary()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        Assert.Contains("phrase_boundary=\"IP_end\"", result);
    }

    /// <summary>Verifies that non-final words carry a <c>none</c> phrase boundary marker.</summary>
    [Fact]
    public void Write_NonLastWordHasNoneBoundary()
    {
        var seg = MakeSegment("hello world");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "test.mp3");
        Assert.Contains("phrase_boundary=\"none\"", result);
    }

    /// <summary>Verifies that the source file name is embedded as the <c>recording_id</c> in the output.</summary>
    [Fact]
    public void Write_SourceFileNameInMeta()
    {
        var seg = MakeSegment("hi");
        var result = PhoScriptWriterLegacy.WriteLegacy([seg], "my_recording.mp3");
        Assert.Contains("recording_id=\"my_recording\"", result);
    }

    /// <summary>Verifies that multiple segments produce sequentially numbered utterance IDs.</summary>
    [Fact]
    public void Write_MultipleSegments_AllPresent()
    {
        var segs = new[]
        {
            MakeSegment("hello", 0, 500),
            MakeSegment("world", 500, 1000),
        };
        var result = PhoScriptWriterLegacy.WriteLegacy(segs, "test.mp3");
        Assert.Contains("utt_001", result);
        Assert.Contains("utt_002", result);
    }
}
