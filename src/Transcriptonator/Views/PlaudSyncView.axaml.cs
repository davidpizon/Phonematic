using Avalonia.Controls;
using Transcriptonator.ViewModels;

namespace Transcriptonator.Views;

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
