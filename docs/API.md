# API Reference

Complete reference for all public classes, interfaces, and records in the Phonematic codebase.

See also:
- [ARCHITECTURE.md](ARCHITECTURE.md) — system layers, data flows, and DB schema
- [PHOSCRIPT.md](PHOSCRIPT.md) — PhoScript 1.0 specification (`.phos` output format)
- [IPA_REFERENCE.md](IPA_REFERENCE.md) — IPA symbol reference used in PhoScript `ipa` attributes
- [TESTING.md](TESTING.md) — test patterns for the classes documented here
- [CONTRIBUTING.md](CONTRIBUTING.md) — guidelines for adding new public APIs

---

## Models

### `AppConfig`
**Namespace:** `Phonematic.Models`

Persisted application configuration. Serialised to `%LOCALAPPDATA%\Phonematic\config\settings.json` via `ConfigService`.

| Property | Type | Default | Description |
|---|---|---|---|
| `OutputDirectory` | `string` | `~/Documents/Phonematic` | Directory where transcription `.txt` files are saved. |
| `WhisperModelSize` | `string` | `"tiny.en"` | Whisper GGML model size key (e.g. `"tiny.en"`, `"small"`, `"large"`). |
| `ThreadCount` | `int` | `ProcessorCount / 2` (min 1) | CPU thread count passed to the Whisper processor. |
| `PlaudToken` | `string?` | `null` | Saved PLAUD API Bearer token. |
| `ChunkSize` | `int` | `500` | Maximum character length of a single transcription chunk for embedding. |
| `ChunkOverlap` | `int` | `100` | Character overlap between consecutive chunks. |
| `RagTopK` | `int` | `5` | Number of top chunks returned by vector search. |
| `MaxConcurrentPlaudDownloads` | `int` | `3` | Semaphore limit for parallel PLAUD file downloads. |
| `LastImportPath` | `string` | `""` | Restored on startup to pre-populate the Transcribe view. |

---

### `ProcessedFile`
**Namespace:** `Phonematic.Models`

EF Core entity. Represents an audio file that has been successfully transcribed and embedded.

| Property | Type | Description |
|---|---|---|
| `Id` | `int` | Primary key. |
| `FilePath` | `string` | Absolute path to the original audio file. |
| `FileHash` | `string` | Lowercase hex SHA-256 of the audio file (used for deduplication). |
| `FileSizeBytes` | `long` | Size of the audio file in bytes. |
| `TranscriptionPath` | `string` | Absolute path to the saved `.txt` transcription file. |
| `TranscribedAtUtc` | `DateTime` | UTC timestamp of when transcription completed. |
| `WhisperModel` | `string` | The model size string used for transcription. |
| `AudioDurationSeconds` | `double` | Duration of the audio file in seconds. |
| `TranscriptionDurationSeconds` | `double` | Wall-clock time in seconds taken to transcribe. |
| `Chunks` | `ICollection<TranscriptionChunk>` | Navigation property — the embedded text chunks. |

---

### `TranscriptionChunk`
**Namespace:** `Phonematic.Models`

EF Core entity. A single text chunk derived from a `ProcessedFile`, with its vector embedding stored as a raw byte blob.

| Property | Type | Description |
|---|---|---|
| `Id` | `int` | Primary key. |
| `ProcessedFileId` | `int` | Foreign key to `ProcessedFile.Id`. |
| `ChunkIndex` | `int` | Zero-based position of this chunk within the source file. |
| `Text` | `string` | The plain text content of this chunk. |
| `Embedding` | `byte[]` | 384 × `float32` L2-normalised embedding, stored little-endian. |
| `ProcessedFile` | `ProcessedFile` | Navigation property. |

---

### `PlaudRecording`
**Namespace:** `Phonematic.Models`

EF Core entity. Mirrors a recording fetched from the PLAUD cloud API.

