using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Helpers;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Train</c> tab.
/// The user selects a folder via <see cref="BrowseFolderCommand"/>; the ViewModel scans
/// that folder recursively for <em>input sets</em>. An input set is an audio file paired
/// with a sibling <c>.phos</c> transcription file that shares the same base name and
/// directory (e.g. <c>recording.mp3</c> + <c>recording.phos</c>). Every discovered audio
/// file produces one <see cref="TrainFileItem"/> regardless of whether its companion
/// <c>.phos</c> file is present; the <c>.phos</c> presence is reflected in
/// <see cref="TrainFileItem.TranscriptionStatus"/>.
/// <see cref="StartTrainingCommand"/> then processes each item in <see cref="Files"/> in turn.
/// The folder-picker dialog is decoupled from the ViewModel via the
/// <see cref="BrowseFolderInteraction"/> delegate, which is assigned by
/// <c>TrainView.axaml.cs</c>.
/// </summary>
public partial class TrainViewModel : ViewModelBase
{
    private readonly IConfigService _configService;

    /// <summary>Gets or sets the folder path the user has selected as training input.</summary>
    [ObservableProperty]
    private string _inputPath = string.Empty;

    /// <summary>Gets or sets the progress of the input set currently being processed (0–1).</summary>
    [ObservableProperty]
    private double _currentFileProgress;

    /// <summary>Gets or sets the overall batch progress across all input sets (0–1).</summary>
    [ObservableProperty]
    private double _overallProgress;

    /// <summary>Gets or sets the human-readable status message shown in the UI.</summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>Gets or sets a value indicating whether a training run is currently in progress.</summary>
    [ObservableProperty]
    private bool _isTraining;

    /// <summary>Gets or sets the number of input sets successfully processed in the current run.</summary>
    [ObservableProperty]
    private int _completedCount;

    /// <summary>Gets or sets the number of input sets skipped in the current run.</summary>
    [ObservableProperty]
    private int _skippedCount;

    /// <summary>Gets or sets the number of input sets that failed in the current run.</summary>
    [ObservableProperty]
    private int _failedCount;

    /// <summary>
    /// The collection of input sets discovered from the selected folder.
    /// Each item represents one audio file and its optional companion <c>.phos</c> file.
    /// </summary>
    public ObservableCollection<TrainFileItem> Files { get; } = new();

    /// <summary>
    /// Initialises a new instance of <see cref="TrainViewModel"/>.
    /// Restores the last used folder from configuration if it still exists on disk.
    /// </summary>
    /// <param name="configService">Application configuration service used to persist the last import path.</param>
    public TrainViewModel(IConfigService configService)
    {
        _configService = configService;

        var config = _configService.Load();
        if (!string.IsNullOrEmpty(config.LastImportPath) &&
            Directory.Exists(config.LastImportPath))
        {
            LoadInputSets(config.LastImportPath);
        }
    }

    // ---------------------------------------------------------------------------
    // Interaction delegate (assigned by the View's code-behind)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Delegate invoked by <see cref="BrowseFolderCommand"/> to open a folder-picker dialog.
    /// Assigned by <c>TrainView.axaml.cs</c>.
    /// </summary>
    public Func<Task>? BrowseFolderInteraction { get; set; }

    // ---------------------------------------------------------------------------
    // Commands
    // ---------------------------------------------------------------------------

