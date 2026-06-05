using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Phonematic.ViewModels;

namespace Phonematic.Views;

/// <summary>
/// Code-behind for <c>TranscribeView.axaml</c>.
/// Wires the file-picker and folder-picker interactions for <see cref="TranscribeViewModel"/>.
/// </summary>
public partial class TranscribeView : UserControl
{
    /// <summary>Initialises the view and its compiled XAML.</summary>
    public TranscribeView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TranscribeViewModel vm)
        {
            vm.BrowseFileInteraction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Audio File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Audio Files") { Patterns = new[]
                            { "*.mp3", "*.wav", "*.aiff", "*.aif", "*.wma", "*.m4a", "*.ogg", "*.flac", "*.voc" } }
                    }
                });

                if (files.Count > 0)
                {
                    var path = files[0].TryGetLocalPath();
                    if (path != null) vm.LoadFiles(path);
                }
            };

            vm.BrowseFolderInteraction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Folder with Audio Files",
                    AllowMultiple = false
                });

                if (folders.Count > 0)
                {
                    var path = folders[0].TryGetLocalPath();
                    if (path != null) vm.LoadFiles(path);
                }
            };
        }
    }
}
