namespace Phonematic.Services;

/// <summary>
/// Loads an on-device LLM (Phi-3 Mini GGUF) and streams token-by-token answers to
/// RAG-augmented queries. Implemented by <see cref="LlmService"/>.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Gets a value indicating whether the model weights have been loaded into memory.
    /// <see langword="false"/> until <see cref="LoadModelAsync"/> completes successfully.
    /// </summary>
    bool IsModelLoaded { get; }

    /// <summary>
    /// Loads the Phi-3 Mini GGUF weights and creates a 4096-token context on a
    /// background thread. No-ops if the model is already loaded.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task LoadModelAsync(CancellationToken ct = default);

    /// <summary>
    /// Builds a RAG prompt from <paramref name="question"/> and the provided
    /// <paramref name="context"/> chunks, then streams the LLM answer one token at a time.
    /// Calls <see cref="LoadModelAsync"/> automatically if the model is not yet loaded.
    /// </summary>
    /// <param name="question">The user's natural-language question.</param>
    /// <param name="context">Top-K search results used as RAG context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of answer tokens.</returns>
    IAsyncEnumerable<string> GenerateAnswerAsync(string question, List<SearchResult> context, CancellationToken ct = default);
}
