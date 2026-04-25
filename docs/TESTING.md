# Testing

This document describes the test suite structure, patterns, and how to run tests for Phonematic.

See also:
- [API.md](API.md) — public API reference for the classes under test
- [ARCHITECTURE.md](ARCHITECTURE.md) — system architecture and data flows
- [CONTRIBUTING.md](CONTRIBUTING.md) — guidelines for adding new tests

## Overview

Tests live in `tests/Phonematic.Tests/` and use **xUnit v3** targeting **.NET 10**. All tests are pure unit tests — no model files, network calls, or GUI are required.

## Running Tests

```bash
# From the solution root
dotnet test

# From the test project directory
dotnet test tests/Phonematic.Tests

# Run a specific test class
dotnet test --filter "ClassName=Phonematic.Tests.FileHasherTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName=Phonematic.Tests.FileHasherTests.ComputeSha256Async_ReturnsCorrectHash"
```

## Test Files

| File | What it tests |
|---|---|
| `AppConfigTests.cs` | Default property values of `AppConfig`. |
| `ChunkTextTests.cs` | `EmbeddingService.ChunkText` — sentence splitting and overlap logic. |
| `ConfigServiceTests.cs` | `ConfigService` directory path computation and settings round-trip. |
| `ConverterTests.cs` | All four Avalonia value converters (`PercentageConverter`, `FileSizeConverter`, `DurationConverter`, `InverseBoolConverter`). |
| `DbContextTests.cs` | `PhonematicDbContext` CRUD operations against an in-memory SQLite database. |
| `FileHasherTests.cs` | `FileHasher.ComputeSha256Async` — hash consistency, correctness, empty file, and cancellation. |
| `ModelManagerServiceTests.cs` | `ModelManagerService` path computation and model-presence detection. |

## Patterns and Conventions

### Temporary Files

Tests that need real files create them via `Path.GetTempFileName()` and delete them in `Dispose`:

```csharp
public class MyTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
```

### Testable Subclass Pattern

`EmbeddingService.ChunkText` is a pure text-processing method that does not need ONNX or database dependencies. To test it without loading models, `ChunkTextTests` uses a minimal subclass:

```csharp
private class TestableEmbeddingService : EmbeddingService
{
    public TestableEmbeddingService()
        : base(null!, null!, new NullDbContextFactory()) { }
}
```

This pattern should be used for any service whose method under test does not exercise the injected dependencies.

### In-Memory SQLite for DbContext Tests

`DbContextTests` creates a fresh in-memory SQLite database for each test using `DbContextOptionsBuilder`:

```csharp
var options = new DbContextOptionsBuilder<PhonematicDbContext>()
    .UseSqlite("Data Source=:memory:")
    .Options;
using var db = new PhonematicDbContext(options);
db.Database.EnsureCreated();
```

### Theory Tests

Data-driven tests use `[Theory]` + `[InlineData]` for converters and other pure functions:

```csharp
[Theory]
[InlineData(0.5, "50%")]
[InlineData(1.0, "100%")]
public void PercentageConverter_FormatsCorrectly(double input, string expected) { ... }
```

## Adding New Tests

1. Create a file named `<ClassName>Tests.cs` in `tests/Phonematic.Tests/`.
2. The test class should be in the `Phonematic.Tests` namespace.
3. Avoid testing implementation details — test observable behaviour through public APIs.
4. Do not test Whisper, ONNX, or LLM inference directly — those require model files unavailable in CI.
5. Do not make real HTTP calls in tests.

## Coverage Notes

The following areas are intentionally not covered by automated unit tests because they require real AI models or platform-specific audio codecs:

- `TranscriptionService.TranscribeAsync` (requires Whisper GGML model)
- `EmbeddingService.GenerateEmbedding` (requires ONNX model file)
- `LlmService.GenerateAnswerAsync` (requires Phi-3 GGUF)
- `AudioConverter.ConvertToWavAsync` / `GetDurationSeconds` (requires real audio files and ffmpeg/NAudio)
- `PlaudApiService` (requires live PLAUD API credentials)

These are covered by manual integration testing during development.
