namespace SnakeGame.App.Input;

/// <summary>
/// Mock 键盘输入提供者，用于单元测试。
/// 支持预设按键状态和按键序列。
/// </summary>
public sealed class MockInputProvider : IInputProvider
{
    private readonly Stack<MockKeyboardState> stateQueue = new();
    private MockKeyboardState currentState = new();

    /// <summary>
    /// 设置当前帧的按键状态。
    /// </summary>
    public void SetKeyDown(Key key)
    {
        currentState.SetKeyDown(key);
    }

    /// <summary>
    /// 设置当前帧的按键为松开状态。
    /// </summary>
    public void SetKeyUp(Key key)
    {
        currentState.SetKeyUp(key);
    }

    /// <summary>
    /// 清除所有按键状态。
    /// </summary>
    public void Clear()
    {
        currentState = new MockKeyboardState();
    }

    /// <summary>
    /// 将当前状态入队，用于模拟按键序列。
    /// </summary>
    public void EnqueueState()
    {
        stateQueue.Push(currentState);
        currentState = new MockKeyboardState();
    }

    /// <summary>
    /// 模拟按键按下再松开的完整操作。
    /// </summary>
    /// <param name="key">按键</param>
    /// <param name="duration">按住帧数，默认 1 帧</param>
    public void SimulateKeyPress(Key key, int duration = 1)
    {
        // 按下帧
        SetKeyDown(key);
        EnqueueState();
        
        // 持续帧
        for (int i = 0; i < duration - 1; i++)
        {
            SetKeyDown(key);
            EnqueueState();
        }
        
        // 松开帧
        Clear();
        EnqueueState();
    }

    public IKeyboardState GetState()
    {
        if (stateQueue.Count > 0)
        {
            currentState = stateQueue.Pop();
        }
        return currentState;
    }

    private sealed class MockKeyboardState : IKeyboardState
    {
        private readonly HashSet<Key> pressedKeys = new();

        public void SetKeyDown(Key key)
        {
            pressedKeys.Add(key);
        }

        public void SetKeyUp(Key key)
        {
            pressedKeys.Remove(key);
        }

        public bool IsKeyDown(Key key)
        {
            return pressedKeys.Contains(key);
        }

        public bool IsKeyUp(Key key)
        {
            return !pressedKeys.Contains(key);
        }
    }
}