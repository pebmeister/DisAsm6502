using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using DisAsm6502.Model;
using Microsoft.SqlServer.Server;
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
        private Window _owner;

        public Window Owner
        {
            get => _owner;
            set
            {
                _owner = value;
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
        /// Do not recalculate lables after each line
        /// Do so when construction is complete
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
        /// Backing data for top of file
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
                if (value < 0 || value > 0xFFFF) return;

                _loadAddress = value;
                OnPropertyChanged();
            }
        }

        private byte[] _data;

        /// <summary>
        /// Bytes read from the .prg files
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
                if (BinFile && index == 0)
                {
                    index++;
                    continue;
                }

                LocalSymbols.Add(assemblerLine.Address, $"L_{index++:D4}");
            }
        }

        /// <summary>
        /// Get symbol for an address
        /// </summary>
        /// <param name="symAddress">address of symbol</param>
        /// <param name="len">length of address (1 for page zero 2 for all others</param>
        /// <returns>symbol</returns>
        private string GetSymCommon(int symAddress, int len)
        {
            const int localSymRange = 5;
            const int externSymRange = 2;

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

                    return sym;
                }
            }

            return sym;
        }

        /// <summary>
        /// Get a 2 byte symbol
        /// </summary>
        /// <param name="symAddress">2 byte address</param>
        /// <returns>symbol for address</returns>
        private string GetWordSym(int symAddress)
        {
            return GetSymCommon(symAddress, 2);
        }

        /// <summary>
        /// Get a 1 byte symbol
        /// </summary>
        /// <param name="symAddress">1 byte adddress</param>
        /// <returns>symbol for address</returns>
        private string GetByteSym(int symAddress)
        {
            return GetSymCommon(symAddress, 1);
        }

        /// <summary>
        /// Format an opcode
        /// </summary>
        /// <param name="op">Opcode to format</param>
        /// <param name="offset">offset of Data to format</param>
        /// <returns>formatted opcode</returns>
        private string FormatOpCode(Op op, int offset)
        {
            var str = $"{op.Opcode.ToUpperInvariant()} ";
            var symAddress = offset + 2 < Data.Length
                ? Data[offset + 1] + Data[offset + 2] * 256
                : -1;

            var pgZeroSymAddress = offset + 1 < Data.Length
                ? Data[offset + 1]
                : -1;

            string sym;
            int target;
            int d;
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
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"{sym}";
                    break;

                case AddressingModes.Zpi:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"({sym})";
                    break;

                case AddressingModes.Zpx:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"{sym},x";
                    break;

                case AddressingModes.Zpy:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"{sym},y";
                    break;

                case AddressingModes.Zpix:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"({sym},x)";
                    break;

                case AddressingModes.Zpiy:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"({sym}),y";
                    break;

                case AddressingModes.A:
                    sym = GetWordSym(symAddress);
                    str += sym;
                    break;

                case AddressingModes.Aix:
                    sym = GetWordSym(symAddress);
                    str += $"({sym},x)";
                    break;

                case AddressingModes.Ax:
                    sym = GetWordSym(symAddress);
                    str += $"{sym},x";
                    break;

                case AddressingModes.Ay:
                    sym = GetWordSym(symAddress);
                    str += $"{sym},y";
                    break;

                case AddressingModes.Ind:
                    sym = GetWordSym(symAddress);
                    str += $"({sym})";
                    break;

                case AddressingModes.R:
                    d = (Data[offset + 1] & 0x80) == 0x80
                        ? (-1 & ~0xFF) | Data[offset + 1]
                        : Data[offset + 1];
                    target = LoadAddress + offset + d + (BinFile ? 2 : 0);
                    if (BinFile)
                    {
                        sym = $"${target.ToHexWord()}";
                        if (LocalSymbols.ContainsKey(target))
                        {
                            if (LocalSymbols.TryGetValue(target, out sym))
                            {
                                if (!UsedLocalSymbols.ContainsKey(target))
                                {
                                    UsedLocalSymbols.Add(target, sym);
                                }
                            }
                        }
                    }
                    else
                    {
                        sym = GetWordSym(target);
                    }

                    str += sym;
                    break;

                case AddressingModes.Zr:
                    d = (Data[offset + 1] & 0x80) == 0x80
                        ? (-1 & ~0xFF) | Data[offset + 1]
                        : Data[offset + 1];
                    target = LoadAddress + offset + d + (BinFile ? 2 : 0);
                    if (LoadAddress == 0)
                    {
                        sym = $"${target.ToHexWord()}";
                        if (LocalSymbols.ContainsKey(target))
                        {
                            if (LocalSymbols.TryGetValue(target, out sym))
                            {
                                if (!UsedLocalSymbols.ContainsKey(target))
                                {
                                    UsedLocalSymbols.Add(target, sym);
                                }
                            }
                        }
                    }
                    else
                    {
                        sym = GetWordSym(target);
                    }

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
            var address = LoadAddress + offset - (BinFile ? 0 : 2);
            int sz;
            string opCode;
            string bytes;
            if (wantType == AssemblerLine.FormatType.Opcode && IsOpCode(offset))
            {
                var op = Ops.Ops[Data[offset]];
                sz = op.Mode.AddressingModeSize();
                var index = 0;
                if (sz + offset < Data.Length)
                {
                    bytes = "";
                    for (var len = 0; len < sz; ++len)
                    {
                        bytes += $"${Data[offset + index++].ToHex()} ";
                    }

                    bytes = bytes.Trim();
                    opCode = FormatOpCode(Ops.Ops[Data[offset]], offset);
                    var line = new AssemblerLine(address, bytes, opCode, AssemblerLine.FormatType.Opcode, sz);
                    line.PropertyChanged += LineOnPropertyChanged;
                    return line;
                }

                sz = 1;
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
                        _ = MessageBox.Show("Failed to disassemble");
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
                ResetIndexes(0, LoadAddress, AssemblerLineCollection.Count);
                BuildLocalSymbols();

                // Copy the current lines
                var temp = new AssemblerLine[AssemblerLineCollection.Count];
                AssemblerLineCollection.CopyTo(temp, 0);
                AssemblerLineCollection.Clear();

                // Rebuild lines with possible new lables
                foreach (var oldLine in temp)
                {
                    var line = BuildOpCode(oldLine.Address - LoadAddress + (BinFile ? 0 : 2),
                        (AssemblerLine.FormatType) oldLine.Format);
                    if (line != null)
                    {
                        line.RowIndex = index++;
                        AssemblerLineCollection.Add(line);
                    }
                    else
                    {
                        break;
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
            if (newLine == null) return;

            newLine.RowIndex = line.RowIndex;
            AssemblerLineCollection.RemoveAt(line.RowIndex);
            AssemblerLineCollection.Insert(newLine.RowIndex, newLine);

            var bytesToInsert = 0;
            var index = newLine.RowIndex;
            if (oldSize > newLine.Size)
            {
                bytesToInsert = oldSize - newLine.Size;
                newOffset += newLine.Size;
            }
            else if (newLine.Size > oldSize)
            {
                var delIndex = newLine.RowIndex + 1;
                var n = AssemblerLineCollection[delIndex].Size;
                AssemblerLineCollection.RemoveAt(delIndex);

                var w = newLine.Size - oldSize;
                bytesToInsert = n - w;
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
            if (string.Compare(e.PropertyName, nameof(Format), StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                return;
            }

            var line = (AssemblerLine) sender;
            FormatLine(line, (AssemblerLine.FormatType) line.Format);
        }

        /// <summary>
        /// Dictionary to hold well known address and symbols
        /// </summary>
        private Dictionary<int, string> _builtInSymbols = Deserialize<SymCollection>("Symbols.xml").ToDictionary();

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
        private OpCollection _ops = Deserialize<OpCollection>("Ops.xml");

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
