using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>Output-path resolution for single-file and directory modes.</summary>
public class OutputPathResolverTests
{
    // ----- single-file -----

    /// <summary>Verifies that the default single-file output replaces the source extension with <c>.phos</c>.</summary>
    [Fact]
    public void SingleOutput_Default_ReplacesExtensionWithPhos()
    {
        var result = OutputPathResolver.ResolveSingleOutput(@"C:\audio\voice.mp3", null);
        Assert.Equal(@"C:\audio\voice.phos", result);
    }

    /// <summary>Verifies that an explicitly supplied path is returned verbatim.</summary>
    [Fact]
    public void SingleOutput_ExplicitPath_UsedVerbatim()
    {
        var result = OutputPathResolver.ResolveSingleOutput(@"C:\audio\voice.mp3", @"D:\out\custom.phos");
        Assert.Equal(@"D:\out\custom.phos", result);
    }

    // ----- directory: no output dir (next to source) -----

    /// <summary>Verifies that when no output directory is specified the .phos file is placed next to the source.</summary>
    [Fact]
    public void DirectoryOutput_NoOutputDir_WritesNextToSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var source = Path.Combine(root, "voice.mp3");

        var result = OutputPathResolver.ResolveDirectoryOutput(source, root, outputDir: null, recursive: false);

        Assert.Equal(Path.Combine(root, "voice.phos"), result);
    }

    // ----- directory: flat output dir (non-recursive) -----

    /// <summary>Verifies that non-recursive mode places all output files flat in the output directory.</summary>
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

    /// <summary>Verifies that recursive mode mirrors the source subfolder structure under the output directory.</summary>
    [Fact]
    public void DirectoryOutput_Recursive_MirrorsRelativeSubfolders()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var source = Path.Combine(root, "a", "b", "voice.mp3");
        var outDir = Path.Combine(Path.GetTempPath(), "out");

        var result = OutputPathResolver.ResolveDirectoryOutput(source, root, outDir, recursive: true);

        Assert.Equal(Path.Combine(outDir, "a", "b", "voice.phos"), result);
    }

    /// <summary>Verifies that a root-level file in recursive mode is placed directly in the output directory without an extra subfolder.</summary>
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
