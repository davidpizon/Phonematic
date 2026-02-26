using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriptonator.Models;
using Transcriptonator.Services;

namespace Transcriptonator.ViewModels;

public partial class TranscriptionsViewModel : ViewModelBase
{
    private readonly IFileTrackingService _fileTrackingService;

    [ObservableProperty]
    private ProcessedFile? _selectedFile;

    [ObservableProperty]
    private string _transcriptionText = string.Empty;

    public ObservableCollection<ProcessedFile> ProcessedFiles { get; } = new();

    public TranscriptionsViewModel(IFileTrackingService fileTrackingService)
    {
        _fileTrackingService = fileTrackingService;
    }

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
