using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pooling;

namespace TestConsole.Tests.Domain
{
    public class ProjectileModel : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Damage { get; set; }
        public bool IsActive { get; set; }

        public void OnSpawn()
        {
            IsActive = true;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Damage = 10f;
        }

        public void OnDespawn()
        {
            IsActive = false;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Damage = 0f;
        }
    }

    public class SoulDropModel : IPoolable
    {
        public Vector2 Position { get; set; }
        public long Value { get; set; } = 1;
        public bool IsActive { get; set; }

        public void OnSpawn()
        {
            IsActive = true;
            Position = Vector2.Zero;
            Value = 1;
        }

        public void OnDespawn()
        {
            IsActive = false;
            Position = Vector2.Zero;
            Value = 0;
        }

        public void MergeWith(SoulDropModel other)
        {
            if (other == null || !other.IsActive) return;
            Value += other.Value;
            other.IsActive = false;
            other.OnDespawn();
        }
    }

    public class SoulManagerModel
    {
        public const int PoolCapacity = 128;
        public const float MergeRadius = 32f;
        private readonly SoulDropModel[] _pool = new SoulDropModel[PoolCapacity];

        public SoulDropModel[] Pool => _pool;

        public int ActiveCount
        {
            get
            {
                int c = 0;
                for (int i = 0; i < PoolCapacity; i++)
                {
                    if (_pool[i].IsActive) c++;
                }
                return c;
            }
        }

        public SoulManagerModel()
        {
            for (int i = 0; i < PoolCapacity; i++)
            {
                _pool[i] = new SoulDropModel();
            }
        }

        public SoulDropModel? SpawnSoul(Vector2 pos, long val)
        {
            if (val <= 0) return null;

            // 1. Procura slot inativo
            for (int i = 0; i < PoolCapacity; i++)
            {
                if (!_pool[i].IsActive)
                {
                    _pool[i].OnSpawn();
                    _pool[i].Position = pos;
                    _pool[i].Value = val;
                    return _pool[i];
                }
            }

            // 2. Pool saturado (128 almas): funde na mais próxima
            SoulDropModel? closest = null;
            float minDistSq = float.MaxValue;
            for (int i = 0; i < PoolCapacity; i++)
            {
                if (!_pool[i].IsActive) continue;
                float dSq = Vector2.DistanceSquared(_pool[i].Position, pos);
                if (dSq < minDistSq)
                {
                    minDistSq = dSq;
                    closest = _pool[i];
                }
            }

            if (closest != null)
            {
                closest.Value += val;
                return closest;
            }

            return null;
        }

        public void UpdateSpatialCoalescence()
        {
            for (int i = 0; i < PoolCapacity; i++)
            {
                var a = _pool[i];
                if (!a.IsActive) continue;

                for (int j = i + 1; j < PoolCapacity; j++)
                {
                    var b = _pool[j];
                    if (!b.IsActive) continue;

                    if (Vector2.Distance(a.Position, b.Position) <= MergeRadius)
                    {
                        a.MergeWith(b);
                    }
                }
            }
        }
    }
}
