using Microsoft.Xna.Framework.Input;
using Key = SnakeGame.App.Input.Key;

namespace SnakeGame.App.Input;

/// <summary>
/// MonoGame 键盘输入提供者的实现。
/// </summary>
internal sealed class MonoGameInputProvider : IInputProvider
{
    public IKeyboardState GetState()
    {
        return new MonoGameKeyboardState(Keyboard.GetState());
    }

    private sealed class MonoGameKeyboardState : IKeyboardState
    {
        private readonly KeyboardState state;

        public MonoGameKeyboardState(KeyboardState state)
        {
            this.state = state;
        }

        public bool IsKeyDown(Key key)
        {
            return state.IsKeyDown(ConvertKey(key));
        }

        public bool IsKeyUp(Key key)
        {
            return state.IsKeyUp(ConvertKey(key));
        }

        private static Keys ConvertKey(Key key)
        {
            return key switch
            {
                Key.Enter => Keys.Enter,
                Key.Escape => Keys.Escape,
                Key.Space => Keys.Space,
                Key.Up => Keys.Up,
                Key.Down => Keys.Down,
                Key.Left => Keys.Left,
                Key.Right => Keys.Right,
                Key.W => Keys.W,
                Key.A => Keys.A,
                Key.S => Keys.S,
                Key.D => Keys.D,
                Key.F4 => Keys.F4,
                Key.F5 => Keys.F5,
                Key.M => Keys.M,
                Key.LeftAlt => Keys.LeftAlt,
                Key.RightAlt => Keys.RightAlt,
                Key.LeftShift => Keys.LeftShift,
                Key.RightShift => Keys.RightShift,
                _ => Keys.None
            };
        }
    }
}