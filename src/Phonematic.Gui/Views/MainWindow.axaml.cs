using Avalonia.Controls;

namespace Phonematic.Views;

/// <summary>The application's single main window; hosts the tab strip and the first-run setup overlay.</summary>
public partial class MainWindow : Window
{
    /// <summary>Initialises the window and its compiled XAML.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
