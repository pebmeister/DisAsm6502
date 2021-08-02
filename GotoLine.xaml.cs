using System;
using System.ComponentModel;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DisAsm6502
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class GotoLine : INotifyPropertyChanged
    {
        private int _maxLine;

        /// <summary>
        /// Max line number
        /// Set by caller
        /// </summary>
        public int MaxLine
        {
            get => _maxLine;
            set
            {
                _maxLine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GotoLine()
        {
            InitializeComponent();
            PropertyChanged += PropertyChangedEventHandler;
        }

        /// <summary>
        /// When MaxLine is set
        /// Update the text field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, nameof(MaxLine), StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                LineTextBlock.Text = $"Line number (1 - {MaxLine - 1})";
            }
        }

        /// <summary>
        /// Set the selected text to search to line 1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GotoLine_OnLoaded(object sender, RoutedEventArgs e)
        {
            _ = LineTextBox.Focus();
            LineTextBox.SelectedText = "1";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    }
}
