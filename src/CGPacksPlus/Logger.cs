namespace CGPacksPlus;

using System;
using BepInEx.Logging;
using BepInExLogLevel = BepInEx.Logging.LogLevel;

enum LogLevel
{
    Info = 0,
    Warning,
    Error,
    Debug,
    DebugError,
}

static class LogLevelExtensions
{
    public static bool IsValid(this LogLevel ll) =>
        !((int)ll < 0 || (int)ll >= typeof(LogLevel).GetEnumValues().Length);

    public static string ShortName(this LogLevel ll) => ll switch
    {
        LogLevel.Info => "I",
        LogLevel.Warning => "W",
        LogLevel.Error => "E",
        LogLevel.Debug => "D",
        LogLevel.DebugError => "DE",
        _ => "???"
    };
}

/// <summary>
/// A wrapper class for writing logs to both the ingame console (plog)
/// and the BepInEx console
/// </summary>
public class Logger
{
    private ManualLogSource ml => Plugin.Instance.Logger;
    private plog.Logger pl;

    public readonly string Name;

    public Logger(string name = null)
    {
        Name = name;
        pl = (Name == null)? new() : new(name);
    }

    private string FormatLog(LogLevel ll, string message, string color = null)
    {
        string coloredMessage = (color == null)? message : $"<color={color}>{message}</color>";
        string nameTag = (Name == null)? "" : $"[{Name}] ";
        return nameTag + $"[{ll.ShortName()}] " + coloredMessage;
    }

    public void Info(string message)
    {
        ml.LogInfo(FormatLog(LogLevel.Info, message));
        pl.Info(FormatLog(LogLevel.Info, message));
    }

    public void Warning(string message)
    {
        ml.LogWarning(FormatLog(LogLevel.Info, message));
        pl.Warning(FormatLog(LogLevel.Info, message, "yellow"));
    }

    public void Error(string message)
    {
        ml.LogError(FormatLog(LogLevel.Info, message));
        pl.Error(FormatLog(LogLevel.Info, message, "red"));
    }

    public void Debug(string message)
    {
        #if DEBUG
        ml.LogInfo(FormatLog(LogLevel.Debug, message));
        pl.Debug(FormatLog(LogLevel.Debug, message, "#0af"));
        #else
        return;
        #endif
    }

    public void DebugError(string message)
    {
        #if DEBUG
        ml.LogError(FormatLog(LogLevel.DebugError, message));
        pl.Error(FormatLog(LogLevel.DebugError, message, "#a0f"));
        #else
        return;
        #endif
    }
}