| Property | Type | Description |
|---|---|---|
| `Id` | `int` | Primary key. |
| `PlaudFileId` | `string` | Unique file identifier from the PLAUD API. |
| `Title` | `string` | Recording title / filename from PLAUD. |
| `RecordedAtUtc` | `DateTime` | UTC time the recording was made. |
| `DurationSeconds` | `double` | Audio duration in seconds. |
| `FolderName` | `string?` | PLAUD folder / tag name (nullable). |
| `LocalFilePath` | `string?` | Absolute path to the downloaded local audio file (null if not yet downloaded). |
| `IsDownloaded` | `bool` | Whether the audio file has been downloaded locally. |
| `DownloadedAtUtc` | `DateTime?` | UTC timestamp of download completion (null if not downloaded). |
| `FileSizeBytes` | `long?` | File size in bytes (null if unknown). |

---

## Services

### `IConfigService` / `ConfigService`
**Namespace:** `Phonematic.Services`

Manages reading and writing the `AppConfig` JSON settings file and exposes well-known directory paths.

**Properties**

| Property | Description |
|---|---|
| `AppDataDirectory` | `%LOCALAPPDATA%\Phonematic` |
| `ConfigDirectory` | `AppDataDirectory\config` |
| `ModelsDirectory` | `AppDataDirectory\models` |
| `WhisperModelsDirectory` | `ModelsDirectory\whisper` |
| `OnnxModelsDirectory` | `ModelsDirectory\onnx` |
| `LlmModelsDirectory` | `ModelsDirectory\llm` |
| `DatabasePath` | `AppDataDirectory\Phonematic.db` |

**Methods**

| Method | Description |
|---|---|
| `AppConfig Load()` | Deserialises `settings.json`; creates it with defaults on first call. |
| `void Save(AppConfig config)` | Serialises `config` to `settings.json` (indented camelCase JSON). |

---

### `IModelManagerService` / `ModelManagerService`
**Namespace:** `Phonematic.Services`

Downloads and locates the three AI model files required by the application.

**Model URLs**

| Model | Source |
|---|---|
| Whisper GGML | `ggerganov/whisper.cpp` via `WhisperGgmlDownloader` |
| ONNX Embedding | `sentence-transformers/all-MiniLM-L6-v2` on HuggingFace |
| LLM (Phi-3) | `microsoft/Phi-3-mini-4k-instruct-gguf` on HuggingFace |

**Methods**

| Method | Description |
|---|---|
| `bool IsWhisperModelDownloaded(string modelSize)` | Returns `true` if the GGML binary exists on disk. |
| `bool IsOnnxModelDownloaded()` | Returns `true` if both `model.onnx` and `vocab.txt` exist. |
| `bool IsLlmModelDownloaded()` | Returns `true` if the Phi-3 GGUF exists. |
| `bool AreAllModelsReady(string whisperModelSize)` | Convenience AND of all three checks. |
| `string GetWhisperModelPath(string modelSize)` | Returns absolute path for a given model size key. |
| `string GetOnnxModelPath()` | Returns absolute path to `model.onnx`. |
| `string GetOnnxVocabPath()` | Returns absolute path to `vocab.txt`. |
| `string GetLlmModelPath()` | Returns absolute path to the Phi-3 GGUF. |
| `Task DownloadWhisperModelAsync(string modelSize, IProgress<double>?, CancellationToken)` | Streams the GGML model; writes to a `.tmp` file then renames on success. Retries up to 3 times. |
| `Task DownloadOnnxModelAsync(IProgress<double>?, CancellationToken)` | Downloads `model.onnx` and `vocab.txt`. |
| `Task DownloadLlmModelAsync(IProgress<double>?, CancellationToken)` | Downloads the Phi-3 GGUF (up to ~2 GB). |

---

### `ITranscriptionService` / `TranscriptionService`
**Namespace:** `Phonematic.Services`

Transcribes a single audio file using Whisper.net. Caches the `WhisperFactory` and `WhisperProcessor` between calls to avoid reloading the model for each file.

**Methods**

| Method | Description |
|---|---|
| `Task<TranscriptionResult> TranscribeAsync(string audioPath, string outputDirectory, string whisperModelSize, IProgress<double>?, CancellationToken)` | Converts audio to 16kHz WAV, runs Whisper, writes a timestamped `.txt` file, and returns a `TranscriptionResult`. |

**`TranscriptionResult` record**

