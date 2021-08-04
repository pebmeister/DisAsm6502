using System;
using System.ComponentModel;
using System.Windows;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
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
            DataContext = View;
            FormatQueueProcessor.Start();
        }

        private FormatQueueProcessor _formatQueueProcessor;

        public FormatQueueProcessor FormatQueueProcessor => _formatQueueProcessor ??
                                                            (_formatQueueProcessor = new FormatQueueProcessor {Owner = this});

        private ICommand _gotoLine;

        public ICommand GotoLine => _gotoLine ?? (_gotoLine = FindResource("GotoLine") as ICommand);

        private ICommand _formatLine;

        public ICommand FormatLine => _formatLine ?? (_formatLine = FindResource("FormatLine") as ICommand);

        private ICommand _setLoadAddress;

        public ICommand SetLoadAddress => _setLoadAddress ?? (_setLoadAddress = FindResource("SetLoadAddress") as ICommand);

        private ViewModel.ViewModel _view;
        public ViewModel.ViewModel View => _view ?? (_view = new ViewModel.ViewModel {Owner = this});

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
                    var fi = new FileInfo(FileName);
                    var saveFileDlg = new SaveFileDialog
                    {
                        FileName = $"{fi.Name.Replace(fi.Extension,"")}",
                        DefaultExt = ".asm",
                        Filter = "Assembler files |*.asm|Text files (*.txt)|*.txt|All files (*.*)|*.*"
                    };

                    var result = saveFileDlg.ShowDialog(this);
                    if (!result.Value)
                    {
                        e.Command.SetIsRunning(false);
                        return;
                    }

                    using (TextWriter writer = File.CreateText(saveFileDlg.FileName))
                    {
                        fi = new FileInfo(saveFileDlg.FileName);

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

            if (FormatLine.IsRunning() || ApplicationCommands.Open.IsRunning() ||
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
        /// Open .prg or .bin file and load it
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
                        Filter = "Commodore Files|*.prg;*.bin;|Program Files (.prg)|*.prg|Bin Files (.bin)|*.bin|All files (*.*)|*.*"
                    };

                    var selected = openFileDlg.ShowDialog();
                    if (!selected.HasValue || !selected.Value)
                    {
                        e.Command.SetIsRunning(false);
                        return;
                    }

                    View.UsedSymbols.Clear();

                    FileName = openFileDlg.FileName;
                    var fi = new FileInfo(FileName);
                    View.BinFile = string.Compare(fi.Extension, ".bin", StringComparison.CurrentCultureIgnoreCase) == 0;
                    // Read the contents of the file into a stream
                    var fileStream = openFileDlg.OpenFile();
                    using (var reader = new BinaryReader(fileStream))
                    {
                        View.Data = reader.ReadBytes((int)fileStream.Length);
                    }

                    MainListBox.IsEnabled = View.Data.Length > 2;

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
            if (GotoLine.IsRunning() || FormatLine.IsRunning() || SetLoadAddress.IsRunning() ||
                ApplicationCommands.Open.IsRunning() || ApplicationCommands.Save.IsRunning())
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

            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    lock (FormatQueueProcessor.FormatQueueLock)
                    {
                        _ = int.TryParse(e.Parameter.ToString(), out var format);

                        var items = (from object selectedItem in MainListBox.SelectedItems
                                select selectedItem as AssemblerLine).OrderBy(selectedItem => selectedItem.RowIndex)
                            .ToList();

                        for (var index = 0; index < items.Count; ++index)
                        {
                            var item = items[index];
                            FormatQueueProcessor.FormatQueue.Enqueue(new Tuple<int, int>(item.Address, format));

                            // The following code is needed because
                            // when a line is reformatted, it is possible for
                            // the formatting to insert a new line that is formatted as a byte
                            // which could create a byte island. This checks to see if
                            // the next line is intended to be formatted the same.
                            // If it is then we will format the byte island the way it
                            // was intended.
                            if (item.Size <= 1 || index + 1 >= items.Count ||
                                item.RowIndex != items[index + 1].RowIndex - 1)
                            {
                                continue;
                            }

                            for (var sz = 1; sz < item.Size; ++sz)
                            {
                                FormatQueueProcessor.FormatQueue.Enqueue(new Tuple<int, int>(item.Address + sz,
                                    format));
                            }
                        }
                    }

                    e.Command.SetIsRunning(false);
                });
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

            if (GotoLine.IsRunning() || FormatLine.IsRunning() || SetLoadAddress.IsRunning() ||
                ApplicationCommands.Open.IsRunning() || ApplicationCommands.Save.IsRunning())
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        /// <summary>
        /// SetLoadAddress_OnExecuted
        ///
        /// Set the load address for .bin file
        /// It is used to determine local labels
        /// </summary>
        /// <param name="sender">unused origin of command</param>
        /// <param name="e">parameter for set load address</param>
        private async void SetLoadAddress_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var dlg = new LoadAddress { Owner = this, MaxLoadAddress = 0xFFFF - View.Data.Length, DefaultAddress = $"${View.LoadAddress.ToHexWord()}"};

                    var result = dlg.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        var text = dlg.LoadAddressTextBox.Text.Trim();
                        if (text.Length > 0)
                        {
                            try
                            {
                                int address;
                                if (text.Length > 1 && text.StartsWith("$"))
                                {
                                   address = int.Parse(text.Remove(0,1), System.Globalization.NumberStyles.HexNumber);
                                }
                                else if (text.Length > 2 && text.StartsWith("0x"))
                                {
                                    address = int.Parse(text.Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                                }
                                else
                                {
                                    address = int.Parse(text);
                                }
                                View.LoadAddress = address;
                            }
                            // ReSharper disable once EmptyGeneralCatchClause
                            catch (Exception)
                            {
                            }
                        }
                    }
                    dlg.Close();

                    e.Command.SetIsRunning(false);
                });
            });
        }

        /// <summary>
        /// SetLoadAddress_OnCanExecute
        ///
        /// Determine if set load address can execute
        /// </summary>
        /// <param name="sender">unused origin of command</param>
        /// <param name="e">parameters for SetLoadAddress_OnCanExecute</param>
        private void SetLoadAddress_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // if there is no data or not a .bin file then wen cant execute
            if (View?.Data == null || View.Data.Length < 1 || !View.BinFile)
            {
                e.CanExecute = false;
                return;
            }

            // make sure no other command is running
            if (GotoLine.IsRunning() || FormatLine.IsRunning() || SetLoadAddress.IsRunning() ||
                ApplicationCommands.Open.IsRunning() || ApplicationCommands.Save.IsRunning())
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        /// <summary>
        /// GotoLine_OnExecuted
        ///
        /// Go to a line number
        /// </summary>
        /// <param name="sender">unused origin of command</param>
        /// <param name="e">parameters for GotoLine_OnExecuted</param>
        private async void GotoLine_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Command.SetIsRunning(true);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var parsed = false;
                    var line = 0;
                    var dlg = new GotoLine {Owner = this, MaxLine = MainListBox.Items.Count - 1};
                    var result = dlg.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        parsed = int.TryParse(dlg.LineTextBox.Text, out line);
                    }
                    dlg.Close();
                    MainListBox.Focus();

                    if (parsed && line > 0 && line < MainListBox.Items.Count)
                    {
                        MainListBox.SelectedIndex = line - 1;
                        MainListBox.ScrollIntoView(MainListBox.SelectedItems[0]);
                    }
                    e.Command.SetIsRunning(false);
                });
            });
        }

        /// <summary>
        /// GotoLine_OnCanExecute
        ///
        /// Determine if Goto Line can be executed
        /// </summary>
        /// <param name="sender">unused origin of command</param>
        /// <param name="e">parameters for GotoLine_OnCanExecute</param>
        private void GotoLine_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (View?.Data == null || View.Data.Length < 1)
            {
                e.CanExecute = false;
                return;
            }

            if (GotoLine.IsRunning() || FormatLine.IsRunning() || SetLoadAddress.IsRunning() ||
                ApplicationCommands.Open.IsRunning() || ApplicationCommands.Save.IsRunning())
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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Main window is closing
        /// </summary>
        /// <param name="sender">unused origin of command</param>
        /// <param name="e">parameters for closing window</param>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            FormatQueueProcessor.Stop();
        }
    }
}
