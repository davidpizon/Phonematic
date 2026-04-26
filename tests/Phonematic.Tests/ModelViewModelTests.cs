using Phonematic.Models;
using Phonematic.Services;
using Phonematic.ViewModels;

namespace Phonematic.Tests;

/// <summary>
/// Unit tests for <see cref="ModelViewModel"/>.
/// Uses a test double (<see cref="FakeActiveVoiceModelService"/>) so that no real
/// file I/O or DI infrastructure is needed.
/// </summary>
public class ModelViewModelTests
{
    // -------------------------------------------------------------------------
    // Construction / initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_PopulatesPropertiesFromActiveModel()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel
        {
            Name = "test-model",
            ModelPath = @"C:\models\test-model.phonematic",
            LastTrainedAtUtc = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        });

        var vm = new ModelViewModel(svc);

        Assert.Equal("test-model", vm.ModelName);
        Assert.Equal(@"C:\models\test-model.phonematic", vm.ModelPath);
        Assert.NotEqual("Never", vm.TrainedDate);
    }

    [Fact]
    public void Constructor_ShowsNeverTrainedDate_WhenLastTrainedAtUtcIsNull()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "blank" });
        var vm = new ModelViewModel(svc);

        Assert.Equal("Never", vm.TrainedDate);
    }

    [Fact]
    public void Constructor_ModelPathIsEmpty_WhenNoFilePath()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "blank" });
        var vm = new ModelViewModel(svc);

        Assert.Equal(string.Empty, vm.ModelPath);
    }

    // -------------------------------------------------------------------------
    // ActiveModelChanged event → ViewModel refreshes
    // -------------------------------------------------------------------------

    [Fact]
    public void ActiveModelChanged_UpdatesModelNameProperty()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "old" });
        var vm = new ModelViewModel(svc);

        svc.SimulateModelChange(new VoiceModel { Name = "new-name" });

        Assert.Equal("new-name", vm.ModelName);
    }

    [Fact]
    public void ActiveModelChanged_UpdatesModelPathProperty()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "old" });
        var vm = new ModelViewModel(svc);

        svc.SimulateModelChange(new VoiceModel
        {
            Name = "updated",
            ModelPath = @"C:\new\path.phonematic",
        });

        Assert.Equal(@"C:\new\path.phonematic", vm.ModelPath);
    }

    [Fact]
    public void ActiveModelChanged_UpdatesTrainedDateProperty()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "old" });
        var vm = new ModelViewModel(svc);

        svc.SimulateModelChange(new VoiceModel
        {
            Name = "trained",
            LastTrainedAtUtc = new DateTime(2025, 1, 15, 8, 0, 0, DateTimeKind.Utc),
        });

        Assert.NotEqual("Never", vm.TrainedDate);
    }

    // -------------------------------------------------------------------------
    // LoadCommand
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadCommand_CallsLoadFromFile_WithPickedPath()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc)
        {
            BrowseLoadFileInteraction = () => Task.FromResult<string?>(@"C:\picked\voice.phonematic"),
        };

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\picked\voice.phonematic", svc.LastLoadedPath);
    }

    [Fact]
    public async Task LoadCommand_DoesNotCallLoadFromFile_WhenUserCancels()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc)
        {
            BrowseLoadFileInteraction = () => Task.FromResult<string?>(null),
        };

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Null(svc.LastLoadedPath);
    }

    [Fact]
    public async Task LoadCommand_SetsStatusText_OnSuccess()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "loaded-name" });
        var vm = new ModelViewModel(svc)
        {
            BrowseLoadFileInteraction = () => Task.FromResult<string?>(@"C:\x\loaded-name.phonematic"),
        };

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Contains("loaded", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadCommand_SetsStatusText_OnError()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel(), throwOnLoad: true);
        var vm = new ModelViewModel(svc)
        {
            BrowseLoadFileInteraction = () => Task.FromResult<string?>(@"C:\bad\path.phonematic"),
        };

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Contains("Failed", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadCommand_DoesNothing_WhenInteractionNotAssigned()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc);  // BrowseLoadFileInteraction is null

        // Should not throw
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Null(svc.LastLoadedPath);
    }

    // -------------------------------------------------------------------------
    // ExportCommand
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExportCommand_CallsExportToFile_WithPickedPath()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc)
        {
            BrowseSaveFileInteraction = () => Task.FromResult<string?>(@"C:\out\export.phonematic"),
        };

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\out\export.phonematic", svc.LastExportedPath);
    }

    [Fact]
    public async Task ExportCommand_DoesNotCallExportToFile_WhenUserCancels()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc)
        {
            BrowseSaveFileInteraction = () => Task.FromResult<string?>(null),
        };

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Null(svc.LastExportedPath);
    }

    [Fact]
    public async Task ExportCommand_SetsStatusText_OnSuccess()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc)
        {
            BrowseSaveFileInteraction = () => Task.FromResult<string?>(@"C:\out\export.phonematic"),
        };

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Contains("exported", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportCommand_SetsStatusText_OnError()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel(), throwOnExport: true);
        var vm = new ModelViewModel(svc)
        {
            BrowseSaveFileInteraction = () => Task.FromResult<string?>(@"C:\out\export.phonematic"),
        };

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Contains("Failed", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportCommand_DoesNothing_WhenInteractionNotAssigned()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel());
        var vm = new ModelViewModel(svc);  // BrowseSaveFileInteraction is null

        // Should not throw
        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Null(svc.LastExportedPath);
    }
}

// =============================================================================
// Test double
// =============================================================================

/// <summary>
/// In-memory test double for <see cref="IActiveVoiceModelService"/> that records
/// every call made to <see cref="LoadFromFile"/> and <see cref="ExportToFile"/>.
/// </summary>
internal sealed class FakeActiveVoiceModelService : IActiveVoiceModelService
{
    private readonly bool _throwOnLoad;
    private readonly bool _throwOnExport;

    public FakeActiveVoiceModelService(
        VoiceModel initial,
        bool throwOnLoad = false,
        bool throwOnExport = false)
    {
        ActiveModel = initial;
        _throwOnLoad = throwOnLoad;
        _throwOnExport = throwOnExport;
    }

    public VoiceModel ActiveModel { get; private set; }

    public event EventHandler? ActiveModelChanged;

    /// <summary>Records the last path passed to <see cref="LoadFromFile"/>.</summary>
    public string? LastLoadedPath { get; private set; }

    /// <summary>Records the last path passed to <see cref="ExportToFile"/>.</summary>
    public string? LastExportedPath { get; private set; }

    public void LoadFromFile(string phonematicFilePath)
    {
        if (_throwOnLoad)
            throw new FileNotFoundException("Simulated load failure.", phonematicFilePath);

        LastLoadedPath = phonematicFilePath;
        ActiveModelChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ExportToFile(string destinationPath)
    {
        if (_throwOnExport)
            throw new InvalidOperationException("Simulated export failure.");

        LastExportedPath = destinationPath;
    }

    /// <summary>
    /// Replaces <see cref="ActiveModel"/> and fires <see cref="ActiveModelChanged"/>
    /// so that subscribed ViewModels (under test) react as they would at runtime.
    /// </summary>
    public void SimulateModelChange(VoiceModel newModel)
    {
        ActiveModel = newModel;
        ActiveModelChanged?.Invoke(this, EventArgs.Empty);
    }
}
