using CommunityToolkit.Mvvm.ComponentModel;

namespace Phonematic.ViewModels;

/// <summary>
/// Shell ViewModel for <c>MainWindow</c>. Holds references to all tab ViewModels and
/// controls whether the first-run setup overlay (<see cref="Setup"/>) is shown instead of
/// the main tab strip. All child ViewModels are assigned by <c>App.OnFrameworkInitializationCompleted</c>
/// after the DI container is built.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>Gets or sets the ViewModel currently displayed in the content area (reserved for future navigation use).</summary>
    [ObservableProperty]
    private ViewModelBase? _currentView;

    /// <summary>
    /// Gets or sets a value indicating whether the first-run setup overlay should be shown.
    /// Set to <see langword="true"/> when <see cref="Phonematic.Services.IModelManagerService.AreAllModelsReady"/> returns
    /// <see langword="false"/>, and reset to <see langword="false"/> by the setup completion callback.
    /// </summary>
    [ObservableProperty]
    private bool _isSetupRequired;

    /// <summary>Gets or sets the zero-based index of the currently selected main tab.</summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>Gets or sets the ViewModel for the Transcribe tab.</summary>
    public TranscribeViewModel? Transcribe { get; set; }

    /// <summary>Gets or sets the ViewModel for the Transcriptions history tab.</summary>
    public TranscriptionsViewModel? Transcriptions { get; set; }

    /// <summary>Gets or sets the ViewModel for the Train tab.</summary>
    public TrainViewModel? Train { get; set; }

    /// <summary>Gets or sets the ViewModel for the Search / RAG tab.</summary>
    public SearchViewModel? Search { get; set; }

    /// <summary>Gets or sets the ViewModel for the Settings tab.</summary>
    public SettingsViewModel? Settings { get; set; }

    /// <summary>Gets or sets the ViewModel for the Model tab.</summary>
    public ModelViewModel? Model { get; set; }

    /// <summary>Gets or sets the ViewModel for the PLAUD Sync tab.</summary>
    public PlaudSyncViewModel? PlaudSync { get; set; }

    /// <summary>
    /// Gets or sets the ViewModel for the first-run setup wizard.
    /// Only non-<see langword="null"/> while <see cref="IsSetupRequired"/> is <see langword="true"/>.
    /// </summary>
    public SetupViewModel? Setup { get; set; }
}
