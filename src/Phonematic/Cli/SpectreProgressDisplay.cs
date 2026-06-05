using Spectre.Console;

namespace Phonematic.Cli;

/// <summary>
/// Spectre.Console-backed <see cref="IProgressDisplay"/>. Renders an animated progress bar
/// to the supplied console (the CLI points this at <c>stderr</c> so <c>stdout</c> stays
/// clean for result lines).
/// </summary>
public sealed class SpectreProgressDisplay : IProgressDisplay
{
    private readonly IAnsiConsole _console;

    /// <summary>Initialises the display to render into <paramref name="console"/>.</summary>
    public SpectreProgressDisplay(IAnsiConsole console)
    {
        _console = console;
    }

    /// <inheritdoc/>
    public async Task<int> RunAsync(Func<IProgressScope, Task<int>> body, CancellationToken ct)
    {
        var exitCode = ExitCodes.Success;

        await _console.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                exitCode = await body(new Scope(ctx));
            });

        return exitCode;
    }

    /// <summary>Adapts a Spectre.Console <see cref="ProgressContext"/> to the <see cref="IProgressScope"/> abstraction.</summary>
    private sealed class Scope : IProgressScope
    {
        private readonly ProgressContext _ctx;

        /// <summary>Wraps <paramref name="ctx"/> as an <see cref="IProgressScope"/>.</summary>
        public Scope(ProgressContext ctx) => _ctx = ctx;

        /// <inheritdoc/>
        public IProgress<double> AddTask(string description)
            => new TaskProgress(_ctx.AddTask(Markup.Escape(description)));
    }

    /// <summary>Adapts a Spectre.Console <see cref="ProgressTask"/> to <see cref="IProgress{T}"/> with a 0.0–1.0 scale.</summary>
    private sealed class TaskProgress : IProgress<double>
    {
        private readonly ProgressTask _task;

        /// <summary>Wraps <paramref name="task"/> as an <see cref="IProgress{T}"/>.</summary>
        public TaskProgress(ProgressTask task) => _task = task;

        /// <inheritdoc/>
        public void Report(double value)
            => _task.Value = Math.Clamp(value, 0.0, 1.0) * _task.MaxValue;
    }
}
