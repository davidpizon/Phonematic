using Phonematic.Services;
using Phonematic.ViewModels;

namespace Phonematic.Tests;

/// <summary>
/// Unit tests for <see cref="TrainViewModel"/>.
/// Uses a lightweight <see cref="FakeConfigService"/> test double so no real file I/O
/// or DI infrastructure is required.
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
    // LoadFiles — single file
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadFiles_PopulatesFiles_WithSupportedAudioFile()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("sample.mp3");
        try
        {
            vm.LoadFiles(path);
            Assert.Single(vm.Files);
            Assert.Equal("sample.mp3", vm.Files[0].FileName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFiles_SetsInputPath()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("test.wav");
        try
        {
            vm.LoadFiles(path);
            Assert.Equal(path, vm.InputPath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFiles_SetsFileStatusToPending()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("check.mp3");
        try
        {
            vm.LoadFiles(path);
            Assert.Equal("Pending", vm.Files[0].Status);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFiles_ClearsExistingFiles_OnSubsequentCall()
    {
        var vm = BuildViewModel();
        var path1 = CreateTempAudioFile("first.mp3");
        var path2 = CreateTempAudioFile("second.mp3");
        try
        {
            vm.LoadFiles(path1);
            vm.LoadFiles(path2);
            Assert.Single(vm.Files);
            Assert.Equal("second.mp3", vm.Files[0].FileName);
        }
        finally
        {
            File.Delete(path1);
            File.Delete(path2);
        }
    }

    [Fact]
    public void LoadFiles_DoesNothing_WhenPathDoesNotExist()
    {
        var vm = BuildViewModel();
        vm.LoadFiles(@"C:\DoesNotExist\missing.mp3");
        Assert.Empty(vm.Files);
    }

    [Fact]
    public void LoadFiles_PopulatesFiles_FromFolder()
    {
        var vm = BuildViewModel();
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        var f1 = Path.Combine(dir, "a.mp3");
        var f2 = Path.Combine(dir, "b.wav");
        try
        {
            File.WriteAllBytes(f1, []);
            File.WriteAllBytes(f2, []);
            vm.LoadFiles(dir);
            Assert.Equal(2, vm.Files.Count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadFiles_ResetsCounters()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("reset.mp3");
        try
        {
            vm.LoadFiles(path);
            Assert.Equal(0, vm.CompletedCount);
            Assert.Equal(0, vm.SkippedCount);
            Assert.Equal(0, vm.FailedCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // -------------------------------------------------------------------------
    // TrainFileItem display helper
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
        var path = CreateTempAudioFile("train.mp3");
        try
        {
            vm.LoadFiles(path);
            bool wasTrainingDuringRun = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.IsTraining) && vm.IsTraining)
                    wasTrainingDuringRun = true;
            };

            await vm.StartTrainingCommand.ExecuteAsync(null);

            Assert.True(wasTrainingDuringRun);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StartTrainingCommand_IsTrainingIsFalse_AfterCompletion()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("done.mp3");
        try
        {
            vm.LoadFiles(path);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.False(vm.IsTraining);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StartTrainingCommand_IncrementsCompletedCount()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("count.mp3");
        try
        {
            vm.LoadFiles(path);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.Equal(1, vm.CompletedCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StartTrainingCommand_SetsOverallProgressToOne_AfterCompletion()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("progress.mp3");
        try
        {
            vm.LoadFiles(path);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.Equal(1.0, vm.OverallProgress);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StartTrainingCommand_SetsFilesStatusToDone_AfterCompletion()
    {
        var vm = BuildViewModel();
        var path = CreateTempAudioFile("status.mp3");
        try
        {
            vm.LoadFiles(path);
            await vm.StartTrainingCommand.ExecuteAsync(null);
            Assert.Equal("Done", vm.Files[0].Status);
        }
        finally
        {
            File.Delete(path);
        }
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
    // BrowseFile / BrowseFolder interaction delegates
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BrowseFileCommand_InvokesInteraction_WhenAssigned()
    {
        var vm = BuildViewModel();
        bool invoked = false;
        vm.BrowseFileInteraction = () => { invoked = true; return Task.CompletedTask; };

        await vm.BrowseFileCommand.ExecuteAsync(null);

        Assert.True(invoked);
    }

    [Fact]
    public async Task BrowseFileCommand_DoesNotThrow_WhenInteractionIsNull()
    {
        var vm = BuildViewModel();
        // Should not throw
        await vm.BrowseFileCommand.ExecuteAsync(null);
    }

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

    private static TrainViewModel BuildViewModel() =>
        new(new ConfigService());

    /// <summary>Creates an empty temporary file with the given name and returns its full path.</summary>
    private static string CreateTempAudioFile(string fileName)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllBytes(path, []);
        return path;
    }
}
