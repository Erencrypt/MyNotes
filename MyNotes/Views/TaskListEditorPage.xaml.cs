using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using System.Diagnostics;
using WinUI3Localizer;

namespace MyNotes.Views
{
    public sealed partial class TaskListEditorPage : Page
    {
        private readonly ILocalizer _Localizer = Localizer.Get();
        public TaskListEditorViewModel ViewModel { get; set; }

        // Kept in sync with the ColorPicker so SaveButton_Click can read it
        private string _selectedHexColor = "#FF6C757D";

        public TaskListEditorPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<TaskListEditorViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Guid listId)
            {
                await ViewModel.LoadAsync(listId);
                DeleteButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                // Push the loaded color into the ColorPicker imperatively
                HeaderColorPicker.Color = HexConverter.HexToColor(ViewModel.TaskListHeaderColor);
                _selectedHexColor = ViewModel.TaskListHeaderColor;
            }
            else
            {
                await ViewModel.Initialize();
                DeleteButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

                // Default color for a new list
                HeaderColorPicker.Color = HexConverter.HexToColor("#FF6C757D");
                _selectedHexColor = "#FF6C757D";
            }
        }

        private void HeaderColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            _selectedHexColor = HexConverter.ColorToHex(args.NewColor);
        }

        private async void SaveButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var name = ListNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                AppInfoBar.Show("Validation Error", "Name is required", AlertInfoBarSeverity.Warning);
                ListNameTextBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                return;
            }

            var saved = await ViewModel.SaveAsync(name, _selectedHexColor);
            if (saved != null)
                NavigationHelper.GoBack();
        }

        private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            NavigationHelper.GoBack();
        }

        private async void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            DeleteFlyout.Hide();

            try
            {
                await ViewModel.DeleteAsync();
                NavigationHelper.GoBack();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting task list: {ex}");
                AppInfoBar.Show("Error", $"An error occurred while deleting the task list: {ex.Message}", AlertInfoBarSeverity.Error);
            }
        }
    }
}