| Member | Description |
|---|---|
| `string Text` | Full transcription text (all segments joined). |
| `string OutputPath` | Absolute path to the saved `.txt` file. |
| `double DurationSeconds` | Wall-clock transcription time. |

Progress is reported at key milestones: 0.05 (start), 0.15 (WAV ready), 0.20 (processor ready), 0.90 (segments done), 1.0 (file saved).

**Implements** `IDisposable` — disposes the `WhisperProcessor` and `WhisperFactory`.

---

### `IEmbeddingService` / `EmbeddingService`
**Namespace:** `Phonematic.Services`

Generates 384-dimensional sentence embeddings using the `all-MiniLM-L6-v2` ONNX model with a hand-rolled WordPiece tokeniser, and stores chunks in the database.

**Constants**

| Constant | Value | Description |
|---|---|---|
| `MaxTokenLength` | `128` | Maximum token sequence length passed to the model. |
| `EmbeddingDimension` | `384` | Output dimension of the model. |

**Methods**

| Method | Description |
|---|---|
| `float[] GenerateEmbedding(string text)` | Tokenises `text`, runs the ONNX model, applies mean pooling over the token dimension, then L2-normalises the result. Loads the model lazily on first call. |
| `List<string> ChunkText(string text, int chunkSize, int chunkOverlap)` | Splits `text` into overlapping sentence-boundary-aware chunks. Returns an empty list for blank input. |
| `Task StoreChunksAsync(ProcessedFile file, string fullText, CancellationToken)` | Chunks `fullText` using configured `ChunkSize`/`ChunkOverlap`, generates an embedding for each chunk, and saves all `TranscriptionChunk` rows in a single `SaveChangesAsync` call. |

**Implements** `IDisposable` — disposes the ONNX `InferenceSession`.

---

### `IVectorSearchService` / `VectorSearchService`
**Namespace:** `Phonematic.Services`

Performs brute-force cosine similarity search over all stored `TranscriptionChunk` embeddings.

**Methods**

| Method | Description |
|---|---|
| `Task<List<SearchResult>> SearchAsync(string query, int topK, CancellationToken)` | Embeds `query`, scores every chunk in the database, and returns the top-K results ordered by descending similarity. |

**`SearchResult` record**

| Member | Description |
|---|---|
| `TranscriptionChunk Chunk` | The matching chunk entity. |
| `string FileName` | Filename portion of `SourceFilePath`. |
| `string SourceFilePath` | Absolute path to the original audio file. |
| `string TranscriptionPath` | Absolute path to the `.txt` transcription file. |
| `double Similarity` | Cosine similarity score (−1 to 1, higher is more relevant). |

---

### `ILlmService` / `LlmService`
**Namespace:** `Phonematic.Services`

Loads a Phi-3 Mini GGUF model via LLamaSharp and streams token-by-token answers to RAG queries.

**Properties**

| Property | Description |
|---|---|
| `bool IsModelLoaded` | `true` after `LoadModelAsync` completes. |

**Methods**

| Method | Description |
|---|---|
| `Task LoadModelAsync(CancellationToken)` | Loads the GGUF weights and creates a 4096-token context. No-ops if already loaded. Runs on a thread-pool thread via `Task.Run`. |
| `IAsyncEnumerable<string> GenerateAnswerAsync(string question, List<SearchResult> context, CancellationToken)` | Builds a RAG prompt (system + context chunks + user question) and streams answer tokens using `InteractiveExecutor`. Auto-loads the model if not yet loaded. |

The RAG prompt uses Phi-3's `<|system|>` / `<|user|>` / `<|assistant|>` chat template. Anti-prompts stop generation at `<|end|>`, `<|user|>`, or `"\nUser:"`.

**Implements** `IDisposable` — disposes the `LLamaContext` and `LLamaWeights`.

---

### `IFileTrackingService` / `FileTrackingService`
**Namespace:** `Phonematic.Services`

Provides deduplication and retrieval of `ProcessedFile` records.

**Methods**

