using System.Collections.Generic;
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
        private static readonly Dictionary<ICommand, bool> CommandDict = new Dictionary<ICommand, bool>();

        /// <summary>
        /// Extension method for ICommand to determine if it is being executed
        /// </summary>
        /// <param name="command">the ICommand instance</param>
        /// <returns>true if the command is being executed</returns>
        internal static bool IsRunning(this ICommand command)
        {
            _ = CommandDict.TryGetValue(command, out var running);
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
            return symbols.Syms.ToDictionary(sym => sym.Address, sym => sym.Name);
        }

        internal static T Deserialize<T>(this T toDeserialize, string resourcePath)
        {
            var a = System.Reflection.Assembly.GetExecutingAssembly();
            var x = new XmlSerializer(typeof(T));
            var stream = a.GetManifestResourceStream(resourcePath);

            // ReSharper disable once AssignNullToNotNullAttribute
            return (T)x.Deserialize(new StreamReader(stream));
        }

        internal static string SerializeObject<T>(this T toSerialize)
        {
            var xmlSerializer = new XmlSerializer(toSerialize.GetType());
            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
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
            if (CommandDict.ContainsKey(command))
            {
                _ = CommandDict.TryGetValue(command, out var running);

                if (value != running)
                {
                    _ = CommandDict.Remove(command);
                    CommandDict.Add(command, value);
                }
            }
            else
            {
                CommandDict.Add(command, value);
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
