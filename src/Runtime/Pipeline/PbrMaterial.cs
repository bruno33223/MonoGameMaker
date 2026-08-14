using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Pipeline
{
    /// <summary>
    /// Represents a Physically Based Rendering (PBR) material definition
    /// holding albedo, normal, and emissive maps alongside metallic, roughness, and alpha test properties.
    /// </summary>
    public class PbrMaterial : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Gets or sets the base albedo/diffuse texture map.
        /// </summary>
        public Texture2D? AlbedoMap { get; set; }

        /// <summary>
        /// Gets or sets the tangent-space normal map texture.
        /// </summary>
        public Texture2D? NormalMap { get; set; }

        /// <summary>
        /// Gets or sets the emissive map texture highlighting glowing regions.
        /// </summary>
        public Texture2D? EmissiveMap { get; set; }

        /// <summary>
        /// Gets or sets the base diffuse color tint. Default is (1, 1, 1, 1).
        /// </summary>
        public Vector4 DiffuseColor { get; set; } = Vector4.One;

        /// <summary>
        /// Gets or sets the tint color for emissive glow. Default is Color.White.
        /// </summary>
        public Color EmissiveColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the scalar intensity multiplier for emissive glow. Default is 1.0f.
        /// </summary>
        public float EmissiveIntensity { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the surface roughness parameter [0.0 = smooth mirror, 1.0 = completely matte]. Default is 0.5f.
        /// </summary>
        public float Roughness { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets the surface metallic parameter [0.0 = dielectric, 1.0 = pure metal]. Default is 0.0f.
        /// </summary>
        public float Metallic { get; set; } = 0.0f;

        /// <summary>
        /// Gets or sets the alpha cutoff threshold for pixel discard. Default is 0.05f.
        /// </summary>
        public float AlphaCutoff { get; set; } = 0.05f;

        /// <summary>
        /// Gets or sets whether this material owns the lifecycle of its textures and should dispose them upon Dispose(). Default is true.
        /// </summary>
        public bool OwnsTextures { get; set; } = true;

        /// <summary>
        /// Applies the material parameters and textures to the target shader Effect.
        /// </summary>
        /// <param name="effect">The shader effect to configure.</param>
        public void Apply(Effect? effect)
        {
            if (effect == null) return;

            // Albedo texture bindings
            if (AlbedoMap != null)
            {
                SetParameter(effect, "AlbedoTexture", AlbedoMap);
                SetParameter(effect, "AlbedoMap", AlbedoMap);
                SetParameter(effect, "Texture", AlbedoMap);
                SetParameter(effect, "DiffuseTexture", AlbedoMap);
            }

            // Normal map bindings
            if (NormalMap != null)
            {
                SetParameter(effect, "NormalMapTexture", NormalMap);
                SetParameter(effect, "NormalMap", NormalMap);
                SetParameter(effect, "NormalTexture", NormalMap);
            }

            // Emissive map bindings
            if (EmissiveMap != null)
            {
                SetParameter(effect, "EmissiveTexture", EmissiveMap);
                SetParameter(effect, "EmissiveMap", EmissiveMap);
            }

            // Numeric & Vector parameters
            SetParameter(effect, "DiffuseColor", DiffuseColor);
            SetParameter(effect, "EmissiveColor", EmissiveColor.ToVector4());
            SetParameter(effect, "EmissiveIntensity", EmissiveIntensity);
            SetParameter(effect, "Roughness", Roughness);
            SetParameter(effect, "Metallic", Metallic);
            SetParameter(effect, "AlphaCutoff", AlphaCutoff);
        }

        private static void SetParameter(Effect effect, string paramName, Texture2D texture)
        {
            var p = effect.Parameters[paramName];
            if (p != null)
            {
                p.SetValue(texture);
            }
        }

        private static void SetParameter(Effect effect, string paramName, Vector4 value)
        {
            var p = effect.Parameters[paramName];
            if (p != null)
            {
                p.SetValue(value);
            }
        }

        private static void SetParameter(Effect effect, string paramName, float value)
        {
            var p = effect.Parameters[paramName];
            if (p != null)
            {
                p.SetValue(value);
            }
        }

        /// <summary>
        /// Disposes of any textures owned by this material.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && OwnsTextures)
                {
                    AlbedoMap?.Dispose();
                    AlbedoMap = null;

                    NormalMap?.Dispose();
                    NormalMap = null;

                    EmissiveMap?.Dispose();
                    EmissiveMap = null;
                }

                _disposed = true;
            }
        }
    }
}
