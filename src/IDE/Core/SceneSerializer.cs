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
            public System.Collections.Generic.Dictionary<string, string> CustomProperties { get; set; } = new();

            // Backward compatibility properties
            public string? assetId { get; set; }
            public string? spriteName { get; set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public List<Component> Components { get; set; } = new();

            public T AddComponent<T>() where T : Component, new()
            {
                var component = new T { Parent = this };
                Components.Add(component);
                return component;
            }

            public T? GetComponent<T>() where T : Component
            {
                foreach (var comp in Components)
                {
                    if (comp is T tComp) return tComp;
                }
                return null;
            }
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
            var data = new SceneData();
            try
            {
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                    {
                        JsonElement root = doc.RootElement;
                        
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            // Legacy format: root is just a list of entity instances
                            foreach (var instElement in root.EnumerateArray())
                            {
                                try
                                {
                                    var inst = DeserializeEntityInstance(instElement, logCallback);
                                    if (string.IsNullOrEmpty(inst.prefabName))
                                    {
                                        if (!string.IsNullOrEmpty(inst.assetId)) inst.prefabName = inst.assetId;
                                        else if (!string.IsNullOrEmpty(inst.spriteName)) inst.prefabName = inst.spriteName;
                                    }
                                    data.Instances.Add(inst);
                                }
                                catch (Exception ex)
                                {
                                    logCallback($"Warning: Error deserializing legacy scene entity node: {ex.Message}");
                                }
                            }
                        }
                        else if (root.ValueKind == JsonValueKind.Object)
                        {
                            // Standard SceneData format
                            if (root.TryGetProperty("Width", out var widthProp))
                            {
                                try { data.Width = widthProp.GetInt32(); } catch { data.Width = 1280; }
                            }
                            if (root.TryGetProperty("Height", out var heightProp))
                            {
                                try { data.Height = heightProp.GetInt32(); } catch { data.Height = 720; }
                            }
                            if (root.TryGetProperty("BackgroundImage", out var bgProp))
                            {
                                try { data.BackgroundImage = bgProp.GetString() ?? ""; } catch { data.BackgroundImage = ""; }
                            }
                            if (root.TryGetProperty("BackgroundColor", out var bgColorProp))
                            {
                                try
                                {
                                    float x = 0.1f, y = 0.1f, z = 0.2f;
                                    if (bgColorProp.ValueKind == JsonValueKind.Object)
                                    {
                                        if (bgColorProp.TryGetProperty("X", out var px)) x = px.GetSingle();
                                        if (bgColorProp.TryGetProperty("Y", out var py)) y = py.GetSingle();
                                        if (bgColorProp.TryGetProperty("Z", out var pz)) z = pz.GetSingle();
                                    }
                                    else if (bgColorProp.ValueKind == JsonValueKind.Array && bgColorProp.GetArrayLength() >= 3)
                                    {
                                        x = bgColorProp[0].GetSingle();
                                        y = bgColorProp[1].GetSingle();
                                        z = bgColorProp[2].GetSingle();
                                    }
                                    data.BackgroundColor = new System.Numerics.Vector3(x, y, z);
                                }
                                catch (Exception ex)
                                {
                                    logCallback($"Warning parsing BackgroundColor: {ex.Message}");
                                    data.BackgroundColor = new System.Numerics.Vector3(0.1f, 0.1f, 0.2f);
                                }
                            }

                            if (root.TryGetProperty("Instances", out var instancesProp) && instancesProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var instElement in instancesProp.EnumerateArray())
                                {
                                    try
                                    {
                                        var inst = DeserializeEntityInstance(instElement, logCallback);
                                        if (string.IsNullOrEmpty(inst.prefabName))
                                        {
                                            if (!string.IsNullOrEmpty(inst.assetId)) inst.prefabName = inst.assetId;
                                            else if (!string.IsNullOrEmpty(inst.spriteName)) inst.prefabName = inst.spriteName;
                                        }
                                        data.Instances.Add(inst);
                                    }
                                    catch (Exception ex)
                                    {
                                        logCallback($"Warning: Error deserializing scene entity node: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error loading scene configuration: {ex.Message}");
            }
            return new SceneData();
        }

        private static EntityInstance DeserializeEntityInstance(JsonElement element, Action<string> logCallback)
        {
            var inst = new EntityInstance();
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Scene entity node is not a JSON object");
            }

            foreach (var prop in element.EnumerateObject())
            {
                try
                {
                    switch (prop.Name.ToLower())
                    {
                        case "prefabname":
                            inst.prefabName = prop.Value.GetString() ?? string.Empty;
                            break;
                        case "x":
                            inst.x = prop.Value.GetSingle();
                            break;
                        case "y":
                            inst.y = prop.Value.GetSingle();
                            break;
                        case "assetid":
                            inst.assetId = prop.Value.GetString();
                            break;
                        case "spritename":
                            inst.spriteName = prop.Value.GetString();
                            break;
                        case "customproperties":
                            if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var subProp in prop.Value.EnumerateObject())
                                {
                                    inst.CustomProperties[subProp.Name] = subProp.Value.ToString();
                                }
                            }
                            break;
                        case "components":
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var compElement in prop.Value.EnumerateArray())
                                {
                                    if (compElement.TryGetProperty("type", out var typeProp))
                                    {
                                        string typeName = typeProp.GetString() ?? "";
                                        try
                                        {
                                            Type? componentType = null;
                                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                            {
                                                var t = assembly.GetType(typeName);
                                                if (t == null) t = assembly.GetType("MonoGameMaker.IDE.Core." + typeName);
                                                if (t != null)
                                                {
                                                    componentType = t;
                                                    break;
                                                }
                                            }
                                            if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                                            {
                                                var compInstance = (Component?)Activator.CreateInstance(componentType);
                                                if (compInstance != null)
                                                {
                                                    compInstance.Parent = inst;
                                                    inst.Components.Add(compInstance);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logCallback($"Error deserializing component {typeName}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            // Flexible dictionary/generic fallback for unexpected properties
                            inst.CustomProperties[prop.Name] = prop.Value.ToString();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logCallback($"Warning parsing entity property '{prop.Name}': {ex.Message}");
                }
            }
            return inst;
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
