# Phonematic CLI

The `Phonematic` console project is a headless command-line converter that turns audio files
into PhoScript (`.phos`) files using the acoustic pipeline (wav2vec2 phone recognition + NWaves
prosodic features). It is designed to be reliable to invoke from automated agents and scripts.

The CLI is a **stateless, self-contained file→file converter**: it reads and writes **no app config
file** and uses **no fixed model cache**. Every model it needs is **embedded inside the
`.phonematic` bundle** you pass on the command line (created by `model create`); at run time the
embedded models are extracted to ephemeral temp files. It does not write to the SQLite database and
does not compute or store embeddings. Deduplication is by output-file existence only.

## Synopsis

```
Phonematic <input> --voice-model <bundle> [options]
```

Build and run from source:

```bash
dotnet run --project src/Phonematic/Phonematic.csproj -- <input> --voice-model <bundle> [options]
```

`<input>` is a path to a supported audio file **or** a directory of audio files. `--voice-model` is
a required path to a `.phonematic` bundle (see `model create` below).

Supported audio extensions: `.mp3 .wav .aiff .aif .wma .m4a .ogg .flac .voc`

## Options

| Flag | Applies to | Description |
|---|---|---|
| `<input>` | both | **(required)** Audio file or directory to convert. |
| `--voice-model <bundle>` | both | **(required)** Path to a `.phonematic` bundle. Supplies the embedded base wav2vec2 model; if the bundle is **trained**, its speaker-adaptation head is also applied. An untrained bundle (straight from `model create`) free-decodes with the base model alone. |
| `-o, --output <file>` | single-file | Output `.phos` file path. Default: same directory and base name as the input, with a `.phos` extension. |
| `--output-dir <dir>` | directory | Output directory. Default: write each `.phos` next to its source. (Alias of `-o`/`--output`; the meaning is chosen by whether `<input>` is a file or a directory.) |
| `-r, --recursive` | directory | Recurse into subdirectories. Default: top-level only. With `--output-dir`, the source's relative subfolder structure is mirrored under `<dir>`. |
| `-f, --overwrite` | both | Overwrite existing `.phos` targets. Default: skip existing targets with a warning. |
| `-q, --quiet` | both | Suppress the progress bar and informational output; warnings and errors (and stdout result lines) still print. |
| `-t, --transcript <file>` | single-file | Path to a text file with the **exact words spoken**. The phones are forced-aligned to those words, so the output `orth` matches the transcript exactly. In directory mode, per-file sibling `<name>.txt` files are used instead of this flag. |
| `--whisper` | both | For files **without** a transcript, use the bundle's **embedded** Whisper model to supply the words (hybrid mode); wav2vec2 then forced-aligns the phones. Requires a bundle created with `model create --whisper`. |
| `-h, --help` | both | Show help and exit. |
| `--version` | both | Show version and exit. |

In directory mode files are processed **sequentially** (no parallelism).

## Word sources & accuracy

By default the phone stream comes from a free CTC decode of the base model, and `<word orth>` is
left empty (the `.phos` is only as accurate as the raw recogniser). The following options make the
output reflect the actual words spoken; precedence is **transcript ▸ Whisper ▸ free decode**:

- **`--transcript words.txt`** (single file) or a sibling **`<name>.txt`** (directory mode) —
  forced-aligns the phones to your known words. Deterministic and exact: `orth` equals your text
  and timing is taken from the audio. This is the most accurate option when you have the words.
