using System.Xml.Linq;
using Phonematic.Helpers;
using Phonematic.Models;

namespace Phonematic.Tests;

/// <summary>Verifies the word-aware <see cref="PhoScriptWriter.Write(System.Collections.Generic.IReadOnlyList{Phonematic.Models.WordAlignment}[], System.Collections.Generic.IReadOnlyList{Phonematic.Models.AcousticFeatureFrame}, Phonematic.Models.SpeakerBaseline, string, string, System.DateOnly?)" /> overload: orth propagation, sentence numbering, phrase boundaries, and XML well-formedness.</summary>
public class WordAwarePhoScriptWriterTests
{
    /// <summary>Speaker baseline used across all tests (modal voice, mid-range F0).</summary>
    private static readonly SpeakerBaseline Baseline = new()
    {
        F0MeanHz = 120, F0P10Hz = 90, F0P90Hz = 160,
        IntensityMeanDb = -20, RatePhonesPerSecond = 5, VoiceQuality = "modal",
    };

    /// <summary>Empty frame list used in tests that do not exercise acoustic feature output.</summary>
    private static readonly IReadOnlyList<AcousticFeatureFrame> NoFrames = [];

    /// <summary>Builds a <see cref="WordAlignment"/> with the given orth and a 50 ms phone per IPA symbol.</summary>
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

    /// <summary>Calls <see cref="PhoScriptWriter.Write"/> with the test baseline, no frames, and the given sentences.</summary>
    private static string Write(string asrModel, params IReadOnlyList<WordAlignment>[] sentences) =>
        PhoScriptWriter.Write(sentences, NoFrames, Baseline, "test.wav", asrModel);

    /// <summary>Verifies that word orth values and the ASR model identifier are present in the output.</summary>
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

    /// <summary>Verifies that multiple sentence lists produce sequentially numbered utterance IDs.</summary>
    [Fact]
    public void Write_MultipleSentences_EmitsNumberedUtterances()
    {
        var result = Write("whisper+wav2vec2-forced-align",
            [Word("one", "/w/")], [Word("two", "/t/")]);

        Assert.Contains("id=\"utt_001\"", result);
        Assert.Contains("id=\"utt_002\"", result);
    }

    /// <summary>Verifies that the final word has an <c>IP_end</c> phrase boundary and preceding words have <c>none</c>.</summary>
    [Fact]
    public void Write_LastWordGetsIpEnd_OthersGetNone()
    {
        var result = Write("m", [Word("a", "/ʌ/"), Word("b", "/b/")]);
        Assert.Contains("phrase_boundary=\"IP_end\"", result);
        Assert.Contains("phrase_boundary=\"none\"", result);
    }

    /// <summary>Verifies that the output, stripped of its leading comment lines, is parseable as well-formed XML.</summary>
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
