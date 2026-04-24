namespace Phonematic.Services;

public interface ILlmService
{
    bool IsModelLoaded { get; }
    Task LoadModelAsync(CancellationToken ct = default);
    IAsyncEnumerable<string> GenerateAnswerAsync(string question, List<SearchResult> context, CancellationToken ct = default);
}
