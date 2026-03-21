using System;
using System.IO;
using System.Reflection;

namespace SnakeGame.App;

/// <summary>
/// 启动日志记录器 - 帮助排查启动问题
/// </summary>
public static class StartupLogger
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SnakeGame",
        "startup.log");

    static StartupLogger()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            File.AppendAllText(LogFilePath, $"\n=== 启动时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        }
        catch
        {
            // 忽略日志写入失败
        }
    }

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFilePath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch
        {
            // 忽略日志写入失败
        }
    }

    public static void LogException(Exception ex, string context)
    {
        try
        {
            File.AppendAllText(LogFilePath, 
                $"[{DateTime.Now:HH:mm:ss.fff}] ❌ {context}\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch
        {
            // 忽略日志写入失败
        }
    }

    public static string GetLogFilePath() => LogFilePath;
}
