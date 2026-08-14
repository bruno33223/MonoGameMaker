using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Rendering
{
    /// <summary>
    /// High-performance RenderTarget2D post-processing pipeline manager with multi-pass Bloom
    /// downsampling, separable Gaussian blur, ACES Filmic Tone Mapping composite,
    /// modular CRT Scanlines overlay, and Radial Vignette edge darkening.
    /// </summary>
    public class PostProcessManager : IDisposable
    {
        /// <summary>
        /// Recommended deep dark space clear color (#030611).
        /// </summary>
        public static readonly Color DeepDarkClearColor = new Color(0x03, 0x06, 0x11, 0xFF);

        private GraphicsDevice? _graphicsDevice;
        private bool _isDisposed;

        // Main Render Targets
        private RenderTarget2D? _sceneTarget;
        private RenderTarget2D? _compositeTarget;

        // Multi-pass Bloom Mip Chain Render Targets
        private RenderTarget2D? _bloomTargetHalf;
        private RenderTarget2D? _blurTempHalf;
        private RenderTarget2D? _bloomTargetQuarter;
        private RenderTarget2D? _blurTempQuarter;
        private RenderTarget2D? _bloomTargetEighth;
        private RenderTarget2D? _blurTempEighth;
        private RenderTarget2D? _bloomCombinedTarget;

        // Post-Processing Procedural Overlay Textures
        private Texture2D? _scanlineTexture;
        private Texture2D? _vignetteTexture;

        /// <summary>
        /// Gets whether the post-processing pipeline is currently enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Luminance threshold above which pixels contribute to the bloom effect (default: 0.75).
        /// </summary>
        public float BloomThreshold { get; set; } = 0.75f;

        /// <summary>
        /// Multiplier for the intensity of the bloom glow (default: 1.2).
        /// </summary>
        public float BloomIntensity { get; set; } = 1.2f;

        /// <summary>
        /// Spread radius of the bloom blur filters (default: 1.0).
        /// </summary>
        public float BloomRadius { get; set; } = 1.0f;

        /// <summary>
        /// Exposure value passed to the ACES Filmic tone mapper (default: 1.0).
        /// </summary>
        public float Exposure { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets whether the modular CRT scanline overlay effect is enabled (default: true).
        /// </summary>
        public bool EnableScanlines { get; set; } = true;

        /// <summary>
        /// Opacity multiplier for the CRT scanlines overlay [0.0 - 1.0] (default: 0.18).
        /// </summary>
        public float ScanlineIntensity { get; set; } = 0.18f;

        /// <summary>
        /// Gets or sets whether the radial vignette overlay effect is enabled (default: true).
        /// </summary>
        public bool EnableVignette { get; set; } = true;

        /// <summary>
        /// Maximum darkness factor of the radial vignette edge [0.0 - 1.0] (default: 0.85).
        /// </summary>
        public float VignetteIntensity { get; set; } = 0.85f;

        /// <summary>
        /// The primary HDR scene render target captured before post-processing.
        /// </summary>
        public RenderTarget2D? SceneTarget => _sceneTarget;

        /// <summary>
        /// The final composite render target containing tone-mapped and bloomed results.
        /// </summary>
        public RenderTarget2D? CompositeTarget => _compositeTarget;

        /// <summary>
        /// The active procedural CRT scanlines texture.
        /// </summary>
        public Texture2D? ScanlineTexture => _scanlineTexture;

        /// <summary>
        /// The active procedural radial vignette texture.
        /// </summary>
        public Texture2D? VignetteTexture => _vignetteTexture;

        /// <summary>
        /// Width in pixels of the active post-processing pipeline.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height in pixels of the active post-processing pipeline.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Gets whether the post-process manager has allocated active render targets.
        /// </summary>
        public bool IsInitialized => _sceneTarget != null && !_sceneTarget.IsDisposed;

        /// <summary>
        /// Initializes or resizes the post-processing render targets and overlay textures.
        /// </summary>
        /// <param name="graphicsDevice">The active MonoGame GraphicsDevice.</param>
        /// <param name="width">Desired resolution width.</param>
        /// <param name="height">Desired resolution height.</param>
        public void Initialize(GraphicsDevice graphicsDevice, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            if (_graphicsDevice == graphicsDevice && Width == width && Height == height && IsInitialized)
            {
                return;
            }

            _graphicsDevice = graphicsDevice;
            Width = width;
            Height = height;

            ReleaseTargets();

            // Scene target with depth buffer for 2.5D / 3D rendering
            _sceneTarget = new RenderTarget2D(
                graphicsDevice,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8,
                0,
                RenderTargetUsage.PreserveContents);

            _compositeTarget = new RenderTarget2D(
                graphicsDevice,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);

            // Mip chain allocations (1/2, 1/4, 1/8)
            int halfW = Math.Max(1, width / 2);
            int halfH = Math.Max(1, height / 2);
            int quartW = Math.Max(1, width / 4);
            int quartH = Math.Max(1, height / 4);
            int eighthW = Math.Max(1, width / 8);
            int eighthH = Math.Max(1, height / 8);

            _bloomTargetHalf = new RenderTarget2D(graphicsDevice, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);
            _blurTempHalf = new RenderTarget2D(graphicsDevice, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);

            _bloomTargetQuarter = new RenderTarget2D(graphicsDevice, quartW, quartH, false, SurfaceFormat.Color, DepthFormat.None);
            _blurTempQuarter = new RenderTarget2D(graphicsDevice, quartW, quartH, false, SurfaceFormat.Color, DepthFormat.None);

            _bloomTargetEighth = new RenderTarget2D(graphicsDevice, eighthW, eighthH, false, SurfaceFormat.Color, DepthFormat.None);
            _blurTempEighth = new RenderTarget2D(graphicsDevice, eighthW, eighthH, false, SurfaceFormat.Color, DepthFormat.None);

            _bloomCombinedTarget = new RenderTarget2D(graphicsDevice, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);

            // Procedural CRT Scanlines and Radial Vignette textures
            _scanlineTexture = GenerateScanlineTexture(graphicsDevice, width, height, 1.0f);
            _vignetteTexture = GenerateVignetteTexture(graphicsDevice, width, height, 0.85f);
        }

        /// <summary>
        /// Binds the SceneTarget to receive subsequent draw calls from the game loop.
        /// </summary>
        /// <param name="graphicsDevice">The active MonoGame GraphicsDevice.</param>
        /// <param name="clearColor">Optional background color to clear the scene target with (defaults to DeepDarkClearColor).</param>
        public void BeginCapture(GraphicsDevice graphicsDevice, Color? clearColor = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            if (_sceneTarget == null || _sceneTarget.IsDisposed)
            {
                Initialize(graphicsDevice, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            }

            graphicsDevice.SetRenderTarget(_sceneTarget);
            graphicsDevice.Clear(clearColor ?? DeepDarkClearColor);
        }

        /// <summary>
        /// Restores the default backbuffer as the active render target.
        /// </summary>
        /// <param name="graphicsDevice">The active MonoGame GraphicsDevice.</param>
        public void EndCapture(GraphicsDevice graphicsDevice)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Executes the multi-pass Bloom downsample/blur pipeline and ACES Filmic Tone Mapping composite.
        /// If no compiled HLSL effect is provided, falls back to a software SpriteBatch bloom approximation.
        /// Also composites the CRT scanlines and radial vignette overlays into the composite target.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch used to render post-processing passes.</param>
        /// <param name="bloomEffect">Optional compiled BloomEffect HLSL shader.</param>
        public void ApplyBloom(SpriteBatch spriteBatch, Effect? bloomEffect = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(spriteBatch);

            if (_graphicsDevice == null || _sceneTarget == null || _compositeTarget == null)
            {
                return;
            }

            if (!Enabled)
            {
                // Bypass post-processing by doing a direct 1:1 blit
                _graphicsDevice.SetRenderTarget(_compositeTarget);
                _graphicsDevice.Clear(Color.Black);

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
                spriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, Width, Height), Color.White);
                spriteBatch.End();

                _graphicsDevice.SetRenderTarget(null);
                return;
            }

            if (bloomEffect != null)
            {
                ApplyBloomShader(spriteBatch, bloomEffect);
            }
            else
            {
                ApplyBloomFallback(spriteBatch);
            }
        }

        /// <summary>
        /// Executes shader-accelerated multi-pass Bloom and ACES tone mapping using BloomEffect.fx,
        /// and composites CRT scanlines and radial vignette overlays into the composite target.
        /// </summary>
        private void ApplyBloomShader(SpriteBatch spriteBatch, Effect bloomEffect)
        {
            if (_graphicsDevice == null || _sceneTarget == null || _compositeTarget == null ||
                _bloomTargetHalf == null || _blurTempHalf == null ||
                _bloomTargetQuarter == null || _blurTempQuarter == null ||
                _bloomTargetEighth == null || _blurTempEighth == null ||
                _bloomCombinedTarget == null)
            {
                return;
            }

            // Set Global Uniforms
            bloomEffect.Parameters["BloomThreshold"]?.SetValue(BloomThreshold);
            bloomEffect.Parameters["BloomIntensity"]?.SetValue(BloomIntensity);
            bloomEffect.Parameters["BloomRadius"]?.SetValue(BloomRadius);
            bloomEffect.Parameters["Exposure"]?.SetValue(Exposure);

            EffectTechnique? brightPassTechnique = bloomEffect.Techniques["BrightPassTechnique"] ?? bloomEffect.Techniques["BrightPass"] ?? bloomEffect.Techniques[0];
            EffectTechnique? blurHTechnique = bloomEffect.Techniques["BlurHorizontalTechnique"] ?? bloomEffect.Techniques["BlurH"] ?? brightPassTechnique;
            EffectTechnique? blurVTechnique = bloomEffect.Techniques["BlurVerticalTechnique"] ?? bloomEffect.Techniques["BlurV"] ?? brightPassTechnique;
            EffectTechnique? compositeTechnique = bloomEffect.Techniques["CompositeTechnique"] ?? bloomEffect.Techniques["Composite"] ?? brightPassTechnique;

            // -----------------------------------------------------------------
            // Pass 1: Bright Pass Filter (Extract luminance > threshold into 1/2)
            // -----------------------------------------------------------------
            _graphicsDevice.SetRenderTarget(_bloomTargetHalf);
            _graphicsDevice.Clear(Color.Transparent);

            bloomEffect.CurrentTechnique = brightPassTechnique;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, bloomEffect);
            spriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, _bloomTargetHalf.Width, _bloomTargetHalf.Height), Color.White);
            spriteBatch.End();

            // -----------------------------------------------------------------
            // Downsample: 1/2 -> 1/4 and 1/4 -> 1/8
            // -----------------------------------------------------------------
            _graphicsDevice.SetRenderTarget(_bloomTargetQuarter);
            _graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_bloomTargetHalf, new Rectangle(0, 0, _bloomTargetQuarter.Width, _bloomTargetQuarter.Height), Color.White);
            spriteBatch.End();

            _graphicsDevice.SetRenderTarget(_bloomTargetEighth);
            _graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_bloomTargetQuarter, new Rectangle(0, 0, _bloomTargetEighth.Width, _bloomTargetEighth.Height), Color.White);
            spriteBatch.End();

            // -----------------------------------------------------------------
            // Separable Gaussian Blur Passes
            // -----------------------------------------------------------------
            BlurTarget(spriteBatch, bloomEffect, blurHTechnique, blurVTechnique, _bloomTargetEighth, _blurTempEighth);
            BlurTarget(spriteBatch, bloomEffect, blurHTechnique, blurVTechnique, _bloomTargetQuarter, _blurTempQuarter);
            BlurTarget(spriteBatch, bloomEffect, blurHTechnique, blurVTechnique, _bloomTargetHalf, _blurTempHalf);

            // -----------------------------------------------------------------
            // Combine Bloom Mips (Additive blending into combined target)
            // -----------------------------------------------------------------
            _graphicsDevice.SetRenderTarget(_bloomCombinedTarget);
            _graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_bloomTargetHalf, new Rectangle(0, 0, _bloomCombinedTarget.Width, _bloomCombinedTarget.Height), Color.White * 0.5f);
            spriteBatch.Draw(_bloomTargetQuarter, new Rectangle(0, 0, _bloomCombinedTarget.Width, _bloomCombinedTarget.Height), Color.White * 0.35f);
            spriteBatch.Draw(_bloomTargetEighth, new Rectangle(0, 0, _bloomCombinedTarget.Width, _bloomCombinedTarget.Height), Color.White * 0.15f);
            spriteBatch.End();

            // -----------------------------------------------------------------
            // Pass 3: Composite + ACES Filmic Tone Mapping
            // -----------------------------------------------------------------
            _graphicsDevice.SetRenderTarget(_compositeTarget);
            _graphicsDevice.Clear(Color.Black);

            bloomEffect.Parameters["BloomTexture"]?.SetValue(_bloomCombinedTarget);
            bloomEffect.CurrentTechnique = compositeTechnique;

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, bloomEffect);
            spriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, Width, Height), Color.White);
            spriteBatch.End();

            // Composite CRT Scanlines and Radial Vignette overlays
            DrawOverlays(spriteBatch);

            _graphicsDevice.SetRenderTarget(null);
        }

        private static void BlurTarget(SpriteBatch spriteBatch, Effect bloomEffect, EffectTechnique blurH, EffectTechnique blurV, RenderTarget2D sourceTarget, RenderTarget2D tempTarget)
        {
            var device = sourceTarget.GraphicsDevice;

            // Horizontal Pass: source -> temp
            device.SetRenderTarget(tempTarget);
            device.Clear(Color.Transparent);
            bloomEffect.Parameters["TexelSize"]?.SetValue(new Vector2(1f / sourceTarget.Width, 1f / sourceTarget.Height));
            bloomEffect.CurrentTechnique = blurH;

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, bloomEffect);
            spriteBatch.Draw(sourceTarget, new Rectangle(0, 0, tempTarget.Width, tempTarget.Height), Color.White);
            spriteBatch.End();

            // Vertical Pass: temp -> source
            device.SetRenderTarget(sourceTarget);
            device.Clear(Color.Transparent);
            bloomEffect.CurrentTechnique = blurV;

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, bloomEffect);
            spriteBatch.Draw(tempTarget, new Rectangle(0, 0, sourceTarget.Width, sourceTarget.Height), Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Software SpriteBatch fallback when compiled shader is not available.
        /// Uses additive blending and multi-tier downsampling to approximate bloom,
        /// and composites CRT scanlines and radial vignette overlays.
        /// </summary>
        private void ApplyBloomFallback(SpriteBatch spriteBatch)
        {
            if (_graphicsDevice == null || _sceneTarget == null || _compositeTarget == null ||
                _bloomTargetHalf == null || _bloomTargetQuarter == null || _bloomTargetEighth == null || _bloomCombinedTarget == null)
            {
                return;
            }

            // Downsample chain
            _graphicsDevice.SetRenderTarget(_bloomTargetHalf);
            _graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, _bloomTargetHalf.Width, _bloomTargetHalf.Height), Color.White);
            spriteBatch.End();

            _graphicsDevice.SetRenderTarget(_bloomTargetQuarter);
            _graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_bloomTargetHalf, new Rectangle(0, 0, _bloomTargetQuarter.Width, _bloomTargetQuarter.Height), Color.White);
            spriteBatch.End();

            _graphicsDevice.SetRenderTarget(_bloomTargetEighth);
            _graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_bloomTargetQuarter, new Rectangle(0, 0, _bloomTargetEighth.Width, _bloomTargetEighth.Height), Color.White);
            spriteBatch.End();

            // Accumulate into combined bloom buffer
            _graphicsDevice.SetRenderTarget(_bloomCombinedTarget);
            _graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_bloomTargetHalf, new Rectangle(0, 0, _bloomCombinedTarget.Width, _bloomCombinedTarget.Height), Color.White * 0.5f);
            spriteBatch.Draw(_bloomTargetQuarter, new Rectangle(0, 0, _bloomCombinedTarget.Width, _bloomCombinedTarget.Height), Color.White * 0.35f);
            spriteBatch.Draw(_bloomTargetEighth, new Rectangle(0, 0, _bloomCombinedTarget.Width, _bloomCombinedTarget.Height), Color.White * 0.15f);
            spriteBatch.End();

            // Composite scene + bloom
            _graphicsDevice.SetRenderTarget(_compositeTarget);
            _graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, Width, Height), Color.White);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            float fallbackAlpha = MathHelper.Clamp(BloomIntensity * 0.6f * Exposure, 0.0f, 1.0f);
            spriteBatch.Draw(_bloomCombinedTarget, new Rectangle(0, 0, Width, Height), Color.White * fallbackAlpha);
            spriteBatch.End();

            // Composite CRT Scanlines and Radial Vignette overlays
            DrawOverlays(spriteBatch);

            _graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Composites the modular CRT scanlines and radial vignette overlays on top of the active composite target.
        /// </summary>
        private void DrawOverlays(SpriteBatch spriteBatch)
        {
            // 1. Modular CRT Scanlines with RGB Chromatic tint
            if (EnableScanlines && _scanlineTexture != null && !_scanlineTexture.IsDisposed && ScanlineIntensity > 0.001f)
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
                spriteBatch.Draw(_scanlineTexture, new Rectangle(0, 0, Width, Height), Color.White * MathHelper.Clamp(ScanlineIntensity, 0f, 1f));
                spriteBatch.End();
            }

            // 2. Radial Vignette (transparent center to dark edge gradient)
            if (EnableVignette && _vignetteTexture != null && !_vignetteTexture.IsDisposed && VignetteIntensity > 0.001f)
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
                float vignetteAlpha = MathHelper.Clamp(VignetteIntensity / 0.85f, 0f, 1.0f);
                spriteBatch.Draw(_vignetteTexture, new Rectangle(0, 0, Width, Height), Color.White * vignetteAlpha);
                spriteBatch.End();
            }
        }

        /// <summary>
        /// Draws the final post-processed composite target (or raw scene target) to the backbuffer.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch used to render the full-screen texture.</param>
        /// <param name="destinationRectangle">Optional destination bounds (defaults to full viewport).</param>
        public void DrawToScreen(SpriteBatch spriteBatch, Rectangle? destinationRectangle = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(spriteBatch);

            RenderTarget2D? targetToDraw = _compositeTarget ?? _sceneTarget;
            if (targetToDraw == null || targetToDraw.IsDisposed)
            {
                return;
            }

            Rectangle destRect = destinationRectangle ?? new Rectangle(0, 0, Width, Height);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(targetToDraw, destRect, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Completes the scene post-processing pipeline (bloom downsampling, separable blur, ACES tone mapping,
        /// CRT scanlines, and radial vignette) and automatically presents the composite result to the backbuffer/screen.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch used for rendering passes.</param>
        /// <param name="bloomEffect">Optional compiled BloomEffect HLSL shader.</param>
        /// <param name="destinationRectangle">Optional destination bounds on the backbuffer (defaults to full viewport).</param>
        public void EndSceneAndPresent(SpriteBatch spriteBatch, Effect? bloomEffect = null, Rectangle? destinationRectangle = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(spriteBatch);

            if (_graphicsDevice != null && _graphicsDevice.GetRenderTargets().Length > 0)
            {
                _graphicsDevice.SetRenderTarget(null);
            }

            ApplyBloom(spriteBatch, bloomEffect);
            DrawToScreen(spriteBatch, destinationRectangle);
        }

        #region Procedural Overlay Generation

        /// <summary>
        /// Procedurally generates a CRT scanline overlay texture featuring horizontal 4px lines with subtle RGB chromatic tinting.
        /// </summary>
        /// <param name="graphicsDevice">Active MonoGame GraphicsDevice.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="intensity">Scanline contrast intensity [0.0 - 1.0].</param>
        /// <returns>A new Texture2D containing CRT scanlines.</returns>
        public static Texture2D GenerateScanlineTexture(GraphicsDevice graphicsDevice, int width, int height, float intensity = 0.18f)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            Texture2D texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            Color[] data = new Color[width * height];

            int rAlpha = Math.Clamp((int)(intensity * 255f * 0.40f), 0, 255);
            int gAlpha = Math.Clamp((int)(intensity * 255f * 0.40f), 0, 255);
            int bAlpha = Math.Clamp((int)(intensity * 255f * 0.40f), 0, 255);
            int gapAlpha = Math.Clamp((int)(intensity * 255f * 0.90f), 0, 255);

            Color line0 = new Color(255, 220, 220, rAlpha);   // subtle red subpixel tint
            Color line1 = new Color(220, 255, 220, gAlpha);   // subtle green subpixel tint
            Color line2 = new Color(220, 220, 255, bAlpha);   // subtle blue subpixel tint
            Color line3 = new Color(0, 0, 0, gapAlpha);       // dark CRT scanline beam gap

            for (int y = 0; y < height; y++)
            {
                int mod = y % 4;
                Color rowColor = mod switch
                {
                    0 => line0,
                    1 => line1,
                    2 => line2,
                    _ => line3
                };

                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    data[rowOffset + x] = rowColor;
                }
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Procedurally generates a smooth Radial Vignette overlay texture (transparent center transitioning to 0.85 black edge gradient).
        /// </summary>
        /// <param name="graphicsDevice">Active MonoGame GraphicsDevice.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="maxEdgeDarkness">Maximum opacity at outer screen corners (default: 0.85f).</param>
        /// <returns>A new Texture2D containing the radial vignette gradient.</returns>
        public static Texture2D GenerateVignetteTexture(GraphicsDevice graphicsDevice, int width, int height, float maxEdgeDarkness = 0.85f)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            Texture2D texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            Color[] data = new Color[width * height];

            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            float invCenterX = centerX > 0.001f ? 1.0f / centerX : 1.0f;
            float invCenterY = centerY > 0.001f ? 1.0f / centerY : 1.0f;

            float innerRadius = 0.35f;
            float outerRadius = 1.25f;
            float radiusRange = outerRadius - innerRadius;

            for (int y = 0; y < height; y++)
            {
                float dy = (y - centerY) * invCenterY;
                float dy2 = dy * dy;
                int rowOffset = y * width;

                for (int x = 0; x < width; x++)
                {
                    float dx = (x - centerX) * invCenterX;
                    float dist = MathF.Sqrt(dx * dx + dy2);

                    float alphaFactor;
                    if (dist <= innerRadius)
                    {
                        alphaFactor = 0f;
                    }
                    else if (dist >= outerRadius)
                    {
                        alphaFactor = maxEdgeDarkness;
                    }
                    else
                    {
                        float t = (dist - innerRadius) / radiusRange;
                        // Smooth cubic Hermite interpolation
                        alphaFactor = t * t * (3.0f - 2.0f * t) * maxEdgeDarkness;
                    }

                    int a = Math.Clamp((int)(alphaFactor * 255f + 0.5f), 0, 255);
                    data[rowOffset + x] = new Color(0, 0, 0, a);
                }
            }

            texture.SetData(data);
            return texture;
        }

        #endregion

        private void ReleaseTargets()
        {
            _sceneTarget?.Dispose();
            _sceneTarget = null;

            _compositeTarget?.Dispose();
            _compositeTarget = null;

            _bloomTargetHalf?.Dispose();
            _bloomTargetHalf = null;

            _blurTempHalf?.Dispose();
            _blurTempHalf = null;

            _bloomTargetQuarter?.Dispose();
            _bloomTargetQuarter = null;

            _blurTempQuarter?.Dispose();
            _blurTempQuarter = null;

            _bloomTargetEighth?.Dispose();
            _bloomTargetEighth = null;

            _blurTempEighth?.Dispose();
            _blurTempEighth = null;

            _bloomCombinedTarget?.Dispose();
            _bloomCombinedTarget = null;

            _scanlineTexture?.Dispose();
            _scanlineTexture = null;

            _vignetteTexture?.Dispose();
            _vignetteTexture = null;
        }

        /// <summary>
        /// Releases all allocated RenderTarget2D textures, overlay textures, and native resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    ReleaseTargets();
                }

                _isDisposed = true;
            }
        }
    }
}
