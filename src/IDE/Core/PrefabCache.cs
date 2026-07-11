using System;
using System.Collections.Generic;
using System.IO;

namespace MonoGameMaker.IDE.Core
{
    public static class PrefabCache
    {
        private static readonly Dictionary<string, PrefabData> _prefabs = new();

        public static PrefabData GetPrefab(string filePath)
        {
            if (_prefabs.TryGetValue(filePath, out var cached))
            {
                return cached;
            }

            var prefab = PrefabSerializer.LoadPrefab(filePath, msg => Console.WriteLine(msg));
            _prefabs[filePath] = prefab;
            return prefab;
        }

        public static void Clear()
        {
            _prefabs.Clear();
        }

        public static void Invalidate(string filePath)
        {
            _prefabs.Remove(filePath);
        }
    }
}
