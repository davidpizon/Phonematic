# Phonematic CLI — Implementation Task

## Goal

Add a headless command-line interface to the Phonematic solution that converts
audio files into PhoScript (`.phos`) files using the acoustic pipeline. The CLI
must be reliable to invoke from automated agents.

## Definition of Done (hard requirement — non-negotiable)

The task is complete **only** when **all** of the following hold:

- The entire solution (`Phonematic`, `Phonematic.Gui`, `Phonematic.Tests`) builds
  with **zero errors and zero warnings**: `dotnet build` from the solution root is
  clean.
- `dotnet test` runs the full suite with **all tests passing** and none skipped due
  to a build failure.
- This applies to **every** project in the solution, not just the changed one.
  Treat warnings as errors for the purposes of this task; do **not** suppress,
  `#pragma`-away, or `<NoWarn>` a warning to satisfy this gate — fix the underlying
  cause.
- If a clean build is not achievable, **stop and report** the blocking
  warning/error rather than marking the task done.

## Project Structure / Refactor

- Current layout (the existing docs are out of date; correct them as part of this
  work):
  - `Phonematic` — .NET 10 console application; this is the CLI.
  - `Phonematic.Gui` — the Avalonia GUI (formerly the `Phonematic` project).
  - `Phonematic.Tests` — xUnit v3 unit tests.
- Migrate the audio-parsing logic and its dependencies from `Phonematic.Gui` into
  the `Phonematic` console project so both projects share it: `AudioConverter`,
  `AcousticPhoneRecognizerService`, `AcousticFeatureExtractorService` (including
  `ComputeSpeakerBaseline`), `PhoScriptWriter`, and the supporting types the
  acoustic path needs (`PhoneAlignment`, `AcousticFeatureFrame`, `SpeakerBaseline`,
  plus `IModelManagerService`/`ModelManagerService` and
  `IConfigService`/`ConfigService` for locating models). Include IPA/CMU/G2P helpers
  only if the acoustic path actually uses them.
- `Phonematic.Gui` references `Phonematic` and resolves the migrated services
  through its existing DI; the GUI's behavior must not change. Update DI
  registrations (`App.axaml.cs`) accordingly. Keep namespaces stable where
  practical.

## Conversion Pipeline (single source of truth)

For each audio file:

1. Validate the path; for single files validate the extension.
2. Verify the wav2vec2 model via `ModelManagerService.IsWav2Vec2ModelDownloaded()`.
   If missing, print actionable instructions and exit with the environment-error
   code. **Do not auto-download.**
3. `AudioConverter.ConvertToWavAsync` → 16 kHz mono WAV.
4. `AcousticPhoneRecognizerService.RecognizeAsync` → `PhoneAlignment[]`.
5. `AcousticFeatureExtractorService.ExtractFramesAsync` → `AcousticFeatureFrame[]`;
   then `ComputeSpeakerBaseline`.
6. `PhoScriptWriter.Write(...)` → fully-annotated `.phos`.

The CLI is a **stateless file→file converter**: do **not** write to the SQLite
database and do **not** compute or store embeddings. Deduplication is by
output-file existence only (see Overwrite behavior).

## Arguments

- Positional `<input>`: path to a supported audio file **or** a directory.
- Supported audio extensions: `.mp3 .wav .aiff .aif .wma .m4a .ogg .flac .voc`
  (define once as a shared constant; reuse for single-file validation and directory
  discovery).
- `-h, --help` and `--version`: standard (provided by System.CommandLine).
- **Single-file mode:**
  - `-o, --output <file>` — optional output `.phos` path. Default: same directory as
    the input audio, same base name, `.phos` extension.
- **Directory mode:**
  - `-r, --recursive` — recurse into subdirectories. Default: top-level only.
  - `-o, --output-dir <dir>` — optional output directory. Default: write each `.phos`
    next to its source file. With `--recursive` + `--output-dir`, mirror the source's
    relative subfolder structure under `<dir>`.
  - Files are processed **sequentially** (no parallelism).
