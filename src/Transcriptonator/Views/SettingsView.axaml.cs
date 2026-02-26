using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Transcriptonator.ViewModels;

namespace Transcriptonator.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

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
