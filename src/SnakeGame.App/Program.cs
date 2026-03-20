using System.Text;
using SnakeGame.App;

var logDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "SnakeGame",
    "logs");

Directory.CreateDirectory(logDirectory);

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    if (eventArgs.ExceptionObject is Exception exception)
    {
        WriteCrashLog(logDirectory, exception);
    }
};

try
{
    using var game = new SnakeGameApp();
    game.Run();
}
catch (Exception exception)
{
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
