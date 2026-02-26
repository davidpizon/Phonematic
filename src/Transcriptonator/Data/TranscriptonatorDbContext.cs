using Microsoft.EntityFrameworkCore;
using Transcriptonator.Models;

namespace Transcriptonator.Data;

public class TranscriptonatorDbContext : DbContext
{
    public DbSet<ProcessedFile> ProcessedFiles => Set<ProcessedFile>();
    public DbSet<TranscriptionChunk> TranscriptionChunks => Set<TranscriptionChunk>();

    private readonly string _dbPath;

    public TranscriptonatorDbContext()
    {
        _dbPath = GetDefaultDbPath();
    }

    public TranscriptonatorDbContext(DbContextOptions<TranscriptonatorDbContext> options)
        : base(options)
    {
        _dbPath = GetDefaultDbPath();
    }

    private static string GetDefaultDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Transcriptonator");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "transcriptonator.db");
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
    }
}
