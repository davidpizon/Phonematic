using Avalonia.Controls;
using Avalonia.Input.Platform;
using Phonematic.ViewModels;

namespace Phonematic.Views;

public partial class PlaudSyncView : UserControl
{
    public PlaudSyncView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is PlaudSyncViewModel vm)
        {
            vm.CopyToClipboardInteraction = async text =>
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(text);
            };
        }
    }
}
