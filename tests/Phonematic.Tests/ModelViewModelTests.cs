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

    /// <summary>Verifies that the constructor populates <c>ModelName</c>, <c>ModelPath</c>, and <c>TrainedDate</c> from the active model.</summary>
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

    /// <summary>Verifies that <c>TrainedDate</c> reads "Never" when <c>LastTrainedAtUtc</c> is null.</summary>
    [Fact]
    public void Constructor_ShowsNeverTrainedDate_WhenLastTrainedAtUtcIsNull()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "blank" });
        var vm = new ModelViewModel(svc);

        Assert.Equal("Never", vm.TrainedDate);
    }

    /// <summary>Verifies that <c>ModelPath</c> is an empty string when the active model has no file path.</summary>
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

    /// <summary>Verifies that <see cref="IActiveVoiceModelService.ActiveModelChanged"/> updates the <c>ModelName</c> property.</summary>
    [Fact]
    public void ActiveModelChanged_UpdatesModelNameProperty()
    {
        var svc = new FakeActiveVoiceModelService(new VoiceModel { Name = "old" });
        var vm = new ModelViewModel(svc);

        svc.SimulateModelChange(new VoiceModel { Name = "new-name" });

        Assert.Equal("new-name", vm.ModelName);
    }

    /// <summary>Verifies that <see cref="IActiveVoiceModelService.ActiveModelChanged"/> updates the <c>ModelPath</c> property.</summary>
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

    /// <summary>Verifies that <see cref="IActiveVoiceModelService.ActiveModelChanged"/> updates the <c>TrainedDate</c> property.</summary>
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

    /// <summary>Verifies that the load command passes the picked path to <see cref="IActiveVoiceModelService.LoadFromFile"/>.</summary>
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

    /// <summary>Verifies that cancelling the file picker (returning null) does not invoke <see cref="IActiveVoiceModelService.LoadFromFile"/>.</summary>
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

    /// <summary>Verifies that a successful load sets status text containing the word "loaded".</summary>
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

    /// <summary>Verifies that a failed load sets status text containing the word "Failed".</summary>
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

    /// <summary>Verifies that the load command is a no-op when the browse interaction has not been assigned.</summary>
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

    /// <summary>Verifies that the export command passes the picked path to <see cref="IActiveVoiceModelService.ExportToFile"/>.</summary>
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

    /// <summary>Verifies that cancelling the save picker does not invoke <see cref="IActiveVoiceModelService.ExportToFile"/>.</summary>
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

    /// <summary>Verifies that a successful export sets status text containing the word "exported".</summary>
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

    /// <summary>Verifies that a failed export sets status text containing the word "Failed".</summary>
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

    /// <summary>Verifies that the export command is a no-op when the browse interaction has not been assigned.</summary>
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

    /// <summary>Creates the fake with a given initial model and optional failure switches.</summary>
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

    /// <inheritdoc/>
    public void LoadFromFile(string phonematicFilePath)
    {
        if (_throwOnLoad)
            throw new FileNotFoundException("Simulated load failure.", phonematicFilePath);

        LastLoadedPath = phonematicFilePath;
        ActiveModelChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
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
