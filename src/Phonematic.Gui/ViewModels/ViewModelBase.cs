using CommunityToolkit.Mvvm.ComponentModel;

namespace Phonematic.ViewModels;

/// <summary>
/// Abstract base class for all ViewModels in the application.
/// Extends <see cref="ObservableObject"/> from CommunityToolkit.Mvvm to provide
/// <see cref="System.ComponentModel.INotifyPropertyChanged"/> support and the
/// <c>[ObservableProperty]</c> / <c>[RelayCommand]</c> source-generator infrastructure.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
