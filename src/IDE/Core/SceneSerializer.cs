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
            public string prefabName { get; set; } = string.Empty;
            public float x { get; set; }
            public float y { get; set; }

            // Backward compatibility properties
            public string? assetId { get; set; }
            public string? spriteName { get; set; }
        }

        public class SceneData
        {
            public int Width { get; set; } = 1280;
            public int Height { get; set; } = 720;
            public System.Numerics.Vector3 BackgroundColor { get; set; } = new System.Numerics.Vector3(0.1f, 0.1f, 0.2f);
            public string BackgroundImage { get; set; } = string.Empty;
            public List<EntityInstance> Instances { get; set; } = new List<EntityInstance>();
        }

        public static SceneData LoadScene(string projectRoot, Action<string> logCallback)
        {
            string jsonPath = Path.Combine(projectRoot, "Content", "Scenes", "scene_init.json");
            return LoadScenePath(jsonPath, logCallback);
        }

        public static SceneData LoadScenePath(string jsonPath, Action<string> logCallback)
        {
            try
            {
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var options = new JsonSerializerOptions { IncludeFields = true };
                    SceneData? data = null;
                    try
                    {
                        data = JsonSerializer.Deserialize<SceneData>(jsonContent, options);
                    }
                    catch
                    {
                        var legacyList = JsonSerializer.Deserialize<List<EntityInstance>>(jsonContent, options);
                        if (legacyList != null)
                        {
                            data = new SceneData
                            {
                                Instances = legacyList
                            };
                        }
                    }

                    if (data != null && data.Instances != null)
                    {
                        foreach (var inst in data.Instances)
                        {
                            if (string.IsNullOrEmpty(inst.prefabName))
                            {
                                if (!string.IsNullOrEmpty(inst.assetId)) inst.prefabName = inst.assetId;
                                else if (!string.IsNullOrEmpty(inst.spriteName)) inst.prefabName = inst.spriteName;
                            }
                        }
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error loading scene configuration: {ex.Message}");
            }
            return new SceneData();
        }

        public static bool SaveScene(string projectRoot, SceneData sceneData, Action<string> logCallback)
        {
            string jsonPath = Path.Combine(projectRoot, "Content", "Scenes", "scene_init.json");
            return SaveScenePath(jsonPath, sceneData, logCallback);
        }

        public static bool SaveScenePath(string jsonPath, SceneData sceneData, Action<string> logCallback)
        {
            try
            {
                string? dir = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
                string jsonContent = JsonSerializer.Serialize(sceneData, options);
                
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
