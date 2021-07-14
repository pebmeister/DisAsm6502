using System.Collections.Generic;

// ReSharper disable UnusedMember.Global

namespace DisAsm6502.Model
{
    public class AssemblerLine : Notifier
    {
        // ReSharper disable once UnusedMember.Global
        public List<string> FormatOptions { get; } = new List<string> { "OpCode", "Byte", "Word" };

        public enum FormatType
        {
            Opcode,
            Byte,
            Word
        }

        private bool _constructing;

        public bool Constructing
        {
            get => _constructing;
            set
            {
                _constructing = value;
                OnPropertyChanged();
            }
        }

        private int _rowIndex;

        public int RowIndex
        {
            get => _rowIndex;
            set
            {
                _rowIndex = value;
                OnPropertyChanged();
            }
        }
        private int _format;

        public int Format
        {
            get => _format;
            set
            {
                _format = value;
                OnPropertyChanged();
            }
        }

        private int _size;

        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged();
            }
        }

        private string _label;

        public string Label
        {
            get => _label;
            set
            {
                _label = value.Trim();
                OnPropertyChanged();
            }
        }

        private int _address;

        public int Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
            }
        }

        private string _bytes;

        public string Bytes
        {
            get => _bytes;
            set
            {
                _bytes = value.Trim();
                OnPropertyChanged();

                Comment = $"; ${Address.ToHexWord()}: {_bytes}";
            }
        }

        private string _opCodes;

        public string OpCodes
        {
            get => _opCodes;
            set
            {
                _opCodes = value.Trim();
                OnPropertyChanged();
            }
        }

        private string _comment;

        public string Comment
        {
            get => _comment;
            set
            {
                _comment = value.Trim();
                OnPropertyChanged();
            }
        }

        public AssemblerLine(int address, string bytes, string opcodes, FormatType format, int sz)
        {
            Constructing = true;
            Address = address;
            Bytes = bytes;
            OpCodes = opcodes;
            Format = (int)format;
            Constructing = false;
            Size = sz;
        }

        public override string ToString()
        {
            return $"{Label,-10} {OpCodes,-20} {Comment}";
        }
    }
}