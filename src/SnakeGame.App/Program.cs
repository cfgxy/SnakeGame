using System.Reflection;
using System.Text;
using SnakeGame.App;

var logDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "SnakeGame",
    "logs");

Directory.CreateDirectory(logDirectory);
var startupLogPath = Path.Combine(logDirectory, "startup.log");

// 记录启动信息
File.AppendAllText(startupLogPath, $"\n=== 启动时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
File.AppendAllText(startupLogPath, $"版本：{Assembly.GetEntryAssembly()?.GetName().Version}\n");
File.AppendAllText(startupLogPath, $".NET 版本：{Environment.Version}\n");
File.AppendAllText(startupLogPath, $"操作系统：{Environment.OSVersion}\n");
File.AppendAllText(startupLogPath, $"工作目录：{Environment.CurrentDirectory}\n");

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    if (eventArgs.ExceptionObject is Exception exception)
    {
        File.AppendAllText(startupLogPath, $"❌ 未处理异常：{exception}\n");
        WriteCrashLog(logDirectory, exception);
    }
};

try
{
    File.AppendAllText(startupLogPath, "✓ 开始创建游戏实例...\n");
    using var game = new SnakeGameApp();
    File.AppendAllText(startupLogPath, "✓ 游戏实例创建成功，开始运行...\n");
    game.Run();
}
catch (Exception exception)
{
    File.AppendAllText(startupLogPath, $"❌ 启动异常：{exception}\n");
    WriteCrashLog(logDirectory, exception);
    throw;
}

static void WriteCrashLog(string logDirectory, Exception exception)
{
    var path = Path.Combine(logDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    var builder = new StringBuilder();
    builder.AppendLine($"Time: {DateTime.Now:O}");
    builder.AppendLine($"Type: {exception.GetType().FullName}");
    builder.AppendLine($"Message: {exception.Message}");
    builder.AppendLine("StackTrace:");
    builder.AppendLine(exception.ToString());
    File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
}