| Method | Description |
|---|---|
| `Task<bool> IsFileProcessedAsync(string filePath, string fileHash, CancellationToken)` | Returns `true` if a record with both the same path and hash exists. |
| `Task<ProcessedFile> RecordTranscriptionAsync(ProcessedFile file, CancellationToken)` | Inserts the new record and returns the entity with its database-assigned `Id`. |
| `Task<List<ProcessedFile>> GetAllProcessedFilesAsync(CancellationToken)` | Returns all records ordered by `TranscribedAtUtc` descending. |
| `Task<ProcessedFile?> GetProcessedFileAsync(int id, CancellationToken)` | Returns the file with its `Chunks` collection eagerly loaded, or `null`. |

---

### `IPlaudApiService` / `PlaudApiService`
**Namespace:** `Phonematic.Services`

HTTP client wrapper for the PLAUD cloud API. Uses two separate `HttpClient` instances: one for API calls (with Bearer token and custom headers), and one for presigned download URLs (no extra headers).

**Properties**

| Property | Description |
|---|---|
| `bool IsAuthenticated` | `true` after `SetAuthToken` is called. |
| `Action<string>? LogCallback` | Optional delegate for structured log messages. |

**Methods**

| Method | Description |
|---|---|
| `void SetAuthToken(string token)` | Strips any `"Bearer "` prefix and sets the `Authorization` header. Sets `IsAuthenticated = true`. |
| `void ClearAuthToken()` | Removes the `Authorization` header and sets `IsAuthenticated = false`. |
| `Task<List<PlaudRecordingDto>> ListRecordingsAsync(CancellationToken)` | Pages through the recordings list endpoint (500 per page) until all are fetched. |
| `Task<string> GetDownloadUrlAsync(string fileId, CancellationToken)` | Returns the presigned temporary download URL for a file. |
| `Task DownloadFileAsync(string url, string destPath, IProgress<double>?, CancellationToken)` | Streams a download to `destPath`, writing first to a `.tmp` file. Retries up to 3 times. |

**`PlaudRecordingDto`**

| Property | Description |
|---|---|
| `FileId` | PLAUD unique file identifier. |
| `Title` | Recording filename / title. |
| `StartTime` | UTC `DateTime` (converted from epoch milliseconds). |
| `Duration` | Duration in seconds (API value is milliseconds). |
| `TagName` | Optional tag / folder name. |
| `FileSize` | File size in bytes (nullable). |

**`PlaudAuthException`** — thrown when the API returns a 401 Unauthorized response.

---

### `TokenListenerService`
**Namespace:** `Phonematic.Services`

An `HttpListener` server on `http://localhost:27839/` that receives the PLAUD Bearer token posted by the companion browser extension.

**Constants**

| Constant | Value |
|---|---|
| `Port` | `27839` |

**Events**

| Event | Description |
|---|---|
| `event Action<string>? TokenReceived` | Raised on the listening loop when a valid token is POSTed to `/plaud-token`. |

**Methods**

| Method | Description |
|---|---|
| `void Start()` | Starts the `HttpListener` and begins the background listen loop. Silently ignores port-in-use errors. |
| `void Dispose()` | Cancels the listen loop and stops the listener. |

---

## Data

### `PhonematicDbContext`
**Namespace:** `Phonematic.Data`

EF Core `DbContext` for the application's SQLite database.

**DbSets**

| Property | Entity |
|---|---|
| `DbSet<ProcessedFile> ProcessedFiles` | Transcribed audio files. |
| `DbSet<TranscriptionChunk> TranscriptionChunks` | Embedded text chunks. |
| `DbSet<PlaudRecording> PlaudRecordings` | PLAUD cloud recordings. |

**Configuration (via `OnModelCreating`)**

- `ProcessedFiles`: indexed on `FilePath`; unique index on `(FilePath, FileHash)`.
- `TranscriptionChunks`: FK to `ProcessedFiles` with cascade delete.
- `PlaudRecordings`: unique index on `PlaudFileId`.

When instantiated without `DbContextOptions`, defaults to `%LOCALAPPDATA%\Phonematic\Phonematic.db`.

---

## Helpers

### `FileHasher`
**Namespace:** `Phonematic.Helpers`

Static utility for computing file hashes.

| Method | Description |
|---|---|
| `static Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)` | Streams the file and returns a 64-character lowercase hex SHA-256 digest. |

---

