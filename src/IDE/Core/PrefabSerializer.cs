using System;
using System.IO;
using System.Text.Json;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public static class PrefabSerializer
    {
        public static PrefabData LoadPrefab(string filePath, Action<string> logCallback)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<PrefabData>(jsonContent);
                    return data ?? new PrefabData();
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error loading prefab {Path.GetFileName(filePath)}: {ex.Message}");
            }
            return new PrefabData();
        }

        public static bool SavePrefab(string filePath, PrefabData data, Action<string> logCallback)
        {
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, jsonContent);
                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Error saving prefab {Path.GetFileName(filePath)}: {ex.Message}");
                return false;
            }
        }
    }
}
