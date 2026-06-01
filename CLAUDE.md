# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Full architectural detail, public API reference, data flows, DB schema, and file layout live in **docs/**. Read those documents for depth on any area below.

---

## Platform & Stack

- **Language:** C# 13, **.NET 10**, nullable enabled, implicit usings
- **UI:** Avalonia UI 12.0.2 (cross-platform, XAML + code-behind)
- **MVVM:** CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- **Database:** SQLite via EF Core 10 (`PhonematicDbContext`), migrations in `src/Phonematic/Migrations/`
- **DI:** `Microsoft.Extensions.DependencyInjection`, composed in `App.axaml.cs → ConfigureServices`
- **Tests:** xUnit in `tests/Phonematic.Tests/`

Key AI/ML packages: `Whisper.net`, `Microsoft.ML.OnnxRuntime`, `LLamaSharp`, `NWaves 0.9.6`, `TorchSharp-cpu`

---

## Build & Test Commands

```bash
dotnet build                                          # build solution
dotnet test                                           # run all tests
dotnet run --project src/Phonematic/Phonematic.csproj # run app

# EF Core migrations
dotnet ef migrations add <Name> --project src/Phonematic
dotnet ef database update --project src/Phonematic
```

---

## Architecture

### Layers (top to bottom)

```
Views (Avalonia XAML)  →  ViewModels (MVVM)  →  Services  →  Data / Helpers / Models
```

### Key Services

| Service | Responsibility |
|---|---|
| `TranscriptionService` | Whisper.net → `SegmentData[]` (legacy backend) |
| `AcousticPhoneRecognizerService` | wav2vec2 ONNX → `PhoneAlignment[]` (acoustic backend) |
| `AcousticFeatureExtractorService` | NWaves pitch + timing → `AcousticFeatureFrame[]` + speaker baseline |
| `VoiceModelTrainingService` | TorchSharp CTC adapter training |
| `PhoScriptWriter` | Produces `.phos` XML output (full or legacy) |
| `EmbeddingService` | all-MiniLM-L6-v2 ONNX → 384-dim embeddings → SQLite |
| `VectorSearchService` | Brute-force cosine similarity over stored chunks |
| `LlmService` | LLamaSharp Phi-3 Mini Q4 streaming RAG answers |
| `PlaudApiService` | PLAUD REST API — list, presigned URL, download |
| `FileTrackingService` | SHA-256 dedup + DB persistence of transcriptions |
| `ModelManagerService` | Model download and readiness check |
| `ConfigService` | `AppConfig` JSON persistence |

### Transcription Backends

Two backends are selectable via `AppConfig.TranscriptionBackend`:

- **`"acoustic"` (default):** wav2vec2 ONNX → phone alignments → NWaves acoustic features → `PhoScriptWriter.Write`
- **`"whisper"` (legacy):** Whisper.net segments → CmuDict/G2P IPA lookup → `PhoScriptWriter.WriteLegacy`

### IPA Resolution Order (Whisper backend / legacy path)
1. `CmuDict` — CMU Pronouncing Dictionary (~134k entries, embedded resource)
2. `GraphemeToPhoneme` — rule-based regex G2P fallback
3. `ArpabetToIpa` — static ARPAbet → IPA lookup table

### DI Lifetimes

| Lifetime | Services |
|---|---|
| `Singleton` | `IConfigService`, `IModelManagerService`, `TokenListenerService`, `IPlaudApiService`, all ViewModels |
| `Transient` | `IFileTrackingService` |
| `Scoped` | `PhonematicDbContext` (via `AddDbContextFactory`) |

### Runtime Data Location

All runtime data: `%LOCALAPPDATA%\Phonematic\`
- `config/settings.json` — `AppConfig`
- `models/whisper/`, `models/onnx/`, `models/llm/`, `models/acoustic/` — AI model files
- `voice_models/<id>/adapter.phonematic` — TorchSharp adapter checkpoints
- `Phonematic.db` — SQLite database
- Transcribed output: `~/Documents/Phonematic/` (user-configurable)

---

## Key Conventions

- All public async methods: `async Task`/`async Task<T>` with `CancellationToken ct = default`
- Long-running progress: `IProgress<double>?` (0.0–1.0 range)
- Interfaces live alongside implementations in `Services/` — `I<ServiceName>` naming
- ViewModels extend `ViewModelBase` (→ `ObservableObject`); use `[ObservableProperty]` / `[RelayCommand]`
- Never reference Avalonia UI types inside a ViewModel — use interaction delegate callbacks
- Services holding unmanaged resources (ONNX, LLaMA, Whisper) must implement `IDisposable`
- No XML doc comments unless explaining non-obvious logic
- Minimal changes — avoid unrelated refactoring

---

## Adding New Code

**New service:** `IMyService.cs` + `MyService.cs` in `Services/` → register in `ConfigureServices`  
**New view:** AXAML + code-behind in `Views/` + matching ViewModel in `ViewModels/` → wire on `MainWindowViewModel`  
**New DB column:** `dotnet ef migrations add <Name>` — never hand-edit migration files  
**New test:** add to `tests/Phonematic.Tests/`; skip tests needing model files or use temp files

---

## Context Navigation (Graphify)

### 3-Layer Query Rule
1. Query `graphify-out/graph.json` for code structure (classes, methods, call chains, service dependencies)
2. Query `docs/ARCHITECTURE.md` and `docs/API.md` for design rationale and full public API reference
3. Read raw `.cs` files only when making edits

### When to rebuild
- After adding/removing classes or significant refactors: `graphify update .`
- Graph persists across sessions — no rebuild per session needed

### Do NOT
- Manually edit anything inside `graphify-out/`
- Re-read the entire codebase if the graph already has the answer

### Vault session commands
- `/resume` — reads recent `~/vault/Phonematic/logs/` entries + `architecture/decisions.md`
- `/save` — writes a new session log to `~/vault/Phonematic/logs/YYYY-MM-DD-description.md`

