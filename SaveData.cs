using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DisAsm6502.Model;

namespace DisAsm6502
{
    [Serializable]
    public class SaveData : Notifier
    {
        public SaveData()
        {
        }

        public SaveData(Window owner)
        {
            _owner = owner;
        }

        private Window _owner;

        public int LoadAddress;
        public bool BinFile;
        public Collection<byte> Data = new Collection<byte>();
        public Collection<AssemblerLine> AssemblerLines = new Collection<AssemblerLine>();

        public void Open(string data)
        {
            var saveData = data.XmlDeserializeFromString<SaveData>();

            var view = ((MainWindow) _owner)?.View;
            if (view == null)
            {
                return;
            }

            view.BinFile = saveData.BinFile;
            view.Data = saveData.Data.ToArray();
            view.LoadAddress = saveData.LoadAddress;
            view.AssemblerLineCollection.Clear();
            foreach (var assemblerLine in saveData.AssemblerLines)
            {
                view.AssemblerLineCollection.Add(assemblerLine);
            }

            view.SyncRowsLabels();
        }

        public string Save()
        {
            var view = ((MainWindow) _owner)?.View;
            if (view == null)
            {
                return string.Empty;
            }

            Data.Clear();
            foreach (var b in view.Data)
            {
                Data.Add(b);
            }

            LoadAddress = view.LoadAddress;
            BinFile = view.BinFile;
            AssemblerLines.Clear();
            foreach (var assemblerLine in view.AssemblerLineCollection)
            {
                AssemblerLines.Add(assemblerLine);
            }

            return this.XmlSerialize();
        }
    }
}