    /// <summary>Opens a folder-picker so the user can select a folder of input sets for training.</summary>
    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        if (BrowseFolderInteraction != null)
            await BrowseFolderInteraction();
    }

    /// <summary>
    /// Scans <paramref name="folderPath"/> recursively for input sets and populates
    /// <see cref="Files"/> with the discovered entries.
    /// An input set is identified by a unique directory + base-name stem. A row is
    /// created for every stem that has at least one of:
    /// <list type="bullet">
    ///   <item>a supported audio file (e.g. <c>.mp3</c>, <c>.wav</c>)</item>
    ///   <item>a <c>.phos</c> transcription file</item>
    /// </list>
    /// Either or both may be present; the presence of each is reflected in
    /// <see cref="TrainFileItem.AudioExtension"/> and
    /// <see cref="TrainFileItem.TranscriptionStatus"/> respectively.
    /// Saves <paramref name="folderPath"/> as the last import path in configuration.
    /// Does nothing when <paramref name="folderPath"/> does not exist on disk.
    /// </summary>
    /// <param name="folderPath">Absolute path to the folder to scan.</param>
    public void LoadInputSets(string folderPath)
    {
        Files.Clear();
        CompletedCount = 0;
        SkippedCount = 0;
        FailedCount = 0;
        InputPath = folderPath;

        var config = _configService.Load();
        config.LastImportPath = folderPath;
        _configService.Save(config);

        if (!Directory.Exists(folderPath))
            return;

        var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        // Index audio files by their stem key (directory + base name, case-insensitive).
        var audioByKey = allFiles
            .Where(AudioConverter.IsSupported)
            .ToDictionary(
                f => StemKey(f),
                f => f,
                StringComparer.OrdinalIgnoreCase);

        // Index .phos files by their stem key.
        var phosByKey = allFiles
            .Where(f => string.Equals(Path.GetExtension(f), ".phos", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                f => StemKey(f),
                f => f,
                StringComparer.OrdinalIgnoreCase);

        // Union of all stems, sorted for a deterministic display order.
        var allKeys = audioByKey.Keys
            .Union(phosByKey.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            audioByKey.TryGetValue(key, out var audioPath);
            phosByKey.TryGetValue(key, out var phosPath);

            // Prefer the audio file's name for display; fall back to the .phos file name.
            var displayPath = audioPath ?? phosPath!;
            var info = new FileInfo(displayPath);

            Files.Add(new TrainFileItem
            {
                FilePath = displayPath,
                FileName = Path.GetFileNameWithoutExtension(displayPath),
                FileSizeBytes = info.Length,
                AudioPath = audioPath ?? string.Empty,
                TranscriptionPath = phosPath ?? string.Empty,
                Status = "Pending",
            });
        }
    }

    /// <summary>
    /// Returns a normalised key for grouping a file with its partner.
    /// The key is the combination of the file's directory and its base name without extension,
    /// used to match audio files with their sibling <c>.phos</c> files.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    private static string StemKey(string filePath) =>
        Path.Combine(
            Path.GetDirectoryName(filePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(filePath));

    /// <summary>
    /// Processes each input set in <see cref="Files"/> as training data.
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
/// Represents a single input set queued for training.
/// An input set is an audio file and its optional companion <c>.phos</c> transcription file.
/// </summary>
public partial class TrainFileItem : ObservableObject
{
    /// <summary>Gets or sets the absolute path to the audio file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of this input set (file base name without extension).</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Gets or sets the raw size of the audio file in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets a human-readable file size string.</summary>
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1048576 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / 1048576.0:F1} MB",
    };

    /// <summary>
    /// Gets or sets the absolute path to the audio file for this input set.
    /// When changed, <see cref="AudioExtension"/> is also notified.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioExtension))]
    private string _audioPath = string.Empty;

    /// <summary>
    /// Gets the file extension of <see cref="AudioPath"/> (e.g. <c>.mp3</c>),
    /// or an empty string when no audio file is set.
    /// Displayed in the <c>Audio</c> column of the Source Input list.
    /// </summary>
    public string AudioExtension =>
        string.IsNullOrEmpty(AudioPath) ? string.Empty : Path.GetExtension(AudioPath);

    /// <summary>
    /// Gets or sets the absolute path to the companion <c>.phos</c> transcription file,
    /// or an empty string when no such file exists.
    /// When changed, <see cref="TranscriptionStatus"/> is also notified.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TranscriptionStatus))]
    private string _transcriptionPath = string.Empty;

    /// <summary>
    /// Gets <c>"OK"</c> when a companion <c>.phos</c> file is associated with this input set,
    /// or an empty string when none is present.
    /// Displayed in the <c>Transcription</c> column of the Source Input list.
    /// </summary>
    public string TranscriptionStatus =>
        string.IsNullOrEmpty(TranscriptionPath) ? string.Empty : "OK";

    /// <summary>Gets or sets the current processing status of this input set.</summary>
    [ObservableProperty]
    private string _status = "Pending";
}
