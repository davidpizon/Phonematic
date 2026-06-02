# Architecture

This document describes the high-level architecture of Phonematic — a local-first audio
transcription and search system on .NET 10, shipped as both an Avalonia UI desktop app and a
headless command-line converter.

See also:
- [API.md](API.md) — full reference for all classes, interfaces, and records mentioned here
- [CLI.md](CLI.md) — command-line interface usage, flags, and exit codes
- [CHANGELOG.md](CHANGELOG.md) — history of all notable changes
- [CONTRIBUTING.md](CONTRIBUTING.md) — development workflow and coding standards
- [TESTING.md](TESTING.md) — test suite structure and patterns
- [PHOSCRIPT.md](PHOSCRIPT.md) — PhoScript 1.0 specification (`.phos` output format produced by `PhoScriptWriter`)
- [IPA_REFERENCE.md](IPA_REFERENCE.md) — IPA symbol reference used in PhoScript output

## Solution Projects

The solution (`src/Phonematic.slnx`) contains two product projects plus tests:

| Project | Assembly | Role |
|---|---|---|
| `Phonematic` | `Phonematic.dll`/`.exe` | Headless **CLI** (`audio → .phos`) **and** the shared acoustic pipeline + config/model services |
| `Phonematic.Gui` | `Phonematic.Gui.dll`/`.exe` | Avalonia desktop app (formerly the `Phonematic` project); references `Phonematic` |
| `Phonematic.Tests` | `Phonematic.Tests.dll` | xUnit v3 test project; references both |

The shared, model-agnostic acoustic code lives in the **`Phonematic`** console project:
`AudioConverter`, `CtcDecoder`, `TimitToIpa`, the acoustic `PhoScriptWriter.Write` overload,
`AcousticPhoneRecognizerService`, `AcousticFeatureExtractorService`, plus `ConfigService` and
`ModelManagerService` (for locating/checking models). The Whisper-legacy IPA path —
`CmuDict`, `GraphemeToPhoneme`, `ArpabetToIpa`, and `PhoScriptWriterLegacy.WriteLegacy` — stays
in `Phonematic.Gui`, since the acoustic pipeline does not use it.

The **CLI** is a stateless file→file converter: it does **not** write to SQLite or compute
embeddings. Per file it runs `AudioConverter.ConvertToWavAsync` →
`AcousticPhoneRecognizerService.RecognizeAsync` → `AcousticFeatureExtractorService` (frames +
baseline) → `PhoScriptWriter.Write`. Argument parsing uses `System.CommandLine`; the progress
bar uses `Spectre.Console` (rendered to stderr, with result paths on stdout). See
[CLI.md](CLI.md) for flags and exit codes.

## Technology Stack

