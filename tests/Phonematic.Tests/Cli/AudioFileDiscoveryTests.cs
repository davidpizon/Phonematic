using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>File discovery: extension filtering and top-level vs recursive enumeration.</summary>
public sealed class AudioFileDiscoveryTests : IDisposable
{
    private readonly string _dir;

    /// <summary>Creates an isolated temp directory for each test run.</summary>
    public AudioFileDiscoveryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "phonematic-discovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    /// <summary>Removes the temp directory created for the test.</summary>
    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    /// <summary>Verifies that <see cref="AudioFileDiscovery.IsSupportedAudioFile"/> accepts known audio extensions and rejects others, case-insensitively.</summary>
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

    /// <summary>Non-recursive discovery returns only supported audio files in the top-level directory, ignoring subdirectories and non-audio files.</summary>
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

    /// <summary>Recursive discovery walks into subdirectories and returns all supported audio files found at any depth.</summary>
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

    /// <summary>Discovered paths are always returned in case-insensitive ordinal sort order regardless of filesystem enumeration order.</summary>
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

    /// <summary>Creates an empty file with the given <paramref name="name"/> inside the test temp directory.</summary>
    private void Touch(string name) => File.WriteAllBytes(Path.Combine(_dir, name), []);
}
