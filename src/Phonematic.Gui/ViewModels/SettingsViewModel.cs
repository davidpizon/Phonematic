using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Settings</c> tab.
/// Loads and saves <see cref="Phonematic.Models.AppConfig"/> values, reports model download
/// status, and triggers individual model downloads with progress reporting.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IModelManagerService _modelManager;

    /// <summary>Gets or sets the directory path where transcription output files are written.</summary>
    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    /// <summary>Gets or sets the currently selected Whisper model size key (e.g. <c>"tiny.en"</c>).</summary>
    [ObservableProperty]
    private string _selectedWhisperModel = "tiny.en";

    /// <summary>Gets or sets the number of CPU threads used by the Whisper processor.</summary>
    [ObservableProperty]
    private int _threadCount = 4;

    /// <summary>Gets or sets the display path to the application configuration directory.</summary>
    [ObservableProperty]
    private string _configDirectoryPath = string.Empty;

    /// <summary>Gets or sets the human-readable download/presence status of the Whisper model.</summary>
    [ObservableProperty]
    private string _whisperModelStatus = "Unknown";

    /// <summary>Gets or sets the human-readable download/presence status of the ONNX embedding model.</summary>
    [ObservableProperty]
    private string _onnxModelStatus = "Unknown";

    /// <summary>Gets or sets the human-readable download/presence status of the LLM.</summary>
    [ObservableProperty]
    private string _llmModelStatus = "Unknown";

    /// <summary>Gets or sets the status message shown below the action buttons.</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>Gets or sets the Whisper model download progress (0–1).</summary>
    [ObservableProperty]
    private double _whisperDownloadProgress;

    /// <summary>Gets or sets a value indicating whether a Whisper model download is in progress.</summary>
    [ObservableProperty]
    private bool _isDownloadingWhisper;

    /// <summary>Gets or sets the ONNX model download progress (0–1).</summary>
    [ObservableProperty]
    private double _onnxDownloadProgress;

    /// <summary>Gets or sets a value indicating whether an ONNX model download is in progress.</summary>
    [ObservableProperty]
    private bool _isDownloadingOnnx;

    /// <summary>Gets or sets the LLM download progress (0–1).</summary>
    [ObservableProperty]
    private double _llmDownloadProgress;

    /// <summary>Gets or sets a value indicating whether an LLM download is in progress.</summary>
    [ObservableProperty]
    private bool _isDownloadingLlm;

    /// <summary>Gets the maximum number of CPU threads available on this machine.</summary>
    public int MaxThreads { get; } = Environment.ProcessorCount;

    /// <summary>Gets the application version string (major.minor.patch).</summary>
    public string AppVersion { get; } =
        System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "unknown";

    /// <summary>The list of Whisper model size keys shown in the size picker.</summary>
    public ObservableCollection<string> WhisperModelSizes { get; } = new()
    {
        "tiny.en", "base.en", "small.en", "medium.en", "tiny", "base", "small", "medium", "large"
    };

    /// <summary>Initialises a new <see cref="SettingsViewModel"/> and populates all fields from the current config.</summary>
    public SettingsViewModel(IConfigService configService, IModelManagerService modelManager)
    {
        _configService = configService;
        _modelManager = modelManager;
        LoadSettings();
    }

    /// <summary>Loads the current config values into observable properties and refreshes model status.</summary>
    private void LoadSettings()
    {
        var config = _configService.Load();
        OutputDirectory = config.OutputDirectory;
        SelectedWhisperModel = config.WhisperModelSize;
        ThreadCount = config.ThreadCount;
        ConfigDirectoryPath = _configService.ConfigDirectory;
        RefreshModelStatus();
    }

    /// <summary>Refreshes model status labels when the selected Whisper model size changes.</summary>
    partial void OnSelectedWhisperModelChanged(string value)
    {
        RefreshModelStatus();
    }

    /// <summary>Updates the three model-status labels by querying <see cref="IModelManagerService"/>.</summary>
    private void RefreshModelStatus()
    {
        WhisperModelStatus = _modelManager.IsWhisperModelDownloaded(SelectedWhisperModel)
            ? "Downloaded" : "Not downloaded";
        OnnxModelStatus = _modelManager.IsOnnxModelDownloaded()
            ? "Downloaded" : "Not downloaded";
        LlmModelStatus = _modelManager.IsLlmModelDownloaded()
            ? "Downloaded" : "Not downloaded";
    }

    /// <summary>Delegate assigned by the View to open a folder-picker dialog for selecting the output directory.</summary>
    public Func<Task>? BrowseOutputDirectoryInteraction { get; set; }

    /// <summary>Invokes the folder-picker interaction to let the user choose the output directory.</summary>
    [RelayCommand]
    private async Task BrowseOutputDirectoryAsync()
    {
        if (BrowseOutputDirectoryInteraction != null)
            await BrowseOutputDirectoryInteraction();
    }

    /// <summary>Persists the current settings to disk and refreshes model status.</summary>
    [RelayCommand]
    private void Save()
    {
        var config = _configService.Load();
        config.OutputDirectory = OutputDirectory;
        config.WhisperModelSize = SelectedWhisperModel;
        config.ThreadCount = ThreadCount;
        _configService.Save(config);
        StatusText = "Settings saved.";
        RefreshModelStatus();
    }

    /// <summary>Downloads the currently selected Whisper GGML model if not already present.</summary>
    [RelayCommand]
    private async Task DownloadWhisperModelAsync(CancellationToken ct)
    {
        IsDownloadingWhisper = true;
        WhisperDownloadProgress = 0;
        StatusText = string.Empty;
        try
        {
            var progress = new Progress<double>(p => WhisperDownloadProgress = p);
            await _modelManager.DownloadWhisperModelAsync(SelectedWhisperModel, progress, ct);
            StatusText = "Whisper model downloaded.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Download cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingWhisper = false;
            RefreshModelStatus();
        }
    }

    /// <summary>Downloads the ONNX embedding model if not already present.</summary>
    [RelayCommand]
    private async Task DownloadOnnxModelAsync(CancellationToken ct)
    {
        IsDownloadingOnnx = true;
        OnnxDownloadProgress = 0;
        StatusText = string.Empty;
        try
        {
            var progress = new Progress<double>(p => OnnxDownloadProgress = p);
            await _modelManager.DownloadOnnxModelAsync(progress, ct);
            StatusText = "Embedding model downloaded.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Download cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingOnnx = false;
            RefreshModelStatus();
        }
    }

    /// <summary>Downloads the Phi-3 LLM if not already present.</summary>
    [RelayCommand]
    private async Task DownloadLlmModelAsync(CancellationToken ct)
    {
        IsDownloadingLlm = true;
        LlmDownloadProgress = 0;
        StatusText = string.Empty;
        try
        {
            var progress = new Progress<double>(p => LlmDownloadProgress = p);
            await _modelManager.DownloadLlmModelAsync(progress, ct);
            StatusText = "LLM downloaded.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Download cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingLlm = false;
            RefreshModelStatus();
        }
    }
}
