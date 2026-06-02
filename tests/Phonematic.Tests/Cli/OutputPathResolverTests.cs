using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>Output-path resolution for single-file and directory modes.</summary>
public class OutputPathResolverTests
{
    // ----- single-file -----

    [Fact]
    public void SingleOutput_Default_ReplacesExtensionWithPhos()
    {
        var result = OutputPathResolver.ResolveSingleOutput(@"C:\audio\voice.mp3", null);
        Assert.Equal(@"C:\audio\voice.phos", result);
    }

    [Fact]
    public void SingleOutput_ExplicitPath_UsedVerbatim()
    {
        var result = OutputPathResolver.ResolveSingleOutput(@"C:\audio\voice.mp3", @"D:\out\custom.phos");
        Assert.Equal(@"D:\out\custom.phos", result);
    }

    // ----- directory: no output dir (next to source) -----

    [Fact]
    public void DirectoryOutput_NoOutputDir_WritesNextToSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var source = Path.Combine(root, "voice.mp3");

        var result = OutputPathResolver.ResolveDirectoryOutput(source, root, outputDir: null, recursive: false);

        Assert.Equal(Path.Combine(root, "voice.phos"), result);
    }

    // ----- directory: flat output dir (non-recursive) -----

    [Fact]
    public void DirectoryOutput_WithOutputDir_NonRecursive_IsFlat()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var source = Path.Combine(root, "voice.mp3");
        var outDir = Path.Combine(Path.GetTempPath(), "out");

        var result = OutputPathResolver.ResolveDirectoryOutput(source, root, outDir, recursive: false);

        Assert.Equal(Path.Combine(outDir, "voice.phos"), result);
    }

    // ----- directory: recursive subfolder mirroring -----

    [Fact]
    public void DirectoryOutput_Recursive_MirrorsRelativeSubfolders()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var source = Path.Combine(root, "a", "b", "voice.mp3");
        var outDir = Path.Combine(Path.GetTempPath(), "out");

        var result = OutputPathResolver.ResolveDirectoryOutput(source, root, outDir, recursive: true);

        Assert.Equal(Path.Combine(outDir, "a", "b", "voice.phos"), result);
    }

    [Fact]
    public void DirectoryOutput_Recursive_RootLevelFile_HasNoExtraSubfolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var source = Path.Combine(root, "voice.mp3");
        var outDir = Path.Combine(Path.GetTempPath(), "out");

        var result = OutputPathResolver.ResolveDirectoryOutput(source, root, outDir, recursive: true);

        Assert.Equal(Path.Combine(outDir, "voice.phos"), result);
    }
}