| Layer | Technology |
|---|---|
| UI framework | [Avalonia UI](https://avaloniaui.net/) (cross-platform WPF-style) |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Database | SQLite via [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) |
| Speech-to-text | [Whisper.net](https://github.com/sandrohanea/whisper.net) (OpenAI Whisper, GGML models) |
| Acoustic analysis | NWaves 0.9.6 (`PitchExtractor`, `TimeDomainFeaturesExtractor`) + NAudio |
| Phone recognition | ONNX Runtime — wav2vec2 TIMIT phone model |
| Speaker adaptation | [TorchSharp](https://github.com/dotnet/TorchSharp) — CTC-trained 2-layer adapter |
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
│  AcousticFeatureExtractorService                      │
│  AcousticPhoneRecognizerService                       │
│  VoiceModelTrainingService · VoiceModelService        │
│  VectorSearchService · LlmService                     │
│  FileTrackingService · PlaudApiService                │
│  TokenListenerService                                 │
├───────────────────────────────────────────────────────┤
│               Data / Helpers / Models                 │
│  PhonematicDbContext (EF Core + SQLite)               │
│  AudioConverter · FileHasher                          │
│  ArpabetToIpa · CmuDict · GraphemeToPhoneme           │
│  PhoScriptWriter · CtcDecoder                         │
│  AppConfig · ProcessedFile · TranscriptionChunk       │
│  PlaudRecording · VoiceModel · TrainingPair           │
└───────────────────────────────────────────────────────┘
```

## Dependency Injection

All services are composed in `App.axaml.cs → ConfigureServices`. The DI container is `Microsoft.Extensions.DependencyInjection` and the root `ServiceProvider` is stored in `App.Services`.

`MainWindowViewModel` is registered as a **singleton**. Per-tab ViewModels (`ModelViewModel`, `TranscribeViewModel`, `TranscriptionsViewModel`, `TrainViewModel`, `SearchViewModel`, `SettingsViewModel`, `PlaudSyncViewModel`) are registered as **transients** and resolved once during `OnFrameworkInitializationCompleted`. Views receive their ViewModel via `DataContext` set in `App.OnFrameworkInitializationCompleted`.

## Core Data Flow

### Transcription Pipeline

Two backends are supported, selected via `AppConfig.TranscriptionBackend`.

#### Acoustic backend (`"acoustic"`, default)

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
AcousticPhoneRecognizerService.RecognizeAsync  ← wav2vec2 ONNX → PhoneAlignment[]
        ↓
AcousticFeatureExtractorService.ExtractFramesAsync  ← NWaves → AcousticFeatureFrame[]
        ↓
AcousticFeatureExtractorService.ComputeSpeakerBaseline
        ↓
PhoScriptWriter.Write                  ← fully-annotated .phos (XmlWriter)
        ↓
FileTrackingService.RecordTranscriptionAsync  ← persist to DB
        ↓
EmbeddingService.StoreChunksAsync      ← chunk → ONNX embed → DB
```

#### Legacy Whisper backend (`"whisper"`)

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
TranscriptionService.TranscribeAsync   ← Whisper.net → SegmentData[]
        ↓
PhoScriptWriter.WriteLegacy            ← timing-approximated .phos (XmlWriter)
  ├─ CmuDict.TryGetPhones              ← primary pronunciation lookup
  └─ GraphemeToPhoneme.Convert         ← rule-based G2P fallback
       └─ ArpabetToIpa.Convert         ← ARPAbet → slash-delimited IPA
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

VoiceModels
  Id                    INTEGER PK
  Name                  TEXT (indexed)
  ModelPath             TEXT
  CreatedAtUtc          TEXT
  LastTrainedAtUtc      TEXT
  BestPhoneErrorRate    REAL

TrainingPairs
  Id                    INTEGER PK
  VoiceModelId          INTEGER FK → VoiceModels.Id (CASCADE DELETE)
  AudioPath             TEXT
  TranscriptPath        TEXT
  FeaturesExtracted     INTEGER
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
│   ├── llm\
│   │   └── phi-3-mini-4k-instruct-q4.gguf
│   └── acoustic\
│       └── wav2vec2-phoneme.onnx  ← wav2vec2 TIMIT phone model
├── voice_models\
│   └── <id>\
│       └── adapter.phonematic ← trained TorchSharp adapter checkpoint
├── Phonematic.db              ← SQLite database
└── transcription.log          ← append-only log
```

Transcribed text files are written to the user-configurable `OutputDirectory` (default: `~/Documents/Phonematic/`).

## Embedding Strategy

Text is split into overlapping sentence-level chunks (`ChunkSize` / `ChunkOverlap` tokens, configurable). Each chunk is encoded with `all-MiniLM-L6-v2` (384-dimensional, L2-normalised) via ONNX Runtime with a hand-rolled WordPiece tokeniser. Embeddings are stored as raw `float32` blobs in SQLite.

Retrieval is brute-force cosine similarity over all stored chunks, then top-K results are passed to the LLM as context (Retrieval-Augmented Generation).

## IPA Resolution Strategy

Every `<phon>` element in a `.phos` output file carries an `ipa` attribute with a slash-delimited IPA symbol (e.g. `/k/`, `/æ/`, `/ʃ/`). The resolution order is:

1. **`CmuDict`** — the CMU Pronouncing Dictionary (~134 000 entries, embedded as a resource) is queried first. It returns an ARPAbet phone array for the word (case-insensitive, punctuation stripped).
2. **`GraphemeToPhoneme`** — if the word is not in the dictionary, a rule-based English G2P engine applies an ordered set of compiled `Regex` rules (digraphs → magic-e → vowel clusters → single-letter defaults) to produce ARPAbet phones.
3. **`ArpabetToIpa`** — in both paths, every ARPAbet symbol is converted to its IPA equivalent via a static lookup table. Stress digits are stripped; output is always enclosed in `/` per PhoScript convention.

Because Whisper provides only segment-level timestamps, phone-level timing inside each `<word>` is a uniform distribution of the word's time budget across its phones. This is an approximation; forced-alignment would be required for millisecond-accurate per-phone timestamps.

## Setup Flow

On first launch, `ModelManagerService.AreAllModelsReady` returns `false`. The app shows `SetupView`, which calls `ModelManagerService.Download*Async` for each model sequentially. After all three models are present, `MainWindowViewModel.IsSetupRequired` is set to `false` and the main tabs become accessible.
