namespace SnakeGame.App.Exceptions;

/// <summary>
/// 异常严重级别。
/// </summary>
public enum ExceptionSeverity
{
    /// <summary>
    /// 致命错误，应用无法继续运行。
    /// 例如：图形设备初始化失败。
    /// </summary>
    Critical,

    /// <summary>
    /// 可恢复错误，应用可以继续运行但功能受限。
    /// 例如：配置文件损坏、排行榜文件损坏。
    /// </summary>
    Recoverable,

    /// <summary>
    /// 用户操作错误，不影响应用运行。
    /// 例如：无效输入。
    /// </summary>
    User
}

/// <summary>
/// 异常分类结果。
/// </summary>
public sealed record ExceptionClassification(
    ExceptionSeverity Severity,
    string UserMessage,
    string? RecoveryHint = null);

/// <summary>
/// 异常分类器，根据异常类型判断严重级别和用户提示。
/// </summary>
public static class ExceptionClassifier
{
    /// <summary>
    /// 分类异常。
    /// </summary>
    /// <param name="exception">要分类的异常</param>
    /// <returns>分类结果</returns>
    public static ExceptionClassification Classify(Exception exception)
    {
        return exception switch
        {
            // 致命错误
            OutOfMemoryException => new ExceptionClassification(
                ExceptionSeverity.Critical,
                "内存不足，无法继续运行。",
                "请关闭其他应用程序后重试。"),

            UnauthorizedAccessException ex when ex.Message.Contains("graphics") => new ExceptionClassification(
                ExceptionSeverity.Critical,
                "无法访问图形设备。",
                "请检查显卡驱动是否正常。"),

            // 可恢复错误
            UnauthorizedAccessException ex => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "无法访问文件。",
                "请检查文件权限。"),

            FileNotFoundException => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "文件不存在，将使用默认设置。",
                null),

            DirectoryNotFoundException => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "目录不存在，将自动创建。",
                null),

            IOException ex => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "文件读写错误。",
                $"错误详情：{ex.Message}"),

            JsonException => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "数据格式错误，将使用默认值。",
                null),

            FormatException => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "数据格式无效，将使用默认值。",
                null),

            // 用户错误
            ArgumentException => new ExceptionClassification(
                ExceptionSeverity.User,
                "无效的操作。",
                null),

            InvalidOperationException => new ExceptionClassification(
                ExceptionSeverity.User,
                "当前状态无法执行此操作。",
                null),

            // 未知错误
            _ => new ExceptionClassification(
                ExceptionSeverity.Recoverable,
                "发生未知错误。",
                $"错误类型：{exception.GetType().Name}")
        };
    }

    /// <summary>
    /// 判断异常是否需要记录到日志。
    /// </summary>
    public static bool ShouldLog(ExceptionSeverity severity)
    {
        return severity != ExceptionSeverity.User;
    }

    /// <summary>
    /// 判断异常是否需要显示给用户。
    /// </summary>
    public static bool ShouldShowToUser(ExceptionSeverity severity)
    {
        return true;
    }

    /// <summary>
    /// 判断应用是否可以继续运行。
    /// </summary>
    public static bool CanContinue(ExceptionSeverity severity)
    {
        return severity != ExceptionSeverity.Critical;
    }
}