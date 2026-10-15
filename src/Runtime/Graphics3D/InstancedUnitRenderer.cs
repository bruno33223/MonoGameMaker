using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Graphics3D
{
    /// <summary>
    /// Renderizador instanciado em lote de alta performance capaz de desenhar centenas a milhares
    /// de unidades em 1 única chamada Draw Call via DrawInstancedPrimitives do MonoGame.
    /// </summary>
    public sealed class InstancedUnitRenderer : IDisposable
    {
        private const int MaxInstances = 2048;

        private readonly GraphicsDevice? _graphicsDevice;
        private readonly VertexBuffer? _quadGeometryBuffer;
        private readonly IndexBuffer? _quadIndexBuffer;
        private readonly DynamicVertexBuffer? _instanceBuffer;
        private readonly VertexBufferBinding[]? _bindings;
        private readonly UnitInstanceData[] _instances;
        private int _count;

        public int InstanceCount => _count;
        public int CurrentInstanceCount => _count;
        public int Capacity => MaxInstances;

        public InstancedUnitRenderer(GraphicsDevice? graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _instances = new UnitInstanceData[MaxInstances];

            if (graphicsDevice != null)
            {
                // 1. Geometria Base (Quad Unitário 1x1 centralizado) utilizando tipo canônico VertexPositionTexture
                var vertices = new VertexPositionTexture[]
                {
                    new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0f), new Vector2(0f, 0f)),
                    new VertexPositionTexture(new Vector3( 0.5f, -0.5f, 0f), new Vector2(1f, 0f)),
                    new VertexPositionTexture(new Vector3( 0.5f,  0.5f, 0f), new Vector2(1f, 1f)),
                    new VertexPositionTexture(new Vector3(-0.5f,  0.5f, 0f), new Vector2(0f, 1f))
                };
                var indices = new short[] { 0, 1, 2, 0, 2, 3 };

                _quadGeometryBuffer = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
                _quadGeometryBuffer.SetData(vertices);

                _quadIndexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
                _quadIndexBuffer.SetData(indices);

                // 2. Buffer Dinâmico de Instâncias (Stream 1)
                _instanceBuffer = new DynamicVertexBuffer(graphicsDevice, UnitInstanceData.VertexDeclaration, MaxInstances, BufferUsage.WriteOnly);

                // 3. Pré-alocação estática de Bindings (Zero-GC no loop de renderização)
                _bindings = new VertexBufferBinding[2];
                _bindings[0] = new VertexBufferBinding(_quadGeometryBuffer, 0, 0);
                _bindings[1] = new VertexBufferBinding(_instanceBuffer, 0, 1);
            }
        }

        public void BeginBatch() => _count = 0;

        public void Submit(Vector2 worldPos, float rotation, float scale, Vector4 atlasUV, Color teamColor, bool isSelected, float healthRatio)
        {
            if (_count >= MaxInstances) return;
            ref var inst = ref _instances[_count++];
            inst.TransformData = new Vector4(worldPos.X, worldPos.Y, rotation, scale);
            inst.AtlasUVRect = atlasUV;
            inst.TeamColor = teamColor.ToVector4();
            inst.VisualFlags = new Vector4(isSelected ? 1f : 0f, healthRatio, 0f, 0f);
        }

        public void Render(Effect instancingEffect, Matrix viewProjection)
        {
            if (_count == 0 || _graphicsDevice == null || _instanceBuffer == null || _bindings == null || _quadIndexBuffer == null) return;
            ArgumentNullException.ThrowIfNull(instancingEffect);

            _instanceBuffer.SetData(_instances, 0, _count, SetDataOptions.Discard);

            _graphicsDevice.SetVertexBuffers(_bindings);
            _graphicsDevice.Indices = _quadIndexBuffer;

            instancingEffect.Parameters["ViewProjection"]?.SetValue(viewProjection);

            foreach (var pass in instancingEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawInstancedPrimitives(
                    PrimitiveType.TriangleList,
                    baseVertex: 0,
                    startIndex: 0,
                    primitiveCount: 2,
                    instanceCount: _count
                );
            }
        }

        public void Dispose()
        {
            _quadGeometryBuffer?.Dispose();
            _quadIndexBuffer?.Dispose();
            _instanceBuffer?.Dispose();
        }
    }
}
