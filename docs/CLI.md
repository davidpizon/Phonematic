# Phonematic CLI

The `Phonematic` console project is a headless command-line converter that turns audio files
into PhoScript (`.phos`) files using the acoustic pipeline (wav2vec2 phone recognition + NWaves
prosodic features). It is designed to be reliable to invoke from automated agents and scripts.

The CLI is a **stateless file→file converter**: it does not write to the SQLite database and
does not compute or store embeddings. Deduplication is by output-file existence only.

## Synopsis

```
Phonematic <input> [options]
```

Build and run from source:

```bash
dotnet run --project src/Phonematic/Phonematic.csproj -- <input> [options]
```

`<input>` is a path to a supported audio file **or** a directory of audio files.

Supported audio extensions: `.mp3 .wav .aiff .aif .wma .m4a .ogg .flac .voc`

## Options

| Flag | Applies to | Description |
|---|---|---|
| `<input>` | both | **(required)** Audio file or directory to convert. |
| `-o, --output <file>` | single-file | Output `.phos` file path. Default: same directory and base name as the input, with a `.phos` extension. |
| `--output-dir <dir>` | directory | Output directory. Default: write each `.phos` next to its source. (Alias of `-o`/`--output`; the meaning is chosen by whether `<input>` is a file or a directory.) |
| `-r, --recursive` | directory | Recurse into subdirectories. Default: top-level only. With `--output-dir`, the source's relative subfolder structure is mirrored under `<dir>`. |
| `-f, --overwrite` | both | Overwrite existing `.phos` targets. Default: skip existing targets with a warning. |
| `-q, --quiet` | both | Suppress the progress bar and informational output; warnings and errors (and stdout result lines) still print. |
| `-t, --transcript <file>` | single-file | Path to a text file with the **exact words spoken**. The phones are forced-aligned to those words, so the output `orth` matches the transcript exactly. In directory mode, per-file sibling `<name>.txt` files are used instead of this flag. |
| `--whisper` | both | For files **without** a transcript, use Whisper to supply the words (hybrid mode); wav2vec2 then forced-aligns the phones. Requires a Whisper model. |
| `--whisper-model <size>` | both | Whisper model size for `--whisper` (`tiny`, `base`, `small`, `medium`, …). Defaults to the app config's `WhisperModelSize`. |
| `--voice-model <file>` | both | Path to a trained `.phonematic` voice model. Its speaker-adaptation head is applied during recognition to improve accuracy for that speaker. |
| `-h, --help` | both | Show help and exit. |
| `--version` | both | Show version and exit. |

In directory mode files are processed **sequentially** (no parallelism).

## Word sources & accuracy

By default the phone stream comes from a free CTC decode of the base model, and `<word orth>` is
left empty (the `.phos` is only as accurate as the raw recogniser). Three options make the output
reflect the actual words spoken; precedence is **transcript ▸ Whisper ▸ free decode**:

- **`--transcript words.txt`** (single file) or a sibling **`<name>.txt`** (directory mode) —
  forced-aligns the phones to your known words. Deterministic and exact: `orth` equals your text
  and timing is taken from the audio. This is the most accurate option when you have the words.
