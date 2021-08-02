using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DisAsm6502
{
    /// <summary>
    /// Interaction logic for LoadAddress.xaml
    /// </summary>
    public partial class LoadAddress : INotifyPropertyChanged
    {
        private int _maxLoadAddress;

        /// <summary>
        /// Max Load address of code
        /// This is only used for .bin files
        /// </summary>
        public int MaxLoadAddress
        {
            get => _maxLoadAddress;
            set
            {
                _maxLoadAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LoadAddress()
        {
            InitializeComponent();
            PropertyChanged += PropertyChangedEventHandler;
        }

        /// <summary>
        /// Set the default load address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadAddress_OnLoaded(object sender, RoutedEventArgs e)
        {
            _ = LoadAddressTextBox.Focus();
            LoadAddressTextBox.SelectedText = "$0000";
        }

        /// Determine if find can be executed
        /// return true if no other command is running
        private void Close_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !ApplicationCommands.Close.IsRunning() && !ApplicationCommands.Find.IsRunning();
        }

        /// <summary>
        /// Set the DialogResult to false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Close_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    DialogResult = false;
                    e.Command.SetIsRunning(false);
                });
            });
        }

        /// <summary>
        /// Determine if find can be executed
        /// return true if no other command is running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Find_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !ApplicationCommands.Close.IsRunning() && !ApplicationCommands.Find.IsRunning();
        }

        private async void Find_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    DialogResult = true;
                    e.Command.SetIsRunning(false);
                });
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// When MaxLoadAddress is set
        /// Update the text field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, nameof(MaxLoadAddress), StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                LoadAddressTextBlock.Text = $"Load Address ($0000 - ${MaxLoadAddress.ToHexWord()})";
            }
        }
    }
}
