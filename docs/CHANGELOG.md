# Changelog

All notable changes to Phonematic are documented here.

---

## [Unreleased]

### Added

#### Command-line interface (`Phonematic` console project)

- **Headless audio→PhoScript CLI.** The `Phonematic` console project is now a real CLI that
  converts a single audio file or a whole directory into `.phos` files using the acoustic
  pipeline. It is a **stateless file→file converter** — it does not touch the SQLite database
  or compute embeddings. See [CLI.md](CLI.md) for the full synopsis, flags, examples, and the
  exit-code table.
  - Single-file mode (`-o, --output <file>`) and directory mode (`--output-dir <dir>`,
    `-r/--recursive` with subfolder mirroring); `-f/--overwrite` and `-q/--quiet` in both modes.
  - Agent-friendly I/O: written `.phos` paths on **stdout**, progress bar + logs on **stderr**,
    Ctrl-C cancellation, and exit codes `0` success / `1` runtime failure / `2` usage error /
    `3` environment error (model missing — never auto-downloaded).
- **New dependencies (justified):** `System.CommandLine` for argument parsing/help/version and
  `Spectre.Console` for the progress bar. These are intentional additions under the `AGENTS.md`
  "avoid new dependencies" guideline, since standards-compliant CLI parsing and progress
  reporting are not reasonably hand-rolled. (The unused `Spectre.Console.Cli` placeholder
  reference was removed.)

#### Accurate words: forced alignment, Whisper hybrid, and CLI training

- **Forced alignment (`-t, --transcript <file>`).** When the exact words are known, the CLI now
  constrains decoding to those words via a Viterbi CTC forced aligner (`CtcForcedAligner`) instead
  of free-guessing. The resulting `.phos` carries the real words in `<word orth>` with audio-derived
  timing. In directory mode, a sibling `<name>.txt` next to each audio file is used automatically.
- **Whisper hybrid (`--whisper`, `--whisper-model <size>`).** For transcript-less audio, Whisper
  supplies the words and sentence segmentation (one `<sentence>` per segment) while wav2vec2
  forced-aligns the phones within each word. New `WhisperWordRecognizer` (lean, DB-free) reuses the
  already-referenced `Whisper.net`.
- **Speaker adaptation applied at inference (`--voice-model <file>`).** Trained `.phonematic`
  adapters are now actually used during recognition (`VoiceAdapter` re-derives logits from wav2vec2
  hidden states), improving transcript-less accuracy for the adapted speaker. Previously adapters
  were trained but never applied.
- **CLI training (`Phonematic train <pairs-dir> --output model.phonematic [--epochs N] [-r]`).**
  New database-free `AdapterTrainer` (lifted from the GUI's training core) trains a speaker adapter
  from many (audio, sibling-`.txt`) pairs; phone-label targets come from a new `PhoneTargetBuilder`
  that maps ARPAbet directly to TIMIT labels (no lossy IPA round-trip).
- **Supporting changes:** `PhoneRecognitionResult` now also exposes raw `Logits`; a new word-aware
  `PhoScriptWriter.Write` overload populates `orth` and emits multiple `<sentence>` blocks; the CMU
  dictionary / G2P / ARPAbet→IPA helpers (and `cmudict.dict`) and `TrainingProgress` moved into the
  shared `Phonematic` project. `TorchSharp-cpu`/`libtorch-cpu` were added to the console project for
  training and adapter inference.
  - *Note:* the GUI's `VoiceModelTrainingService` retains its own DB/feature-cache training path and
    was intentionally left unchanged; the shared `AdapterTrainer` powers the CLI.

#### Project restructure — shared acoustic pipeline

- **Renamed the Avalonia app project to `Phonematic.Gui`** (assembly `Phonematic.Gui`) and moved
  the shared, model-agnostic acoustic code into the `Phonematic` console project so both
  products share one implementation:
  - Helpers: `AudioConverter`, `CtcDecoder`, `TimitToIpa`, and the acoustic
    `PhoScriptWriter.Write` overload.
  - Services: `AcousticPhoneRecognizerService`, `AcousticFeatureExtractorService` (incl.
    `ComputeSpeakerBaseline`), `ConfigService`/`IConfigService`,
    `ModelManagerService`/`IModelManagerService`.
  - Models: `AppConfig`, `PhoneAlignment`, `AcousticFeatureFrame`, `SpeakerBaseline`,
    `PhoneRecognitionResult`.
- The Whisper-legacy IPA path is **not** shared: the `WriteLegacy` overload was split out into
  `PhoScriptWriterLegacy` in `Phonematic.Gui` (alongside `CmuDict`, `GraphemeToPhoneme`,
  `ArpabetToIpa`), since the acoustic pipeline does not use it.
- `Phonematic.Gui` now references `Phonematic` and resolves the migrated services through its
  existing DI; GUI behaviour is unchanged.

### Fixed

- **`TrainFileItem.Name`** now returns the file's base name without its extension (e.g.
  `sample`, not `sample.mp3`), matching the documented stem-based pairing of audio and `.phos`
  files. Fixes four failing `TrainViewModelTests`.

