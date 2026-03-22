using SnakeGame.App.Input;

namespace SnakeGame.App.Tests;

/// <summary>
/// 应用层输入抽象测试。
/// </summary>
public class InputProviderTests
{
    [Fact]
    public void MockInputProvider_ShouldReturnCorrectKeyDownState()
    {
        // Arrange
        var provider = new MockInputProvider();
        provider.SetKeyDown(Key.Up);

        // Act
        var state = provider.GetState();

        // Assert
        state.IsKeyDown(Key.Up).Should().BeTrue();
        state.IsKeyUp(Key.Up).Should().BeFalse();
    }

    [Fact]
    public void MockInputProvider_ShouldTrackKeyPressSequence()
    {
        // Arrange
        var provider = new MockInputProvider();
        provider.SimulateKeyPress(Key.Enter);

        // Act - First state (key down)
        var state1 = provider.GetState();
        state1.IsKeyDown(Key.Enter).Should().BeTrue();

        // Act - Second state (key up)
        var state2 = provider.GetState();
        state2.IsKeyUp(Key.Enter).Should().BeTrue();
    }

    [Fact]
    public void MockInputProvider_ShouldSupportMultipleKeys()
    {
        // Arrange
        var provider = new MockInputProvider();
        provider.SetKeyDown(Key.Up);
        provider.SetKeyDown(Key.Left);

        // Act
        var state = provider.GetState();

        // Assert
        state.IsKeyDown(Key.Up).Should().BeTrue();
        state.IsKeyDown(Key.Left).Should().BeTrue();
        state.IsKeyUp(Key.Right).Should().BeTrue();
    }

    [Fact]
    public void MockInputProvider_Clear_ShouldResetAllKeys()
    {
        // Arrange
        var provider = new MockInputProvider();
        provider.SetKeyDown(Key.Up);
        provider.SetKeyDown(Key.Down);

        // Act
        provider.Clear();
        var state = provider.GetState();

        // Assert
        state.IsKeyUp(Key.Up).Should().BeTrue();
        state.IsKeyUp(Key.Down).Should().BeTrue();
    }
}