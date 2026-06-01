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

    public SpectreProgressDisplay(IAnsiConsole console)
    {
        _console = console;
    }

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

    private sealed class Scope : IProgressScope
    {
        private readonly ProgressContext _ctx;

        public Scope(ProgressContext ctx) => _ctx = ctx;

        public IProgress<double> AddTask(string description)
            => new TaskProgress(_ctx.AddTask(Markup.Escape(description)));
    }

    private sealed class TaskProgress : IProgress<double>
    {
        private readonly ProgressTask _task;

        public TaskProgress(ProgressTask task) => _task = task;

        public void Report(double value)
            => _task.Value = Math.Clamp(value, 0.0, 1.0) * _task.MaxValue;
    }
}
