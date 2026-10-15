using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Typography
{
    /// <summary>
    /// Fachada de renderização tipográfica resiliente de alta fidelidade.
    /// Tenta carregar e utilizar SpriteFont do Content pipeline (arquivos .xnb); caso ausente ou inacessível,
    /// comuta automaticamente e transparentemente para o BitmapFontRenderer procedural (C# puro),
    /// garantindo que o jogo e a UI nunca crashem por falta de assets de fonte.
    /// </summary>
    public static class TextRenderer
    {
        private static ContentManager? _content;
        private static SpriteBatch? _spriteBatch;
        private static Texture2D? _fallbackPixel;
        private static BitmapFontRenderer? _bitmapRenderer;
        private static SpriteFont? _defaultFont;
        private static readonly Dictionary<string, SpriteFont> _loadedFonts = new Dictionary<string, SpriteFont>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Inicializa o subsistema com o ContentManager, SpriteBatch global e textura de fallback opcional.
        /// </summary>
        public static void Initialize(ContentManager content, SpriteBatch spriteBatch, Texture2D? fallbackPixel = null)
        {
            _content = content;
            _spriteBatch = spriteBatch;
            _fallbackPixel = fallbackPixel;

            if (_bitmapRenderer == null)
            {
                _bitmapRenderer = new BitmapFontRenderer(fallbackPixel, _defaultFont);
            }
            else if (fallbackPixel != null)
            {
                _bitmapRenderer.SetPixelTexture(fallbackPixel);
            }

            TryLoadDefaultFont();
            ScanCustomFonts();
        }

        private static void TryLoadDefaultFont()
        {
            if (_content == null) return;

            string[] candidatePaths = { "Fonts/default", "default", "Fonts/DefaultFont", "DefaultFont" };
            foreach (var path in candidatePaths)
            {
                try
                {
                    var font = _content.Load<SpriteFont>(path);
                    if (font != null)
                    {
                        _defaultFont = font;
                        _loadedFonts["default"] = font;
                        _bitmapRenderer?.SetFont(font);
                        break;
                    }
                }
                catch
                {
                    // Fallback gracioso contínuo
                }
            }
        }

        private static void ScanCustomFonts()
        {
            if (_content == null) return;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fontsDir = Path.Combine(baseDir, _content.RootDirectory, "Fonts");
                if (Directory.Exists(fontsDir))
                {
                    foreach (var file in Directory.GetFiles(fontsDir, "*.xnb"))
                    {
                        string fontName = Path.GetFileNameWithoutExtension(file);
                        if (fontName.Equals("default", StringComparison.OrdinalIgnoreCase)) continue;

                        try
                        {
                            var font = _content.Load<SpriteFont>("Fonts/" + fontName);
                            if (font != null)
                            {
                                _loadedFonts[fontName] = font;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Desenha uma cadeia de caracteres na posição de tela com a cor especificada.
        /// </summary>
        public static void Draw(string text, Vector2 position, Color color)
        {
            Draw(text, position, color, "default", 1);
        }

        /// <summary>
        /// Desenha uma cadeia de caracteres com nome de fonte específico.
        /// </summary>
        public static void Draw(string text, Vector2 position, Color color, string fontName)
        {
            Draw(text, position, color, fontName, 1);
        }

        /// <summary>
        /// Desenha uma cadeia de caracteres com escala configurável e seleção de fonte.
        /// </summary>
        public static void Draw(string text, Vector2 position, Color color, string fontName, int scale)
        {
            if (string.IsNullOrEmpty(text) || _spriteBatch == null) return;

            SpriteFont? fontToUse = null;
            if (!string.IsNullOrEmpty(fontName) && _loadedFonts.TryGetValue(fontName, out var foundFont))
            {
                fontToUse = foundFont;
            }
            else
            {
                fontToUse = _defaultFont;
            }

            if (fontToUse != null)
            {
                try
                {
                    string sanitized = BitmapFontRenderer.SanitizeText(text);
                    float fScale = scale == 1 ? 1.0f : scale;
                    _spriteBatch.DrawString(fontToUse, sanitized, position, color, 0f, Vector2.Zero, fScale, SpriteEffects.None, 0f);
                    return;
                }
                catch
                {
                    // Fallback transparente para BitmapFontRenderer
                }
            }

            EnsureBitmapRenderer(_spriteBatch.GraphicsDevice);
            _bitmapRenderer?.DrawText(_spriteBatch, text, (int)position.X, (int)position.Y, color, scale);
        }

        /// <summary>
        /// Desenha texto com quebra de linha automática.
        /// </summary>
        public static void DrawWrapped(string text, Vector2 position, int maxLineWidth, Color color, int scale = 1, int lineSpacing = 13)
        {
            if (string.IsNullOrEmpty(text) || _spriteBatch == null) return;

            EnsureBitmapRenderer(_spriteBatch.GraphicsDevice);
            _bitmapRenderer?.DrawTextWrapped(_spriteBatch, text, (int)position.X, (int)position.Y, maxLineWidth, color, scale, lineSpacing);
        }

        /// <summary>
        /// Mede a largura em pixels de uma string na escala informada.
        /// </summary>
        public static int MeasureText(string text, int scale = 1, string fontName = "default")
        {
            if (string.IsNullOrEmpty(text)) return 0;

            if (_loadedFonts.TryGetValue(fontName, out var font) && font != null)
            {
                try
                {
                    string sanitized = BitmapFontRenderer.SanitizeText(text);
                    return (int)(font.MeasureString(sanitized).X * scale);
                }
                catch
                {
                }
            }

            return BitmapFontRenderer.MeasureText(text, scale);
        }

        /// <summary>
        /// Limpa o cache de fontes e reinicializa os renderizadores.
        /// </summary>
        public static void ClearCache()
        {
            _loadedFonts.Clear();
            _defaultFont = null;
            if (_content != null && _spriteBatch != null)
            {
                Initialize(_content, _spriteBatch, _fallbackPixel);
            }
        }

        private static void EnsureBitmapRenderer(GraphicsDevice device)
        {
            if (_bitmapRenderer == null)
            {
                _bitmapRenderer = new BitmapFontRenderer(device, _defaultFont);
            }
        }
    }
}
