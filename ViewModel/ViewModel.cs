using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using DisAsm6502.Model;
using static DisAsm6502.Extensions;

// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

namespace DisAsm6502.ViewModel
{
    /// <summary>
    /// Class to display model
    /// </summary>
    public class ViewModel : Notifier
    {
        private object _owner;

        public object Owner
        {
            get => _owner;
            set
            {
                _owner = value;
                OnPropertyChanged();
            }
        }

        private Collection<Tuple<int, byte>> _immediateValues = new Collection<Tuple<int, byte>>();

        public Collection<Tuple<int, byte>> ImmediateValues
        {
            get => _immediateValues;
            set
            {
                _immediateValues = value;
                OnPropertyChanged();
            }
        }

        private bool _binFile;

        /// <summary>
        /// True if bin file
        /// There is no load address provided
        /// </summary>
        public bool BinFile
        {
            get => _binFile;
            set
            {
                _binFile = value;
                OnPropertyChanged();
            }
        }

        private bool _building;

        /// <summary>
        /// True if initial construction Assembler Lines
        /// </summary>
        public bool Building
        {
            get => _building;
            set
            {
                _building = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _symCollection = new ObservableCollection<string>();

        /// <summary>
        /// Holds symbols
        /// Backing data for symbols
        /// </summary>
        public ObservableCollection<string> SymCollection
        {
            get => _symCollection;
            set
            {
                _symCollection = value;
                OnPropertyChanged();
            }
        }

        private string _org;

        /// <summary>
        /// string representation of ORG directive
        /// </summary>
        public string Org
        {
            get => _org;
            set
            {
                _org = value;
                OnPropertyChanged();
            }
        }

        private int _loadAddress;

        /// <summary>
        /// Address the program loads at
        /// First 2 bytes of .prg file
        /// or the Load address set by user if
        /// this is a .bin file
        /// Causes Org to be recalculated
        /// </summary>
        public int LoadAddress
        {
            get => _loadAddress;
            set
            {
                if (value < 0 || value > 0xFFFF)
                {
                    return;
                }

                _loadAddress = value;
                OnPropertyChanged();
            }
        }

        private byte[] _data;

        /// <summary>
        /// Bytes read from the .prg or .bin files
        /// Causes initial parse
        /// </summary>
        public byte[] Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Symbols used in the program
        /// </summary>
        public Dictionary<int, string> UsedSymbols = new Dictionary<int, string>();

        /// <summary>
        /// 
        /// Symbols used in the program
        /// </summary>
        public Dictionary<int, string> LocalSymbols = new Dictionary<int, string>();

        /// <summary>
        /// Symbols used in the program
        /// </summary>
        public Dictionary<int, string> UsedLocalSymbols = new Dictionary<int, string>();

        /// <summary>
        /// Determines if symbol is within the program if external
        /// </summary>
        /// <param name="addr">address of symbol</param>
        /// <returns>true if symbol is local</returns>
        private bool IsSymLocal(int addr)
        {
            return addr >= LoadAddress && addr <= LoadAddress + Data.Length - (BinFile ? 0 : 2);
        }

        /// <summary>
        /// Build the local symbols
        /// The label will be the line number
        /// </summary>
        private void BuildLocalSymbols()
        {
            LocalSymbols.Clear();
            UsedLocalSymbols.Clear();

            var index = 0;
            foreach (var assemblerLine in AssemblerLineCollection)
            {
                LocalSymbols.Add(assemblerLine.Address, $"L_{index++:D4}");
            }
        }

        /// <summary>
        /// Get symbol for an address
        /// </summary>
        /// <param name="symAddress">address of symbol</param>
        /// <param name="len">length of address (1 for page zero 2 for all others</param>
        /// <param name="found"></param>
        /// <returns>symbol</returns>
        private string GetSymCommon(int symAddress, int len, out bool found)
        {
            const int localSymRange = 128;
            const int externSymRange = 2;
            found = false;
            var sym = len == 1 ? $"${symAddress.ToHex()}" : $"${symAddress.ToHexWord()}";
            if (LoadAddress != 0 && IsSymLocal(symAddress))
            {
                for (var range = 0; range < localSymRange; ++range)
                {
                    if (symAddress - range < 0)
                    {
                        continue;
                    }

                    if (!LocalSymbols.TryGetValue(symAddress - range, out var tempSym))
                    {
                        continue;
                    }

                    sym = tempSym;
                    if (!UsedLocalSymbols.ContainsKey(symAddress - range))
                    {
                        UsedLocalSymbols.Add(symAddress - range, sym);
                    }

                    if (range != 0)
                    {
                        sym = $"{sym} + {range}";
                    }

                    found = true;
                    return sym;
                }
            }
            else
            {
                for (var range = 0; range < externSymRange; ++range)
                {
                    if (!BuiltInSymbols.TryGetValue(symAddress - range, out var tempSym))
                    {
                        continue;
                    }

                    sym = tempSym;
                    if (!UsedSymbols.ContainsKey(symAddress - range))
                    {
                        UsedSymbols.Add(symAddress - range, sym);
                    }

                    if (range != 0)
                    {
                        sym = $"{sym} + {range}";
                    }

                    found = true;
                    return sym;
                }
            }

            return sym;
        }

        /// <summary>
        /// Get a 2 byte symbol
        /// </summary>
        /// <param name="symAddress">2 byte address</param>
        /// <param name="found"></param>
        /// <returns>symbol for address</returns>
        private string GetWordSym(int symAddress, out bool found)
        {
            return GetSymCommon(symAddress, 2, out found);
        }

        /// <summary>
        /// Get a 1 byte symbol
        /// </summary>
        /// <param name="symAddress">1 byte adddress</param>
        /// <param name="found"></param>
        /// <returns>symbol for address</returns>
        private string GetByteSym(int symAddress, out bool  found)
        {
            found = true;
            return GetSymCommon(symAddress, 1, out _);
        }

        /// <summary>
        /// Format an opcode
        /// </summary>
        /// <param name="op">Opcode to format</param>
        /// <param name="offset">offset of Data to format</param>
        /// <param name="symFound"></param>
        /// <returns>formatted opcode</returns>
        private string FormatOpCode(Op op, int offset, out bool symFound)
        {
            var str = $"{op.Opcode.ToUpperInvariant()} ";
            var symAddress = offset + 2 < Data.Length ? Data[offset + 1] + Data[offset + 2] * 256 : -1;
            var pgZeroSymAddress = offset + 1 < Data.Length ? Data[offset + 1] : -1;

            string sym;
            int target;
            int d;

            symFound = true;

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (op.Mode)
            {
                case AddressingModes.I:
                    break;

                case AddressingModes.Im:
                    sym = $"${pgZeroSymAddress.ToHex()}";
                    str += $"#{sym}";
                    break;

                case AddressingModes.Zp:
                    sym = GetByteSym(pgZeroSymAddress, out symFound);
                    str += $"{sym}";
                    break;

                case AddressingModes.Zpi:
                    sym = GetByteSym(pgZeroSymAddress, out symFound);
                    str += $"({sym})";
                    break;

                case AddressingModes.Zpx:
                    sym = GetByteSym(pgZeroSymAddress, out symFound);
                    str += $"{sym},x";
                    break;

                case AddressingModes.Zpy:
                    sym = GetByteSym(pgZeroSymAddress, out symFound);
                    str += $"{sym},y";
                    break;

                case AddressingModes.Zpix:
                    sym = GetByteSym(pgZeroSymAddress, out symFound);
                    str += $"({sym},x)";
                    break;

                case AddressingModes.Zpiy:
                    sym = GetByteSym(pgZeroSymAddress, out symFound);
                    str += $"({sym}),y";
                    break;

                case AddressingModes.A:
                    sym = GetWordSym(symAddress, out symFound);
                    str += sym;
                    break;

                case AddressingModes.Aix:
                    sym = GetWordSym(symAddress, out symFound);
                    str += $"({sym},x)";
                    break;

                case AddressingModes.Ax:
                    sym = GetWordSym(symAddress, out symFound);
                    str += $"{sym},x";
                    break;

                case AddressingModes.Ay:
                    sym = GetWordSym(symAddress, out symFound);
                    str += $"{sym},y";
                    break;

                case AddressingModes.Ind:
                    sym = GetWordSym(symAddress, out symFound);
                    str += $"({sym})";
                    break;

                case AddressingModes.R:
                    d = (Data[offset + 1] & 0x80) == 0x80 ? (-1 & ~0xFF) | Data[offset + 1] : Data[offset + 1];
                    target = LoadAddress + offset + d + (BinFile ? 2 : 0);
                    sym = GetWordSym(target, out symFound);
                    str += sym;
                    break;

                case AddressingModes.Zr:
                    d = (Data[offset + 1] & 0x80) == 0x80 ? (-1 & ~0xFF) | Data[offset + 1] : Data[offset + 1];
                    target = LoadAddress + offset + d + (BinFile ? 2 : 0);
                    sym = GetWordSym(target, out symFound);
                    str += sym;
                    break;

                case AddressingModes.Ac:
                    break;

                case AddressingModes.MaxAddressingMode:
                    break;
            }

            return str;
        }

        /// <summary>
        /// Determine if a byte is an opcode
        /// </summary>
        /// <param name="offset">offst into Data of byte</param>
        /// <returns>true if byte is an opcode</returns>
        public bool IsOpCode(int offset)
        {
            if (offset < 0 || offset >= Data.Length)
            {
                return false;
            }

            var op = Ops.Ops[Data[offset]];
            return !string.IsNullOrEmpty(op.Opcode);
        }

        /// <summary>
        /// Format bytes at offset to the given format type if possible
        /// </summary>
        /// <param name="offset">offset in data</param>
        /// <param name="wantType">desired format type</param>
        /// <returns>AssemblerLine of the data</returns>
        public AssemblerLine BuildOpCode(int offset, AssemblerLine.FormatType wantType)
        {
            var formatByteLen = ((int)wantType & ~0xFF) >> 8;
            wantType &= (AssemblerLine.FormatType) 0xFF;
            var address = LoadAddress + offset - (BinFile ? 0 : 2);
            int sz;
            string opCode;
            string bytes;
            if (wantType == AssemblerLine.FormatType.Opcode && IsOpCode(offset))
            {
                var op = Ops.Ops[Data[offset]];
                sz = op.Mode.AddressingModeSize();
                if (sz + offset <= Data.Length)
                {
                    bytes = "";
                    for (var len = 0; len < sz; ++len)
                    {
                        bytes += $"${Data[offset + len].ToHex()} ";
                    }
                    bytes = bytes.Trim();
                    opCode = FormatOpCode(Ops.Ops[Data[offset]], offset, out bool foundSymFound);
                    var line = new AssemblerLine(address, bytes, opCode, AssemblerLine.FormatType.Opcode, sz);
                    line.PropertyChanged += LineOnPropertyChanged;
                    line.UnresolvedLabel = !foundSymFound;
                    return line;
                }

                sz = 1;
            }
            else if (wantType == AssemblerLine.FormatType.MultiByte)
            {
                formatByteLen = Math.Min(formatByteLen, Data.Length - offset);
                var bytesOp = ".BYTE ";
                var bytesBytes = "";

                sz = formatByteLen;

                for (var len = 0; len < sz; ++len)
                {
                    bytesBytes += $"${Data[offset + len].ToHex()} ";
                    bytesOp += $"${Data[offset + len].ToHex()},";
                }

                bytesOp = bytesOp.Substring(0, bytesOp.Length - 1);

                var bytesDataLine = new AssemblerLine(address, bytesBytes, bytesOp,
                    (AssemblerLine.FormatType)((int)wantType | formatByteLen << 8), sz);
                bytesDataLine.PropertyChanged += LineOnPropertyChanged;
                return bytesDataLine;
            }
            else if (wantType == AssemblerLine.FormatType.Text)
            {
                formatByteLen = Math.Min(formatByteLen, Data.Length - offset);
                var bytesOp = ".TEXT ";
                var bytesBytes = "";

                sz = formatByteLen;

                // last
                // 0 = not initialized
                // 1 = text
                // 2 = byte
                var last = 0;
                for (var len = 0; len < sz; ++len)
                {
                    var b = (char) Data[offset + len];
                    if (char.IsLetterOrDigit(b) || char.IsPunctuation(b) || b == 0x20)
                    {
                        switch (last)
                        {
                            case 0:
                                bytesOp += $"\"{b}";
                                break;
                            case 1:
                                if (b == '"')
                                {
                                    bytesOp += "\\\"";
                                }
                                bytesOp += b;
                                break;
                            default:
                                bytesOp += $",\"{b}";
                                break;
                        }

                        last = 1;
                    }
                    else
                    {
                        var bStr = $"${Data[offset + len].ToHex()}";
                        switch (last)
                        {
                            case 0:
                                bytesOp += bStr;
                                break;
                            case 1:
                                bytesOp += $"\",{bStr}";
                                break;
                            default:
                                bytesOp += $",{bStr}";
                                break;
                        }

                        last = 2;
                    }
                    bytesBytes += $"${Data[offset + len].ToHex()} ";
                }

                if (last == 1)
                {
                    bytesOp += "\"";
                }

                var bytesDataLine = new AssemblerLine(address, bytesBytes, bytesOp,
                    (AssemblerLine.FormatType)((int)wantType | formatByteLen << 8), sz);
                bytesDataLine.PropertyChanged += LineOnPropertyChanged;
                return bytesDataLine;
            }
            else
            {
                sz = wantType == AssemblerLine.FormatType.Word ? 2 : 1;
            }

            while (sz >= 0 && offset + sz > Data.Length)
            {
                --sz;
            }

            if (sz == 0)
            {
                return null;
            }

            bytes = "";
            for (var len = 0; len < sz; ++len)
            {
                bytes += $"${Data[offset + len].ToHex()} ";
            }

            bytes = bytes.Trim();

            int addr;
            string sym;
            string directive;
            if (sz == 1)
            {
                addr = Data[offset];
                directive = ".BYTE";
                sym = $"${addr.ToHex()}";
            }
            else
            {
                addr = Data[offset] + Data[offset + 1] * 256;
                directive = ".WORD";
                sym = $"${addr.ToHexWord()}";
            }

            if (LoadAddress != 0 && LocalSymbols.TryGetValue(addr, out var tempSym))
            {
                sym = tempSym;
                if (!UsedLocalSymbols.ContainsKey(addr))
                {
                    UsedLocalSymbols.Add(addr, sym);
                }
            }

            opCode = $"{directive} {sym}";

            var dataLine = new AssemblerLine(address, bytes, opCode,
                sz == 1 ? AssemblerLine.FormatType.Byte : AssemblerLine.FormatType.Word, sz);
            dataLine.PropertyChanged += LineOnPropertyChanged;
            return dataLine;
        }

        /// <summary>
        /// Build all the Assembler lines
        /// </summary>
        public void BuildAssemblerLines()
        {
            Building = true;

            LocalSymbols.Clear();
            UsedSymbols.Clear();
            UsedLocalSymbols.Clear();

            lock (AssemblerLineCollectionLock)
            {
                AssemblerLineCollection.Clear();

                var offset = 0;
                if (!BinFile)
                {
                    LoadAddress = Data[0] + Data[1] * 256;
                    offset += 2;
                }

                var index = 0;
                while (offset < Data.Length)
                {
                    var wantType = AssemblerLine.FormatType.Opcode;
                    var line = BuildOpCode(offset, wantType);
                    if (line == null)
                    {
                        _ = MessageBox.Show((Window)Owner, "Failed to disassemble", $"DisAsm6502 {MainWindow.GetAssemblyFileVersion()}" );
                        return;
                    }

                    offset += line.Size;
                    line.RowIndex = index++;
                    AssemblerLineCollection.Add(line);
                }

                Building = false;
            }

            SyncRowsLabels();
            ValidateCollection();
        }

        /// <summary>
        /// Reset the Assembler indexes
        /// </summary>
        private void ResetIndexes(int startIndex, int addr, int endIndex)
        {
            lock (AssemblerLineCollectionLock)
            {
                for (var r = Math.Max(0, startIndex); r < Math.Min(AssemblerLineCollection.Count, endIndex + 1); ++r)
                {
                    AssemblerLineCollection[r].RowIndex = r;
                    AssemblerLineCollection[r].Address = addr;
                    AssemblerLineCollection[r].Label = "";
                    addr += AssemblerLineCollection[r].Size;
                }
            }
        }

        /// <summary>
        /// sync symbols and indexes for labels
        /// must be called if there is any change to an assembler line
        /// </summary>
        public void SyncRowsLabels()
        {
            if (Building)
            {
                return;
            }

            lock (AssemblerLineCollectionLock)
            {
                var index = 0;
                LocalSymbols.Clear();
                UsedLocalSymbols.Clear();
                ImmediateValues.Clear();

                ResetIndexes(0, LoadAddress, AssemblerLineCollection.Count);
                BuildLocalSymbols();

                // Copy the current lines
                var temp = new AssemblerLine[AssemblerLineCollection.Count];
                AssemblerLineCollection.CopyTo(temp, 0);
                AssemblerLineCollection.Clear();

                var brkCount = 0;
                // Rebuild lines with possible new lables
                foreach (var oldLine in temp)
                {
                    var offset = oldLine.Address - LoadAddress + (BinFile ? 0 : 2);
                    var line = BuildOpCode(offset, (AssemblerLine.FormatType) oldLine.Format);
                    if (line != null)
                    {
                        line.RowIndex = index++;
                        AssemblerLineCollection.Add(line);

                        if (line.Format == (int) AssemblerLine.FormatType.Opcode)
                        {
                            var op = Ops.Ops[Data[offset]];
                            if (op.Mode == AddressingModes.Im)
                            {
                                ImmediateValues.Add(new Tuple<int, byte>(line.RowIndex, Data[offset + 1]));
                            }

                            if (Data[offset] == 0)
                            {
                                ++brkCount;
                                if (brkCount <= 1)
                                {
                                    continue;
                                }

                                AssemblerLineCollection[AssemblerLineCollection.Count - 2].UnresolvedLabel = true;
                                AssemblerLineCollection[AssemblerLineCollection.Count - 1].UnresolvedLabel = true;
                            }
                            else
                            {
                                brkCount = 0;
                            }
                        }
                        else
                        {
                            brkCount = 0;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (LoadAddress > 0)
                {
                    var i = 1;
                    while (i < ImmediateValues.Count)
                    {
                        const char lo = '<';
                        const char hi = '>';

                        if (ImmediateValues[i].Item1 - ImmediateValues[i - 1].Item1 < 10)
                        {
                            for (var order = 0; order < 2; ++order)
                            {
                                var address = ImmediateValues[i - 1].Item2 * (order == 0 ? 1 : 256) + ImmediateValues[i].Item2 * (order == 0 ? 256 : 1);

                                if (!IsSymLocal(address))
                                {
                                    continue;
                                }

                                if (LocalSymbols.TryGetValue(address, out var sym))
                                {
                                    if (!UsedLocalSymbols.ContainsKey(address))
                                    {
                                        UsedLocalSymbols.Add(address, sym);
                                    }

                                    var byteLo = order == 0 ? lo : hi;
                                    var byteHi = order == 0 ? hi : lo;
                                    AssemblerLineCollection[ImmediateValues[i - 1].Item1].OpCodes =
                                        AssemblerLineCollection[ImmediateValues[i - 1].Item1].OpCodes
                                            .Replace($"${ImmediateValues[i - 1].Item2.ToHex()}", $"{byteLo}{sym}");

                                    AssemblerLineCollection[ImmediateValues[i].Item1].OpCodes =
                                        AssemblerLineCollection[ImmediateValues[i].Item1].OpCodes
                                            .Replace($"${ImmediateValues[i].Item2.ToHex()}", $"{byteHi}{sym}");
                                }

                                ++i;
                                break;
                            }
                        }

                        ++i;
                    }
                }

                // Now add the left column label for the used labels
                foreach (var assemblerLine in AssemblerLineCollection)
                {
                    if (!UsedLocalSymbols.ContainsKey(assemblerLine.Address))
                    {
                        continue;
                    }

                    if (LocalSymbols.TryGetValue(assemblerLine.Address, out var sym))
                    {
                        assemblerLine.Label = sym;
                    }
                }
            }

            // build the external labels
            SymCollection.Clear();
            foreach (var usedSymbolsKey in UsedSymbols.Keys.OrderBy(key => key))
            {
                if (UsedSymbols.TryGetValue(usedSymbolsKey, out var val))
                {
                    SymCollection.Add((usedSymbolsKey & 0xFF00) != 0
                        ? $"{string.Empty,-10}{val} = ${usedSymbolsKey.ToHexWord()}"
                        : $"{string.Empty,-10}{val} = ${usedSymbolsKey.ToHex()}");
                }
            }

            // add blank line and org
            SymCollection.Add("");
            SymCollection.Add($"{string.Empty,-10}.ORG ${LoadAddress.ToHexWord()}");
        }

        /// <summary>
        /// Reformat a line
        /// </summary>
        /// <param name="line">line to format</param>
        /// <param name="format">new format</param>
        public void FormatLine(AssemblerLine line, AssemblerLine.FormatType format)
        {
            var startIndex = line.RowIndex;
            var startAddress = line.Address;
            var oldSize = line.Size;
            var newOffset = line.Address - LoadAddress + (BinFile ? 0 : 2);
            var newLine = BuildOpCode(newOffset, format);
            if (newLine == null)
            {
                return;
            }

            newLine.RowIndex = line.RowIndex;
            AssemblerLineCollection.RemoveAt(line.RowIndex);
            AssemblerLineCollection.Insert(newLine.RowIndex, newLine);

            var index = newLine.RowIndex;
            var bytesToInsert = oldSize - newLine.Size;
            if (oldSize > newLine.Size)
            {
                newOffset += newLine.Size;
            }
            else if (newLine.Size > oldSize)
            {
                do
                {
                    var delIndex = newLine.RowIndex + 1;
                    var n = AssemblerLineCollection[delIndex].Size;
                    AssemblerLineCollection.RemoveAt(delIndex);
                    bytesToInsert += n;
                } while (bytesToInsert < 0);

                newOffset += newLine.Size;
            }

            var tempIndex = index;
            while (bytesToInsert > 0)
            {
                var insertLine = BuildOpCode(newOffset, AssemblerLine.FormatType.Byte);
                insertLine.RowIndex = ++tempIndex;
                AssemblerLineCollection.Insert(insertLine.RowIndex, insertLine);
                bytesToInsert -= insertLine.Size;
                newOffset += insertLine.Size;
            }

            var endIndex = AssemblerLineCollection.Count - 1;
            ResetIndexes(startIndex, startAddress, endIndex);
        }

        public static object AssemblerLineCollectionLock = new object();

        private ObservableCollection<AssemblerLine> _assemblerLineCollection;

        /// <summary>
        /// Collection of assembled lines (backing for GUI)
        /// </summary>
        public ObservableCollection<AssemblerLine> AssemblerLineCollection
        {
            get => _assemblerLineCollection;
            set
            {
                _assemblerLineCollection = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Constructor Create collection of lines
        /// </summary>
        public ViewModel()
        {
            AssemblerLineCollection = new ObservableCollection<AssemblerLine>();
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// A property has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, nameof(LoadAddress), StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Org = $".ORG ${LoadAddress.ToHexWord()}";
                SyncRowsLabels();
            }
            else if (string.Compare(e.PropertyName, nameof(Data), StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                lock (AssemblerLineCollectionLock)
                {
                    AssemblerLineCollection.Clear();
                }

                LoadAddress = 0;
                BuildAssemblerLines();
            }
            else if (string.Compare(e.PropertyName, nameof(BinFile), StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                if (BinFile)
                {
                    LoadAddress = 0;
                }
            }
        }

        /// <summary>
        /// A line has changed
        /// </summary>
        /// <param name="sender">line being changed</param>
        /// <param name="e">parameters chamged</param>
        private void LineOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var line = (AssemblerLine) sender;
            if (string.Compare(e.PropertyName, "Format", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                FormatLine(line, (AssemblerLine.FormatType) line.Format);
            }
        }

        /// <summary>
        /// Dictionary to hold well known address and symbols
        /// </summary>
        private Dictionary<int, string> _builtInSymbols = XmlDeserializeFromResource<SymCollection>("Symbols.xml").ToDictionary();

        public Dictionary<int, string> BuiltInSymbols
        {
            get => _builtInSymbols;
            set
            {
                _builtInSymbols = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Array holding Opcodes, addressing mode and string name
        /// </summary>
        private OpCollection _ops = XmlDeserializeFromResource<OpCollection>("Ops.xml");

        public OpCollection Ops
        {
            get => _ops;
            set
            {
                _ops = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Sanity check Assembler lines
        /// </summary>
        [Conditional("DEBUG")]
        public void ValidateCollection()
        {
            var lastLine = -1;
            var address = LoadAddress;

            foreach (var assemblerLine in AssemblerLineCollection)
            {
                if (assemblerLine.RowIndex != lastLine + 1)
                {
                    _ = MessageBox.Show($"Index out of sync ROW {assemblerLine.RowIndex}  should be {lastLine + 1}.\n" +
                                        $"{assemblerLine.Label} {assemblerLine.OpCodes} {assemblerLine.Comment}");
                    return;
                }

                if (address != assemblerLine.Address)
                {
                    _ = MessageBox.Show($"Address out of sync ROW {assemblerLine.RowIndex}.\n" +
                                        $"{assemblerLine.Label} {assemblerLine.OpCodes} {assemblerLine.Comment}");
                    return;
                }

                address += assemblerLine.Size;
                lastLine = assemblerLine.RowIndex;
            }
        }
    }
}
