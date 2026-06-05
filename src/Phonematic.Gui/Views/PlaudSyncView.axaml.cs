using Avalonia.Controls;
using Avalonia.Input.Platform;
using Phonematic.ViewModels;

namespace Phonematic.Views;

/// <summary>
/// Code-behind for <c>PlaudSyncView.axaml</c>.
/// Wires the clipboard copy interaction for <see cref="PlaudSyncViewModel"/> using
/// Avalonia's <see cref="IClipboard"/> API.
/// </summary>
public partial class PlaudSyncView : UserControl
{
    /// <summary>Initialises the view and its compiled XAML.</summary>
    public PlaudSyncView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
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
