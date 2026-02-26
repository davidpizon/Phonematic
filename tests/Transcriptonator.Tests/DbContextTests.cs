using Microsoft.EntityFrameworkCore;
using Transcriptonator.Data;
using Transcriptonator.Models;

namespace Transcriptonator.Tests;

public class DbContextTests : IDisposable
{
    private readonly TranscriptonatorDbContext _db;

    public DbContextTests()
    {
        var options = new DbContextOptionsBuilder<TranscriptonatorDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new TranscriptonatorDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task CanInsertAndRetrieveProcessedFile()
    {
        var file = new ProcessedFile
        {
            FilePath = "/test/audio.mp3",
            FileHash = "abc123",
            FileSizeBytes = 1024,
            TranscriptionPath = "/output/audio.txt",
            TranscribedAtUtc = DateTime.UtcNow,
            WhisperModel = "small",
            AudioDurationSeconds = 60.0,
            TranscriptionDurationSeconds = 10.0
        };

        _db.ProcessedFiles.Add(file);
        await _db.SaveChangesAsync();

        var retrieved = await _db.ProcessedFiles.FirstAsync();
        Assert.Equal("/test/audio.mp3", retrieved.FilePath);
        Assert.Equal("abc123", retrieved.FileHash);
    }

    [Fact]
    public async Task UniqueConstraint_PreventseDuplicateFilePathHash()
    {
        var file1 = new ProcessedFile
        {
            FilePath = "/test/audio.mp3",
            FileHash = "abc123",
            FileSizeBytes = 1024,
            TranscriptionPath = "/output/audio.txt",
            TranscribedAtUtc = DateTime.UtcNow,
            WhisperModel = "small"
        };

        var file2 = new ProcessedFile
        {
            FilePath = "/test/audio.mp3",
            FileHash = "abc123",
            FileSizeBytes = 1024,
            TranscriptionPath = "/output/audio2.txt",
            TranscribedAtUtc = DateTime.UtcNow,
            WhisperModel = "small"
        };

        _db.ProcessedFiles.Add(file1);
        await _db.SaveChangesAsync();

        _db.ProcessedFiles.Add(file2);
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task CascadeDelete_RemovesChunks()
    {
        var file = new ProcessedFile
        {
            FilePath = "/test/audio.mp3",
            FileHash = "abc123",
            FileSizeBytes = 1024,
            TranscriptionPath = "/output/audio.txt",
            TranscribedAtUtc = DateTime.UtcNow,
            WhisperModel = "small"
        };

        _db.ProcessedFiles.Add(file);
        await _db.SaveChangesAsync();

        _db.TranscriptionChunks.Add(new TranscriptionChunk
        {
            ProcessedFileId = file.Id,
            ChunkIndex = 0,
            Text = "test chunk",
            Embedding = new byte[1536]
        });
        await _db.SaveChangesAsync();

        Assert.Equal(1, await _db.TranscriptionChunks.CountAsync());

        _db.ProcessedFiles.Remove(file);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _db.TranscriptionChunks.CountAsync());
    }

    [Fact]
    public async Task SameFilePath_DifferentHash_AllowsBoth()
    {
        var file1 = new ProcessedFile
        {
            FilePath = "/test/audio.mp3",
            FileHash = "hash1",
            FileSizeBytes = 1024,
            TranscriptionPath = "/output/audio.txt",
            TranscribedAtUtc = DateTime.UtcNow,
            WhisperModel = "small"
        };

        var file2 = new ProcessedFile
        {
            FilePath = "/test/audio.mp3",
            FileHash = "hash2",
            FileSizeBytes = 2048,
            TranscriptionPath = "/output/audio2.txt",
            TranscribedAtUtc = DateTime.UtcNow,
            WhisperModel = "small"
        };

        _db.ProcessedFiles.AddRange(file1, file2);
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.ProcessedFiles.CountAsync());
    }
}
