﻿using System.Runtime.CompilerServices;

using Windows.Win32;
using Windows.Win32.System.Console;

namespace FMAC;

internal static class Log
{

    static unsafe Log()
    {
        CONSOLE_MODE mode;
        var handle = Native.GetStdHandle(STD_HANDLE.STD_OUTPUT_HANDLE);
        if (!Native.GetConsoleMode(handle, &mode)) return;
        Native.SetConsoleMode(handle, mode | CONSOLE_MODE.ENABLE_VIRTUAL_TERMINAL_PROCESSING);
    }

    public static void Error(object value, Exception? exception = null, [CallerMemberName] string callerName = "")
    {
        WriteLog(value, exception, callerName);
    }

    public static void Warn(object value, Exception? exception = null, [CallerMemberName] string callerName = "")
    {
        WriteLog(value, exception, callerName);
    }

    public static void Info(object value, Exception? exception = null, [CallerMemberName] string callerName = "")
    {
        WriteLog(value, exception, callerName);
    }

    public static void Debug(object value, Exception? exception = null, [CallerMemberName] string callerName = "")
    {
        WriteLog(value, exception, callerName);
    }

    public static void Trace(object value, Exception? exception = null, [CallerMemberName] string callerName = "")
    {
        WriteLog(value, exception, callerName);
    }

    private static void WriteLog(object message, Exception? exception, string tag, [CallerMemberName] string level = "")
    {
        var color = level switch
        {
            "Error" => "1;31",
            "Warn" => "93",
            "Info" => "94",
            "Debug" => "38;5;43",
            "Trace" => "95",
            _ => throw new ArgumentException($"Invalid log level: {level}")
        };
        var levelStr = $"\u001b[{color}m{level,5}\u001b[0m";
        var time = DateTimeOffset.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{time}][{levelStr}] {tag} : {message}");
        if (exception != null)
        {
            Console.WriteLine(exception.ToString());
        }
    }

}

