# `PBR Surface Shader` (`PbrSurface.fx`)

The `PbrSurface.fx` HLSL shader provides a physically-based rendering (PBR) lighting pipeline for 2.5D and 3D scenes in MonoGame. It implements the standard microfacet specular model (Blinn-Phong / Schlick-Fresnel approximation) with tangent-space normal mapping, alpha cutoff testing, directional and point light attenuation, and unclamped HDR emissive surfaces.

---

## HLSL Parameter Signatures

```hlsl
// Transformation Matrices & Camera
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float3   CameraPosition;

// Lighting Properties
float3 LightDirection;          // Direction vector toward directional light
float4 LightColor;              // Directional light color & HDR intensity
float4 AmbientColor;            // Ambient irradiance color

float3 PointLightPosition;      // World-space point light coordinates
float4 PointLightColor;         // Point light RGB & intensity
float  PointLightRadius;        // Attenuation cutoff radius
float  PointLightIntensity;     // Point light multiplier

// Material Properties
float  Roughness;               // Surface microfacet roughness [0.04 - 1.0]
float  Metallic;                // Metallic factor [0.0 = dielectric, 1.0 = metal]
float  EmissiveIntensity;       // Multiplier for glow / bloom contribution
float4 EmissiveColor;           // Emissive tint color
float  AlphaCutoff;             // Discard threshold for alpha testing

// Textures & Samplers
Texture2D AlbedoTexture;        // Albedo / Diffuse base color
Texture2D NormalMapTexture;     // Tangent-space Normal Map (RGB [0,1] -> [-1,1])
Texture2D EmissiveTexture;      // Emissive / Self-illumination mask

// Vertex Shader
VertexShaderOutput MainVS(VertexShaderInput input);

// Pixel Shader
float4 MainPS(VertexShaderOutput input) : COLOR0;

// Techniques
technique PbrTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
```

---

## Mathematical Model & Features

1. **Alpha Testing (`clip`)**:
   `clip(albedo.a - AlphaCutoff);`
   Discards fragments below the cutoff threshold for pixel-perfect foliage, cutouts, and 2.5D sprites.

2. **Normal Mapping (TBN)**:
   Transposes tangent-space normal map vectors `[0, 1]` to `[-1, 1]` using the Gram-Schmidt orthogonalized Tangent-Bitangent-Normal (TBN) frame.

3. **Fresnel-Schlick Specular**:
   Calculates base reflectivity $F_0$:
   $$F_0 = \text{lerp}(0.04, \text{albedo.rgb}, \text{Metallic})$$
   $$F = F_0 + (1 - F_0) \cdot (1 - \max(H \cdot V, 0))^5$$

4. **Energy Conservation**:
   Calculates diffuse factor $k_D = (1 - F) \cdot (1 - \text{Metallic})$ so metallic materials exhibit colored specular reflections and zero dielectric diffuse scattering.

5. **Distance Attenuation**:
   Point light utilizes smooth windowed quadratic attenuation:
   $$\text{atten} = \text{saturate}\left(1 - \frac{\text{dist}}{\text{radius}}\right)^2 \cdot \text{Intensity}$$

---

## Practical C# Game Loop Example

Below is a complete, compilable C# script demonstrating how to configure and draw a mesh/quad using `PbrSurface.fx`:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class PbrModelRenderer : EntityBehavior
    {
        private Effect _pbrEffect = null!;
        private Texture2D _albedoTex = null!;
        private Texture2D _normalTex = null!;
        private Texture2D _emissiveTex = null!;
        private VertexBuffer _vertexBuffer = null!;
        private IndexBuffer _indexBuffer = null!;

        public override void Awake()
        {
            // Note: In runtime, effects and textures are loaded via ContentManager
            // Example configuration:
            // _pbrEffect = SceneManager.Content.Load<Effect>("Shaders/PbrSurface");
            // _albedoTex = SceneManager.Content.Load<Texture2D>("Textures/Wall_Albedo");
            // _normalTex = SceneManager.Content.Load<Texture2D>("Textures/Wall_Normal");
            // _emissiveTex = SceneManager.Content.Load<Texture2D>("Textures/Wall_Emissive");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_pbrEffect == null || _vertexBuffer == null)
            {
                return;
            }

            var graphicsDevice = spriteBatch.GraphicsDevice;

            // 1. Set Transformation Matrices
            Matrix world = Matrix.CreateTranslation(new Vector3(Entity.Position, 0f));
            Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 500), Vector3.Zero, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(60),
                graphicsDevice.Viewport.AspectRatio,
                1.0f,
                5000f);

            _pbrEffect.Parameters["World"]?.SetValue(world);
            _pbrEffect.Parameters["View"]?.SetValue(view);
            _pbrEffect.Parameters["Projection"]?.SetValue(projection);
            _pbrEffect.Parameters["WorldInverseTranspose"]?.SetValue(Matrix.Transpose(Matrix.Invert(world)));
            _pbrEffect.Parameters["CameraPosition"]?.SetValue(new Vector3(0, 0, 500));

            // 2. Set Lighting
            _pbrEffect.Parameters["LightDirection"]?.SetValue(Vector3.Normalize(new Vector3(-1, -1, -0.5f)));
            _pbrEffect.Parameters["LightColor"]?.SetValue(new Vector4(1.5f, 1.4f, 1.2f, 1.0f)); // HDR Sunlight
            _pbrEffect.Parameters["AmbientColor"]?.SetValue(new Vector4(0.1f, 0.12f, 0.15f, 1.0f));

            // Point Light (e.g. torch or explosion)
            _pbrEffect.Parameters["PointLightPosition"]?.SetValue(new Vector3(Entity.Position.X, Entity.Position.Y, 50f));
            _pbrEffect.Parameters["PointLightColor"]?.SetValue(new Vector4(1.0f, 0.6f, 0.2f, 1.0f));
            _pbrEffect.Parameters["PointLightRadius"]?.SetValue(300.0f);
            _pbrEffect.Parameters["PointLightIntensity"]?.SetValue(2.5f);

            // 3. Set Material Properties
            _pbrEffect.Parameters["Roughness"]?.SetValue(0.35f);
            _pbrEffect.Parameters["Metallic"]?.SetValue(0.1f);
            _pbrEffect.Parameters["EmissiveIntensity"]?.SetValue(3.0f); // High intensity triggers Bloom
            _pbrEffect.Parameters["EmissiveColor"]?.SetValue(Color.Cyan.ToVector4());
            _pbrEffect.Parameters["AlphaCutoff"]?.SetValue(0.5f);

            // 4. Set Textures
            _pbrEffect.Parameters["AlbedoTexture"]?.SetValue(_albedoTex);
            _pbrEffect.Parameters["NormalMapTexture"]?.SetValue(_normalTex);
            _pbrEffect.Parameters["EmissiveTexture"]?.SetValue(_emissiveTex);

            // 5. Draw Primitives
            graphicsDevice.SetVertexBuffer(_vertexBuffer);
            graphicsDevice.Indices = _indexBuffer;

            foreach (EffectPass pass in _pbrEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }
        }
    }
}
```

---

## AI Prompting & Context Guidelines

*   **When to Use**: Use `PbrSurface.fx` whenever lighting realism, metallic reflections, normal map depth, or HDR emissive lighting (for glow/bloom) is desired on 2.5D sprites, isometric geometry, or 3D meshes.
*   **Emissive and Bloom**: When pairing with `PostProcessManager`, set `EmissiveIntensity` > 1.0 to ensure the smooth-knee bright pass extracts the emissive channels into the bloom buffer.
