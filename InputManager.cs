using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoKuratko
{
    public class InputManager
    {
        private MouseState _lastMouseState;
        private MouseState _currentMouseState;

        private KeyboardState _lastKeyboardState;
        private KeyboardState _currentKeyboardState;

        public bool IsKeyJustPressed(Keys key)
        {
            return _lastKeyboardState.IsKeyUp(key) && _currentKeyboardState.IsKeyDown(key);
        }

        public bool IsKeyJustReleased(Keys key)
        {
            return _lastKeyboardState.IsKeyDown(key) && _currentKeyboardState.IsKeyUp(key);
        }

        public void Refresh()
        {
            _lastMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            _lastKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();
        }

        public Point MousePosition => new Point(Mouse.GetState().X, Mouse.GetState().Y);

        public bool JustLeftClicked()
        {
            return _lastMouseState.LeftButton == ButtonState.Released &&
                   _currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool JustRightClicked()
        {
            return _lastMouseState.RightButton == ButtonState.Released &&
                   _currentMouseState.RightButton == ButtonState.Pressed;
        }
    }
}