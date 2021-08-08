using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
                var index = collection.FindAddress(address);
                if (index >= 0 && index < collection.Count)
                {
                    collection[index].Format = format;
                }
            }
            items.Clear();
        }

        internal static int FindAddress(this ObservableCollection<AssemblerLine> collection, int address)
        {
            var low = 0;
            var max = collection.Count - 1;
            var high = max;

            while (low <= high)
            {
                var guess = low + ((high - low) / 2);
                if (collection[guess].Address < address)
                {
                    if (low == guess)
                    {
                        return collection[high].Address == address ? high : -1;
                    }
                    low = guess;
                }
                else if (collection[guess].Address > address)
                {
                    if (high == guess)
                    {
                        return collection[low].Address == address ? low : -1;
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
