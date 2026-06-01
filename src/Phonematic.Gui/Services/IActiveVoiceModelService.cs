using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Manages the single in-memory active <see cref="VoiceModel"/> for the current session.
/// Only one voice model is active at a time; loading a new model fully replaces the previous one.
/// The active model is not persisted to the database — it resets to a blank default on every
/// application start.
/// Implemented by <see cref="ActiveVoiceModelService"/>.
/// </summary>
public interface IActiveVoiceModelService
{
    /// <summary>
    /// Gets the currently active voice model.
    /// On application start this is a blank default model with no associated file.
    /// </summary>
    VoiceModel ActiveModel { get; }

    /// <summary>
    /// Occurs when <see cref="ActiveModel"/> is replaced by a call to <see cref="LoadFromFile"/>.
    /// </summary>
    event EventHandler? ActiveModelChanged;

    /// <summary>
    /// Replaces <see cref="ActiveModel"/> with a new <see cref="VoiceModel"/> constructed from
    /// the supplied <c>.phonematic</c> file.  The model name is taken from the file name
    /// (without the extension) and <see cref="VoiceModel.LastTrainedAtUtc"/> is inferred from
    /// the file's last-write timestamp.
    /// </summary>
    /// <param name="phonematicFilePath">Absolute path to an existing <c>.phonematic</c> file.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="phonematicFilePath"/> does not exist on disk.
    /// </exception>
    void LoadFromFile(string phonematicFilePath);

    /// <summary>
    /// Copies the active model's <c>.phonematic</c> artefact to <paramref name="destinationPath"/>,
    /// overwriting any existing file at that location.
    /// </summary>
    /// <param name="destinationPath">Absolute path where the exported file should be written.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the active model has no associated file (i.e. the default blank model is active).
    /// </exception>
    void ExportToFile(string destinationPath);
}
