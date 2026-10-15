# `InputService` Manual da API

O `InputService` é o subsistema unificado e multiplataforma de captura e mapeamento de entradas para Teclado, Mouse e GamePad no MonoGameMaker. Construído com arquitetura 100% gerenciada e agnóstica de sistema operacional (expurgo total de chamadas nativas Win32 `user32.dll` / `kernel32.dll`), ele se integra diretamente ao `ResolutionManager` para fornecer coordenadas de mouse calibradas para o espaço virtual de referência (1920x1080), deadzone circular estrita para analógicos de joysticks e mapeamento dinâmico reconfigurável de ações (`ActionBinding`).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Core
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    public enum InputDeviceType
    {
        KeyboardMouse,
        GamePad
    }

    public class ActionBinding
    {
        public string Name { get; }
        public List<Keys> Keys { get; }
        public List<Buttons> GamePadButtons { get; }
        public List<MouseButton> MouseButtons { get; }
    }

    public class InputService
    {
        public static InputService Instance { get; }

        public Vector2 MoveVector { get; }
        public Vector2 MouseClientPos { get; }
        public Vector2 MouseVirtualPos { get; }
        public Vector2 MousePosition { get; }
        public Vector2 MouseWorldPos { get; set; }
        public int MouseWheelDelta { get; }

        public InputDeviceType ActiveDevice { get; set; }
        public PlayerIndex GamePadIndex { get; set; }
        public float ThumbstickDeadZone { get; set; }
        public float MouseMovementThreshold { get; set; }
        public bool IsGamePadConnected { get; }

        public void Update(GameTime? gameTime = null);

        // Registro e Mapeamento de Ações
        public void RegisterAction(string actionName, Keys[]? keys = null, Buttons[]? padButtons = null, MouseButton[]? mouseButtons = null);
        public void BindKey(string actionName, Keys key);
        public void BindGamePadButton(string actionName, Buttons button);
        public void BindMouseButton(string actionName, MouseButton button);

        // Consultas de Estado de Ação
        public bool IsActionPressed(string actionName);
        public bool IsActionJustPressed(string actionName);
        public bool IsActionJustReleased(string actionName);

        // Ações Canônicas de Combate
        public bool IsFirePressed();
        public bool IsFireJustPressed();
        public bool IsBoostPressed();
        public bool IsBoostJustPressed();
        public bool IsEmpPressed();
        public bool IsEmpJustPressed();
        public bool IsPausePressed();
        public bool IsPauseJustPressed();
        public bool IsConfirmPressed();
        public bool IsConfirmJustPressed();
        public bool IsCancelPressed();
        public bool IsCancelJustReleased();

        // Consultas Diretas de Hardware
        public bool IsKeyDown(Keys key);
        public bool IsKeyJustPressed(Keys key);
        public bool IsKeyJustReleased(Keys key);
        public bool IsMouseButtonDown(MouseButton button);
        public bool IsMouseButtonJustPressed(MouseButton button);
        public bool IsMouseButtonJustReleased(MouseButton button);
        public bool IsGamePadButtonDown(Buttons button);
        public bool IsGamePadButtonJustPressed(Buttons button);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Movimentação Newtoniana Normalizada:** Utilize `InputService.Instance.MoveVector` para alimentar o vetor de aceleração da nave do jogador. O vetor retornado já possui magnitude limitada no intervalo `[0.0, 1.0]`, deadzone analógica circular aplicada e tratamento para impedir acelerações excessivas em diagonais (`WASD`).
2. **Mira Calibrada em Resoluções Heterogêneas:** Ao calcular o ângulo de rotação da torreta ou disparo do jogador, utilize `InputService.Instance.MouseVirtualPos` em vez de consultar `Mouse.GetState()`. Em monitores 4K ou proporções Ultrawide/4:3 com Letterbox, `MouseVirtualPos` projeta a mira exatamente sobre o plano de design 1920x1080.
3. **Mapeamento de Ações Reconfigurável:** Permita ao usuário personalizar comandos chamando `RegisterAction`, `BindKey` ou `BindGamePadButton`. A lógica de gameplay consulta apenas `IsActionPressed("Fire")`, mantendo-se agnóstica quanto ao periférico ativo.
4. **Ambientes Headless e Servidores CI/CD:** O `InputService` executa perfeitamente em modo sem janela e sem monitor (Linux Headless), sem disparar exceções de ausência de contexto de dispositivo.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra o consumo de ações e vetor de movimento do `InputService` dentro do método `Update` de uma entidade controlável:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.Runtime.Resolution;

namespace MyGame
{
    public class PlayerShipController
    {
        private Vector2 _position = new Vector2(960f, 540f);
        private Vector2 _velocity = Vector2.Zero;
        private float _rotation = 0f;
        private const float Speed = 500f;

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var input = InputService.Instance;

            // 1. Atualização obrigatória de entrada no início do frame
            input.Update(gameTime);

            // 2. Leitura de movimento normalizado (Keyboard WASD / GamePad Stick)
            Vector2 moveDir = input.MoveVector;
            _velocity = moveDir * Speed;
            _position += _velocity * dt;

            // 3. Orientação da nave em direção ao cursor virtual calibrado (1080p)
            Vector2 targetAim = input.MouseVirtualPos;
            Vector2 aimDelta = targetAim - _position;
            if (aimDelta.LengthSquared() > 0.001f)
            {
                _rotation = MathF.Atan2(aimDelta.Y, aimDelta.X);
            }

            // 4. Disparo com suporte a Teclado, Mouse e GamePad
            if (input.IsFireJustPressed())
            {
                SpawnProjectile(_position, _rotation);
            }

            // 5. Acionamento de Habilidade Especial (EMP)
            if (input.IsEmpJustPressed())
            {
                TriggerEmpBlast(_position);
            }
        }

        private void SpawnProjectile(Vector2 pos, float angle)
        {
            // Instanciação e disparo do projétil
        }

        private void TriggerEmpBlast(Vector2 pos)
        {
            // Onda de choque eletromagnética
        }
    }
}
```
