using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using static System.Console;

namespace AcidChicken.CommonsUpdater
{
    partial class Program
    {
        enum WriteType
        {
            Information,
            Warning,
            Error,
            Memo,
            Success,
            Failure,
            Input,
            Select
        }

        static void WriteMessage(string message = default, WriteType type = WriteType.Information, bool inline = false)
        {
            switch (type)
            {
                case WriteType.Information:
                    ForegroundColor = ConsoleColor.Cyan;
                    Write("[情報] ");
                    break;

                case WriteType.Warning:
                    ForegroundColor = ConsoleColor.Yellow;
                    Write("[注意] ");
                    break;

                case WriteType.Error:
                    ForegroundColor = ConsoleColor.Red;
                    Write("[警告] ");
                    break;

                case WriteType.Memo:
                    ForegroundColor = ConsoleColor.DarkBlue;
                    Write("[メモ] ");
                    break;

                case WriteType.Success:
                    ForegroundColor = ConsoleColor.Green;
                    Write("[成功] ");
                    break;

                case WriteType.Failure:
                    ForegroundColor = ConsoleColor.DarkRed;
                    Write("[失敗] ");
                    break;

                case WriteType.Input:
                    ForegroundColor = ConsoleColor.DarkGray;
                    Write("[入力] ");
                    break;

                case WriteType.Select:
                    ForegroundColor = ConsoleColor.DarkMagenta;
                    Write("[選択] ");
                    break;
            }

            if (message is string)
            {
                if (inline)
                    Write(message);
                else
                    WriteLine(message);
            }

            ResetColor();
        }

        static void WriteStack(Exception e)
        {
            while (e is Exception)
            {
                WriteMessage($"{e}: {e.Message}", WriteType.Memo);

                foreach (var kvp in e.Data)
                    if (kvp is DictionaryEntry entry)
                        WriteMessage($"{entry.Key}: {entry.Value}", WriteType.Memo);

                e = e.InnerException;
            }
        }

        static void WriteLines(TextWriter writer, params string[] source)
        {
            foreach (var line in source)
                writer.WriteLine(line);
        }

        static IProgress<string> CreateConsoleProgressBar(int target, string log = default, object parentLocker = default)
        {
            const char full = (char)0x2588;
            const char empty = ' ';

            WriteLines(Out, null, null, null);

            var locker = parentLocker ?? new object();
            var source = -1;
            var origin = CursorTop - 3;
            var stopwatch = Stopwatch.StartNew();

            var progress = new Progress<string>(e =>
            {
                lock (locker)
                {
                    var elapsed = stopwatch.ElapsedTicks;
                    var width = BufferWidth - 2;

                    if (origin >= 0 && width > 0)
                    {
                        var ratio = (double)(++source) / target;
                        var fill = (int)(ratio * width * 8);
                        var left = fill == 0 ? "" : new string(full, (fill - 1) >> 3);
                        var part = fill & 7;
                        var center = fill == 0 ? empty : part == 0 ? full : (char)(full + 8 - part);
                        var right = new string(empty, width - left.Length - 1);

                        CursorLeft = 0;
                        CursorTop = origin;

                        WriteLines(Out,
                            $"[{left}{center}{right}]",
                            $"{ratio:P} {source:N0}/{target:N0} {(source == 0 ? "" : right.Length == 0 ? "まもなく完了" : new TimeSpan(((long)(elapsed / ratio) - elapsed) / 100).ToString("g"))}",
                            e);
                    }
                    else
                        WriteLines(Out,
                            e);

                    ResetColor();
                }
            }) as IProgress<string>;
            progress.Report(log);
            return progress;
        }
    }
}
