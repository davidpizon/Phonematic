# Contributing to Phonematic

Thank you for your interest in contributing! Please read these guidelines before opening a pull request.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022+ or Rider (Avalonia Designer support recommended)
- ffmpeg on PATH (required for Linux/macOS audio conversion)
- Git

## Getting Started

```bash
git clone https://github.com/davidpizon/Phonematic.git
cd Phonematic
dotnet build
dotnet test
```

On first run the application will download three AI model files (~500 MB total for the default `tiny.en` configuration). Models are cached in `%LOCALAPPDATA%\Phonematic\models\`.

## Project Layout

```
src/Phonematic/           ← Main Avalonia application
tests/Phonematic.Tests/   ← xUnit unit tests
docs/                     ← Documentation (Markdown)
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for a full description of layers and data flow.

## Development Workflow

1. Fork the repository and create a feature branch off `main`.
2. Make changes following the coding standards below.
3. Add or update tests in `tests/Phonematic.Tests/`.
4. Run `dotnet build` and `dotnet test` — both must pass.
5. Open a pull request against `main` with a focused description.

## Coding Standards

### General

- Target **.NET 10** for all projects.
- Follow the existing code style in each file — do not reformat unrelated code.
- Keep changes minimal and scoped to the stated concern.
- Do not add XML doc comments unless they explain non-obvious logic.

### Async / Await

- All I/O-bound methods must be `async` and return `Task` or `Task<T>`.
- Every public async method must accept a `CancellationToken ct = default` parameter.
- Long-running operations that report progress must accept `IProgress<double>?` where `0.0` = start and `1.0` = complete.

### MVVM

- ViewModels extend `ViewModelBase` (which extends `ObservableObject`).
- Use `[ObservableProperty]` for bindable fields and `[RelayCommand]` for commands.
- Never reference `Avalonia` UI types inside a ViewModel — use interaction delegate callbacks (see `BrowseFileInteraction` in `TranscribeViewModel`).

### Services

- Define an interface in `Services/I<Name>.cs` before implementing in `Services/<Name>.cs`.
- Register the service in `App.axaml.cs → ConfigureServices` with the appropriate lifetime.
- Services that own unmanaged resources must implement `IDisposable`.

### Database

- All schema changes must be done via EF Core migrations:
  ```bash
  dotnet ef migrations add <MigrationName> --project src/Phonematic
  ```
- Never modify existing migration files.
- Foreign keys should use cascade delete where appropriate.

## Testing Guidelines

- Unit tests live in `tests/Phonematic.Tests/` and use **xUnit v3**.
- Tests must not depend on real model files, network access, or system-specific paths.
- Use `Path.GetTempFileName()` / `Path.GetTempPath()` for file-based tests, and clean up in `Dispose`.
- For services whose methods have paths that don't need model loading (e.g. `EmbeddingService.ChunkText`), use a testable subclass that passes `null!` for unused dependencies — see `ChunkTextTests.TestableEmbeddingService`.

### Running Tests

```bash
dotnet test tests/Phonematic.Tests
```

## Adding a New Feature

### New Service

1. Create `Services/IMyService.cs` with the interface.
2. Create `Services/MyService.cs` with the implementation.
3. Register in `App.axaml.cs → ConfigureServices`.
4. Write tests in `tests/Phonematic.Tests/MyServiceTests.cs`.

### New View / Tab

1. Create `Views/MyView.axaml` + `Views/MyView.axaml.cs`.
2. Create `ViewModels/MyViewModel.cs` extending `ViewModelBase`.
3. Add a property `MyViewModel? MyView { get; set; }` to `MainWindowViewModel`.
4. Register `MyViewModel` as a singleton in `App.axaml.cs`.
5. Assign `mainVm.MyView = Services.GetRequiredService<MyViewModel>()` in `OnFrameworkInitializationCompleted`.

### New Configuration Value

1. Add the property to `Models/AppConfig.cs` with a sensible default.
2. Expose it in `SettingsViewModel` if user-configurable.
3. Add a test to `AppConfigTests` verifying the default.

## Commit Messages

Use the imperative mood and keep the subject line under 72 characters:

```
Add PLAUD folder filter to sync view
Fix cosine similarity calculation for zero-length embeddings
Update Whisper model download retry logic
```

## Pull Request Checklist

- [ ] `dotnet build` passes with no errors or warnings.
- [ ] `dotnet test` passes.
- [ ] New public APIs are documented in `docs/API.md`.
- [ ] Architectural changes are reflected in `docs/ARCHITECTURE.md`.
- [ ] PR description explains *what* changed and *why*.
