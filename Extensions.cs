using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DisAsm6502
{
    public static class Extensions
    {
        public static void ClearRoutedCommands()
        {
            CommandDict.Clear();
        }

        public static void InvalidateRoutedCommands()
        {
            CommandManager.InvalidateRequerySuggested();
        }

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
    }
}
