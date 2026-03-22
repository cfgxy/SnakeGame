namespace SnakeGame.App.Input;

/// <summary>
/// 输入提供者接口，用于抽象 MonoGame 的键盘输入，便于测试。
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// 获取当前键盘状态。
    /// </summary>
    IKeyboardState GetState();
}

/// <summary>
/// 键盘状态接口，抽象 MonoGame 的 KeyboardState。
/// </summary>
public interface IKeyboardState
{
    /// <summary>
    /// 检查指定按键是否按下。
    /// </summary>
    bool IsKeyDown(Key key);

    /// <summary>
    /// 检查指定按键是否松开。
    /// </summary>
    bool IsKeyUp(Key key);
}

/// <summary>
/// 按键枚举，与 MonoGame.Keys 对应。
/// </summary>
public enum Key
{
    None = 0,
    Enter = 13,
    Escape = 27,
    Space = 32,
    Up = 38,
    Down = 40,
    Left = 37,
    Right = 39,
    W = 87,
    A = 65,
    S = 83,
    D = 68,
    F4 = 115,
    F5 = 116,
    M = 77,
    LeftAlt = 164,
    RightAlt = 165,
    LeftShift = 160,
    RightShift = 161,
}