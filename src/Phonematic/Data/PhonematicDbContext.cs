using Microsoft.EntityFrameworkCore;
using Phonematic.Models;

namespace Phonematic.Data;

/// <summary>
/// EF Core <see cref="DbContext"/> for the Phonematic SQLite database located at
/// <c>%LOCALAPPDATA%\Phonematic\Phonematic.db</c>.
/// Exposes three <see cref="DbSet{TEntity}"/> properties and configures indexes,
/// unique constraints, and a cascade-delete relationship via <see cref="OnModelCreating"/>.
/// When instantiated without <see cref="DbContextOptions"/> the default SQLite path is
/// used; when options are provided (e.g. in-memory for tests) those take precedence.
/// </summary>
public class PhonematicDbContext : DbContext
{
    /// <summary>Gets the set of transcribed audio file records.</summary>
    public DbSet<ProcessedFile> ProcessedFiles => Set<ProcessedFile>();

    /// <summary>Gets the set of text chunks with vector embeddings derived from transcribed files.</summary>
    public DbSet<TranscriptionChunk> TranscriptionChunks => Set<TranscriptionChunk>();

    /// <summary>Gets the set of recordings fetched from the PLAUD cloud API.</summary>
    public DbSet<PlaudRecording> PlaudRecordings => Set<PlaudRecording>();

    private readonly string _dbPath;

    /// <summary>
    /// Initialises a new instance using the default database path resolved from
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/>.
    /// </summary>
    public PhonematicDbContext()
    {
        _dbPath = GetDefaultDbPath();
    }

    /// <summary>
    /// Initialises a new instance with the supplied <paramref name="options"/>.
    /// Used by the DI container (which configures the SQLite connection string) and
    /// by unit tests (which use an in-memory connection).
    /// </summary>
    /// <param name="options">EF Core context options.</param>
    public PhonematicDbContext(DbContextOptions<PhonematicDbContext> options)
        : base(options)
    {
        _dbPath = GetDefaultDbPath();
    }

    /// <summary>
    /// Resolves the default database file path to
    /// <c>%LOCALAPPDATA%\Phonematic\Phonematic.db</c>, creating the directory if needed.
    /// </summary>
    /// <returns>Absolute path to the SQLite database file.</returns>
    private static string GetDefaultDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Phonematic");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "Phonematic.db");
    }

    /// <summary>
    /// Configures SQLite as the database provider when no provider has been set via
    /// <see cref="DbContextOptions"/> (i.e. when the parameterless constructor is used).
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    /// <summary>
    /// Configures the EF Core model:
    /// <list type="bullet">
    ///   <item><see cref="ProcessedFile"/>: non-unique index on <c>FilePath</c>; unique composite index on (<c>FilePath</c>, <c>FileHash</c>).</item>
    ///   <item><see cref="TranscriptionChunk"/>: FK to <see cref="ProcessedFile"/> with cascade delete.</item>
    ///   <item><see cref="PlaudRecording"/>: unique index on <c>PlaudFileId</c>.</item>
    /// </list>
    /// </summary>
    /// <param name="modelBuilder">The model builder provided by EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FilePath);
            entity.HasIndex(e => new { e.FilePath, e.FileHash }).IsUnique();
        });

        modelBuilder.Entity<TranscriptionChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ProcessedFile)
                  .WithMany(p => p.Chunks)
                  .HasForeignKey(e => e.ProcessedFileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlaudRecording>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlaudFileId).IsUnique();
        });
    }
}
