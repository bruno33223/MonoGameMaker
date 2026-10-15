using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Graphics3D
{
    /// <summary>
    /// Custom 3D Vertex format holding Position, Normal, Color, and UV texture coordinates.
    /// </summary>
    public struct VertexPositionNormalColorTexture : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 TextureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Color color, Vector2 texCoord)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TextureCoordinate = texCoord;
        }
    }

    /// <summary>
    /// High-performance 3D Mesh with procedural generators for Asteroids, Shards, and geometric primitives.
    /// Includes GPU vertex/index buffers, smooth/faceted normals, and zero-allocation 3D Draw calls.
    /// </summary>
    public class Mesh3D : IDisposable
    {
        public GraphicsDevice GraphicsDevice { get; private set; }
        public VertexBuffer VertexBuffer { get; private set; } = null!;
        public IndexBuffer IndexBuffer { get; private set; } = null!;
        public int VertexCount { get; private set; }
        public int TriangleCount { get; private set; }
        public float BoundingRadius { get; private set; }

        private BasicEffect? _basicEffect;
        private bool _disposed;

        public Mesh3D(GraphicsDevice graphicsDevice, VertexPositionNormalColorTexture[] vertices, short[] indices, float boundingRadius = 1f)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            VertexCount = vertices.Length;
            TriangleCount = indices.Length / 3;
            BoundingRadius = boundingRadius;

            VertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalColorTexture.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);

            IndexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indices);

            _basicEffect = new BasicEffect(graphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = true
            };
        }

        /// <summary>
        /// Renders the 3D mesh with transformation matrices and lighting.
        /// </summary>
        public void Draw(
            GraphicsDevice device,
            Matrix world,
            Matrix view,
            Matrix projection,
            Texture2D? texture = null,
            Color? diffuseColor = null,
            Color? emissiveColor = null,
            Vector3? lightDirection = null,
            Color? lightColor = null,
            Color? ambientColor = null,
            Color? rimColor = null,
            float? specularPower = null)
        {
            if (_disposed || TriangleCount == 0) return;

            _basicEffect ??= new BasicEffect(device);
            _basicEffect.World = world;
            _basicEffect.View = view;
            _basicEffect.Projection = projection;

            // Lighting setup for vibrant, crisp 3D shading
            _basicEffect.LightingEnabled = true;
            _basicEffect.AmbientLightColor = (ambientColor ?? new Color(90, 100, 125)).ToVector3();
            _basicEffect.SpecularPower = specularPower ?? 18f;
            
            // Key Light from near camera angle
            _basicEffect.DirectionalLight0.Enabled = true;
            _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(lightDirection ?? new Vector3(0.25f, 0.45f, -0.95f));
            _basicEffect.DirectionalLight0.DiffuseColor = (lightColor ?? new Color(255, 250, 240)).ToVector3() * 1.15f;
            _basicEffect.DirectionalLight0.SpecularColor = new Vector3(0.7f, 0.7f, 0.7f);

            // Rim Light for edge definition against space (Rayleigh atmospheric scattering)
            _basicEffect.DirectionalLight1.Enabled = true;
            _basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-0.6f, -0.4f, -0.7f));
            _basicEffect.DirectionalLight1.DiffuseColor = (rimColor ?? new Color(140, 190, 255)).ToVector3();
            _basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.4f, 0.5f, 0.7f);

            _basicEffect.DiffuseColor = (diffuseColor ?? Color.White).ToVector3();
            _basicEffect.EmissiveColor = (emissiveColor ?? new Color(25, 35, 45)).ToVector3();

            if (texture != null)
            {
                _basicEffect.TextureEnabled = true;
                _basicEffect.Texture = texture;
            }
            else
            {
                _basicEffect.TextureEnabled = false;
            }

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
            device.DepthStencilState = DepthStencilState.Default;
            device.BlendState = BlendState.Opaque;
            device.SamplerStates[0] = SamplerState.PointClamp;

            foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, TriangleCount);
            }
        }

        #region Procedural Generators

        /// <summary>
        /// Generates a UV sphere mesh with smooth normals and seamless texture coordinates.
        /// </summary>
        public static Mesh3D CreateSphere(GraphicsDevice device, float radius, int rings = 32, int slices = 32)
        {
            if (rings < 2) rings = 2;
            if (slices < 3) slices = 3;

            int vertexCount = (rings + 1) * (slices + 1);
            VertexPositionNormalColorTexture[] vertices = new VertexPositionNormalColorTexture[vertexCount];
            List<short> indices = new List<short>(rings * slices * 6);

            int vertexIndex = 0;
            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float theta = v * MathF.PI;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                for (int s = 0; s <= slices; s++)
                {
                    float u = (float)s / slices;
                    float phi = u * MathHelper.TwoPi;
                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    float x = sinTheta * cosPhi;
                    float y = cosTheta;
                    float z = sinTheta * sinPhi;

                    Vector3 unitDir = new Vector3(x, y, z);
                    Vector3 normal = unitDir.LengthSquared() > 0f ? Vector3.Normalize(unitDir) : Vector3.Up;
                    Vector3 position = normal * radius;
                    Vector2 texCoord = new Vector2(u, v);

                    vertices[vertexIndex++] = new VertexPositionNormalColorTexture(
                        position,
                        normal,
                        Color.White,
                        texCoord
                    );
                }
            }

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < slices; s++)
                {
                    short current = (short)(r * (slices + 1) + s);
                    short next = (short)((r + 1) * (slices + 1) + s);

                    indices.Add(current);
                    indices.Add((short)(current + 1));
                    indices.Add(next);

                    indices.Add((short)(current + 1));
                    indices.Add((short)(next + 1));
                    indices.Add(next);
                }
            }

            return new Mesh3D(device, vertices, indices.ToArray(), radius);
        }

        /// <summary>
        /// Generates a perturbed 3D Asteroid sphere with organic craters, ridges, and spherical UVs.
        /// </summary>
        public static Mesh3D CreateAsteroid(GraphicsDevice device, float radius, int subdivisions = 2, int seed = 0)
        {
            Random rand = new Random(seed);
            List<Vector3> basePositions = new List<Vector3>();
            List<short> indices = new List<short>();

            // Generate initial Icosahedron base
            float t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

            basePositions.Add(Vector3.Normalize(new Vector3(-1,  t,  0)));
            basePositions.Add(Vector3.Normalize(new Vector3( 1,  t,  0)));
            basePositions.Add(Vector3.Normalize(new Vector3(-1, -t,  0)));
            basePositions.Add(Vector3.Normalize(new Vector3( 1, -t,  0)));

            basePositions.Add(Vector3.Normalize(new Vector3( 0, -1,  t)));
            basePositions.Add(Vector3.Normalize(new Vector3( 0,  1,  t)));
            basePositions.Add(Vector3.Normalize(new Vector3( 0, -1, -t)));
            basePositions.Add(Vector3.Normalize(new Vector3( 0,  1, -t)));

            basePositions.Add(Vector3.Normalize(new Vector3( t,  0, -1)));
            basePositions.Add(Vector3.Normalize(new Vector3( t,  0,  1)));
            basePositions.Add(Vector3.Normalize(new Vector3(-t,  0, -1)));
            basePositions.Add(Vector3.Normalize(new Vector3(-t,  0,  1)));

            // 20 Icosahedron Triangles
            int[] baseTriangles = new int[]
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            for (int i = 0; i < baseTriangles.Length; i++)
            {
                indices.Add((short)baseTriangles[i]);
            }

            // Subdivide triangles
            Dictionary<long, short> middlePointCache = new Dictionary<long, short>();

            for (int s = 0; s < subdivisions; s++)
            {
                List<short> newIndices = new List<short>();
                for (int i = 0; i < indices.Count; i += 3)
                {
                    short v1 = indices[i];
                    short v2 = indices[i + 1];
                    short v3 = indices[i + 2];

                    short a = GetMiddlePoint(v1, v2, basePositions, middlePointCache);
                    short b = GetMiddlePoint(v2, v3, basePositions, middlePointCache);
                    short c = GetMiddlePoint(v3, v1, basePositions, middlePointCache);

                    newIndices.Add(v1); newIndices.Add(a); newIndices.Add(c);
                    newIndices.Add(v2); newIndices.Add(b); newIndices.Add(a);
                    newIndices.Add(v3); newIndices.Add(c); newIndices.Add(b);
                    newIndices.Add(a);  newIndices.Add(b); newIndices.Add(c);
                }
                indices = newIndices;
            }

            // Perturb vertices with organic noise
            float[] noiseFrequencies = new float[] { 1.0f, 2.3f, 4.7f, 8.1f };
            float[] noiseAmplitudes = new float[] { 0.22f, 0.12f, 0.06f, 0.03f };

            Vector3[] noiseOffsets = new Vector3[4];
            for (int o = 0; o < 4; o++)
            {
                noiseOffsets[o] = new Vector3(
                    (float)rand.NextDouble() * 10f,
                    (float)rand.NextDouble() * 10f,
                    (float)rand.NextDouble() * 10f
                );
            }

            VertexPositionNormalColorTexture[] vertices = new VertexPositionNormalColorTexture[basePositions.Count];
            float maxR = 0f;

            for (int i = 0; i < basePositions.Count; i++)
            {
                Vector3 unitDir = basePositions[i];
                float displacement = 1.0f;

                for (int h = 0; h < 4; h++)
                {
                    Vector3 sample = unitDir * noiseFrequencies[h] + noiseOffsets[h];
                    float n = MathF.Sin(sample.X * 2.1f) * MathF.Cos(sample.Y * 1.9f) * MathF.Sin(sample.Z * 2.3f);
                    displacement += n * noiseAmplitudes[h];
                }

                // Random slight crater indentations
                float crater = MathF.Sin(unitDir.X * 3.5f + unitDir.Y * 2.8f);
                if (crater > 0.7f)
                {
                    displacement -= (crater - 0.7f) * 0.35f;
                }

                float finalRadius = radius * displacement;
                if (finalRadius > maxR) maxR = finalRadius;

                Vector3 pos = unitDir * finalRadius;

                // Spherical UV coordinates
                float u = 0.5f + (MathF.Atan2(unitDir.Z, unitDir.X) / (MathHelper.TwoPi));
                float v = 0.5f - (MathF.Asin(MathHelper.Clamp(unitDir.Y, -1f, 1f)) / MathHelper.Pi);

                // Subtle mineral coloration tint
                byte shade = (byte)MathHelper.Clamp(180 + (int)(displacement * 40f) + rand.Next(-15, 15), 100, 255);
                Color rockColor = new Color(shade, (byte)(shade * 0.95f), (byte)(shade * 0.9f));

                vertices[i] = new VertexPositionNormalColorTexture(pos, unitDir, rockColor, new Vector2(u, v));
            }

            // Recalculate true surface normals for smooth shading
            Vector3[] calculatedNormals = new Vector3[vertices.Length];
            for (int i = 0; i < indices.Count; i += 3)
            {
                short i0 = indices[i];
                short i1 = indices[i + 1];
                short i2 = indices[i + 2];

                Vector3 p0 = vertices[i0].Position;
                Vector3 p1 = vertices[i1].Position;
                Vector3 p2 = vertices[i2].Position;

                Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
                calculatedNormals[i0] += faceNormal;
                calculatedNormals[i1] += faceNormal;
                calculatedNormals[i2] += faceNormal;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                if (calculatedNormals[i].LengthSquared() > 1e-6f)
                {
                    vertices[i].Normal = Vector3.Normalize(calculatedNormals[i]);
                }
            }

            return new Mesh3D(device, vertices, indices.ToArray(), maxR);
        }

        /// <summary>
        /// Generates a sharp, faceted 3D rock shard for shattered asteroid debris.
        /// </summary>
        public static Mesh3D CreateShard(GraphicsDevice device, float size, int seed = 0)
        {
            Random rand = new Random(seed);

            // Create an irregular 5-vertex polyhedron (jagged wedge / shard)
            Vector3 apex = new Vector3(
                ((float)rand.NextDouble() * 2f - 1f) * size * 0.3f,
                size * (0.8f + (float)rand.NextDouble() * 0.4f),
                ((float)rand.NextDouble() * 2f - 1f) * size * 0.3f
            );

            Vector3 b0 = new Vector3(-size * 0.6f, -size * 0.4f, -size * 0.5f);
            Vector3 b1 = new Vector3( size * 0.7f, -size * 0.5f, -size * 0.4f);
            Vector3 b2 = new Vector3( size * 0.4f, -size * 0.3f,  size * 0.6f);
            Vector3 b3 = new Vector3(-size * 0.5f, -size * 0.4f,  size * 0.5f);

            // Jitter base vertices
            b0 += new Vector3(((float)rand.NextDouble() - 0.5f) * size * 0.2f, ((float)rand.NextDouble() - 0.5f) * size * 0.2f, 0f);
            b1 += new Vector3(((float)rand.NextDouble() - 0.5f) * size * 0.2f, ((float)rand.NextDouble() - 0.5f) * size * 0.2f, 0f);
            b2 += new Vector3(((float)rand.NextDouble() - 0.5f) * size * 0.2f, ((float)rand.NextDouble() - 0.5f) * size * 0.2f, 0f);
            b3 += new Vector3(((float)rand.NextDouble() - 0.5f) * size * 0.2f, ((float)rand.NextDouble() - 0.5f) * size * 0.2f, 0f);

            // 6 Faceted Triangles with distinct face normals
            List<VertexPositionNormalColorTexture> vertices = new List<VertexPositionNormalColorTexture>();
            List<short> indices = new List<short>();

            void AddTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
            {
                Vector3 normal = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
                short baseIdx = (short)vertices.Count;

                Color c = new Color(rand.Next(160, 230), rand.Next(150, 220), rand.Next(140, 210));

                vertices.Add(new VertexPositionNormalColorTexture(p0, normal, c, new Vector2(0, 0)));
                vertices.Add(new VertexPositionNormalColorTexture(p1, normal, c, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalColorTexture(p2, normal, c, new Vector2(0.5f, 1)));

                indices.Add((short)(baseIdx + 0));
                indices.Add((short)(baseIdx + 1));
                indices.Add((short)(baseIdx + 2));
            }

            // Pyramid faces
            AddTriangle(apex, b0, b1);
            AddTriangle(apex, b1, b2);
            AddTriangle(apex, b2, b3);
            AddTriangle(apex, b3, b0);

            // Base faces (2 triangles)
            AddTriangle(b0, b2, b1);
            AddTriangle(b0, b3, b2);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), size);
        }

        private static short GetMiddlePoint(short p1, short p2, List<Vector3> positions, Dictionary<long, short> cache)
        {
            bool firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            long key = (smallerIndex << 32) + greaterIndex;

            if (cache.TryGetValue(key, out short ret))
            {
                return ret;
            }

            Vector3 point1 = positions[p1];
            Vector3 point2 = positions[p2];
            Vector3 middle = Vector3.Normalize((point1 + point2) * 0.5f);

            short i = (short)positions.Count;
            positions.Add(middle);
            cache.Add(key, i);
            return i;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                VertexBuffer?.Dispose();
                IndexBuffer?.Dispose();
                _basicEffect?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