### `AudioConverter`
**Namespace:** `Phonematic.Helpers`

Static utility for audio format normalisation and duration detection.

**Fields**

| Member | Value |
|---|---|
| `SupportedExtensions` | `.mp3`, `.wav`, `.aiff`, `.aif`, `.wma`, `.m4a`, `.ogg`, `.flac`, `.voc` |

**Methods**

| Method | Description |
|---|---|
| `static bool IsSupported(string filePath)` | Returns `true` if the file extension is in `SupportedExtensions`. |
| `static Task<string> ConvertToWavAsync(string audioPath, CancellationToken)` | Converts the audio to a 16kHz mono PCM WAV in `%TEMP%\Phonematic\`. Uses ffmpeg on Linux/macOS and NAudio on Windows. Returns the temp WAV path (caller is responsible for deleting it; `TranscriptionService` deletes in a `finally` block). |
| `static double GetDurationSeconds(string audioPath)` | Returns audio duration in seconds. Uses `ffprobe` on Linux/macOS and NAudio on Windows. |

---

## Converters

Avalonia `IValueConverter` implementations in `Phonematic.Converters`. All expose a static `Instance` singleton.

### `PercentageConverter`
Converts a `double` (0.0–1.0) to a percentage string (e.g. `0.75` → `"75%"`).

### `FileSizeConverter`
Converts a `long` byte count to a human-readable string (B / KB / MB / GB).

### `DurationConverter`
Converts a `double` seconds value to a formatted string (e.g. `3725.0` → `"1h 2m 5s"`).

### `InverseBoolConverter`
Inverts a `bool` value. Both `Convert` and `ConvertBack` return `!value`.

---

## ViewModels

### `ViewModelBase`
**Namespace:** `Phonematic.ViewModels`

Abstract base class. Extends `ObservableObject` from CommunityToolkit.Mvvm.

---

### `MainWindowViewModel`
**Namespace:** `Phonematic.ViewModels`

Shell ViewModel. Holds references to all tab ViewModels and controls setup visibility.

| Property | Type | Description |
|---|---|---|
| `CurrentView` | `ViewModelBase?` | The currently displayed view (observable). |
| `IsSetupRequired` | `bool` | When `true` the setup overlay is shown instead of the main tabs. |
| `SelectedTabIndex` | `int` | Currently selected tab (observable). |
| `Transcribe` | `TranscribeViewModel?` | Child ViewModel. |
| `Transcriptions` | `TranscriptionsViewModel?` | Child ViewModel. |
| `Search` | `SearchViewModel?` | Child ViewModel. |
| `Settings` | `SettingsViewModel?` | Child ViewModel. |
| `PlaudSync` | `PlaudSyncViewModel?` | Child ViewModel. |
| `Setup` | `SetupViewModel?` | Child ViewModel (only set when `IsSetupRequired`). |

---

### `TranscribeViewModel`
**Namespace:** `Phonematic.ViewModels`

Manages the Transcribe tab: file selection, batch transcription, and per-file progress.

| Observable | Type | Description |
|---|---|---|
| `InputPath` | `string` | Currently loaded file or folder path. |
| `CurrentFileProgress` | `double` | Progress (0–1) for the file currently being transcribed. |
| `OverallProgress` | `double` | Progress (0–1) across all files in the batch. |
| `StatusText` | `string` | Human-readable status message. |
| `IsTranscribing` | `bool` | `true` while `StartTranscriptionCommand` is running. |
| `CompletedCount` | `int` | Files successfully transcribed in the current batch. |
| `SkippedCount` | `int` | Files skipped (already processed). |
| `FailedCount` | `int` | Files that errored. |
| `Files` | `ObservableCollection<Mp3FileItem>` | List of discovered audio files. |

**Interaction delegates** (set by code-behind):

| Delegate | Description |
|---|---|
| `Func<Task>? BrowseFileInteraction` | Opens a platform file-picker dialog. |
| `Func<Task>? BrowseFolderInteraction` | Opens a platform folder-picker dialog. |

**Commands:** `BrowseFileCommand`, `BrowseFolderCommand`, `StartTranscriptionCommand` (cancellable).

`LoadFiles(string path)` — populates `Files` from a single audio file or an entire directory tree, saves `LastImportPath` to config.

---

### `TranscriptionsViewModel`
**Namespace:** `Phonematic.ViewModels`

Displays all previously transcribed files and shows the selected file's transcription text.

| Observable | Type | Description |
|---|---|---|
| `SelectedFile` | `ProcessedFile?` | Currently selected file; changing it loads the transcription text. |
| `TranscriptionText` | `string` | Content of the selected file's `.txt` transcript. |
| `ProcessedFiles` | `ObservableCollection<ProcessedFile>` | All transcribed files, newest first. |

**Commands:** `RefreshCommand` — reloads `ProcessedFiles` from the database.

---

### `SearchViewModel`
**Namespace:** `Phonematic.ViewModels`

Implements the RAG search UI: query input, vector search results, and streamed LLM answer.

| Observable | Type | Description |
|---|---|---|
| `QueryText` | `string` | The user's search query. |
| `LlmAnswer` | `string` | Streamed LLM answer, updated token-by-token. |
| `IsSearching` | `bool` | `true` while search or generation is in progress. |
| `IsLoadingLlm` | `bool` | `true` while the LLM is loading for the first time. |
| `StatusText` | `string` | Human-readable status. |
| `SelectedResult` | `SearchResultItem?` | Currently selected search result; changing it loads the source transcript. |
| `SourceTranscriptionText` | `string` | Full transcript of the selected result's source file. |
| `SourceHeaderText` | `string` | Header label showing the source file path. |
| `SearchResults` | `ObservableCollection<SearchResultItem>` | Top-K results from the last search. |

**Commands:** `SearchCommand` (cancellable).

---

### `SettingsViewModel`
**Namespace:** `Phonematic.ViewModels`

Allows the user to configure output directory, Whisper model size, thread count, and download individual models.

Notable observable properties: `OutputDirectory`, `SelectedWhisperModel`, `ThreadCount`, `WhisperModelStatus`, `OnnxModelStatus`, `LlmModelStatus`, and download-progress properties for each model.

`AppVersion` — read from the assembly version.
`MaxThreads` — `Environment.ProcessorCount`.
`WhisperModelSizes` — fixed `ObservableCollection<string>` of all supported size keys.

**Commands:** `SaveCommand`, `BrowseOutputDirectoryCommand`, `DownloadWhisperModelCommand`, `DownloadOnnxModelCommand`, `DownloadLlmModelCommand`.

---

### `SetupViewModel`
**Namespace:** `Phonematic.ViewModels`

First-run wizard that downloads all three required models.

| Observable | Type | Description |
|---|---|---|
| `WhisperProgress` / `OnnxProgress` / `LlmProgress` | `double` | Download progress per model (0–1). |
| `WhisperStatus` / `OnnxStatus` / `LlmStatus` | `string` | `"Pending"`, `"Downloading..."`, `"Ready"`, or `"Failed"`. |
| `StatusText` | `string` | Overall status message. |
| `IsDownloading` | `bool` | `true` while any download is in progress. |
| `CanRetry` | `bool` | `true` after a failure, enabling the retry button. |

**Commands:** `StartDownloadCommand` (cancellable), `SkipCommand`.

The `onSetupComplete` callback (injected via constructor) is invoked after all models are ready, setting `MainWindowViewModel.IsSetupRequired = false`.

---

### `PlaudSyncViewModel`
**Namespace:** `Phonematic.ViewModels`

Manages PLAUD device synchronisation: authentication, listing recordings, and batch downloading.

Key observable properties: `IsSyncing`, `IsDownloading`, `StatusText`, `TotalCount`, `DownloadedCount`, `PendingCount`, `FailedCount`, `OverallProgress`, `IsLoggedIn`, `ManualToken`, `DebugLogText`, `HasRecordings`, `MaxConcurrentDownloads`.

`Recordings` — `ObservableCollection<PlaudRecordingItem>` displayed in the UI.

`Log(string message)` — appends a timestamped line to `DebugLogText`.

Subscribes to `TokenListenerService.TokenReceived` for automatic login via the browser extension. On construction, restores any previously saved token from `AppConfig.PlaudToken`.
