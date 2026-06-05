using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Phonematic.ViewModels;

namespace Phonematic.Views;

/// <summary>
/// Code-behind for <c>SettingsView.axaml</c>.
/// Wires the folder-picker interaction for the output directory using
/// Avalonia's <see cref="IStorageProvider"/> API.
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>Initialises the view and its compiled XAML.</summary>
    public SettingsView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is SettingsViewModel vm)
        {
            vm.BrowseOutputDirectoryInteraction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Output Directory",
                    AllowMultiple = false
                });

                if (folders.Count > 0)
                {
                    var path = folders[0].TryGetLocalPath();
                    if (path != null) vm.OutputDirectory = path;
                }
            };
        }
    }
}
