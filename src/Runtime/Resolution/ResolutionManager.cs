using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Resolution
{
    /// <summary>
    /// Gerenciador central de resolução virtual e escalonamento de tela com preservação estrita de proporção (16:9).
    /// Mantém uma resolução virtual fixa de 1920x1080 como padrão de referência, calculando matrizes de escala,
    /// viewports com compensação de Letterbox/Pillarbox e conversões bidirecionais de coordenadas entre espaço físico e virtual.
    /// </summary>
    public static class ResolutionManager
    {
        public const int VirtualWidth = 1920;
        public const int VirtualHeight = 1080;
        public const float VirtualAspectRatio = (float)VirtualWidth / VirtualHeight; // 16:9 (~1.7777778)

        public static int ScreenWidth { get; private set; } = 1920;
        public static int ScreenHeight { get; private set; } = 1080;
        public static float ScaleX { get; private set; } = 1f;
        public static float ScaleY { get; private set; } = 1f;
        public static float Scale { get; private set; } = 1f;
        public static int OffsetX { get; private set; } = 0;
        public static int OffsetY { get; private set; } = 0;

        public static Matrix ScaleMatrix { get; private set; } = Matrix.Identity;
        public static Matrix InvertScaleMatrix { get; private set; } = Matrix.Identity;

        public static Viewport Viewport { get; private set; } = new Viewport(0, 0, 1920, 1080);
        public static Viewport VirtualViewport { get; private set; } = new Viewport(0, 0, 1920, 1080);

        public static Rectangle VirtualBounds { get; private set; } = new Rectangle(0, 0, 1920, 1080);
        public static Rectangle LetterboxBounds { get; private set; } = new Rectangle(0, 0, 1920, 1080);

        static ResolutionManager()
        {
            Update(1920, 1080);
        }

        /// <summary>
        /// Define diretamente a resolução física da janela e recalcula a escala virtual.
        /// </summary>
        public static void SetResolution(int screenWidth, int screenHeight) => Update(screenWidth, screenHeight);

        /// <summary>
        /// Atualiza o cálculo da escala virtual e letterbox/pillarbox com base na resolução física da janela/tela.
        /// </summary>
        public static void Update(int screenWidth, int screenHeight)
        {
            ScreenWidth = Math.Max(1, screenWidth);
            ScreenHeight = Math.Max(1, screenHeight);

            float targetAspect = VirtualAspectRatio;
            float currentAspect = (float)ScreenWidth / ScreenHeight;

            if (currentAspect >= targetAspect)
            {
                // Tela mais larga que 16:9 (Pillarbox: barras nas laterais esquerda e direita)
                Scale = (float)ScreenHeight / VirtualHeight;
                ScaleX = Scale;
                ScaleY = Scale;
                OffsetX = (int)MathF.Round((ScreenWidth - (VirtualWidth * Scale)) * 0.5f);
                OffsetY = 0;
            }
            else
            {
                // Tela mais alta que 16:9 (Letterbox: barras no topo e na base)
                Scale = (float)ScreenWidth / VirtualWidth;
                ScaleX = Scale;
                ScaleY = Scale;
                OffsetX = 0;
                OffsetY = (int)MathF.Round((ScreenHeight - (VirtualHeight * Scale)) * 0.5f);
            }

            int scaledW = (int)MathF.Round(VirtualWidth * Scale);
            int scaledH = (int)MathF.Round(VirtualHeight * Scale);

            Viewport = new Viewport(OffsetX, OffsetY, Math.Max(1, scaledW), Math.Max(1, scaledH));
            VirtualViewport = new Viewport(0, 0, VirtualWidth, VirtualHeight);
            VirtualBounds = new Rectangle(0, 0, VirtualWidth, VirtualHeight);
            LetterboxBounds = new Rectangle(OffsetX, OffsetY, Math.Max(1, scaledW), Math.Max(1, scaledH));

            ScaleMatrix = Matrix.CreateScale(Scale, Scale, 1f) * Matrix.CreateTranslation(OffsetX, OffsetY, 0f);
            InvertScaleMatrix = Matrix.Invert(ScaleMatrix);
        }

        /// <summary>
        /// Atualiza o cálculo da escala virtual a partir dos parâmetros de apresentação do GraphicsDevice.
        /// </summary>
        public static void Update(GraphicsDevice graphicsDevice)
        {
            if (graphicsDevice == null) return;
            Update(graphicsDevice.PresentationParameters.BackBufferWidth, graphicsDevice.PresentationParameters.BackBufferHeight);
        }

        /// <summary>
        /// Retorna a escala proporcional para elementos circulares ou corpos celestes
        /// com base na largura física da janela relativa à resolução de design 1920x1080.
        /// </summary>
        public static float GetProportionalPlanetScale(int physicalWidth)
        {
            if (physicalWidth <= 0) return 1.0f;
            return MathF.Min(1.0f, (float)physicalWidth / VirtualWidth);
        }

        /// <summary>
        /// Retorna a escala proporcional uniforme relativa à resolução de design 1920x1080.
        /// </summary>
        public static float GetProportionalScale(int physicalWidth, int physicalHeight)
        {
            if (physicalWidth <= 0 || physicalHeight <= 0) return 1.0f;
            float scaleX = (float)physicalWidth / VirtualWidth;
            float scaleY = (float)physicalHeight / VirtualHeight;
            return MathF.Min(scaleX, scaleY);
        }

        public static Vector2 ViewportOffset => new Vector2(Viewport.X, Viewport.Y);
        public static Point ViewportOffsetPoint => new Point(Viewport.X, Viewport.Y);

        /// <summary>
        /// Converte uma coordenada física de tela/pixel de mouse para o espaço virtual de 1920x1080.
        /// Desconta com exatidão Viewport.X e Viewport.Y (barras de letterbox/pillarbox) e divide pela escala uniforme.
        /// </summary>
        public static Vector2 ScreenToVirtual(Vector2 screenPoint, bool clampToVirtualBounds = false)
        {
            float scale = Scale > 0f ? Scale : 1f;
            float vx = (screenPoint.X - OffsetX) / scale;
            float vy = (screenPoint.Y - OffsetY) / scale;
            if (clampToVirtualBounds)
            {
                vx = MathHelper.Clamp(vx, 0f, VirtualWidth);
                vy = MathHelper.Clamp(vy, 0f, VirtualHeight);
            }
            return new Vector2(vx, vy);
        }

        public static Point ScreenToVirtual(Point screenPoint, bool clampToVirtualBounds = false)
        {
            float scale = Scale > 0f ? Scale : 1f;
            float vx = (screenPoint.X - OffsetX) / scale;
            float vy = (screenPoint.Y - OffsetY) / scale;
            if (clampToVirtualBounds)
            {
                vx = MathHelper.Clamp(vx, 0f, VirtualWidth);
                vy = MathHelper.Clamp(vy, 0f, VirtualHeight);
            }
            return new Point((int)MathF.Round(vx), (int)MathF.Round(vy));
        }

        public static Vector2 ScreenToVirtual(float screenX, float screenY, bool clampToVirtualBounds = false) =>
            ScreenToVirtual(new Vector2(screenX, screenY), clampToVirtualBounds);

        public static Point ScreenToVirtual(int screenX, int screenY, bool clampToVirtualBounds = false) =>
            ScreenToVirtual(new Point(screenX, screenY), clampToVirtualBounds);

        public static Vector2 ScreenToVirtualClamped(Vector2 screenPoint) => ScreenToVirtual(screenPoint, true);

        public static Point ScreenToVirtualClamped(Point screenPoint) => ScreenToVirtual(screenPoint, true);

        /// <summary>
        /// Converte uma coordenada virtual de 1920x1080 para a coordenada física da janela/tela.
        /// </summary>
        public static Vector2 VirtualToScreen(Vector2 virtualPoint)
        {
            float scale = Scale > 0f ? Scale : 1f;
            float sx = (virtualPoint.X * scale) + OffsetX;
            float sy = (virtualPoint.Y * scale) + OffsetY;
            return new Vector2(sx, sy);
        }

        public static Point VirtualToScreen(Point virtualPoint)
        {
            var s = VirtualToScreen(new Vector2(virtualPoint.X, virtualPoint.Y));
            return new Point((int)MathF.Round(s.X), (int)MathF.Round(s.Y));
        }

        public static Vector2 VirtualToScreen(float virtualX, float virtualY) => VirtualToScreen(new Vector2(virtualX, virtualY));

        public static Point VirtualToScreen(int virtualX, int virtualY) => VirtualToScreen(new Point(virtualX, virtualY));

        public static Rectangle ScreenToVirtual(Rectangle screenRect)
        {
            Vector2 min = ScreenToVirtual(new Vector2(screenRect.Left, screenRect.Top));
            Vector2 max = ScreenToVirtual(new Vector2(screenRect.Right, screenRect.Bottom));
            return new Rectangle((int)MathF.Round(min.X), (int)MathF.Round(min.Y), (int)MathF.Round(max.X - min.X), (int)MathF.Round(max.Y - min.Y));
        }

        public static Rectangle VirtualToScreen(Rectangle virtualRect)
        {
            Vector2 min = VirtualToScreen(new Vector2(virtualRect.Left, virtualRect.Top));
            Vector2 max = VirtualToScreen(new Vector2(virtualRect.Right, virtualRect.Bottom));
            return new Rectangle((int)MathF.Round(min.X), (int)MathF.Round(min.Y), (int)MathF.Round(max.X - min.X), (int)MathF.Round(max.Y - min.Y));
        }

        public static bool IsInVirtualBounds(Vector2 virtualPoint)
        {
            return virtualPoint.X >= 0f && virtualPoint.X <= VirtualWidth &&
                   virtualPoint.Y >= 0f && virtualPoint.Y <= VirtualHeight;
        }

        public static bool IsInVirtualBounds(Point virtualPoint)
        {
            return virtualPoint.X >= 0 && virtualPoint.X <= VirtualWidth &&
                   virtualPoint.Y >= 0 && virtualPoint.Y <= VirtualHeight;
        }

        public static bool IsScreenPointInViewport(Vector2 screenPoint)
        {
            return screenPoint.X >= Viewport.X && screenPoint.X <= Viewport.X + Viewport.Width &&
                   screenPoint.Y >= Viewport.Y && screenPoint.Y <= Viewport.Y + Viewport.Height;
        }

        public static bool IsScreenPointInViewport(Point screenPoint)
        {
            return screenPoint.X >= Viewport.X && screenPoint.X <= Viewport.X + Viewport.Width &&
                   screenPoint.Y >= Viewport.Y && screenPoint.Y <= Viewport.Y + Viewport.Height;
        }
    }
}
