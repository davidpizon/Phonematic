using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phonematic.Services;

namespace Phonematic.ViewModels;

/// <summary>
/// ViewModel for the <c>Search</c> / RAG tab.
/// Accepts a natural-language query, performs vector similarity search over stored
/// transcription chunks, then streams an LLM-generated answer grounded in those chunks.
/// </summary>
public partial class SearchViewModel : ViewModelBase
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILlmService _llmService;
    private readonly IConfigService _configService;

    /// <summary>Gets or sets the query text entered by the user.</summary>
    [ObservableProperty]
    private string _queryText = string.Empty;

    /// <summary>Gets or sets the LLM-generated answer streamed token-by-token.</summary>
    [ObservableProperty]
    private string _llmAnswer = string.Empty;

    /// <summary>Gets or sets a value indicating whether a vector search is currently running.</summary>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>Gets or sets a value indicating whether the LLM is loading its model weights.</summary>
    [ObservableProperty]
    private bool _isLoadingLlm;

    /// <summary>Gets or sets the status message shown below the results panel.</summary>
    [ObservableProperty]
    private string _statusText = "Enter a query to search transcriptions";

    /// <summary>Gets or sets the currently selected search result item; changing it loads its source transcription.</summary>
    [ObservableProperty]
    private SearchResultItem? _selectedResult;

    /// <summary>Gets or sets the full text of the transcription file for the selected result.</summary>
    [ObservableProperty]
    private string _sourceTranscriptionText = string.Empty;

    /// <summary>Gets or sets the header label shown above the source transcription panel.</summary>
    [ObservableProperty]
    private string _sourceHeaderText = string.Empty;

    /// <summary>The ordered list of vector-search results displayed in the results panel.</summary>
    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

    /// <summary>Initialises a new <see cref="SearchViewModel"/> with the required services.</summary>
    public SearchViewModel(IVectorSearchService vectorSearchService, ILlmService llmService, IConfigService configService)
    {
        _vectorSearchService = vectorSearchService;
        _llmService = llmService;
        _configService = configService;
    }

    /// <summary>Loads the source transcription text for the newly selected result.</summary>
    partial void OnSelectedResultChanged(SearchResultItem? value)
    {
        if (value == null)
        {
            SourceTranscriptionText = string.Empty;
            SourceHeaderText = string.Empty;
            return;
        }

        SourceHeaderText = $"Source: {value.SourceFilePath}";

        try
        {
            if (File.Exists(value.TranscriptionPath))
            {
                SourceTranscriptionText = File.ReadAllText(value.TranscriptionPath);
            }
            else
            {
                SourceTranscriptionText = "(Transcription file not found at: " + value.TranscriptionPath + ")";
            }
        }
        catch (Exception ex)
        {
            SourceTranscriptionText = $"Error reading transcription: {ex.Message}";
        }
    }

    /// <summary>Executes a vector search and streams an LLM-generated answer for the query.</summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SearchAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(QueryText)) return;

        IsSearching = true;
        SearchResults.Clear();
        LlmAnswer = string.Empty;
        SelectedResult = null;
        StatusText = "Searching...";

        try
        {
            var config = _configService.Load();
            var results = await _vectorSearchService.SearchAsync(QueryText, config.RagTopK, ct);

            foreach (var result in results)
            {
                SearchResults.Add(new SearchResultItem
                {
                    FileName = result.FileName,
                    SourceFilePath = result.SourceFilePath,
                    TranscriptionPath = result.TranscriptionPath,
                    Similarity = result.Similarity,
                    ChunkText = result.Chunk.Text,
                    TextPreview = result.Chunk.Text.Length > 200
                        ? result.Chunk.Text[..200] + "..."
                        : result.Chunk.Text
                });
            }

            if (results.Count == 0)
            {
                StatusText = "No results found.";
                IsSearching = false;
                return;
            }

            // Auto-select first result to show its source
            SelectedResult = SearchResults[0];

            // Load LLM if needed
            if (!_llmService.IsModelLoaded)
            {
                IsLoadingLlm = true;
                StatusText = "Loading LLM (first time may take a moment)...";
                await _llmService.LoadModelAsync(ct);
                IsLoadingLlm = false;
            }

            // Stream LLM answer
            StatusText = "Generating answer...";
            var sb = new StringBuilder();
            await foreach (var token in _llmService.GenerateAnswerAsync(QueryText, results, ct))
            {
                sb.Append(token);
                LlmAnswer = sb.ToString();
            }

            StatusText = $"Found {results.Count} relevant chunks";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Search cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
            IsLoadingLlm = false;
        }
    }
}

/// <summary>A single vector-search result item, containing chunk text, similarity score, and source file metadata.</summary>
public class SearchResultItem
{
    /// <summary>Gets or sets the display name of the source audio file.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Gets or sets the absolute path to the source audio file.</summary>
    public string SourceFilePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the absolute path to the transcription <c>.phos</c> file.</summary>
    public string TranscriptionPath { get; set; } = string.Empty;
    /// <summary>Gets or sets the cosine similarity score (0–1) between the query and this chunk.</summary>
    public double Similarity { get; set; }
    /// <summary>Gets the similarity formatted as a percentage string.</summary>
    public string SimilarityDisplay => $"{Similarity:P0}";
    /// <summary>Gets or sets the full text of the matching chunk.</summary>
    public string ChunkText { get; set; } = string.Empty;
    /// <summary>Gets or sets a truncated preview of the chunk text for display in the results list.</summary>
    public string TextPreview { get; set; } = string.Empty;
}
