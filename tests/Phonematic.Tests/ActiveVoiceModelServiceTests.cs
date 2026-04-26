using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Tests;

/// <summary>
/// Unit tests for <see cref="ActiveVoiceModelService"/>.
/// All tests that require an on-disk file create a temporary file in the system temp
/// directory and clean it up in a <c>finally</c> block.
/// </summary>
public class ActiveVoiceModelServiceTests
{
    // -------------------------------------------------------------------------
    // Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_InitialisesBlankActiveModel()
    {
        var svc = new ActiveVoiceModelService();

        Assert.NotNull(svc.ActiveModel);
        Assert.Equal(string.Empty, svc.ActiveModel.Name);
        Assert.Null(svc.ActiveModel.ModelPath);
        Assert.Null(svc.ActiveModel.LastTrainedAtUtc);
    }

    // -------------------------------------------------------------------------
    // LoadFromFile — happy path
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadFromFile_SetsActiveModelNameFromFileName()
    {
        var svc = new ActiveVoiceModelService();
        var path = CreateTempPhonematicFile("my-speaker");
        try
        {
            svc.LoadFromFile(path);
            Assert.Equal("my-speaker", svc.ActiveModel.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_SetsActiveModelPath()
    {
        var svc = new ActiveVoiceModelService();
        var path = CreateTempPhonematicFile("voice");
        try
        {
            svc.LoadFromFile(path);
            Assert.Equal(path, svc.ActiveModel.ModelPath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_SetsLastTrainedAtUtcFromFileLastWriteTime()
    {
        var svc = new ActiveVoiceModelService();
        var path = CreateTempPhonematicFile("voice");
        try
        {
            var expectedWrite = File.GetLastWriteTimeUtc(path);
            svc.LoadFromFile(path);
            Assert.Equal(expectedWrite, svc.ActiveModel.LastTrainedAtUtc);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_ReplacesExistingActiveModel()
    {
        var svc = new ActiveVoiceModelService();
        var path1 = CreateTempPhonematicFile("first");
        var path2 = CreateTempPhonematicFile("second");
        try
        {
            svc.LoadFromFile(path1);
            svc.LoadFromFile(path2);
            Assert.Equal("second", svc.ActiveModel.Name);
        }
        finally
        {
            File.Delete(path1);
            File.Delete(path2);
        }
    }

    [Fact]
    public void LoadFromFile_RaisesActiveModelChangedEvent()
    {
        var svc = new ActiveVoiceModelService();
        var path = CreateTempPhonematicFile("voice");
        try
        {
            var raised = false;
            svc.ActiveModelChanged += (_, _) => raised = true;
            svc.LoadFromFile(path);
            Assert.True(raised);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_RaisesActiveModelChangedEvent_WithNewModel()
    {
        var svc = new ActiveVoiceModelService();
        var path = CreateTempPhonematicFile("newmodel");
        try
        {
            VoiceModel? receivedModel = null;
            svc.ActiveModelChanged += (_, _) => receivedModel = svc.ActiveModel;
            svc.LoadFromFile(path);
            Assert.NotNull(receivedModel);
            Assert.Equal("newmodel", receivedModel.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // -------------------------------------------------------------------------
    // LoadFromFile — error cases
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadFromFile_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        var svc = new ActiveVoiceModelService();
        Assert.Throws<FileNotFoundException>(() =>
            svc.LoadFromFile(@"C:\DoesNotExist\missing.phonematic"));
    }

    [Fact]
    public void LoadFromFile_DoesNotChangeActiveModel_WhenFileNotFound()
    {
        var svc = new ActiveVoiceModelService();
        var original = svc.ActiveModel;
        try
        {
            svc.LoadFromFile(@"C:\DoesNotExist\missing.phonematic");
        }
        catch (FileNotFoundException) { }

        Assert.Same(original, svc.ActiveModel);
    }

    // -------------------------------------------------------------------------
    // ExportToFile — happy path
    // -------------------------------------------------------------------------

    [Fact]
    public void ExportToFile_CopiesArtefactToDestination()
    {
        var svc = new ActiveVoiceModelService();
        var src = CreateTempPhonematicFile("export-test");
        var dest = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.phonematic");
        try
        {
            svc.LoadFromFile(src);
            svc.ExportToFile(dest);
            Assert.True(File.Exists(dest));
        }
        finally
        {
            File.Delete(src);
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    [Fact]
    public void ExportToFile_OverwritesExistingFile()
    {
        var svc = new ActiveVoiceModelService();
        var src = CreateTempPhonematicFile("overwrite-test");
        var dest = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.phonematic");
        try
        {
            File.WriteAllText(dest, "old content");
            svc.LoadFromFile(src);
            svc.ExportToFile(dest);
            var written = File.ReadAllText(dest);
            Assert.Equal("dummy phonematic content", written);
        }
        finally
        {
            File.Delete(src);
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    // -------------------------------------------------------------------------
    // ExportToFile — error cases
    // -------------------------------------------------------------------------

    [Fact]
    public void ExportToFile_ThrowsInvalidOperationException_WhenNoModelLoaded()
    {
        var svc = new ActiveVoiceModelService();
        Assert.Throws<InvalidOperationException>(() =>
            svc.ExportToFile(Path.Combine(Path.GetTempPath(), "out.phonematic")));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a temporary <c>.phonematic</c> file whose base name is
    /// <paramref name="modelName"/> and returns its full path.
    /// </summary>
    private static string CreateTempPhonematicFile(string modelName)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{modelName}.phonematic");
        File.WriteAllText(path, "dummy phonematic content");
        return path;
    }
}