- **`--whisper`** — when no transcript is available, Whisper provides the words and sentence
  segmentation; wav2vec2 forced-aligns the phones within each word. `orth` is then populated from
  Whisper (as good as Whisper's transcription).
- **`--voice-model spk.phonematic`** — applies a trained speaker adapter (see `train` below) to
  improve the raw phones. Composes with the above (it improves the logits that feed either path).

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
Phonematic ./recordings -r --output-dir ./out > written.txt
```

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success — all targets were written or skipped. |
| `1` | One or more files failed to process (partial or total runtime failure), or the run was cancelled. |
| `2` | Usage error — invalid arguments, a missing/nonexistent input path, or an unsupported single-file extension. |
| `3` | Environment error — the required wav2vec2 model is not downloaded. |

In directory mode the run continues past per-file errors; a summary
(`succeeded / skipped / failed`) is printed to stderr at the end. Skipped is **not** counted as
failed.

## Model requirement

`convert` and `train` verify the base wav2vec2 model before running and exit with code `3` if it
is missing — they **never download as a side effect**. Fetch models explicitly with
`phonematic models download` (below), or place the file at the printed path:

```
%LOCALAPPDATA%\Phonematic\models\acoustic\<base-model-name>.onnx   (default name: wav2vec2-phoneme)
```

When `--whisper` is requested, the corresponding Whisper GGML model must also be present (else exit
code `3`):

```
%LOCALAPPDATA%\Phonematic\models\whisper\ggml-<size>.bin
```

## Managing models (`models`)

```
Phonematic models download [--name <name>] [--url <url>] [--whisper] [--whisper-model <size>] [--quiet]
Phonematic models status
```

`models download` is the **only** command that downloads. It fetches the base wav2vec2 model to
`acoustic/<name>.onnx` (name + URL default to the app config — both configurable so you can fetch
and reference **different base models for different speakers**). `--whisper` also downloads the
Whisper model. `models status` reports which models are present. Downloaded paths print to stdout;
progress prints to stderr.

```bash
Phonematic models download                      # default base model
Phonematic models download --name spk-fr --url https://…/fr-phone.onnx   # a different base
Phonematic models download --whisper --whisper-model small
Phonematic models status
```

## Training a voice model (`train`)

Train a `.phonematic` speaker model from many (audio, transcript) pairs, then apply it later with
`--voice-model` to improve transcript-less recognition for that speaker.

```
Phonematic train <pairs-dir> --output <model.phonematic> [--epochs N] [--base-model <name>] [--recursive] [--quiet]
```

Training pairs are discovered as audio files under `<pairs-dir>` that each have a sibling
`<name>.txt` transcript (the same convention as directory-mode forced alignment). The base model
must already be downloaded (`models download`); `--base-model <name>` selects which one (default:
app config). The trained model path is written to stdout; per-epoch progress (loss, validation
phone-error-rate) goes to stderr.

The output `.phonematic` is a **self-contained bundle** (a zip): the trained adapter weights, the
speaker baseline, and the **identity of the base model** it was trained against. It is the portable
import/export unit — reference different bundles for different speakers; at conversion time the
bundle's recorded base model is loaded automatically.

```bash
# 1. Fetch the base model once
Phonematic models download

# 2. Train from a folder of recordings, each with a matching .txt
Phonematic train ./speaker-A --output ./models/speaker-A.phonematic --recursive

# 3. Use the trained model on new, transcript-less audio (base model resolved from the bundle)
Phonematic new-recording.mp3 --voice-model ./models/speaker-A.phonematic -o out.phos
```

## Examples

Single file, default output next to the source (`song.phos`):

```bash
Phonematic song.mp3
```

Single file, explicit output path:

```bash
Phonematic song.mp3 -o transcripts/song.phos
```

Directory (top-level only), `.phos` written next to each source:

```bash
Phonematic ./recordings
```

Directory, recursive, mirrored into a separate output tree, overwriting existing targets:

```bash
Phonematic ./recordings --recursive --output-dir ./out --overwrite
```

Quiet mode (only result paths on stdout, no progress bar):

```bash
Phonematic ./recordings -q > written.txt
```

Single file with a known transcript (exact words via forced alignment):

```bash
Phonematic interview.mp3 --transcript interview.txt -o interview.phos
```

Directory where each audio file has a sibling `<name>.txt` transcript (forced-aligned per file;
files without a sibling fall back to free decode, or to Whisper if `--whisper` is given):

```bash
Phonematic ./recordings --recursive
```

Transcript-less audio, words supplied by Whisper:

```bash
Phonematic lecture.mp3 --whisper --whisper-model small -o lecture.phos
```
