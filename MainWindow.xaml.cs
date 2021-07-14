using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DisAsm6502.Annotations;
using DisAsm6502.Model;
using Microsoft.Win32;

namespace DisAsm6502
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    // ReSharper disable once UnusedMember.Global
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            View = new ViewModel.ViewModel();
            DataContext = View;
        }

        /// <summary>
        /// ViewModel
        /// </summary>
        private ViewModel.ViewModel View { get; }

        private string _filename;

        /// <summary>
        /// File being disassembled
        /// </summary>
        public string FileName
        {
            get => _filename;
            set
            {
                _filename = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Save_OnExecuted
        /// 
        /// Save Command execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Save_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var saveFileDlg = new SaveFileDialog
                    {
                        FileName = "filename",
                        DefaultExt = ".asm",
                        Filter = "Assembler file (.asm)|*.asm|Text files (*.txt)|*.txt|All files (*.*)|*.*"
                    };

                    var result = saveFileDlg.ShowDialog(this);
                    if (!result.Value)
                    {
                        e.Command.SetIsRunning(false);
                        return;
                    }

                    using (TextWriter writer = File.CreateText(saveFileDlg.FileName))
                    {
                        var fi = new FileInfo(FileName);

                        writer.WriteLine($"; File created from {fi.Name} by DisAsm6502");
                        writer.WriteLine();

                        foreach (var s in View.SymCollection)
                        {
                            writer.WriteLine(s);
                        }

                        writer.WriteLine();

                        foreach (var assemblerLine in View.AssemblerLineCollection)
                        {
                            writer.WriteLine(assemblerLine);
                        }

                        writer.WriteLine();
                    }

                    e.Command.SetIsRunning(false);
                });
            });
        }

        /// <summary>
        /// Save_OnCanExecute
        ///
        /// Determine if Save can be executed
        /// Disable if no file loaded or if eny other command is being executed
        /// </summary>
        /// <param name="sender">Save button</param>
        /// <param name="e">execution event arguments</param>
        private void Save_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (View?.Data == null || View.Data.Length < 1)
            {
                e.CanExecute = false;
                return;
            }

            var formatLine = ((FrameworkElement)sender).FindResource("FormatLine") as ICommand;
            if (formatLine.IsRunning() || ApplicationCommands.Open.IsRunning() ||
                ApplicationCommands.Save.IsRunning())
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        /// <summary>
        /// Open_OnExecuted
        ///
        /// Open .prg file and load it
        /// </summary>
        /// <param name="sender">Open button</param>
        /// <param name="e">execution event arguments</param>
        private async void Open_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var openFileDlg = new OpenFileDialog
                    {
                        Filter = "Program Files (.prg)|*.prg|Text files (*.txt)|*.txt|All files (*.*)|*.*"
                    };

                    var selected = openFileDlg.ShowDialog();
                    if (!selected.HasValue || !selected.Value)
                    {
                        e.Command.SetIsRunning(false);
                        return;
                    }

                    View.UsedSymbols.Clear();

                    FileName = openFileDlg.FileName;

                    // Read the contents of the file into a stream
                    var fileStream = openFileDlg.OpenFile();
                    using (var reader = new BinaryReader(fileStream))
                    {
                        View.Data = reader.ReadBytes((int)fileStream.Length);
                    }

                    MainListBox.IsEnabled = View.Data.Length > 0;

                    e.Command.SetIsRunning(false);
                });
            });
        }

        /// <summary>
        /// Open_OnCanExecute
        ///
        /// Determine if Open can be executed
        /// Disable if no file loaded or if eny other command is being executed
        /// </summary>
        /// <param name="sender">Open button</param>
        /// <param name="e">can execute command args</param>
        private void Open_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var formatLine = ((FrameworkElement)sender).FindResource("FormatLine") as ICommand;
            if (formatLine.IsRunning() || ApplicationCommands.Open.IsRunning() ||
                ApplicationCommands.Save.IsRunning())
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        /// <summary>
        /// FormatLine_OnExecuted
        ///
        /// Format the selected line(s) by byte word or opcode
        /// </summary>
        /// <param name="sender">Format context menu</param>
        /// <param name="e">execution event arguments</param>
        private async void FormatLine_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var items = new List<int>(0);
                    items.AddRange(from AssemblerLine selectedItem in MainListBox.SelectedItems
                                   select selectedItem.Address);

                    _ = int.TryParse(e.Parameter.ToString(), out var format);
                    foreach (var item in items)
                    {
                        foreach (var line in View.AssemblerLineCollection)
                        {
                            if (line.Address != item)
                            {
                                continue;
                            }

                            line.Format = format;
                            break;
                        }
                    }

                    View.RebuildAssemblerLines();

                });
                e.Command.SetIsRunning(false);
            });
        }

        /// <summary>
        /// FormatLine_OnCanExecute
        ///
        /// Format the selected line based on parameter
        /// </summary>
        /// <param name="sender">ContextMenu MenuItem</param>
        /// <param name="e">Parameter contains the style to format the bytes as</param>
        private void FormatLine_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (View?.Data == null || View.Data.Length < 1)
            {
                e.CanExecute = false;
                return;
            }

            var formatLine = ((FrameworkElement)sender).FindResource("FormatLine") as ICommand;
            if (formatLine.IsRunning() || ApplicationCommands.Open.IsRunning() ||
                ApplicationCommands.Save.IsRunning())
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        /// <summary>
        /// Event when a property changed value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when a property value changes
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
