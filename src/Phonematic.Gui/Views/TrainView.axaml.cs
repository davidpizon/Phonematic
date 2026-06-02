using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Phonematic.ViewModels;

namespace Phonematic.Views;

/// <summary>
/// Code-behind for <c>TrainView.axaml</c>.
/// Wires the folder-picker interaction expected by <see cref="TrainViewModel"/> using
/// Avalonia's <see cref="IStorageProvider"/> API, keeping the ViewModel free of any
/// direct UI or platform dependencies.
/// </summary>
public partial class TrainView : UserControl
{
    /// <summary>Initialises the view and its compiled XAML.</summary>
    public TrainView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TrainViewModel vm)
        {
            vm.BrowseFolderInteraction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Folder with Input Sets",
                    AllowMultiple = false,
                });

                if (folders.Count > 0)
                {
                    var path = folders[0].TryGetLocalPath();
                    if (path != null) vm.LoadInputSets(path);
                }
            };
        }
    }
}
