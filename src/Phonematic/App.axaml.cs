using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phonematic.Data;
using Phonematic.Services;
using Phonematic.ViewModels;
using Phonematic.Views;

namespace Phonematic;

/// <summary>
/// Root Avalonia <see cref="Application"/> class. Acts as the DI composition root:
/// all services are registered in <see cref="ConfigureServices"/> and the resulting
/// <see cref="ServiceProvider"/> is stored in the static <see cref="Services"/> property
/// so that code-behind can resolve dependencies when needed.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the application-wide DI container. Set during
    /// <see cref="OnFrameworkInitializationCompleted"/> and available for the lifetime of
    /// the process. <see langword="null"/> before initialisation completes.
    /// </summary>
    public static ServiceProvider? Services { get; private set; }

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Called by Avalonia after the framework is ready. Builds the DI container, runs
    /// EF Core migrations, wires up child ViewModels, checks whether all AI models are
    /// present (triggering the setup wizard if not), and creates the <see cref="MainWindow"/>.
    /// Any exception during initialisation is surfaced via <see cref="Program.ShowFatalError"/>.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                Services = services.BuildServiceProvider();

                // Run migrations
                using (var scope = Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<PhonematicDbContext>();
                    db.Database.Migrate();
                }

                var mainVm = Services.GetRequiredService<MainWindowViewModel>();
                var configService = Services.GetRequiredService<IConfigService>();
                var modelManager = Services.GetRequiredService<IModelManagerService>();
                var config = configService.Load();

                // Wire up child ViewModels
                mainVm.Model = Services.GetRequiredService<ModelViewModel>();
                mainVm.Transcribe = Services.GetRequiredService<TranscribeViewModel>();
                mainVm.Transcriptions = Services.GetRequiredService<TranscriptionsViewModel>();
                mainVm.Train = Services.GetRequiredService<TrainViewModel>();
                mainVm.Search = Services.GetRequiredService<SearchViewModel>();
                mainVm.Settings = Services.GetRequiredService<SettingsViewModel>();
                mainVm.PlaudSync = Services.GetRequiredService<PlaudSyncViewModel>();

                // Check if setup is needed
                if (!modelManager.AreAllModelsReady(config.WhisperModelSize))
                {
                    mainVm.IsSetupRequired = true;
                    mainVm.Setup = new SetupViewModel(modelManager, configService, () =>
                    {
                        mainVm.IsSetupRequired = false;
                    });

                    // Auto-start download
                    _ = mainVm.Setup.StartDownloadCommand.ExecuteAsync(null);
                }

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainVm
                };
            }
            catch (Exception ex)
            {
                Program.ShowFatalError(
                    "Phonematic failed to initialize.\n\n" +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Registers all application services, the EF Core context, and ViewModels into
    /// the provided <paramref name="services"/> collection.
    /// <list type="table">
    ///   <listheader><term>Lifetime</term><description>Services</description></listheader>
    ///   <item><term>Singleton</term><description>Config, ModelManager, AI services, PLAUD services, TokenListener, MainWindowViewModel</description></item>
    ///   <item><term>Transient</term><description>FileTrackingService, per-tab ViewModels</description></item>
    ///   <item><term>Singleton factory</term><description>PhonematicDbContext (via AddDbContextFactory)</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to populate.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IModelManagerService, ModelManagerService>();

        // Database
        services.AddDbContext<PhonematicDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfigService>();
            options.UseSqlite($"Data Source={config.DatabasePath}");
        });
        services.AddDbContextFactory<PhonematicDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfigService>();
            options.UseSqlite($"Data Source={config.DatabasePath}");
        }, ServiceLifetime.Singleton);

        // Voice model (active session model — in-memory, not persisted)
        services.AddSingleton<IActiveVoiceModelService, ActiveVoiceModelService>();

        // Data services
        services.AddTransient<IFileTrackingService, FileTrackingService>();

        // AI services
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorSearchService, VectorSearchService>();
        services.AddSingleton<ILlmService, LlmService>();

        // PLAUD sync
        services.AddSingleton<IPlaudApiService, PlaudApiService>();
        services.AddSingleton<TokenListenerService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<ModelViewModel>();
        services.AddTransient<TranscribeViewModel>();
        services.AddTransient<TranscriptionsViewModel>();
        services.AddTransient<TrainViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PlaudSyncViewModel>();
    }
}
