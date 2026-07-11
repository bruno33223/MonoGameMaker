using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MonoGameMaker.IDE.Core
{
    public static class SceneSerializer
    {
        public class EntityInstance
        {
            public string assetId { get; set; } = string.Empty;
            public float x { get; set; }
            public float y { get; set; }
        }

        public static List<EntityInstance> LoadScene(string projectRoot, Action<string> logCallback)
        {
            string jsonPath = Path.Combine(projectRoot, "Content", "Scenes", "scene_init.json");
            return LoadScenePath(jsonPath, logCallback);
        }

        public static List<EntityInstance> LoadScenePath(string jsonPath, Action<string> logCallback)
        {
            try
            {
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var list = JsonSerializer.Deserialize<List<EntityInstance>>(jsonContent);
                    return list ?? new List<EntityInstance>();
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error loading scene configuration: {ex.Message}");
            }
            return new List<EntityInstance>();
        }

        public static bool SaveScene(string projectRoot, List<EntityInstance> instances, Action<string> logCallback)
        {
            string jsonPath = Path.Combine(projectRoot, "Content", "Scenes", "scene_init.json");
            return SaveScenePath(jsonPath, instances, logCallback);
        }

        public static bool SaveScenePath(string jsonPath, List<EntityInstance> instances, Action<string> logCallback)
        {
            try
            {
                string? dir = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(instances, options);
                
                File.WriteAllText(jsonPath, jsonContent);
                logCallback($"Saved scene configuration to {jsonPath}");
                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Error saving scene configuration: {ex.Message}");
                return false;
            }
        }
    }
}
