using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;
using MonoGameMaker.Runtime.Spatial;

namespace MonoGameMaker.Runtime.Simulation
{
    /// <summary>
    /// Subsistema de simulação em lote de unidades militares RTS (Zero-GC).
    /// </summary>
    public sealed class RtsSimulationSystem
    {
        private readonly int _maxUnits;
        private readonly RtsUnitPhysicsData[] _units;
        private readonly Vector2[] _positionsBuffer;
        private readonly SpatialHashGrid _spatialGrid;
        private readonly int[] _neighborsBuffer;

        public int MaxUnits => _maxUnits;
        public SpatialHashGrid SpatialGrid => _spatialGrid;
        public ReadOnlySpan<RtsUnitPhysicsData> Units => _units;

        public RtsSimulationSystem(int maxUnits, int worldWidth, int worldHeight, float cellSize = 32f)
        {
            _maxUnits = maxUnits;
            _units = new RtsUnitPhysicsData[maxUnits];
            _positionsBuffer = new Vector2[maxUnits];
            _spatialGrid = new SpatialHashGrid(worldWidth, worldHeight, cellSize, maxUnits);
            _neighborsBuffer = new int[64];
        }

        public void SetUnit(int id, in RtsUnitPhysicsData data)
        {
            if ((uint)id < (uint)_maxUnits)
            {
                _units[id] = data;
                _positionsBuffer[id] = data.Position;
            }
        }

        public ref RtsUnitPhysicsData GetUnitRef(int id) => ref _units[id];

        public Vector2 GetUnitPosition(int id) => ((uint)id < (uint)_maxUnits) ? _units[id].Position : Vector2.Zero;

        public void Update(float deltaTime, FlowField? activeFlowField = null)
        {
            if (deltaTime <= 0.0f) return;

            // 1. Reindexação Espacial em O(N)
            _spatialGrid.Clear();
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].IsActive != 0)
                {
                    _positionsBuffer[i] = _units[i].Position;
                    _spatialGrid.Insert(i, _units[i].Position);
                }
            }

            Span<int> neighborsSpan = _neighborsBuffer.AsSpan();

            // 2. Loop de Steering & Separação Suave
            for (int i = 0; i < _maxUnits; i++)
            {
                ref var unit = ref _units[i];
                if (unit.IsActive == 0) continue;

                Vector2 pos = unit.Position;
                Vector2 vel = unit.Velocity;

                // A. Amostragem de FlowField (se houver)
                Vector2 steerFlow = Vector2.Zero;
                if (activeFlowField != null)
                {
                    Vector2 flowDir = activeFlowField.SampleSmoothedDirection(pos);
                    Vector2 desiredVel = flowDir * unit.MaxSpeed;
                    steerFlow = desiredVel - vel;
                }

                // B. Repulsão Suave de Reynolds via SpatialHashGrid
                float queryRadius = unit.Radius * 2.5f;
                int neighborCount = _spatialGrid.FindNeighbors(pos, queryRadius, neighborsSpan, ignoreEntityId: i);

                Vector2 separationForce = Vector2.Zero;
                for (int n = 0; n < neighborCount; n++)
                {
                    int otherId = neighborsSpan[n];
                    Vector2 otherPos = _positionsBuffer[otherId];
                    separationForce += RtsPhysics.CalculateReynoldsRepulsion(pos, otherPos, 45.0f, RtsPhysics.EpsilonAntiSingularity);
                }

                // C. Combinação de Forças
                Vector2 totalForce = steerFlow + separationForce;
                float forceMagSq = totalForce.LengthSquared();
                float maxForceSq = unit.MaxForce * unit.MaxForce;
                if (maxForceSq > 0f && forceMagSq > maxForceSq)
                {
                    totalForce = (totalForce / MathF.Sqrt(forceMagSq)) * unit.MaxForce;
                }

                // D. Integração de Euler Semi-Implícito com Arrasto
                float mass = MathF.Max(0.1f, unit.Mass);
                Vector2 acceleration = totalForce / mass;
                const float dragFactor = 1.5f;
                vel += acceleration * deltaTime;
                vel *= MathF.Max(0f, 1.0f - dragFactor * deltaTime);

                float speedSq = vel.LengthSquared();
                float maxSpeedSq = unit.MaxSpeed * unit.MaxSpeed;
                if (maxSpeedSq > 0f && speedSq > maxSpeedSq)
                {
                    vel = (vel / MathF.Sqrt(speedSq)) * unit.MaxSpeed;
                }

                pos += vel * deltaTime;

                if (speedSq > 0.1f)
                {
                    float targetAngle = MathF.Atan2(vel.Y, vel.X);
                    unit.Rotation = MathHelper.Lerp(unit.Rotation, targetAngle, Math.Clamp(unit.TurnSpeed * deltaTime, 0f, 1f));
                }

                unit.Position = pos;
                unit.Velocity = vel;
                _positionsBuffer[i] = pos;
            }

            // 3. Relaxamento Rígido de Penetração (Circle-Circle Hard Push)
            _spatialGrid.Clear();
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].IsActive != 0)
                {
                    _spatialGrid.Insert(i, _units[i].Position);
                }
            }

            for (int it = 0; it < 12; it++)
            {
                bool anyOverlap = false;
                for (int i = 0; i < _maxUnits; i++)
                {
                    ref var unit = ref _units[i];
                    if (unit.IsActive == 0) continue;

                    int neighborCount = _spatialGrid.FindNeighbors(unit.Position, unit.Radius * 2.5f, neighborsSpan, ignoreEntityId: i);

                    for (int n = 0; n < neighborCount; n++)
                    {
                        int otherId = neighborsSpan[n];
                        if (otherId <= i) continue;

                        ref var other = ref _units[otherId];
                        Vector2 oldA = unit.Position;
                        Vector2 oldB = other.Position;
                        Vector2 posA = oldA;
                        Vector2 posB = oldB;

                        if (RtsPhysics.ResolveHardPush(ref posA, ref posB, unit.Radius, other.Radius, i, otherId))
                        {
                            unit.Position = posA;
                            other.Position = posB;
                            _positionsBuffer[i] = posA;
                            _positionsBuffer[otherId] = posB;
                            _spatialGrid.UpdatePosition(i, oldA, posA);
                            _spatialGrid.UpdatePosition(otherId, oldB, posB);
                            anyOverlap = true;
                        }
                    }
                }
                if (!anyOverlap) break;
            }
        }
    }
}
