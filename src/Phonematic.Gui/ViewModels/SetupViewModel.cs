using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the first-run setup wizard overlay.
/// Downloads the three required AI model files (Whisper GGML, ONNX embedding, Phi-3 LLM)
/// sequentially and reports per-model and overall status. The overlay is dismissed either
/// after all downloads succeed or when the user clicks Skip.
/// </summary>
public partial class SetupViewModel : ViewModelBase
{
    private readonly IModelManagerService _modelManager;
    private readonly IConfigService _configService;
    private readonly Action _onSetupComplete;

    /// <summary>Gets or sets the download progress for the Whisper model (0–1).</summary>
    [ObservableProperty]
    private double _whisperProgress;

    /// <summary>Gets or sets the download progress for the ONNX embedding model (0–1).</summary>
    [ObservableProperty]
    private double _onnxProgress;

    /// <summary>Gets or sets the download progress for the LLM model (0–1).</summary>
    [ObservableProperty]
    private double _llmProgress;

    /// <summary>Gets or sets the overall status message shown in the wizard.</summary>
    [ObservableProperty]
    private string _statusText = "Checking models...";

    /// <summary>Gets or sets a value indicating whether a download is currently in progress.</summary>
    [ObservableProperty]
    private bool _isDownloading;

    /// <summary>Gets or sets the per-model status label for Whisper (e.g. "Pending", "Downloading…", "Ready", "Failed").</summary>
    [ObservableProperty]
    private string _whisperStatus = "Pending";

    /// <summary>Gets or sets the per-model status label for the ONNX embedding model.</summary>
    [ObservableProperty]
    private string _onnxStatus = "Pending";

    /// <summary>Gets or sets the per-model status label for the LLM.</summary>
    [ObservableProperty]
    private string _llmStatus = "Pending";

    /// <summary>Gets or sets a value indicating whether the Retry button should be visible after a failed download.</summary>
    [ObservableProperty]
    private bool _canRetry;

    /// <summary>Initialises a new <see cref="SetupViewModel"/> with the required services and a completion callback.</summary>
    /// <param name="modelManager">Service used to check model presence and trigger downloads.</param>
    /// <param name="configService">Service used to read the current Whisper model size setting.</param>
    /// <param name="onSetupComplete">Callback invoked when setup finishes or the user skips.</param>
    public SetupViewModel(IModelManagerService modelManager, IConfigService configService, Action onSetupComplete)
    {
        _modelManager = modelManager;
        _configService = configService;
        _onSetupComplete = onSetupComplete;
    }

    /// <summary>Downloads all missing models sequentially and dismisses the wizard on success.</summary>
    [RelayCommand]
    private async Task StartDownloadAsync(CancellationToken ct)
    {
        IsDownloading = true;
        CanRetry = false;
        var config = _configService.Load();

        try
        {
            // Download Whisper model
            if (!_modelManager.IsWhisperModelDownloaded(config.WhisperModelSize))
            {
                WhisperStatus = "Downloading...";
                StatusText = $"Downloading Whisper {config.WhisperModelSize} model...";
                var whisperProgress = new Progress<double>(p => WhisperProgress = p);
                await _modelManager.DownloadWhisperModelAsync(config.WhisperModelSize, whisperProgress, ct);
            }
            WhisperProgress = 1.0;
            WhisperStatus = "Ready";

            // Download ONNX embedding model
            if (!_modelManager.IsOnnxModelDownloaded())
            {
                OnnxStatus = "Downloading...";
                StatusText = "Downloading embedding model...";
                var onnxProgress = new Progress<double>(p => OnnxProgress = p);
                await _modelManager.DownloadOnnxModelAsync(onnxProgress, ct);
            }
            OnnxProgress = 1.0;
            OnnxStatus = "Ready";

            // Download LLM
            if (!_modelManager.IsLlmModelDownloaded())
            {
                LlmStatus = "Downloading...";
                StatusText = "Downloading Phi-3 LLM...";
                var llmProgress = new Progress<double>(p => LlmProgress = p);
                await _modelManager.DownloadLlmModelAsync(llmProgress, ct);
            }
            LlmProgress = 1.0;
            LlmStatus = "Ready";

            StatusText = "All models ready!";
            await Task.Delay(500, ct);
            _onSetupComplete();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Download cancelled.";
            MarkInProgressAsFailed();
            CanRetry = true;
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
            MarkInProgressAsFailed();
            CanRetry = true;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    /// <summary>Cancels any in-progress download and immediately dismisses the wizard.</summary>
    [RelayCommand]
    private void Skip()
    {
        StartDownloadCommand.Cancel();
        _onSetupComplete();
    }

    /// <summary>Transitions any model whose status is "Downloading…" to "Failed" after a cancelled or failed download.</summary>
    private void MarkInProgressAsFailed()
    {
        if (WhisperStatus == "Downloading...") WhisperStatus = "Failed";
        if (OnnxStatus == "Downloading...") OnnxStatus = "Failed";
        if (LlmStatus == "Downloading...") LlmStatus = "Failed";
    }
}
