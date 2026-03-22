using SnakeGame.App.Input;
using Key = SnakeGame.App.Input.Key;

namespace SnakeGame.App.Screens;

/// <summary>
/// 屏幕状态机，管理应用屏幕切换逻辑。
/// 独立于 MonoGame，便于单元测试。
/// </summary>
public sealed class ScreenStateMachine
{
    private AppScreen currentScreen = AppScreen.MainMenu;
    private int menuIndex;
    private int selectedLevelIndex;
    private bool paused;

    public AppScreen CurrentScreen => currentScreen;
    public int MenuIndex => menuIndex;
    public int SelectedLevelIndex => selectedLevelIndex;
    public bool IsPaused => paused;

    /// <summary>
    /// 处理主菜单输入。
    /// </summary>
    /// <param name="keyboard">当前键盘状态</param>
    /// <param name="previousKeyboard">上一帧键盘状态</param>
    /// <returns>菜单操作结果</returns>
    public MenuResult HandleMainMenuInput(IKeyboardState keyboard, IKeyboardState? previousKeyboard)
    {
        const int menuCount = 5;

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Down))
        {
            menuIndex = (menuIndex + 1) % menuCount;
            return MenuResult.Navigation;
        }

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Up))
        {
            menuIndex = (menuIndex - 1 + menuCount) % menuCount;
            return MenuResult.Navigation;
        }

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Enter))
        {
            return menuIndex switch
            {
                0 => MenuResult.StartGame,
                1 => MenuResult.SelectLevel,
                2 => MenuResult.Leaderboard,
                3 => MenuResult.Settings,
                4 => MenuResult.Exit,
                _ => MenuResult.None
            };
        }

        return MenuResult.None;
    }

    /// <summary>
    /// 处理关卡选择输入。
    /// </summary>
    public LevelSelectResult HandleLevelSelectInput(
        IKeyboardState keyboard,
        IKeyboardState? previousKeyboard,
        int maxLevelIndex)
    {
        if (IsKeyPressed(keyboard, previousKeyboard, Key.Left))
        {
            selectedLevelIndex = Math.Max(0, selectedLevelIndex - 1);
            return LevelSelectResult.Navigation;
        }

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Right))
        {
            selectedLevelIndex = Math.Min(maxLevelIndex, selectedLevelIndex + 1);
            return LevelSelectResult.Navigation;
        }

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Enter))
        {
            return LevelSelectResult.Confirm;
        }

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Escape))
        {
            return LevelSelectResult.Back;
        }

        return LevelSelectResult.None;
    }

    /// <summary>
    /// 处理游戏中输入。
    /// </summary>
    public PlayingResult HandlePlayingInput(IKeyboardState keyboard, IKeyboardState? previousKeyboard)
    {
        if (IsKeyPressed(keyboard, previousKeyboard, Key.Escape))
        {
            paused = !paused;
            return paused ? PlayingResult.Pause : PlayingResult.Resume;
        }

        return paused ? PlayingResult.None : PlayingResult.Running;
    }

    /// <summary>
    /// 处理结果界面输入。
    /// </summary>
    public ResultResult HandleResultInput(IKeyboardState keyboard, IKeyboardState? previousKeyboard)
    {
        if (IsKeyPressed(keyboard, previousKeyboard, Key.Enter))
        {
            return ResultResult.PlayAgain;
        }

        if (IsKeyPressed(keyboard, previousKeyboard, Key.Escape))
        {
            return ResultResult.BackToMenu;
        }

        return ResultResult.None;
    }

    /// <summary>
    /// 切换到指定屏幕。
    /// </summary>
    public void NavigateTo(AppScreen screen)
    {
        currentScreen = screen;
        if (screen == AppScreen.MainMenu)
        {
            menuIndex = 0;
        }
    }

    /// <summary>
    /// 重置状态。
    /// </summary>
    public void Reset()
    {
        currentScreen = AppScreen.MainMenu;
        menuIndex = 0;
        selectedLevelIndex = 0;
        paused = false;
    }

    private static bool IsKeyPressed(IKeyboardState current, IKeyboardState? previous, Key key)
    {
        return current.IsKeyDown(key) && (previous?.IsKeyUp(key) ?? true);
    }
}

/// <summary>
/// 应用屏幕枚举。
/// </summary>
public enum AppScreen
{
    MainMenu,
    LevelSelect,
    Leaderboard,
    Settings,
    Playing,
    Result
}

/// <summary>
/// 主菜单操作结果。
/// </summary>
public enum MenuResult
{
    None,
    Navigation,
    StartGame,
    SelectLevel,
    Leaderboard,
    Settings,
    Exit
}

/// <summary>
/// 关卡选择操作结果。
/// </summary>
public enum LevelSelectResult
{
    None,
    Navigation,
    Confirm,
    Back
}

/// <summary>
/// 游戏中操作结果。
/// </summary>
public enum PlayingResult
{
    None,
    Running,
    Pause,
    Resume
}

/// <summary>
/// 结果界面操作结果。
/// </summary>
public enum ResultResult
{
    None,
    PlayAgain,
    BackToMenu
}