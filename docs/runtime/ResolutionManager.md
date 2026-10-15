# `ResolutionManager` Manual da API

O `ResolutionManager` é a autoridade central de resolução virtual e compensação de aspect ratio no MonoGameMaker. Fixando a resolução de design de referência em **1920x1080 (16:9)**, ele projeta automaticamente matrizes de transformação de tela, calcula barras de compensação (*Letterbox* no topo/fundo ou *Pillarbox* nas laterais) e viabiliza a conversão bidirecional de coordenadas entre a janela física do sistema operacional e o mundo virtual do jogo.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Resolution
{
    public static class ResolutionManager
    {
        public const int VirtualWidth = 1920;
        public const int VirtualHeight = 1080;
        public const float VirtualAspectRatio = 1.7777778f;

        public static int ScreenWidth { get; }
        public static int ScreenHeight { get; }
        public static float ScaleX { get; }
        public static float ScaleY { get; }
        public static float Scale { get; }
        public static int OffsetX { get; }
        public static int OffsetY { get; }

        public static Matrix ScaleMatrix { get; }
        public static Matrix InvertScaleMatrix { get; }

        public static Viewport Viewport { get; }
        public static Viewport VirtualViewport { get; }
        public static Rectangle VirtualBounds { get; }
        public static Rectangle LetterboxBounds { get; }

        public static void SetResolution(int screenWidth, int screenHeight);
        public static void Update(int screenWidth, int screenHeight);
        public static void Update(GraphicsDevice graphicsDevice);

        public static float GetProportionalPlanetScale(int physicalWidth);
        public static float GetProportionalScale(int physicalWidth, int physicalHeight);

        public static Vector2 ScreenToVirtual(Vector2 screenPoint, bool clampToVirtualBounds = false);
        public static Point ScreenToVirtual(Point screenPoint, bool clampToVirtualBounds = false);
        public static Vector2 ScreenToVirtual(float screenX, float screenY, bool clampToVirtualBounds = false);
        public static Point ScreenToVirtual(int screenX, int screenY, bool clampToVirtualBounds = false);
        public static Vector2 ScreenToVirtualClamped(Vector2 screenPoint);
        public static Point ScreenToVirtualClamped(Point screenPoint);

        public static Vector2 VirtualToScreen(Vector2 virtualPoint);
        public static Point VirtualToScreen(Point virtualPoint);
        public static Vector2 VirtualToScreen(float virtualX, float virtualY);
        public static Point VirtualToScreen(int virtualX, int virtualY);

        public static Rectangle ScreenToVirtual(Rectangle screenRect);
        public static Rectangle VirtualToScreen(Rectangle virtualRect);

        public static bool IsInVirtualBounds(Vector2 virtualPoint);
        public static bool IsInVirtualBounds(Point virtualPoint);
        public static bool IsScreenPointInViewport(Vector2 screenPoint);
        public static bool IsScreenPointInViewport(Point screenPoint);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Desenho de Interface e Sprites com Matriz Global:** Ao abrir o `spriteBatch.Begin(...)`, passe `transformMatrix: ResolutionManager.ScaleMatrix`. Todas as posições de UI, textos e elementos desenhados em coordenadas de 0 a 1920 e 0 a 1080 serão automaticamente escalonados com nitidez e sem distorção.
2. **Conversão de Coordenadas de Mouse Físico:** Nunca utilize diretamente `Mouse.GetState().X` e `Y` na lógica de mira da nave do jogador. Em monitores 4K ou janelas redimensionadas, a coordenada física difere da lógica do jogo. Sempre converta usando `ResolutionManager.ScreenToVirtual(mousePos)`.
3. **Pillarbox e Letterbox Automáticos:** Em monitores Ultrawide (21:9) ou telas antigas (4:3), o `ResolutionManager` centraliza o viewport sem distorcer círculos ou sprites.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a atualização do viewport e o uso da matriz de escala no ciclo padrão de um jogo MonoGame:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime.Resolution;

namespace MyGame
{
    public class ResolutionSampleGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;
        private Texture2D _crosshairTexture = null!;

        public ResolutionSampleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            // Sincroniza o gerenciador com as dimensões iniciais da janela
            ResolutionManager.Update(GraphicsDevice);
            Window.ClientSizeChanged += (s, e) => ResolutionManager.Update(GraphicsDevice);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _crosshairTexture = new Texture2D(GraphicsDevice, 16, 16);
        }

        protected override void Update(GameTime gameTime)
        {
            // Captura posição física do mouse e projeta com precisão no espaço virtual 1080p
            MouseState mouseState = Mouse.GetState();
            Vector2 physicalMouse = new Vector2(mouseState.X, mouseState.Y);
            Vector2 virtualAim = ResolutionManager.ScreenToVirtual(physicalMouse, clampToVirtualBounds: true);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Limpa o fundo do letterbox em preto
            GraphicsDevice.Clear(Color.Black);

            // Desenha a cena aplicando a matriz de escala calculada
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                effect: null,
                transformMatrix: ResolutionManager.ScaleMatrix);

            // Mira desenhada no espaço 1080p
            Vector2 virtualCenter = new Vector2(1920f / 2f, 1080f / 2f);
            _spriteBatch.Draw(_crosshairTexture, virtualCenter, Color.Lime);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
```
