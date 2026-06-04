# Agents

This document describes the AI agents and coding guidelines for contributors and automated agents working on the Phonematic project.

## Documentation

All project documentation lives in the `/docs` folder. Refer to these documents when working on related areas:

| Document | Description |
|---|---|
| [API.md](API.md) | Full reference for all public classes, interfaces, records, and ViewModels |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture, data flows, DB schema, and file layout |
| [CLI.md](CLI.md) | Command-line interface: synopsis, flags, examples, and exit codes |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Development workflow, coding standards, and PR checklist |
| [TESTING.md](TESTING.md) | Test suite structure, patterns, and how to run tests |
| [PHOSCRIPT.md](PHOSCRIPT.md) | PhoScript 1.0 specification — the application's prosodic markup output format |
| [IPA_REFERENCE.md](IPA_REFERENCE.md) | International Phonetic Alphabet reference — consonants, vowels, diacritics |

## Application Purpose

Phonematic is a desktop application for creating exportable AI voice models that replicate the prosody of a specific human voice.

### Core Workflow

1. **Model management** — The user creates a new empty voice model or imports an existing model file (`.phonematic` format).
2. **Training input** — The user imports two paired files: a plain-text transcript and an audio recording of a human voice reading that exact transcript.
3. **Model training** — The application analyses the recording against the transcript to learn the speaker's full prosody — pitch contour, timing, stress, rhythm, and intonation — and incorporates that information into the model.
4. **Export** — The trained model can be exported so it can be shared, archived, or loaded into other tools.
5. **PhoScript output** — The model produces a PhoScript file (`.phos`) that encodes every spoken word together with its complete prosodic annotation, ready for use by downstream synthesis or analysis pipelines.

### Key Concepts

| Term | Meaning |
|---|---|
| Voice model | A trained artefact that captures the acoustic and prosodic characteristics of a single speaker |
| Prosody | The suprasegmental features of speech: pitch, loudness, duration, rhythm, and intonation |
| PhoScript | The application's native output format; a structured representation of spoken text with full prosodic markup |
| Training pair | A matched (transcript text file, audio recording) pair used to train or refine a voice model |

## General Guidelines

- Follow existing code style and conventions found throughout the codebase.
- Target **.NET 10** for all projects unless otherwise specified.
- Make minimal changes to achieve the goal; avoid unnecessary refactoring.
- Do not add comments unless they match existing comment style or explain complex logic.
- Use existing libraries whenever possible; avoid adding new dependencies unless absolutely necessary.
- Validate all changes by building the solution and running relevant tests before considering a task complete.

## Project Structure

```
Phonematic/                    ← Solution root (Phonematic.slnx)
├── src/
│   ├── Phonematic/            ← Console CLI + shared services (.NET 10) → Phonematic.dll/exe
│   │   ├── Program.cs         ← CLI entry point (System.CommandLine)
│   │   ├── Cli/               ← CLI orchestration (CliRunner, options, path resolver, progress)
│   │   ├── Helpers/           ← Shared static helpers (AudioConverter, CtcDecoder, TimitToIpa, PhoScriptWriter, CtcForcedAligner, PhoneTargetBuilder, CmuDict, GraphemeToPhoneme, ArpabetToIpa, PhoScriptWriterLegacy)
│   │   ├── Models/            ← Shared data models (AppConfig, PhoneAlignment, AcousticFeatureFrame, SpeakerBaseline, …)
│   │   └── Services/          ← Shared services (ConfigService, ModelManagerService, acoustic recognizer + feature extractor, Transcription, AdapterTrainer, VoiceAdapter, VoiceModelBundle, WhisperWordRecognizer)
│   └── Phonematic.Gui/        ← Avalonia GUI (.NET 10) → Phonematic.Gui.dll/exe; references Phonematic
│       ├── App.axaml.cs       ← DI composition root and app bootstrap
│       ├── Program.cs         ← Entry point, fatal error handling
│       ├── Converters/        ← Avalonia IValueConverter implementations
│       ├── Data/              ← EF Core DbContext
│       ├── Helpers/           ← GUI-only helpers (FileHasher)
│       ├── Migrations/        ← EF Core migration files
│       ├── Models/            ← GUI-only data models (ProcessedFile, VoiceModel, etc.)
│       ├── Services/          ← GUI-only services (Embedding, LLM, Plaud, VoiceModelService, ActiveVoiceModel, …)
│       ├── ViewModels/        ← MVVM ViewModels (CommunityToolkit.Mvvm)
│       └── Views/             ← Avalonia XAML views and code-behind
├── tests/
│   └── Phonematic.Tests/      ← xUnit v3 test project (references both projects)
└── docs/                      ← Project documentation (this folder)
```

- The **`Phonematic`** project is the headless console **CLI** (`audio → .phos`) and also owns the
  shared acoustic pipeline + config/model services. The **`Phonematic.Gui`** project is the Avalonia
  desktop app (formerly named `Phonematic`); it references `Phonematic` and resolves the shared
  services through its existing DI.
- The acoustic pipeline produces PhoScript output with the `.phos` extension.
- Source code lives under the solution root at `C:\git\Phonematic\`.
- Documentation lives in the `/docs` folder; CLI usage is in [CLI.md](CLI.md).

## Coding Standards

- Use idiomatic C# and follow the conventions already present in the file being edited.
- Prefer `async`/`await` for asynchronous code.
- Use `CancellationToken` parameters (named `ct`) on all async public methods.
- Use `IProgress<double>` for long-running operations that report percentage (0.0–1.0).
- Interfaces live alongside their implementations in `Services/` — name them `I<ServiceName>`.
- ViewModels use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm; avoid manual `INotifyPropertyChanged` boilerplate.
- Services that hold unmanaged resources (ONNX sessions, LLaMA models, Whisper processors) must implement `IDisposable`.
- Keep pull requests focused and scoped to a single concern.

## Dependency Injection

All services are registered in `App.axaml.cs → ConfigureServices`. Follow these lifetime rules:

| Lifetime | Used for |
|---|---|
| `Singleton` | `IConfigService`, `IModelManagerService`, `IActiveVoiceModelService`, `TokenListenerService`, `IPlaudApiService`, `MainWindowViewModel` |
| `Transient` | `IFileTrackingService`, per-tab ViewModels (`ModelViewModel`, `TranscribeViewModel`, `TranscriptionsViewModel`, `TrainViewModel`, `SearchViewModel`, `SettingsViewModel`, `PlaudSyncViewModel`) |
| `Scoped / Factory` | `PhonematicDbContext` (via `AddDbContextFactory`) |

## Adding a New Service

1. Define `IMyService` in `Services/IMyService.cs`.
2. Implement `MyService : IMyService` in `Services/MyService.cs`.
3. Register in `App.axaml.cs → ConfigureServices`.
4. Add unit tests in `tests/Phonematic.Tests/`.

## Adding a New View

1. Create the AXAML + code-behind pair in `Views/`.
2. Create a matching ViewModel in `ViewModels/` that extends `ViewModelBase`.
3. Wire the ViewModel as a property on `MainWindowViewModel` and register it in DI.

## Testing

- Run all relevant tests after making changes to verify nothing is broken.
- Add tests for new functionality where appropriate.
- Tests that require model files or real I/O should be skipped or use temp files.
- `ChunkTextTests` demonstrates the "testable subclass" pattern for services that don't need model dependencies in every test path.

## Branching

- The default branch is `main`.
- Feature work should be done on a dedicated branch and submitted via pull request.
