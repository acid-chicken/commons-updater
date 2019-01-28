using System;
using System.Linq;
using System.Security;

using static System.Console;
using static System.Runtime.InteropServices.Marshal;

namespace AcidChicken.CommonsUpdater
{
    partial class Program
    {
        static int ReadChoice(params string[] choices)
        {
            var cursor = 0;

            foreach (var _ in choices)
                WriteLine();

            void Render()
            {
                CursorTop -= choices.Length;

                foreach (var (choice, index) in choices.Select((x, i) => (x, i)))
                {
                    if (index == cursor)
                        ForegroundColor = ConsoleColor.Magenta;

                    Write(index == cursor ? "> " : "  ");
                    WriteLine(choice);

                    ResetColor();
                }
            }

            Render();

            while (true)
            {
                var key = ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        return cursor;

                    case ConsoleKey.UpArrow when cursor-- == 0:
                        cursor++;
                        break;

                    case ConsoleKey.DownArrow when ++cursor == choices.Length:
                        cursor--;
                        break;

                    case ConsoleKey.PageUp:
                        cursor = 0;
                        break;

                    case ConsoleKey.PageDown:
                        cursor = choices.Length - 1;
                        break;
                }

                Render();
            }
        }

        static string ReadPassword()
        {
            var origin = CursorLeft;

            using (var password = new SecureString())
            {
                var cursor = 0;
                var still = true;

                while (still)
                {
                    var key = ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.Backspace:
                            if (cursor > 0 && password.Length > 0)
                                password.RemoveAt(--cursor);
                            break;

                        case ConsoleKey.Delete:
                            if (cursor < password.Length && password.Length > 0)
                                password.RemoveAt(cursor);
                            break;

                        case ConsoleKey.Enter:
                            still = false;
                            break;

                        case ConsoleKey.LeftArrow:
                            if (cursor > 0)
                                cursor--;
                            break;

                        case ConsoleKey.RightArrow:
                            if (cursor < password.Length)
                                cursor++;
                            break;

                        default:
                            password.InsertAt(cursor++, key.KeyChar);
                            break;
                    }

                    CursorLeft = origin;
                    Write($"{new string('*', password.Length)} ");
                    CursorLeft--;
                }

                WriteLine();
                return PtrToStringUni(SecureStringToGlobalAllocUnicode(password));
            }
        }
    }
}
