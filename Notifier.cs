using System.ComponentModel;
using System.Runtime.CompilerServices;
using DisAsm6502.Annotations;

namespace DisAsm6502
{
    public abstract class Notifier : INotifyPropertyChanged
    {
        /// <summary>
        /// Event for property change
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires property changed event
        /// </summary>
        /// <param name="propertyName">not used. defaults to name of caller</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
