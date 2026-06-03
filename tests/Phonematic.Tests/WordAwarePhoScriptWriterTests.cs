using System.Xml.Linq;
using Phonematic.Helpers;
using Phonematic.Models;

namespace Phonematic.Tests;

public class WordAwarePhoScriptWriterTests
{
    private static readonly SpeakerBaseline Baseline = new()
    {
        F0MeanHz = 120, F0P10Hz = 90, F0P90Hz = 160,
        IntensityMeanDb = -20, RatePhonesPerSecond = 5, VoiceQuality = "modal",
    };

    private static readonly IReadOnlyList<AcousticFeatureFrame> NoFrames = [];

    private static WordAlignment Word(string orth, params string[] ipa)
    {
        var phones = new List<PhoneAlignment>();
        var t = 0;
        foreach (var symbol in ipa)
        {
            phones.Add(new PhoneAlignment(symbol, t, t + 50, 0.9f));
            t += 50;
        }
        return new WordAlignment(orth, phones);
    }

    private static string Write(string asrModel, params IReadOnlyList<WordAlignment>[] sentences) =>
        PhoScriptWriter.Write(sentences, NoFrames, Baseline, "test.wav", asrModel);

    [Fact]
    public void Write_PopulatesOrthFromWords_AndAsrModel()
    {
        var result = Write("wav2vec2-phoneme+forced-align",
            [Word("hello", "/h/", "/ɛ/"), Word("world", "/w/")]);

        Assert.Contains("<word orth=\"hello\"", result);
        Assert.Contains("<word orth=\"world\"", result);
        Assert.Contains("asr_model=\"wav2vec2-phoneme+forced-align\"", result);
        Assert.DoesNotContain("\r\n", result); // LF line endings
    }

    [Fact]
    public void Write_MultipleSentences_EmitsNumberedUtterances()
    {
        var result = Write("whisper+wav2vec2-forced-align",
            [Word("one", "/w/")], [Word("two", "/t/")]);

        Assert.Contains("id=\"utt_001\"", result);
        Assert.Contains("id=\"utt_002\"", result);
    }

    [Fact]
    public void Write_LastWordGetsIpEnd_OthersGetNone()
    {
        var result = Write("m", [Word("a", "/ʌ/"), Word("b", "/b/")]);
        Assert.Contains("phrase_boundary=\"IP_end\"", result);
        Assert.Contains("phrase_boundary=\"none\"", result);
    }

    [Fact]
    public void Write_ProducesWellFormedXml()
    {
        var result = Write("m", [Word("hi", "/h/", "/aɪ/")]);

        // Drop the leading "## …" comment header, wrap the fragment, and parse.
        var body = string.Join("\n", result.Split('\n').Where(l => !l.StartsWith("##")));
        var doc = XDocument.Parse($"<root>{body}</root>");
        Assert.Equal("hi", (string?)doc.Root!.Element("sentence")!.Element("word")!.Attribute("orth"));
    }
}
