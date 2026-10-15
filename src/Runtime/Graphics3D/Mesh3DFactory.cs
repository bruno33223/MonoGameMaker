using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Graphics3D
{
    /// <summary>
    /// Factory for generating procedural 3D meshes: ships, bosses, asteroids,
    /// debris shards, and geometric 3D primitives.
    /// </summary>
    public static class Mesh3DFactory
    {
        #region Primitives

        /// <summary>
        /// Generates a UV sphere mesh with smooth normals.
        /// </summary>
        public static Mesh3D CreateSphereMesh(GraphicsDevice device, float radius = 1.0f, int rings = 32, int slices = 32)
        {
            return Mesh3D.CreateSphere(device, radius, rings, slices);
        }

        /// <summary>
        /// Generates a perturbed asteroid sphere with organic craters and ridges.
        /// </summary>
        public static Mesh3D CreateAsteroidMesh(GraphicsDevice device, float radius = 20.0f, int subdivisions = 2, int seed = 0)
        {
            return Mesh3D.CreateAsteroid(device, radius, subdivisions, seed);
        }

        /// <summary>
        /// Generates a sharp, faceted rock shard for destroyed debris.
        /// </summary>
        public static Mesh3D CreateShardMesh(GraphicsDevice device, float size = 1.0f, int seed = 0)
        {
            return Mesh3D.CreateShard(device, size, seed);
        }

        /// <summary>
        /// Generates a 3D Cube mesh with distinct per-face normals and UV coordinates.
        /// </summary>
        public static Mesh3D CreateCubeMesh(GraphicsDevice device, float size = 1f)
        {
            float half = size * 0.5f;
            var vertices = new List<VertexPositionNormalColorTexture>(24);
            var indices = new List<short>(36);

            void AddFace(Vector3 normal, Vector3 up, Vector3 right, Color color)
            {
                short baseIdx = (short)vertices.Count;
                Vector3 center = normal * half;
                Vector3 p0 = center - right * half - up * half;
                Vector3 p1 = center + right * half - up * half;
                Vector3 p2 = center + right * half + up * half;
                Vector3 p3 = center - right * half + up * half;

                vertices.Add(new VertexPositionNormalColorTexture(p0, normal, color, new Vector2(0, 1)));
                vertices.Add(new VertexPositionNormalColorTexture(p1, normal, color, new Vector2(1, 1)));
                vertices.Add(new VertexPositionNormalColorTexture(p2, normal, color, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalColorTexture(p3, normal, color, new Vector2(0, 0)));

                indices.Add((short)(baseIdx + 0));
                indices.Add((short)(baseIdx + 1));
                indices.Add((short)(baseIdx + 2));

                indices.Add((short)(baseIdx + 0));
                indices.Add((short)(baseIdx + 2));
                indices.Add((short)(baseIdx + 3));
            }

            // 6 Faces
            AddFace(Vector3.Forward, Vector3.Up, Vector3.Right, Color.White);
            AddFace(Vector3.Backward, Vector3.Up, Vector3.Left, Color.White);
            AddFace(Vector3.Up, Vector3.Backward, Vector3.Right, Color.White);
            AddFace(Vector3.Down, Vector3.Forward, Vector3.Right, Color.White);
            AddFace(Vector3.Right, Vector3.Up, Vector3.Backward, Color.White);
            AddFace(Vector3.Left, Vector3.Up, Vector3.Forward, Color.White);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), size * 0.866f);
        }

        /// <summary>
        /// Generates a flat 2D/3D quad plane mesh on the XY or XZ plane.
        /// </summary>
        public static Mesh3D CreatePlaneMesh(GraphicsDevice device, float width = 1f, float height = 1f)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;

            var vertices = new[]
            {
                new VertexPositionNormalColorTexture(new Vector3(-hw,  hh, 0), Vector3.UnitZ, Color.White, new Vector2(0, 0)),
                new VertexPositionNormalColorTexture(new Vector3( hw,  hh, 0), Vector3.UnitZ, Color.White, new Vector2(1, 0)),
                new VertexPositionNormalColorTexture(new Vector3( hw, -hh, 0), Vector3.UnitZ, Color.White, new Vector2(1, 1)),
                new VertexPositionNormalColorTexture(new Vector3(-hw, -hh, 0), Vector3.UnitZ, Color.White, new Vector2(0, 1))
            };

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };
            return new Mesh3D(device, vertices, indices, MathF.Sqrt(hw * hw + hh * hh));
        }

        /// <summary>
        /// Generates a centered 2D/3D Quad mesh on the XY plane for billboards, vortex portals, and holographic projections.
        /// </summary>
        public static Mesh3D CreateQuadMesh(GraphicsDevice device, float width = 1.0f, float height = 1.0f)
        {
            return CreatePlaneMesh(device, width, height);
        }

        /// <summary>
        /// Generates a 3D Shield Dome (hemisphere) mesh with outward normals and smooth UV coordinates.
        /// </summary>
        public static Mesh3D CreateShieldDomeMesh(GraphicsDevice device, float radius = 1.0f, int rings = 24, int slices = 24)
        {
            if (rings < 2) rings = 2;
            if (slices < 3) slices = 3;

            int vertexCount = (rings + 1) * (slices + 1);
            var vertices = new VertexPositionNormalColorTexture[vertexCount];
            var indices = new List<short>(rings * slices * 6);

            int vertexIndex = 0;
            for (int r = 0; r <= rings; r++)
            {
                // Full 3D sphere forcefield: theta spans from top pole (theta = 0) to bottom pole (theta = pi)
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
                    Vector3 normal = unitDir.LengthSquared() > 0.0001f ? Vector3.Normalize(unitDir) : Vector3.Up;
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
        /// Generates a 3D Cylinder mesh with end caps.
        /// </summary>
        public static Mesh3D CreateCylinderMesh(GraphicsDevice device, float radius = 1f, float height = 2f, int slices = 24)
        {
            if (slices < 3) slices = 3;
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();
            float halfH = height * 0.5f;

            // Side vertices
            for (int i = 0; i <= slices; i++)
            {
                float u = (float)i / slices;
                float angle = u * MathHelper.TwoPi;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                Vector3 normal = new Vector3(cos, 0, sin);
                vertices.Add(new VertexPositionNormalColorTexture(new Vector3(cos * radius,  halfH, sin * radius), normal, Color.White, new Vector2(u, 0)));
                vertices.Add(new VertexPositionNormalColorTexture(new Vector3(cos * radius, -halfH, sin * radius), normal, Color.White, new Vector2(u, 1)));
            }

            for (int i = 0; i < slices; i++)
            {
                short v0 = (short)(i * 2);
                short v1 = (short)(i * 2 + 1);
                short v2 = (short)((i + 1) * 2);
                short v3 = (short)((i + 1) * 2 + 1);

                indices.Add(v0); indices.Add(v2); indices.Add(v1);
                indices.Add(v1); indices.Add(v2); indices.Add(v3);
            }

            // Top Cap
            short topCenterIdx = (short)vertices.Count;
            vertices.Add(new VertexPositionNormalColorTexture(new Vector3(0, halfH, 0), Vector3.Up, Color.White, new Vector2(0.5f, 0.5f)));
            for (int i = 0; i <= slices; i++)
            {
                float angle = ((float)i / slices) * MathHelper.TwoPi;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                vertices.Add(new VertexPositionNormalColorTexture(new Vector3(cos * radius, halfH, sin * radius), Vector3.Up, Color.White, new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f)));
            }
            for (int i = 0; i < slices; i++)
            {
                indices.Add(topCenterIdx);
                indices.Add((short)(topCenterIdx + 1 + i));
                indices.Add((short)(topCenterIdx + 2 + i));
            }

            // Bottom Cap
            short bottomCenterIdx = (short)vertices.Count;
            vertices.Add(new VertexPositionNormalColorTexture(new Vector3(0, -halfH, 0), Vector3.Down, Color.White, new Vector2(0.5f, 0.5f)));
            for (int i = 0; i <= slices; i++)
            {
                float angle = ((float)i / slices) * MathHelper.TwoPi;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                vertices.Add(new VertexPositionNormalColorTexture(new Vector3(cos * radius, -halfH, sin * radius), Vector3.Down, Color.White, new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f)));
            }
            for (int i = 0; i < slices; i++)
            {
                indices.Add(bottomCenterIdx);
                indices.Add((short)(bottomCenterIdx + 2 + i));
                indices.Add((short)(bottomCenterIdx + 1 + i));
            }

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), MathF.Sqrt(radius * radius + halfH * halfH));
        }

        /// <summary>
        /// Generates a 3D Cone mesh with base cap.
        /// </summary>
        public static Mesh3D CreateConeMesh(GraphicsDevice device, float radius = 1f, float height = 2f, int slices = 24)
        {
            if (slices < 3) slices = 3;
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();
            float halfH = height * 0.5f;

            Vector3 apex = new Vector3(0, halfH, 0);

            // Sides
            for (int i = 0; i < slices; i++)
            {
                float a1 = ((float)i / slices) * MathHelper.TwoPi;
                float a2 = ((float)(i + 1) / slices) * MathHelper.TwoPi;

                Vector3 p1 = new Vector3(MathF.Cos(a1) * radius, -halfH, MathF.Sin(a1) * radius);
                Vector3 p2 = new Vector3(MathF.Cos(a2) * radius, -halfH, MathF.Sin(a2) * radius);

                Vector3 normal = Vector3.Normalize(Vector3.Cross(p2 - p1, apex - p1));
                short baseIdx = (short)vertices.Count;

                vertices.Add(new VertexPositionNormalColorTexture(apex, normal, Color.White, new Vector2(0.5f, 0)));
                vertices.Add(new VertexPositionNormalColorTexture(p1, normal, Color.White, new Vector2((float)i / slices, 1)));
                vertices.Add(new VertexPositionNormalColorTexture(p2, normal, Color.White, new Vector2((float)(i + 1) / slices, 1)));

                indices.Add((short)(baseIdx + 0));
                indices.Add((short)(baseIdx + 1));
                indices.Add((short)(baseIdx + 2));
            }

            // Bottom Base
            short baseCenterIdx = (short)vertices.Count;
            vertices.Add(new VertexPositionNormalColorTexture(new Vector3(0, -halfH, 0), Vector3.Down, Color.White, new Vector2(0.5f, 0.5f)));
            for (int i = 0; i <= slices; i++)
            {
                float angle = ((float)i / slices) * MathHelper.TwoPi;
                vertices.Add(new VertexPositionNormalColorTexture(
                    new Vector3(MathF.Cos(angle) * radius, -halfH, MathF.Sin(angle) * radius),
                    Vector3.Down,
                    Color.White,
                    new Vector2(MathF.Cos(angle) * 0.5f + 0.5f, MathF.Sin(angle) * 0.5f + 0.5f)));
            }
            for (int i = 0; i < slices; i++)
            {
                indices.Add(baseCenterIdx);
                indices.Add((short)(baseCenterIdx + 2 + i));
                indices.Add((short)(baseCenterIdx + 1 + i));
            }

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), MathF.Sqrt(radius * radius + halfH * halfH));
        }

        #endregion

        #region Ship & Entity 3D Meshes

        /// <summary>
        /// Generates a detailed 3D Player Fighter Ship Mesh with cockpit canopy, swept wings, and twin thrusters.
        /// </summary>
        public static Mesh3D CreatePlayerShipMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            void AddPoly(Vector3[] pts, Color color)
            {
                if (pts.Length < 3) return;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]));
                short baseIdx = (short)vertices.Count;

                for (int i = 0; i < pts.Length; i++)
                {
                    float u = (pts[i].X / (scale * 2.5f)) + 0.5f;
                    float v = (pts[i].Z / (scale * 3.0f)) + 0.5f;
                    vertices.Add(new VertexPositionNormalColorTexture(pts[i], normal, color, new Vector2(u, v)));
                }

                for (int i = 1; i < pts.Length - 1; i++)
                {
                    indices.Add(baseIdx);
                    indices.Add((short)(baseIdx + i));
                    indices.Add((short)(baseIdx + i + 1));
                }
            }

            float s = scale;

            // Nose and Main Hull
            Vector3 noseTip = new Vector3(0, 0, -2.2f * s);
            Vector3 cockpitPeak = new Vector3(0, 0.45f * s, -0.6f * s);
            Vector3 cockpitRear = new Vector3(0, 0.35f * s, 0.6f * s);
            Vector3 hullLeftMid = new Vector3(-0.7f * s, 0.05f * s, 0.2f * s);
            Vector3 hullRightMid = new Vector3(0.7f * s, 0.05f * s, 0.2f * s);
            Vector3 hullLeftRear = new Vector3(-0.55f * s, 0.05f * s, 1.6f * s);
            Vector3 hullRightRear = new Vector3(0.55f * s, 0.05f * s, 1.6f * s);
            Vector3 hullBottomMid = new Vector3(0, -0.3f * s, 0.2f * s);

            // Wingtips & Engine Nacelles
            Vector3 wingLeftTip = new Vector3(-2.4f * s, 0.02f * s, 1.1f * s);
            Vector3 wingRightTip = new Vector3(2.4f * s, 0.02f * s, 1.1f * s);
            Vector3 wingLeftRear = new Vector3(-1.8f * s, 0.05f * s, 1.7f * s);
            Vector3 wingRightRear = new Vector3(1.8f * s, 0.05f * s, 1.7f * s);

            Color hullColor = new Color(210, 230, 255);
            Color canopyColor = new Color(0, 220, 255);
            Color wingColor = new Color(160, 185, 220);
            Color thrusterColor = new Color(50, 70, 95);

            // 1. Cockpit Canopy
            AddPoly(new[] { noseTip, cockpitPeak, hullRightMid }, canopyColor);
            AddPoly(new[] { noseTip, hullLeftMid, cockpitPeak }, canopyColor);
            AddPoly(new[] { cockpitPeak, cockpitRear, hullRightMid }, canopyColor);
            AddPoly(new[] { cockpitPeak, hullLeftMid, cockpitRear }, canopyColor);

            // 2. Main Dorsal Hull
            AddPoly(new[] { cockpitRear, hullRightRear, hullRightMid }, hullColor);
            AddPoly(new[] { cockpitRear, hullLeftMid, hullLeftRear }, hullColor);
            AddPoly(new[] { cockpitRear, hullLeftRear, hullRightRear }, hullColor);

            // 3. Ventral Keel
            AddPoly(new[] { noseTip, hullRightMid, hullBottomMid }, hullColor);
            AddPoly(new[] { noseTip, hullBottomMid, hullLeftMid }, hullColor);
            AddPoly(new[] { hullBottomMid, hullRightMid, hullRightRear }, hullColor);
            AddPoly(new[] { hullBottomMid, hullLeftRear, hullLeftMid }, hullColor);
            AddPoly(new[] { hullBottomMid, hullRightRear, hullLeftRear }, thrusterColor);

            // 4. Swept Port & Starboard Wings (Upper & Lower)
            AddPoly(new[] { hullLeftMid, wingLeftTip, wingLeftRear, hullLeftRear }, wingColor);
            AddPoly(new[] { hullRightMid, hullRightRear, wingRightRear, wingRightTip }, wingColor);
            AddPoly(new[] { hullLeftRear, wingLeftRear, wingLeftTip, hullLeftMid }, wingColor);
            AddPoly(new[] { hullRightRear, hullRightMid, wingRightTip, wingRightRear }, wingColor);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 2.6f);
        }

        /// <summary>
        /// Generates a predatory 3D Alien Viper Interceptor Mesh with needle hull and forward-canted wings.
        /// </summary>
        public static Mesh3D CreateViperMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            void AddPoly(Vector3[] pts, Color color)
            {
                if (pts.Length < 3) return;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]));
                short baseIdx = (short)vertices.Count;

                for (int i = 0; i < pts.Length; i++)
                {
                    float u = (pts[i].X / (scale * 2.0f)) + 0.5f;
                    float v = (pts[i].Z / (scale * 2.5f)) + 0.5f;
                    vertices.Add(new VertexPositionNormalColorTexture(pts[i], normal, color, new Vector2(u, v)));
                }

                for (int i = 1; i < pts.Length - 1; i++)
                {
                    indices.Add(baseIdx);
                    indices.Add((short)(baseIdx + i));
                    indices.Add((short)(baseIdx + i + 1));
                }
            }

            float s = scale;
            Vector3 nose = new Vector3(0, 0, -2.5f * s);
            Vector3 spinePeak = new Vector3(0, 0.4f * s, -0.2f * s);
            Vector3 spineRear = new Vector3(0, 0.3f * s, 1.5f * s);
            Vector3 spineBottom = new Vector3(0, -0.3f * s, 0.5f * s);

            Vector3 wingLeftFront = new Vector3(-1.8f * s, -0.1f * s, -0.8f * s);
            Vector3 wingRightFront = new Vector3(1.8f * s, -0.1f * s, -0.8f * s);
            Vector3 wingLeftMid = new Vector3(-0.6f * s, 0, 0.2f * s);
            Vector3 wingRightMid = new Vector3(0.6f * s, 0, 0.2f * s);
            Vector3 rearLeft = new Vector3(-0.5f * s, 0, 1.4f * s);
            Vector3 rearRight = new Vector3(0.5f * s, 0, 1.4f * s);

            Color viperColor = new Color(50, 240, 120);
            Color darkPlating = new Color(20, 60, 40);

            AddPoly(new[] { nose, spinePeak, wingLeftFront }, viperColor);
            AddPoly(new[] { nose, wingRightFront, spinePeak }, viperColor);
            AddPoly(new[] { spinePeak, spineRear, wingLeftMid }, darkPlating);
            AddPoly(new[] { spinePeak, wingRightMid, spineRear }, darkPlating);
            AddPoly(new[] { wingLeftFront, wingLeftMid, rearLeft }, viperColor);
            AddPoly(new[] { wingRightFront, rearRight, wingRightMid }, viperColor);
            AddPoly(new[] { nose, spineBottom, wingLeftFront }, darkPlating);
            AddPoly(new[] { nose, wingRightFront, spineBottom }, darkPlating);
            AddPoly(new[] { spineBottom, rearLeft, rearRight }, darkPlating);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 2.5f);
        }

        /// <summary>
        /// Generates a heavy 3D Battlecruiser Capital Ship Mesh with armored flank wings and command bridge.
        /// </summary>
        public static Mesh3D CreateCruiserMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            void AddPoly(Vector3[] pts, Color color)
            {
                if (pts.Length < 3) return;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]));
                short baseIdx = (short)vertices.Count;

                for (int i = 0; i < pts.Length; i++)
                {
                    float u = (pts[i].X / (scale * 3.5f)) + 0.5f;
                    float v = (pts[i].Z / (scale * 4.0f)) + 0.5f;
                    vertices.Add(new VertexPositionNormalColorTexture(pts[i], normal, color, new Vector2(u, v)));
                }

                for (int i = 1; i < pts.Length - 1; i++)
                {
                    indices.Add(baseIdx);
                    indices.Add((short)(baseIdx + i));
                    indices.Add((short)(baseIdx + i + 1));
                }
            }

            float s = scale;
            Vector3 bow = new Vector3(0, 0, -3.0f * s);
            Vector3 bowLeft = new Vector3(-0.9f * s, 0.3f * s, -1.8f * s);
            Vector3 bowRight = new Vector3(0.9f * s, 0.3f * s, -1.8f * s);
            Vector3 bridgeTop = new Vector3(0, 0.85f * s, 0.4f * s);
            Vector3 flankLeft = new Vector3(-2.2f * s, 0.2f * s, 1.2f * s);
            Vector3 flankRight = new Vector3(2.2f * s, 0.2f * s, 1.2f * s);
            Vector3 sternLeft = new Vector3(-1.6f * s, 0.1f * s, 2.6f * s);
            Vector3 sternRight = new Vector3(1.6f * s, 0.1f * s, 2.6f * s);
            Vector3 sternMid = new Vector3(0, 0.4f * s, 2.6f * s);
            Vector3 ventralKeel = new Vector3(0, -0.7f * s, 0.2f * s);

            Color armorColor = new Color(180, 70, 240);
            Color bridgeColor = new Color(240, 160, 255);
            Color darkArmor = new Color(60, 25, 80);

            // Bow Sections
            AddPoly(new[] { bow, bowRight, bowLeft }, armorColor);
            AddPoly(new[] { bow, bowLeft, ventralKeel }, darkArmor);
            AddPoly(new[] { bow, ventralKeel, bowRight }, darkArmor);

            // Bridge & Citadel
            AddPoly(new[] { bowLeft, bridgeTop, bowRight }, bridgeColor);
            AddPoly(new[] { bowLeft, flankLeft, bridgeTop }, armorColor);
            AddPoly(new[] { bowRight, bridgeTop, flankRight }, armorColor);
            AddPoly(new[] { bridgeTop, sternMid, flankRight }, armorColor);
            AddPoly(new[] { bridgeTop, flankLeft, sternMid }, armorColor);

            // Flanks & Stern Engines
            AddPoly(new[] { flankLeft, sternLeft, sternMid }, darkArmor);
            AddPoly(new[] { flankRight, sternMid, sternRight }, darkArmor);
            AddPoly(new[] { ventralKeel, sternLeft, flankLeft }, darkArmor);
            AddPoly(new[] { ventralKeel, flankRight, sternRight }, darkArmor);
            AddPoly(new[] { sternMid, sternLeft, sternRight }, new Color(0, 240, 255));

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 3.4f);
        }

        /// <summary>
        /// Generates an aggressive 3D Solar Raider Strike Craft Mesh with twin forward prongs.
        /// </summary>
        public static Mesh3D CreateRaiderMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            void AddPoly(Vector3[] pts, Color color)
            {
                if (pts.Length < 3) return;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]));
                short baseIdx = (short)vertices.Count;

                for (int i = 0; i < pts.Length; i++)
                {
                    float u = (pts[i].X / (scale * 2.2f)) + 0.5f;
                    float v = (pts[i].Z / (scale * 2.6f)) + 0.5f;
                    vertices.Add(new VertexPositionNormalColorTexture(pts[i], normal, color, new Vector2(u, v)));
                }

                for (int i = 1; i < pts.Length - 1; i++)
                {
                    indices.Add(baseIdx);
                    indices.Add((short)(baseIdx + i));
                    indices.Add((short)(baseIdx + i + 1));
                }
            }

            float s = scale;
            Vector3 prongLeft = new Vector3(-0.9f * s, 0, -2.0f * s);
            Vector3 prongRight = new Vector3(0.9f * s, 0, -2.0f * s);
            Vector3 centerNotch = new Vector3(0, 0.1f * s, -0.6f * s);
            Vector3 cabinTop = new Vector3(0, 0.5f * s, 0.3f * s);
            Vector3 wingLeft = new Vector3(-1.9f * s, 0.15f * s, 0.8f * s);
            Vector3 wingRight = new Vector3(1.9f * s, 0.15f * s, 0.8f * s);
            Vector3 tailLeft = new Vector3(-0.8f * s, 0, 1.8f * s);
            Vector3 tailRight = new Vector3(0.8f * s, 0, 1.8f * s);
            Vector3 bottomPeak = new Vector3(0, -0.35f * s, 0.3f * s);

            Color raiderColor = new Color(255, 140, 30);
            Color darkPlating = new Color(70, 35, 15);

            AddPoly(new[] { prongLeft, centerNotch, cabinTop }, raiderColor);
            AddPoly(new[] { prongRight, cabinTop, centerNotch }, raiderColor);
            AddPoly(new[] { prongLeft, cabinTop, wingLeft }, raiderColor);
            AddPoly(new[] { prongRight, wingRight, cabinTop }, raiderColor);
            AddPoly(new[] { cabinTop, wingLeft, tailLeft }, darkPlating);
            AddPoly(new[] { cabinTop, tailRight, wingRight }, darkPlating);
            AddPoly(new[] { cabinTop, tailLeft, tailRight }, darkPlating);
            AddPoly(new[] { prongLeft, wingLeft, bottomPeak }, darkPlating);
            AddPoly(new[] { prongRight, bottomPeak, wingRight }, darkPlating);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 2.3f);
        }

        /// <summary>
        /// Generates a compact, heavy 3D Scarab Rammer Craft Mesh with faceted carapace.
        /// </summary>
        public static Mesh3D CreateScarabMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            void AddPoly(Vector3[] pts, Color color)
            {
                if (pts.Length < 3) return;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]));
                short baseIdx = (short)vertices.Count;

                for (int i = 0; i < pts.Length; i++)
                {
                    float u = (pts[i].X / (scale * 2.0f)) + 0.5f;
                    float v = (pts[i].Z / (scale * 2.2f)) + 0.5f;
                    vertices.Add(new VertexPositionNormalColorTexture(pts[i], normal, color, new Vector2(u, v)));
                }

                for (int i = 1; i < pts.Length - 1; i++)
                {
                    indices.Add(baseIdx);
                    indices.Add((short)(baseIdx + i));
                    indices.Add((short)(baseIdx + i + 1));
                }
            }

            float s = scale;
            Vector3 horn = new Vector3(0, 0, -1.8f * s);
            Vector3 headLeft = new Vector3(-0.7f * s, 0.2f * s, -1.0f * s);
            Vector3 headRight = new Vector3(0.7f * s, 0.2f * s, -1.0f * s);
            Vector3 shellPeak = new Vector3(0, 0.7f * s, 0.2f * s);
            Vector3 flankLeft = new Vector3(-1.3f * s, 0.1f * s, 0.4f * s);
            Vector3 flankRight = new Vector3(1.3f * s, 0.1f * s, 0.4f * s);
            Vector3 rearLeft = new Vector3(-0.9f * s, 0.1f * s, 1.5f * s);
            Vector3 rearRight = new Vector3(0.9f * s, 0.1f * s, 1.5f * s);
            Vector3 rearMid = new Vector3(0, 0.35f * s, 1.6f * s);
            Vector3 belly = new Vector3(0, -0.4f * s, 0.2f * s);

            Color scarabColor = new Color(255, 60, 60);
            Color darkCarapace = new Color(60, 20, 20);

            AddPoly(new[] { horn, headRight, headLeft }, scarabColor);
            AddPoly(new[] { headLeft, headRight, shellPeak }, scarabColor);
            AddPoly(new[] { headLeft, shellPeak, flankLeft }, scarabColor);
            AddPoly(new[] { headRight, flankRight, shellPeak }, scarabColor);
            AddPoly(new[] { shellPeak, flankRight, rearMid }, darkCarapace);
            AddPoly(new[] { shellPeak, rearMid, flankLeft }, darkCarapace);
            AddPoly(new[] { flankLeft, rearLeft, rearMid }, darkCarapace);
            AddPoly(new[] { flankRight, rearMid, rearRight }, darkCarapace);
            AddPoly(new[] { horn, belly, headRight }, darkCarapace);
            AddPoly(new[] { horn, headLeft, belly }, darkCarapace);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 2.0f);
        }

        /// <summary>
        /// Generates a colossal 3D Titan Boss Dreadnought Mesh with multi-tiered battlements and reactor hull.
        /// </summary>
        public static Mesh3D CreateBossMesh(GraphicsDevice device, float scale = 1f)
        {
            return CreateTitanBossMesh(device, scale);
        }

        /// <summary>
        /// Generates a colossal 3D Titan Boss Dreadnought Mesh with multi-tiered battlements and reactor hull.
        /// </summary>
        public static Mesh3D CreateTitanBossMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            void AddPoly(Vector3[] pts, Color color)
            {
                if (pts.Length < 3) return;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]));
                short baseIdx = (short)vertices.Count;

                for (int i = 0; i < pts.Length; i++)
                {
                    float u = (pts[i].X / (scale * 5.0f)) + 0.5f;
                    float v = (pts[i].Z / (scale * 5.0f)) + 0.5f;
                    vertices.Add(new VertexPositionNormalColorTexture(pts[i], normal, color, new Vector2(u, v)));
                }

                for (int i = 1; i < pts.Length - 1; i++)
                {
                    indices.Add(baseIdx);
                    indices.Add((short)(baseIdx + i));
                    indices.Add((short)(baseIdx + i + 1));
                }
            }

            float s = scale;
            Vector3 bow = new Vector3(0, 0.2f * s, -4.0f * s);
            Vector3 bowUpper = new Vector3(0, 0.9f * s, -2.5f * s);
            Vector3 bowLeft = new Vector3(-1.8f * s, 0.3f * s, -2.2f * s);
            Vector3 bowRight = new Vector3(1.8f * s, 0.3f * s, -2.2f * s);

            Vector3 wingLeftOuter = new Vector3(-3.8f * s, 0.2f * s, 0.8f * s);
            Vector3 wingRightOuter = new Vector3(3.8f * s, 0.2f * s, 0.8f * s);
            Vector3 citadelTop = new Vector3(0, 1.4f * s, 0.5f * s);

            Vector3 sternLeft = new Vector3(-2.8f * s, 0.2f * s, 3.5f * s);
            Vector3 sternRight = new Vector3(2.8f * s, 0.2f * s, 3.5f * s);
            Vector3 sternCore = new Vector3(0, 0.6f * s, 3.6f * s);
            Vector3 ventralCore = new Vector3(0, -1.0f * s, 0.2f * s);

            Color goldArmor = new Color(240, 200, 40);
            Color darkMetal = new Color(45, 40, 30);
            Color energyCore = new Color(255, 80, 0);

            // Forward Prows
            AddPoly(new[] { bow, bowRight, bowUpper }, goldArmor);
            AddPoly(new[] { bow, bowUpper, bowLeft }, goldArmor);
            AddPoly(new[] { bowUpper, bowRight, citadelTop }, goldArmor);
            AddPoly(new[] { bowUpper, citadelTop, bowLeft }, goldArmor);

            // Massive Wings
            AddPoly(new[] { bowLeft, citadelTop, wingLeftOuter }, darkMetal);
            AddPoly(new[] { bowRight, wingRightOuter, citadelTop }, darkMetal);
            AddPoly(new[] { citadelTop, sternCore, wingLeftOuter }, goldArmor);
            AddPoly(new[] { citadelTop, wingRightOuter, sternCore }, goldArmor);
            AddPoly(new[] { wingLeftOuter, sternLeft, sternCore }, darkMetal);
            AddPoly(new[] { wingRightOuter, sternCore, sternRight }, darkMetal);

            // Keel
            AddPoly(new[] { bow, bowLeft, ventralCore }, darkMetal);
            AddPoly(new[] { bow, ventralCore, bowRight }, darkMetal);
            AddPoly(new[] { ventralCore, sternLeft, wingLeftOuter }, darkMetal);
            AddPoly(new[] { ventralCore, wingRightOuter, sternRight }, darkMetal);
            AddPoly(new[] { sternCore, sternLeft, sternRight }, energyCore);

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 4.6f);
        }

        /// <summary>
        /// Generates a segmented 3D Void Leviathan Boss Mesh with serpentine radial vertebrae.
        /// </summary>
        public static Mesh3D CreateVoidLeviathanMesh(GraphicsDevice device, float scale = 1f)
        {
            var vertices = new List<VertexPositionNormalColorTexture>();
            var indices = new List<short>();

            int segments = 8;
            float radius = scale * 2.2f;
            float height = scale * 1.2f;

            for (int seg = 0; seg < segments; seg++)
            {
                float a1 = ((float)seg / segments) * MathHelper.TwoPi;
                float a2 = ((float)(seg + 1) / segments) * MathHelper.TwoPi;

                Vector3 p1 = new Vector3(MathF.Cos(a1) * radius, 0, MathF.Sin(a1) * radius);
                Vector3 p2 = new Vector3(MathF.Cos(a2) * radius, 0, MathF.Sin(a2) * radius);
                Vector3 apexTop = new Vector3(0, height, 0);
                Vector3 apexBottom = new Vector3(0, -height, 0);

                Vector3 spike = new Vector3(MathF.Cos((a1 + a2) * 0.5f) * (radius * 1.6f), 0, MathF.Sin((a1 + a2) * 0.5f) * (radius * 1.6f));

                short baseIdx = (short)vertices.Count;
                Color col = (seg % 2 == 0) ? new Color(180, 50, 240) : new Color(60, 15, 90);

                Vector3 normTop = Vector3.Normalize(Vector3.Cross(p2 - p1, apexTop - p1));
                Vector3 normBot = Vector3.Normalize(Vector3.Cross(apexBottom - p1, p2 - p1));

                vertices.Add(new VertexPositionNormalColorTexture(apexTop, normTop, col, new Vector2(0.5f, 0)));
                vertices.Add(new VertexPositionNormalColorTexture(p1, normTop, col, new Vector2(0, 1)));
                vertices.Add(new VertexPositionNormalColorTexture(spike, normTop, new Color(255, 60, 220), new Vector2(0.5f, 1)));
                vertices.Add(new VertexPositionNormalColorTexture(p2, normTop, col, new Vector2(1, 1)));

                vertices.Add(new VertexPositionNormalColorTexture(apexBottom, normBot, col, new Vector2(0.5f, 1)));

                // Top Triangles
                indices.Add((short)(baseIdx + 0)); indices.Add((short)(baseIdx + 1)); indices.Add((short)(baseIdx + 2));
                indices.Add((short)(baseIdx + 0)); indices.Add((short)(baseIdx + 2)); indices.Add((short)(baseIdx + 3));

                // Bottom Triangles
                indices.Add((short)(baseIdx + 4)); indices.Add((short)(baseIdx + 2)); indices.Add((short)(baseIdx + 1));
                indices.Add((short)(baseIdx + 4)); indices.Add((short)(baseIdx + 3)); indices.Add((short)(baseIdx + 2));
            }

            return new Mesh3D(device, vertices.ToArray(), indices.ToArray(), scale * 3.5f);
        }

        #endregion
    }
}
