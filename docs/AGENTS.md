# Agents

This document describes the AI agents and coding guidelines for contributors and automated agents working on the Phonematic project.

## General Guidelines

- Follow existing code style and conventions found throughout the codebase.
- Target **.NET 10** for all projects unless otherwise specified.
- Make minimal changes to achieve the goal; avoid unnecessary refactoring.
- Do not add comments unless they match existing comment style or explain complex logic.
- Use existing libraries whenever possible; avoid adding new dependencies unless absolutely necessary.
- Validate all changes by building the solution and running relevant tests before considering a task complete.

## Project Structure

```
Phonematic/                    ← Solution root
├── src/
│   └── Phonematic/            ← Main application project (Avalonia, .NET 10)
│       ├── App.axaml.cs       ← DI composition root and app bootstrap
│       ├── Program.cs         ← Entry point, fatal error handling
│       ├── Converters/        ← Avalonia IValueConverter implementations
│       ├── Data/              ← EF Core DbContext
│       ├── Helpers/           ← Static utility classes (AudioConverter, FileHasher)
│       ├── Migrations/        ← EF Core migration files
│       ├── Models/            ← Plain data models (AppConfig, ProcessedFile, etc.)
│       ├── Services/          ← Business logic services and their interfaces
│       ├── ViewModels/        ← MVVM ViewModels (CommunityToolkit.Mvvm)
│       └── Views/             ← Avalonia XAML views and code-behind
├── tests/
│   └── Phonematic.Tests/      ← xUnit test project
└── docs/                      ← Project documentation (this folder)
```

- Source code lives under the solution root at `C:\Users\david.pizon\source\repos\Phonematic\`.
- Documentation lives in the `/docs` folder.

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
| `Singleton` | `IConfigService`, `IModelManagerService`, `TokenListenerService`, `IPlaudApiService`, all ViewModels |
| `Transient` | `IFileTrackingService` |
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
