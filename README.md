# Phonematic

**The Enterprise-Grade Audio Transcription Solution You Didn't Know You Needed™**

A fully local, privacy-first desktop application that transcribes audio files with [OpenAI Whisper](https://github.com/openai/whisper), stores searchable vector embeddings, and answers questions about your recordings using an on-device LLM — no cloud, no subscriptions, no "we updated our privacy policy" emails.

---

## Features

- **Audio Transcription** — Converts MP3, WAV, M4A, FLAC, OGG, and more to timestamped text using Whisper AI. Yes, it actually works.
- **Semantic Search** — Queries your transcriptions using natural language via a 384-dimensional `all-MiniLM-L6-v2` embedding model and cosine similarity. Ask questions. Get answers. Revolutionary.
- **RAG-Powered Q&A** — Top matching chunks are fed as context to a local Phi-3 Mini LLM that streams a grounded answer token-by-token.
- **PLAUD Sync** — Downloads recordings from your PLAUD device directly into the transcription queue. Authenticate via the companion browser extension or by pasting a Bearer token.
- **Fully Local** — All AI inference runs on your machine. Your audio files never leave your premises. Your IT security team can finally sleep at night.
- **Cross-Platform** — Avalonia UI on Windows, Linux, and macOS. We believe in equality of suffering.
- **SQLite Database** — Transcriptions and embeddings stored locally in a format that will outlive most startups.
- **Deduplication** — SHA-256 file hashing prevents re-transcribing files you have already processed.

## System Requirements

| Component | Minimum | Recommended |
|---|---|---|
| OS | Windows 10, Ubuntu 20.04, macOS 12 | Latest stable release |
| CPU | x64 with AVX2 | Modern multi-core (AVX2 required for Whisper) |
| RAM | 8 GB | 16 GB (LLM uses ~2 GB) |
| Storage | 500 MB (app) | 4 GB (all models: Whisper small + ONNX + Phi-3 Q4) |
| Runtime | .NET 10 | .NET 10 |
| Linux extra | ffmpeg on `$PATH` | ffmpeg 6+ |

## Installation

### Windows

Download the installer from the [Releases](https://github.com/davidpizon/Phonematic/releases) page. Double-click. Follow prompts. We've made it simple enough that even management can do it.

### Linux

```bash
wget https://github.com/davidpizon/Phonematic/releases/latest/download/Phonematic-linux-x64.tar.gz
tar -xzf Phonematic-linux-x64.tar.gz
./Phonematic
```

Install ffmpeg if you haven't already: `sudo apt install ffmpeg`

You're using Linux. We trust you can figure out the rest.

### Building from Source

For those who trust no one (we respect that):

```bash
git clone https://github.com/davidpizon/Phonematic.git
cd Phonematic
dotnet build src/Phonematic.slnx -c Release
dotnet run --project src/Phonematic.Gui/Phonematic.Gui.csproj
```

Requires .NET 10 SDK. Yes, we're living in the future.

Prefer the terminal? There's a headless CLI too — point it at an audio file or a folder and it
writes PhoScript (`.phos`) files:

```bash
dotnet run --project src/Phonematic/Phonematic.csproj -- ./recordings --recursive
```

See [docs/CLI.md](docs/CLI.md) for all flags and exit codes.

## First Run — Model Setup

On first launch Phonematic will automatically download the three AI models it needs:

| Model | Size | Purpose |
|---|---|---|
| Whisper `tiny.en` (default) | ~75 MB | Speech-to-text |
| `all-MiniLM-L6-v2` (ONNX) | ~23 MB | Sentence embeddings |
| Phi-3 Mini 4k Q4 (GGUF) | ~2.2 GB | On-device LLM for Q&A |

Downloads go to `%LOCALAPPDATA%\Phonematic\models\`. You can switch to a larger Whisper model (e.g. `small.en`, `medium.en`) in **Settings** at any time.

## Usage

### Transcribe Tab
1. Click **Browse File** to select a single audio file, or **Browse Folder** to queue an entire directory.
2. Click **Start Transcription**. Already-processed files are skipped automatically (hash-based dedup).
3. Transcripts are saved as `.txt` files to your configured **Output Directory** (default: `~/Documents/Phonematic/`).

### Transcriptions Tab
Browse and read all previously transcribed files. Select any entry to view its full timestamped transcript.

### Search Tab
1. Type a natural-language question in the query box and press **Search**.
2. The top-K most semantically similar transcript chunks are shown, sorted by relevance.
3. Select any result to read its full source transcript on the right.
4. A streaming LLM answer citing source files appears below the results.

### Settings Tab
- Choose your Whisper model size (larger → more accurate, slower, more RAM).
- Set the output directory for transcript files.
- Configure CPU thread count for Whisper.
- Download / re-download individual models.

### PLAUD Sync Tab
1. Paste your PLAUD Bearer token into the token box, or install the companion browser extension to capture it automatically on `http://localhost:27839/plaud-token`.
2. Click **Sync Recordings** to fetch your recording list from the PLAUD API.
3. Select recordings and click **Download Selected** to save them locally.
4. Downloaded files can then be queued for transcription on the Transcribe tab.

## Command-Line Interface (CLI)

Phonematic also ships a headless `audio → PhoScript (.phos)` converter for scripts and automation.
Examples below use the `Phonematic` command; from source, substitute
`dotnet run --project src/Phonematic/Phonematic.csproj -- <args>`. Result paths print to **stdout**;
progress and logs go to **stderr**. Full flag/exit-code reference: [docs/CLI.md](docs/CLI.md).

### Convert a single file

```bash
# Default: writes song.phos next to the source
Phonematic song.mp3

# Explicit output path
Phonematic song.mp3 -o transcripts/song.phos
```

### Convert a folder

```bash
# Every supported audio file in the folder (top level only), .phos next to each source
Phonematic ./recordings

# Recurse into subfolders, mirror the tree into ./out, overwrite existing targets
Phonematic ./recordings --recursive --output-dir ./out --overwrite

# Quiet mode — capture just the written paths
Phonematic ./recordings -q > written.txt
```

### Forced alignment with a known transcript

Supply the exact words spoken; the phones are aligned to them so `<word orth="…">` matches your text.

```bash
# Single file + its transcript
Phonematic interview.mp3 --transcript interview.txt -o interview.phos

# Folder: each audio file is paired with its sibling <name>.txt automatically
Phonematic ./recordings --recursive
```

### Whisper hybrid (no transcript on hand)

Whisper supplies the words; wav2vec2 forced-aligns the phones.

```bash
# Use the default (config) Whisper model size
Phonematic lecture.mp3 --whisper

# Pick a larger Whisper model for the whole folder
Phonematic ./recordings --whisper --whisper-model small
```

### Apply a trained speaker model

```bash
# Improve recognition of a known speaker's new, transcript-less audio
Phonematic new-recording.mp3 --voice-model models/speaker-A.phonematic -o out.phos

# Batch a folder through the same speaker model
Phonematic ./recordings -r --voice-model models/speaker-A.phonematic --output-dir ./out
```

### Train a speaker model (`train`)

Learns a portable `.phonematic` model from `(audio, sibling-<name>.txt)` pairs.

```bash
# Train from a folder of recordings + matching transcripts
Phonematic train ./speaker-A --output models/speaker-A.phonematic --recursive

# Different speaker, more epochs, an explicitly chosen base model
Phonematic train ./speaker-B -o models/speaker-B.phonematic --epochs 80 --base-model wav2vec2-phoneme
```

### Create a model (`model create`)

The only command that downloads. Conversion/training otherwise exit `3` if a model is missing.
It downloads the base model and writes a new, untrained `.phonematic` bundle (a scaffold to be
trained later) to `--output` (required).

```bash
# Download the default base model and write the bundle
Phonematic model create --output models/spk.phonematic

# Also download a Whisper model for hybrid mode
Phonematic model create --output models/spk.phonematic --whisper --whisper-model small
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Avalonia UI (Views)                    │
├─────────────────────────────────────────────────────────────┤
│                   ViewModels (MVVM / CommunityToolkit)      │
├──────────────┬──────────────┬──────────────┬────────────────┤
│ Transcription│  Embedding & │  PLAUD API   │  Config &      │
│ Service      │  Vector Search│  Service     │  Model Manager │
│ (Whisper.net)│  (ONNX + LLM)│  (HttpClient)│  Service       │
├──────────────┴──────────────┴──────────────┴────────────────┤
│              EF Core + SQLite  (PhonematicDbContext)         │
├─────────────────────────────────────────────────────────────┤
│            %LOCALAPPDATA%\Phonematic\  (models, DB, logs)   │
└─────────────────────────────────────────────────────────────┘
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for a detailed description of all layers, data flows, and the database schema.

## Documentation

| Document | Description |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture, data flows, DB schema, file layout |
| [docs/API.md](docs/API.md) | Full reference for all classes, interfaces, and records |
| [docs/CLI.md](docs/CLI.md) | Command-line interface: synopsis, flags, examples, and exit codes |
| [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) | Development workflow, coding standards, PR checklist |
| [docs/TESTING.md](docs/TESTING.md) | Test suite structure, patterns, and how to run tests |
| [docs/AGENTS.md](docs/AGENTS.md) | Guidelines for AI coding agents working on this repo |
| [docs/PHOSCRIPT.md](docs/PHOSCRIPT.md) | PhoScript 1.0 specification — prosodic markup format for `.phos` files |
| [docs/IPA_REFERENCE.md](docs/IPA_REFERENCE.md) | International Phonetic Alphabet reference — how sounds are represented in IPA |

## FAQ

**Q: Why not just use [Cloud Service X]?**  
A: Because you read the terms of service, didn't you? ...You didn't? We recommend reading them. With a lawyer present.

**Q: How accurate is the transcription?**  
A: Depends on your audio quality and chosen model. The default `tiny.en` is fast; `small.en` or `medium.en` gives noticeably better results for unclear audio. Garbage in, garbage out — this is a universal constant.

**Q: Can I transcribe in languages other than English?**  
A: Yes. Switch to a non-`.en` model (e.g. `small`) in Settings. Whisper supports ~100 languages. Your mileage may vary based on your accent's deviation from the training data.

**Q: Where is my data stored?**  
A: Everything — database, models, config, logs — lives under `%LOCALAPPDATA%\Phonematic\` on your own machine. Transcripts go to `~/Documents/Phonematic/` by default (configurable).

**Q: Can I use a different LLM?**  
A: The LLM path is managed by `ModelManagerService.GetLlmModelPath()`. Swap in any GGUF compatible with LLamaSharp and point `llm/` to it.

## License

See [LICENSE](LICENSE) for details.

**Q: Why is the first transcription slow?**  
A: The AI models need to be loaded into memory. Subsequent transcriptions are faster. Patience is a virtue.

**Q: My transcription has errors.**  
A: See "Garbage in, garbage out" above. Also, AI is not perfect. Neither are humans. We're all doing our best here.

## Contributing

Pull requests are welcome. Please ensure your code:
- Compiles
- Passes existing tests
- Doesn't introduce security vulnerabilities
- Is not generated entirely by AI without review (ironic, we know)

## License

MIT License. Use it, modify it, sell it, tattoo it on your forearm. We don't care. See [LICENSE](LICENSE) for the legal boilerplate.

---

*Phonematic: Because your audio files aren't going to transcribe themselves.*

*Built with caffeine and mass quantities of reasonable expectations*
