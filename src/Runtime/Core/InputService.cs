using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime.Resolution;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Tipos de dispositivo ativo de entrada.
    /// </summary>
    public enum InputDeviceType
    {
        KeyboardMouse,
        GamePad
    }

    /// <summary>
    /// Botões de mouse suportados para mapeamento de ações.
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
    /// Container configurável de mapeamento de ações associando nomes a teclas, botões de gamepad e mouse.
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
    /// Serviço unificado de entrada multiplataforma 100% C# gerenciado (Zero Win32 P/Invoke),
    /// operando sobre as abstrações de janela do MonoGame DesktopGL (SDL2).
    /// Integra detecção automática de dispositivo ativo, threshold de movimento do mouse (>2px),
    /// conversão direta para coordenadas do ResolutionManager (1080p) e suporte a Gamepads/Joysticks.
    /// </summary>
    public class InputService
    {
        #region Static Singleton Instance

        private static InputService? _instance;
        public static InputService Instance => _instance ??= new InputService();

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
        private readonly HashSet<Keys> _previousKeys = new HashSet<Keys>();
        private readonly HashSet<Keys> _currentKeys = new HashSet<Keys>();

        private readonly HashSet<MouseButton> _previousMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _currentMouseButtons = new HashSet<MouseButton>();

        private Vector2 _lastRawMousePos = Vector2.Zero;

        #endregion

        #region Public Properties

        /// <summary>
        /// Dispositivo de controle atualmente ativo com base na última interação física detectada.
        /// </summary>
        public InputDeviceType ActiveDevice { get; private set; } = InputDeviceType.KeyboardMouse;

        /// <summary>
        /// Vetor de movimentação normalizado [0, 1] combinando Teclado (WASD/Setas) e GamePad (ThumbStick/D-Pad).
        /// </summary>
        public Vector2 MoveVector { get; private set; } = Vector2.Zero;

        /// <summary>
        /// Posição do cursor do mouse em coordenadas físicas de janela/cliente.
        /// </summary>
        public Vector2 MouseClientPos { get; private set; } = Vector2.Zero;

        /// <summary>
        /// Posição do cursor do mouse projetada no espaço virtual de referência (1920x1080) via ResolutionManager.
        /// </summary>
        public Vector2 MouseVirtualPos { get; private set; } = Vector2.Zero;

        /// <summary>
        /// Posição do mouse (por padrão projeta as coordenadas virtuais de resolução para consistência de gameplay).
        /// </summary>
        public Vector2 MousePosition => MouseVirtualPos;

        /// <summary>
        /// Coordenada do mouse desprojetada no espaço tridimensional/mundo de combate.
        /// </summary>
        public Vector2 MouseWorldPos { get; set; } = Vector2.Zero;

        /// <summary>
        /// Variação da roda de rolagem do mouse no frame ativo.
        /// </summary>
        public int MouseWheelDelta { get; private set; } = 0;

        /// <summary>
        /// Índice de jogador do GamePad monitorado (padrão PlayerIndex.One).
        /// </summary>
        public PlayerIndex GamePadIndex { get; set; } = PlayerIndex.One;

        /// <summary>
        /// Zona morta analógica dos analógicos do GamePad (padrão 0.15f).
        /// </summary>
        public float ThumbstickDeadZone { get; set; } = 0.15f;

        /// <summary>
        /// Limiar mínimo de deslocamento do mouse (em pixels) para comutar dispositivo ativo para KeyboardMouse (padrão: 2.0px).
        /// </summary>
        public float MouseMovementThreshold { get; set; } = 2.0f;

        /// <summary>
        /// Handle opcional de janela mantido para compatibilidade de API (sem chamadas nativas de SO).
        /// </summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>
        /// Indica se o GamePad selecionado está fisicamente conectado.
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
        /// Atualiza os estados de entrada de Teclado, Mouse e GamePad através de APIs SDL2 gerenciadas do MonoGame,
        /// sem nenhuma chamada Win32 P/Invoke.
        /// </summary>
        public void Update(GameTime? gameTime = null)
        {
            // Snapshot previous states
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;
            _previousGamePad = _currentGamePad;

            _previousKeys.Clear();
            foreach (var k in _currentKeys) _previousKeys.Add(k);
            _currentKeys.Clear();

            _previousMouseButtons.Clear();
            foreach (var b in _currentMouseButtons) _previousMouseButtons.Add(b);
            _currentMouseButtons.Clear();

            // Fetch managed MonoGame states
            _currentKeyboard = Keyboard.GetState();
            _currentMouse = Mouse.GetState();
            _currentGamePad = GetPrimaryGamePadState();

            // Calculate physical client mouse coordinates
            Vector2 currentPhysicalMouse = new Vector2(_currentMouse.X, _currentMouse.Y);
            MouseClientPos = currentPhysicalMouse;
            MouseVirtualPos = ResolutionManager.ScreenToVirtual(currentPhysicalMouse);
            MouseWheelDelta = _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

            // Cache active inputs
            CacheActiveInputs();

            // Device activity detection with >2px threshold
            DetectActiveDevice(currentPhysicalMouse);

            // Compute normalized MoveVector
            UpdateMoveVector();
        }

        private GamePadState GetPrimaryGamePadState()
        {
            var pad = GamePad.GetState(GamePadIndex);
            if (pad.IsConnected) return pad;

            // Universal fallback scan across PlayerIndex 0 to 3
            for (int i = 0; i < 4; i++)
            {
                var candidate = GamePad.GetState((PlayerIndex)i);
                if (candidate.IsConnected)
                {
                    return candidate;
                }
            }

            return pad;
        }

        private void DetectActiveDevice(Vector2 currentPhysicalMouse)
        {
            // Mouse movement threshold > 2px
            float mouseDelta = Vector2.Distance(currentPhysicalMouse, _lastRawMousePos);
            if (mouseDelta > MouseMovementThreshold)
            {
                ActiveDevice = InputDeviceType.KeyboardMouse;
                _lastRawMousePos = currentPhysicalMouse;
            }

            // Mouse button or key press
            if (_currentMouseButtons.Count > 0 || _currentKeys.Count > 0 || MouseWheelDelta != 0)
            {
                ActiveDevice = InputDeviceType.KeyboardMouse;
            }

            // Gamepad activity detection
            if (_currentGamePad.IsConnected)
            {
                if (_currentGamePad.ThumbSticks.Left.LengthSquared() > (ThumbstickDeadZone * ThumbstickDeadZone) ||
                    _currentGamePad.ThumbSticks.Right.LengthSquared() > (ThumbstickDeadZone * ThumbstickDeadZone) ||
                    _currentGamePad.Triggers.Left > 0.15f ||
                    _currentGamePad.Triggers.Right > 0.15f ||
                    HasAnyGamePadButtonPressed(_currentGamePad))
                {
                    ActiveDevice = InputDeviceType.GamePad;
                }
            }
        }

        private static bool HasAnyGamePadButtonPressed(GamePadState state)
        {
            return state.Buttons.A == ButtonState.Pressed ||
                   state.Buttons.B == ButtonState.Pressed ||
                   state.Buttons.X == ButtonState.Pressed ||
                   state.Buttons.Y == ButtonState.Pressed ||
                   state.Buttons.Start == ButtonState.Pressed ||
                   state.Buttons.Back == ButtonState.Pressed ||
                   state.Buttons.LeftShoulder == ButtonState.Pressed ||
                   state.Buttons.RightShoulder == ButtonState.Pressed ||
                   state.DPad.Up == ButtonState.Pressed ||
                   state.DPad.Down == ButtonState.Pressed ||
                   state.DPad.Left == ButtonState.Pressed ||
                   state.DPad.Right == ButtonState.Pressed;
        }

        private void CacheActiveInputs()
        {
            var pressedKeys = _currentKeyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                _currentKeys.Add(pressedKeys[i]);
            }

            if (_currentMouse.LeftButton == ButtonState.Pressed) _currentMouseButtons.Add(MouseButton.Left);
            if (_currentMouse.RightButton == ButtonState.Pressed) _currentMouseButtons.Add(MouseButton.Right);
            if (_currentMouse.MiddleButton == ButtonState.Pressed) _currentMouseButtons.Add(MouseButton.Middle);
            if (_currentMouse.XButton1 == ButtonState.Pressed) _currentMouseButtons.Add(MouseButton.XButton1);
            if (_currentMouse.XButton2 == ButtonState.Pressed) _currentMouseButtons.Add(MouseButton.XButton2);
        }

        private void UpdateMoveVector()
        {
            Vector2 move = Vector2.Zero;

            // Keyboard WASD / Arrows
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

        public bool IsFirePressed()
        {
            return IsActionPressed("Fire") ||
                   IsKeyDown(Keys.Space) ||
                   IsMouseButtonDown(MouseButton.Left) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Triggers.Right > 0.3f || _currentGamePad.Buttons.A == ButtonState.Pressed));
        }

        public bool IsFireJustPressed()
        {
            return IsActionJustPressed("Fire");
        }

        public bool IsBoostPressed()
        {
            return IsActionPressed("Boost") ||
                   IsKeyDown(Keys.LeftShift) ||
                   IsKeyDown(Keys.RightShift) ||
                   IsMouseButtonDown(MouseButton.Right) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Triggers.Left > 0.3f || _currentGamePad.Buttons.B == ButtonState.Pressed));
        }

        public bool IsBoostJustPressed()
        {
            return IsActionJustPressed("Boost");
        }

        public bool IsEmpPressed()
        {
            return IsActionPressed("Emp") ||
                   IsKeyDown(Keys.E) ||
                   IsKeyDown(Keys.F) ||
                   IsKeyDown(Keys.C) ||
                   IsMouseButtonDown(MouseButton.Middle) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Buttons.X == ButtonState.Pressed || _currentGamePad.Buttons.Y == ButtonState.Pressed));
        }

        public bool IsEmpJustPressed()
        {
            return IsActionJustPressed("Emp");
        }

        public bool IsPausePressed()
        {
            return IsActionPressed("Pause") ||
                   IsKeyDown(Keys.Escape) ||
                   IsKeyDown(Keys.P) ||
                   (_currentGamePad.IsConnected && (_currentGamePad.Buttons.Start == ButtonState.Pressed || _currentGamePad.Buttons.Back == ButtonState.Pressed));
        }

        public bool IsPauseJustPressed()
        {
            return IsActionJustPressed("Pause");
        }

        #endregion

        #region Key & Mouse Query Methods

        public bool IsKeyDown(Keys key)
        {
            return _currentKeyboard.IsKeyDown(key);
        }

        public bool IsKeyUp(Keys key) => !IsKeyDown(key);

        public bool IsKeyJustPressed(Keys key)
        {
            bool wasDown = _previousKeyboard.IsKeyDown(key) || _previousKeys.Contains(key);
            bool isDown = IsKeyDown(key);
            return isDown && !wasDown;
        }

        public bool IsKeyJustReleased(Keys key)
        {
            bool wasDown = _previousKeyboard.IsKeyDown(key) || _previousKeys.Contains(key);
            bool isDown = IsKeyDown(key);
            return wasDown && !isDown;
        }

        public bool IsMouseButtonDown(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => _currentMouse.LeftButton == ButtonState.Pressed,
                MouseButton.Right => _currentMouse.RightButton == ButtonState.Pressed,
                MouseButton.Middle => _currentMouse.MiddleButton == ButtonState.Pressed,
                MouseButton.XButton1 => _currentMouse.XButton1 == ButtonState.Pressed,
                MouseButton.XButton2 => _currentMouse.XButton2 == ButtonState.Pressed,
                _ => false
            };
        }

        public bool IsMouseButtonJustPressed(MouseButton button)
        {
            bool wasDown = _previousMouseButtons.Contains(button);
            bool isDown = IsMouseButtonDown(button);
            return isDown && !wasDown;
        }

        public bool IsMouseButtonJustReleased(MouseButton button)
        {
            bool wasDown = _previousMouseButtons.Contains(button);
            bool isDown = IsMouseButtonDown(button);
            return wasDown && !isDown;
        }

        public bool IsButtonDown(Buttons button)
        {
            return _currentGamePad.IsConnected && _currentGamePad.IsButtonDown(button);
        }

        public bool IsButtonJustPressed(Buttons button)
        {
            return _currentGamePad.IsConnected && _currentGamePad.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);
        }

        public bool IsButtonJustReleased(Buttons button)
        {
            return _currentGamePad.IsConnected && _currentGamePad.IsButtonUp(button) && _previousGamePad.IsButtonDown(button);
        }

        #endregion

        #region Action Mapping API

        public void RegisterAction(string actionName, params Keys[] keys)
        {
            RegisterAction(actionName, keys, Array.Empty<Buttons>(), Array.Empty<MouseButton>());
        }

        public void RegisterAction(string actionName, Keys[] keys, Buttons[] buttons)
        {
            RegisterAction(actionName, keys, buttons, Array.Empty<MouseButton>());
        }

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

        public void BindKey(string actionName, Keys key)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }
            if (!binding.Keys.Contains(key)) binding.Keys.Add(key);
        }

        public void BindGamePadButton(string actionName, Buttons button)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }
            if (!binding.GamePadButtons.Contains(button)) binding.GamePadButtons.Add(button);
        }

        public void BindMouseButton(string actionName, MouseButton button)
        {
            if (!_actionBindings.TryGetValue(actionName, out var binding))
            {
                binding = new ActionBinding(actionName);
                _actionBindings[actionName] = binding;
            }
            if (!binding.MouseButtons.Contains(button)) binding.MouseButtons.Add(button);
        }

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
