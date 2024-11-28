using System;
using System.IO;
using System.Runtime.CompilerServices;

using KLPlugins.DynLeaderboards.Common;

[assembly: InternalsVisibleTo("KLPlugins.DynLeaderboards")]
[assembly: InternalsVisibleTo("KLPlugins.DynLeaderboards.Settings")]

namespace KLPlugins.DynLeaderboards.Log;

internal static class Logging {
    private static FileStream? _logFile;
    private static TextWriter? _logWriter;
    private static bool _isLogFlushed = false;
    private static string? _logFileName;
    private static string _logInitTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";
    private static bool _logInfo = false;

    public static void Init(bool logInfo) {
        Logging._logInitTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";
        Logging._logFileName = $"{PluginConstants.DataDir}\\Logs\\Log_{Logging._logInitTime}.txt";
        Logging._logInfo = logInfo;
        var dirPath = Path.GetExtension(Logging._logFileName);
        if (logInfo && dirPath != null) {
            Directory.CreateDirectory(dirPath);
            Logging.Dispose();
            Logging._logFile = File.Create(Logging._logFileName);
            Logging._logWriter = TextWriter.Synchronized(new StreamWriter(Logging._logFile));
        }
    }

    public static void Dispose() {
        if (Logging._logWriter != null) {
            Logging._logWriter.Dispose();
            Logging._logWriter = null;
        }

        if (Logging._logFile != null) {
            Logging._logFile.Dispose();
            Logging._logFile = null;
        }
    }

    public static void TryFlush() {
        if (Logging._logWriter != null && !Logging._isLogFlushed) {
            Logging._logWriter.Flush();
            Logging._isLogFlushed = true;
        }
    }

    public static void LogInfo(
        string msg,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (Logging._logInfo) {
            Logging.Log(msg, memberName, sourceFilePath, lineNumber, "INFO", SimHub.Logging.Current.Info);
        }
    }

    public static void LogWarn(
        string msg,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        Logging.Log(msg, memberName, sourceFilePath, lineNumber, "WARN", SimHub.Logging.Current.Warn);
    }

    public static void LogError(
        string msg,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        Logging.Log(msg, memberName, sourceFilePath, lineNumber, "ERROR", SimHub.Logging.Current.Error);
    }

    private static void Log(
        string msg,
        string memberName,
        string sourceFilePath,
        int lineNumber,
        string lvl,
        Action<string> simHubLog
    ) {
        var pathParts = sourceFilePath.Split('\\');
        var m = Logging.CreateMessage(msg, pathParts[pathParts.Length - 1], memberName, lineNumber);
        simHubLog($"{PluginConstants.PLUGIN_NAME} {m}");
        Logging.LogToFile($"{DateTime.Now:dd.MM.yyyy HH:mm.ss} {lvl.ToUpper()} {m}\n");
    }

    private static string CreateMessage(string msg, string source, string memberName, int lineNumber) {
        return $"({source}: {memberName},{lineNumber})\n\t{msg}";
    }

    internal static void LogFileSeparator() {
        Logging.LogToFile("\n----------------------------------------------------------\n");
    }

    private static void LogToFile(string msq) {
        if (Logging._logWriter != null) {
            Logging._logWriter.WriteLine(msq);
            Logging._isLogFlushed = false;
        }
    }
}