- **`--whisper`** — when no transcript is available, the bundle's embedded Whisper model provides
  the words and sentence segmentation; wav2vec2 forced-aligns the phones within each word. `orth` is
  then populated from Whisper (as good as Whisper's transcription).
- **A trained bundle** — when `--voice-model` points at a bundle produced by `train`, its speaker
  adapter improves the raw phones. Composes with the above (it improves the logits that feed either
  path). An untrained `model create` bundle skips this step.

Per-`<phon>` IPA is the canonical dictionary pronunciation aligned in time (as with Montreal Forced
Aligner / Gentle), not a transcription of every realised allophone.

## I/O contract (agent-friendly)

- **stdout** — one line per written `.phos` file (the result paths). Skipped files are not listed.
- **stderr** — the progress bar and all informational/verbose logs, plus warnings and errors.
- `--quiet` silences stderr progress/info but leaves stdout result lines and error reporting intact.
- **Ctrl-C** cancels the in-flight work, stops cleanly, and exits non-zero.

This separation means a script can capture the written paths with a simple stdout redirect while
progress and diagnostics stay on stderr:

```bash
Phonematic ./recordings -r --voice-model base.phonematic --output-dir ./out > written.txt
```

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success — all targets were written or skipped. |
| `1` | One or more files failed to process (partial or total runtime failure), or the run was cancelled. |
| `2` | Usage error — invalid arguments, a missing required option, a missing/nonexistent input or bundle path, or an unsupported single-file extension. |
| `3` | Environment error — the `.phonematic` bundle is invalid or missing a required embedded model (e.g. `--whisper` was requested but the bundle embeds no Whisper model). |

In directory mode the run continues past per-file errors; a summary
(`succeeded / skipped / failed`) is printed to stderr at the end. Skipped is **not** counted as
failed.

## Self-contained models (no config, no cache)

The CLI never downloads as a side effect and never reads a config file or a shared model cache.
Everything it needs is **inside the `.phonematic` bundle**:

- the base wav2vec2 phoneme **ONNX** (always),
- the speaker **adapter** (untrained for a fresh `model create` bundle; trained after `train`),
- optionally the **Whisper GGML** model (when the bundle was created with `--whisper`),
- a manifest (base-model identity, dimensions, speaker baseline, and a *trained* flag).

`model create` is the **only** command that downloads. `convert` and `train` consume a bundle and
extract its embedded models to temp files for the duration of the run.

## Creating a model (`model create`)

```
Phonematic model create --output <file> [--url <url>] [--whisper] [--whisper-model <size>] [--quiet]
```

`model create` downloads the base wav2vec2 model (from a built-in default URL, or `--url`) and,
with `--whisper`, the Whisper GGML model, then **embeds the model bytes** into a new, **untrained**
`.phonematic` bundle written to `--output` (required). The bundle is a portable, self-contained
scaffold: it can be used directly with `--voice-model` (free decode, no speaker adaptation), or
passed to `train` to produce a *trained* bundle. The created bundle path prints to stdout; download
progress prints to stderr.

```bash
Phonematic model create --output models/base.phonematic                      # default base model
Phonematic model create --output models/fr.phonematic --url https://…/fr-phone.onnx   # a different base model URL
Phonematic model create --output models/base.phonematic --whisper --whisper-model small   # also embed a Whisper model
```

## Training a voice model (`train`)

Train a `.phonematic` speaker model from many (audio, transcript) pairs, then apply it later with
`--voice-model` to improve transcript-less recognition for that speaker.

```
Phonematic train <pairs-dir> --base-model <base.phonematic> --output <model.phonematic> [--epochs N] [--recursive] [--quiet]
```

Training pairs are discovered as audio files under `<pairs-dir>` that each have a sibling
`<name>.txt` transcript (the same convention as directory-mode forced alignment). `--base-model`
(required) is the path to a `.phonematic` bundle created by `model create`; its embedded base model
is used for training and **re-embedded** into the output so the trained bundle stays self-contained
(the Whisper model, if present, is carried over too). The trained model path is written to stdout;
per-epoch progress (loss, validation phone-error-rate) goes to stderr.

The output `.phonematic` is a **self-contained bundle** (a zip): the embedded base ONNX, the trained
adapter weights, the speaker baseline, the optional Whisper model, and the manifest. It is the
portable import/export unit — reference different bundles for different speakers; everything needed
at conversion time travels inside the file.

```bash
# 1. Create a base bundle once (downloads + embeds the base model)
Phonematic model create --output ./models/base.phonematic

# 2. Train from a folder of recordings, each with a matching .txt
Phonematic train ./speaker-A --base-model ./models/base.phonematic --output ./models/speaker-A.phonematic --recursive

# 3. Use the trained model on new, transcript-less audio (base model resolved from the bundle)
Phonematic new-recording.mp3 --voice-model ./models/speaker-A.phonematic -o out.phos
```

## Examples

Single file, default output next to the source (`song.phos`):

```bash
Phonematic song.mp3 --voice-model models/base.phonematic
```

Single file, explicit output path:

```bash
Phonematic song.mp3 --voice-model models/base.phonematic -o transcripts/song.phos
```

Directory (top-level only), `.phos` written next to each source:

```bash
Phonematic ./recordings --voice-model models/base.phonematic
```

Directory, recursive, mirrored into a separate output tree, overwriting existing targets:

```bash
Phonematic ./recordings --voice-model models/base.phonematic --recursive --output-dir ./out --overwrite
```

Quiet mode (only result paths on stdout, no progress bar):

```bash
Phonematic ./recordings --voice-model models/base.phonematic -q > written.txt
```

Single file with a known transcript (exact words via forced alignment):

```bash
Phonematic interview.mp3 --voice-model models/base.phonematic --transcript interview.txt -o interview.phos
```

Directory where each audio file has a sibling `<name>.txt` transcript (forced-aligned per file;
files without a sibling fall back to free decode, or to Whisper if `--whisper` is given):

```bash
Phonematic ./recordings --voice-model models/base.phonematic --recursive
```

Transcript-less audio, words supplied by the bundle's embedded Whisper model:

```bash
Phonematic lecture.mp3 --voice-model models/base-whisper.phonematic --whisper -o lecture.phos
```