### Removed

- Deleted the stale, unreferenced duplicate test directory `src/tests/Phonematic.Tests`
  (an empty project file plus an out-of-date `PhoScriptWriterTests` copy). The canonical test
  project remains `tests/Phonematic.Tests`.

#### Acoustic Pipeline — new services and models

- **`AcousticFeatureExtractorService`** (`Services/AcousticFeatureExtractorService.cs`)  
  Extracts per-frame pitch (F0), RMS energy, zero-crossing rate, and harmonic-to-noise ratio from a 16 kHz mono WAV file using NWaves `PitchExtractor` and `TimeDomainFeaturesExtractor`. Returns `IReadOnlyList<AcousticFeatureFrame>`.

- **`AcousticPhoneRecognizerService`** (`Services/AcousticPhoneRecognizerService.cs`)  
  Runs a wav2vec2 ONNX model to produce phone-level alignments (`PhoneAlignment` records with IPA symbols and millisecond timestamps). TIMIT 44-phone vocabulary embedded internally.

- **`VoiceModelTrainingService`** (`Services/VoiceModelTrainingService.cs`)  
  Trains a two-layer TorchSharp adapter (Linear → ReLU → Dropout → Linear) on top of frozen wav2vec2 hidden states using CTC loss. Saves the best checkpoint to `voice_models/<id>/adapter.phonematic`.

- **`VoiceModelService`** / **`IVoiceModelService`**  
  CRUD service for `VoiceModel` entities. Lists, creates, renames, and deletes voice models and their associated training pairs.

- **`LlmService`** (`Services/LlmService.cs`)  
  Wraps LLamaSharp to generate streaming RAG answers using Microsoft Phi-3 Mini 4k (Q4 GGUF).

- **`EmbeddingService`** (`Services/EmbeddingService.cs`)  
  Generates 384-dimensional sentence embeddings with `all-MiniLM-L6-v2` via ONNX Runtime and stores chunked text in the database.

- **`TokenListenerService`** (`Services/TokenListenerService.cs`)  
  Listens for PLAUD Bearer tokens injected by the companion browser extension via a local HTTP endpoint.

#### New EF Core entities

- **`VoiceModel`** — stores user-created speaker adaptation models (name, path, training metadata).
- **`TrainingPair`** — stores `(audio path, transcript path, features-extracted flag)` pairs linked to a `VoiceModel` with cascade delete.

Both entities are registered in `PhonematicDbContext` with appropriate indexes.

#### `IModelManagerService` / `ModelManagerService` — wav2vec2 support

Three new members added to the interface and implementation:

| Member | Description |
|---|---|
| `bool IsWav2Vec2ModelDownloaded()` | Returns `true` if `acoustic/wav2vec2-phoneme.onnx` exists. |
| `string GetWav2Vec2ModelPath()` | Returns the absolute path to `acoustic/wav2vec2-phoneme.onnx`. |
| `Task DownloadWav2Vec2ModelAsync(IProgress<double>?, CancellationToken)` | Downloads the wav2vec2 ONNX model. No-ops if already present. |

`AreAllModelsReady` now also checks `IsWav2Vec2ModelDownloaded()`.

#### `IConfigService` / `ConfigService` — new directories

| Property | Path |
|---|---|
| `AcousticModelsDirectory` | `ModelsDirectory\acoustic` |
| `VoiceModelsDirectory` | `ModelsDirectory\voice_models` |

Both directories are created at startup by `EnsureDirectories()`.

#### `AppConfig` — new settings

| Property | Type | Default | Description |
|---|---|---|---|
| `TranscriptionBackend` | `string` | `"acoustic"` | Selects the active transcription backend (`"acoustic"` or `"whisper"`). |
| `UseGpuForTraining` | `bool` | `false` | When `true`, TorchSharp uses CUDA for voice model training. |

#### `PhoScriptWriter` — acoustic overload and `XmlWriter` rewrite

- **New primary overload** `Write(IReadOnlyList<PhoneAlignment>, IReadOnlyList<AcousticFeatureFrame>, SpeakerBaseline, string, string, DateOnly?)` — produces fully-annotated PhoScript with real per-phone prosodic data: `f0_hz`, `f0_rel_st`, `f0_onset_hz`, `f0_offset_hz`, `f0_contour`, `f0_rate_st_per_ms`, `intensity_db`, `intensity_rel`, `voice_quality`, `coart_lead`, `coart_lag`.

