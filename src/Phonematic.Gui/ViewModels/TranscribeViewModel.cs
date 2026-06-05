using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Helpers;
using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Transcribe</c> tab.
/// Accepts a single audio file or a folder of audio files, runs Whisper transcription on each,
/// tracks processed files, generates embeddings, and updates the UI with per-file and overall
/// progress. File-picker dialogs are decoupled via <see cref="BrowseFileInteraction"/> and
/// <see cref="BrowseFolderInteraction"/> delegates assigned by the View's code-behind.
/// </summary>
public partial class TranscribeViewModel : ViewModelBase
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IFileTrackingService _fileTrackingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IConfigService _configService;

    /// <summary>Gets or sets the input path (file or folder) selected by the user.</summary>
    [ObservableProperty]
    private string _inputPath = string.Empty;

    /// <summary>Gets or sets the transcription progress for the file currently being processed (0–1).</summary>
    [ObservableProperty]
    private double _currentFileProgress;

    /// <summary>Gets or sets the overall batch progress across all queued files (0–1).</summary>
    [ObservableProperty]
    private double _overallProgress;

    /// <summary>Gets or sets the human-readable status message shown in the UI.</summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>Gets or sets a value indicating whether transcription is currently running.</summary>
    [ObservableProperty]
    private bool _isTranscribing;

    /// <summary>Gets or sets the number of files successfully transcribed in the current run.</summary>
    [ObservableProperty]
    private int _completedCount;

    /// <summary>Gets or sets the number of files skipped (already processed) in the current run.</summary>
    [ObservableProperty]
    private int _skippedCount;

    /// <summary>Gets or sets the number of files that failed in the current run.</summary>
    [ObservableProperty]
    private int _failedCount;

    /// <summary>The collection of audio files queued for transcription.</summary>
    public ObservableCollection<Mp3FileItem> Files { get; } = new();

    /// <summary>Initialises a new <see cref="TranscribeViewModel"/> with the required services.</summary>
    public TranscribeViewModel(
        ITranscriptionService transcriptionService,
        IFileTrackingService fileTrackingService,
        IEmbeddingService embeddingService,
        IConfigService configService)
    {
        _transcriptionService = transcriptionService;
        _fileTrackingService = fileTrackingService;
        _embeddingService = embeddingService;
        _configService = configService;

        // Restore last import path
        var config = _configService.Load();
    }

    /// <summary>Delegate assigned by the View to open a single-file picker dialog.</summary>
    public Func<Task>? BrowseFileInteraction { get; set; }
    /// <summary>Delegate assigned by the View to open a folder-picker dialog.</summary>
    public Func<Task>? BrowseFolderInteraction { get; set; }

    /// <summary>Invokes the file-picker interaction to let the user select a single audio file.</summary>
    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        if (BrowseFileInteraction != null)
            await BrowseFileInteraction();
    }

    /// <summary>Invokes the folder-picker interaction to let the user select a folder of audio files.</summary>
    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        if (BrowseFolderInteraction != null)
            await BrowseFolderInteraction();
    }

    /// <summary>
    /// Populates <see cref="Files"/> from a file or folder path, resets counters, and saves
    /// the path to config as the last import path.
    /// </summary>
    /// <param name="path">Absolute path to an audio file or a directory of audio files.</param>
    public void LoadFiles(string path)
    {
        Files.Clear();
        CompletedCount = 0;
        SkippedCount = 0;
        FailedCount = 0;
        InputPath = path;

        // Save last import path
        var config = _configService.Load();
        _configService.Save(config);

        IEnumerable<string> audioFiles;

        if (File.Exists(path) && AudioConverter.IsSupported(path))
        {
            audioFiles = new[] { path };
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
            Files.Add(new Mp3FileItem
            {
                FilePath = file,
                FileName = info.Name,
                FileSizeBytes = info.Length,
                Status = "Pending"
            });
        }
    }

    /// <summary>Processes all queued files — transcribes, tracks, and embeds each in turn.</summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartTranscriptionAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;

        IsTranscribing = true;
        CompletedCount = 0;
        SkippedCount = 0;
        FailedCount = 0;
        var config = _configService.Load();

        try
        {
            for (int i = 0; i < Files.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var item = Files[i];
                OverallProgress = (double)i / Files.Count;

                // Check if already processed
                var hash = await FileHasher.ComputeSha256Async(item.FilePath, ct);
                if (await _fileTrackingService.IsFileProcessedAsync(item.FilePath, hash, ct))
                {
                    item.Status = "Skipped";
                    SkippedCount++;
                    continue;
                }

                item.Status = "Transcribing...";
                StatusText = $"Transcribing: {item.FileName}";
                CurrentFileProgress = 0;

                try
                {
                    var progress = new Progress<double>(p => CurrentFileProgress = p);
                    var audioDuration = AudioConverter.GetDurationSeconds(item.FilePath);

                    var result = await _transcriptionService.TranscribeAsync(
                        item.FilePath, config.OutputDirectory, config.WhisperModelSize, progress, ct);

                    var processedFile = new ProcessedFile
                    {
                        FilePath = item.FilePath,
                        FileHash = hash,
                        FileSizeBytes = item.FileSizeBytes,
                        TranscriptionPath = result.OutputPath,
                        TranscribedAtUtc = DateTime.UtcNow,
                        WhisperModel = config.WhisperModelSize,
                        AudioDurationSeconds = audioDuration,
                        TranscriptionDurationSeconds = result.DurationSeconds
                    };

                    var saved = await _fileTrackingService.RecordTranscriptionAsync(processedFile, ct);

                    // Generate and store embeddings
                    StatusText = $"Embedding: {item.FileName}";
                    await _embeddingService.StoreChunksAsync(saved, result.Text, ct);

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
            StatusText = $"Complete: {CompletedCount} transcribed, {SkippedCount} skipped, {FailedCount} failed";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Transcription cancelled.";
        }
        finally
        {
            IsTranscribing = false;
        }
    }
}

/// <summary>
/// Represents a single audio file queued for transcription, together with its display metadata
/// and live status string.
/// </summary>
public partial class Mp3FileItem : ObservableObject
{
    /// <summary>Gets or sets the absolute path to the audio file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the display file name (without directory).</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets a human-readable file size string (B / KB / MB).</summary>
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1048576 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / 1048576.0:F1} MB"
    };

    /// <summary>Gets or sets the processing status label (e.g. "Pending", "Transcribing…", "Done", "Skipped").</summary>
    [ObservableProperty]
    private string _status = "Pending";
}
