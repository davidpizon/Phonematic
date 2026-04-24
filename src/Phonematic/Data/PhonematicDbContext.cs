using Microsoft.EntityFrameworkCore;
using Phonematic.Models;

namespace Phonematic.Data;

public class PhonematicDbContext : DbContext
{
    public DbSet<ProcessedFile> ProcessedFiles => Set<ProcessedFile>();
    public DbSet<TranscriptionChunk> TranscriptionChunks => Set<TranscriptionChunk>();
    public DbSet<PlaudRecording> PlaudRecordings => Set<PlaudRecording>();

    private readonly string _dbPath;

    public PhonematicDbContext()
    {
        _dbPath = GetDefaultDbPath();
    }

    public PhonematicDbContext(DbContextOptions<PhonematicDbContext> options)
        : base(options)
    {
        _dbPath = GetDefaultDbPath();
    }

    private static string GetDefaultDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Phonematic");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "Phonematic.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

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
