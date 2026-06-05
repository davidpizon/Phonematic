using Avalonia.Controls;
using Phonematic.ViewModels;

namespace Phonematic.Views;

/// <summary>
/// Code-behind for <c>TranscriptionsView.axaml</c>.
/// Triggers the initial data load by executing <see cref="TranscriptionsViewModel.RefreshCommand"/>
/// when the view is attached to the visual tree.
/// </summary>
public partial class TranscriptionsView : UserControl
{
    /// <summary>Initialises the view and its compiled XAML.</summary>
    public TranscriptionsView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override async void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is TranscriptionsViewModel vm)
        {
            await vm.RefreshCommand.ExecuteAsync(null);
        }
    }
}
