# Architecture

This document describes the high-level architecture of Phonematic — a local-first audio transcription and search desktop application built with Avalonia UI on .NET 10.

## Technology Stack

| Layer | Technology |
|---|---|
| UI framework | [Avalonia UI](https://avaloniaui.net/) (cross-platform WPF-style) |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Database | SQLite via [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) |
| Speech-to-text | [Whisper.net](https://github.com/sandrohanea/whisper.net) (OpenAI Whisper, GGML models) |
| Embedding model | ONNX Runtime — `all-MiniLM-L6-v2` (sentence-transformers) |
| LLM | [LLamaSharp](https://github.com/SciSharp/LLamaSharp) — Microsoft Phi-3 Mini 4k (Q4 GGUF) |
| Audio decoding | NAudio (Windows) / ffmpeg (Linux/macOS) |
| DI container | `Microsoft.Extensions.DependencyInjection` |

## Application Layers

```
┌───────────────────────────────────────────────────────┐
│                   Views (Avalonia XAML)               │
│  MainWindow · Transcribe · Transcriptions · Search    │
│  Settings · Setup · PlaudSync                         │
├───────────────────────────────────────────────────────┤
│                    ViewModels (MVVM)                  │
│  MainWindowViewModel · TranscribeViewModel            │
│  TranscriptionsViewModel · SearchViewModel            │
│  SettingsViewModel · SetupViewModel                   │
│  PlaudSyncViewModel                                   │
├───────────────────────────────────────────────────────┤
│                      Services                         │
│  ConfigService · ModelManagerService                  │
│  TranscriptionService · EmbeddingService              │
│  VectorSearchService · LlmService                     │
│  FileTrackingService · PlaudApiService                │
│  TokenListenerService                                 │
├───────────────────────────────────────────────────────┤
│               Data / Helpers / Models                 │
│  PhonematicDbContext (EF Core + SQLite)               │
│  AudioConverter · FileHasher                          │
│  AppConfig · ProcessedFile · TranscriptionChunk       │
│  PlaudRecording                                       │
└───────────────────────────────────────────────────────┘
```

## Dependency Injection

All services are composed in `App.axaml.cs → ConfigureServices`. The DI container is `Microsoft.Extensions.DependencyInjection` and the root `ServiceProvider` is stored in `App.Services`.

ViewModels are registered as **singletons** and resolved directly from the container. Views receive their ViewModel via `DataContext` set in `App.OnFrameworkInitializationCompleted`.

## Core Data Flow

### Transcription Pipeline

```
User selects audio file(s)
        ↓
TranscribeViewModel.StartTranscriptionAsync
        ↓
FileHasher.ComputeSha256Async          ← dedup check
        ↓
FileTrackingService.IsFileProcessedAsync
        ↓  (if not already processed)
AudioConverter.ConvertToWavAsync       ← 16kHz mono WAV
        ↓
TranscriptionService.TranscribeAsync   ← Whisper.net
        ↓
FileTrackingService.RecordTranscriptionAsync  ← persist to DB
        ↓
EmbeddingService.StoreChunksAsync      ← chunk → ONNX embed → DB
```

### RAG Search Pipeline

```
User enters query
        ↓
SearchViewModel.SearchAsync
        ↓
EmbeddingService.GenerateEmbedding(query)
        ↓
VectorSearchService.SearchAsync        ← cosine similarity over all chunks
        ↓
LlmService.GenerateAnswerAsync         ← Phi-3 Mini with RAG prompt
        ↓
Streamed answer tokens → UI
```

### PLAUD Sync Pipeline

```
User provides Bearer token (manual paste or browser extension)
        ↓
PlaudApiService.SetAuthToken
        ↓
PlaudSyncViewModel.SyncRecordingsAsync
        ↓
PlaudApiService.ListRecordingsAsync    ← paginated REST API
        ↓
PlaudApiService.GetDownloadUrlAsync    ← presigned cloud URL
        ↓
PlaudApiService.DownloadFileAsync      ← streamed download with progress
        ↓
Records stored in PlaudRecordings table
```

## Database Schema

The SQLite database is stored at `%LOCALAPPDATA%\Phonematic\Phonematic.db`. EF Core migrations manage the schema.

```
ProcessedFiles
  Id                    INTEGER PK
  FilePath              TEXT (indexed)
  FileHash              TEXT
  FileSizeBytes         INTEGER
  TranscriptionPath     TEXT
  TranscribedAtUtc      TEXT
  WhisperModel          TEXT
  AudioDurationSeconds  REAL
  TranscriptionDurationSeconds REAL
  UNIQUE (FilePath, FileHash)

TranscriptionChunks
  Id                    INTEGER PK
  ProcessedFileId       INTEGER FK → ProcessedFiles.Id (CASCADE DELETE)
  ChunkIndex            INTEGER
  Text                  TEXT
  Embedding             BLOB  (384 × float32, L2-normalised)

PlaudRecordings
  Id                    INTEGER PK
  PlaudFileId           TEXT (UNIQUE)
  Title                 TEXT
  RecordedAtUtc         TEXT
  DurationSeconds       REAL
  FolderName            TEXT
  LocalFilePath         TEXT
  IsDownloaded          INTEGER
  DownloadedAtUtc       TEXT
  FileSizeBytes         INTEGER
```

## File System Layout

All runtime data is stored under `%LOCALAPPDATA%\Phonematic\`:

```
%LOCALAPPDATA%\Phonematic\
├── config\
│   └── settings.json          ← AppConfig (JSON)
├── models\
│   ├── whisper\
│   │   └── ggml-<size>.bin    ← Whisper GGML model(s)
│   ├── onnx\
│   │   ├── model.onnx         ← all-MiniLM-L6-v2
│   │   └── vocab.txt
│   └── llm\
│       └── phi-3-mini-4k-instruct-q4.gguf
├── Phonematic.db              ← SQLite database
└── transcription.log          ← append-only log
```

Transcribed text files are written to the user-configurable `OutputDirectory` (default: `~/Documents/Phonematic/`).

## Embedding Strategy

Text is split into overlapping sentence-level chunks (`ChunkSize` / `ChunkOverlap` tokens, configurable). Each chunk is encoded with `all-MiniLM-L6-v2` (384-dimensional, L2-normalised) via ONNX Runtime with a hand-rolled WordPiece tokeniser. Embeddings are stored as raw `float32` blobs in SQLite.

Retrieval is brute-force cosine similarity over all stored chunks, then top-K results are passed to the LLM as context (Retrieval-Augmented Generation).

## Setup Flow

On first launch, `ModelManagerService.AreAllModelsReady` returns `false`. The app shows `SetupView`, which calls `ModelManagerService.Download*Async` for each model sequentially. After all three models are present, `MainWindowViewModel.IsSetupRequired` is set to `false` and the main tabs become accessible.