- **Overwrite (both modes):**
  - Default: if the target `.phos` already exists, skip it and print a warning.
  - `-f, --overwrite` — force overwrite of existing targets.
- **Verbosity (both modes):**
  - Default: verbose, with a Spectre.Console progress bar.
  - `-q, --quiet` — suppress the progress bar and informational output; still emit
    warnings and errors.

## I/O Contract (agent-friendly)

- Progress bar and informational/verbose logs → **stderr**.
- Result lines (paths of written `.phos` files), one per line → **stdout**.
- `--quiet` silences stderr progress/info but leaves stdout result lines and error
  reporting intact.
- Handle Ctrl-C: cancel the `CancellationToken`, stop cleanly, exit non-zero.

## Failure Handling & Exit Codes

- Directory mode: continue past per-file errors; collect failures; print an
  end-of-run summary (succeeded / skipped / failed counts). Skipped ≠ failed.
- Exit codes:
  - `0` — Success: all targets written or skipped.
  - `1` — One or more files failed to process (partial or total runtime failure).
  - `2` — Usage error: invalid arguments, missing/nonexistent input path, or an
    unsupported single-file extension.
  - `3` — Environment error: required model not downloaded.
- Single-file mode: a processing failure exits `1`.

## Libraries

- `System.CommandLine` for argument parsing and help/version.
- `Spectre.Console` for the progress bar.
- Note in the PR/CHANGELOG that these are justified additions under the `AGENTS.md`
  "avoid new dependencies" guideline, since standards-compliant CLI parsing and
  progress reporting are not reasonably hand-rolled.

## Coding Standards (per AGENTS.md / CONTRIBUTING.md)

- Target .NET 10. Idiomatic C#, nullable enabled, minimal scoped changes.
- All I/O-bound methods async, returning `Task`/`Task<T>`, accepting
  `CancellationToken ct = default`. Long-running per-file work reports
  `IProgress<double>` (0.0–1.0) surfaced through the progress bar.
- Services owning unmanaged resources (ONNX sessions) implement `IDisposable` and
  are disposed.

## Documentation

- Correct `AGENTS.md` and `ARCHITECTURE.md` to reflect the real layout
  (`Phonematic` = console/CLI + shared services; `Phonematic.Gui` = Avalonia GUI)
  and confirm `.phos` as the PhoScript output extension.
- Add a CLI usage doc (`docs/CLI.md` and/or README): synopsis, every flag, examples
  for single-file and directory (incl. recursive), and the exit-code table.
- Update `CHANGELOG.md` `[Unreleased]` with the refactor and the new CLI.
- Update `CONTRIBUTING.md` run/build instructions if the run target changed.

## Tests (per TESTING.md — xUnit v3, .NET 10, no model/network/GUI dependencies)

- Argument parsing: valid/invalid combinations; `-h`/`--help`/`--version`.
- Output-path resolution: single-file default vs `-o`; directory default vs
  `--output-dir`; recursive subfolder mirroring.
- File discovery: extension filtering; top-level vs recursive.
- Skip-vs-overwrite against pre-existing target files (temp files/dirs, cleaned up
  in `Dispose`).
- Exit-code mapping: success / skip / per-file failure / usage error / missing
  model.
- Use fakes / the testable-subclass pattern to avoid loading ONNX/Whisper/LLM
  models; no real inference, audio conversion, or HTTP.
- `dotnet build` and `dotnet test` must both pass cleanly (see **Definition of
  Done**): zero errors, zero warnings, all tests passing.

## Open Items to Confirm

- `-o` means a file path in single-file mode but a directory in directory mode.
  Acceptable, or prefer one `--output` that auto-detects file vs directory?
- Should `--quiet` also suppress stdout result lines (fully silent on success)?
- Should the CLI append to the existing `transcription.log`?