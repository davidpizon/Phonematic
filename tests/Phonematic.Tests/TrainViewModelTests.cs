using Phonematic.Services;
using Phonematic.ViewModels;

namespace Phonematic.Tests;

/// <summary>
/// Unit tests for <see cref="TrainViewModel"/> and <see cref="TrainFileItem"/>.
/// Uses <see cref="ConfigService"/> directly; all file I/O is performed against
/// temporary directories/files that are cleaned up in <c>finally</c> blocks.
/// </summary>
public class TrainViewModelTests
{
    // -------------------------------------------------------------------------
    // Construction / initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_StatusTextIsReady()
    {
        var vm = BuildViewModel();
        Assert.Equal("Ready", vm.StatusText);
    }

    [Fact]
    public void Constructor_FilesCollectionIsEmpty_WhenNoLastImportPath()
    {
        var vm = BuildViewModel();
        Assert.Empty(vm.Files);
    }

    [Fact]
    public void Constructor_IsTrainingIsFalse()
    {
        var vm = BuildViewModel();
        Assert.False(vm.IsTraining);
    }

    [Fact]
    public void Constructor_CountersAreZero()
    {
        var vm = BuildViewModel();
        Assert.Equal(0, vm.CompletedCount);
        Assert.Equal(0, vm.SkippedCount);
        Assert.Equal(0, vm.FailedCount);
    }

