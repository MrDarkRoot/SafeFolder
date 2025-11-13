using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SafeFolder.ViewModels
{
    /// <summary>
    /// A base class for all ViewModels that implements the INotifyPropertyChanged interface.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for a given property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed. 
        /// This is automatically filled by the compiler.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
