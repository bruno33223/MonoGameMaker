# `BitmapFontRenderer` Manual da API

O `BitmapFontRenderer` é o sistema de tipografia matricial procedural embutido em código C# puro do MonoGameMaker. Desenhando caracteres a partir de uma matriz de bits 5x7 através de uma textura 1x1 sem recorrer a arquivos compilados `.xnb`, ele oferece suporte nativo ao conjunto Latin-1 com acentuação da Língua Portuguesa (`á, é, í, ó, ú, ã, õ, ç, ê, ô`), medição de largura de texto, quebra automática de linha e animação de texto tipo máquina de escrever.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Typography
{
    public class BitmapFontRenderer : IDisposable
    {
        public BitmapFontRenderer(Texture2D? pixel = null, SpriteFont? font = null);
        public BitmapFontRenderer(GraphicsDevice graphicsDevice, SpriteFont? font = null);

        public void SetPixelTexture(Texture2D pixel);
        public void SetFont(SpriteFont? font);

        public static string SanitizeText(string input);
        public static char DecomposeExtendedChar(char c);
        public static int MeasureText(string text, int scale = 1);

        public void DrawText(SpriteBatch sb, string text, int x, int y, Color color, int scale = 1);
        public void DrawGlyph(SpriteBatch sb, char c, int x, int y, Color color, int scale = 1);
        public void DrawTextWrapped(SpriteBatch sb, string text, int x, int y, int maxLineWidth, Color color, int scale = 1, int lineSpacing = 13);
        public bool DrawTypewriterTextWrapped(SpriteBatch sb, string text, int x, int y, int maxLineWidth, Color color, int visibleCharCount, int scale = 1, int lineSpacing = 13, bool showCursor = true, float animTimer = 0f);

        public void Dispose();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Tipografia Resiliente sem Dependência de MGCB:** No MonoGame padrão, a ausência de uma fonte compilada pelo pipeline de conteúdo causa exceções de crash imediato (`ContentLoadException`). O `BitmapFontRenderer` garante que textos de depuração, menus e diálogos sempre renderizem perfeitamente.
2. **Suporte Completo a Português (PT-BR):** Trata caracteres especiais acentuados e cedilha (`ç, Ç, ã, õ`), mapeando-os para glifos procedurais dedicados ou realizando decomposição limpa em tempo de execução.
3. **Efeito Máquina de Escrever:** O método `DrawTypewriterTextWrapped` processa o avanço progressivo de caracteres com cursor piscante integrado para caixas de diálogo narrativas sci-fi.

---

## Exemplo Prático e Compilável

O exemplo a seguir ilustra a renderização de diálogos com quebra de linha em uma interface de jogo:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Typography;

namespace MyGame.UI
{
    public class DialogueBox
    {
        private readonly BitmapFontRenderer _fontRenderer;
        private int _visibleCharacters = 0;
        private float _typewriterTimer = 0f;
        private const string Message = "COMUNICADO DA FROTA: A estação de Neo-Sol detectou distorções no setor. Preparem as armas!";

        public DialogueBox(GraphicsDevice device)
        {
            _fontRenderer = new BitmapFontRenderer(device);
        }

        public void Update(float dt)
        {
            _typewriterTimer += dt;
            // Revela 30 caracteres por segundo
            _visibleCharacters = (int)(_typewriterTimer * 30f);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            // Desenha caixa de diálogo com quebra automática em 400 pixels de largura
            _fontRenderer.DrawTypewriterTextWrapped(
                spriteBatch,
                Message,
                x: 100,
                y: 500,
                maxLineWidth: 400,
                color: Color.Yellow,
                visibleCharCount: _visibleCharacters,
                scale: 2,
                lineSpacing: 20,
                showCursor: true,
                animTimer: _typewriterTimer);

            spriteBatch.End();
        }
    }
}
```
