using Microsoft.UI.Xaml.Media.Animation;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Helper class for navigation between pages
    /// </summary>
    public static class NavigationHelper
    {
        public static void NavigateTo<T>(object parameter)
        {
            MainWindow._rootFrame.Navigate(typeof(T), parameter);
        }
        public static void NavigateTo<T>()
        {
            MainWindow._rootFrame.Navigate(typeof(T));
        }
        public static void NavigateTo<T>(NavigationTransitionInfo navigationTransitionInfo)
        {
            MainWindow._rootFrame.Navigate(typeof(T),null,navigationTransitionInfo);
        }

        /// <summary>
        /// Go back to previous page
        /// </summary>
        public static void GoBack()
        {
            if (MainWindow._rootFrame.CanGoBack)
            {
                MainWindow._rootFrame.GoBack();
            }
        }
    }
}
