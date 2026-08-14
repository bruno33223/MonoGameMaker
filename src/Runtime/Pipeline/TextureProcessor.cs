using System;
using System.Buffers;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Pipeline
{
    /// <summary>
    /// Background color key type for procedural background removal.
    /// </summary>
    public enum BgType
    {
        /// <summary>
        /// Pure or near black background (RGB: 0, 0, 0).
        /// </summary>
        Black = 0,

        /// <summary>
        /// Pure or near white background (RGB: 255, 255, 255).
        /// </summary>
        White = 1,

        /// <summary>
        /// Custom color key (e.g. Chroma green, Magenta).
        /// </summary>
        ColorKey = 2,

        /// <summary>
        /// Automatically detects background color using 4-corner sampling (supports dark space, white backgrounds, and custom color keys).
        /// </summary>
        Adaptive = 3,

        /// <summary>
        /// Alias for <see cref="Adaptive"/> automatic background detection.
        /// </summary>
        Auto = 3
    }

    /// <summary>
    /// Static utility for runtime procedural texture processing, including 4-corner adaptive background removal,
    /// normal map generation using Sobel filtering, emissive map extraction, and automated PBR material synthesis.
    /// Features Zero-GC buffer pooling and pure CPU headless processing capabilities.
    /// </summary>
    public static class TextureProcessor
    {
        #region 4-Corner Background Detection

        /// <summary>
        /// Automatically detects the dominant background color by sampling the 4 image corners
        /// (Top-Left, Top-Right, Bottom-Left, Bottom-Right) with outlier filtering.
        /// Identifies dark space (black/near-black), high-key white backgrounds, or vibrant custom color keys (e.g. chroma green/magenta).
        /// </summary>
        /// <param name="source">Source pixel color buffer.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="sampleRadius">Radius in pixels around each corner to sample (defaults to 2).</param>
        /// <returns>The detected dominant background key color.</returns>
        public static Color DetectBackgroundColor(
            ReadOnlySpan<Color> source,
            int width,
            int height,
            int sampleRadius = 2)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

            sampleRadius = Math.Clamp(sampleRadius, 0, Math.Min(width / 2, height / 2));

            long totalR = 0;
            long totalG = 0;
            long totalB = 0;
            int count = 0;

            int maxX = width - 1;
            int maxY = height - 1;

            int leftMax = Math.Min(sampleRadius, maxX);
            int rightMin = Math.Max(0, maxX - sampleRadius);
            int topMax = Math.Min(sampleRadius, maxY);
            int bottomMin = Math.Max(0, maxY - sampleRadius);

            // 1. Top-Left Corner
            for (int y = 0; y <= topMax; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x <= leftMax; x++)
                {
                    Color c = source[rowOffset + x];
                    if (c.A > 0)
                    {
                        totalR += c.R;
                        totalG += c.G;
                        totalB += c.B;
                        count++;
                    }
                }
            }

            // 2. Top-Right Corner
            for (int y = 0; y <= topMax; y++)
            {
                int rowOffset = y * width;
                for (int x = rightMin; x <= maxX; x++)
                {
                    Color c = source[rowOffset + x];
                    if (c.A > 0)
                    {
                        totalR += c.R;
                        totalG += c.G;
                        totalB += c.B;
                        count++;
                    }
                }
            }

            // 3. Bottom-Left Corner
            for (int y = bottomMin; y <= maxY; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x <= leftMax; x++)
                {
                    Color c = source[rowOffset + x];
                    if (c.A > 0)
                    {
                        totalR += c.R;
                        totalG += c.G;
                        totalB += c.B;
                        count++;
                    }
                }
            }

            // 4. Bottom-Right Corner
            for (int y = bottomMin; y <= maxY; y++)
            {
                int rowOffset = y * width;
                for (int x = rightMin; x <= maxX; x++)
                {
                    Color c = source[rowOffset + x];
                    if (c.A > 0)
                    {
                        totalR += c.R;
                        totalG += c.G;
                        totalB += c.B;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                return Color.Black;
            }

            int avgR = (int)(totalR / count);
            int avgG = (int)(totalG / count);
            int avgB = (int)(totalB / count);

            // Check if dark space (RGB sum < 45 or max component < 25)
            if ((avgR + avgG + avgB) < 45 || (Math.Max(avgR, Math.Max(avgG, avgB)) < 25))
            {
                return Color.Black;
            }

            // Check if high-key white background (R > 230 && G > 230 && B > 230)
            if (avgR > 230 && avgG > 230 && avgB > 230)
            {
                return Color.White;
            }

            // Custom chroma / tinted color key
            return new Color(avgR, avgG, avgB, 255);
        }

        #endregion

        #region CPU Headless Buffer Processing Methods

        /// <summary>
        /// Processes raw pixel colors to key out a background color with smooth anti-aliased Hermite alpha edge feathering.
        /// Supports dark space (black), high-key white backgrounds, custom color keys, and automatic 4-corner adaptive detection.
        /// Pure CPU implementation suitable for headless execution and zero GC allocations.
        /// </summary>
        /// <param name="source">Source pixel color buffer.</param>
        /// <param name="destination">Destination pixel color buffer.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="type">The background type to key out (Black, White, ColorKey, or Adaptive/Auto).</param>
        /// <param name="threshold">Distance threshold in normalized color space [0.0 - 1.0].</param>
        /// <param name="customKey">Custom key color when type is ColorKey.</param>
        public static void ProcessRemoveBackground(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            BgType type,
            float threshold = 0.1f,
            Color? customKey = null)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            int totalPixels = width * height;
            if (source.Length < totalPixels) throw new ArgumentException("Source span is smaller than width * height.", nameof(source));
            if (destination.Length < totalPixels) throw new ArgumentException("Destination span is smaller than width * height.", nameof(destination));

            threshold = MathHelper.Clamp(threshold, 0f, 1f);

            Color keyColor = type switch
            {
                BgType.Black => Color.Black,
                BgType.White => Color.White,
                BgType.ColorKey => customKey ?? new Color(255, 0, 255, 255),
                BgType.Adaptive => DetectBackgroundColor(source, width, height),
                _ => Color.Black
            };

            float keyR = keyColor.R / 255f;
            float keyG = keyColor.G / 255f;
            float keyB = keyColor.B / 255f;

            // Hermite feather range for smooth anti-aliasing
            float feather = MathF.Max(0.02f, threshold * 0.5f);
            float invSqrt3 = 1.0f / 1.7320508f;

            for (int i = 0; i < totalPixels; i++)
            {
                Color c = source[i];
                if (c.A == 0)
                {
                    destination[i] = Color.Transparent;
                    continue;
                }

                float r = c.R / 255f;
                float g = c.G / 255f;
                float b = c.B / 255f;

                float dr = r - keyR;
                float dg = g - keyG;
                float db = b - keyB;

                float dist = MathF.Sqrt(dr * dr + dg * dg + db * db) * invSqrt3;

                float alphaFactor;
                if (dist <= threshold)
                {
                    alphaFactor = 0f;
                }
                else if (dist >= threshold + feather)
                {
                    alphaFactor = 1f;
                }
                else
                {
                    // Smooth Hermite cubic interpolation (smoothstep)
                    float t = (dist - threshold) / feather;
                    alphaFactor = t * t * (3.0f - 2.0f * t);
                }

                float finalAlpha = (c.A / 255f) * alphaFactor;
                byte aByte = (byte)Math.Clamp((int)(finalAlpha * 255f + 0.5f), 0, 255);

                if (aByte == 0)
                {
                    destination[i] = Color.Transparent;
                }
                else
                {
                    destination[i] = new Color(c.R, c.G, c.B, aByte);
                }
            }
        }

        /// <summary>
        /// Automatically detects background color using 4-corner analysis and removes it with Hermite alpha edge feathering.
        /// Supports dark space, white backgrounds, and custom color keys seamlessly.
        /// </summary>
        /// <param name="source">Source pixel color buffer.</param>
        /// <param name="destination">Destination pixel color buffer.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="threshold">Distance threshold in normalized color space [0.0 - 1.0].</param>
        /// <param name="feather">Feathering transition width in normalized color space [0.0 - 1.0].</param>
        public static void ProcessRemoveBackgroundAdaptive(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            float threshold = 0.1f,
            float feather = 0.05f)
        {
            Color detectedKey = DetectBackgroundColor(source, width, height);

            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            int totalPixels = width * height;
            if (source.Length < totalPixels) throw new ArgumentException("Source span is smaller than width * height.", nameof(source));
            if (destination.Length < totalPixels) throw new ArgumentException("Destination span is smaller than width * height.", nameof(destination));

            threshold = MathHelper.Clamp(threshold, 0f, 1f);
            feather = MathF.Max(0.01f, feather);

            float keyR = detectedKey.R / 255f;
            float keyG = detectedKey.G / 255f;
            float keyB = detectedKey.B / 255f;
            float invSqrt3 = 1.0f / 1.7320508f;

            for (int i = 0; i < totalPixels; i++)
            {
                Color c = source[i];
                if (c.A == 0)
                {
                    destination[i] = Color.Transparent;
                    continue;
                }

                float r = c.R / 255f;
                float g = c.G / 255f;
                float b = c.B / 255f;

                float dr = r - keyR;
                float dg = g - keyG;
                float db = b - keyB;

                float dist = MathF.Sqrt(dr * dr + dg * dg + db * db) * invSqrt3;

                float alphaFactor;
                if (dist <= threshold)
                {
                    alphaFactor = 0f;
                }
                else if (dist >= threshold + feather)
                {
                    alphaFactor = 1f;
                }
                else
                {
                    float t = (dist - threshold) / feather;
                    alphaFactor = t * t * (3.0f - 2.0f * t);
                }

                float finalAlpha = (c.A / 255f) * alphaFactor;
                byte aByte = (byte)Math.Clamp((int)(finalAlpha * 255f + 0.5f), 0, 255);

                if (aByte == 0)
                {
                    destination[i] = Color.Transparent;
                }
                else
                {
                    destination[i] = new Color(c.R, c.G, c.B, aByte);
                }
            }
        }

        /// <summary>
        /// Generates a 3D tangent-space normal map from raw pixel colors using a 3x3 Sobel filter on pixel luminance.
        /// Pure CPU implementation suitable for headless execution and zero GC allocations.
        /// </summary>
        /// <param name="source">Source pixel color buffer.</param>
        /// <param name="destination">Destination pixel color buffer for normal map.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="strength">Strength multiplier for surface gradient depth.</param>
        public static void ProcessNormalMap(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            float strength = 2.0f)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            int totalPixels = width * height;
            if (source.Length < totalPixels) throw new ArgumentException("Source span is smaller than width * height.", nameof(source));
            if (destination.Length < totalPixels) throw new ArgumentException("Destination span is smaller than width * height.", nameof(destination));

            if (float.IsNaN(strength) || float.IsInfinity(strength)) strength = 2.0f;

            // Rent pooled float buffer for Rec. 601 luminance
            float[] lumPool = ArrayPool<float>.Shared.Rent(totalPixels);
            try
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    Color c = source[i];
                    lumPool[i] = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
                }

                float GetLuminance(int x, int y)
                {
                    int cx = Math.Clamp(x, 0, width - 1);
                    int cy = Math.Clamp(y, 0, height - 1);
                    return lumPool[cy * width + cx];
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // 3x3 Sobel Neighborhood
                        float tl = GetLuminance(x - 1, y - 1);
                        float t  = GetLuminance(x,     y - 1);
                        float tr = GetLuminance(x + 1, y - 1);
                        float l  = GetLuminance(x - 1, y);
                        float r  = GetLuminance(x + 1, y);
                        float bl = GetLuminance(x - 1, y + 1);
                        float b  = GetLuminance(x,     y + 1);
                        float br = GetLuminance(x + 1, y + 1);

                        // Sobel gradients
                        float dx = (tr + 2.0f * r + br) - (tl + 2.0f * l + bl);
                        float dy = (bl + 2.0f * b + br) - (tl + 2.0f * t + tr);

                        // Tangent space normal vector: (-dx * strength, -dy * strength, 1.0f)
                        Vector3 normal = new Vector3(-dx * strength, -dy * strength, 1.0f);
                        normal = Vector3.Normalize(normal);

                        if (float.IsNaN(normal.X) || float.IsNaN(normal.Y) || float.IsNaN(normal.Z))
                        {
                            normal = Vector3.UnitZ;
                        }

                        // Encode [-1, 1] to [0, 255]
                        byte nr = (byte)Math.Clamp((int)(((normal.X * 0.5f) + 0.5f) * 255f + 0.5f), 0, 255);
                        byte ng = (byte)Math.Clamp((int)(((normal.Y * 0.5f) + 0.5f) * 255f + 0.5f), 0, 255);
                        byte nb = (byte)Math.Clamp((int)(((normal.Z * 0.5f) + 0.5f) * 255f + 0.5f), 0, 255);
                        byte na = source[y * width + x].A;

                        destination[y * width + x] = new Color(nr, ng, nb, na);
                    }
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(lumPool);
            }
        }

        /// <summary>
        /// Analyzes HSV saturation and brightness from raw pixel colors to isolate neon/intense emissive areas.
        /// Pure CPU implementation suitable for headless execution and zero GC allocations.
        /// </summary>
        /// <param name="source">Source pixel color buffer.</param>
        /// <param name="destination">Destination pixel color buffer for emissive mask.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="threshold">Minimum HSV brightness/value to consider as emissive [0.0 - 1.0].</param>
        /// <param name="saturationMin">Minimum HSV saturation required for colored emissive glow [0.0 - 1.0].</param>
        public static void ProcessEmissiveMap(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            float threshold = 0.65f,
            float saturationMin = 0.4f)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            int totalPixels = width * height;
            if (source.Length < totalPixels) throw new ArgumentException("Source span is smaller than width * height.", nameof(source));
            if (destination.Length < totalPixels) throw new ArgumentException("Destination span is smaller than width * height.", nameof(destination));

            threshold = MathHelper.Clamp(threshold, 0f, 1f);
            saturationMin = MathHelper.Clamp(saturationMin, 0f, 1f);

            for (int i = 0; i < totalPixels; i++)
            {
                Color c = source[i];
                if (c.A == 0)
                {
                    destination[i] = Color.Transparent;
                    continue;
                }

                float r = c.R / 255f;
                float g = c.G / 255f;
                float b = c.B / 255f;

                float max = MathF.Max(r, MathF.Max(g, b));
                float min = MathF.Min(r, MathF.Min(g, b));
                float delta = max - min;

                float v = max;
                float s = (max > 0.0001f) ? (delta / max) : 0f;

                // High saturation and brightness (neon colors) or ultra-bright hot spots
                bool isEmissive = (v >= threshold && s >= saturationMin) || (v >= MathF.Max(0.92f, threshold + 0.1f));

                if (isEmissive)
                {
                    destination[i] = new Color(c.R, c.G, c.B, c.A);
                }
                else
                {
                    destination[i] = Color.Transparent;
                }
            }
        }

        #endregion

        #region Texture2D Wrapped Methods

        /// <summary>
        /// Removes background color from a source texture with smooth anti-aliased alpha edge falloff.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <param name="type">The background type to key out (Black, White, ColorKey, or Adaptive).</param>
        /// <param name="threshold">Distance threshold in normalized color space [0.0 - 1.0]. Pixels closer than this become fully transparent.</param>
        /// <param name="customKey">Custom key color when <paramref name="type"/> is <see cref="BgType.ColorKey"/>.</param>
        /// <returns>A newly created <see cref="Texture2D"/> with keyed background and smoothed edges.</returns>
        public static Texture2D RemoveBackground(Texture2D source, BgType type, float threshold = 0.1f, Color? customKey = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (source.Width <= 0 || source.Height <= 0)
                throw new ArgumentException("Source texture has invalid dimensions.", nameof(source));

            int width = source.Width;
            int height = source.Height;
            int totalPixels = width * height;

            Color[] srcPool = ArrayPool<Color>.Shared.Rent(totalPixels);
            Color[] dstPool = ArrayPool<Color>.Shared.Rent(totalPixels);

            try
            {
                source.GetData(srcPool, 0, totalPixels);
                ProcessRemoveBackground(srcPool.AsSpan(0, totalPixels), dstPool.AsSpan(0, totalPixels), width, height, type, threshold, customKey);

                Texture2D output = new Texture2D(source.GraphicsDevice, width, height);
                output.SetData(dstPool, 0, totalPixels);
                return output;
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(srcPool);
                ArrayPool<Color>.Shared.Return(dstPool);
            }
        }

        /// <summary>
        /// Automatically detects background color using 4-corner analysis and removes it with Hermite alpha edge feathering.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <param name="threshold">Distance threshold in normalized color space [0.0 - 1.0].</param>
        /// <param name="feather">Feathering transition width in normalized color space [0.0 - 1.0].</param>
        /// <returns>A newly created <see cref="Texture2D"/> with keyed background and feathered edges.</returns>
        public static Texture2D RemoveBackgroundAdaptive(Texture2D source, float threshold = 0.1f, float feather = 0.05f)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (source.Width <= 0 || source.Height <= 0)
                throw new ArgumentException("Source texture has invalid dimensions.", nameof(source));

            int width = source.Width;
            int height = source.Height;
            int totalPixels = width * height;

            Color[] srcPool = ArrayPool<Color>.Shared.Rent(totalPixels);
            Color[] dstPool = ArrayPool<Color>.Shared.Rent(totalPixels);

            try
            {
                source.GetData(srcPool, 0, totalPixels);
                ProcessRemoveBackgroundAdaptive(srcPool.AsSpan(0, totalPixels), dstPool.AsSpan(0, totalPixels), width, height, threshold, feather);

                Texture2D output = new Texture2D(source.GraphicsDevice, width, height);
                output.SetData(dstPool, 0, totalPixels);
                return output;
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(srcPool);
                ArrayPool<Color>.Shared.Return(dstPool);
            }
        }

        /// <summary>
        /// Generates a 3D tangent-space normal map from a source texture using a 3x3 Sobel filter on pixel luminance.
        /// </summary>
        /// <param name="source">The source texture (diffuse/height map).</param>
        /// <param name="strength">Strength multiplier for surface gradient depth.</param>
        /// <returns>A newly created <see cref="Texture2D"/> containing the encoded RGB tangent space normal map.</returns>
        public static Texture2D GenerateNormalMap(Texture2D source, float strength = 2.0f)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (source.Width <= 0 || source.Height <= 0)
                throw new ArgumentException("Source texture has invalid dimensions.", nameof(source));

            int width = source.Width;
            int height = source.Height;
            int totalPixels = width * height;

            Color[] srcPool = ArrayPool<Color>.Shared.Rent(totalPixels);
            Color[] dstPool = ArrayPool<Color>.Shared.Rent(totalPixels);

            try
            {
                source.GetData(srcPool, 0, totalPixels);
                ProcessNormalMap(srcPool.AsSpan(0, totalPixels), dstPool.AsSpan(0, totalPixels), width, height, strength);

                Texture2D normalMap = new Texture2D(source.GraphicsDevice, width, height);
                normalMap.SetData(dstPool, 0, totalPixels);
                return normalMap;
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(srcPool);
                ArrayPool<Color>.Shared.Return(dstPool);
            }
        }

        /// <summary>
        /// Analyzes HSV saturation and brightness to isolate neon/intense colors (lasers, engines, reactors) into an emissive mask texture.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <param name="threshold">Minimum HSV brightness/value to consider as emissive [0.0 - 1.0].</param>
        /// <param name="saturationMin">Minimum HSV saturation required for colored emissive glow [0.0 - 1.0].</param>
        /// <returns>A newly created <see cref="Texture2D"/> containing isolated emissive areas.</returns>
        public static Texture2D GenerateEmissiveMap(Texture2D source, float threshold = 0.65f, float saturationMin = 0.4f)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (source.Width <= 0 || source.Height <= 0)
                throw new ArgumentException("Source texture has invalid dimensions.", nameof(source));

            int width = source.Width;
            int height = source.Height;
            int totalPixels = width * height;

            Color[] srcPool = ArrayPool<Color>.Shared.Rent(totalPixels);
            Color[] dstPool = ArrayPool<Color>.Shared.Rent(totalPixels);

            try
            {
                source.GetData(srcPool, 0, totalPixels);
                ProcessEmissiveMap(srcPool.AsSpan(0, totalPixels), dstPool.AsSpan(0, totalPixels), width, height, threshold, saturationMin);

                Texture2D emissiveMap = new Texture2D(source.GraphicsDevice, width, height);
                emissiveMap.SetData(dstPool, 0, totalPixels);
                return emissiveMap;
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(srcPool);
                ArrayPool<Color>.Shared.Return(dstPool);
            }
        }

        /// <summary>
        /// Loads an image from disk, optionally removes its background, generates normal and emissive maps, and returns a fully initialized <see cref="PbrMaterial"/>.
        /// </summary>
        /// <param name="graphicsDevice">The active MonoGame GraphicsDevice.</param>
        /// <param name="filePath">Path to the image file on disk.</param>
        /// <param name="normalStrength">Strength multiplier for normal map generation.</param>
        /// <param name="emissiveThreshold">Brightness threshold for emissive map extraction.</param>
        /// <param name="bgType">Background color type if background removal is enabled.</param>
        /// <param name="removeBg">Whether to perform procedural background removal.</param>
        /// <returns>A new <see cref="PbrMaterial"/> ready for rendering.</returns>
        public static PbrMaterial CreatePbrMaterial(GraphicsDevice graphicsDevice, string filePath, float normalStrength = 2.0f, float emissiveThreshold = 0.65f, BgType bgType = BgType.Black, bool removeBg = false)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Source texture file not found: {filePath}", filePath);

            Texture2D baseTexture;
            using (var stream = File.OpenRead(filePath))
            {
                baseTexture = Texture2D.FromStream(graphicsDevice, stream);
            }

            return CreatePbrMaterial(baseTexture, normalStrength, emissiveThreshold, bgType, removeBg);
        }

        /// <summary>
        /// Procedurally generates normal and emissive maps for an existing texture and constructs a ready-to-use <see cref="PbrMaterial"/>.
        /// </summary>
        /// <param name="source">The source albedo texture.</param>
        /// <param name="normalStrength">Strength multiplier for normal map generation.</param>
        /// <param name="emissiveThreshold">Brightness threshold for emissive map extraction.</param>
        /// <param name="bgType">Background color type if background removal is enabled.</param>
        /// <param name="removeBg">Whether to perform procedural background removal.</param>
        /// <returns>A new <see cref="PbrMaterial"/> ready for rendering.</returns>
        public static PbrMaterial CreatePbrMaterial(Texture2D source, float normalStrength = 2.0f, float emissiveThreshold = 0.65f, BgType bgType = BgType.Black, bool removeBg = false)
        {
            ArgumentNullException.ThrowIfNull(source);

            Texture2D albedo = removeBg ? RemoveBackground(source, bgType) : source;
            Texture2D normalMap = GenerateNormalMap(albedo, normalStrength);
            Texture2D emissiveMap = GenerateEmissiveMap(albedo, emissiveThreshold);

            return new PbrMaterial
            {
                AlbedoMap = albedo,
                NormalMap = normalMap,
                EmissiveMap = emissiveMap,
                DiffuseColor = Vector4.One,
                EmissiveColor = Color.White,
                EmissiveIntensity = 1.0f,
                Roughness = 0.5f,
                Metallic = 0.0f,
                AlphaCutoff = 0.05f,
                OwnsTextures = true
            };
        }

        #endregion
    }
}
