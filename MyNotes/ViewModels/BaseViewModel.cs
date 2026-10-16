using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// Base ViewModel class with common functionality
    /// </summary>
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isLoading;

        /// <summary>
        /// Indicates if the ViewModel is currently loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Called when the ViewModel is initialized
        /// </summary>
        public virtual async Task Initialize()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when the ViewModel is being disposed
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
