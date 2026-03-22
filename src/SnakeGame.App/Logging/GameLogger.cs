using System.Text;

namespace SnakeGame.App.Logging;

/// <summary>
/// 日志级别。
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// 游戏日志记录器。
/// 线程安全，支持文件滚动。
/// </summary>
public sealed class GameLogger : IDisposable
{
    private readonly string logDirectory;
    private readonly string logFileName;
    private readonly object lockObject = new();
    private StreamWriter? writer;
    private DateTime currentDate;
    private bool disposed;

    public GameLogger(string? logDirectory = null)
    {
        this.logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SnakeGame",
            "logs");

        logFileName = "game.log";
        currentDate = DateTime.Today;

        EnsureLogDirectoryExists();
    }

    /// <summary>
    /// 记录日志。
    /// </summary>
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        lock (lockObject)
        {
            CheckFileRolling();
            EnsureWriterCreated();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpperInvariant().PadLeft(8);
            var sb = new StringBuilder();
            sb.Append($"[{timestamp}] [{levelStr}] {message}");

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append($"  Exception: {exception.GetType().FullName}");
                sb.AppendLine();
                sb.Append($"  Message: {exception.Message}");
                sb.AppendLine();
                sb.Append($"  StackTrace: {exception.StackTrace}");
            }

            writer?.WriteLine(sb.ToString());
            writer?.Flush();
        }
    }

    /// <summary>
    /// 记录调试日志。
    /// </summary>
    public void Debug(string message) => Log(LogLevel.Debug, message);

    /// <summary>
    /// 记录信息日志。
    /// </summary>
    public void Info(string message) => Log(LogLevel.Info, message);

    /// <summary>
    /// 记录警告日志。
    /// </summary>
    public void Warning(string message, Exception? exception = null) => Log(LogLevel.Warning, message, exception);

    /// <summary>
    /// 记录错误日志。
    /// </summary>
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);

    /// <summary>
    /// 记录致命错误日志。
    /// </summary>
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);

    private void EnsureLogDirectoryExists()
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }

    private void EnsureWriterCreated()
    {
        if (writer == null)
        {
            var logPath = Path.Combine(logDirectory, logFileName);
            writer = new StreamWriter(logPath, append: true, Encoding.UTF8);
        }
    }

    private void CheckFileRolling()
    {
        if (currentDate != DateTime.Today)
        {
            // 日期变化，滚动日志文件
            writer?.Close();
            writer?.Dispose();
            writer = null;

            // 归档旧日志
            var oldLogPath = Path.Combine(logDirectory, logFileName);
            if (File.Exists(oldLogPath))
            {
                var archivePath = Path.Combine(
                    logDirectory,
                    $"game_{currentDate:yyyyMMdd}.log");
                File.Move(oldLogPath, archivePath, overwrite: true);
            }

            currentDate = DateTime.Today;
        }
    }

    /// <summary>
    /// 清理超过指定天数的日志文件。
    /// </summary>
    public void CleanupOldLogs(int maxDays = 30)
    {
        lock (lockObject)
        {
            var cutoffDate = DateTime.Today.AddDays(-maxDays);
            var files = Directory.GetFiles(logDirectory, "game_*.log");

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Length >= 10 && fileName.StartsWith("game_"))
                {
                    var dateStr = fileName.Substring(5, 8);
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                                // 忽略删除失败
                            }
                        }
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            lock (lockObject)
            {
                writer?.Close();
                writer?.Dispose();
                writer = null;
            }
            disposed = true;
        }
    }
}