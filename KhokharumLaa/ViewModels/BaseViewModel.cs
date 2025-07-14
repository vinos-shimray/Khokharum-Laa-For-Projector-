using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KhokharumLaa.ViewModels
{
    /// <summary>
    /// A base class for all ViewModels that implements the INotifyPropertyChanged interface.
    /// This allows the UI to automatically update when a property value changes.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// A helper method to raise the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed. 
        /// This is automatically filled in by the compiler.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
