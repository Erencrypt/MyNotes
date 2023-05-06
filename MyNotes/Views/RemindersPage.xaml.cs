using Microsoft.UI.Xaml.Controls;

using MyNotes.ViewModels;

namespace MyNotes.Views;

public sealed partial class RemindersPage : Page
{
    public RemindersViewModel ViewModel
    {
        get;
    }

    public RemindersPage()
    {
        ViewModel = App.GetService<RemindersViewModel>();
        InitializeComponent();
    }
}
