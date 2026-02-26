using CommunityToolkit.Mvvm.ComponentModel;

namespace Transcriptonator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private bool _isSetupRequired;

    [ObservableProperty]
    private int _selectedTabIndex;

    public TranscribeViewModel? Transcribe { get; set; }
    public TranscriptionsViewModel? Transcriptions { get; set; }
    public SearchViewModel? Search { get; set; }
    public SettingsViewModel? Settings { get; set; }
    public SetupViewModel? Setup { get; set; }
}
