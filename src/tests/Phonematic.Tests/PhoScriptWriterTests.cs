using Phonematic.Helpers;
using Whisper.net;

namespace Phonematic.Tests;

public class PhoScriptWriterTests
{
    private static SegmentData MakeSegment(double startSec, double endSec, string text, float probability = 0.95f) =>
        new()
        {
            Start = TimeSpan.FromSeconds(startSec),
            End = TimeSpan.FromSeconds(endSec),
            Text = text,
            Probability = probability,
            Language = "en",
        };

    [Fact]
    public void Write_EmptySegments_ReturnsHeaderOnly()
    {
        var result = PhoScriptWriter.Write([], "test.m4a");

        Assert.Contains("## PhoScript 1.0", result);
        Assert.DoesNotContain("<sentence", result);
    }

    [Fact]
    public void Write_SingleSegment_ContainsSentenceBlock()
    {
        var segments = new List<SegmentData> { MakeSegment(0, 2, "Hello world") };

        var result = PhoScriptWriter.Write(segments, "audio.m4a", new DateOnly(2025, 1, 15));

        Assert.Contains("<sentence id=\"utt_001\"", result);
        Assert.Contains("lang=\"en\"", result);
        Assert.Contains("duration_ms=\"2000\"", result);
        Assert.Contains("</sentence>", result);
    }

    [Fact]
    public void Write_SingleSegment_ContainsMetaBlock()
    {
        var segments = new List<SegmentData> { MakeSegment(0, 1, "Hi") };

        var result = PhoScriptWriter.Write(segments, "my_recording.m4a", new DateOnly(2025, 6, 1));

        Assert.Contains("recording_id=\"my_recording\"", result);
        Assert.Contains("date=\"2025-06-01\"", result);
        Assert.Contains("asr_model=\"whisper\"", result);
    }

    [Fact]
    public void Write_SingleSegment_ContainsWordBlocks()
    {
        var segments = new List<SegmentData> { MakeSegment(0, 2, "Hello world") };

        var result = PhoScriptWriter.Write(segments, "audio.m4a");

        Assert.Contains("orth=\"Hello\"", result);
        Assert.Contains("orth=\"world\"", result);
    }

    [Fact]
    public void Write_LastWordHasIPEndBoundary()
    {
        var segments = new List<SegmentData> { MakeSegment(0, 1, "One two three") };

        var result = PhoScriptWriter.Write(segments, "audio.m4a");

        // The last word block should have IP_end boundary
        var lines = result.Split('\n');
        var lastWordBoundaryLine = lines
            .Where(l => l.Contains("phrase_boundary="))
            .Last();
        Assert.Contains("IP_end", lastWordBoundaryLine);
    }

    [Fact]
    public void Write_MultipleSegments_AllIncluded()
    {
        var segments = new List<SegmentData>
        {
            MakeSegment(0, 1, "First sentence"),
            MakeSegment(1, 3, "Second sentence"),
            MakeSegment(3, 5, "Third sentence"),
        };

        var result = PhoScriptWriter.Write(segments, "audio.m4a");

        Assert.Contains("id=\"utt_001\"", result);
        Assert.Contains("id=\"utt_002\"", result);
        Assert.Contains("id=\"utt_003\"", result);
    }

    [Fact]
    public void Write_UsesLFLineEndings()
    {
        var segments = new List<SegmentData> { MakeSegment(0, 1, "Test") };

        var result = PhoScriptWriter.Write(segments, "audio.m4a");

        Assert.DoesNotContain("\r\n", result);
        Assert.Contains("\n", result);
    }

    [Theory]
    [InlineData("AT&T", "AT&amp;T")]
    [InlineData("say \"hi\"", "say &quot;hi&quot;")]
    [InlineData("<tag>", "&lt;tag&gt;")]
    public void Escape_HandlesSpecialCharacters(string input, string expected)
    {
        Assert.Equal(expected, PhoScriptWriter.Escape(input));
    }

    [Theory]
    [InlineData("  hello   world  ", 2)]
    [InlineData("one", 1)]
    [InlineData("", 0)]
    [InlineData("  ", 0)]
    public void SplitWords_ReturnsCorrectCount(string input, int expectedCount)
    {
        Assert.Equal(expectedCount, PhoScriptWriter.SplitWords(input).Count);
    }
}
