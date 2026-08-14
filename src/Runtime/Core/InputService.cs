using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Supported mouse buttons for action mapping and input queries.
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    /// <summary>
    /// Configurable action binding container mapping action names to keyboard, gamepad, and mouse inputs.
    /// </summary>
    public class ActionBinding
    {
        public string Name { get; }
        public List<Keys> Keys { get; } = new List<Keys>();
        public List<Buttons> GamePadButtons { get; } = new List<Buttons>();
        public List<MouseButton> MouseButtons { get; } = new List<MouseButton>();

        public ActionBinding(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Unified multi-device Input Service handling Keyboard, Mouse, and GamePad with
    /// raw Win32 GetAsyncKeyState resilience on Windows so SDL2 focus drops never freeze controls.
    /// </summary>
    public class InputService
    {
        #region Static Singleton Instance

        private static InputService? _instance;
        public static InputService Instance => _instance ??= new InputService();

        #endregion

        #region Win32 Interop & Resilience

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        private static readonly bool IsWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static readonly Dictionary<Keys, int> KeyToVk = new Dictionary<Keys, int>
        {
            { Keys.A, 0x41 }, { Keys.B, 0x42 }, { Keys.C, 0x43 }, { Keys.D, 0x44 },
            { Keys.E, 0x45 }, { Keys.F, 0x46 }, { Keys.G, 0x47 }, { Keys.H, 0x48 },
            { Keys.I, 0x49 }, { Keys.J, 0x4A }, { Keys.K, 0x4B }, { Keys.L, 0x4C },
            { Keys.M, 0x4D }, { Keys.N, 0x4E }, { Keys.O, 0x4F }, { Keys.P, 0x50 },
            { Keys.Q, 0x51 }, { Keys.R, 0x52 }, { Keys.S, 0x53 }, { Keys.T, 0x54 },
            { Keys.U, 0x55 }, { Keys.V, 0x56 }, { Keys.W, 0x57 }, { Keys.X, 0x58 },
            { Keys.Y, 0x59 }, { Keys.Z, 0x5A },
            { Keys.D0, 0x30 }, { Keys.D1, 0x31 }, { Keys.D2, 0x32 }, { Keys.D3, 0x33 },
            { Keys.D4, 0x34 }, { Keys.D5, 0x35 }, { Keys.D6, 0x36 }, { Keys.D7, 0x37 },
            { Keys.D8, 0x38 }, { Keys.D9, 0x39 },
            { Keys.NumPad0, 0x60 }, { Keys.NumPad1, 0x61 }, { Keys.NumPad2, 0x62 },
            { Keys.NumPad3, 0x63 }, { Keys.NumPad4, 0x64 }, { Keys.NumPad5, 0x65 },
            { Keys.NumPad6, 0x66 }, { Keys.NumPad7, 0x67 }, { Keys.NumPad8, 0x68 },
            { Keys.NumPad9, 0x69 },
            { Keys.Up, 0x26 }, { Keys.Down, 0x28 }, { Keys.Left, 0x25 }, { Keys.Right, 0x27 },
            { Keys.Space, 0x20 }, { Keys.Enter, 0x0D }, { Keys.Escape, 0x1B },
            { Keys.Tab, 0x09 }, { Keys.Back, 0x08 }, { Keys.Delete, 0x2E }, { Keys.Insert, 0x2D },
            { Keys.Home, 0x24 }, { Keys.End, 0x23 }, { Keys.PageUp, 0x21 }, { Keys.PageDown, 0x22 },
            { Keys.LeftShift, 0xA0 }, { Keys.RightShift, 0xA1 },
            { Keys.LeftControl, 0xA2 }, { Keys.RightControl, 0xA3 },
            { Keys.LeftAlt, 0xA4 }, { Keys.RightAlt, 0xA5 },
            { Keys.F1, 0x70 }, { Keys.F2, 0x71 }, { Keys.F3, 0x72 }, { Keys.F4, 0x73 },
            { Keys.F5, 0x74 }, { Keys.F6, 0x75 }, { Keys.F7, 0x76 }, { Keys.F8, 0x77 },
            { Keys.F9, 0x78 }, { Keys.F10, 0x79 }, { Keys.F11, 0x7A }, { Keys.F12, 0x7B },
            { Keys.OemTilde, 0xC0 }, { Keys.OemMinus, 0xBD }, { Keys.OemPlus, 0xBB },
            { Keys.OemOpenBrackets, 0xDB }, { Keys.OemCloseBrackets, 0xDD },
            { Keys.OemPipe, 0xDC }, { Keys.OemSemicolon, 0xBA }, { Keys.OemQuotes, 0xDE },
            { Keys.OemComma, 0xBC }, { Keys.OemPeriod, 0xBE }, { Keys.OemQuestion, 0xBF }
        };

        private static bool CheckRawVkDown(int vKey)
        {
            if (IsWindowsPlatform && vKey > 0)
            {
                return (GetAsyncKeyState(vKey) & 0x8000) != 0;
            }
            return false;
        }

        #endregion

        #region State Fields

        private KeyboardState _currentKeyboard;
        private KeyboardState _previousKeyboard;

        private MouseState _currentMouse;
        private MouseState _previousMouse;

        private GamePadState _currentGamePad;
        private GamePadState _previousGamePad;

        private readonly Dictionary<string, ActionBinding> _actionBindings = new Dictionary<string, ActionBinding>(StringComparer.OrdinalIgnoreCase);

        // Previous frame raw state cache for just pressed / just released
        private readonly HashSet<Keys> _previousRawKeys = new HashSet<Keys>();
        private readonly HashSet<Keys> _currentRawKeys = new HashSet<Keys>();

        private readonly HashSet<MouseButton> _previousRawMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _currentRawMouseButtons = new HashSet<MouseButton>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current normalized movement vector from Keyboard (WASD/Arrows) and GamePad (ThumbStick/DPad).
        /// Magnitude is clamped between 0 and 1.
        /// </summary>
        public Vector2 MoveVector { get; private set; } = Vector2.Zero;

        /// <summary>
        /// Gets the current mouse cursor position in client/window coordinates.
        /// </summary>
        public Vector2 MouseClientPos { get; private set; } = Vector2.Zero;

        /// <summary>
        /// Alias for MouseClientPos.
        /// </summary>
        public Vector2 MousePosition => MouseClientPos;

        /// <summary>
        /// Optional mouse world coordinate calculated from camera unprojection.
        /// </summary>
        public Vector2 MouseWorldPos { get; set; } = Vector2.Zero;

        /// <summary>
        /// Mouse scroll wheel delta in the current frame.
        /// </summary>
        public int MouseWheelDelta { get; private set; } = 0;

        /// <summary>
        /// Selected GamePad PlayerIndex (default is PlayerIndex.One).
        /// </summary>
        public PlayerIndex GamePadIndex { get; set; } = PlayerIndex.One;

        /// <summary>
        /// Deadzone threshold for GamePad analog thumbsticks (default is 0.15f).
        /// </summary>
        public float ThumbstickDeadZone { get; set; } = 0.15f;

        /// <summary>
        /// Optional native window handle for pixel-perfect Win32 ScreenToClient fallback.
        /// </summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>
        /// Gets whether the current GamePad is connected.
        /// </summary>
        public bool IsGamePadConnected => _currentGamePad.IsConnected;

        #endregion

        #region Constructor

        public InputService()
        {
            RegisterDefaultActions();
        }

        private void RegisterDefaultActions()
        {
            RegisterAction("Fire",
                new[] { Keys.Space },
                new[] { Buttons.A, Buttons.RightTrigger },
                new[] { MouseButton.Left });

            RegisterAction("Boost",
                new[] { Keys.LeftShift, Keys.RightShift },
                new[] { Buttons.B, Buttons.LeftTrigger },
                new[] { MouseButton.Right });

            RegisterAction("Emp",
                new[] { Keys.E, Keys.F, Keys.C },
                new[] { Buttons.X, Buttons.Y },
                new[] { MouseButton.Middle });

            RegisterAction("Pause",
                new[] { Keys.Escape, Keys.P },
                new[] { Buttons.Start, Buttons.Back },
                Array.Empty<MouseButton>());

            RegisterAction("Confirm",
                new[] { Keys.Enter, Keys.Space },
                new[] { Buttons.A, Buttons.Start },
                new[] { MouseButton.Left });

            RegisterAction("Cancel",
                new[] { Keys.Escape, Keys.Back },
                new[] { Buttons.B, Buttons.Back },
                new[] { MouseButton.Right });
        }

        #endregion

        #region Update Cycle

        /// <summary>
        /// Updates input states from Keyboard, Mouse, GamePad, and raw Win32 APIs.
        /// </summary>
        public void Update(GameTime? gameTime = null)
        {
            // Snapshot previous states
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;
            _previousGamePad = _currentGamePad;

            _previousRawKeys.Clear();
            foreach (var k in _currentRawKeys) _previousRawKeys.Add(k);
            _currentRawKeys.Clear();

            _previousRawMouseButtons.Clear();
            foreach (var b in _currentRawMouseButtons) _previousRawMouseButtons.Add(b);
            _currentRawMouseButtons.Clear();

            // Fetch MonoGame states
            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();
            _currentGamePad = GamePad.GetState(GamePadIndex);

            // Compute Mouse Position with Win32 fallback resilience
            Vector2 mousePos = new Vector2(_currentMouse.X, _currentMouse.Y);
            if (IsWindowsPlatform && WindowHandle != IntPtr.Zero)
            {
                if (GetCursorPos(out POINT pt) && ScreenToClient(WindowHandle, ref pt))
                {
                    mousePos = new Vector2(pt.X, pt.Y);
                }
            }
            MouseClientPos = mousePos;
            MouseWheelDelta = _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

            // Cache active raw keys and mouse buttons for frame
            CacheActiveRawInputs();

            // Compute MoveVector
            UpdateMoveVector();
        }

        private void CacheActiveRawInputs()
        {
            foreach (var kvp in KeyToVk)
            {
                if (_currentKeyboard.IsKeyDown(kvp.Key) || CheckRawVkDown(kvp.Value))
                {
                    _currentRawKeys.Add(kvp.Key);
                }
            }

            // Mouse button raw checks
            if (_currentMouse.LeftButton == ButtonState.Pressed || CheckRawVkDown(0x01))
                _currentRawMouseButtons.Add(MouseButton.Left);
            if (_currentMouse.RightButton == ButtonState.Pressed || CheckRawVkDown(0x02))
                _currentRawMouseButtons.Add(MouseButton.Right);
            if (_currentMouse.MiddleButton == ButtonState.Pressed || CheckRawVkDown(0x04))
                _currentRawMouseButtons.Add(MouseButton.Middle);
            if (_currentMouse.XButton1 == ButtonState.Pressed || CheckRawVkDown(0x05))
                _currentRawMouseButtons.Add(MouseButton.XButton1);
            if (_currentMouse.XButton2 == ButtonState.Pressed || CheckRawVkDown(0x06))
                _currentRawMouseButtons.Add(MouseButton.XButton2);
        }

        private void UpdateMoveVector()
        {
            Vector2 move = Vector2.Zero;

            // Keyboard WASD / Arrows (with Win32 fallback)
            if (IsKeyDown(Keys.W) || IsKeyDown(Keys.Up)) move.Y += 1f;
            if (IsKeyDown(Keys.S) || IsKeyDown(Keys.Down)) move.Y -= 1f;
            if (IsKeyDown(Keys.A) || IsKeyDown(Keys.Left)) move.X -= 1f;
            if (IsKeyDown(Keys.D) || IsKeyDown(Keys.Right)) move.X += 1f;

            // GamePad Thumbstick and D-Pad
            if (_currentGamePad.IsConnected)
            {
                Vector2 stick = _currentGamePad.ThumbSticks.Left;
                if (stick.LengthSquared() > (ThumbstickDeadZone * ThumbstickDeadZone))
                {
                    move += stick;
                }

                if (_currentGamePad.DPad.Up == ButtonState.Pressed) move.Y += 1f;
                if (_currentGamePad.DPad.Down == ButtonState.Pressed) move.Y -= 1f;
                if (_currentGamePad.DPad.Left == ButtonState.Pressed) move.X -= 1f;
                if (_currentGamePad.DPad.Right == ButtonState.Pressed) move.X += 1f;
            }

            // Clamp / Normalize to unit circle
            float len = move.Length();
            if (len > 1f)
            {
                move /= len;
            }
            else if (len < 0.0001f)
            {
                move = Vector2.Zero;
            }

            MoveVector = move;
        }

        #endregion

        #region Core Action Methods

        /// <summary>
        /// Returns true if Fire action is currently held down (Space, Left Mouse Button, GamePad Right Trigger or A button).
        /// </summary>
        public bool IsFirePressed()
        {
            return IsActionPressed("Fire") ||
                   IsKeyDown(Keys.Space) ||
                   IsMouseButtonDown(MouseButton.Left) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Triggers.Right > 0.3f || _currentGamePad.Buttons.A == ButtonState.Pressed));
        }

        /// <summary>
        /// Returns true if Fire action was just pressed in the current frame.
        /// </summary>
        public bool IsFireJustPressed()
        {
            return IsActionJustPressed("Fire");
        }

        /// <summary>
        /// Returns true if Boost action is currently held down (Shift, Right Mouse Button, GamePad Left Trigger or B button).
        /// </summary>
        public bool IsBoostPressed()
        {
            return IsActionPressed("Boost") ||
                   IsKeyDown(Keys.LeftShift) ||
                   IsKeyDown(Keys.RightShift) ||
                   IsMouseButtonDown(MouseButton.Right) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Triggers.Left > 0.3f || _currentGamePad.Buttons.B == ButtonState.Pressed));
        }

        /// <summary>
        /// Returns true if Boost action was just pressed in the current frame.
        /// </summary>
        public bool IsBoostJustPressed()
        {
            return IsActionJustPressed("Boost");
        }

        /// <summary>
        /// Returns true if EMP/Bomb action is currently held down (E, F, C, Middle Mouse Button, GamePad X or Y button).
        /// </summary>
        public bool IsEmpPressed()
        {
            return IsActionPressed("Emp") ||
                   IsKeyDown(Keys.E) ||
                   IsKeyDown(Keys.F) ||
                   IsKeyDown(Keys.C) ||
                   IsMouseButtonDown(MouseButton.Middle) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Buttons.X == ButtonState.Pressed || _currentGamePad.Buttons.Y == ButtonState.Pressed));
        }

        /// <summary>
        /// Returns true if EMP/Bomb action was just pressed in the current frame.
        /// </summary>
        public bool IsEmpJustPressed()
        {
            return IsActionJustPressed("Emp");
        }

        /// <summary>
        /// Returns true if Pause action is currently held down (Escape, P, GamePad Start or Back button).
        /// </summary>
        public bool IsPausePressed()
        {
            return IsActionPressed("Pause") ||
                   IsKeyDown(Keys.Escape) ||
                   IsKeyDown(Keys.P) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Buttons.Start == ButtonState.Pressed || _currentGamePad.Buttons.Back == ButtonState.Pressed));
        }

        /// <summary>
        /// Returns true if Pause action was just pressed in the current frame.
        /// </summary>
        public bool IsPauseJustPressed()
        {
            return IsActionJustPressed("Pause");
        }

        #endregion

        #region Key & Mouse Query Methods

        /// <summary>
        /// Checks if a key is held down, combining MonoGame KeyboardState with Win32 GetAsyncKeyState resilience.
        /// </summary>
        public bool IsKeyDown(Keys key)
        {
            if (_currentKeyboard.IsKeyDown(key)) return true;
            if (KeyToVk.TryGetValue(key, out int vk))
            {
                return CheckRawVkDown(vk);
            }
            return false;
        }

        /// <summary>
        /// Checks if a key is currently up/released.
        /// </summary>
        public bool IsKeyUp(Keys key) => !IsKeyDown(key);

        /// <summary>
        /// Checks if a key was pressed down in the current frame.
        /// </summary>
        public bool IsKeyJustPressed(Keys key)
        {
            bool wasDown = _previousKeyboard.IsKeyDown(key) || _previousRawKeys.Contains(key);
            bool isDown = IsKeyDown(key);
            return isDown && !wasDown;
        }

        /// <summary>
        /// Checks if a key was released in the current frame.
        /// </summary>
        public bool IsKeyJustReleased(Keys key)
        {
            bool wasDown = _previousKeyboard.IsKeyDown(key) || _previousRawKeys.Contains(key);
            bool isDown = IsKeyDown(key);
            return wasDown && !isDown;
        }

        /// <summary>
        /// Checks if a mouse button is held down.
        /// </summary>
        public bool IsMouseButtonDown(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return _currentMouse.LeftButton == ButtonState.Pressed || CheckRawVkDown(0x01);
                case MouseButton.Right:
                    return _currentMouse.RightButton == ButtonState.Pressed || CheckRawVkDown(0x02);
                case MouseButton.Middle:
                    return _currentMouse.MiddleButton == ButtonState.Pressed || CheckRawVkDown(0x04);
                case MouseButton.XButton1:
                    return _currentMouse.XButton1 == ButtonState.Pressed || CheckRawVkDown(0x05);
                case MouseButton.XButton2:
                    return _currentMouse.XButton2 == ButtonState.Pressed || CheckRawVkDown(0x06);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a mouse button was just pressed down in the current frame.
        /// </summary>
        public bool IsMouseButtonJustPressed(MouseButton button)
        {
            bool wasDown = _previousRawMouseButtons.Contains(button);
            bool isDown = IsMouseButtonDown(button);
            return isDown && !wasDown;
        }

        /// <summary>
        /// Checks if a mouse button was released in the current frame.
        /// </summary>
        public bool IsMouseButtonJustReleased(MouseButton button)
        {
            bool wasDown = _previousRawMouseButtons.Contains(button);
            bool isDown = IsMouseButtonDown(button);
            return wasDown && !isDown;
        }

        /// <summary>
        /// Checks if a GamePad button is currently held down.
        /// </summary>
        public bool IsButtonDown(Buttons button)
        {
            return _currentGamePad.IsConnected && _currentGamePad.IsButtonDown(button);
        }

        /// <summary>
        /// Checks if a GamePad button was just pressed in the current frame.
        /// </summary>
        public bool IsButtonJustPressed(Buttons button)
        {
            return _currentGamePad.IsConnected && _currentGamePad.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);
        }

        /// <summary>
        /// Checks if a GamePad button was released in the current frame.
        /// </summary>
        public bool IsButtonJustReleased(Buttons button)
        {
            return _currentGamePad.IsConnected && _currentGamePad.IsButtonUp(button) && _previousGamePad.IsButtonDown(button);
        }

        #endregion

        #region Action Mapping API

        /// <summary>
        /// Registers or updates a named action with keyboard keys.
        /// </summary>
        public void RegisterAction(string actionName, params Keys[] keys)
        {
            RegisterAction(actionName, keys, Array.Empty<Buttons>(), Array.Empty<MouseButton>());
        }

        /// <summary>
        /// Registers or updates a named action with keyboard keys and gamepad buttons.
        /// </summary>
        public void RegisterAction(string actionName, Keys[] keys, Buttons[] buttons)
        {
            RegisterAction(actionName, keys, buttons, Array.Empty<MouseButton>());
        }

        /// <summary>
        /// Registers or updates a named action with keyboard keys, gamepad buttons, and mouse buttons.
        /// </summary>
        public void RegisterAction(string actionName, Keys[] keys, Buttons[] buttons, MouseButton[] mouseButtons)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return;

            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }

            binding.Keys.Clear();
            if (keys != null) binding.Keys.AddRange(keys);

            binding.GamePadButtons.Clear();
            if (buttons != null) binding.GamePadButtons.AddRange(buttons);

            binding.MouseButtons.Clear();
            if (mouseButtons != null) binding.MouseButtons.AddRange(mouseButtons);
        }

        /// <summary>
        /// Adds a key binding to an existing or new action.
        /// </summary>
        public void BindKey(string actionName, Keys key)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }
            if (!binding.Keys.Contains(key)) binding.Keys.Add(key);
        }

        /// <summary>
        /// Adds a gamepad button binding to an existing or new action.
        /// </summary>
        public void BindGamePadButton(string actionName, Buttons button)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }
            if (!binding.GamePadButtons.Contains(button)) binding.GamePadButtons.Add(button);
        }

        /// <summary>
        /// Adds a mouse button binding to an existing or new action.
        /// </summary>
        public void BindMouseButton(string actionName, MouseButton button)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }
            if (!binding.MouseButtons.Contains(button)) binding.MouseButtons.Add(button);
        }

        /// <summary>
        /// Checks if any input bound to the action name is currently pressed.
        /// </summary>
        public bool IsActionPressed(string actionName)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding)) return false;

            for (int i = 0; i < binding.Keys.Count; i++)
            {
                if (IsKeyDown(binding.Keys[i])) return true;
            }

            for (int i = 0; i < binding.MouseButtons.Count; i++)
            {
                if (IsMouseButtonDown(binding.MouseButtons[i])) return true;
            }

            if (_currentGamePad.IsConnected)
            {
                for (int i = 0; i < binding.GamePadButtons.Count; i++)
                {
                    var btn = binding.GamePadButtons[i];
                    if (btn == Buttons.RightTrigger && _currentGamePad.Triggers.Right > 0.3f) return true;
                    if (btn == Buttons.LeftTrigger && _currentGamePad.Triggers.Left > 0.3f) return true;
                    if (_currentGamePad.IsButtonDown(btn)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any input bound to the action name was just pressed in the current frame.
        /// </summary>
        public bool IsActionJustPressed(string actionName)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding)) return false;

            for (int i = 0; i < binding.Keys.Count; i++)
            {
                if (IsKeyJustPressed(binding.Keys[i])) return true;
            }

            for (int i = 0; i < binding.MouseButtons.Count; i++)
            {
                if (IsMouseButtonJustPressed(binding.MouseButtons[i])) return true;
            }

            if (_currentGamePad.IsConnected)
            {
                for (int i = 0; i < binding.GamePadButtons.Count; i++)
                {
                    var btn = binding.GamePadButtons[i];
                    if (btn == Buttons.RightTrigger && _currentGamePad.Triggers.Right > 0.3f && _previousGamePad.Triggers.Right <= 0.3f) return true;
                    if (btn == Buttons.LeftTrigger && _currentGamePad.Triggers.Left > 0.3f && _previousGamePad.Triggers.Left <= 0.3f) return true;
                    if (IsButtonJustPressed(btn)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any input bound to the action name was released in the current frame.
        /// </summary>
        public bool IsActionJustReleased(string actionName)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding)) return false;

            for (int i = 0; i < binding.Keys.Count; i++)
            {
                if (IsKeyJustReleased(binding.Keys[i])) return true;
            }

            for (int i = 0; i < binding.MouseButtons.Count; i++)
            {
                if (IsMouseButtonJustReleased(binding.MouseButtons[i])) return true;
            }

            if (_currentGamePad.IsConnected)
            {
                for (int i = 0; i < binding.GamePadButtons.Count; i++)
                {
                    var btn = binding.GamePadButtons[i];
                    if (btn == Buttons.RightTrigger && _currentGamePad.Triggers.Right <= 0.3f && _previousGamePad.Triggers.Right > 0.3f) return true;
                    if (btn == Buttons.LeftTrigger && _currentGamePad.Triggers.Left <= 0.3f && _previousGamePad.Triggers.Left > 0.3f) return true;
                    if (IsButtonJustReleased(btn)) return true;
                }
            }

            return false;
        }

        #endregion
    }
}
