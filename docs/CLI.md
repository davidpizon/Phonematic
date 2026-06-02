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
| `-h, --help` | both | Show help and exit. |
| `--version` | both | Show version and exit. |

In directory mode files are processed **sequentially** (no parallelism).

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

The CLI verifies the wav2vec2 model via `ModelManagerService.IsWav2Vec2ModelDownloaded()` before
converting. If the model is missing it prints the expected path and exits with code `3` — it
**never downloads models automatically**. Download the model through the Phonematic GUI setup
wizard (or place the file at the printed path):

```
%LOCALAPPDATA%\Phonematic\models\acoustic\wav2vec2-phoneme.onnx
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
