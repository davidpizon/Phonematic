using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Transcriptions</c> history tab.
/// Displays all previously processed audio files and shows the full transcription text
/// when the user selects a row.
/// </summary>
public partial class TranscriptionsViewModel : ViewModelBase
{
    private readonly IFileTrackingService _fileTrackingService;

    /// <summary>Gets or sets the currently selected processed-file record; changing it loads its transcription text.</summary>
    [ObservableProperty]
    private ProcessedFile? _selectedFile;

    /// <summary>Gets or sets the full transcription text of the selected file.</summary>
    [ObservableProperty]
    private string _transcriptionText = string.Empty;

    /// <summary>The ordered list of previously processed audio files shown in the history grid.</summary>
    public ObservableCollection<ProcessedFile> ProcessedFiles { get; } = new();

    /// <summary>Initialises a new <see cref="TranscriptionsViewModel"/> with the file-tracking service.</summary>
    public TranscriptionsViewModel(IFileTrackingService fileTrackingService)
    {
        _fileTrackingService = fileTrackingService;
    }

    /// <summary>Loads the transcription text for the newly selected file.</summary>
    partial void OnSelectedFileChanged(ProcessedFile? value)
    {
        if (value == null)
        {
            TranscriptionText = string.Empty;
            return;
        }

        try
        {
            if (File.Exists(value.TranscriptionPath))
            {
                TranscriptionText = File.ReadAllText(value.TranscriptionPath);
            }
            else
            {
                TranscriptionText = "(Transcription file not found)";
            }
        }
        catch (Exception ex)
        {
            TranscriptionText = $"Error reading transcription: {ex.Message}";
        }
    }

    /// <summary>Reloads the processed-files list from the database.</summary>
    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        ProcessedFiles.Clear();
        var files = await _fileTrackingService.GetAllProcessedFilesAsync(ct);
        foreach (var file in files)
        {
            ProcessedFiles.Add(file);
        }
    }
}
