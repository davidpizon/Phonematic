using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Helpers;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Train</c> tab.
/// Mirrors the structure of <see cref="TranscribeViewModel"/>: the user selects an audio
/// file or folder, a <see cref="TrainFileItem"/> list is populated, and
/// <see cref="StartTrainingCommand"/> processes each file in turn.
/// File-picker dialogs are decoupled from the ViewModel via the
/// <see cref="BrowseFileInteraction"/> and <see cref="BrowseFolderInteraction"/> delegates,
/// which are assigned by <c>TrainView.axaml.cs</c>.
/// </summary>
public partial class TrainViewModel : ViewModelBase
{
    private readonly IConfigService _configService;

    /// <summary>Gets or sets the file or folder path the user has selected as training input.</summary>
    [ObservableProperty]
    private string _inputPath = string.Empty;

    /// <summary>Gets or sets the progress of the file currently being processed (0–1).</summary>
    [ObservableProperty]
    private double _currentFileProgress;

    /// <summary>Gets or sets the overall batch progress across all files (0–1).</summary>
    [ObservableProperty]
    private double _overallProgress;

    /// <summary>Gets or sets the human-readable status message shown in the UI.</summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>Gets or sets a value indicating whether a training run is currently in progress.</summary>
    [ObservableProperty]
    private bool _isTraining;

    /// <summary>Gets or sets the number of files successfully processed in the current run.</summary>
    [ObservableProperty]
    private int _completedCount;

    /// <summary>Gets or sets the number of files skipped in the current run.</summary>
    [ObservableProperty]
    private int _skippedCount;

    /// <summary>Gets or sets the number of files that failed in the current run.</summary>
    [ObservableProperty]
    private int _failedCount;

    /// <summary>The collection of training audio files discovered from <see cref="InputPath"/>.</summary>
    public ObservableCollection<TrainFileItem> Files { get; } = new();

    /// <summary>
    /// Initialises a new instance of <see cref="TrainViewModel"/>.
    /// </summary>
    /// <param name="configService">Application configuration service used to persist the last import path.</param>
    public TrainViewModel(IConfigService configService)
    {
        _configService = configService;

        var config = _configService.Load();
        if (!string.IsNullOrEmpty(config.LastImportPath) &&
            (File.Exists(config.LastImportPath) || Directory.Exists(config.LastImportPath)))
        {
            LoadFiles(config.LastImportPath);
        }
    }

    // ---------------------------------------------------------------------------
    // Interaction delegates (assigned by the View's code-behind)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Delegate invoked by <see cref="BrowseFileCommand"/> to open a file-picker dialog.
    /// Assigned by <c>TrainView.axaml.cs</c>.
    /// </summary>
    public Func<Task>? BrowseFileInteraction { get; set; }

    /// <summary>
    /// Delegate invoked by <see cref="BrowseFolderCommand"/> to open a folder-picker dialog.
    /// Assigned by <c>TrainView.axaml.cs</c>.
    /// </summary>
    public Func<Task>? BrowseFolderInteraction { get; set; }

    // ---------------------------------------------------------------------------
    // Commands
    // ---------------------------------------------------------------------------

    /// <summary>Opens a file-picker so the user can select a single audio file for training.</summary>
    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        if (BrowseFileInteraction != null)
            await BrowseFileInteraction();
    }

    /// <summary>Opens a folder-picker so the user can select a folder of audio files for training.</summary>
    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        if (BrowseFolderInteraction != null)
            await BrowseFolderInteraction();
    }

    /// <summary>
    /// Populates <see cref="Files"/> from the given file or folder <paramref name="path"/>
    /// and saves it as the last import path in configuration.
    /// </summary>
    /// <param name="path">Absolute path to an audio file or a directory containing audio files.</param>
    public void LoadFiles(string path)
    {
        Files.Clear();
        CompletedCount = 0;
        SkippedCount = 0;
        FailedCount = 0;
        InputPath = path;

        var config = _configService.Load();
        config.LastImportPath = path;
        _configService.Save(config);

        IEnumerable<string> audioFiles;

        if (File.Exists(path) && AudioConverter.IsSupported(path))
        {
            audioFiles = [path];
        }
        else if (Directory.Exists(path))
        {
            audioFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(AudioConverter.IsSupported);
        }
        else
        {
            return;
        }

        foreach (var file in audioFiles.OrderBy(f => f))
        {
            var info = new FileInfo(file);
            Files.Add(new TrainFileItem
            {
                FilePath = file,
                FileName = info.Name,
                FileSizeBytes = info.Length,
                Status = "Pending",
            });
        }
    }

    /// <summary>
    /// Processes each file in <see cref="Files"/> as a training input.
    /// Supports cancellation via <see cref="StartTrainingCancelCommand"/>.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartTrainingAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;

        IsTraining = true;
        CompletedCount = 0;
        SkippedCount = 0;
        FailedCount = 0;

        try
        {
            for (int i = 0; i < Files.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var item = Files[i];
                OverallProgress = (double)i / Files.Count;
                item.Status = "Processing...";
                StatusText = $"Processing: {item.FileName}";
                CurrentFileProgress = 0;

                try
                {
                    // Simulate per-file work so progress feedback is visible.
                    // Replace this with real training logic when the training pipeline is wired up.
                    await Task.Delay(0, ct);

                    CurrentFileProgress = 1.0;
                    item.Status = "Done";
                    CompletedCount++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    item.Status = $"Error: {ex.Message}";
                    FailedCount++;
                }
            }

            OverallProgress = 1.0;
            StatusText = $"Complete: {CompletedCount} processed, {SkippedCount} skipped, {FailedCount} failed";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Training cancelled.";
        }
        finally
        {
            IsTraining = false;
        }
    }
}

/// <summary>
/// Represents a single audio file queued for training.
/// Mirrors <see cref="Mp3FileItem"/> in the Transcribe pipeline.
/// </summary>
public partial class TrainFileItem : ObservableObject
{
    /// <summary>Gets or sets the absolute file path.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the display file name.</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Gets or sets the raw file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets a human-readable file size string.</summary>
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1048576 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / 1048576.0:F1} MB",
    };

    /// <summary>Gets or sets the current processing status of this file.</summary>
    [ObservableProperty]
    private string _status = "Pending";
}
