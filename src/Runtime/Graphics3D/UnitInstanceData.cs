using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Graphics3D
{
    /// <summary>
    /// Struct de 64 bytes com alinhamento sequencial exato (1 linha de cache L1 / 4 registradores vec4)
    /// para GPU Instanced Rendering de unidades militares RTS.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UnitInstanceData : IVertexType
    {
        public Vector4 TransformData; // X, Y = WorldPos, Z = Rotation, W = Scale (16 bytes)
        public Vector4 AtlasUVRect;   // X = UMin, Y = VMin, Z = UWidth, W = VHeight (16 bytes)
        public Vector4 TeamColor;     // RGBA Cor da Facção (16 bytes)
        public Vector4 VisualFlags;   // X = IsSelected, Y = HealthRatio, Z/W = Reservados (16 bytes) -> Total 64 bytes

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Color, 1),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
