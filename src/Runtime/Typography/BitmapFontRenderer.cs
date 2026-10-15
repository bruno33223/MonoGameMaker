using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Typography
{
    /// <summary>
    /// Renderizador de fonte matricial procedural 5x7 em C# puro via Texture2D 1x1.
    /// Oferece suporte completo ao bloco Latin-1 com caracteres acentuados em Português (PT-BR),
    /// sanitização e decomposição de caracteres estendidos, medição de texto, quebra de linha e efeito máquina de escrever.
    /// Funciona de forma resiliente sem depender de compiladores .xnb ou arquivos de assets externos.
    /// </summary>
    public class BitmapFontRenderer : IDisposable
    {
        private static readonly Dictionary<char, byte[]> _fontMap = new Dictionary<char, byte[]>();
        private static SpriteFont? _globalSpriteFont;
        private SpriteFont? _spriteFont;
        private Texture2D? _pixel;
        private bool _ownsPixel = false;
        private bool _isDisposed = false;

        static BitmapFontRenderer()
        {
            InitFont();
        }

        public BitmapFontRenderer(Texture2D? pixel = null, SpriteFont? font = null)
        {
            _pixel = pixel;
            SetFont(font);
        }

        public BitmapFontRenderer(GraphicsDevice graphicsDevice, SpriteFont? font = null)
        {
            if (graphicsDevice != null)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
                _ownsPixel = true;
            }
            SetFont(font);
        }

        /// <summary>
        /// Define a textura 1x1 usada para desenhar os pixels da fonte procedural.
        /// </summary>
        public void SetPixelTexture(Texture2D pixel)
        {
            if (_ownsPixel && _pixel != null && !_pixel.IsDisposed)
            {
                _pixel.Dispose();
                _ownsPixel = false;
            }
            _pixel = pixel;
        }

        /// <summary>
        /// Configura a SpriteFont de alta fidelidade usada preferencialmente, mantendo a matricial como fallback.
        /// </summary>
        public void SetFont(SpriteFont? font)
        {
            _spriteFont = font;
            _globalSpriteFont = font;
            if (_spriteFont != null)
            {
                try
                {
                    _spriteFont.DefaultCharacter = '?';
                }
                catch { }
            }
        }

        /// <summary>
        /// Sanitiza caracteres de aspas curvas, hífens tipográficos e decompõe diacríticos não suportados.
        /// </summary>
        public static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                switch (c)
                {
                    case '–': case '—': c = '-'; break;
                    case '“': case '”': case '«': case '»': c = '"'; break;
                    case '‘': case '’': c = '\''; break;
                    case '•': case '·': c = '*'; break;
                    case '…': sb.Append("..."); continue;
                }

                // Preserva ASCII (32-126) e Latin-1 suplementar com acentuação (160-255)
                if ((c >= 32 && c <= 126) || (c >= 160 && c <= 255))
                {
                    sb.Append(c);
                }
                else if (c == '\n' || c == '\r' || c == '\t')
                {
                    sb.Append(c);
                }
                else
                {
                    // Decompõe diacríticos estendidos (Europa Oriental / Turco / etc.)
                    char decomposed = DecomposeExtendedChar(c);
                    sb.Append(decomposed);
                }
            }
            return sb.ToString();
        }

        public static char DecomposeExtendedChar(char c) => c switch
        {
            'Ł' or 'ł' => 'L',
            'Ś' or 'ś' or 'Š' or 'š' or 'Ş' or 'ş' => 'S',
            'Ć' or 'ć' or 'Č' or 'č' => 'C',
            'Ź' or 'ź' or 'Ż' or 'ż' or 'Ž' or 'ž' => 'Z',
            'Ń' or 'ń' => 'N',
            'Ď' or 'ď' => 'D',
            'Ř' or 'ř' => 'R',
            'Ť' or 'ť' => 'T',
            'Ň' or 'ň' => 'N',
            'Ě' or 'ě' => 'E',
            'Ů' or 'ů' => 'U',
            'Ğ' or 'ğ' => 'G',
            'İ' or 'ı' => 'I',
            _ => '?'
        };

        /// <summary>
        /// Mede a largura em pixels de uma string na escala desejada.
        /// </summary>
        public static int MeasureText(string text, int scale = 1)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            if (_globalSpriteFont != null)
            {
                try
                {
                    string sanitized = SanitizeText(text);
                    float fScale = scale == 1 ? 0.72f : (scale == 2 ? 0.95f : (scale == 3 ? 1.25f : 1.5f));
                    return (int)(_globalSpriteFont.MeasureString(sanitized).X * fScale);
                }
                catch
                {
                }
            }
            return text.Length * 6 * Math.Max(1, scale);
        }

        /// <summary>
        /// Desenha texto com suporte preferencial a SpriteFont e fallback imediato para glifos procedurais 5x7.
        /// </summary>
        public void DrawText(SpriteBatch sb, string text, int x, int y, Color color, int scale = 1)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_spriteFont != null)
            {
                try
                {
                    string sanitized = SanitizeText(text);
                    float fScale = scale == 1 ? 0.72f : (scale == 2 ? 0.95f : (scale == 3 ? 1.25f : 1.5f));
                    sb.DrawString(_spriteFont, sanitized, new Vector2(x, y), color, 0f, Vector2.Zero, fScale, SpriteEffects.None, 0f);
                    return;
                }
                catch
                {
                }
            }

            EnsurePixelTexture(sb.GraphicsDevice);
            if (_pixel == null) return;

            string upperText = text.ToUpperInvariant();
            int curX = x;
            int startY = y;
            int step = 6 * Math.Max(1, scale);

            for (int i = 0; i < upperText.Length; i++)
            {
                char c = upperText[i];
                if (c == '\n')
                {
                    startY += 10 * Math.Max(1, scale);
                    curX = x;
                    continue;
                }
                DrawGlyph(sb, c, curX, startY, color, Math.Max(1, scale));
                curX += step;
            }
        }

        public void DrawGlyph(SpriteBatch sb, char c, int x, int y, Color color, int scale = 1)
        {
            EnsurePixelTexture(sb.GraphicsDevice);
            if (_pixel == null) return;

            char glyphChar = char.ToUpperInvariant(c);

            if (!_fontMap.TryGetValue(glyphChar, out var glyph))
            {
                glyphChar = glyphChar switch
                {
                    'Ã' or 'Á' or 'À' or 'Â' or 'Ä' or 'Å' or 'Æ' => 'A',
                    'É' or 'È' or 'Ê' or 'Ë' => 'E',
                    'Í' or 'Ì' or 'Î' or 'Ï' => 'I',
                    'Õ' or 'Ó' or 'Ò' or 'Ô' or 'Ö' or 'Ø' or 'Œ' => 'O',
                    'Ú' or 'Ù' or 'Û' or 'Ü' => 'U',
                    'Ý' or 'Ÿ' => 'Y',
                    'Ç' => 'C',
                    'Ñ' => 'N',
                    'ß' => 'B',
                    '–' or '—' => '-',
                    '“' or '”' or '«' or '»' => '"',
                    '‘' or '’' or '`' => '\'',
                    '•' or '·' => '*',
                    '¿' => '?',
                    '¡' => '!',
                    '°' => 'O',
                    '±' => '+',
                    _ => DecomposeExtendedChar(glyphChar)
                };

                if (!_fontMap.TryGetValue(glyphChar, out glyph))
                {
                    _fontMap.TryGetValue('?', out glyph);
                }
            }

            if (glyph != null)
            {
                for (int row = 0; row < 7; row++)
                {
                    byte b = glyph[row];
                    if (b == 0) continue;

                    int col = 0;
                    while (col < 5)
                    {
                        if ((b & (1 << (4 - col))) != 0)
                        {
                            int startCol = col;
                            while (col < 5 && (b & (1 << (4 - col))) != 0)
                            {
                                col++;
                            }
                            int span = col - startCol;
                            sb.Draw(_pixel, new Rectangle(x + startCol * scale, y + row * scale, span * scale, scale), color);
                        }
                        else
                        {
                            col++;
                        }
                    }
                }
            }
        }

        public void DrawTextWrapped(
            SpriteBatch sb,
            string text,
            int x,
            int y,
            int maxLineWidth,
            Color color,
            int scale = 1,
            int lineSpacing = 13)
        {
            if (string.IsNullOrEmpty(text)) return;

            string[] words = text.Split(' ');
            string currentLine = "";
            int curY = y;

            for (int i = 0; i < words.Length; i++)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? words[i] : $"{currentLine} {words[i]}";
                int testWidth = MeasureText(testLine, scale);

                if (testWidth > maxLineWidth && !string.IsNullOrEmpty(currentLine))
                {
                    DrawText(sb, currentLine, x, curY, color, scale);
                    curY += lineSpacing;
                    currentLine = words[i];
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                DrawText(sb, currentLine, x, curY, color, scale);
            }
        }

        public bool DrawTypewriterTextWrapped(
            SpriteBatch sb,
            string text,
            int x,
            int y,
            int maxLineWidth,
            Color color,
            int visibleCharCount,
            int scale = 1,
            int lineSpacing = 13,
            bool showCursor = true,
            float animTimer = 0f)
        {
            if (string.IsNullOrEmpty(text) || visibleCharCount <= 0)
            {
                if (showCursor && (int)(animTimer * 4f) % 2 == 0)
                {
                    DrawGlyph(sb, '_', x, y, color, scale);
                }
                return false;
            }

            int totalLen = text.Length;
            int limit = Math.Min(totalLen, visibleCharCount);
            int charW = 6 * Math.Max(1, scale);
            int curX = x;
            int curY = y;

            int wordStart = 0;
            while (wordStart < totalLen)
            {
                if (curX == x && text[wordStart] == ' ')
                {
                    wordStart++;
                    continue;
                }

                if (text[wordStart] == '\n')
                {
                    curY += lineSpacing;
                    curX = x;
                    wordStart++;
                    continue;
                }

                int wordEnd = wordStart;
                while (wordEnd < totalLen && text[wordEnd] != ' ' && text[wordEnd] != '\n')
                {
                    wordEnd++;
                }

                int wordLen = wordEnd - wordStart;
                int wordPixelWidth = wordLen * charW;

                if (curX > x && (curX + wordPixelWidth - x) > maxLineWidth)
                {
                    curY += lineSpacing;
                    curX = x;
                }

                int drawLimit = Math.Min(wordEnd, limit);
                for (int i = wordStart; i < drawLimit; i++)
                {
                    DrawGlyph(sb, text[i], curX, curY, color, scale);
                    curX += charW;
                }

                if (wordEnd >= limit)
                {
                    break;
                }

                if (wordEnd < totalLen && text[wordEnd] == ' ')
                {
                    if (wordEnd < limit)
                    {
                        curX += charW;
                    }
                    wordStart = wordEnd + 1;
                }
                else if (wordEnd < totalLen && text[wordEnd] == '\n')
                {
                    curY += lineSpacing;
                    curX = x;
                    wordStart = wordEnd + 1;
                }
                else
                {
                    wordStart = wordEnd;
                }
            }

            bool isDone = visibleCharCount >= totalLen;

            if (!isDone && showCursor && (int)(animTimer * 4f) % 2 == 0)
            {
                DrawGlyph(sb, '_', curX, curY, color, scale);
            }

            return isDone;
        }

        private void EnsurePixelTexture(GraphicsDevice device)
        {
            if (_pixel == null || _pixel.IsDisposed)
            {
                if (device != null)
                {
                    _pixel = new Texture2D(device, 1, 1);
                    _pixel.SetData(new[] { Color.White });
                    _ownsPixel = true;
                }
            }
        }

        private static void InitFont()
        {
            _fontMap['A'] = new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };
            _fontMap['B'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E };
            _fontMap['C'] = new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E };
            _fontMap['D'] = new byte[] { 0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C };
            _fontMap['E'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F };
            _fontMap['F'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 };
            _fontMap['G'] = new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0E };
            _fontMap['H'] = new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };
            _fontMap['I'] = new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E };
            _fontMap['J'] = new byte[] { 0x07, 0x02, 0x02, 0x02, 0x12, 0x12, 0x0C };
            _fontMap['K'] = new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 };
            _fontMap['L'] = new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F };
            _fontMap['M'] = new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 };
            _fontMap['N'] = new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 };
            _fontMap['O'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
            _fontMap['P'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 };
            _fontMap['Q'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D };
            _fontMap['R'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 };
            _fontMap['S'] = new byte[] { 0x0E, 0x11, 0x10, 0x0E, 0x01, 0x11, 0x0E };
            _fontMap['T'] = new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 };
            _fontMap['U'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
            _fontMap['V'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 };
            _fontMap['W'] = new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 };
            _fontMap['X'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 };
            _fontMap['Y'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 };
            _fontMap['Z'] = new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F };

            _fontMap['0'] = new byte[] { 0x0E, 0x13, 0x15, 0x11, 0x11, 0x11, 0x0E };
            _fontMap['1'] = new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E };
            _fontMap['2'] = new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F };
            _fontMap['3'] = new byte[] { 0x1F, 0x01, 0x02, 0x0E, 0x01, 0x11, 0x0E };
            _fontMap['4'] = new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 };
            _fontMap['5'] = new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E };
            _fontMap['6'] = new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E };
            _fontMap['7'] = new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 };
            _fontMap['8'] = new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E };
            _fontMap['9'] = new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C };

            _fontMap[':'] = new byte[] { 0x00, 0x04, 0x00, 0x00, 0x04, 0x00, 0x00 };
            _fontMap['.'] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00 };
            _fontMap[','] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x08 };
            _fontMap['-'] = new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 };
            _fontMap['+'] = new byte[] { 0x00, 0x04, 0x04, 0x1F, 0x04, 0x04, 0x00 };
            _fontMap['/'] = new byte[] { 0x01, 0x02, 0x02, 0x04, 0x08, 0x08, 0x10 };
            _fontMap['*'] = new byte[] { 0x00, 0x15, 0x0E, 0x1F, 0x0E, 0x15, 0x00 };
            _fontMap['%'] = new byte[] { 0x19, 0x19, 0x02, 0x04, 0x08, 0x13, 0x13 };
            _fontMap['['] = new byte[] { 0x0E, 0x08, 0x08, 0x08, 0x08, 0x08, 0x0E };
            _fontMap[']'] = new byte[] { 0x0E, 0x02, 0x02, 0x02, 0x02, 0x02, 0x0E };
            _fontMap['{'] = new byte[] { 0x06, 0x04, 0x04, 0x08, 0x04, 0x04, 0x06 };
            _fontMap['}'] = new byte[] { 0x0C, 0x04, 0x04, 0x02, 0x04, 0x04, 0x0C };
            _fontMap['('] = new byte[] { 0x02, 0x04, 0x08, 0x08, 0x08, 0x04, 0x02 };
            _fontMap[')'] = new byte[] { 0x08, 0x04, 0x02, 0x02, 0x02, 0x04, 0x08 };
            _fontMap['<'] = new byte[] { 0x02, 0x04, 0x08, 0x10, 0x08, 0x04, 0x02 };
            _fontMap['>'] = new byte[] { 0x08, 0x04, 0x02, 0x01, 0x02, 0x04, 0x08 };
            _fontMap['!'] = new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04 };
            _fontMap['?'] = new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 };
            _fontMap['='] = new byte[] { 0x00, 0x1F, 0x00, 0x1F, 0x00, 0x00, 0x00 };
            _fontMap['_'] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F };
            _fontMap['\''] = new byte[] { 0x08, 0x08, 0x04, 0x00, 0x00, 0x00, 0x00 };
            _fontMap['"'] = new byte[] { 0x14, 0x14, 0x14, 0x00, 0x00, 0x00, 0x00 };
            _fontMap['&'] = new byte[] { 0x0C, 0x12, 0x14, 0x08, 0x15, 0x12, 0x0D };
            _fontMap['#'] = new byte[] { 0x0A, 0x0A, 0x1F, 0x0A, 0x1F, 0x0A, 0x0A };
            _fontMap['$'] = new byte[] { 0x04, 0x0F, 0x14, 0x0E, 0x05, 0x1E, 0x04 };
            _fontMap['|'] = new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 };
            _fontMap['\\'] = new byte[] { 0x10, 0x08, 0x08, 0x04, 0x02, 0x02, 0x01 };
            _fontMap[';'] = new byte[] { 0x00, 0x04, 0x00, 0x00, 0x04, 0x04, 0x08 };
            _fontMap['~'] = new byte[] { 0x00, 0x0D, 0x16, 0x00, 0x00, 0x00, 0x00 };
            _fontMap['^'] = new byte[] { 0x04, 0x0A, 0x11, 0x00, 0x00, 0x00, 0x00 };
            _fontMap['`'] = new byte[] { 0x08, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00 };
            _fontMap['@'] = new byte[] { 0x0E, 0x11, 0x17, 0x15, 0x17, 0x10, 0x0F };
            _fontMap[' '] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            // Glifos procedurais acentuados e internacionais
            _fontMap['Ç'] = new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x11, 0x0E, 0x04 };
            _fontMap['Ñ'] = new byte[] { 0x0E, 0x00, 0x11, 0x19, 0x15, 0x13, 0x11 };
            _fontMap['¿'] = new byte[] { 0x04, 0x00, 0x04, 0x08, 0x10, 0x11, 0x0E };
            _fontMap['¡'] = new byte[] { 0x04, 0x00, 0x04, 0x04, 0x04, 0x04, 0x04 };
            _fontMap['°'] = new byte[] { 0x06, 0x09, 0x06, 0x00, 0x00, 0x00, 0x00 };
            _fontMap['±'] = new byte[] { 0x04, 0x0E, 0x04, 0x00, 0x0E, 0x00, 0x00 };
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_ownsPixel && _pixel != null && !_pixel.IsDisposed)
                {
                    _pixel.Dispose();
                }
                _isDisposed = true;
            }
        }
    }
}
