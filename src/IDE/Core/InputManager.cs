using Microsoft.Xna.Framework.Input;

namespace MonoGameMaker.IDE.Core
{
    public class InputManager
    {
        public KeyboardState KeyboardState { get; private set; }
        public MouseState MouseState { get; private set; }

        public void Update()
        {
            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();
        }

        public bool IsKeyDown(Keys key) => KeyboardState.IsKeyDown(key);
        public bool IsKeyUp(Keys key) => KeyboardState.IsKeyUp(key);
    }
}
