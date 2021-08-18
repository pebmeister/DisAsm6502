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

        public SaveData(object owner)
        {
            _owner = owner;
        }

        [NonSerialized]
        private readonly object _owner;

        public int LoadAddress;
        public bool BinFile;
        public Collection<byte> Data = new Collection<byte>();
        public Collection<AssemblerLine> AssemblerLines = new Collection<AssemblerLine>();

        public double Top;
        public double Left;
        public double Width;
        public double Height;
        public WindowState WinState;

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

            if (!(saveData.Width > 0) || !(saveData.Height > 0))
            {
                return;
            }

            ((MainWindow) _owner).Top = saveData.Top;
            ((MainWindow) _owner).Left = saveData.Left;
            ((MainWindow) _owner).Width = saveData.Width;
            ((MainWindow) _owner).Height = saveData.Height;
            ((MainWindow) _owner).WindowState = saveData.WinState;
        }

        /// <summary>
        /// Save the current data
        /// </summary>
        /// <returns>string containing xml serialization of save data</returns>
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

            Left = ((MainWindow)_owner).Left;
            Top = ((MainWindow)_owner).Top;
            Width = ((MainWindow)_owner).Width;
            Height = ((MainWindow)_owner).Height;
            WinState = ((MainWindow) _owner).WindowState;

            return this.XmlSerialize();
        }
    }
}
