using SnakeGame.App.Input;
using SnakeGame.App.Screens;
using Key = SnakeGame.App.Input.Key;

namespace SnakeGame.App.Tests;

/// <summary>
/// 屏幕状态机测试。
/// </summary>
public class ScreenStateMachineTests
{
    private readonly ScreenStateMachine stateMachine = new();
    private readonly MockInputProvider inputProvider = new();

    #region 主菜单测试

    [Fact]
    public void MainMenu_DownKey_ShouldNavigateDown()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Down);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.Navigation);
        stateMachine.MenuIndex.Should().Be(1);
    }

    [Fact]
    public void MainMenu_UpKey_ShouldNavigateUp()
    {
        // Arrange
        stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, 2);
        inputProvider.SimulateKeyPress(Key.Up);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.Navigation);
        stateMachine.MenuIndex.Should().Be(1);
    }

    [Fact]
    public void MainMenu_DownAtBottom_ShouldWrapToTop()
    {
        // Arrange
        var field = stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(stateMachine, 4); // 最后一项
        inputProvider.SimulateKeyPress(Key.Down);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.Navigation);
        stateMachine.MenuIndex.Should().Be(0); // 循环到第一项
    }

    [Fact]
    public void MainMenu_EnterAtIndex0_ShouldReturnStartGame()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.StartGame);
    }

    [Fact]
    public void MainMenu_EnterAtIndex1_ShouldReturnSelectLevel()
    {
        // Arrange
        var field = stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(stateMachine, 1);
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.SelectLevel);
    }

    [Fact]
    public void MainMenu_EnterAtIndex2_ShouldReturnLeaderboard()
    {
        // Arrange
        var field = stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(stateMachine, 2);
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.Leaderboard);
    }

    [Fact]
    public void MainMenu_EnterAtIndex3_ShouldReturnSettings()
    {
        // Arrange
        var field = stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(stateMachine, 3);
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.Settings);
    }

    [Fact]
    public void MainMenu_EnterAtIndex4_ShouldReturnExit()
    {
        // Arrange
        var field = stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(stateMachine, 4);
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleMainMenuInput(state, null);

        // Assert
        result.Should().Be(MenuResult.Exit);
    }

    #endregion

    #region 关卡选择测试

    [Fact]
    public void LevelSelect_LeftKey_ShouldNavigateLeft()
    {
        // Arrange
        stateMachine.GetType()
            .GetField("selectedLevelIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, 2);
        inputProvider.SimulateKeyPress(Key.Left);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleLevelSelectInput(state, null, 5);

        // Assert
        result.Should().Be(LevelSelectResult.Navigation);
        stateMachine.SelectedLevelIndex.Should().Be(1);
    }

    [Fact]
    public void LevelSelect_RightKey_ShouldNavigateRight()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Right);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleLevelSelectInput(state, null, 5);

        // Assert
        result.Should().Be(LevelSelectResult.Navigation);
        stateMachine.SelectedLevelIndex.Should().Be(1);
    }

    [Fact]
    public void LevelSelect_RightAtMax_ShouldNotExceedMax()
    {
        // Arrange
        stateMachine.GetType()
            .GetField("selectedLevelIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, 5);
        inputProvider.SimulateKeyPress(Key.Right);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleLevelSelectInput(state, null, 5);

        // Assert
        result.Should().Be(LevelSelectResult.Navigation);
        stateMachine.SelectedLevelIndex.Should().Be(5);
    }

    [Fact]
    public void LevelSelect_Enter_ShouldReturnConfirm()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleLevelSelectInput(state, null, 5);

        // Assert
        result.Should().Be(LevelSelectResult.Confirm);
    }

    [Fact]
    public void LevelSelect_Escape_ShouldReturnBack()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Escape);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleLevelSelectInput(state, null, 5);

        // Assert
        result.Should().Be(LevelSelectResult.Back);
    }

    #endregion

    #region 游戏中测试

    [Fact]
    public void Playing_Escape_ShouldTogglePause()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Escape);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandlePlayingInput(state, null);

        // Assert
        result.Should().Be(PlayingResult.Pause);
        stateMachine.IsPaused.Should().BeTrue();
    }

    [Fact]
    public void Playing_EscapeTwice_ShouldToggleResume()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Escape);
        var state1 = inputProvider.GetState();
        stateMachine.HandlePlayingInput(state1, null);

        inputProvider.SimulateKeyPress(Key.Escape);
        var state2 = inputProvider.GetState();

        // Act
        var result = stateMachine.HandlePlayingInput(state2, state1);

        // Assert
        result.Should().Be(PlayingResult.Resume);
        stateMachine.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Playing_NoInput_WhenNotPaused_ShouldReturnRunning()
    {
        // Arrange
        inputProvider.Clear();
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandlePlayingInput(state, null);

        // Assert
        result.Should().Be(PlayingResult.Running);
    }

    [Fact]
    public void Playing_NoInput_WhenPaused_ShouldReturnNone()
    {
        // Arrange
        stateMachine.GetType()
            .GetField("paused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, true);
        inputProvider.Clear();
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandlePlayingInput(state, null);

        // Assert
        result.Should().Be(PlayingResult.None);
    }

    #endregion

    #region 结果界面测试

    [Fact]
    public void Result_Enter_ShouldReturnPlayAgain()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Enter);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleResultInput(state, null);

        // Assert
        result.Should().Be(ResultResult.PlayAgain);
    }

    [Fact]
    public void Result_Escape_ShouldReturnBackToMenu()
    {
        // Arrange
        inputProvider.SimulateKeyPress(Key.Escape);
        var state = inputProvider.GetState();

        // Act
        var result = stateMachine.HandleResultInput(state, null);

        // Assert
        result.Should().Be(ResultResult.BackToMenu);
    }

    #endregion

    #region 导航测试

    [Fact]
    public void NavigateTo_ShouldChangeScreen()
    {
        // Act
        stateMachine.NavigateTo(AppScreen.Playing);

        // Assert
        stateMachine.CurrentScreen.Should().Be(AppScreen.Playing);
    }

    [Fact]
    public void NavigateToMainMenu_ShouldResetMenuIndex()
    {
        // Arrange
        stateMachine.GetType()
            .GetField("menuIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, 3);

        // Act
        stateMachine.NavigateTo(AppScreen.MainMenu);

        // Assert
        stateMachine.MenuIndex.Should().Be(0);
    }

    [Fact]
    public void Reset_ShouldResetAllState()
    {
        // Arrange
        stateMachine.NavigateTo(AppScreen.Playing);
        stateMachine.GetType()
            .GetField("paused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, true);

        // Act
        stateMachine.Reset();

        // Assert
        stateMachine.CurrentScreen.Should().Be(AppScreen.MainMenu);
        stateMachine.MenuIndex.Should().Be(0);
        stateMachine.SelectedLevelIndex.Should().Be(0);
        stateMachine.IsPaused.Should().BeFalse();
    }

    #endregion

    #region 按键检测测试

    [Fact]
    public void IsKeyPressed_KeyDown_ShouldReturnTrue()
    {
        // Arrange
        inputProvider.SetKeyDown(Key.Enter);
        var current = inputProvider.GetState();
        inputProvider.Clear();
        var previous = inputProvider.GetState();

        // Act
        inputProvider.SetKeyDown(Key.Enter);
        var now = inputProvider.GetState();
        var result = stateMachine.HandleMainMenuInput(now, previous);

        // Assert
        result.Should().Be(MenuResult.StartGame);
    }

    [Fact]
    public void IsKeyPressed_KeyHeld_ShouldReturnFalse()
    {
        // Arrange - 按键持续按下
        inputProvider.SetKeyDown(Key.Enter);
        var previous = inputProvider.GetState();
        var current = inputProvider.GetState(); // 同样的状态

        // Act
        var result = stateMachine.HandleMainMenuInput(current, previous);

        // Assert
        result.Should().Be(MenuResult.None);
    }

    #endregion
}