- **All XML emission** in both overloads rewritten from `StringBuilder` string interpolation to `System.Xml.XmlWriter` with shared `XmlWriterSettings` (2-space indent, LF line endings, no XML declaration, fragment conformance). This guarantees well-formed output and correct attribute escaping without any manual `Replace` calls.

- New internal helpers: `GetFramesForPhone`, `ClassifyContour`, `ComputeF0RateStPerMs`, `ClassifyVoiceQuality`, `ClassifyCoartLead`, `ClassifyCoartLag`, `GroupPhonesIntoWords`.

#### Test project

- `NWaves 0.9.6`, `MathNet.Numerics 5.0.0`, and `Microsoft.Bcl.Memory 10.0.7` added as test dependencies (needed to compile service tests that reference NWaves and MathNet types).

---

### Changed

#### `PhoScriptWriter.WriteLegacy` (renamed from `Write`)

The original Whisper-based `Write(IReadOnlyList<SegmentData>, ...)` overload has been renamed to `WriteLegacy` to make room for the new acoustic primary overload. The `[Obsolete]` attribute was removed because the method remains the correct path for Whisper-only output. All callers have been updated:

- `TranscriptionService.TranscribeAsync` — updated to call `WriteLegacy`.
- `PhoScriptWriterTests` — all 11 test calls updated to `WriteLegacy`.

#### `AcousticFeatureExtractorService` — correct NWaves 0.9.6 types

Replaced non-existent types with their actual NWaves 0.9.6 equivalents:

| Before | After |
|---|---|
| `PitchExtractorOptions` | `PitchOptions` |
| `YinPitchExtractor` | `PitchExtractor` |
| `EnergyExtractor` | `TimeDomainFeaturesExtractor` |

`TimeDomainFeaturesExtractor` outputs `[energy, rms, zcr, entropy]` per frame; ZCR (index 2) is now read directly from the extractor output, removing the hand-rolled `ComputeZcr` helper.

#### Avalonia XAML — deprecated `Watermark` replaced

`TextBox.Watermark` (deprecated in Avalonia 11.1) replaced with `TextBox.PlaceholderText` in three views:

| View | Placeholder text |
|---|---|
| `PlaudSyncView.axaml` | `"Paste token here"` |
| `SearchView.axaml` | `"Ask a question about your transcriptions..."` |
| `TranscribeView.axaml` | `"Enter file or folder path..."` |

---

### Fixed

#### Build errors resolved

| Error | Fix |
|---|---|
| `CS0101` — duplicate `PhoScriptWriter` class | Removed the duplicate class body that was left in the file after the acoustic overload was prepended. |
| `CS0111` — duplicate `GetIpaPhones`, `SplitWords`, `Escape` members | Same root cause as above; resolved by the duplicate class removal. |
| `CS0535` — `ModelManagerService` did not implement `IsWav2Vec2ModelDownloaded`, `GetWav2Vec2ModelPath`, `DownloadWav2Vec2ModelAsync` | Implemented all three members. |
| `CS0246` — `Sequential`, `CTCLoss` not found in `VoiceModelTrainingService` | Corrected to `TorchSharp.Modules.Sequential` and `torch.nn.CTCLoss(...)`. |
| `CS0246` — `PitchExtractorOptions`, `YinPitchExtractor`, `EnergyExtractor` not found | Replaced with correct NWaves 0.9.6 types (see above). |
| `CS7036` — `PhoScriptWriter.Write` missing `baseline` argument in `TranscriptionService` | Caller updated to `WriteLegacy`. |
| `CS0103` — `log_softmax` not in scope | Replaced with `torch.nn.functional.log_softmax(...)`. |

#### Build warnings resolved

| Warning | Fix |
|---|---|
| `CS8632` — nullable annotations without nullable context (100+ sites) | Added `<Nullable>enable</Nullable>` to `Phonematic.csproj`. |
| `CS0618` — `WriteLegacy` is obsolete | Removed `[Obsolete]` attribute. |
| `AVLN5001` — `TextBox.Watermark` is obsolete | Replaced with `PlaceholderText` in all three AXAML views. |
| `xUnit1051` — `CancellationToken` not from `TestContext` (14 sites) | Passed `TestContext.Current.CancellationToken` via a shared `CT` property in `DbContextTests` and `FileHasherTests`. |

---

### Infrastructure

#### `Phonematic.csproj`

- Added `<Nullable>enable</Nullable>` — enables C# nullable reference types project-wide.

#### `Phonematic.Tests.csproj`

- Added package references: `NWaves 0.9.6`, `MathNet.Numerics 5.0.0`, `Microsoft.Bcl.Memory 10.0.7`.
