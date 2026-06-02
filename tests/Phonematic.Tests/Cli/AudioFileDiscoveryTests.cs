using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>File discovery: extension filtering and top-level vs recursive enumeration.</summary>
public sealed class AudioFileDiscoveryTests : IDisposable
{
    private readonly string _dir;

    public AudioFileDiscoveryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "phonematic-discovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    [Theory]
    [InlineData("song.mp3", true)]
    [InlineData("clip.WAV", true)]   // case-insensitive
    [InlineData("voice.flac", true)]
    [InlineData("notes.txt", false)]
    [InlineData("script.phos", false)]
    public void IsSupportedAudioFile_FiltersByExtension(string name, bool expected)
    {
        Assert.Equal(expected, AudioFileDiscovery.IsSupportedAudioFile(Path.Combine(_dir, name)));
    }

    [Fact]
    public void Discover_TopLevelOnly_ExcludesSubdirectoriesAndNonAudio()
    {
        Touch("a.mp3");
        Touch("b.wav");
        Touch("ignore.txt");
        var sub = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllBytes(Path.Combine(sub, "deep.flac"), []);

        var result = AudioFileDiscovery.Discover(_dir, recursive: false);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(AudioFileDiscovery.IsSupportedAudioFile(p)));
        Assert.DoesNotContain(result, p => p.Contains("deep.flac"));
    }

    [Fact]
    public void Discover_Recursive_IncludesSubdirectories()
    {
        Touch("a.mp3");
        var sub = Path.Combine(_dir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllBytes(Path.Combine(sub, "deep.flac"), []);

        var result = AudioFileDiscovery.Discover(_dir, recursive: true);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.EndsWith("deep.flac", StringComparison.Ordinal));
    }

    [Fact]
    public void Discover_ReturnsDeterministicOrder()
    {
        Touch("c.mp3");
        Touch("a.mp3");
        Touch("b.mp3");

        var result = AudioFileDiscovery.Discover(_dir, recursive: false);

        var sorted = result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(sorted, result);
    }

    private void Touch(string name) => File.WriteAllBytes(Path.Combine(_dir, name), []);
}
