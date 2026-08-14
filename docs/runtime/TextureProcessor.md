# `TextureProcessor`

The `TextureProcessor` static utility provides runtime procedural texture manipulation and PBR map generation in Mono GameMaker. It allows dynamic background keying with smooth anti-aliased edge falloff, automatic heightfield-to-normal map generation via a 3x3 Sobel luminance kernel, HSV-based neon emissive map extraction, and one-line PBR material construction from raw image assets.

---

## API Definition

```csharp
namespace MonoGameMaker.Runtime.Pipeline
{
    public enum BgType
    {
        Black,
        White,
        ColorKey
    }

    public static class TextureProcessor
    {
        // CPU Headless Buffer Processing Methods (Zero-GC)
        public static void ProcessRemoveBackground(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            BgType type,
            float threshold = 0.1f,
            Color? customKey = null);

        public static void ProcessNormalMap(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            float strength = 2.0f);

        public static void ProcessEmissiveMap(
            ReadOnlySpan<Color> source,
            Span<Color> destination,
            int width,
            int height,
            float threshold = 0.65f,
            float saturationMin = 0.4f);

        // Texture2D GPU Helpers
        public static Texture2D RemoveBackground(
            Texture2D source,
            BgType type,
            float threshold = 0.1f,
            Color? customKey = null);

        public static Texture2D GenerateNormalMap(
            Texture2D source,
            float strength = 2.0f);

        public static Texture2D GenerateEmissiveMap(
            Texture2D source,
            float threshold = 0.65f,
            float saturationMin = 0.4f);

        public static PbrMaterial CreatePbrMaterial(
            GraphicsDevice graphicsDevice,
            string filePath,
            float normalStrength = 2.0f,
            float emissiveThreshold = 0.65f,
            BgType bgType = BgType.Black,
            bool removeBg = false);

        public static PbrMaterial CreatePbrMaterial(
            Texture2D source,
            float normalStrength = 2.0f,
            float emissiveThreshold = 0.65f,
            BgType bgType = BgType.Black,
            bool removeBg = false);
    }
}
```

### Enumerations

- **`BgType`**:
  - `Black`: Keys out solid black backgrounds (`#000000`).
  - `White`: Keys out solid white backgrounds (`#FFFFFF`).
  - `ColorKey`: Keys out a custom color specified via `customKey` (e.g. Magenta/Green screen).

### Static Methods

1. **`RemoveBackground(source, type, threshold, customKey)`**
   - **Returns**: A new `Texture2D` where matching pixels are made transparent, with smooth Hermite edge falloff to avoid harsh pixel fringes.
   - **Parameters**:
     - `source`: The input texture.
     - `type`: Target background type.
     - `threshold`: Normalized color distance threshold `[0.0 - 1.0]`.
     - `customKey`: Optional custom key color when using `BgType.ColorKey`.

2. **`GenerateNormalMap(source, strength)`**
   - **Returns**: A tangent-space normal map texture (`Texture2D`) encoded into RGB `[0, 255]`.
   - **Algorithm**: Converts pixels into Rec.601 perceptual luminance and applies horizontal/vertical 3x3 Sobel gradient kernels. The resulting vector `(-dx * strength, -dy * strength, 1.0f).Normalized()` is packed into standard RGB normal map format.
   - **Parameters**:
     - `source`: Height or diffuse texture.
     - `strength`: Surface gradient depth multiplier (default `2.0f`).

3. **`GenerateEmissiveMap(source, threshold, saturationMin)`**
   - **Returns**: A masked texture containing only bright, highly-saturated neon colors (lasers, glowing glyphs, engines, plasma) or extreme specular highlights.
   - **Algorithm**: Computes HSV value ($V$) and saturation ($S$) per pixel. Pixels matching $(V \ge threshold \land S \ge saturationMin)$ or ultra-bright highlights $(V \ge 0.92)$ are isolated; all other pixels are set to transparent.
   - **Parameters**:
     - `threshold`: Minimum brightness/value cutoff (default `0.65f`).
     - `saturationMin`: Minimum color saturation (default `0.4f`).

4. **`CreatePbrMaterial(graphicsDevice, filePath, ...)`** / **`CreatePbrMaterial(source, ...)`**
   - **Returns**: A complete `PbrMaterial` instance containing configured Albedo, Normal, and Emissive maps.
   - **Parameters**:
     - `graphicsDevice`: MonoGame `GraphicsDevice` for texture allocation.
     - `filePath`: Absolute or relative file path to the source image.
     - `source`: Existing loaded `Texture2D`.
     - `normalStrength`: Strength of generated surface normals.
     - `emissiveThreshold`: Threshold for neon glow detection.
     - `bgType`: Background removal mode.
     - `removeBg`: If `true`, automatically removes background color before generating maps.

---

## AI Prompting & Use Cases

### When to Use
- **Dynamic Sprite Normal Mapping**: When generating realistic 2.5D surface lighting without requiring pre-baked normal maps from external 3D modeling tools.
- **Automated Sprite Clean-Up**: When importing user assets or AI-generated sprites with solid black or white backgrounds.
- **Neon / Weapon Glow Extraction**: When needing an instant emissive mask for laser blasts, energy shields, and Bloom post-processing.
- **Rapid Material Prototyping**: Generating full PBR materials with a single function call.

---

## Practical & Compilable Example

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;
using MonoGameMaker.Runtime.Pipeline;

namespace MyGame.Scripts
{
    public class LaserCannonBehavior : EntityBehavior
    {
        private PbrMaterial? _pbrMaterial;
        private Texture2D? _cleanedTexture;

        public override void Awake()
        {
            if (Entity.Texture != null)
            {
                // 1. Procedurally remove solid black background from sprite
                _cleanedTexture = TextureProcessor.RemoveBackground(
                    source: Entity.Texture,
                    type: BgType.Black,
                    threshold: 0.12f
                );

                // Assign cleaned texture back to entity
                Entity.Texture = _cleanedTexture;

                // 2. Generate a complete PBR material from the cleaned texture
                _pbrMaterial = TextureProcessor.CreatePbrMaterial(
                    source: _cleanedTexture,
                    normalStrength: 3.0f,          // Deep surface bevels
                    emissiveThreshold: 0.60f,       // Capture glowing laser diodes
                    bgType: BgType.Black,
                    removeBg: false
                );

                _pbrMaterial.Roughness = 0.4f;
                _pbrMaterial.Metallic = 0.9f;
                _pbrMaterial.EmissiveIntensity = 2.5f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Game logic...
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Material is ready for custom rendering or shader application
        }

        public override void Dispose()
        {
            _pbrMaterial?.Dispose();
            _pbrMaterial = null;
            _cleanedTexture?.Dispose();
            _cleanedTexture = null;
            base.Dispose();
        }
    }
}
```