    // -------------------------------------------------------------------------
    // LoadInputSets — folder scanning
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadInputSets_PopulatesFiles_WithSupportedAudioFile()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "sample.mp3");
            vm.LoadInputSets(dir);
            Assert.Single(vm.Files);
            Assert.Equal("sample", vm.Files[0].FileName);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_SetsInputPath()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "test.wav");
            vm.LoadInputSets(dir);
            Assert.Equal(dir, vm.InputPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_SetsFileStatusToPending()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "check.mp3");
            vm.LoadInputSets(dir);
            Assert.Equal("Pending", vm.Files[0].Status);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_ClearsExistingFiles_OnSubsequentCall()
    {
        var vm = BuildViewModel();
        var dir1 = CreateTempDir();
        var dir2 = CreateTempDir();
        try
        {
            CreateFile(dir1, "first.mp3");
            CreateFile(dir2, "second.mp3");
            vm.LoadInputSets(dir1);
            vm.LoadInputSets(dir2);
            Assert.Single(vm.Files);
            Assert.Equal("second", vm.Files[0].FileName);
        }
        finally
        {
            Directory.Delete(dir1, recursive: true);
            Directory.Delete(dir2, recursive: true);
        }
    }

    [Fact]
    public void LoadInputSets_DoesNothing_WhenPathDoesNotExist()
    {
        var vm = BuildViewModel();
        vm.LoadInputSets(@"C:\DoesNotExist\missing");
        Assert.Empty(vm.Files);
    }

    [Fact]
    public void LoadInputSets_PopulatesMultipleFiles_FromFolder()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "a.mp3");
            CreateFile(dir, "b.wav");
            vm.LoadInputSets(dir);
            Assert.Equal(2, vm.Files.Count);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_ResetsCounters()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "reset.mp3");
            vm.LoadInputSets(dir);
            Assert.Equal(0, vm.CompletedCount);
            Assert.Equal(0, vm.SkippedCount);
            Assert.Equal(0, vm.FailedCount);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_IgnoresNonAudioAndNonPhosFiles()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "notes.txt");
            CreateFile(dir, "image.png");
            vm.LoadInputSets(dir);
            Assert.Empty(vm.Files);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_SetsAudioPath_ToAudioFilePath()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            var audioPath = CreateFile(dir, "voice.mp3");
            vm.LoadInputSets(dir);
            Assert.Equal(audioPath, vm.Files[0].AudioPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // LoadInputSets — .phos companion detection
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadInputSets_SetsTranscriptionPath_WhenPhosFileExists()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "voice.mp3");
            var phosPath = CreateFile(dir, "voice.phos");
            vm.LoadInputSets(dir);
            Assert.Equal(phosPath, vm.Files[0].TranscriptionPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_SetsTranscriptionPathToEmpty_WhenNoPhosFile()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "voice.mp3");
            vm.LoadInputSets(dir);
            Assert.Equal(string.Empty, vm.Files[0].TranscriptionPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_CreatesItem_ForPhosFileWithoutAudio()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "orphan.phos");
            vm.LoadInputSets(dir);
            Assert.Single(vm.Files);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_PhosOnlyItem_HasEmptyAudioPath()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "orphan.phos");
            vm.LoadInputSets(dir);
            Assert.Equal(string.Empty, vm.Files[0].AudioPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_PhosOnlyItem_HasTranscriptionPath()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            var phosPath = CreateFile(dir, "orphan.phos");
            vm.LoadInputSets(dir);
            Assert.Equal(phosPath, vm.Files[0].TranscriptionPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_PhosOnlyItem_ShowsPhosFileName()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "orphan.phos");
            vm.LoadInputSets(dir);
            Assert.Equal("orphan", vm.Files[0].FileName);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_AudioAndPhosSameStem_ProducesSingleRow()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "voice.mp3");
            CreateFile(dir, "voice.phos");
            vm.LoadInputSets(dir);
            Assert.Single(vm.Files);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_MixedStems_ProducesCorrectRowCount()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "a.mp3");   // has audio only
            CreateFile(dir, "b.mp3");   // has audio + phos
            CreateFile(dir, "b.phos");
            CreateFile(dir, "c.phos");  // has phos only
            vm.LoadInputSets(dir);
            Assert.Equal(3, vm.Files.Count);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void LoadInputSets_MatchesPhosFile_BySameBaseNameInSameDirectory()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "a.mp3");
            CreateFile(dir, "a.phos");
            CreateFile(dir, "b.wav");   // no b.phos
            vm.LoadInputSets(dir);

            var itemA = vm.Files.Single(f => f.FileName == "a");
            var itemB = vm.Files.Single(f => f.FileName == "b");

            Assert.NotEmpty(itemA.TranscriptionPath);
            Assert.Empty(itemB.TranscriptionPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // TrainFileItem — AudioExtension
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"C:\audio\voice.mp3", ".mp3")]
    [InlineData(@"C:\audio\voice.wav", ".wav")]
    [InlineData(@"C:\audio\voice.FLAC", ".FLAC")]
    public void TrainFileItem_AudioExtension_ReturnsFileExtension(string path, string expected)
    {
        var item = new TrainFileItem { AudioPath = path };
        Assert.Equal(expected, item.AudioExtension);
    }

    [Fact]
    public void TrainFileItem_AudioExtension_IsEmpty_WhenAudioPathIsEmpty()
    {
        var item = new TrainFileItem();
        Assert.Equal(string.Empty, item.AudioExtension);
    }

    [Fact]
    public void TrainFileItem_AudioExtension_RaisesPropertyChanged_WhenAudioPathChanges()
    {
        var item = new TrainFileItem();
        var raised = false;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(item.AudioExtension)) raised = true;
        };

        item.AudioPath = @"C:\audio\new.wav";

        Assert.True(raised);
    }

    // -------------------------------------------------------------------------
    // TrainFileItem — TranscriptionStatus
    // -------------------------------------------------------------------------

    [Fact]
    public void TrainFileItem_TranscriptionStatus_IsOk_WhenTranscriptionPathIsSet()
    {
        var item = new TrainFileItem { TranscriptionPath = @"C:\data\voice.phos" };
        Assert.Equal("OK", item.TranscriptionStatus);
    }

    [Fact]
    public void TrainFileItem_TranscriptionStatus_IsEmpty_WhenTranscriptionPathIsEmpty()
    {
        var item = new TrainFileItem();
        Assert.Equal(string.Empty, item.TranscriptionStatus);
    }

    [Fact]
    public void TrainFileItem_TranscriptionStatus_RaisesPropertyChanged_WhenTranscriptionPathChanges()
    {
        var item = new TrainFileItem();
        var raised = false;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(item.TranscriptionStatus)) raised = true;
        };

        item.TranscriptionPath = @"C:\data\voice.phos";

        Assert.True(raised);
    }

    // -------------------------------------------------------------------------
    // TrainFileItem — FileSizeDisplay
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2.0 KB")]
    [InlineData(2097152, "2.0 MB")]
    public void TrainFileItem_FileSizeDisplay_FormatsCorrectly(long bytes, string expected)
    {
        var item = new TrainFileItem { FileSizeBytes = bytes };
        Assert.Equal(expected, item.FileSizeDisplay);
    }

    // -------------------------------------------------------------------------
    // StartTrainingCommand
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartTrainingCommand_SetsIsTrainingDuringExecution()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "train.mp3");
            vm.LoadInputSets(dir);
            bool wasTrainingDuringRun = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.IsTraining) && vm.IsTraining)
                    wasTrainingDuringRun = true;
            };

            await vm.StartTrainingCommand.ExecuteAsync(null);

            Assert.True(wasTrainingDuringRun);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task StartTrainingCommand_IsTrainingIsFalse_AfterCompletion()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "done.mp3");
            vm.LoadInputSets(dir);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.False(vm.IsTraining);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task StartTrainingCommand_IncrementsCompletedCount()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "count.mp3");
            vm.LoadInputSets(dir);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.Equal(1, vm.CompletedCount);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task StartTrainingCommand_SetsOverallProgressToOne_AfterCompletion()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "progress.mp3");
            vm.LoadInputSets(dir);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.Equal(1.0, vm.OverallProgress);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task StartTrainingCommand_SetsFilesStatusToDone_AfterCompletion()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "status.mp3");
            vm.LoadInputSets(dir);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.Equal("Done", vm.Files[0].Status);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task StartTrainingCommand_DoesNothing_WhenFilesIsEmpty()
    {
        var vm = BuildViewModel();
        await vm.StartTrainingCommand.ExecuteAsync(null);
        Assert.Equal(0, vm.CompletedCount);
        Assert.False(vm.IsTraining);
    }

    // -------------------------------------------------------------------------
    // BrowseFolder interaction delegate
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BrowseFolderCommand_InvokesInteraction_WhenAssigned()
    {
        var vm = BuildViewModel();
        bool invoked = false;
        vm.BrowseFolderInteraction = () => { invoked = true; return Task.CompletedTask; };

        await vm.BrowseFolderCommand.ExecuteAsync(null);

        Assert.True(invoked);
    }

    [Fact]
    public async Task BrowseFolderCommand_DoesNotThrow_WhenInteractionIsNull()
    {
        var vm = BuildViewModel();
        // Should not throw
        await vm.BrowseFolderCommand.ExecuteAsync(null);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TrainViewModel BuildViewModel() => new(new ConfigService());

    /// <summary>Creates a uniquely-named temporary directory and returns its path.</summary>
    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Creates an empty file named <paramref name="fileName"/> inside <paramref name="dir"/>
    /// and returns its full path.
    /// </summary>
    private static string CreateFile(string dir, string fileName)
    {
        var path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, []);
        return path;
    }
}

