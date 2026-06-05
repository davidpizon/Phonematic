namespace Phonematic.Cli;

/// <summary>
/// Abstraction over the progress UI so <see cref="CliRunner"/> can drive a Spectre.Console
/// progress bar in production while tests inject a no-op implementation.
/// </summary>
public interface IProgressDisplay
{
    /// <summary>
    /// Runs <paramref name="body"/> within a progress-display scope and returns the exit
    /// code it produced.
    /// </summary>
    Task<int> RunAsync(Func<IProgressScope, Task<int>> body, CancellationToken ct);
}

/// <summary>A scope in which per-file progress tasks can be created.</summary>
public interface IProgressScope
{
    /// <summary>Adds a progress task with the given description and returns a 0.0–1.0 reporter.</summary>
    IProgress<double> AddTask(string description);
}

/// <summary>
/// No-op <see cref="IProgressDisplay"/> used in quiet mode and in unit tests. Runs the body
/// directly with progress reporters that discard their values.
/// </summary>
public sealed class NullProgressDisplay : IProgressDisplay
{
    /// <inheritdoc/>
    public Task<int> RunAsync(Func<IProgressScope, Task<int>> body, CancellationToken ct)
        => body(NullScope.Instance);

    /// <summary>No-op <see cref="IProgressScope"/> that discards all task registrations.</summary>
    private sealed class NullScope : IProgressScope
    {
        public static readonly NullScope Instance = new();
        /// <inheritdoc/>
        public IProgress<double> AddTask(string description) => NullProgress.Instance;
    }

    /// <summary>No-op <see cref="IProgress{T}"/> that discards all reported values.</summary>
    private sealed class NullProgress : IProgress<double>
    {
        public static readonly NullProgress Instance = new();
        /// <inheritdoc/>
        public void Report(double value) { }
    }
}
