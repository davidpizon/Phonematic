using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Model</c> tab.
/// Exposes the currently active voice model's display properties and the
/// <see cref="LoadCommand"/> / <see cref="ExportCommand"/> relay commands.
/// File-picker dialogs are decoupled from the ViewModel via
/// <see cref="BrowseLoadFileInteraction"/> and <see cref="BrowseSaveFileInteraction"/>
/// — the View assigns these delegates in code-behind, keeping the ViewModel
/// independently testable without any UI dependencies.
/// </summary>
public partial class ModelViewModel : ViewModelBase
{
    private readonly IActiveVoiceModelService _activeVoiceModelService;

    /// <summary>
    /// Initialises a new instance of <see cref="ModelViewModel"/>.
    /// </summary>
    /// <param name="activeVoiceModelService">The application-wide active model service.</param>
    public ModelViewModel(IActiveVoiceModelService activeVoiceModelService)
    {
        _activeVoiceModelService = activeVoiceModelService;
        _activeVoiceModelService.ActiveModelChanged += OnActiveModelChanged;
        RefreshFromActiveModel();
    }

    // ---------------------------------------------------------------------------
    // Observable properties
    // ---------------------------------------------------------------------------

    /// <summary>Gets or sets the human-readable name of the active voice model.</summary>
    [ObservableProperty]
    private string _modelName = string.Empty;

    /// <summary>Gets or sets the file-system path of the active model's <c>.phonematic</c> artefact, or an empty string when no file is loaded.</summary>
    [ObservableProperty]
    private string _modelPath = string.Empty;

    /// <summary>Gets or sets the UTC trained date formatted for display, or <c>"Never"</c> when the model has not been trained.</summary>
    [ObservableProperty]
    private string _trainedDate = string.Empty;

    /// <summary>Gets or sets the status text shown below the action buttons.</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    // ---------------------------------------------------------------------------
    // Interaction delegates (assigned by the View's code-behind)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Delegate invoked by <see cref="LoadCommand"/> to open a file-picker dialog.
    /// The View assigns this in code-behind.
    /// Returns the chosen file path, or <see langword="null"/> when the user cancels.
    /// </summary>
    public Func<Task<string?>>? BrowseLoadFileInteraction { get; set; }

    /// <summary>
    /// Delegate invoked by <see cref="ExportCommand"/> to open a save-file dialog.
    /// The View assigns this in code-behind.
    /// Returns the chosen file path, or <see langword="null"/> when the user cancels.
    /// </summary>
    public Func<Task<string?>>? BrowseSaveFileInteraction { get; set; }

    // ---------------------------------------------------------------------------
    // Commands
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Opens a file-picker for the user to choose a <c>.phonematic</c> file.
    /// The selected file becomes the new active voice model.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (BrowseLoadFileInteraction is null)
            return;

        var path = await BrowseLoadFileInteraction();
        if (path is null)
            return;

        try
        {
            _activeVoiceModelService.LoadFromFile(path);
            StatusText = $"Model \"{ModelName}\" loaded successfully.";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load model: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens a save-file dialog for the user to choose where to export the active model.
    /// The active model's <c>.phonematic</c> artefact is copied to the chosen path.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (BrowseSaveFileInteraction is null)
            return;

        var path = await BrowseSaveFileInteraction();
        if (path is null)
            return;

        try
        {
            _activeVoiceModelService.ExportToFile(path);
            StatusText = $"Model exported to \"{path}\".";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to export model: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private void OnActiveModelChanged(object? sender, EventArgs e) => RefreshFromActiveModel();

    private void RefreshFromActiveModel()
    {
        var model = _activeVoiceModelService.ActiveModel;
        ModelName = model.Name;
        ModelPath = model.ModelPath ?? string.Empty;
        TrainedDate = model.LastTrainedAtUtc.HasValue
            ? model.LastTrainedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "Never";
    }
}
