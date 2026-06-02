using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace Phonematic;

/// <summary>
/// Application entry point. Bootstraps the Avalonia desktop lifetime and provides a
/// last-resort fatal-error display that works on both Windows (MessageBox) and other
/// platforms (stderr).
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry point. Registers an <see cref="AppDomain.UnhandledException"/> handler for
    /// background-thread failures, then starts the Avalonia classic desktop lifetime.
    /// Any exception that escapes <c>StartWithClassicDesktopLifetime</c> is caught and shown
    /// as a fatal error before the process exits.
    /// </summary>
    /// <param name="args">Command-line arguments passed through to Avalonia.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                ShowFatalError($"An unexpected error occurred.\n\n{ex.GetType().Name}: {ex.Message}");
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            ShowFatalError(
                "Phonematic failed to start.\n\n" +
                $"{ex.GetType().Name}: {ex.Message}\n\n" +
                "This may indicate a missing runtime or corrupted installation.\n" +
                "Try reinstalling the application.");
        }
    }

    /// <summary>
    /// Builds the Avalonia <see cref="AppBuilder"/> with platform auto-detection,
    /// the Inter font, and trace logging. Called by both <see cref="Main"/> and the
    /// Avalonia designer.
    /// </summary>
    /// <returns>A configured <see cref="AppBuilder"/> instance.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Displays a fatal error message and exits the process with code 1.
    /// On Windows, shows a native MessageBox (MB_ICONERROR) before writing to stderr.
    /// On other platforms, writes directly to stderr.
    /// </summary>
    /// <param name="message">Human-readable error text to display.</param>
    internal static void ShowFatalError(string message)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                MessageBox(IntPtr.Zero, message, "Phonematic - Error", 0x10 /* MB_ICONERROR */);
            }
            catch { /* P/Invoke unavailable, fall through to stderr */ }
        }

        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }

    /// <summary>
    /// P/Invoke declaration for the Win32 <c>MessageBox</c> function in user32.dll.
    /// Used only on Windows to show a blocking modal error dialog.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
