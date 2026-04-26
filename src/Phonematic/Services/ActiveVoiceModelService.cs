using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Singleton implementation of <see cref="IActiveVoiceModelService"/>.
/// Holds the single in-memory active <see cref="VoiceModel"/> for the current application session.
/// The active model is not persisted to the database and is reset to a blank default each time
/// the application starts.
/// </summary>
public sealed class ActiveVoiceModelService : IActiveVoiceModelService
{
    /// <summary>Initialises a new <see cref="ActiveVoiceModelService"/> with a blank default model.</summary>
    public ActiveVoiceModelService()
    {
        ActiveModel = CreateDefaultModel();
    }

    /// <inheritdoc/>
    public VoiceModel ActiveModel { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? ActiveModelChanged;

    /// <inheritdoc/>
    public void LoadFromFile(string phonematicFilePath)
    {
        if (!File.Exists(phonematicFilePath))
            throw new FileNotFoundException("The specified .phonematic file was not found.", phonematicFilePath);

        var name = Path.GetFileNameWithoutExtension(phonematicFilePath);
        var lastWrite = File.GetLastWriteTimeUtc(phonematicFilePath);

        ActiveModel = new VoiceModel
        {
            Name = name,
            ModelPath = phonematicFilePath,
            CreatedAtUtc = DateTime.UtcNow,
            LastTrainedAtUtc = lastWrite,
        };

        ActiveModelChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void ExportToFile(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(ActiveModel.ModelPath) || !File.Exists(ActiveModel.ModelPath))
            throw new InvalidOperationException(
                "No voice model file is loaded. Load a .phonematic file before exporting.");

        File.Copy(ActiveModel.ModelPath, destinationPath, overwrite: true);
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static VoiceModel CreateDefaultModel() =>
        new()
        {
            Name = string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
        };
}
