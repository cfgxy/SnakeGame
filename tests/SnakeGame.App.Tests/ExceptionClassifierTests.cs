using SnakeGame.App.Exceptions;

namespace SnakeGame.App.Tests;

/// <summary>
/// 异常分类器测试。
/// </summary>
public class ExceptionClassifierTests
{
    #region 致命错误测试

    [Fact]
    public void Classify_OutOfMemoryException_ShouldBeCritical()
    {
        // Arrange
        var exception = new OutOfMemoryException();

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Critical);
        result.UserMessage.Should().Contain("内存不足");
        ExceptionClassifier.CanContinue(result.Severity).Should().BeFalse();
    }

    [Fact]
    public void Classify_GraphicsUnauthorizedAccessException_ShouldBeCritical()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("graphics device access denied");

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Critical);
        result.UserMessage.Should().Contain("图形设备");
    }

    #endregion

    #region 可恢复错误测试

    [Fact]
    public void Classify_FileNotFoundException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new FileNotFoundException();

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("文件不存在");
        ExceptionClassifier.CanContinue(result.Severity).Should().BeTrue();
    }

    [Fact]
    public void Classify_DirectoryNotFoundException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new DirectoryNotFoundException();

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("目录不存在");
    }

    [Fact]
    public void Classify_UnauthorizedAccessException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("无法访问文件");
    }

    [Fact]
    public void Classify_IOException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new IOException("Disk error");

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("文件读写错误");
    }

    [Fact]
    public void Classify_JsonException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new JsonException("Invalid JSON");

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("数据格式错误");
    }

    [Fact]
    public void Classify_FormatException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new FormatException();

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("数据格式无效");
    }

    #endregion

    #region 用户错误测试

    [Fact]
    public void Classify_ArgumentException_ShouldBeUserError()
    {
        // Arrange
        var exception = new ArgumentException();

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.User);
        result.UserMessage.Should().Contain("无效的操作");
        ExceptionClassifier.ShouldLog(result.Severity).Should().BeFalse();
    }

    [Fact]
    public void Classify_InvalidOperationException_ShouldBeUserError()
    {
        // Arrange
        var exception = new InvalidOperationException();

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.User);
        result.UserMessage.Should().Contain("当前状态无法执行此操作");
    }

    #endregion

    #region 未知错误测试

    [Fact]
    public void Classify_UnknownException_ShouldBeRecoverable()
    {
        // Arrange
        var exception = new CustomException("Unknown error");

        // Act
        var result = ExceptionClassifier.Classify(exception);

        // Assert
        result.Severity.Should().Be(ExceptionSeverity.Recoverable);
        result.UserMessage.Should().Contain("未知错误");
        result.UserMessage.Should().Contain(nameof(CustomException));
    }

    #endregion

    #region 辅助方法测试

    [Fact]
    public void ShouldLog_UserError_ShouldReturnFalse()
    {
        // Act
        var result = ExceptionClassifier.ShouldLog(ExceptionSeverity.User);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldLog_RecoverableError_ShouldReturnTrue()
    {
        // Act
        var result = ExceptionClassifier.ShouldLog(ExceptionSeverity.Recoverable);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldLog_CriticalError_ShouldReturnTrue()
    {
        // Act
        var result = ExceptionClassifier.ShouldLog(ExceptionSeverity.Critical);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldShowToUser_AllSeverities_ShouldReturnTrue()
    {
        // Act & Assert
        ExceptionClassifier.ShouldShowToUser(ExceptionSeverity.User).Should().BeTrue();
        ExceptionClassifier.ShouldShowToUser(ExceptionSeverity.Recoverable).Should().BeTrue();
        ExceptionClassifier.ShouldShowToUser(ExceptionSeverity.Critical).Should().BeTrue();
    }

    [Fact]
    public void CanContinue_Critical_ShouldReturnFalse()
    {
        // Act
        var result = ExceptionClassifier.CanContinue(ExceptionSeverity.Critical);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanContinue_RecoverableOrUser_ShouldReturnTrue()
    {
        // Act & Assert
        ExceptionClassifier.CanContinue(ExceptionSeverity.Recoverable).Should().BeTrue();
        ExceptionClassifier.CanContinue(ExceptionSeverity.User).Should().BeTrue();
    }

    #endregion

    /// <summary>
    /// 自定义异常，用于测试未知异常分类。
    /// </summary>
    private sealed class CustomException : Exception
    {
        public CustomException(string message) : base(message) { }
    }
}