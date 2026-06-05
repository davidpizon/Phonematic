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

    /// <summary>Verifies that <c>StatusText</c> is "Ready" after construction.</summary>
    [Fact]
    public void Constructor_StatusTextIsReady()
    {
        var vm = BuildViewModel();
        Assert.Equal("Ready", vm.StatusText);
    }

    /// <summary>Verifies that <c>Files</c> is empty when no last import path exists.</summary>
    [Fact]
    public void Constructor_FilesCollectionIsEmpty_WhenNoLastImportPath()
    {
        var vm = BuildViewModel();
        Assert.Empty(vm.Files);
    }

    /// <summary>Verifies that <c>IsTraining</c> is false after construction.</summary>
    [Fact]
    public void Constructor_IsTrainingIsFalse()
    {
        var vm = BuildViewModel();
        Assert.False(vm.IsTraining);
    }

    /// <summary>Verifies that all counters are zero after construction.</summary>
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

    /// <summary>Verifies that a supported audio file in the folder is added to <c>Files</c>.</summary>
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
            Assert.Equal("sample", vm.Files[0].Name);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>Verifies that <c>InputPath</c> is set to the scanned folder path.</summary>
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

    /// <summary>Verifies that newly loaded files have status "Pending".</summary>
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

    /// <summary>Verifies that a second call to <c>LoadInputSets</c> replaces the previous file list.</summary>
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
            Assert.Equal("second", vm.Files[0].Name);
        }
        finally
        {
            Directory.Delete(dir1, recursive: true);
            Directory.Delete(dir2, recursive: true);
        }
    }

    /// <summary>Verifies that a non-existent path leaves <c>Files</c> empty.</summary>
    [Fact]
    public void LoadInputSets_DoesNothing_WhenPathDoesNotExist()
    {
        var vm = BuildViewModel();
        vm.LoadInputSets(@"C:\DoesNotExist\missing");
        Assert.Empty(vm.Files);
    }

    /// <summary>Verifies that all audio files in the folder are added to <c>Files</c>.</summary>
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

    /// <summary>Verifies that all counters are reset to zero on each call to <c>LoadInputSets</c>.</summary>
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

    /// <summary>Verifies that an empty directory leaves <c>Files</c> empty.</summary>
    [Fact]
    public void LoadInputSets_LeavesFilesEmpty_WhenDirectoryContainsNoAudioOrPhosFiles()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            vm.LoadInputSets(dir);
            Assert.Empty(vm.Files);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>Verifies that non-audio and non-phos files are ignored during scanning.</summary>
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

    /// <summary>Verifies that <c>AudioPath</c> on a file item points to the discovered audio file.</summary>
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

    /// <summary>Verifies that a .phos file matching the audio stem sets <c>TranscriptionPath</c>.</summary>
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

    /// <summary>Verifies that <c>TranscriptionPath</c> is empty when no matching .phos file exists.</summary>
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

    /// <summary>Verifies that a .phos file without a matching audio file still creates a row.</summary>
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

    /// <summary>Verifies that a phos-only row has an empty <c>AudioPath</c>.</summary>
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

    /// <summary>Verifies that a phos-only row has its <c>TranscriptionPath</c> set to the .phos file.</summary>
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

    /// <summary>Verifies that the display name of a phos-only row is the stem of the .phos filename.</summary>
    [Fact]
    public void LoadInputSets_PhosOnlyItem_ShowsPhosFileName()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "orphan.phos");
            vm.LoadInputSets(dir);
            Assert.Equal("orphan", vm.Files[0].Name);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>Verifies that an audio file and a .phos file with the same stem produce only one row.</summary>
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

    /// <summary>Verifies that mixed audio-only, paired, and phos-only files produce the correct total row count.</summary>
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

    /// <summary>Verifies that phos-matching only applies to same-stem entries in the same directory.</summary>
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

            var itemA = vm.Files.Single(f => f.Name == "a");
            var itemB = vm.Files.Single(f => f.Name == "b");

            Assert.NotEmpty(itemA.TranscriptionPath);
            Assert.Empty(itemB.TranscriptionPath);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // TrainFileItem — AudioExtension
    // -------------------------------------------------------------------------

    /// <summary>Verifies that <c>AudioExtension</c> returns the file extension of <c>AudioPath</c>.</summary>
    [Theory]
    [InlineData(@"C:\audio\voice.mp3", ".mp3")]
    [InlineData(@"C:\audio\voice.wav", ".wav")]
    [InlineData(@"C:\audio\voice.FLAC", ".FLAC")]
    public void TrainFileItem_AudioExtension_ReturnsFileExtension(string path, string expected)
    {
        var item = new TrainFileItem { AudioPath = path };
        Assert.Equal(expected, item.AudioExtension);
    }

    /// <summary>Verifies that <c>AudioExtension</c> is empty when <c>AudioPath</c> is empty.</summary>
    [Fact]
    public void TrainFileItem_AudioExtension_IsEmpty_WhenAudioPathIsEmpty()
    {
        var item = new TrainFileItem();
        Assert.Equal(string.Empty, item.AudioExtension);
    }

    /// <summary>Verifies that <c>AudioExtension</c> raises <c>PropertyChanged</c> when <c>AudioPath</c> changes.</summary>
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

    /// <summary>Verifies that <c>TranscriptionStatus</c> is "OK" when a transcription path is set.</summary>
    [Fact]
    public void TrainFileItem_TranscriptionStatus_IsOk_WhenTranscriptionPathIsSet()
    {
        var item = new TrainFileItem { TranscriptionPath = @"C:\data\voice.phos" };
        Assert.Equal("OK", item.TranscriptionStatus);
    }

    /// <summary>Verifies that <c>TranscriptionStatus</c> is empty when no transcription path is set.</summary>
    [Fact]
    public void TrainFileItem_TranscriptionStatus_IsEmpty_WhenTranscriptionPathIsEmpty()
    {
        var item = new TrainFileItem();
        Assert.Equal(string.Empty, item.TranscriptionStatus);
    }

    /// <summary>Verifies that <c>TranscriptionStatus</c> raises <c>PropertyChanged</c> when <c>TranscriptionPath</c> changes.</summary>
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
    // StartTrainingCommand
    // -------------------------------------------------------------------------

    /// <summary>Verifies that <c>IsTraining</c> is true while the training command is executing.</summary>
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

    /// <summary>Verifies that <c>IsTraining</c> is false once training completes.</summary>
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

    /// <summary>Verifies that <c>CompletedCount</c> is incremented after a successful training run.</summary>
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

    /// <summary>Verifies that <c>OverallProgress</c> reaches 1.0 after all files are processed.</summary>
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

    /// <summary>Verifies that file status is set to "Done" after processing completes.</summary>
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

    /// <summary>Verifies that the training command is a no-op when no files are loaded.</summary>
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

    /// <summary>Verifies that the browse-folder command invokes the assigned interaction delegate.</summary>
    [Fact]
    public async Task BrowseFolderCommand_InvokesInteraction_WhenAssigned()
    {
        var vm = BuildViewModel();
        bool invoked = false;
        vm.BrowseFolderInteraction = () => { invoked = true; return Task.CompletedTask; };

        await vm.BrowseFolderCommand.ExecuteAsync(null);

        Assert.True(invoked);
    }

    /// <summary>Verifies that the browse-folder command does not throw when no interaction delegate is assigned.</summary>
    [Fact]
    public async Task BrowseFolderCommand_DoesNotThrow_WhenInteractionIsNull()
    {
        var vm = BuildViewModel();
        // Should not throw
        await vm.BrowseFolderCommand.ExecuteAsync(null);
    }

    /// <summary>Verifies that <c>Files</c> is cleared before the browse dialog opens.</summary>
    [Fact]
    public async Task BrowseFolderCommand_ClearsFilesBeforeInteraction()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            // Pre-populate the list so there is something to clear.
            CreateFile(dir, "existing.mp3");
            vm.LoadInputSets(dir);
            Assert.NotEmpty(vm.Files);

            bool listWasEmptyWhenDialogOpened = false;
            vm.BrowseFolderInteraction = () =>
            {
                listWasEmptyWhenDialogOpened = vm.Files.Count == 0;
                return Task.CompletedTask;
            };

            await vm.BrowseFolderCommand.ExecuteAsync(null);

            Assert.True(listWasEmptyWhenDialogOpened);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>Verifies that <c>InputPath</c> is cleared before the browse dialog opens.</summary>
    [Fact]
    public async Task BrowseFolderCommand_ClearsInputPathBeforeInteraction()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "existing.mp3");
            vm.LoadInputSets(dir);
            Assert.NotEmpty(vm.InputPath);

            bool inputPathWasEmptyWhenDialogOpened = false;
            vm.BrowseFolderInteraction = () =>
            {
                inputPathWasEmptyWhenDialogOpened = string.IsNullOrEmpty(vm.InputPath);
                return Task.CompletedTask;
            };

            await vm.BrowseFolderCommand.ExecuteAsync(null);

            Assert.True(inputPathWasEmptyWhenDialogOpened);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>Verifies that <c>Files</c> remains empty when the user cancels the browse dialog.</summary>
    [Fact]
    public async Task BrowseFolderCommand_LeavesFilesEmpty_WhenUserCancels()
    {
        var vm = BuildViewModel();
        var dir = CreateTempDir();
        try
        {
            CreateFile(dir, "existing.mp3");
            vm.LoadInputSets(dir);

            // Simulate cancel: interaction completes without calling LoadInputSets.
            vm.BrowseFolderInteraction = () => Task.CompletedTask;

            await vm.BrowseFolderCommand.ExecuteAsync(null);

            Assert.Empty(vm.Files);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates a default <see cref="TrainViewModel"/> for use in tests.</summary>
    private static TrainViewModel BuildViewModel() => new();

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

