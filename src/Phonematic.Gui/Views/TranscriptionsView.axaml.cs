using Avalonia.Controls;
using Phonematic.ViewModels;

namespace Phonematic.Views;

public partial class TranscriptionsView : UserControl
{
    public TranscriptionsView()
    {
        InitializeComponent();
    }

    protected override async void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is TranscriptionsViewModel vm)
        {
            await vm.RefreshCommand.ExecuteAsync(null);
        }
    }
}
