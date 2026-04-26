using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Phonematic.ViewModels;

namespace Phonematic.Views;

/// <summary>
/// Code-behind for <c>ModelView.axaml</c>.
/// Wires the file-picker interactions expected by <see cref="ModelViewModel"/> using
/// Avalonia's <see cref="IStorageProvider"/> API, keeping the ViewModel free of any
/// direct UI or platform dependencies.
/// </summary>
public partial class ModelView : UserControl
{
    /// <summary>Initialises the view and its compiled XAML.</summary>
    public ModelView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ModelViewModel vm)
        {
            vm.BrowseLoadFileInteraction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return null;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Load Voice Model",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Phonematic Voice Model")
                        {
                            Patterns = ["*.phonematic"],
                        }
                    ],
                });

                return files.Count > 0 ? files[0].TryGetLocalPath() : null;
            };

            vm.BrowseSaveFileInteraction = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return null;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export Voice Model",
                    SuggestedFileName = string.IsNullOrWhiteSpace(vm.ModelName)
                        ? "model"
                        : vm.ModelName,
                    DefaultExtension = "phonematic",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Phonematic Voice Model")
                        {
                            Patterns = ["*.phonematic"],
                        }
                    ],
                });

                return file?.TryGetLocalPath();
            };
        }
    }
}
