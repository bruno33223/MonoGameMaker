# `PbrMaterial`

The `PbrMaterial` class encapsulates surface material attributes for Physically Based Rendering (PBR) workflows in Mono GameMaker. It unifies texture maps (Albedo, Tangent-Space Normal Map, Emissive Mask) and scalar/vector optical parameters (Roughness, Metallic, Emissive Intensity/Color, Alpha Cutoff) into a clean, reusable container with automatic shader parameter binding.

---

## API Definition

```csharp
namespace MonoGameMaker.Runtime.Pipeline
{
    public class PbrMaterial : IDisposable
    {
        public Texture2D? AlbedoMap { get; set; }
        public Texture2D? NormalMap { get; set; }
        public Texture2D? EmissiveMap { get; set; }
        public Vector4 DiffuseColor { get; set; } = Vector4.One;
        public Color EmissiveColor { get; set; } = Color.White;
        public float EmissiveIntensity { get; set; } = 1.0f;
        public float Roughness { get; set; } = 0.5f;
        public float Metallic { get; set; } = 0.0f;
        public float AlphaCutoff { get; set; } = 0.05f;
        public bool OwnsTextures { get; set; } = true;

        public void Apply(Effect? effect);
        public void Dispose();
    }
}
```

### Properties

- **`AlbedoMap`** (`Texture2D?`): The base color texture map representing diffuse reflectance.
- **`NormalMap`** (`Texture2D?`): Tangent-space normal map perturbing surface lighting normals for high microfacet detail.
- **`EmissiveMap`** (`Texture2D?`): Emissive texture mask isolating glowing elements (lasers, engines, neon glyphs).
- **`DiffuseColor`** (`Vector4`): Base tint applied to the albedo channel. Default is `(1, 1, 1, 1)`.
- **`EmissiveColor`** (`Color`): Color multiplier applied to the emissive channel. Default is `Color.White`.
- **`EmissiveIntensity`** (`float`): Scalar intensity multiplier for HDR glow and bloom generation. Default is `1.0f`.
- **`Roughness`** (`float`): Microfacet roughness parameter `[0.0 = mirror-smooth, 1.0 = completely matte]`. Default is `0.5f`.
- **`Metallic`** (`float`): Conductor/metalness parameter `[0.0 = dielectric/insulator, 1.0 = pure metal]`. Default is `0.0f`.
- **`AlphaCutoff`** (`float`): Cutoff threshold for pixel shader alpha discarding (e.g. foliage, cutouts). Default is `0.05f`.
- **`OwnsTextures`** (`bool`): Determines whether the material instance owns and disposes its assigned texture maps when `Dispose()` is invoked. Default is `true`.

### Methods

- **`void Apply(Effect? effect)`**: Binds all non-null textures and parameters (`AlbedoTexture`, `NormalMapTexture`, `EmissiveTexture`, `Roughness`, `Metallic`, `EmissiveIntensity`, `EmissiveColor`, `DiffuseColor`, `AlphaCutoff`) directly to the target shader effect.
- **`void Dispose()`**: Disposes managed texture resources if `OwnsTextures` is `true`.

---

## AI Prompting & Use Cases

### When to Use
- **2.5D & 3D Lighting**: Whenever rendering sprites or 3D meshes that require dynamic directional or point lighting with realistic surface response.
- **Glowing Elements**: When rendering sci-fi units, cybernetic armor, projectiles, or glowing magic effects that feed directly into post-processing Bloom passes.
- **Procedural Pipeline Integration**: Directly consumed from `TextureProcessor.CreatePbrMaterial()` to instantly convert raw 2D sprite textures into shaded materials.

### Why Use `PbrMaterial`
- Eliminates redundant, error-prone manual `effect.Parameters["Name"]?.SetValue(...)` calls across multiple draw routines.
- Manages GPU texture lifetimes deterministically with `IDisposable`.

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
    public class ShadedBossBehavior : EntityBehavior
    {
        private PbrMaterial? _material;
        private Effect? _pbrEffect;

        public override void Awake()
        {
            // Load shader from Content or Graphics pipeline
            // In a real project: _pbrEffect = Content.Load<Effect>("Shaders/PbrSurface");
            
            // Create a procedural PBR material for the boss sprite
            if (Entity.Texture != null)
            {
                _material = TextureProcessor.CreatePbrMaterial(
                    source: Entity.Texture,
                    normalStrength: 2.5f,
                    emissiveThreshold: 0.7f,
                    bgType: BgType.Black,
                    removeBg: false
                );

                // Configure boss material parameters
                _material.Metallic = 0.8f;        // Metallic armor
                _material.Roughness = 0.3f;       // Glossy finish
                _material.EmissiveIntensity = 2.0f; // Bright pulsating glow
                _material.EmissiveColor = Color.Red;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_material != null)
            {
                // Pulsate emissive glow dynamically over time
                float totalSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
                _material.EmissiveIntensity = 1.5f + MathF.Sin(totalSeconds * 4.0f) * 0.8f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_material != null && _pbrEffect != null)
            {
                // Apply all material maps and uniforms to the shader
                _material.Apply(_pbrEffect);

                // Render with custom effect pass
                foreach (EffectPass pass in _pbrEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    // Draw geometry / sprite with shader active
                }
            }
        }

        public override void Dispose()
        {
            _material?.Dispose();
            _material = null;
            base.Dispose();
        }
    }
}
```
