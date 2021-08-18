using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using DisAsm6502.Model;

namespace DisAsm6502
{
    public static class Extensions
    {
        /// <summary>
        /// dictionary to hold the state of running Commands
        /// </summary>
        private static readonly Dictionary<ICommand, bool> CommandDictionary = new Dictionary<ICommand, bool>();

        /// <summary>
        /// Extension method for ICommand to determine if it is being executed
        /// </summary>
        /// <param name="command">the ICommand instance</param>
        /// <returns>true if the command is being executed</returns>
        internal static bool IsRunning(this ICommand command)
        {
            _ = CommandDictionary.TryGetValue(command, out var running);
            return running;
        }

        internal static string ToHex(this byte b)
        {
            return $"{b:X2}";
        }

        internal static string ToHex(this int b)
        {
            return ((byte) (b & 0xFF)).ToHex();
        }

        internal static string ToHexWord(this int w)
        {
            return ((w & 0xFF00) >> 8).ToHex() + (w & 0xFF).ToHex();
        }

        internal static Dictionary<int, string> ToDictionary(this SymCollection symbols)
        {
            return symbols.Symbols.ToDictionary(sym => sym.Address, sym => sym.Name);
        }

        public static T XmlDeserializeFromString<T>(this string objectData)
        {
            return (T)XmlDeserializeFromString(objectData, typeof(T));
        }

        public static object XmlDeserializeFromString(this string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }

        internal static T XmlDeserializeFromResource<T>(string resourcePath)
        {
            var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = executingAssembly.GetManifestResourceStream($"{executingAssembly.GetName().Name}.{resourcePath}");

            var xmlSerializer = new XmlSerializer(typeof(T));
            // ReSharper disable once AssignNullToNotNullAttribute
            return (T)xmlSerializer.Deserialize(new StreamReader(stream));
        }

        // ReSharper disable once UnusedMember.Global
        internal static string BinSerialize<T>(this T toSerialize)
        {
            var binSerializer = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binSerializer.Serialize(memoryStream, toSerialize);
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        public static object BinDeserializeFromString(this string objectData)
        {
            var serializer = new BinaryFormatter();
            object result;

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(Encoding.UTF8.GetBytes(objectData), 0, objectData.Length);
                result = serializer.Deserialize(memoryStream);
            }

            return result;
        }

        // ReSharper disable once UnusedMember.Global
        public static T BinDeserializeFromString<T>(this string objectData)
        {
            return (T)BinDeserializeFromString(objectData);
        }

        // ReSharper disable once UnusedMember.Global
        internal static string XmlSerialize<T>(this T toSerialize)
        {
            var xmlSerializer = new XmlSerializer(toSerialize.GetType());
            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        internal static void FormatItems(this ObservableCollection<AssemblerLine> collection,
            ICollection<Tuple<int, int>> items)
        {
            foreach (var (address, format) in items)
            {
                var index = BSearch(collection, address, (line, i) => line.Address.CompareTo(i));
                if (index >= 0 && index < collection.Count)
                {
                    collection[index].Format = format;
                }
            }
            items.Clear();
        }


        internal delegate int Compare<in T, in T2>(T a, T2 b);

        internal static int BSearch<T, T2>(this Collection<T> collection, T2 address, Compare<T, T2> cmp)
        {
            var low = 0;
            var max = collection.Count - 1;
            var high = max;

            while (low <= high)
            {
                var guess = low + (high - low) / 2;
                var c = cmp(collection[guess], address);
                if (c < 0)
                {
                    if (low == guess)
                    {
                        return cmp(collection[high], address) == 0 ? high : -1;
                    }

                    low = guess;
                }
                else if (c > 0)
                {
                    if (high == guess)
                    {
                        return cmp(collection[low], address) == low ? low : -1;
                    }

                    high = guess;
                }
                else
                {
                    return guess;
                }
            }

            return -1;
        }

        /// <summary>
        /// Extension method to set the state of an ICommand
        /// 
        /// This will also cause invalidation of the command
        /// causing the CanExecute method to be executed
        /// </summary>
        /// <param name="command">the RoutedCommand instance</param>
        /// <param name="value">true or false</param>
        /// <param name="param"></param>
        /// <param name="name"></param>
        internal static void SetIsRunning(this ICommand command, bool value, object param = null,
            [CallerMemberName] string name = null)
        {
            if (CommandDictionary.ContainsKey(command))
            {
                _ = CommandDictionary.TryGetValue(command, out var running);

                if (value != running)
                {
                    _ = CommandDictionary.Remove(command);
                    CommandDictionary.Add(command, value);
                }
            }
            else
            {
                CommandDictionary.Add(command, value);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Number of bytes used in each addressing mode
        /// </summary>;
        private static readonly int[] ModeSizes =
        {
            1, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 2, 2, 1
        };

        internal static int AddressingModeSize(this AddressingModes mode)
        {
            return ModeSizes[(int) mode];
        }
    }
}
