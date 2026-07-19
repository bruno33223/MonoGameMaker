using Microsoft.Xna.Framework.Input;

namespace MonoGameMaker.IDE.Core
{
    public class InputManager
    {
        public KeyboardState KeyboardState { get; private set; }
        public KeyboardState PreviousKeyboardState { get; private set; }
        public MouseState MouseState { get; private set; }

        public void Update()
        {
            PreviousKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();
        }

        public bool IsKeyDown(Keys key) => KeyboardState.IsKeyDown(key);
        public bool IsKeyUp(Keys key) => KeyboardState.IsKeyUp(key);
        public bool IsKeyPressed(Keys key) => KeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);
    }
}
